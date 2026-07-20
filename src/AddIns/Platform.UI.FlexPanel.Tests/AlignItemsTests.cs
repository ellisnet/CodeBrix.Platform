#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_align_items.c (MIT, Microsoft Corporation).
/// </summary>
public class AlignItemsTests
{
	[Fact]
	public void start_packs_children_at_the_cross_axis_start() // test_align_items1
	{
		//Arrange
		var root = new Item(100, 100) { AlignItems = AlignItems.Start };

		var child1 = new Item(50, 25);
		root.Add(child1);

		var child2 = new Item(50, 25);
		root.Add(child2);

		var child3 = new Item(50, 25);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 25);
		AssertFrame(child2, 0, 25, 50, 25);
		AssertFrame(child3, 0, 50, 50, 25);
	}

	[Fact]
	public void end_packs_children_at_the_cross_axis_end() // test_align_items2
	{
		//Arrange
		var root = new Item(100, 100) { AlignItems = AlignItems.End };

		var child1 = new Item(50, 25);
		root.Add(child1);

		var child2 = new Item(50, 25);
		root.Add(child2);

		var child3 = new Item(50, 25);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 50, 0, 50, 25);
		AssertFrame(child2, 50, 25, 50, 25);
		AssertFrame(child3, 50, 50, 50, 25);
	}

	[Fact]
	public void center_centers_children_on_the_cross_axis() // test_align_items3
	{
		//Arrange
		var root = new Item(100, 100) { AlignItems = AlignItems.Center };

		var child1 = new Item(50, 25);
		root.Add(child1);

		var child2 = new Item(50, 25);
		root.Add(child2);

		var child3 = new Item(50, 25);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 25, 0, 50, 25);
		AssertFrame(child2, 25, 25, 50, 25);
		AssertFrame(child3, 25, 50, 50, 25);
	}

	[Fact]
	public void stretch_expands_children_without_a_cross_axis_size() // test_align_items4
	{
		//Arrange
		var root = new Item(100, 100) { AlignItems = AlignItems.Stretch };

		var child1 = new Item(50, 25);
		root.Add(child1);

		var child2 = new Item(0, 25);
		root.Add(child2);

		var child3 = new Item { Height = 25 };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 25);
		AssertFrame(child2, 0, 25, 100, 25);
		AssertFrame(child3, 0, 50, 100, 25);
	}

	[Fact]
	public void align_self_overrides_the_parent_align_items() // test_align_items5
	{
		//Arrange
		var root = new Item(100, 100) { AlignItems = AlignItems.Center };

		var child1 = new Item(50, 25);
		root.Add(child1);

		var child2 = new Item(50, 25) { AlignSelf = AlignSelf.Start };
		root.Add(child2);

		var child3 = new Item(50, 25) { AlignSelf = AlignSelf.Auto };
		root.Add(child3);

		var child4 = new Item(50, 25) { AlignSelf = AlignSelf.End };
		root.Add(child4);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 25, 0, 50, 25);
		AssertFrame(child2, 0, 25, 50, 25);
		AssertFrame(child3, 25, 50, 50, 25);
		AssertFrame(child4, 50, 75, 50, 25);
	}
}
