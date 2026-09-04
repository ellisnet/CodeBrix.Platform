using System;
using System.IO;
using System.Text;
using CodeBrix.Platform.UI.Svg;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Builds the platform image source behind an SVG icon.
/// </summary>
/// <remarks>
/// Everything SVG-specific happens here and it is all the platform's own route: an
/// <c>SvgImageSource</c>, whose rendering the Svg add-in supplies over CodeBrix.SkiaSvg, told what
/// size to rasterise at and handed the stylesheet that carries the tint. There is no SkiaSharp call
/// site in this add-in.
/// </remarks>
internal static class SvgImageSourceFactory
{
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
		var svg = new SvgImageSource
		{
			RasterizePixelWidth = size,
			RasterizePixelHeight = size,
		};

		//Before the source is given anything to load: the stylesheet is applied at PARSE.
		SvgProvider.SetCss(svg, css);

		if (!string.IsNullOrEmpty(markup))
		{
			SetStream(svg, Encoding.UTF8.GetBytes(markup));
		}
		else if (IconResourceScheme.TryOpen(artwork, out var resource))
		{
			using (resource)
			{
				using var buffer = new MemoryStream();
				resource.CopyTo(buffer);
				SetStream(svg, buffer.ToArray());
			}
		}
		else if (artwork is not null)
		{
			svg.UriSource = artwork;
		}

		return svg;
	}

	private static void SetStream(SvgImageSource svg, byte[] bytes)
	{
		//Deliberately not awaited: the icon appears when the parse finishes, and the element that
		//owns it is already in the tree waiting for the image to open.
		_ = svg.SetSourceAsync(new MemoryStream(bytes).AsRandomAccessStream());
	}
}
