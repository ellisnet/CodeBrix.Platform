#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_grow.c (MIT, Microsoft Corporation).
/// </summary>
/// <remarks>
/// The original C engine IGNORES a growing child's own main-axis size (it resets the size to zero
/// and distributes container space inflated by those sizes). The .NET engine this add-in ports
/// (dotnet/maui) deliberately PRESERVES the measured size and distributes only the true free
/// space on top - the CSS "flex: 1 1 auto" interpretation. The two behaviors coincide whenever
/// growing children have equal grow factors and equal sizes (or zero sizes); grow7 and grow8
/// below are the cases where they differ, and their expectations are adapted to the .NET engine
/// with the C expectations noted inline.
/// </remarks>
public class GrowTests
{
	[Fact]
	public void free_space_is_distributed_proportionally_to_grow_factors() // test_grow1
	{
		//Arrange
		var root = new Item(60, 240);

		var child1 = new Item(60, 30) { Grow = 0 };
		root.Add(child1);

		var child2 = new Item(60, 0) { Grow = 1 };
		root.Add(child2);

		var child3 = new Item(60, 0) { Grow = 2 };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 60, 30);
		AssertFrame(child2, 0, 30, 60, 70);
		AssertFrame(child3, 0, 100, 60, 140);
	}

	[Fact]
	public void only_growing_children_take_the_free_space() // test_grow2
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 20) { Grow = 1 };
		root.Add(child1);

		var child2 = new Item(100, 20) { Grow = 0 };
		root.Add(child2);

		var child3 = new Item(100, 20);
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 60);
		AssertFrame(child2, 0, 60, 100, 20);
		AssertFrame(child3, 0, 80, 100, 20);
	}

	[Fact]
	public void grow_is_ignored_when_there_is_no_free_space() // test_grow3
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 50) { Grow = 2 };
		root.Add(child1);

		var child2 = new Item(100, 50) { Grow = 3 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 50);
		AssertFrame(child2, 0, 50, 100, 50);
	}

	[Fact]
	public void grow_is_not_inherited_from_the_parent() // test_grow4
	{
		//Arrange
		var root = new Item(100, 100) { Grow = 2 };

		var child1 = new Item(100, 25);
		root.Add(child1);

		var child2 = new Item(100, 25);
		root.Add(child2);

		//Assert (defaults)
		Assert.Equal(0f, child1.Grow);
		Assert.Equal(0f, child2.Grow);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 25);
		AssertFrame(child2, 0, 25, 100, 25);
	}

	[Fact]
	public void a_single_growing_child_takes_all_the_container_space() // test_grow5
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 25) { Grow = 1 };
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 100);
	}

	[Fact]
	public void equal_grow_factors_split_the_free_space_equally() // test_grow6
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 45) { Grow = 1 };
		root.Add(child1);

		var child2 = new Item(100, 45) { Grow = 1 };
		root.Add(child2);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 0, 0, 100, 50);
		AssertFrame(child2, 0, 50, 100, 50);
	}

	[Fact]
	public void growing_children_keep_their_measured_size_and_share_only_free_space() // test_grow7
	{
		//Arrange
		var root = new Item(500, 600);

		var child1 = new Item(250, 0) { Grow = 1 };
		root.Add(child1);

		var child2 = new Item(250, 50) { Grow = 1 };
		root.Add(child2);

		var child3 = new Item(250, 0);
		root.Add(child3);

		var child4 = new Item(250, 0) { Grow = 1 };
		root.Add(child4);

		var child5 = new Item(250, 0);
		root.Add(child5);

		//Act
		Layout(root);

		//Assert
		// Free space is 600 - 50 = 550, split across three grow factors. The C engine instead
		// ignored child2's size and gave every growing child 200 (positions 0/200/400/400/600).
		var share = 550f / 3f;
		AssertFrame(child1, 0, 0, 250, share);
		AssertFrame(child2, 0, share, 250, 50f + share);
		AssertFrame(child3, 0, share + (50f + share), 250, 0);
		AssertFrame(child4, 0, share + (50f + share), 250, share);
		AssertFrame(child5, 0, share + (50f + share) + share, 250, 0);
	}

	[Fact]
	public void grow_factors_can_be_floating_point() // test_grow8
	{
		//Arrange
		var root = new Item(100, 100);

		var child1 = new Item(100, 10);
		root.Add(child1);

		var child2 = new Item(100, 20) { Grow = 1 };
		root.Add(child2);

		var child3 = new Item(100, 20) { Grow = 1.5f };
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		// Free space is 100 - 50 = 50 across 2.5 grow units: child2 gets 20 + 20, child3 gets
		// 20 + 30. The C engine instead ignored the growing children's sizes and distributed an
		// inflated 90 units (child2 36, child3 54).
		AssertFrame(child1, 0, 0, 100, 10);
		AssertFrame(child2, 0, 10, 100, 40);
		AssertFrame(child3, 0, 50, 100, 50);
	}
}
