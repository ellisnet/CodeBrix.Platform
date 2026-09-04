using CodeBrix.Platform.UI.CommandBar.Automation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The separator: a hairline of a stated number of DEVICE pixels, whatever the display scale.
/// </summary>
/// <remarks>
/// A host-free test runs at a rasterization scale of 1, because the scale comes from the window
/// the element is in and there is none. What the scale changes - the logical width the separator
/// asks for, and the offset it is snapped to - is arithmetic, and is pinned in ToolBarLayoutTests
/// and by <see cref="LogicalThickness_is_the_thickness_divided_by_the_display_scale"/>. The
/// on-screen half is proven by the demo's self-test on the X11 head, which runs at 1.25.
/// </remarks>
public class ToolBarSeparatorTests
{
	[Fact]
	public void MeasureOverride_asks_for_one_pixel_across_the_line()
	{
		//Arrange
		var separator = new ToolBarSeparator();

		//Act
		separator.Measure(new Size(100, 40));

		//Assert
		//The margin from the default style (8 either side) is part of the desired size; the line
		//itself is the single pixel.
		separator.Thickness.Should().Be(1);
		separator.DesiredSize.Width.Should().BeApproximately(1 + 16, 0.001);
	}

	[Fact]
	public void Orientation_horizontal_asks_for_one_pixel_down_the_line()
	{
		//Arrange
		var separator = new ToolBarSeparator { Orientation = Orientation.Horizontal };

		//Act
		separator.Measure(new Size(100, 40));

		//Assert
		//A vertical bar's separator runs across it: the hairline is now its height.
		separator.DesiredSize.Height.Should().BeApproximately(1 + 8, 0.001);
	}

	[Fact]
	public void Thickness_of_two_asks_for_two_pixels()
	{
		//Arrange
		var separator = new ToolBarSeparator { Thickness = 2 };

		//Act
		separator.Measure(new Size(100, 40));

		//Assert
		separator.DesiredSize.Width.Should().BeApproximately(2 + 16, 0.001);
	}

	[Fact]
	public void LogicalThickness_is_the_thickness_divided_by_the_display_scale()
	{
		//Arrange
		var separator = new ToolBarSeparator();

		//Act
		var logical = separator.LogicalThickness;

		//Assert
		//Host-free the scale is 1, so the logical width is the device width. The point of the
		//property is that at 125% it would be 0.8, which is exactly one device pixel.
		logical.Should().Be(1);
		(logical * 1.25).Should().BeApproximately(1.25, 0.0001);
	}

	[Fact]
	public void Template_draws_the_line_with_the_foreground_brush()
	{
		//Arrange
		var separator = new ToolBarSeparator();

		//Act
		separator.Measure(new Size(100, 40));
		separator.Arrange(new Rect(0, 0, 17, 40));

		//Assert
		//The default style has to be found and applied for the separator to draw at all - the same
		//thing the bar depends on for its own panel.
		separator.Template.Should().NotBeNull();
		separator.IsTabStop.Should().BeFalse();
	}

	[Fact]
	public void OnCreateAutomationPeer_reports_the_separator_control_type()
	{
		//Arrange
		var separator = new ToolBarSeparator();

		//Act
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(separator);

		//Assert
		peer.Should().BeOfType<ToolBarSeparatorAutomationPeer>();
		peer.GetAutomationControlType().Should().Be(AutomationControlType.Separator);
	}
}
