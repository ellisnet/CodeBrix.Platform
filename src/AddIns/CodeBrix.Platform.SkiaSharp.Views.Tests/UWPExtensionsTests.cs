using SilverAssertions;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Foundation;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// The add-in's conversions between the framework's geometry and colour types and SkiaSharp's.
/// Every conversion is exercised in both directions, because the two families disagree about
/// number width (double against float) and, for colour, about channel ORDER.
/// </summary>
public class UWPExtensionsTests
{
	[Fact]
	public void ToSKPoint_narrows_the_coordinates_to_float()
	{
		//Arrange
		var point = new Point(12.5, -7.25);

		//Act
		var converted = point.ToSKPoint();

		//Assert
		converted.X.Should().Be(12.5f);
		converted.Y.Should().Be(-7.25f);
	}

	[Fact]
	public void ToPoint_widens_the_coordinates_back()
	{
		//Arrange
		var point = new SKPoint(12.5f, -7.25f);

		//Act
		var converted = point.ToPoint();

		//Assert
		converted.X.Should().Be(12.5);
		converted.Y.Should().Be(-7.25);
	}

	[Fact]
	public void ToSKPoint_and_ToPoint_round_trip_a_value_a_float_can_hold()
	{
		//Arrange
		var original = new Point(0.5, 128.25);

		//Act
		var roundTripped = original.ToSKPoint().ToPoint();

		//Assert
		roundTripped.Should().Be(original);
	}

	[Fact]
	public void ToSKRect_converts_the_edges_not_the_size()
	{
		//Arrange
		//A framework Rect is X/Y/Width/Height; an SKRect is Left/Top/Right/Bottom. The conversion
		//goes through the edges, so a rect with an offset must not lose it.
		var rect = new Rect(10, 20, 30, 40);

		//Act
		var converted = rect.ToSKRect();

		//Assert
		converted.Left.Should().Be(10f);
		converted.Top.Should().Be(20f);
		converted.Right.Should().Be(40f);
		converted.Bottom.Should().Be(60f);
		converted.Width.Should().Be(30f);
		converted.Height.Should().Be(40f);
	}

	[Fact]
	public void ToRect_passes_the_edges_straight_into_a_rect_that_wants_a_size()
	{
		//Arrange
		//CHARACTERISATION, NOT ENDORSEMENT. ToSKRect converts the framework's offset-and-size Rect
		//into SKRect's four EDGES; ToRect hands those same four edges to Rect's
		//(x, y, WIDTH, HEIGHT) constructor, so the SKRect's Right and Bottom become the Rect's
		//Width and Height. The two conversions are therefore each other's inverse only for a rect
		//anchored at the origin. This is the vendored upstream source, unchanged since it was
		//vendored (git: one commit, the initial one), and the add-in's files are deliberately kept
		//byte-identical with upstream so they can be re-diffed at a SkiaSharp bump - so the
		//behaviour is pinned here rather than corrected there. If upstream ever fixes it, THIS
		//TEST fails at the bump and says exactly what moved.
		var rect = new SKRect(10f, 20f, 40f, 60f);

		//Act
		var converted = rect.ToRect();

		//Assert
		converted.X.Should().Be(10);
		converted.Y.Should().Be(20);
		converted.Width.Should().Be(40);
		converted.Height.Should().Be(60);
	}

	[Fact]
	public void ToSKRect_then_ToRect_round_trips_only_a_rect_at_the_origin()
	{
		//Arrange
		//The consequence of the asymmetry above, stated so it cannot be discovered by surprise.
		var atOrigin = new Rect(0, 0, 3.75, 4.5);
		var offset = new Rect(1.5, 2.25, 3.75, 4.5);

		//Act
		var atOriginRoundTripped = atOrigin.ToSKRect().ToRect();
		var offsetRoundTripped = offset.ToSKRect().ToRect();

		//Assert
		atOriginRoundTripped.Should().Be(atOrigin);
		offsetRoundTripped.Should().NotBe(offset);
		//The offset one comes back with the edges as its size.
		offsetRoundTripped.Width.Should().Be(5.25);
		offsetRoundTripped.Height.Should().Be(6.75);
	}

	[Fact]
	public void ToSKSize_narrows_the_dimensions_to_float()
	{
		//Arrange
		var size = new Size(200.5, 100.25);

		//Act
		var converted = size.ToSKSize();

		//Assert
		converted.Width.Should().Be(200.5f);
		converted.Height.Should().Be(100.25f);
	}

	[Fact]
	public void ToSize_widens_the_dimensions_back()
	{
		//Arrange
		var size = new SKSize(200.5f, 100.25f);

		//Act
		var converted = size.ToSize();

		//Assert
		converted.Width.Should().Be(200.5);
		converted.Height.Should().Be(100.25);
	}

	[Fact]
	public void ToSKSize_and_ToSize_round_trip_a_zero_size()
	{
		//Arrange
		var original = new Size(0, 0);

		//Act
		var roundTripped = original.ToSKSize().ToSize();

		//Assert
		roundTripped.Width.Should().Be(0);
		roundTripped.Height.Should().Be(0);
	}

	[Fact]
	public void ToSKColor_reorders_the_channels_from_ARGB_to_RGBA()
	{
		//Arrange
		//This is the one conversion where a mistake is invisible in grey and obvious in colour:
		//the framework's Color.FromArgb takes A first, SKColor's constructor takes A LAST.
		var color = Color.FromArgb(0x11, 0x22, 0x44, 0x88);

		//Act
		var converted = color.ToSKColor();

		//Assert
		converted.Alpha.Should().Be(0x11);
		converted.Red.Should().Be(0x22);
		converted.Green.Should().Be(0x44);
		converted.Blue.Should().Be(0x88);
	}

	[Fact]
	public void ToColor_reorders_the_channels_back()
	{
		//Arrange
		var color = new SKColor(0x22, 0x44, 0x88, 0x11);

		//Act
		var converted = color.ToColor();

		//Assert
		converted.A.Should().Be(0x11);
		converted.R.Should().Be(0x22);
		converted.G.Should().Be(0x44);
		converted.B.Should().Be(0x88);
	}

	[Theory]
	[InlineData(0x00, 0x00, 0x00, 0x00)]
	[InlineData(0xFF, 0xFF, 0xFF, 0xFF)]
	[InlineData(0xFF, 0xFF, 0x00, 0x00)]
	[InlineData(0x80, 0x01, 0x02, 0x03)]
	public void ToSKColor_and_ToColor_round_trip_every_channel(byte a, byte r, byte g, byte b)
	{
		//Arrange
		var original = Color.FromArgb(a, r, g, b);

		//Act
		var roundTripped = original.ToSKColor().ToColor();

		//Assert
		roundTripped.Should().Be(original);
	}

	[Fact]
	public void a_transparent_colour_stays_transparent_in_both_directions()
	{
		//Arrange
		//SKColor stores its channels unpremultiplied, so a fully transparent colour must keep its
		//RGB rather than collapsing to zero.
		var original = Color.FromArgb(0x00, 0x10, 0x20, 0x30);

		//Act
		var converted = original.ToSKColor();
		var roundTripped = converted.ToColor();

		//Assert
		converted.Alpha.Should().Be(0x00);
		converted.Red.Should().Be(0x10);
		roundTripped.Should().Be(original);
	}
}
