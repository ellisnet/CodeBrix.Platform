using System.Threading.Tasks;
using CodeBrix.Platform.UI.Svg;
using Microsoft.UI.Xaml.Media.Imaging;
using SilverAssertions;
using SkiaSharp;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The tinting plumbing, measured on the platform's own SVG route rather than on a stand-in.
/// </summary>
/// <remarks>
/// <para>
/// This is the fence for the change the ICONS stream made to the Svg add-in's <c>SvgProvider</c>:
/// a stylesheet can now be attached to an <c>SvgImageSource</c> and is handed to CodeBrix.SkiaSvg
/// as <c>SvgParameters.Css</c> when the document is parsed. Every test here drives the real
/// provider - the same class an application head uses - and reads the picture it produced.
/// </para>
/// <para>
/// It is also the fence for the rasterised size: the provider rasterises at the source's
/// <c>RasterizePixelWidth/Height</c> - LOGICAL pixels - multiplied by the display scale, which is
/// what makes an icon pixel-exact on a 125% display instead of stretched.
/// </para>
/// </remarks>
[Collection(DisplayScaleCollection.Name)]
public class SvgProviderCssTests
{
	private static readonly Color Blue = Color.FromArgb(0xFF, 0x22, 0x66, 0xDD);

	[Fact]
	public void SetCss_and_GetCss_round_trip()
	{
		//Arrange
		var image = new SvgImageSource();

		//Act
		SvgProvider.SetCss(image, "* { color: #FF0000; }");

		//Assert
		SvgProvider.GetCss(image).Should().Be("* { color: #FF0000; }");
	}

	[Fact]
	public void SetCss_with_null_removes_the_stylesheet()
	{
		//Arrange
		var image = new SvgImageSource();
		SvgProvider.SetCss(image, "* { color: #FF0000; }");

		//Act
		SvgProvider.SetCss(image, null);

		//Assert
		SvgProvider.GetCss(image).Should().BeNull();
	}

	[Fact]
	public async Task a_currentColor_svg_is_parsed_black_with_no_stylesheet()
	{
		//Arrange
		var image = new SvgImageSource();
		var provider = new SvgProvider(image);

		//Act
		var loaded = await provider.TryLoadSvgDataAsync(IconFixtures.Bytes(IconFixtures.CurrentColorSvg));

		//Assert
		//The SVG default for "color" is black, so an untinted themed icon is a black icon.
		loaded.Should().BeTrue();
		PixelAtCentre(provider).Should().Be(new SKColor(0x00, 0x00, 0x00, 0xFF));
	}

	[Fact]
	public async Task a_currentColor_svg_renders_in_the_tint()
	{
		//Arrange
		var image = new SvgImageSource();
		SvgProvider.SetCss(image, SvgTintCssFor(IconTintMode.CurrentColorOnly));
		var provider = new SvgProvider(image);

		//Act
		var loaded = await provider.TryLoadSvgDataAsync(IconFixtures.Bytes(IconFixtures.CurrentColorSvg));

		//Assert
		//The whole point of the icon design: the file is untouched on disk and comes out in the
		//application's colour, because the stylesheet reached the parser.
		loaded.Should().BeTrue();
		PixelAtCentre(provider).Should().Be(new SKColor(0x22, 0x66, 0xDD, 0xFF));
	}

	[Fact]
	public async Task the_default_tint_mode_leaves_a_hard_coded_black_icon_black()
	{
		//Arrange
		var image = new SvgImageSource();
		SvgProvider.SetCss(image, SvgTintCssFor(IconTintMode.CurrentColorOnly));
		var provider = new SvgProvider(image);

		//Act
		await provider.TryLoadSvgDataAsync(IconFixtures.Bytes(IconFixtures.MonochromeSvg));

		//Assert
		//CurrentColorOnly is a promise not to touch a colour the artwork states outright - which is
		//what makes it safe to switch on for a whole bar of mixed icons.
		PixelAtCentre(provider).Should().Be(new SKColor(0x00, 0x00, 0x00, 0xFF));
	}

	[Fact]
	public async Task the_replace_mode_recolours_a_hard_coded_black_icon()
	{
		//Arrange
		var image = new SvgImageSource();
		SvgProvider.SetCss(image, SvgTintCssFor(IconTintMode.ReplaceBlackAndWhite));
		var provider = new SvgProvider(image);

		//Act
		await provider.TryLoadSvgDataAsync(IconFixtures.Bytes(IconFixtures.MonochromeSvg));

		//Assert
		PixelAtCentre(provider).Should().Be(new SKColor(0x22, 0x66, 0xDD, 0xFF));
	}

	[Fact]
	public async Task the_rasterised_bitmap_is_the_requested_size_at_100_percent()
	{
		//Arrange
		var image = new SvgImageSource { RasterizePixelWidth = 24, RasterizePixelHeight = 24 };
		var provider = new SvgProvider(image);

		//Act
		await DisplayScale.AtAsync(1.0, () => provider.TryLoadSvgDataAsync(IconFixtures.Bytes(IconFixtures.CurrentColorSvg)));

		//Assert
		provider.RasterizedPixelSize.Width.Should().Be(24d);
		provider.RasterizedPixelSize.Height.Should().Be(24d);
	}

	[Fact]
	public async Task the_rasterised_bitmap_doubles_on_a_200_percent_display()
	{
		//Arrange
		var image = new SvgImageSource { RasterizePixelWidth = 24, RasterizePixelHeight = 24 };
		var provider = new SvgProvider(image);

		//Act
		await DisplayScale.AtAsync(2.0, () => provider.TryLoadSvgDataAsync(IconFixtures.Bytes(IconFixtures.CurrentColorSvg)));

		//Assert
		//RasterizePixelWidth/Height are LOGICAL pixels; the platform multiplies by the display
		//scale, so a 24-pixel icon really is 48 device pixels on a 200% display.
		provider.RasterizedPixelSize.Width.Should().Be(48d);
		provider.RasterizedPixelSize.Height.Should().Be(48d);
	}

	[Fact]
	public async Task a_non_square_rasterisation_is_not_transposed()
	{
		//Arrange
		var image = new SvgImageSource { RasterizePixelWidth = 32, RasterizePixelHeight = 16 };
		var provider = new SvgProvider(image);

		//Act
		await DisplayScale.AtAsync(1.0, () => provider.TryLoadSvgDataAsync(IconFixtures.Bytes(IconFixtures.CurrentColorSvg)));

		//Assert
		//The fence for a defect found while building this stream: the provider was computing its
		//bitmap's width from RasterizePixelHeight and its height from RasterizePixelWidth. Square
		//icons hid it; a 32x16 rasterisation came out 16x32.
		provider.RasterizedPixelSize.Width.Should().Be(32d);
		provider.RasterizedPixelSize.Height.Should().Be(16d);
	}

	private static string SvgTintCssFor(IconTintMode mode)
		=> new SvgIcon
		{
			Tint = new Microsoft.UI.Xaml.Media.SolidColorBrush(Blue),
			TintMode = mode,
		}.TintCss!;

	private static SKColor PixelAtCentre(SvgProvider provider)
	{
		var picture = (SKPicture)provider.TryGetLoadedDataAsPictureAsync()!;
		using var bitmap = new SKBitmap(24, 24);
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(SKColors.Transparent);
		canvas.DrawPicture(picture);
		return bitmap.GetPixel(12, 12);
	}
}
