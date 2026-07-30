#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml;

using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightingManager.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Manages a list of syntax highlighting definitions.
/// </summary>
/// <remarks>
/// All members on this class (including instance members) are thread-safe.
/// </remarks>
public class HighlightingManager : IHighlightingDefinitionReferenceResolver
{
	sealed class DelayLoadedHighlightingDefinition : IHighlightingDefinition
	{
		readonly object lockObj = new object();
		readonly string? name;
		Func<IHighlightingDefinition>? lazyLoadingFunction;
		IHighlightingDefinition? definition;
		Exception? storedException;

		public DelayLoadedHighlightingDefinition(string? name, Func<IHighlightingDefinition> lazyLoadingFunction)
		{
			this.name = name;
			this.lazyLoadingFunction = lazyLoadingFunction;
		}

		public string? Name
		{
			get
			{
				if (name != null)
				{
					return name;
				}
				else
				{
					return GetDefinition().Name;
				}
			}
		}

		IHighlightingDefinition GetDefinition()
		{
			Func<IHighlightingDefinition>? func;
			lock (lockObj)
			{
				if (this.definition != null)
				{
					return this.definition;
				}
				func = this.lazyLoadingFunction;
			}
			Exception? exception = null;
			IHighlightingDefinition? def = null;
			try
			{
				using (var busyLock = BusyManager.Enter(this))
				{
					if (!busyLock.Success)
					{
						throw new InvalidOperationException("Tried to create delay-loaded highlighting definition recursively. Make sure the are no cyclic references between the highlighting definitions.");
					}
					// func is only null after the first load attempt completed; in that case
					// storedException is already set and rethrown below, matching the old behavior.
					if (func == null)
					{
						throw new InvalidOperationException("Function for delay-loading highlighting definition returned null");
					}
					def = func();
				}
				if (def == null)
				{
					throw new InvalidOperationException("Function for delay-loading highlighting definition returned null");
				}
			}
			catch (Exception ex)
			{
				exception = ex;
			}
			lock (lockObj)
			{
				this.lazyLoadingFunction = null;
				if (this.definition == null && this.storedException == null)
				{
					this.definition = def;
					this.storedException = exception;
				}
				if (this.storedException != null)
				{
					throw new HighlightingDefinitionInvalidException("Error delay-loading highlighting definition", this.storedException);
				}
				return this.definition!;
			}
		}

		public HighlightingRuleSet MainRuleSet
		{
			get
			{
				return GetDefinition().MainRuleSet;
			}
		}

		public HighlightingRuleSet? GetNamedRuleSet(string name)
		{
			return GetDefinition().GetNamedRuleSet(name);
		}

		public HighlightingColor? GetNamedColor(string name)
		{
			return GetDefinition().GetNamedColor(name);
		}

		public IEnumerable<HighlightingColor> NamedHighlightingColors
		{
			get
			{
				return GetDefinition().NamedHighlightingColors;
			}
		}

		public override string? ToString()
		{
			return this.Name;
		}

		public IDictionary<string, string> Properties
		{
			get
			{
				return GetDefinition().Properties;
			}
		}
	}

	readonly object lockObj = new object();
	Dictionary<string, IHighlightingDefinition> highlightingsByName = new Dictionary<string, IHighlightingDefinition>();
	Dictionary<string, IHighlightingDefinition> highlightingsByExtension = new Dictionary<string, IHighlightingDefinition>(StringComparer.OrdinalIgnoreCase);
	List<IHighlightingDefinition> allHighlightings = new List<IHighlightingDefinition>();

	/// <summary>
	/// Gets a highlighting definition by name.
	/// Returns null if the definition is not found.
	/// </summary>
	public IHighlightingDefinition? GetDefinition(string name)
	{
		lock (lockObj)
		{
			IHighlightingDefinition? rh;
			if (highlightingsByName.TryGetValue(name, out rh))
			{
				return rh;
			}
			else
			{
				return null;
			}
		}
	}

	/// <summary>
	/// Gets a copy of all highlightings.
	/// </summary>
	public ReadOnlyCollection<IHighlightingDefinition> HighlightingDefinitions
	{
		get
		{
			lock (lockObj)
			{
				return Array.AsReadOnly(allHighlightings.ToArray());
			}
		}
	}

	/// <summary>
	/// Gets a highlighting definition by extension.
	/// Returns null if the definition is not found.
	/// </summary>
	public IHighlightingDefinition? GetDefinitionByExtension(string extension)
	{
		lock (lockObj)
		{
			IHighlightingDefinition? rh;
			if (highlightingsByExtension.TryGetValue(extension, out rh))
			{
				return rh;
			}
			else
			{
				return null;
			}
		}
	}

	/// <summary>
	/// Registers a highlighting definition.
	/// </summary>
	/// <param name="name">The name to register the definition with.</param>
	/// <param name="extensions">The file extensions to register the definition for.</param>
	/// <param name="highlighting">The highlighting definition.</param>
	public void RegisterHighlighting(string? name, string[]? extensions, IHighlightingDefinition highlighting)
	{
		if (highlighting == null)
		{
			throw new ArgumentNullException(nameof(highlighting));
		}

		lock (lockObj)
		{
			if (name != null)
			{
				if (highlightingsByName.TryGetValue(name, out var existingDefinition))
				{
					allHighlightings.Remove(existingDefinition);
				}
				highlightingsByName[name] = highlighting;
			}
			if (extensions != null)
			{
				foreach (string ext in extensions)
				{
					highlightingsByExtension[ext] = highlighting;
				}
			}
			allHighlightings.Add(highlighting);
		}
	}

	/// <summary>
	/// Registers a highlighting definition.
	/// </summary>
	/// <param name="name">The name to register the definition with.</param>
	/// <param name="extensions">The file extensions to register the definition for.</param>
	/// <param name="lazyLoadedHighlighting">A function that loads the highlighting definition.</param>
	public void RegisterHighlighting(string? name, string[]? extensions, Func<IHighlightingDefinition> lazyLoadedHighlighting)
	{
		if (lazyLoadedHighlighting == null)
		{
			throw new ArgumentNullException(nameof(lazyLoadedHighlighting));
		}
		RegisterHighlighting(name, extensions, new DelayLoadedHighlightingDefinition(name, lazyLoadedHighlighting));
	}

	/// <summary>
	/// Gets the default HighlightingManager instance.
	/// The default HighlightingManager comes with built-in highlightings.
	/// </summary>
	public static HighlightingManager Instance
	{
		get
		{
			return DefaultHighlightingManager.Instance;
		}
	}

	internal sealed class DefaultHighlightingManager : HighlightingManager
	{
		public new static readonly DefaultHighlightingManager Instance = new DefaultHighlightingManager();

		public DefaultHighlightingManager()
		{
			Resources.RegisterBuiltInHighlightings(this);
		}

		// Registering a built-in highlighting
		internal void RegisterHighlighting(string name, string[]? extensions, string resourceName)
		{
			try
			{
#if DEBUG
				// don't use lazy-loading in debug builds, show errors immediately
				Xshd.XshdSyntaxDefinition xshd;
				using (Stream s = Resources.OpenStream(resourceName))
				{
					using (XmlTextReader reader = new XmlTextReader(s))
					{
						xshd = Xshd.HighlightingLoader.LoadXshd(reader, false);
					}
				}
				Debug.Assert(name == xshd.Name);
				if (extensions != null)
				{
					Debug.Assert(System.Linq.Enumerable.SequenceEqual(extensions, xshd.Extensions));
				}
				else
				{
					Debug.Assert(xshd.Extensions.Count == 0);
				}

				RegisterHighlighting(name, extensions, Xshd.HighlightingLoader.Load(xshd, this));
#else
				RegisterHighlighting(name, extensions, LoadHighlighting(resourceName));
#endif
			}
			catch (HighlightingDefinitionInvalidException ex)
			{
				throw new InvalidOperationException("The built-in highlighting '" + name + "' is invalid.", ex);
			}
		}

		Func<IHighlightingDefinition> LoadHighlighting(string resourceName)
		{
			Func<IHighlightingDefinition> func = delegate
			{
				Xshd.XshdSyntaxDefinition xshd;
				using (Stream s = Resources.OpenStream(resourceName))
				{
					using (XmlTextReader reader = new XmlTextReader(s))
					{
						// in release builds, skip validating the built-in highlightings
						xshd = Xshd.HighlightingLoader.LoadXshd(reader, true);
					}
				}
				return Xshd.HighlightingLoader.Load(xshd, this);
			};
			return func;
		}
	}
}
