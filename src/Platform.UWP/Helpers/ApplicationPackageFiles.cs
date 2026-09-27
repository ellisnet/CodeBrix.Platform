#nullable enable

using System;
using System.IO;
using CodeBrix.Platform.Contracts;

namespace CodeBrix.Platform.Helpers;

/// <summary>
/// Core's single entry point to the application package's files when a platform registered
/// <see cref="IApplicationPackageFilesPlatform"/> (WPE1-13): the ms-appx URI to package-relative path mapping and the
/// stream access the Core callers use instead of a path under the installed folder.
/// </summary>
/// <remarks>
/// When nothing is registered (the Skia heads) <see cref="Platform"/> is <see langword="null"/> and every caller keeps
/// its file-path behavior exactly.
/// </remarks>
internal static class ApplicationPackageFiles
{
	/// <summary>
	/// Gets the registered package-files platform, or <see langword="null"/> when the package files are plain files
	/// under the installed folder. Looked up on each call: an optional contract that callers use once per file opened.
	/// </summary>
	internal static IApplicationPackageFilesPlatform? Platform
		=> PlatformContract.TryResolve<IApplicationPackageFilesPlatform>();

	/// <summary>
	/// Returns the package-relative path an ms-appx URI names: its host (when it has one) and its path, percent-escapes
	/// decoded, '/'-separated, without a leading '/'.
	/// </summary>
	/// <param name="uri">An absolute ms-appx URI.</param>
	/// <returns>The package-relative path.</returns>
	internal static string GetRelativePath(Uri uri)
	{
		var path = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
		// Uri.Host is lower-cased; a package (an APK's assets) is case-sensitive, so the host is taken as written.
		return uri.Host is { Length: > 0 } ? InstalledPackagePath.GetWrittenHost(uri) + "/" + path : path;
	}

	/// <summary>
	/// Returns the package-relative form of a raw ms-appx path ("/Assets/a.png", "MyLibrary/a.png"): '/'-separated,
	/// without a leading '/'.
	/// </summary>
	/// <param name="rawPath">The path part (and host) of an ms-appx URI.</param>
	/// <returns>The package-relative path.</returns>
	internal static string NormalizeRelativePath(string rawPath)
		=> rawPath.Replace('\\', '/').TrimStart('/');

	/// <summary>
	/// Returns the ms-appx URI string of a package-relative path.
	/// </summary>
	/// <param name="relativePath">The package-relative path.</param>
	/// <returns>"ms-appx:///" followed by the path.</returns>
	internal static string ToUriString(string relativePath)
		=> "ms-appx:///" + relativePath;

	/// <summary>
	/// The package counterpart of the image loaders' .scale-NNN probe: the "name.scale-NNN.ext" variant of
	/// <paramref name="relativePath"/> for the display scale, chosen as the file-path probe chooses it (the largest known
	/// scale not above <paramref name="resolutionScale"/> that exists, else the smallest one above it that exists), or
	/// <paramref name="relativePath"/> itself when there is no variant.
	/// </summary>
	/// <param name="platform">The registered package-files platform.</param>
	/// <param name="relativePath">The package-relative path of the unqualified image.</param>
	/// <param name="resolutionScale">The display's resolution scale in percent.</param>
	/// <param name="knownScales">The known scales, ascending.</param>
	/// <returns>A package-relative path.</returns>
	internal static string FindScaledPath(IApplicationPackageFilesPlatform platform, string relativePath, int resolutionScale, int[] knownScales)
	{
		var slash = relativePath.LastIndexOf('/');
		var directory = slash >= 0 ? relativePath.Substring(0, slash + 1) : string.Empty;
		var fileName = slash >= 0 ? relativePath.Substring(slash + 1) : relativePath;
		var baseName = Path.GetFileNameWithoutExtension(fileName);
		var extension = Path.GetExtension(fileName);

		return Find(onlyMatching: true) ?? Find(onlyMatching: false) ?? relativePath;

		string? Find(bool onlyMatching)
		{
			for (var i = knownScales.Length - 1; i >= 0; i--)
			{
				var probeScale = knownScales[i];
				if ((onlyMatching && resolutionScale >= probeScale) || (!onlyMatching && resolutionScale < probeScale))
				{
					var candidate = $"{directory}{baseName}.scale-{probeScale}{extension}";
					if (platform.FileExists(candidate))
					{
						return candidate;
					}
				}
			}

			return null;
		}
	}

	/// <summary>
	/// Opens a package file through <paramref name="platform"/>.
	/// </summary>
	/// <param name="platform">The registered package-files platform.</param>
	/// <param name="relativePath">The package-relative path.</param>
	/// <returns>The platform's stream.</returns>
	/// <exception cref="FileNotFoundException">The package has no such file.</exception>
	internal static Stream OpenRead(IApplicationPackageFilesPlatform platform, string relativePath)
		=> platform.OpenRead(relativePath)
			?? throw new FileNotFoundException($"The file [{relativePath}] cannot be found in the application package.", relativePath);

	/// <summary>
	/// Opens a package file as a SEEKABLE stream: a stream the platform returns that cannot seek is copied into memory.
	/// </summary>
	/// <param name="platform">The registered package-files platform.</param>
	/// <param name="relativePath">The package-relative path.</param>
	/// <returns>A seekable stream positioned at 0.</returns>
	/// <exception cref="FileNotFoundException">The package has no such file.</exception>
	internal static Stream OpenSeekable(IApplicationPackageFilesPlatform platform, string relativePath)
	{
		var stream = OpenRead(platform, relativePath);
		if (stream.CanSeek)
		{
			return stream;
		}

		using (stream)
		{
			var memory = new MemoryStream();
			stream.CopyTo(memory);
			memory.Position = 0;
			return memory;
		}
	}
}
