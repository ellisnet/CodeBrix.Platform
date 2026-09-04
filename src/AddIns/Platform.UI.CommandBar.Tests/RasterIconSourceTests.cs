using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using SkiaSharp;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The bitmap icon: which artwork the theme chooses, how a tint is wired, that the formats beyond
/// PNG really do decode, and how a scale-qualified file beside the named one is picked up.
/// </summary>
public class RasterIconSourceTests
{
	private static readonly Color Red = Color.FromArgb(0xFF, 0xFF, 0x00, 0x00);

	[Fact]
	public void CreateIconElement_makes_a_RasterIcon_carrying_the_sources_values()
	{
		//Arrange
		var light = IconFixtures.FileUri(IconFixtures.AlphaPng);
		var dark = IconFixtures.FileUri(IconFixtures.Bmp);
		var source = new RasterIconSource { Source = light, Dark = dark, Size = 20d };

		//Act
		var element = source.CreateIconElement();

		//Assert
		var icon = element.Should().BeOfType<RasterIcon>().Subject;
		icon.UriSource.Should().Be(light);
		icon.DarkUriSource.Should().Be(dark);
		icon.Size.Should().Be(20d);
	}

	[Fact]
	public void Dark_replaces_the_artwork_in_the_dark_theme()
	{
		//Arrange
		var light = IconFixtures.FileUri(IconFixtures.AlphaPng);
		var dark = IconFixtures.FileUri(IconFixtures.Bmp);
		var icon = new RasterIcon { UriSource = light, DarkUriSource = dark };

		//Act
		icon.RequestedTheme = ElementTheme.Dark;

		//Assert
		icon.ResolvedUriSource.Should().Be(dark);
	}

	[Fact]
	public void a_tint_turns_the_image_into_a_mask_painted_with_that_colour()
	{
		//Arrange
		var icon = new RasterIcon { UriSource = IconFixtures.FileUri(IconFixtures.AlphaPng) };

		//Act
		icon.Tint = new SolidColorBrush(Red);

		//Assert
		//The platform's own tint: ShowAsMonochrome keeps the image's ALPHA and paints it with the
		//foreground, which is why a tinted icon needs artwork with transparency.
		var source = icon.IconSource.Should().BeOfType<BitmapIconSource>().Subject;
		source.ShowAsMonochrome.Should().BeTrue();
		((SolidColorBrush)source.Foreground).Color.Should().Be(Red);
	}

	[Fact]
	public void without_a_tint_the_image_is_drawn_as_it_is()
	{
		//Arrange
		//Act
		var icon = new RasterIcon { UriSource = IconFixtures.FileUri(IconFixtures.AlphaPng) };

		//Assert
		//A multi-colour icon must survive untouched; ShowAsMonochrome defaults to TRUE on the
		//framework's own source, so leaving it alone would silently flatten every raster icon.
		var source = icon.IconSource.Should().BeOfType<BitmapIconSource>().Subject;
		source.ShowAsMonochrome.Should().BeFalse();
	}

	[Fact]
	public void an_embedded_resource_is_resolved_to_a_file_the_decoder_can_read()
	{
		//Arrange
		var icon = new RasterIcon();

		//Act
		icon.UriSource = IconFixtures.ResourceUri(IconFixtures.AlphaPng);

		//Assert
		//A library's icons live inside its assembly, where the platform's file-based decoder cannot
		//reach them, so they are written out once and read like any other file.
		icon.ResolvedUriSource.Should().NotBeNull();
		icon.ResolvedUriSource!.IsFile.Should().BeTrue();
		File.ReadAllBytes(icon.ResolvedUriSource.LocalPath)
			.Should().Equal(IconFixtures.Bytes(IconFixtures.AlphaPng));
	}

	[Fact]
	public void an_unknown_embedded_resource_leaves_the_icon_empty_rather_than_throwing()
	{
		//Arrange
		var icon = new RasterIcon();

		//Act
		icon.UriSource = new Uri($"{IconResourceScheme.Scheme}://NoSuchAssembly/nothing.png");

		//Assert
		icon.ResolvedUriSource.Should().BeNull();
	}

	[Fact]
	public void the_png_fixture_decodes_with_the_alpha_the_tint_uses_as_its_mask()
	{
		//Arrange
		//Act
		using var bitmap = SKBitmap.Decode(IconFixtures.Bytes(IconFixtures.AlphaPng));

		//Assert
		//Decoded by SkiaSharp's codec, which IS the platform's image decoder on every Skia head.
		bitmap.Should().NotBeNull();
		bitmap.Width.Should().Be(8);
		bitmap.GetPixel(1, 4).Should().Be(new SKColor(0x00, 0xAA, 0x00, 0xFF));
		bitmap.GetPixel(6, 4).Alpha.Should().Be(0);
	}

	[Fact]
	public void the_jpeg_fixture_decodes_without_a_line_of_jpeg_specific_code()
	{
		//Arrange
		//Act
		using var bitmap = SKBitmap.Decode(IconFixtures.Bytes(IconFixtures.Jpeg));

		//Assert
		//PNG and SVG are the two required formats; JPEG, BMP, GIF, WebP and ICO come free because
		//the decoding is the platform's. JPEG is lossy, hence the tolerance.
		bitmap.Should().NotBeNull();
		var pixel = bitmap.GetPixel(4, 4);
		((int)pixel.Blue).Should().BeGreaterThan(0xF0);
		((int)pixel.Red).Should().BeLessThan(0x10);
		((int)pixel.Alpha).Should().Be(0xFF);
	}

	[Fact]
	public void the_bmp_fixture_decodes_too()
	{
		//Arrange
		//Act
		using var bitmap = SKBitmap.Decode(IconFixtures.Bytes(IconFixtures.Bmp));

		//Assert
		bitmap.Should().NotBeNull();
		bitmap.GetPixel(4, 4).Should().Be(new SKColor(0x00, 0xAA, 0x00, 0xFF));
	}

	[Fact]
	public void a_scale_qualified_file_beside_the_named_one_is_preferred_when_it_fits_the_display()
	{
		//Arrange
		var directory = Path.Combine(
			Path.GetTempPath(),
			"CodeBrix.CommandBar.IconTests." + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		var bytes = IconFixtures.Bytes(IconFixtures.AlphaPng);
		File.WriteAllBytes(Path.Combine(directory, "open.png"), bytes);
		File.WriteAllBytes(Path.Combine(directory, "open.scale-200.png"), bytes);
		var source = new Uri(Path.Combine(directory, "open.png"));

		try
		{
			//Act
			var atOne = IconAssetLocator.ResolveScaleVariant(source, 1.0);
			var atTwo = IconAssetLocator.ResolveScaleVariant(source, 2.0);
			var atOnePointTwentyFive = IconAssetLocator.ResolveScaleVariant(source, 1.25);

			//Assert
			//The file named without a qualifier IS the 100% artwork, so it wins at 100%. Above that
			//the smallest variant big enough wins - upscaling a bitmap is what looks wrong.
			atOne.Should().Be(source);
			atTwo.LocalPath.Should().EndWith("open.scale-200.png");
			atOnePointTwentyFive.LocalPath.Should().EndWith("open.scale-200.png");
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void a_file_with_no_scale_variants_is_used_exactly_as_it_was_given()
	{
		//Arrange
		var source = IconFixtures.FileUri(IconFixtures.AlphaPng);

		//Act
		var resolved = IconAssetLocator.ResolveScaleVariant(source, 2.0);

		//Assert
		resolved.Should().Be(source);
	}
}
