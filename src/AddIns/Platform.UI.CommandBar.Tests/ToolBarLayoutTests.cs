using CodeBrix.Platform.UI.CommandBar.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The overflow partition and the pixel snapping, tested as the arithmetic they are.
/// </summary>
/// <remarks>
/// A bar's own tests exercise the same code through a real control; these pin the numbers at
/// widths a window would have to be dragged to, which is quicker to state and quicker to read when
/// one of them changes.
/// </remarks>
public class ToolBarLayoutTests
{
	[Fact]
	public void ComputeVisibleCount_keeps_every_item_when_they_all_fit()
	{
		//Arrange
		var extents = new[] { 50d, 50d, 50d };

		//Act
		var count = ToolBarLayout.ComputeVisibleCount(extents, 4, 200, 28, out var hasOverflow);

		//Assert
		count.Should().Be(3);
		hasOverflow.Should().BeFalse();
	}

	[Fact]
	public void ComputeVisibleCount_keeps_every_item_when_the_width_is_unbounded()
	{
		//Arrange
		var extents = new[] { 50d, 50d, 50d };

		//Act
		var count = ToolBarLayout.ComputeVisibleCount(
			extents, 4, double.PositiveInfinity, 28, out var hasOverflow);

		//Assert
		count.Should().Be(3);
		hasOverflow.Should().BeFalse();
	}

	[Theory]
	//Five 50-pixel items, 4 between them, a 28-pixel chevron: 266 wide in total. The usable width
	//once the chevron and its own gap are reserved is available - 32, and each further item costs
	//54. These are the widths a window is dragged through, one item at a time.
	[InlineData(400, 5, false)]
	[InlineData(266, 5, false)]
	[InlineData(265, 4, true)]
	[InlineData(244, 4, true)]
	[InlineData(243, 3, true)]
	[InlineData(190, 3, true)]
	[InlineData(189, 2, true)]
	[InlineData(136, 2, true)]
	[InlineData(135, 1, true)]
	[InlineData(82, 1, true)]
	[InlineData(81, 0, true)]
	[InlineData(0, 0, true)]
	public void ComputeVisibleCount_partitions_at_a_given_width(double available, int expected, bool expectedOverflow)
	{
		//Arrange
		var extents = new[] { 50d, 50d, 50d, 50d, 50d };

		//Act
		var count = ToolBarLayout.ComputeVisibleCount(extents, 4, available, 28, out var hasOverflow);

		//Assert
		count.Should().Be(expected);
		hasOverflow.Should().Be(expectedOverflow);
	}

	[Fact]
	public void ComputeVisibleCount_answers_zero_items_for_an_empty_bar()
	{
		//Arrange
		//Act
		var count = ToolBarLayout.ComputeVisibleCount([], 4, 100, 28, out var hasOverflow);

		//Assert
		count.Should().Be(0);
		hasOverflow.Should().BeFalse();
	}

	[Theory]
	[InlineData(10.4, 1, 10)]
	[InlineData(10.4, 1.25, 10.4)]
	[InlineData(10.5, 1.25, 10.4)]
	[InlineData(3.3, 2, 3.5)]
	public void SnapToDevicePixel_lands_on_a_whole_device_pixel(double value, double scale, double expected)
	{
		//Arrange
		//Act
		var snapped = ToolBarLayout.SnapToDevicePixel(value, scale);

		//Assert
		snapped.Should().BeApproximately(expected, 0.0001);
		(snapped * scale).Should().BeApproximately(System.Math.Round(snapped * scale), 0.0001);
	}
}
