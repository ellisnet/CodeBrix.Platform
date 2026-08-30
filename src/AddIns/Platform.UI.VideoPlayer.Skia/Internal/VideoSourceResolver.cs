using System;
using System.IO;
using System.Reflection;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;

/// <summary>
/// Resolves the video source forms the VideoPlayer AddIn accepts - a plain file path, a
/// file:// URI, an http:// or https:// address, an ms-appx:/// application-asset URI, or an
/// embedded://Assembly/Resource.Name embedded-resource URI (the same scheme the CodeBrix.Platform
/// SVG, Lottie and AudioPlayer AddIns use).
/// </summary>
/// <remarks>
/// A copy of the AudioPlayer AddIn's resolver, extended with the two network schemes: AddIns share
/// no code with one another (the SVG and Lottie AddIns already carry their own copies), and the
/// playback session opens an address itself, so an http(s) source is passed straight through as a
/// "path" for the session to fetch.
/// </remarks>
internal static class VideoSourceResolver
{
	/// <summary>
	/// Resolves <paramref name="source"/> to either something the playback session can open by
	/// name - a local file path or an http(s) address - or an open stream (exactly one of the two
	/// is non-null). Throws when the source cannot be resolved.
	/// </summary>
	/// <param name="source">The source in any form this AddIn accepts.</param>
	public static (string? PathOrUrl, Stream? Stream) Resolve(string source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			throw new ArgumentException("The video source is empty.", nameof(source));
		}

		if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case "embedded":
					return (null, OpenEmbeddedResource(uri));
				case "ms-appx":
					return (Path.Join(Package.Current.InstalledPath, AssetRelativePath(source)), null);
				case "file":
					return (uri.LocalPath, null);
				case "http":
				case "https":
					// The session reads an address itself (progressive/range HTTP), so hand the
					// address over untouched rather than opening a stream here.
					return (source, null);
			}
		}

		// Not a recognized URI: treat it as a filesystem path.
		return (source, null);
	}

	/// <summary>
	/// The path an ms-appx: URI names, relative to the application's installed folder.
	/// </summary>
	/// <remarks>
	/// Read from the original text rather than from the parsed <see cref="Uri"/>, which gets two
	/// things wrong for an asset path. It percent-encodes: an asset whose name contains a space
	/// comes back as "My%20Clip.webm", which no filesystem holds. And it lower-cases the host, so
	/// the two-slash form (ms-appx://LibraryName/file) - the natural way to address an asset that
	/// arrived in a library package, where the first segment is an assembly name - would resolve
	/// with that folder's name in the wrong case, which fails on a case-sensitive filesystem.
	/// Both slash forms name the same thing here: everything after the scheme, without its
	/// leading slashes.
	/// </remarks>
	private static string AssetRelativePath(string source)
	{
		var afterScheme = source[(source.IndexOf(':') + 1)..].TrimStart('/');

		// A caller may equally have written the escaped form, so unescape whatever arrived: on
		// text that carries no escape sequence this returns it unchanged.
		return Uri.UnescapeDataString(afterScheme);
	}

	/// <summary>
	/// Opens an embedded://AssemblyName/Manifest.Resource.Name resource. An assembly name of
	/// "." refers to the application's own assembly, and "(assembly)" inside the resource name
	/// is replaced with the resolved assembly's name - both matching the Lottie AddIn's behavior.
	/// </summary>
	private static Stream OpenEmbeddedResource(Uri uri)
	{
		var assemblyName = uri.Host;
		var assembly = assemblyName == "."
			? Application.Current.GetType().Assembly
			: Assembly.Load(assemblyName);

		var resourceName = Uri.UnescapeDataString(uri.AbsolutePath[1..]).Replace("(assembly)", assembly.GetName().Name);
		var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream is null)
		{
			throw new FileNotFoundException(
				$"Unable to find an embedded resource named '{resourceName}' in assembly '{assembly.GetName().Name}'.");
		}
		return stream;
	}
}
