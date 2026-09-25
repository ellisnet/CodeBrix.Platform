using System;
using System.IO;
using System.Text;
using CodeBrix.Platform.UI.CommandBar.Contracts;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Builds the platform image source behind an SVG icon.
/// </summary>
/// <remarks>
/// This side finds the artwork - an SVG document written inline, an embedded resource behind a cb-res:// URI, or any
/// other URI the image source loads itself. Rasterising it is the platform's: an <c>SvgImageSource</c> told what size
/// to rasterise at and handed the stylesheet that carries the tint, created by the platform's
/// <see cref="IIconRasterizationPlatform"/> (on this platform, the Svg add-in over CodeBrix.SkiaSvg). There is no
/// SkiaSharp call site in this add-in's Core assembly.
/// </remarks>
internal static class SvgImageSourceFactory
{
	private static IIconRasterizationPlatform? _platform;

	private static IIconRasterizationPlatform Platform => _platform ??= PlatformContract.Resolve<IIconRasterizationPlatform>();

	/// <summary>
	/// Creates and starts loading one SVG image source.
	/// </summary>
	/// <param name="artwork">The artwork's URI, ignored when <paramref name="markup"/> is given.</param>
	/// <param name="markup">An SVG document written inline, or null.</param>
	/// <param name="size">The icon's edge length in LOGICAL pixels. The platform multiplies it by
	/// the display scale to get the bitmap it rasterises.</param>
	/// <param name="css">The stylesheet carrying the tint, or null.</param>
	/// <returns>The image source, which loads in the background.</returns>
	internal static SvgImageSource Create(Uri? artwork, string? markup, double size, string? css)
	{
		byte[]? document = null;

		if (!string.IsNullOrEmpty(markup))
		{
			document = Encoding.UTF8.GetBytes(markup);
		}
		else if (IconResourceScheme.TryOpen(artwork, out var resource))
		{
			using (resource)
			{
				using var buffer = new MemoryStream();
				resource.CopyTo(buffer);
				document = buffer.ToArray();
			}
		}

		return Platform.CreateSvgImageSource(document is null ? artwork : null, document, size, css);
	}
}
