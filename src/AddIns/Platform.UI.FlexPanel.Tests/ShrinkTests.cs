#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_shrink.c (MIT, Microsoft Corporation).
/// </summary>
public class ShrinkTests
{
	[Fact]
	public void overflow_is_reclaimed_proportionally_to_shrink_factors() // test_shrink1
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 100) { Shrink = 2 };
		root.Add(child1);

		var child2 = new Item(100, 100) { Shrink = 3 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 60);
		AssertFrame(child2, 0, 60, 100, 40);
	}

	[Fact]
	public void default_shrink_of_one_participates_in_shrinking() // test_shrink2
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 100);
		root.Add(child1);

		var child2 = new Item(100, 100) { Shrink = 4 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 80);
		AssertFrame(child2, 0, 80, 100, 20);
	}

	[Fact]
	public void shrink_is_ignored_when_children_fit() // test_shrink3
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 40) { Shrink = 2 };
		root.Add(child1);

		var child2 = new Item(100, 40) { Shrink = 3 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 40);
		AssertFrame(child2, 0, 40, 100, 40);
	}

	[Fact]
	public void shrink_is_not_inherited_from_the_parent() // test_shrink4
	{
		//Arrange
		var root = new Item(100, 100) { Shrink = 2 };

		var child1 = new Item(100, 25);
		root.Add(child1);

		var child2 = new Item(100, 25);
		root.Add(child2);

		//Assert (defaults)
		Assert.Equal(1f, child1.Shrink);
		Assert.Equal(1f, child2.Shrink);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 25);
		AssertFrame(child2, 0, 25, 100, 25);
	}

	[Fact]
	public void a_single_shrinking_child_is_clamped_to_the_container() // test_shrink5
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 550) { Shrink = 1 };
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 100);
	}

	[Fact]
	public void equal_shrink_factors_split_the_overflow_equally() // test_shrink6
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 75) { Shrink = 1 };
		root.Add(child1);

		var child2 = new Item(100, 75) { Shrink = 1 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 50);
		AssertFrame(child2, 0, 50, 100, 50);
	}

	[Fact]
	public void shrink_factors_can_be_floating_point() // test_shrink7
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 75) { Shrink = 1 };
		root.Add(child1);

		var child2 = new Item(100, 75) { Shrink = 1.5f };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 55);
		AssertFrame(child2, 0, 55, 100, 45);
	}
}
