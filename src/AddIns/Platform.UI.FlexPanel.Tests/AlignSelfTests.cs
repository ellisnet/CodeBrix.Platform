#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_align_self.c (MIT, Microsoft Corporation).
/// </summary>
public class AlignSelfTests
{
	[Fact]
	public void start_packs_children_at_the_cross_axis_start() // test_align_self1
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(50, 25) { AlignSelf = AlignSelf.Start };
		root.Add(child1);

		var child2 = new Item(50, 25) { AlignSelf = AlignSelf.Start };
		root.Add(child2);

		var child3 = new Item(50, 25) { AlignSelf = AlignSelf.Start };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 25);
		AssertFrame(child2, 0, 25, 50, 25);
		AssertFrame(child3, 0, 50, 50, 25);
	}

	[Fact]
	public void end_packs_children_at_the_cross_axis_end() // test_align_self2
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(50, 25) { AlignSelf = AlignSelf.End };
		root.Add(child1);

		var child2 = new Item(50, 25) { AlignSelf = AlignSelf.End };
		root.Add(child2);

		var child3 = new Item(50, 25) { AlignSelf = AlignSelf.End };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 50, 0, 50, 25);
		AssertFrame(child2, 50, 25, 50, 25);
		AssertFrame(child3, 50, 50, 50, 25);
	}

	[Fact]
	public void center_centers_children_on_the_cross_axis() // test_align_self3
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(50, 25) { AlignSelf = AlignSelf.Center };
		root.Add(child1);

		var child2 = new Item(50, 25) { AlignSelf = AlignSelf.Center };
		root.Add(child2);

		var child3 = new Item(50, 25) { AlignSelf = AlignSelf.Center };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 25, 0, 50, 25);
		AssertFrame(child2, 25, 25, 50, 25);
		AssertFrame(child3, 25, 50, 50, 25);
	}

	[Fact]
	public void stretch_applies_when_the_cross_axis_size_is_unset_or_zero() // test_align_self4
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item { Height = 25, AlignSelf = AlignSelf.Stretch };
		root.Add(child1);

		var child2 = new Item(0, 25) { AlignSelf = AlignSelf.Stretch };
		root.Add(child2);

		var child3 = new Item { Height = 25, AlignSelf = AlignSelf.Stretch };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 25);
		AssertFrame(child2, 0, 25, 100, 25);
		AssertFrame(child3, 0, 50, 100, 25);
	}

	[Fact]
	public void stretch_does_not_apply_when_the_cross_axis_size_is_set() // test_align_self5
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(50, 25) { AlignSelf = AlignSelf.Stretch };
		root.Add(child1);

		var child2 = new Item(50, 50) { AlignSelf = AlignSelf.Stretch };
		root.Add(child2);

		var child3 = new Item(50, 25) { AlignSelf = AlignSelf.Stretch };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 25);
		AssertFrame(child2, 0, 25, 50, 50);
		AssertFrame(child3, 0, 75, 50, 25);
	}

	[Fact]
	public void mixed_align_self_values_apply_per_child() // test_align_self6
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(50, 25) { AlignSelf = AlignSelf.Start };
		root.Add(child1);

		var child2 = new Item(50, 25) { AlignSelf = AlignSelf.Center };
		root.Add(child2);

		var child3 = new Item(0, 25) { AlignSelf = AlignSelf.Stretch };
		root.Add(child3);

		var child4 = new Item(50, 25) { AlignSelf = AlignSelf.End };
		root.Add(child4);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 50, 25);
		AssertFrame(child2, 25, 25, 50, 25);
		AssertFrame(child3, 0, 50, 100, 25);
		AssertFrame(child4, 50, 75, 50, 25);
	}
}
