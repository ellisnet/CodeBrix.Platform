using System;
using CodeBrix.Platform.UI.Runtime.Skia;
using SilverAssertions;
using Windows.Foundation;
using Windows.Graphics;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// Fences the one rule the Skia heads share for window sizing: the numbers an application supplies
/// are effective pixels, and a head that speaks raw device pixels multiplies by the display scale.
/// </summary>
public class WindowSizeConversionTests
{
	[Fact]
	public void scale_of_one_leaves_a_launch_size_alone()
	{
		//Arrange
		var logical = new Size(1024, 640);

		//Act
		var native = WindowSizeConversion.LogicalToNative(logical, 1.0);

		//Assert
		native.Width.Should().Be(1024);
		native.Height.Should().Be(640);
	}

	[Theory]
	[InlineData(1.0, 1024, 640)]
	[InlineData(1.5, 1536, 960)]
	[InlineData(2.0, 2048, 1280)]
	public void a_launch_size_is_multiplied_by_the_display_scale(double scale, double expectedWidth, double expectedHeight)
	{
		//Arrange
		var logical = new Size(1024, 640);

		//Act
		var native = WindowSizeConversion.LogicalToNative(logical, scale);

		//Assert
		native.Width.Should().Be(expectedWidth);
		native.Height.Should().Be(expectedHeight);
	}

	[Fact]
	public void a_launch_size_that_does_not_divide_evenly_rounds_to_whole_pixels()
	{
		//Arrange
		var logical = new Size(1205, 641);

		//Act
		var native = WindowSizeConversion.LogicalToNative(logical, 1.5);

		//Assert
		native.Width.Should().Be(1808);   // 1807.5 rounds away from zero
		native.Height.Should().Be(962);   // 961.5 rounds away from zero
	}

	[Fact]
	public void a_positive_launch_size_never_rounds_down_to_nothing()
	{
		//Arrange
		var logical = new Size(1, 1);

		//Act
		var native = WindowSizeConversion.LogicalToNative(logical, 0.25);

		//Assert
		native.Width.Should().Be(1);
		native.Height.Should().Be(1);
	}

	[Fact]
	public void an_empty_launch_size_is_left_alone()
	{
		//Act
		var native = WindowSizeConversion.LogicalToNative(Size.Empty, 2.0);

		//Assert
		native.IsEmpty.Should().BeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	public void an_unusable_scale_falls_back_to_one(double scale)
	{
		//Act
		var normalized = WindowSizeConversion.NormalizeScale(scale);

		//Assert
		normalized.Should().Be(WindowSizeConversion.FallbackScale);
	}

	[Theory]
	[InlineData(1.0, 400, 400)]
	[InlineData(1.5, 400, 600)]
	[InlineData(2.0, 400, 800)]
	public void a_presenter_minimum_is_multiplied_by_the_display_scale(double scale, int logical, int expected)
	{
		//Act
		var native = WindowSizeConversion.LogicalToNative(logical, scale);

		//Assert
		native.Should().Be(expected);
	}

	[Fact]
	public void the_unconstrained_minimum_stays_zero_at_every_scale()
	{
		//Act
		var native = WindowSizeConversion.LogicalToNative(0, 2.0);

		//Assert
		native.Should().Be(0);
	}

	[Fact]
	public void the_unconstrained_maximum_saturates_instead_of_overflowing()
	{
		//Act
		var native = WindowSizeConversion.LogicalToNative(int.MaxValue, 1.5);

		//Assert
		native.Should().Be(int.MaxValue);
	}

	[Fact]
	public void an_unset_constraint_stays_unset()
	{
		//Act
		var native = WindowSizeConversion.LogicalToNative((int?)null, 2.0);

		//Assert
		native.Should().BeNull();
	}

	[Fact]
	public void a_set_constraint_is_converted_through_the_nullable_overload()
	{
		//Act
		var native = WindowSizeConversion.LogicalToNative((int?)400, 1.5);

		//Assert
		native.Should().Be(600);
	}

	[Theory]
	[InlineData(1.0, 1024, 640)]
	[InlineData(1.5, 1536, 960)]
	[InlineData(2.0, 2048, 1280)]
	public void a_framed_size_is_multiplied_by_the_display_scale(double scale, int expectedWidth, int expectedHeight)
	{
		//Arrange
		var logical = new SizeInt32 { Width = 1024, Height = 640 };

		//Act
		var native = WindowSizeConversion.LogicalToNative(logical, scale);

		//Assert
		native.Width.Should().Be(expectedWidth);
		native.Height.Should().Be(expectedHeight);
	}
}
