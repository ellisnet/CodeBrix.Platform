using System;
using System.IO;
using System.Reflection;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.AudioPlayer.Skia.Internal;

/// <summary>
/// Resolves the audio source forms the AudioPlayer AddIn accepts - a plain file path, a
/// file:// URI, an ms-appx:/// application-asset URI, or an embedded://Assembly/Resource.Name
/// embedded-resource URI (the same scheme the CodeBrix.Platform SVG and Lottie AddIns use).
/// </summary>
internal static class AudioSourceResolver
{
	/// <summary>
	/// Resolves <paramref name="source"/> to either a local file path or an open stream
	/// (exactly one of the two is non-null). Throws when the source cannot be resolved.
	/// </summary>
	public static (string? FilePath, Stream? Stream) Resolve(string source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			throw new ArgumentException("The audio source is empty.", nameof(source));
		}

		if (Uri.TryCreate(source, UriKind.Absolute, out var uri)
			&& uri.Scheme.Equals("embedded", StringComparison.OrdinalIgnoreCase))
		{
			return (null, OpenEmbeddedResource(uri));
		}

		// Everything else names a place on disk - an ms-appx:/// asset, a file:// URI, or, when it is
		// no recognized URI at all, a filesystem path.
		return (ResolveLocalPathOrNull(source), null);
	}

	/// <summary>
	/// Resolves <paramref name="source"/> to the local file-system path it names, or returns null
	/// when the source names something that only exists as a stream (an embedded resource).
	/// </summary>
	/// <remarks>
	/// For sources that must be real files or folders on disk because other files sit beside them -
	/// an SFZ instrument and its sample folder, a Decent Sampler preset and its samples, a
	/// Decent Sampler archive that is read in place - rather than sources that merely prefer to be.
	/// Nothing is opened: what is wanted is the NAME, and a source that has none has already failed.
	/// The path is returned whether or not anything exists there, so that the failure to open it is
	/// reported by whatever tried, in its own words.
	/// </remarks>
	public static string? ResolveLocalPathOrNull(string source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			throw new ArgumentException("The audio source is empty.", nameof(source));
		}

		if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case "embedded":
					return null;
				case "ms-appx":
					return Path.Join(Package.Current.InstalledPath, AssetRelativePath(source));
				case "file":
					return uri.LocalPath;
			}
		}

		return source;
	}

	/// <summary>
	/// The path an ms-appx: URI names, relative to the application's installed folder.
	/// </summary>
	/// <remarks>
	/// Read from the original text rather than from the parsed <see cref="Uri"/>, which gets two
	/// things wrong for an asset path. It percent-encodes: an asset whose name contains a space
	/// comes back as "My%20Song.mp3", which no filesystem holds. And it lower-cases the host, so
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

	/// <summary>
	/// Reads the full content of <paramref name="source"/> into memory (used by SoundEffect,
	/// which preloads effects so no disk I/O happens on the real-time audio thread).
	/// </summary>
	public static byte[] ReadAllBytes(string source)
	{
		var (filePath, stream) = Resolve(source);
		if (filePath is not null)
		{
			return File.ReadAllBytes(filePath);
		}

		using (stream)
		{
			using var buffer = new MemoryStream();
			stream!.CopyTo(buffer);
			return buffer.ToArray();
		}
	}
}
