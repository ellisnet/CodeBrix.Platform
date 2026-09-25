using System;
using System.IO;
using CodeBrix.Platform.UI.CommandBar.Contracts;
using CodeBrix.Platform.UI.Svg;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CodeBrix.Platform.UI.CommandBar.Skia;

/// <summary>
/// The Skia implementation of <see cref="IIconRasterizationPlatform"/>: an <see cref="SvgImageSource"/>, whose
/// rendering the Svg add-in supplies over CodeBrix.SkiaSvg, sized for rasterisation and given the tint stylesheet
/// through <see cref="SvgProvider.SetCss"/> before it loads anything.
/// </summary>
internal sealed class IconRasterizationSkiaPlatform : IIconRasterizationPlatform
{
	/// <inheritdoc />
	public SvgImageSource CreateSvgImageSource(Uri? artwork, byte[]? document, double size, string? css)
	{
		var svg = new SvgImageSource
		{
			RasterizePixelWidth = size,
			RasterizePixelHeight = size,
		};

		//Before the source is given anything to load: the stylesheet is applied at PARSE.
		SvgProvider.SetCss(svg, css);

		if (document is not null)
		{
			SetStream(svg, document);
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
