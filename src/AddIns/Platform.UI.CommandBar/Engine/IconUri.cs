using System;

namespace CodeBrix.Platform.UI.CommandBar.Engine;

/// <summary>
/// Reads the URI an icon was written with in XAML (WPE1 C14: moved into the Engine namespace; names no XAML type).
/// </summary>
/// <remarks>
/// A markup extension receives its arguments as strings, and the shortest thing an application
/// wants to write is a path - <c>Assets/open.svg</c> - not a full absolute URI. A relative path is
/// therefore read as <c>ms-appx:///</c>, which is where an application's own assets live, and an
/// absolute URI is taken exactly as written.
/// <para>
/// A path with ONE leading slash - <c>/Assets/open.svg</c> - is rooted at the application package too, on
/// every operating system (decision D1, WPE1-11). It is checked before <see cref="Uri.TryCreate(string, UriKind, out Uri)"/>
/// because on Linux and macOS that call reads such a path as <c>file:///Assets/open.svg</c> (a rooted file-system
/// path) while on Windows it is not an absolute URI at all; an icon string written that way means a package asset.
/// Two leading slashes (<c>//server/share/icon.svg</c>, a UNC path) are still taken as written.
/// </para>
/// </remarks>
internal static class IconUri
{
	/// <summary>The application package's own scheme, used for a relative path.</summary>
	private const string PackagePrefix = "ms-appx:///";

	/// <summary>
	/// Turns the text of an icon URI into a URI.
	/// </summary>
	/// <param name="value">Text from XAML; null or blank gives null.</param>
	/// <returns>The URI, or null.</returns>
	internal static Uri? Parse(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var text = value.Trim();

		if (text.Length > 1 && text[0] == '/' && text[1] != '/')
		{
			return new Uri(string.Concat(PackagePrefix, text.AsSpan(1)), UriKind.Absolute);
		}

		if (Uri.TryCreate(text, UriKind.Absolute, out var absolute))
		{
			return absolute;
		}

		return new Uri(PackagePrefix + text.TrimStart('/'), UriKind.Absolute);
	}
}
