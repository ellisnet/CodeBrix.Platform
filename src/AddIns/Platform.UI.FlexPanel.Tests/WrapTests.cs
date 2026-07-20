#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_wrap.c (MIT, Microsoft Corporation).
/// </summary>
public class WrapTests
{
	[Fact]
	public void no_wrap_shrinks_children_onto_a_single_line() // test_wrap1
	{
		//Arrange
		var root = new Item(100, 300) { Wrap = Wrap.NoWrap };

		var child1 = new Item(100, 150);
		root.Add(child1);

		var child2 = new Item(100, 150);
		root.Add(child2);

		var child3 = new Item(100, 150);
		root.Add(child3);

		var child4 = new Item(100, 150);
		root.Add(child4);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 75);
		AssertFrame(child2, 0, 75, 100, 75);
		AssertFrame(child3, 0, 150, 100, 75);
		AssertFrame(child4, 0, 225, 100, 75);
	}

	[Fact]
	public void children_wrap_onto_a_second_line() // test_wrap2
	{
		//Arrange
		var root = new Item(100, 300)
		{
			Wrap = Wrap.Wrap,
			AlignContent = AlignContent.Start,
		};

		var child1 = new Item(50, 150);
		root.Add(child1);

		var child2 = new Item(50, 150);
		root.Add(child2);

		var child3 = new Item(50, 150);
		root.Add(child3);

		var child4 = new Item(50, 150);
		root.Add(child4);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 150);
		AssertFrame(child2, 0, 150, 50, 150);
		AssertFrame(child3, 50, 0, 50, 150);
		AssertFrame(child4, 50, 150, 50, 150);
	}

	[Fact]
	public void a_child_that_does_not_fit_moves_to_the_next_line() // test_wrap3
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			AlignContent = AlignContent.Start,
		};

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
		AssertFrame(child3, 50, 0, 50, 50);
	}

	[Fact]
	public void wrapping_fills_lines_completely_before_moving_on() // test_wrap4
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			AlignContent = AlignContent.Start,
		};

		var children = new Item[6];
		for (int i = 0; i < children.Length; i++)
		{
			children[i] = new Item(25, 50);
			root.Add(children[i]);
		}

		//Act
		Layout(root);

		//Assert
		AssertFrame(children[0], 0, 0, 25, 50);
		AssertFrame(children[1], 0, 50, 25, 50);
		AssertFrame(children[2], 25, 0, 25, 50);
		AssertFrame(children[3], 25, 50, 25, 50);
		AssertFrame(children[4], 50, 0, 25, 50);
		AssertFrame(children[5], 50, 50, 25, 50);
	}

	[Fact]
	public void justify_end_applies_per_line() // test_wrap5
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			JustifyContent = Justify.End,
			AlignContent = AlignContent.Start,
		};

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50);
		root.Add(child2);

		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 20, 50, 50);
		AssertFrame(child2, 0, 70, 50, 50);
		AssertFrame(child3, 50, 70, 50, 50);
	}

	[Fact]
	public void justify_center_applies_per_line() // test_wrap6
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			JustifyContent = Justify.Center,
			AlignContent = AlignContent.Start,
		};

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50);
		root.Add(child2);

		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 10, 50, 50);
		AssertFrame(child2, 0, 60, 50, 50);
		AssertFrame(child3, 50, 35, 50, 50);
	}

	[Fact]
	public void justify_space_around_applies_per_line() // test_wrap7
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

		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 5, 50, 50);
		AssertFrame(child2, 0, 65, 50, 50);
		AssertFrame(child3, 50, 35, 50, 50);
	}

	[Fact]
	public void justify_space_between_applies_per_line() // test_wrap8
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			JustifyContent = Justify.SpaceBetween,
			AlignContent = AlignContent.Start,
		};

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
		AssertFrame(child2, 0, 70, 50, 50);
		AssertFrame(child3, 50, 0, 50, 50);
	}

	[Fact]
	public void grow_applies_per_line() // test_wrap9
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			AlignContent = AlignContent.Start,
		};

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50) { Grow = 1 };
		root.Add(child2);

		var child3 = new Item(50, 50) { Grow = 1 };
		root.Add(child3);

		var child4 = new Item(50, 50);
		root.Add(child4);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 50);
		AssertFrame(child2, 0, 50, 50, 70);
		AssertFrame(child3, 50, 0, 50, 70);
		AssertFrame(child4, 50, 70, 50, 50);
	}

	[Fact]
	public void align_items_start_applies_within_each_line() // test_wrap10
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			AlignItems = AlignItems.Start,
		};

		var child1 = new Item(50, 40);
		root.Add(child1);

		var child2 = new Item(70, 30);
		root.Add(child2);

		var child3 = new Item(60, 40);
		root.Add(child3);

		var child4 = new Item(40, 50);
		root.Add(child4);

		var child5 = new Item(50, 60);
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 40);
		AssertFrame(child2, 0, 40, 70, 30);
		AssertFrame(child3, 0, 70, 60, 40);
		AssertFrame(child4, 70, 0, 40, 50);
		AssertFrame(child5, 70, 50, 50, 60);
	}

	[Fact]
	public void align_items_center_applies_within_each_line() // test_wrap11
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			AlignItems = AlignItems.Center,
		};

		var child1 = new Item(50, 40);
		root.Add(child1);

		var child2 = new Item(70, 30);
		root.Add(child2);

		var child3 = new Item(60, 40);
		root.Add(child3);

		var child4 = new Item(40, 50);
		root.Add(child4);

		var child5 = new Item(50, 60);
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 0, 50, 40);
		AssertFrame(child2, 0, 40, 70, 30);
		AssertFrame(child3, 5, 70, 60, 40);
		AssertFrame(child4, 75, 0, 40, 50);
		AssertFrame(child5, 70, 50, 50, 60);
	}

	[Fact]
	public void align_items_end_applies_within_each_line() // test_wrap12
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.Wrap,
			AlignItems = AlignItems.End,
		};

		var child1 = new Item(50, 40);
		root.Add(child1);

		var child2 = new Item(70, 30);
		root.Add(child2);

		var child3 = new Item(60, 40);
		root.Add(child3);

		var child4 = new Item(40, 50);
		root.Add(child4);

		var child5 = new Item(50, 60);
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 20, 0, 50, 40);
		AssertFrame(child2, 0, 40, 70, 30);
		AssertFrame(child3, 10, 70, 60, 40);
		AssertFrame(child4, 80, 0, 40, 50);
		AssertFrame(child5, 70, 50, 50, 60);
	}

	[Fact]
	public void align_self_applies_within_each_line() // test_wrap13
	{
		//Arrange
		var root = new Item(120, 120) { Wrap = Wrap.Wrap };

		var child1 = new Item(50, 40) { AlignSelf = AlignSelf.End };
		root.Add(child1);

		var child2 = new Item(70, 30);
		root.Add(child2);

		var child3 = new Item(60, 40) { AlignSelf = AlignSelf.Center };
		root.Add(child3);

		var child4 = new Item(40, 50) { AlignSelf = AlignSelf.Start };
		root.Add(child4);

		var child5 = new Item(50, 60);
		root.Add(child5);

		var child6 = new Item(10, 10) { AlignSelf = AlignSelf.End };
		root.Add(child6);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 20, 0, 50, 40);
		AssertFrame(child2, 0, 40, 70, 30);
		AssertFrame(child3, 5, 70, 60, 40);
		AssertFrame(child4, 70, 0, 40, 50);
		AssertFrame(child5, 70, 50, 50, 60);
		AssertFrame(child6, 110, 110, 10, 10);
	}

	[Fact]
	public void wrap_reverse_stacks_lines_from_the_cross_axis_end() // test_wrap14
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.WrapReverse,
			AlignContent = AlignContent.Start,
		};

		var child1 = new Item(50, 50);
		root.Add(child1);

		var child2 = new Item(50, 50);
		root.Add(child2);

		var child3 = new Item(50, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 70, 0, 50, 50);
		AssertFrame(child2, 70, 50, 50, 50);
		AssertFrame(child3, 20, 0, 50, 50);
	}

	[Fact]
	public void wrap_reverse_orders_every_line_from_the_end() // test_wrap15
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Wrap = Wrap.WrapReverse,
			AlignContent = AlignContent.Start,
		};

		var children = new Item[6];
		for (int i = 0; i < children.Length; i++)
		{
			children[i] = new Item(25, 50);
			root.Add(children[i]);
		}

		//Act
		Layout(root);

		//Assert
		AssertFrame(children[0], 95, 0, 25, 50);
		AssertFrame(children[1], 95, 50, 25, 50);
		AssertFrame(children[2], 70, 0, 25, 50);
		AssertFrame(children[3], 70, 50, 25, 50);
		AssertFrame(children[4], 45, 0, 25, 50);
		AssertFrame(children[5], 45, 50, 25, 50);
	}

	[Fact]
	public void align_content_stretch_spaces_two_lines() // test_wrap16
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Direction = Direction.Column,
			Wrap = Wrap.Wrap,
			AlignContent = AlignContent.Stretch,
		};

		var child1 = new Item(20, 50);
		root.Add(child1);

		var child2 = new Item(20, 50);
		root.Add(child2);

		var child3 = new Item(20, 50);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 20, 50);
		AssertFrame(child2, 0, 50, 20, 50);
		AssertFrame(child3, 60, 0, 20, 50);
	}

	[Fact]
	public void align_content_stretch_spaces_three_lines() // test_wrap17
	{
		//Arrange
		var root = new Item(120, 120)
		{
			Direction = Direction.Column,
			Wrap = Wrap.Wrap,
			AlignContent = AlignContent.Stretch,
		};

		var children = new Item[5];
		for (int i = 0; i < children.Length; i++)
		{
			children[i] = new Item(20, 50);
			root.Add(children[i]);
		}

		//Act
		Layout(root);

		//Assert
		AssertFrame(children[0], 0, 0, 20, 50);
		AssertFrame(children[1], 0, 50, 20, 50);
		AssertFrame(children[2], 40, 0, 20, 50);
		AssertFrame(children[3], 40, 50, 20, 50);
		AssertFrame(children[4], 80, 0, 20, 50);
	}

	[Fact]
	public void unsized_lines_split_the_cross_axis_evenly() // test_wrap18
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
			AlignContent = AlignContent.Stretch,
		};

		var children = new Item[5];
		for (int i = 0; i < children.Length; i++)
		{
			children[i] = new Item { Width = 50 };
			root.Add(children[i]);
		}

		//Act
		Layout(root);

		//Assert
		AssertFrame(children[0], 0, 0, 50, 50);
		AssertFrame(children[1], 50, 0, 50, 50);
		AssertFrame(children[2], 100, 0, 50, 50);
		AssertFrame(children[3], 0, 50, 50, 50);
		AssertFrame(children[4], 50, 50, 50, 50);
	}

	[Fact]
	public void stretch_spacing_adds_to_a_sized_line() // test_wrap19
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
			AlignContent = AlignContent.Stretch,
		};

		var child1 = new Item { Width = 50, Height = 10 };
		root.Add(child1);

		var child2 = new Item { Width = 50 };
		root.Add(child2);

		var child3 = new Item { Width = 50 };
		root.Add(child3);

		var child4 = new Item { Width = 50 };
		root.Add(child4);

		var child5 = new Item { Width = 50 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 10);
		AssertFrame(child2, 50, 0, 50, 55);
		AssertFrame(child3, 100, 0, 50, 55);
		AssertFrame(child4, 0, 55, 50, 45);
		AssertFrame(child5, 50, 55, 50, 45);
	}

	[Fact]
	public void sized_children_keep_their_cross_size_under_stretch() // test_wrap20
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
			AlignContent = AlignContent.Stretch,
		};

		var child1 = new Item { Width = 50 };
		root.Add(child1);

		var child2 = new Item { Width = 50, Height = 5 };
		root.Add(child2);

		var child3 = new Item { Width = 50 };
		root.Add(child3);

		var child4 = new Item { Width = 50 };
		root.Add(child4);

		var child5 = new Item { Width = 50, Height = 5 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 50);
		AssertFrame(child2, 50, 0, 50, 5);
		AssertFrame(child3, 100, 0, 50, 50);
		AssertFrame(child4, 0, 50, 50, 50);
		AssertFrame(child5, 50, 50, 50, 5);
	}

	[Fact]
	public void align_content_space_between_pushes_lines_apart() // test_wrap21
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
			AlignContent = AlignContent.SpaceBetween,
		};

		var child1 = new Item { Width = 50, Height = 20 };
		root.Add(child1);

		var child2 = new Item { Width = 50, Grow = 1 };
		root.Add(child2);

		var child3 = new Item { Width = 50, Height = 20 };
		root.Add(child3);

		var child4 = new Item { Width = 50, Grow = 1 };
		root.Add(child4);

		var child5 = new Item { Width = 50, Height = 20 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 20);
		AssertFrame(child2, 50, 0, 50, 20);
		AssertFrame(child3, 100, 0, 50, 20);
		AssertFrame(child4, 0, 80, 100, 20);
		AssertFrame(child5, 100, 80, 50, 20);
	}

	[Fact]
	public void align_content_space_evenly_spaces_lines_and_edges() // test_wrap22
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
			AlignContent = AlignContent.SpaceEvenly,
		};

		var child1 = new Item { Width = 50, Height = 20 };
		root.Add(child1);

		var child2 = new Item { Width = 50, Grow = 1 };
		root.Add(child2);

		var child3 = new Item { Width = 50, Height = 20 };
		root.Add(child3);

		var child4 = new Item { Width = 50, Grow = 1 };
		root.Add(child4);

		var child5 = new Item { Width = 50, Height = 20 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 20, 50, 20);
		AssertFrame(child2, 50, 20, 50, 20);
		AssertFrame(child3, 100, 20, 50, 20);
		AssertFrame(child4, 0, 60, 100, 20);
		AssertFrame(child5, 100, 60, 50, 20);
	}

	[Fact]
	public void align_content_space_around_gives_lines_equal_margins() // test_wrap23
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
			AlignContent = AlignContent.SpaceAround,
		};

		var child1 = new Item { Width = 50, Height = 20 };
		root.Add(child1);

		var child2 = new Item { Width = 50, Grow = 1 };
		root.Add(child2);

		var child3 = new Item { Width = 50, Height = 20 };
		root.Add(child3);

		var child4 = new Item { Width = 50, Grow = 1 };
		root.Add(child4);

		var child5 = new Item { Width = 50, Height = 20 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 15, 50, 20);
		AssertFrame(child2, 50, 15, 50, 20);
		AssertFrame(child3, 100, 15, 50, 20);
		AssertFrame(child4, 0, 65, 100, 20);
		AssertFrame(child5, 100, 65, 50, 20);
	}

	[Fact]
	public void a_line_is_as_tall_as_its_tallest_child() // test_wrap24
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
			AlignContent = AlignContent.Start,
		};

		var child1 = new Item(50, 10);
		root.Add(child1);

		var child2 = new Item(50, 20);
		root.Add(child2);

		var child3 = new Item(50, 30);
		root.Add(child3);

		var child4 = new Item(50, 40);
		root.Add(child4);

		var child5 = new Item { Width = 50 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 10);
		AssertFrame(child2, 50, 0, 50, 20);
		AssertFrame(child3, 100, 0, 50, 30);
		AssertFrame(child4, 0, 30, 50, 40);
		AssertFrame(child5, 50, 30, 50, 40);
	}

	[Fact]
	public void zero_basis_children_take_no_main_axis_space() // test_wrap25
	{
		//Arrange
		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
		};

		var child1 = new Item { Width = 50 };
		root.Add(child1);

		var child2 = new Item { Width = 50, Basis = new Basis(0) };
		root.Add(child2);

		var child3 = new Item { Width = 50 };
		root.Add(child3);

		var child4 = new Item { Width = 50, Basis = new Basis(0) };
		root.Add(child4);

		var child5 = new Item { Width = 50 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 100);
		AssertFrame(child2, 50, 0, 0, 100);
		AssertFrame(child3, 50, 0, 50, 100);
		AssertFrame(child4, 100, 0, 0, 100);
		AssertFrame(child5, 100, 0, 50, 100);
	}

	[Fact]
	public void stretched_children_ignore_self_sizing_cross_results_in_wrap() // test_wrap26
	{
		//Arrange
		static void SelfSizing26(Item item, ref float width, ref float height, bool inMeasureMode)
		{
			// Do not set any width.
			height = 20;
		}

		var root = new Item(150, 100)
		{
			Wrap = Wrap.Wrap,
			Direction = Direction.Row,
		};

		var child1 = new Item { Width = 50, SelfSizing = SelfSizing26 };
		root.Add(child1);

		var child2 = new Item { Width = 50, Basis = new Basis(0), SelfSizing = SelfSizing26 };
		root.Add(child2);

		var child3 = new Item { Width = 50, SelfSizing = SelfSizing26 };
		root.Add(child3);

		var child4 = new Item { Width = 50, Basis = new Basis(0), SelfSizing = SelfSizing26 };
		root.Add(child4);

		var child5 = new Item { Width = 50, SelfSizing = SelfSizing26 };
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 100);
		AssertFrame(child2, 50, 0, 0, 100);
		AssertFrame(child3, 50, 0, 50, 100);
		AssertFrame(child4, 100, 0, 0, 100);
		AssertFrame(child5, 100, 0, 50, 100);
	}
}
