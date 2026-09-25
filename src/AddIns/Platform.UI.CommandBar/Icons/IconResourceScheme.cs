using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
/// <para>
/// WPE1 C14: the implementation lives in the add-in's Engine namespace (Engine/IconResourceScheme, no XAML type);
/// this public class forwards to it, unchanged in shape.
/// </para>
/// </remarks>
public static class IconResourceScheme
{
	/// <summary>The URI scheme itself: <c>cb-res</c>.</summary>
	public const string Scheme = Engine.IconResourceScheme.Scheme;

	/// <summary>
	/// Registers an assembly so its embedded icons resolve by simple name.
	/// </summary>
	/// <param name="assembly">The assembly holding the icons.</param>
	/// <remarks>
	/// Only needed for an assembly the runtime cannot find by name on its own. Registering the same
	/// assembly twice is harmless.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
	public static void RegisterAssembly(Assembly assembly) => Engine.IconResourceScheme.RegisterAssembly(assembly);

	/// <summary>
	/// Builds the URI naming one embedded resource.
	/// </summary>
	/// <param name="assembly">The assembly holding the resource. It is registered as a side effect,
	/// so an icon built this way always resolves.</param>
	/// <param name="resourceName">The manifest resource name, or a suffix of one.</param>
	/// <returns>A <c>cb-res://</c> URI.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="assembly"/> or
	/// <paramref name="resourceName"/> is null.</exception>
	public static Uri Create(Assembly assembly, string resourceName) => Engine.IconResourceScheme.Create(assembly, resourceName);

	/// <summary>Whether <paramref name="uri"/> names an embedded resource.</summary>
	/// <param name="uri">The URI to test; null is not.</param>
	/// <returns>True when the URI uses this scheme.</returns>
	public static bool IsResourceUri(Uri? uri) => Engine.IconResourceScheme.IsResourceUri(uri);

	/// <summary>
	/// Opens the resource a <c>cb-res://</c> URI names.
	/// </summary>
	/// <param name="uri">The resource URI.</param>
	/// <param name="stream">The resource's bytes, which the caller disposes.</param>
	/// <returns>True when the assembly and the resource were both found.</returns>
	public static bool TryOpen(Uri? uri, [NotNullWhen(true)] out Stream? stream) => Engine.IconResourceScheme.TryOpen(uri, out stream);
}
