using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// The URI scheme this add-in registers for icons that ship as EMBEDDED RESOURCES rather than as
/// files beside the application.
/// </summary>
/// <remarks>
/// <para>
/// The platform's own image loading understands <c>ms-appx:///</c>, <c>ms-appdata:///</c>,
/// <c>file:</c> and <c>http(s):</c>, all of which name something on a disk or a server. A library
/// that carries its own icon set has nothing on disk: the artwork is compiled into the assembly.
/// This scheme names that artwork, so a library can ship one icon set and every application that
/// references it gets those icons with no build step and nothing to copy.
/// </para>
/// <para>
/// The shape is <c>cb-res://ASSEMBLY/RESOURCE</c>, where ASSEMBLY is the assembly's SIMPLE name and
/// RESOURCE is a manifest resource name. Because an SDK-style project names an embedded resource
/// after its folder path - <c>MyLibrary.Assets.Icons.open.svg</c> for
/// <c>Assets/Icons/open.svg</c> - a SUFFIX also resolves: <c>cb-res://MyLibrary/open.svg</c> finds
/// that resource as long as exactly one resource in the assembly ends that way.
/// </para>
/// <para>
/// An assembly is found by simple name among the assemblies already loaded, and failing that by
/// asking the runtime to load it. An assembly that is neither - one loaded into a custom context,
/// say - can be handed over once with <see cref="RegisterAssembly"/>.
/// </para>
/// </remarks>
public static class IconResourceScheme
{
	/// <summary>The URI scheme itself: <c>cb-res</c>.</summary>
	public const string Scheme = "cb-res";

	private static readonly ConcurrentDictionary<string, Assembly> _registered =
		new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Registers an assembly so its embedded icons resolve by simple name.
	/// </summary>
	/// <param name="assembly">The assembly holding the icons.</param>
	/// <remarks>
	/// Only needed for an assembly the runtime cannot find by name on its own. Registering the same
	/// assembly twice is harmless.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
	public static void RegisterAssembly(Assembly assembly)
	{
		if (assembly is null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		var name = assembly.GetName().Name;
		if (!string.IsNullOrEmpty(name))
		{
			_registered[name] = assembly;
		}
	}

	/// <summary>
	/// Builds the URI naming one embedded resource.
	/// </summary>
	/// <param name="assembly">The assembly holding the resource. It is registered as a side effect,
	/// so an icon built this way always resolves.</param>
	/// <param name="resourceName">The manifest resource name, or a suffix of one.</param>
	/// <returns>A <c>cb-res://</c> URI.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="assembly"/> or
	/// <paramref name="resourceName"/> is null.</exception>
	public static Uri Create(Assembly assembly, string resourceName)
	{
		if (assembly is null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		if (resourceName is null)
		{
			throw new ArgumentNullException(nameof(resourceName));
		}

		RegisterAssembly(assembly);
		return new Uri($"{Scheme}://{assembly.GetName().Name}/{Uri.EscapeDataString(resourceName)}");
	}

	/// <summary>Whether <paramref name="uri"/> names an embedded resource.</summary>
	/// <param name="uri">The URI to test; null is not.</param>
	/// <returns>True when the URI uses this scheme.</returns>
	public static bool IsResourceUri(Uri? uri)
		=> uri is { IsAbsoluteUri: true }
			&& string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Opens the resource a <c>cb-res://</c> URI names.
	/// </summary>
	/// <param name="uri">The resource URI.</param>
	/// <param name="stream">The resource's bytes, which the caller disposes.</param>
	/// <returns>True when the assembly and the resource were both found.</returns>
	public static bool TryOpen(Uri? uri, [NotNullWhen(true)] out Stream? stream)
	{
		stream = null;

		if (!IsResourceUri(uri))
		{
			return false;
		}

		var assemblyName = uri!.Host;
		var resourceName = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');

		if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(resourceName))
		{
			return false;
		}

		if (FindAssembly(assemblyName) is not { } assembly)
		{
			return false;
		}

		stream = assembly.GetManifestResourceStream(resourceName);
		if (stream is not null)
		{
			return true;
		}

		//An SDK-style project prefixes a resource with the root namespace and its folder path, so
		//the terse form in the URI is matched as a suffix - but only when it is unambiguous.
		var suffix = "." + resourceName;
		var matches = assembly.GetManifestResourceNames()
			.Where(name => name.EndsWith(suffix, StringComparison.Ordinal))
			.ToArray();

		if (matches.Length == 1)
		{
			stream = assembly.GetManifestResourceStream(matches[0]);
		}

		return stream is not null;
	}

	private static Assembly? FindAssembly(string simpleName)
	{
		if (_registered.TryGetValue(simpleName, out var registered))
		{
			return registered;
		}

		var loaded = AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(a => string.Equals(a.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));

		if (loaded is not null)
		{
			_registered[simpleName] = loaded;
			return loaded;
		}

		try
		{
			var byName = Assembly.Load(new AssemblyName(simpleName));
			_registered[simpleName] = byName;
			return byName;
		}
		catch (Exception)
		{
			//An icon that names an assembly this process does not have is a missing icon, not a
			//crash: the element shows nothing and the application keeps running.
			return null;
		}
	}
}
