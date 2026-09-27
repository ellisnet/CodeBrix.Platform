#nullable enable

using System;
using System.IO;

namespace CodeBrix.Platform.Helpers;

/// <summary>
/// Chooses the casing of an ms-appx URI's host (a library's folder, as in <c>ms-appx://MyLibrary/Assets/a.png</c>) when
/// the URI is mapped to a file under the application's installed folder (WPE1-14).
/// </summary>
/// <remarks>
/// <see cref="Uri.Host"/> is lower-cased, so combining it into a path looked in "mylibrary/" and missed a "MyLibrary"
/// folder on a case-sensitive file system. The host AS WRITTEN is tried first; when that path does not exist, the
/// lower-cased path of before is used, so nothing that resolved before resolves differently unless the written-case
/// path exists.
/// </remarks>
internal static class InstalledPackagePath
{
	/// <summary>
	/// Returns the host of <paramref name="uri"/> as it is written in <see cref="Uri.OriginalString"/> (same name as
	/// <see cref="Uri.Host"/>, original casing), or <see cref="Uri.Host"/> when the written form cannot be read.
	/// </summary>
	/// <param name="uri">An absolute URI.</param>
	/// <returns>The host with its written casing; empty when the URI has no host.</returns>
	internal static string GetWrittenHost(Uri uri)
	{
		var host = uri.Host;
		if (host.Length == 0)
		{
			return host;
		}

		var text = uri.OriginalString;
		var start = text.IndexOf("://", StringComparison.Ordinal);
		if (start < 0)
		{
			return host;
		}

		start += 3;
		var end = text.IndexOfAny(['/', '?', '#', '\\'], start);
		var written = end < 0 ? text.Substring(start) : text.Substring(start, end - start);
		return string.Equals(written, host, StringComparison.OrdinalIgnoreCase) ? written : host;
	}

	/// <summary>
	/// Returns the host folder name to combine into a path under <paramref name="basePath"/> for <paramref name="uri"/>:
	/// the written casing when the file or folder it names exists (or, for a path that exists under neither casing, when
	/// only the written-case host folder exists), else <see cref="Uri.Host"/> (lower-cased), as before.
	/// </summary>
	/// <param name="uri">An absolute ms-appx URI.</param>
	/// <param name="basePath">The folder the host folder lives in (the installed folder).</param>
	/// <param name="relativePath">The URI's path after the host, as the caller combines it (leading '/' allowed).</param>
	/// <returns>The host folder name; empty when the URI has no host.</returns>
	internal static string ResolveHost(Uri uri, string basePath, string relativePath)
		=> ChooseHost(basePath, GetWrittenHost(uri), uri.Host, relativePath);

	/// <summary>
	/// The rule behind <see cref="ResolveHost"/>, with the two casings given (unit-testable without a URI).
	/// </summary>
	/// <param name="basePath">The folder the host folder lives in.</param>
	/// <param name="writtenHost">The host as written.</param>
	/// <param name="lowerHost">The host as <see cref="Uri.Host"/> reports it.</param>
	/// <param name="relativePath">The path after the host (leading '/' allowed).</param>
	/// <returns>The host folder name to use.</returns>
	internal static string ChooseHost(string basePath, string writtenHost, string lowerHost, string relativePath)
	{
		if (string.Equals(writtenHost, lowerHost, StringComparison.Ordinal) || string.IsNullOrEmpty(basePath))
		{
			return lowerHost;
		}

		var rest = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
		if (Exists(Path.Combine(basePath, writtenHost, rest)))
		{
			return writtenHost;
		}

		if (Exists(Path.Combine(basePath, lowerHost, rest)))
		{
			return lowerHost;
		}

		// Neither file exists (a caller that probes variants such as name.scale-200.png next to it): pick the
		// library folder that exists, the written one when only it does.
		return Directory.Exists(Path.Combine(basePath, writtenHost)) && !Directory.Exists(Path.Combine(basePath, lowerHost))
			? writtenHost
			: lowerHost;
	}

	private static bool Exists(string path)
		=> File.Exists(path) || Directory.Exists(path);
}
