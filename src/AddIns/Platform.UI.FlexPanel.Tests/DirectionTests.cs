#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_direction.c (MIT, Microsoft Corporation).
/// </summary>
public class DirectionTests
{
	[Fact]
	public void row_stacks_children_horizontally() // test_direction1
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.Row };
		var child1 = new Item(50, 50);
		root.Add(child1);
		var child2 = new Item(50, 50);
		root.Add(child2);
		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 50);
		AssertFrame(child2, 50, 0, 50, 50);
		AssertFrame(child3, 100, 0, 50, 50);
	}

	[Fact]
	public void column_stacks_children_vertically() // test_direction2
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.Column };
		var child1 = new Item(50, 50);
		root.Add(child1);
		var child2 = new Item(50, 50);
		root.Add(child2);
		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 50);
		AssertFrame(child2, 0, 50, 50, 50);
		AssertFrame(child3, 0, 100, 50, 50);
	}

	[Fact]
	public void row_reverse_stacks_children_right_to_left() // test_direction3
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.RowReverse };
		var child1 = new Item(50, 50);
		root.Add(child1);
		var child2 = new Item(50, 50);
		root.Add(child2);
		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 150, 0, 50, 50);
		AssertFrame(child2, 100, 0, 50, 50);
		AssertFrame(child3, 50, 0, 50, 50);
	}

	[Fact]
	public void column_reverse_stacks_children_bottom_to_top() // test_direction4
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.ColumnReverse };
		var child1 = new Item(50, 50);
		root.Add(child1);
		var child2 = new Item(50, 50);
		root.Add(child2);
		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 150, 50, 50);
		AssertFrame(child2, 0, 100, 50, 50);
		AssertFrame(child3, 0, 50, 50, 50);
	}
}
