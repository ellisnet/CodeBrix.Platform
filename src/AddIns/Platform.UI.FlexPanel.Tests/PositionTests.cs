#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_position.c (MIT, Microsoft Corporation). These exercise
/// the engine's per-item absolute positioning, which the FlexPanel control does not currently
/// expose (matching the .NET MAUI FlexLayout public surface).
/// </summary>
public class PositionTests
{
	[Fact]
	public void an_absolute_item_defaults_to_the_top_left_corner() // test_position1
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(10, 10) { Position = Position.Absolute };
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 10, 10);
	}

	[Fact]
	public void absolute_items_anchor_to_each_corner() // test_position2
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(10, 10) { Position = Position.Absolute, Left = 10, Top = 10 };
		root.Add(child1);

		var child2 = new Item(10, 10) { Position = Position.Absolute, Right = 10, Top = 10 };
		root.Add(child2);

		var child3 = new Item(10, 10) { Position = Position.Absolute, Right = 10, Bottom = 10 };
		root.Add(child3);

		var child4 = new Item(10, 10) { Position = Position.Absolute, Left = 10, Bottom = 10 };
		root.Add(child4);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 10, 10, 10);
		AssertFrame(child2, 80, 10, 10, 10);
		AssertFrame(child3, 80, 80, 10, 10);
		AssertFrame(child4, 10, 80, 10, 10);
	}

	[Fact]
	public void left_and_top_win_when_the_size_dimension_is_set() // test_position3
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(10, 10) { Position = Position.Absolute, Left = 10, Right = 10 };
		root.Add(child1);

		var child2 = new Item(10, 10) { Position = Position.Absolute, Top = 10, Bottom = 10 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 0, 10, 10);
		AssertFrame(child2, 0, 10, 10, 10);
	}

	[Fact]
	public void opposing_anchors_size_the_item_when_the_dimension_is_unset() // test_position4
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item { Height = 20, Position = Position.Absolute, Left = 10, Right = 10 };
		root.Add(child1);

		var child2 = new Item { Width = 20, Position = Position.Absolute, Top = 10, Bottom = 10 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 0, 80, 20);
		AssertFrame(child2, 0, 10, 20, 80);
	}

	[Fact]
	public void basis_is_ignored_for_absolute_items() // test_position5
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(10, 10)
		{
			Basis = new Basis(20),
			Position = Position.Absolute,
			Left = 10,
			Bottom = 10,
		};
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 80, 10, 10);
	}

	[Fact]
	public void absolute_items_do_not_participate_in_flex_flow() // test_position6
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.Row };

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50) { Position = Position.Absolute, Right = 0, Bottom = 0 };
		root.Add(child2);

		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 50);
		AssertFrame(child2, 150, 150, 50, 50);
		AssertFrame(child3, 50, 0, 50, 50);
	}

	[Fact]
	public void absolute_items_are_excluded_from_spacing_calculations() // test_position7
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			JustifyContent = Justify.SpaceAround,
			AlignContent = AlignContent.Start,
		};

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50);
		root.Add(child2);

		var child3 = new Item(50, 50) { Position = Position.Absolute, Right = 0, Top = 0 };
		root.Add(child3);

		var child4 = new Item(50, 50);
		root.Add(child4);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 5, 50, 50);
		AssertFrame(child2, 0, 65, 50, 50);
		AssertFrame(child3, 70, 0, 50, 50);
		AssertFrame(child4, 50, 35, 50, 50);
	}

	[Fact]
	public void absolute_items_can_nest() // test_position8
	{
		//Arrange
		var root = new Item(100, 100) { Direction = Direction.Row };

		var child1 = new Item
		{
			Position = Position.Absolute,
			Left = 10,
			Top = 10,
			Right = 10,
			Bottom = 10,
		};
		root.Add(child1);

		var child2 = new Item(60, 60) { Position = Position.Absolute, Right = 10, Top = 10 };
		child1.Add(child2);

		var child3 = new Item(40, 40) { Position = Position.Absolute, Left = 10, Bottom = 10 };
		child2.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 10, 80, 80);
		AssertFrame(child2, 10, 10, 60, 60);
		AssertFrame(child3, 10, 10, 40, 40);
	}
}
