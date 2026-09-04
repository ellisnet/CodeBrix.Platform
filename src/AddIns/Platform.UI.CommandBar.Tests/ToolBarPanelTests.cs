using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The panel a bar lays its items out in, on its own: spacing, wrapping, and how items are aligned
/// across the run.
/// </summary>
public class ToolBarPanelTests
{
	[Fact]
	public void ItemSpacing_puts_the_gap_between_items_but_not_at_the_ends()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 10 };
		panel.Children.Add(new Border { Width = 30, Height = 20 });
		panel.Children.Add(new Border { Width = 30, Height = 20 });
		panel.Children.Add(new Border { Width = 30, Height = 20 });

		//Act
		LayOut(panel, 500, 40);

		//Assert
		panel.DesiredSize.Width.Should().BeApproximately((3 * 30) + (2 * 10), 0.5);
	}

	[Fact]
	public void Wrap_false_lets_the_items_run_past_the_end()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0 };
		for (var i = 0; i < 5; i++)
		{
			panel.Children.Add(new Border { Width = 50, Height = 20 });
		}

		//Act
		//The framework clamps DesiredSize to what was offered, so the run's real extent is read
		//from an unbounded measure; the arrange below is what a 100-wide parent would do with it.
		panel.Measure(new Size(double.PositiveInfinity, 40));
		var unbounded = panel.DesiredSize;
		LayOut(panel, 100, 40);

		//Assert
		//One line, 250 wide, in a 100-wide panel: clipping is the parent's business, not the
		//panel's, which is exactly what OverflowMode.None means on a bar.
		unbounded.Width.Should().BeApproximately(250, 0.5);
		unbounded.Height.Should().BeApproximately(20, 0.5);
		panel.DesiredSize.Height.Should().BeApproximately(20, 0.5);
	}

	[Fact]
	public void Wrap_true_continues_on_a_further_line()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0, Wrap = true };
		for (var i = 0; i < 5; i++)
		{
			panel.Children.Add(new Border { Width = 50, Height = 20 });
		}

		//Act
		LayOut(panel, 100, 200);

		//Assert
		//Two per line, three lines, with the item spacing used between the lines as well.
		panel.DesiredSize.Width.Should().BeApproximately(100, 0.5);
		panel.DesiredSize.Height.Should().BeApproximately(60, 0.5);
	}

	[Fact]
	public void ArrangeOverride_centres_an_item_that_asks_to_be_centred()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0 };
		var centred = new Border { Width = 30, Height = 20, VerticalAlignment = VerticalAlignment.Center };
		panel.Children.Add(centred);

		//Act
		LayOut(panel, 200, 60);

		//Assert
		centred.ActualHeight.Should().BeApproximately(20, 0.5);
	}

	[Fact]
	public void ArrangeOverride_stretches_an_item_across_the_run_by_default()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 0 };
		var stretched = new Border { Width = 30 };
		panel.Children.Add(stretched);

		//Act
		LayOut(panel, 200, 60);

		//Assert
		//A separator relies on this: with no height of its own it fills the bar and its margin
		//insets it.
		stretched.ActualHeight.Should().BeApproximately(60, 0.5);
	}

	[Fact]
	public void Orientation_vertical_runs_the_items_down_the_panel()
	{
		//Arrange
		var panel = new ToolBarPanel { ItemSpacing = 6, Orientation = Orientation.Vertical };
		panel.Children.Add(new Border { Width = 30, Height = 20 });
		panel.Children.Add(new Border { Width = 40, Height = 20 });

		//Act
		LayOut(panel, 200, 400);

		//Assert
		panel.DesiredSize.Height.Should().BeApproximately(20 + 6 + 20, 0.5);
		panel.DesiredSize.Width.Should().BeApproximately(40, 0.5);
	}

	private static void LayOut(FrameworkElement element, double width, double height)
	{
		element.Measure(new Size(width, height));
		element.Arrange(new Rect(0, 0, width, height));
	}
}
