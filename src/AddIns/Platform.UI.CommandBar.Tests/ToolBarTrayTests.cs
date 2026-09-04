using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The tray: several bars in one row while there is room, and stacked when there is not.
/// </summary>
public class ToolBarTrayTests
{
	[Fact]
	public void MeasureOverride_puts_two_bars_side_by_side_when_they_fit()
	{
		//Arrange
		var tray = new ToolBarTray();
		tray.Children.Add(CreateBar(2));
		tray.Children.Add(CreateBar(2));

		//Act
		LayOut(tray, 600, 200);

		//Assert
		//Each bar is 2 x 50 with a 4-pixel gap plus 8 of padding, so 112; the tray's own 8-pixel
		//gap sits between them.
		tray.DesiredSize.Width.Should().BeApproximately(112 + 8 + 112, 0.5);
		tray.DesiredSize.Height.Should().BeApproximately(20 + 8 + 1, 0.5);
	}

	[Fact]
	public void MeasureOverride_wraps_a_bar_that_does_not_fit_onto_a_further_row()
	{
		//Arrange
		var tray = new ToolBarTray();
		tray.Children.Add(CreateBar(2));
		tray.Children.Add(CreateBar(2));

		//Act
		LayOut(tray, 200, 200);

		//Assert
		//Two rows of 29-tall bars with the tray's 8-pixel gap between them.
		tray.DesiredSize.Height.Should().BeApproximately(29 + 8 + 29, 0.5);
		tray.DesiredSize.Width.Should().BeApproximately(112, 0.5);
	}

	[Fact]
	public void ToolBarSpacing_sets_the_gap_between_two_bars()
	{
		//Arrange
		var tray = new ToolBarTray { ToolBarSpacing = 24 };
		tray.Children.Add(CreateBar(2));
		tray.Children.Add(CreateBar(2));

		//Act
		LayOut(tray, 600, 200);

		//Assert
		tray.DesiredSize.Width.Should().BeApproximately(112 + 24 + 112, 0.5);
	}

	[Fact]
	public void Orientation_vertical_stacks_the_bars_and_turns_them()
	{
		//Arrange
		var tray = new ToolBarTray { Orientation = Orientation.Vertical };
		var first = CreateBar(2);
		var second = CreateBar(2);
		tray.Children.Add(first);
		tray.Children.Add(second);

		//Act
		LayOut(tray, 400, 600);

		//Assert
		//A vertical tray of bars the tray itself turned: each bar is now 2 items tall.
		first.Orientation.Should().Be(Orientation.Vertical);
		second.Orientation.Should().Be(Orientation.Vertical);
		tray.DesiredSize.Height.Should().BeApproximately(53 + 8 + 53, 0.5);
	}

	[Fact]
	public void Orientation_leaves_a_bar_that_stated_its_own_alone()
	{
		//Arrange
		var tray = new ToolBarTray { Orientation = Orientation.Vertical };
		var stubborn = CreateBar(2);
		stubborn.Orientation = Orientation.Horizontal;
		tray.Children.Add(stubborn);

		//Act
		LayOut(tray, 400, 600);

		//Assert
		stubborn.Orientation.Should().Be(Orientation.Horizontal);
	}

	[Fact]
	public void Orientation_changed_later_still_turns_the_bars_the_tray_had_filled_in()
	{
		//Arrange
		var tray = new ToolBarTray();
		var bar = CreateBar(2);
		tray.Children.Add(bar);
		LayOut(tray, 400, 600);

		//Act
		tray.Orientation = Orientation.Vertical;
		LayOut(tray, 400, 600);

		//Assert
		//The tray set the value once, which made it local; remembering that it was the tray's doing
		//is what lets the bar follow a later change.
		bar.Orientation.Should().Be(Orientation.Vertical);
	}

	[Fact]
	public void Visibility_a_collapsed_bar_takes_no_room_in_the_tray()
	{
		//Arrange
		var tray = new ToolBarTray();
		var visible = CreateBar(2);
		var hidden = CreateBar(2);
		hidden.Visibility = Visibility.Collapsed;
		tray.Children.Add(visible);
		tray.Children.Add(hidden);

		//Act
		LayOut(tray, 600, 200);

		//Assert
		tray.DesiredSize.Width.Should().BeApproximately(112, 0.5);
	}

	private static ToolBar CreateBar(int items)
	{
		var bar = new ToolBar { OverflowMode = OverflowMode.None };
		for (var i = 0; i < items; i++)
		{
			bar.Items.Add(new Border { Width = 50, Height = 20 });
		}

		return bar;
	}

	private static void LayOut(FrameworkElement element, double width, double height)
	{
		element.Measure(new Size(width, height));
		element.Arrange(new Rect(0, 0, width, height));
	}
}
