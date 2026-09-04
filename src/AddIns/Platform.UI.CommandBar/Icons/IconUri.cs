using System;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Reads the URI an icon was written with in XAML.
/// </summary>
/// <remarks>
/// A markup extension receives its arguments as strings, and the shortest thing an application
/// wants to write is a path - <c>Assets/open.svg</c> - not a full absolute URI. A relative path is
/// therefore read as <c>ms-appx:///</c>, which is where an application's own assets live, and an
/// absolute URI is taken exactly as written.
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

		if (Uri.TryCreate(text, UriKind.Absolute, out var absolute))
		{
			return absolute;
		}

		return new Uri(PackagePrefix + text.TrimStart('/'), UriKind.Absolute);
	}
}
