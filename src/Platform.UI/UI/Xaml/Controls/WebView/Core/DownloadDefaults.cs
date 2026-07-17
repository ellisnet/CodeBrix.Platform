#nullable enable

using System;
using System.IO;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Computes the default location for files downloaded by a WebView when the app does not
/// override it through <see cref="CoreWebView2.DownloadStarting"/>.
/// </summary>
internal static class DownloadDefaults
{
	/// <summary>
	/// Returns the user's Downloads folder: the XDG download directory on Linux (per
	/// ~/.config/user-dirs.dirs) and ~/Downloads elsewhere (also the Linux fallback).
	/// The folder is created when it does not exist yet.
	/// </summary>
	internal static string GetDownloadsFolder()
	{
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var folder = OperatingSystem.IsLinux() ? TryGetXdgDownloadDirectory(home) : null;
		folder ??= Path.Combine(home, "Downloads");
		Directory.CreateDirectory(folder);
		return folder;
	}

	/// <summary>
	/// Combines <paramref name="folder"/> and <paramref name="suggestedFileName"/> into a path
	/// that does not collide with an existing file, appending " (1)", " (2)", ... to the file
	/// name when needed - the auto-rename scheme WebView2 uses on Windows.
	/// </summary>
	internal static string GetCollisionFreePath(string folder, string suggestedFileName)
	{
		var fileName = MakeSafeFileName(suggestedFileName);
		var candidate = Path.Combine(folder, fileName);
		if (!File.Exists(candidate))
		{
			return candidate;
		}

		var baseName = Path.GetFileNameWithoutExtension(fileName);
		var extension = Path.GetExtension(fileName);
		for (var counter = 1; ; counter++)
		{
			candidate = Path.Combine(folder, $"{baseName} ({counter}){extension}");
			if (!File.Exists(candidate))
			{
				return candidate;
			}
		}
	}

	private static string MakeSafeFileName(string suggestedFileName)
	{
		if (string.IsNullOrWhiteSpace(suggestedFileName))
		{
			return "download";
		}

		// Keep only the file name portion (a hostile server could suggest path separators).
		var fileName = Path.GetFileName(suggestedFileName.Trim());
		foreach (var invalid in Path.GetInvalidFileNameChars())
		{
			fileName = fileName.Replace(invalid, '_');
		}
		return fileName.Length == 0 ? "download" : fileName;
	}

	private static string? TryGetXdgDownloadDirectory(string home)
	{
		try
		{
			var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
			if (string.IsNullOrEmpty(configHome))
			{
				configHome = Path.Combine(home, ".config");
			}

			var userDirsFile = Path.Combine(configHome, "user-dirs.dirs");
			if (!File.Exists(userDirsFile))
			{
				return null;
			}

			foreach (var line in File.ReadAllLines(userDirsFile))
			{
				var trimmed = line.Trim();
				if (!trimmed.StartsWith("XDG_DOWNLOAD_DIR=", StringComparison.Ordinal))
				{
					continue;
				}

				var value = trimmed["XDG_DOWNLOAD_DIR=".Length..].Trim().Trim('"');
				if (value.StartsWith("$HOME/", StringComparison.Ordinal))
				{
					value = Path.Combine(home, value["$HOME/".Length..]);
				}
				else if (value == "$HOME" || !value.StartsWith('/'))
				{
					// "$HOME" alone means the download directory is disabled; relative and
					// other unexpanded forms are not valid per the xdg-user-dirs spec.
					return null;
				}
				return value;
			}
		}
		catch (Exception)
		{
			// Fall through to the ~/Downloads fallback on any parsing/IO problem.
		}
		return null;
	}
}
