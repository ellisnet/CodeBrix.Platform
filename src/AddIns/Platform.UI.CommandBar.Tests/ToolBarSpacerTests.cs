using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The spacer: a fixed gap, or all the room that is left.
/// </summary>
public class ToolBarSpacerTests
{
	[Fact]
	public void Width_a_fixed_spacer_asks_for_exactly_its_width()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0 };
		var spacer = new ToolBarSpacer { Width = 24 };
		panel.Children.Add(spacer);

		//Act
		LayOut(panel, 400, 40);

		//Assert
		spacer.DesiredSize.Width.Should().BeApproximately(24, 0.001);
		spacer.ActualWidth.Should().BeApproximately(24, 0.001);
	}

	[Fact]
	public void Fill_pushes_what_follows_to_the_far_end()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0 };
		var left = new Border { Width = 50, Height = 20 };
		var spacer = new ToolBarSpacer { Fill = true };
		var right = new Border { Width = 30, Height = 20 };
		panel.Children.Add(left);
		panel.Children.Add(spacer);
		panel.Children.Add(right);

		//Act
		LayOut(panel, 400, 40);

		//Assert
		//400 - 50 - 30 is the spacer's share, which puts the trailing item's left edge at 370.
		spacer.ActualWidth.Should().BeApproximately(320, 0.5);
		panel.DesiredSize.Width.Should().BeApproximately(80, 0.5);
	}

	[Fact]
	public void Fill_two_spacers_share_the_room_that_is_left_equally()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0 };
		panel.Children.Add(new Border { Width = 40, Height = 20 });
		var first = new ToolBarSpacer { Fill = true };
		panel.Children.Add(first);
		panel.Children.Add(new Border { Width = 40, Height = 20 });
		var second = new ToolBarSpacer { Fill = true };
		panel.Children.Add(second);
		panel.Children.Add(new Border { Width = 40, Height = 20 });

		//Act
		LayOut(panel, 400, 40);

		//Assert
		//Left, centre and right runs from two filling spacers: (400 - 120) / 2 each.
		first.ActualWidth.Should().BeApproximately(140, 0.5);
		second.ActualWidth.Should().BeApproximately(140, 0.5);
	}

	[Fact]
	public void Fill_asks_for_nothing_when_the_panel_has_no_width_to_give()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0 };
		var spacer = new ToolBarSpacer { Fill = true };
		panel.Children.Add(new Border { Width = 40, Height = 20 });
		panel.Children.Add(spacer);

		//Act
		//An unbounded width - a horizontally scrolling parent - has nothing left over to hand out.
		panel.Measure(new Size(double.PositiveInfinity, 40));

		//Assert
		panel.DesiredSize.Width.Should().BeApproximately(40, 0.5);
	}

	[Fact]
	public void Fill_in_a_vertical_panel_takes_the_height_that_is_left()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0, Orientation = Orientation.Vertical };
		var spacer = new ToolBarSpacer { Fill = true };
		panel.Children.Add(new Border { Width = 40, Height = 20 });
		panel.Children.Add(spacer);
		panel.Children.Add(new Border { Width = 40, Height = 20 });

		//Act
		LayOut(panel, 100, 300);

		//Assert
		spacer.ActualHeight.Should().BeApproximately(260, 0.5);
	}

	private static void LayOut(FrameworkElement element, double width, double height)
	{
		element.Measure(new Size(width, height));
		element.Arrange(new Rect(0, 0, width, height));
	}
}
