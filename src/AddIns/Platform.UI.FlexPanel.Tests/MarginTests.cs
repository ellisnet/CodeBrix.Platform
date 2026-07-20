#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_margin.c (MIT, Microsoft Corporation).
/// </summary>
public class MarginTests
{
	private static Item MakeMarginRoot(AlignItems alignItems, Justify justifyContent)
	{
		var root = new Item(100, 100)
		{
			AlignItems = alignItems,
			JustifyContent = justifyContent,
		};

		root.Add(new Item(25, 25));

		root.Add(new Item(25, 25)
		{
			MarginTop = 10,
			MarginBottom = 10,
			MarginLeft = 15,
			MarginRight = 15,
		});

		root.Add(new Item(25, 25));

		return root;
	}

	[Fact]
	public void margins_offset_children_with_start_alignment() // test_margin1
	{
		//Arrange
		var root = MakeMarginRoot(AlignItems.Start, Justify.Start);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 0, 0, 25, 25);
		AssertFrame(root[1], 15, 35, 25, 25);
		AssertFrame(root[2], 0, 70, 25, 25);
	}

	[Fact]
	public void margins_offset_children_with_end_alignment() // test_margin2
	{
		//Arrange
		var root = MakeMarginRoot(AlignItems.End, Justify.Start);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 75, 0, 25, 25);
		AssertFrame(root[1], 60, 35, 25, 25);
		AssertFrame(root[2], 75, 70, 25, 25);
	}

	[Fact]
	public void margins_offset_children_with_end_justification() // test_margin3
	{
		//Arrange
		var root = MakeMarginRoot(AlignItems.Start, Justify.End);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 0, 5, 25, 25);
		AssertFrame(root[1], 15, 40, 25, 25);
		AssertFrame(root[2], 0, 75, 25, 25);
	}

	[Fact]
	public void margins_offset_children_with_end_alignment_and_end_justification() // test_margin4
	{
		//Arrange
		var root = MakeMarginRoot(AlignItems.End, Justify.End);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 75, 5, 25, 25);
		AssertFrame(root[1], 60, 40, 25, 25);
		AssertFrame(root[2], 75, 75, 25, 25);
	}

	[Fact]
	public void unbalanced_margins_shift_a_centered_child() // test_margin5
	{
		//Arrange
		var root = new Item(100, 100)
		{
			AlignItems = AlignItems.Center,
			JustifyContent = Justify.Start,
		};

		var child1 = new Item(10, 10);
		root.Add(child1);

		var child2 = new Item(10, 10) { MarginLeft = 15, MarginRight = 10 };
		root.Add(child2);

		var child3 = new Item(10, 10);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 45, 0, 10, 10);
		AssertFrame(child2, 50, 10, 10, 10);
		AssertFrame(child3, 45, 20, 10, 10);
	}

	[Fact]
	public void stretch_subtracts_margins_from_the_stretched_size() // test_margin6
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(10, 10);
		root.Add(child1);

		var child2 = new Item(0, 10)
		{
			AlignSelf = AlignSelf.Stretch,
			MarginLeft = 15,
			MarginRight = 10,
		};
		root.Add(child2);

		var child3 = new Item(10, 10);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 10, 10);
		AssertFrame(child2, 15, 10, 75, 10);
		AssertFrame(child3, 0, 20, 10, 10);
	}

	[Fact]
	public void stretch_does_not_resize_a_child_with_a_cross_axis_size() // test_margin7
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(10, 10);
		root.Add(child1);

		var child2 = new Item(10, 10)
		{
			AlignSelf = AlignSelf.Stretch,
			MarginLeft = 15,
			MarginRight = 10,
		};
		root.Add(child2);

		var child3 = new Item(10, 10);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 10, 10);
		AssertFrame(child2, 15, 10, 10, 10);
		AssertFrame(child3, 0, 20, 10, 10);
	}

	[Fact]
	public void default_cross_axis_size_accounts_for_margins_in_column() // test_margin8
	{
		//Arrange
		var root = new Item(100, 100) { Direction = Direction.Column };

		var child1 = new Item { Height = 10, MarginLeft = 10 };
		root.Add(child1);

		var child2 = new Item { Height = 10, MarginRight = 10 };
		root.Add(child2);

		var child3 = new Item { Height = 10, MarginLeft = 10, MarginRight = 20 };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 0, 90, 10);
		AssertFrame(child2, 0, 10, 90, 10);
		AssertFrame(child3, 10, 20, 70, 10);
	}

	[Fact]
	public void default_cross_axis_size_accounts_for_margins_in_row() // test_margin9
	{
		//Arrange
		var root = new Item(100, 100) { Direction = Direction.Row };

		var child1 = new Item { Width = 10, MarginTop = 10 };
		root.Add(child1);

		var child2 = new Item { Width = 10, MarginBottom = 10 };
		root.Add(child2);

		var child3 = new Item { Width = 10, MarginTop = 10, MarginBottom = 20 };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 10, 10, 90);
		AssertFrame(child2, 10, 0, 10, 90);
		AssertFrame(child3, 20, 10, 10, 70);
	}

	[Fact]
	public void a_growing_child_leaves_room_for_its_margins() // test_margin10
	{
		//Arrange
		var root = new Item(100, 100) { Direction = Direction.Row };

		var child1 = new Item { Grow = 1, MarginLeft = 10, MarginRight = 10 };
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 0, 80, 100);
	}
}
