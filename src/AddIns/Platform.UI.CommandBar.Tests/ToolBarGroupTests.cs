using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The group: a run of items with its own tighter spacing, laid out along the bar's axis.
/// </summary>
public class ToolBarGroupTests
{
	[Fact]
	public void Spacing_uses_the_group_gap_rather_than_the_bar_gap()
	{
		//Arrange
		var group = CreateGroup(3);
		group.Spacing = 2;

		//Act
		LayOut(group, 500, 100);

		//Assert
		group.DesiredSize.Width.Should().BeApproximately((3 * 30) + (2 * 2), 0.5);
	}

	[Fact]
	public void Spacing_defaults_to_four_pixels()
	{
		//Arrange
		var group = CreateGroup(2);

		//Act
		LayOut(group, 500, 100);

		//Assert
		group.Spacing.Should().Be(ToolBarGroup.DefaultSpacing);
		group.DesiredSize.Width.Should().BeApproximately(30 + 4 + 30, 0.5);
	}

	[Fact]
	public void Orientation_vertical_stacks_the_group_items()
	{
		//Arrange
		var group = CreateGroup(3);
		group.Orientation = Orientation.Vertical;

		//Act
		LayOut(group, 500, 500);

		//Assert
		group.DesiredSize.Height.Should().BeApproximately((3 * 20) + (2 * 4), 0.5);
		group.DesiredSize.Width.Should().BeApproximately(30, 0.5);
	}

	[Fact]
	public void Visibility_a_collapsed_group_item_takes_no_space()
	{
		//Arrange
		var group = CreateGroup(3);
		group.Children[1].Visibility = Visibility.Collapsed;

		//Act
		LayOut(group, 500, 100);

		//Assert
		//Two items and one gap, not three items and two gaps - and no gap left where the collapsed
		//item was.
		group.DesiredSize.Width.Should().BeApproximately(30 + 4 + 30, 0.5);
	}

	[Fact]
	public void MeasureOverride_an_empty_group_takes_no_space()
	{
		//Arrange
		var group = new ToolBarGroup();

		//Act
		LayOut(group, 500, 100);

		//Assert
		group.DesiredSize.Width.Should().Be(0);
		group.DesiredSize.Height.Should().Be(0);
	}

	[Fact]
	public void ArrangeOverride_lays_the_items_end_to_end()
	{
		//Arrange
		var group = CreateGroup(3);

		//Act
		LayOut(group, 500, 100);

		//Assert
		//The group is one item as far as the bar is concerned, so its own children are the only
		//thing it positions: 30 wide each, 4 apart.
		var first = (Border)group.Children[0];
		var second = (Border)group.Children[1];
		first.ActualWidth.Should().BeApproximately(30, 0.5);
		second.ActualWidth.Should().BeApproximately(30, 0.5);
		group.ActualWidth.Should().BeApproximately(500, 0.5);
	}

	private static ToolBarGroup CreateGroup(int items)
	{
		var group = new ToolBarGroup();
		for (var i = 0; i < items; i++)
		{
			group.Children.Add(new Border { Width = 30, Height = 20 });
		}

		return group;
	}

	private static void LayOut(FrameworkElement element, double width, double height)
	{
		element.Measure(new Size(width, height));
		element.Arrange(new Rect(0, 0, width, height));
	}
}
