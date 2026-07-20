#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_order.c (MIT, Microsoft Corporation).
/// </summary>
public class OrderTests
{
	[Fact]
	public void children_are_arranged_by_ascending_order_value() // test_order1
	{
		//Arrange
		var root = new Item(200, 200);

		var child1 = new Item(50, 50) { Order = 1 };
		root.Add(child1);

		var child2 = new Item(50, 50) { Order = 3 };
		root.Add(child2);

		var child3 = new Item(50, 50) { Order = 2 };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 50);
		AssertFrame(child2, 0, 100, 50, 50);
		AssertFrame(child3, 0, 50, 50, 50);
	}

	[Fact]
	public void order_composes_with_reversed_directions() // test_order2
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.ColumnReverse };

		var child1 = new Item(50, 50) { Order = 2 };
		root.Add(child1);

		var child2 = new Item(50, 50) { Order = 3 };
		root.Add(child2);

		var child3 = new Item(50, 50) { Order = 1 };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 100, 50, 50);
		AssertFrame(child2, 0, 50, 50, 50);
		AssertFrame(child3, 0, 150, 50, 50);
	}

	[Fact]
	public void negative_order_moves_a_child_before_default_ordered_siblings() // test_order3
	{
		//Arrange
		var root = new Item(200, 200);

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50) { Order = -1 };
		root.Add(child2);

		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 50, 50, 50);
		AssertFrame(child2, 0, 0, 50, 50);
		AssertFrame(child3, 0, 100, 50, 50);
	}

	[Fact]
	public void insertion_order_is_preserved_between_children_with_equal_order() // test_order4
	{
		//Arrange
		var root = new Item(200, 200);

		const int max = 100;
		for (int i = 0; i < max; i++)
		{
			root.Add(new Item(1, 1));
		}

		root[0].Order = 1;
		root[max - 1].Order = -1;

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[max - 1], 0, 0, 1, 1);

		for (int i = 1; i < max - 1; i++)
		{
			AssertFrame(root[i], 0, i, 1, 1);
		}

		AssertFrame(root[0], 0, max - 1, 1, 1);
	}
}
