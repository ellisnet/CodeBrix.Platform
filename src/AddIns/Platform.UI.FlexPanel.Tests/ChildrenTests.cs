#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_children.c (MIT, Microsoft Corporation).
/// </summary>
public class ChildrenTests
{
	[Fact]
	public void add_remove_and_reinsert_maintain_count_parent_and_positions() // test_children1
	{
		//Arrange
		var root = new Item();

		//Assert (initial)
		Assert.Empty(root);
		Assert.Null(root.Parent);

		//Act + Assert (add first child)
		var child1 = new Item();
		root.Add(child1);

		Assert.Single(root);
		Assert.Same(root, child1.Parent);
		Assert.Same(child1, root[0]);

		//Act + Assert (add second child)
		var child2 = new Item();
		root.Add(child2);

		Assert.Equal(2, root.Count);
		Assert.Same(root, child2.Parent);
		Assert.Same(child2, root[1]);

		//Act + Assert (remove first child)
		Assert.Same(child1, root.RemoveAt(0u));

		Assert.Single(root);
		Assert.Null(child1.Parent);
		Assert.Same(child2, root[0]);

		//Act + Assert (re-insert at the front)
		root.InsertAt(0, child1);

		Assert.Equal(2, root.Count);
		Assert.Same(root, child1.Parent);
		Assert.Same(child1, root[0]);
		Assert.Same(child2, root[1]);
	}

	[Fact]
	public void insert_places_children_at_the_given_index() // test_children2
	{
		//Arrange
		var root = new Item();

		//Act + Assert
		var child1 = new Item();
		root.InsertAt(0, child1);

		Assert.Single(root);
		Assert.Same(child1, root[0]);

		var child2 = new Item();
		root.InsertAt(0, child2);

		Assert.Equal(2, root.Count);
		Assert.Same(child2, root[0]);
		Assert.Same(child1, root[1]);

		var child3 = new Item();
		root.InsertAt(1, child3);

		Assert.Equal(3, root.Count);
		Assert.Same(child2, root[0]);
		Assert.Same(child3, root[1]);
		Assert.Same(child1, root[2]);
	}

	[Fact]
	public void root_walks_up_to_the_topmost_parent() // test_children3
	{
		//Arrange
		var root = new Item();

		Assert.Same(root, root.Root);

		var child1 = new Item();
		root.Add(child1);

		Assert.Same(root, child1.Root);

		var child2 = new Item();
		child1.Add(child2);

		Assert.Same(root, child2.Root);

		//Act
		root.RemoveAt(0u);

		//Assert
		Assert.Same(child1, child1.Root);
		Assert.Same(child1, child2.Root);
	}

	[Fact]
	public void nested_centered_children_center_at_every_level() // test_children4
	{
		//Arrange
		static Item CenterItem(float width, float height) => new Item(width, height)
		{
			AlignItems = AlignItems.Center,
			JustifyContent = Justify.Center,
		};

		var root = CenterItem(100, 100);

		var child1 = CenterItem(90, 90);
		root.Add(child1);

		var child2 = CenterItem(80, 80);
		child1.Add(child2);

		var child3 = CenterItem(70, 70);
		child2.Add(child3);

		var child4 = CenterItem(60, 60);
		child3.Add(child4);

		var child5 = CenterItem(50, 50);
		child4.Add(child5);

		//Act
		Layout(root);

		//Assert
		Assert.Same(child1, root[0]);
		Assert.Same(child2, child1[0]);
		Assert.Same(child3, child2[0]);
		Assert.Same(child4, child3[0]);
		Assert.Same(child5, child4[0]);

		AssertFrame(child1, 5, 5, 90, 90);
		AssertFrame(child2, 5, 5, 80, 80);
		AssertFrame(child3, 5, 5, 70, 70);
		AssertFrame(child4, 5, 5, 60, 60);
		AssertFrame(child5, 5, 5, 50, 50);
	}

	[Fact]
	public void layout_follows_child_list_mutations_with_self_sizing_and_grow() // test_children5
	{
		//Arrange
		static void SelfSizing5(Item item, ref float width, ref float height, bool inMeasureMode)
		{
			width = 100;
			height = 20;
		}

		var root = new Item(300, 50)
		{
			Direction = Direction.Row,
			AlignItems = AlignItems.Start,
		};

		var child1 = new Item { Grow = 1, SelfSizing = SelfSizing5 };
		root.Add(child1);

		var child2 = new Item { Grow = 1, SelfSizing = SelfSizing5 };
		root.Add(child2);

		var child3 = new Item { Grow = 1, SelfSizing = SelfSizing5 };
		root.Add(child3);

		//Act + Assert (three children fill the row exactly)
		Layout(root);

		AssertFrame(child1, 0, 0, 100, 20);
		AssertFrame(child2, 100, 0, 100, 20);
		AssertFrame(child3, 200, 0, 100, 20);

		//Act + Assert (removing one redistributes the freed space)
		root.RemoveAt(2u);

		Layout(root);

		Assert.Equal(2, root.Count);
		Assert.Null(child3.Parent);

		AssertFrame(child1, 0, 0, 150, 20);
		AssertFrame(child2, 150, 0, 150, 20);

		//Act + Assert (inserting at the front restores three equal shares)
		var child4 = new Item { Grow = 1, SelfSizing = SelfSizing5 };

		root.InsertAt(0, child4);

		Layout(root);

		Assert.Equal(3, root.Count);
		Assert.Same(child4, root[0]);
		Assert.Same(child1, root[1]);
		Assert.Same(child2, root[2]);

		AssertFrame(child4, 0, 0, 100, 20);
		AssertFrame(child1, 100, 0, 100, 20);
		AssertFrame(child2, 200, 0, 100, 20);
	}

	[Fact]
	public void insert_and_delete_keep_sibling_order_intact() // test_children6
	{
		//Arrange
		var root = new Item();

		var child1 = new Item();
		root.Add(child1);

		var child2 = new Item();
		root.Add(child2);

		//Act + Assert
		var child3 = new Item();
		root.InsertAt(1, child3);

		Assert.Equal(3, root.Count);
		Assert.Same(child1, root[0]);
		Assert.Same(child3, root[1]);
		Assert.Same(child2, root[2]);

		var child4 = new Item();
		root.InsertAt(3, child4);

		Assert.Equal(4, root.Count);
		Assert.Same(child1, root[0]);
		Assert.Same(child3, root[1]);
		Assert.Same(child2, root[2]);
		Assert.Same(child4, root[3]);

		var child5 = new Item();
		root.InsertAt(0, child5);

		Assert.Equal(5, root.Count);
		Assert.Same(child5, root[0]);
		Assert.Same(child1, root[1]);
		Assert.Same(child3, root[2]);
		Assert.Same(child2, root[3]);
		Assert.Same(child4, root[4]);

		root.RemoveAt(2u);

		Assert.Equal(4, root.Count);
		Assert.Same(child5, root[0]);
		Assert.Same(child1, root[1]);
		Assert.Same(child2, root[2]);
		Assert.Same(child4, root[3]);

		root.RemoveAt(3u);

		Assert.Equal(3, root.Count);
		Assert.Same(child5, root[0]);
		Assert.Same(child1, root[1]);
		Assert.Same(child2, root[2]);

		root.RemoveAt(0u);

		Assert.Equal(2, root.Count);
		Assert.Same(child1, root[0]);
		Assert.Same(child2, root[1]);
	}
}
