#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_justify_content.c (MIT, Microsoft Corporation).
/// </summary>
public class JustifyContentTests
{
	[Fact]
	public void center_packs_children_around_the_main_axis_center() // test_justify_content1
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.Center };

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 50, 50, 100);
		AssertFrame(child2, 0, 150, 50, 100);
	}

	[Fact]
	public void start_packs_children_at_the_main_axis_start() // test_justify_content2
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.Start };

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 100);
		AssertFrame(child2, 0, 100, 50, 100);
	}

	[Fact]
	public void end_packs_children_at_the_main_axis_end() // test_justify_content3
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.End };

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 100, 50, 100);
		AssertFrame(child2, 0, 200, 50, 100);
	}

	[Fact]
	public void space_between_pushes_two_children_to_the_edges() // test_justify_content4
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.SpaceBetween };

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 100);
		AssertFrame(child2, 0, 200, 50, 100);
	}

	[Fact]
	public void space_between_spaces_three_children_evenly_between_the_edges() // test_justify_content5
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.SpaceBetween };

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
		AssertFrame(child2, 0, 125, 50, 50);
		AssertFrame(child3, 0, 250, 50, 50);
	}

	[Fact]
	public void space_around_gives_two_children_half_size_edge_spaces() // test_justify_content6
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.SpaceAround };

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 25, 50, 100);
		AssertFrame(child2, 0, 175, 50, 100);
	}

	[Fact]
	public void space_around_gives_three_children_equal_surrounding_space() // test_justify_content7
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.SpaceAround };

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50);
		root.Add(child2);

		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 25, 50, 50);
		AssertFrame(child2, 0, 125, 50, 50);
		AssertFrame(child3, 0, 225, 50, 50);
	}

	[Fact]
	public void space_evenly_gives_two_children_equal_spaces_everywhere() // test_justify_content8
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.SpaceEvenly };

		var child1 = new Item(50, 105);
		root.Add(child1);

		var child2 = new Item(50, 105);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 30, 50, 105);
		AssertFrame(child2, 0, 165, 50, 105);
	}

	[Fact]
	public void space_evenly_gives_three_children_equal_spaces_everywhere() // test_justify_content9
	{
		//Arrange
		var root = new Item(100, 300) { JustifyContent = Justify.SpaceEvenly };

		var child1 = new Item(50, 40);
		root.Add(child1);

		var child2 = new Item(50, 40);
		root.Add(child2);

		var child3 = new Item(50, 40);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 45, 50, 40);
		AssertFrame(child2, 0, 130, 50, 40);
		AssertFrame(child3, 0, 215, 50, 40);
	}

	[Fact]
	public void center_composes_with_column_reverse() // test_justify_content10
	{
		//Arrange
		var root = new Item(100, 300)
		{
			Direction = Direction.ColumnReverse,
			JustifyContent = Justify.Center,
		};

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 150, 50, 100);
		AssertFrame(child2, 0, 50, 50, 100);
	}

	[Fact]
	public void start_composes_with_column_reverse() // test_justify_content11
	{
		//Arrange
		var root = new Item(100, 300)
		{
			Direction = Direction.ColumnReverse,
			JustifyContent = Justify.Start,
		};

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 200, 50, 100);
		AssertFrame(child2, 0, 100, 50, 100);
	}

	[Fact]
	public void end_composes_with_column_reverse() // test_justify_content12
	{
		//Arrange
		var root = new Item(100, 300)
		{
			Direction = Direction.ColumnReverse,
			JustifyContent = Justify.End,
		};

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 100, 50, 100);
		AssertFrame(child2, 0, 0, 50, 100);
	}

	[Fact]
	public void space_between_composes_with_column_reverse() // test_justify_content13
	{
		//Arrange
		var root = new Item(100, 300)
		{
			Direction = Direction.ColumnReverse,
			JustifyContent = Justify.SpaceBetween,
		};

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 200, 50, 100);
		AssertFrame(child2, 0, 0, 50, 100);
	}

	[Fact]
	public void space_around_composes_with_column_reverse() // test_justify_content14
	{
		//Arrange
		var root = new Item(100, 300)
		{
			Direction = Direction.ColumnReverse,
			JustifyContent = Justify.SpaceAround,
		};

		var child1 = new Item(50, 100);
		root.Add(child1);

		var child2 = new Item(50, 100);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 175, 50, 100);
		AssertFrame(child2, 0, 25, 50, 100);
	}

	[Fact]
	public void space_evenly_composes_with_column_reverse() // test_justify_content15
	{
		//Arrange
		var root = new Item(100, 300)
		{
			Direction = Direction.ColumnReverse,
			JustifyContent = Justify.SpaceEvenly,
		};

		var child1 = new Item(50, 105);
		root.Add(child1);

		var child2 = new Item(50, 105);
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 165, 50, 105);
		AssertFrame(child2, 0, 30, 50, 105);
	}

	private static readonly Justify[] AllJustifyModes =
	{
		Justify.Center,
		Justify.Start,
		Justify.End,
		Justify.SpaceBetween,
		Justify.SpaceAround,
		Justify.SpaceEvenly,
	};

	[Fact]
	public void justify_content_is_ignored_when_children_fill_the_space() // test_justify_content16
	{
		foreach (var mode in AllJustifyModes)
		{
			//Arrange
			var root = new Item(100, 100) { JustifyContent = mode };

			var child1 = new Item(50, 50);
			root.Add(child1);

			var child2 = new Item(50, 50);
			root.Add(child2);

			//Act
			Layout(root);

			//Assert
			AssertFrame(child1, 0, 0, 50, 50);
			AssertFrame(child2, 0, 50, 50, 50);
		}
	}

	[Fact]
	public void justify_content_is_ignored_when_children_overflow_the_space() // test_justify_content17
	{
		foreach (var mode in AllJustifyModes)
		{
			//Arrange
			var root = new Item(100, 100) { JustifyContent = mode };

			var child1 = new Item(50, 100);
			root.Add(child1);

			var child2 = new Item(50, 100);
			root.Add(child2);

			var child3 = new Item(50, 100);
			root.Add(child3);

			var child4 = new Item(50, 100);
			root.Add(child4);

			//Act
			Layout(root);

			//Assert
			AssertFrame(child1, 0, 0, 50, 25);
			AssertFrame(child2, 0, 25, 50, 25);
			AssertFrame(child3, 0, 50, 50, 25);
			AssertFrame(child4, 0, 75, 50, 25);
		}
	}

	[Fact]
	public void justify_content_is_ignored_when_a_child_grows() // test_justify_content18
	{
		foreach (var mode in AllJustifyModes)
		{
			//Arrange
			var root = new Item(100, 100) { JustifyContent = mode };

			var child1 = new Item(50, 20);
			root.Add(child1);

			var child2 = new Item(50, 20) { Grow = 1 };
			root.Add(child2);

			var child3 = new Item(50, 20);
			root.Add(child3);

			//Act
			Layout(root);

			//Assert
			AssertFrame(child1, 0, 0, 50, 20);
			AssertFrame(child2, 0, 20, 50, 60);
			AssertFrame(child3, 0, 80, 50, 20);
		}
	}
}
