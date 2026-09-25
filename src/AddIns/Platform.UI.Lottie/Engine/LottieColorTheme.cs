#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;

namespace CodeBrix.Platform.UI.Lottie.Engine;

/// <summary>
/// The Lottie colour-theming ENGINE (WPE1 C9): parses an animation's JSON into the vendored mutable System.Json DOM,
/// finds the shapes whose names carry colour bindings ("{Color=MyBinding}", PropertyBindingsParser), and rewrites their
/// colour keyframes when a binding's colour is set. Moved verbatim out of ThemableLottieVisualSource, which wraps it;
/// names no XAML or WinRT type. Colours are packed ARGB values (0xAARRGGBB - the packing of SKColor and of
/// Windows.UI.Color), so this compiles in every flavor of the add-in, including the unit-test one without SkiaSharp.
/// </summary>
internal sealed class LottieColorTheme
{
	private readonly Dictionary<string, ColorBinding> _colorsBindings = new Dictionary<string, ColorBinding>(2);

	private JsonValue? _currentDocument;

	/// <summary>Whether a document has been loaded (colour changes are applied only then).</summary>
	internal bool HasDocument => _currentDocument != null;

	/// <summary>The current (themed) JSON of the animation, or null before a document is loaded.</summary>
	/// <returns>The JSON.</returns>
	internal string? GetJson() => _currentDocument?.ToString();

	/// <summary>
	/// Parses a document and collects its colour-bound shapes; a pending colour of every binding carries over (its
	/// current value becomes pending again, to be applied to the new document). Does nothing when the JSON is not an
	/// object.
	/// </summary>
	/// <param name="json">The animation's JSON (not closed here).</param>
	internal void Load(Stream json)
	{
		JsonObject? document = JsonValue.Load(json) as JsonObject;

		if (document == null)
		{
			return;
		}

		_currentDocument = document;

		foreach (var colorBinding in _colorsBindings)
		{
			colorBinding.Value.Elements.Clear();
			colorBinding.Value.NextValue ??= colorBinding.Value.CurrentValue;
		}

		void ParseLayers(JsonArray layers)
		{
			if (layers == null)
			{
				return; // potentially invalid lottie file
			}

			foreach (var layer in layers)
			{
				if (layer is JsonObject l)
				{
					if (l.TryGetValue("shapes", out var shapesValue))
					{
						if (shapesValue is JsonArray shapes)
						{
							foreach (var shape in shapes)
							{
								if (shape is JsonObject s)
								{
									ParseShape(s);
								}
							}
						}
					}
				}
			}
		}

		void ParseShape(JsonObject shapeElement)
		{
			if (!shapeElement.TryGetValue("ty", out var typeValue))
			{
				return; // potentially invalid lottie file
			}
			if (typeValue.JsonType != JsonType.String)
			{
				return; // potentially invalid lottie file
			}

			var shapeType = (string)typeValue;

			if (shapeType != null && shapeType.Equals("gr"))
			{
				// That's a group

				if (!shapeElement.TryGetValue("it", out var itemsProperty)
					|| itemsProperty.JsonType != JsonType.Array)
				{
					return; // potentially invalid lottie file
				}
				if (itemsProperty is JsonArray items)
				{
					foreach (var item in items)
					{
						if (item is JsonObject s)
						{
							ParseShape(s);
						}
					}
				}

				return;
			}

			if (!shapeElement.TryGetValue("nm", out var nameProperty)
				|| nameProperty.JsonType != JsonType.String)
			{
				return; // No name
			}

			var name = (string)nameProperty;

			if (!string.IsNullOrWhiteSpace(name))
			{
				var elementBindings = PropertyBindingsParser.ParseBindings(name);
				if (elementBindings.Length > 0)
				{
					foreach (var binding in elementBindings)
					{
						if (binding.propertyName.Equals("Color", StringComparison.Ordinal))
						{
							if (_colorsBindings.TryGetValue(binding.bindingName, out var colorBinding))
							{
								colorBinding.Elements.Add(shapeElement);
							}
							else
							{
								colorBinding = new ColorBinding();
								colorBinding.Elements.Add(shapeElement);
								_colorsBindings[binding.bindingName] = colorBinding;
							}
						}
					}
				}
			}
		}

		if (document.TryGetValue("layers", out var lyrs) && lyrs is JsonArray documentLayers)
		{
			ParseLayers(documentLayers);
		}
	}

	/// <summary>
	/// Writes every pending binding colour into the document's bound shapes (as the [r, g, b, a] fractions Lottie uses)
	/// and makes it the binding's current colour. Returns true when a shape changed.
	/// </summary>
	/// <returns>Whether the document changed.</returns>
	internal bool ApplyProperties()
	{
		var changed = false;
		foreach (var colorBinding in _colorsBindings)
		{
			if (!(colorBinding.Value.NextValue is { } color))
			{
				continue; // nothing to change
			}

			var colorComponents = new[] { Red(color) / 255f, Green(color) / 255f, Blue(color) / 255f, Alpha(color) / 255f };

			foreach (var element in colorBinding.Value.Elements)
			{
				if (element.TryGetValue("c", out var cElm)
					&& cElm is JsonObject c
					&& c.TryGetValue("k", out var kElm)
					&& kElm is JsonArray k)
				{

					k.Clear();
					k.Add(colorComponents[0]);
					k.Add(colorComponents[1]);
					k.Add(colorComponents[2]);
					k.Add(colorComponents[3]);

					changed = true;
				}
			}

			colorBinding.Value.CurrentValue = colorBinding.Value.NextValue;
			colorBinding.Value.NextValue = null;
		}

		return changed;
	}

	/// <summary>
	/// Sets the colour of a binding (null clears it); it is applied by the next <see cref="ApplyProperties"/>.
	/// </summary>
	/// <param name="bindingName">The binding name.</param>
	/// <param name="argb">The packed ARGB colour, or null.</param>
	internal void SetColor(string bindingName, uint? argb)
	{
		if (_colorsBindings.TryGetValue(bindingName, out var existing))
		{
			existing.NextValue = argb;
		}
		else
		{
			_colorsBindings[bindingName] = new ColorBinding { NextValue = argb };
		}
	}

	/// <summary>The colour of a binding (the pending one, else the current one), or null.</summary>
	/// <param name="bindingName">The binding name.</param>
	/// <returns>The packed ARGB colour, or null.</returns>
	internal uint? GetColor(string bindingName)
	{
		if (_colorsBindings.TryGetValue(bindingName, out var existing))
		{
			return existing.NextValue ?? existing.CurrentValue;
		}

		return default;
	}

	/// <summary>
	/// The cache key of the themed document: the source's key, then each binding's name and current colour
	/// ("#AARRGGBB", as Windows.UI.Color formats itself; empty when none).
	/// </summary>
	/// <param name="sourceCacheKey">The source's cache key.</param>
	/// <returns>The key.</returns>
	internal string GetCacheKey(string? sourceCacheKey)
	{
		var propertiesKey = string.Join("-", _colorsBindings
			.Select(kvp => $"{kvp.Key}-{FormatColor(kvp.Value.CurrentValue)}"));

		return sourceCacheKey + "-" + propertiesKey;
	}

	/// <summary>Packs a colour's components as ARGB (the SKColor / Windows.UI.Color packing).</summary>
	internal static uint ToArgb(byte a, byte r, byte g, byte b) => ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;

	internal static byte Alpha(uint argb) => (byte)(argb >> 24);

	internal static byte Red(uint argb) => (byte)(argb >> 16);

	internal static byte Green(uint argb) => (byte)(argb >> 8);

	internal static byte Blue(uint argb) => (byte)argb;

	private static string FormatColor(uint? argb) =>
		argb is { } c ? string.Format(null, "#{0:X2}{1:X2}{2:X2}{3:X2}", Alpha(c), Red(c), Green(c), Blue(c)) : string.Empty;

	private sealed class ColorBinding
	{
		internal List<JsonObject> Elements { get; } = new List<JsonObject>(1);
		internal uint? CurrentValue { get; set; }
		internal uint? NextValue { get; set; }
	}
}
