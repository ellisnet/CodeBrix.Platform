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

		if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case "embedded":
					return (null, OpenEmbeddedResource(uri));
				case "ms-appx":
					return (Path.Join(Package.Current.InstalledPath, uri.PathAndQuery), null);
				case "file":
					return (uri.LocalPath, null);
			}
		}

		// Not a recognized URI: treat it as a filesystem path.
		return (source, null);
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

		var resourceName = uri.AbsolutePath[1..].Replace("(assembly)", assembly.GetName().Name);
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
