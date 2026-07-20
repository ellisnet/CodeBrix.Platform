#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_self_sizing.c (MIT, Microsoft Corporation). The C
/// callback signature (item, float size[2]) becomes the engine delegate's (item, ref width,
/// ref height, inMeasureMode) pair.
/// </summary>
public class SelfSizingTests
{
	[Fact]
	public void self_sizing_can_be_set_and_cleared() // test_self_sizing_dummy0
	{
		//Arrange
		var item = new Item();

		Assert.Null(item.SelfSizing);

		//Act + Assert
		static void Dummy(Item it, ref float w, ref float h, bool inMeasureMode)
		{
		}

		Item.SelfSizingDelegate dummy = Dummy;
		item.SelfSizing = dummy;
		Assert.Same(dummy, item.SelfSizing);

		item.SelfSizing = null;
		Assert.Null(item.SelfSizing);
	}

	[Fact]
	public void callback_receives_default_frame_sizes() // test_self_sizing_dummy1
	{
		//Arrange
		var root = new Item(100, 100);

		var called = 0;
		var seen = new float[2];
		var child = new Item();
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			called++;
			seen[0] = w;
			seen[1] = h;
		};
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		Assert.Equal(1, called);
		Assert.Equal(100f, seen[0]);
		Assert.Equal(0f, seen[1]);
	}

	[Fact]
	public void callback_receives_the_explicit_width() // test_self_sizing_dummy2
	{
		//Arrange
		var root = new Item(100, 100);

		var called = 0;
		var seen = new float[2];
		var child = new Item { Width = 50 };
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			called++;
			seen[0] = w;
			seen[1] = h;
		};
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		Assert.Equal(1, called);
		Assert.Equal(50f, seen[0]);
		Assert.Equal(0f, seen[1]);
	}

	[Fact]
	public void callback_receives_the_explicit_height() // test_self_sizing_dummy3
	{
		//Arrange
		var root = new Item(100, 100);

		var called = 0;
		var seen = new float[2];
		var child = new Item { Height = 50 };
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			called++;
			seen[0] = w;
			seen[1] = h;
		};
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		Assert.Equal(1, called);
		Assert.Equal(100f, seen[0]);
		Assert.Equal(50f, seen[1]);
	}

	[Fact]
	public void callback_receives_both_explicit_dimensions() // test_self_sizing_dummy4
	{
		//Arrange
		var root = new Item(100, 100);

		var called = 0;
		var seen = new float[2];
		var child = new Item { Width = 50, Height = 50 };
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			called++;
			seen[0] = w;
			seen[1] = h;
		};
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		Assert.Equal(1, called);
		Assert.Equal(50f, seen[0]);
		Assert.Equal(50f, seen[1]);
	}

	[Fact]
	public void callback_runs_before_grow_is_applied() // test_self_sizing_dummy5
	{
		//Arrange
		var root = new Item(100, 100);

		var called = 0;
		var seen = new float[2];
		var child = new Item { Grow = 1 };
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			called++;
			seen[0] = w;
			seen[1] = h;
		};
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		Assert.Equal(1, called);
		Assert.Equal(100f, seen[0]);
		Assert.Equal(0f, seen[1]);
	}

	[Fact]
	public void callback_runs_once_per_layout_pass() // test_self_sizing_dummy6
	{
		//Arrange
		var root = new Item(100, 100);

		var called = 0;
		var child = new Item();
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			called++;
		};
		root.Add(child);

		//Act + Assert
		for (int i = 0; i < 42; i++)
		{
			Layout(root);
			Assert.Equal(i + 1, called);
		}

		Assert.Equal(42, called);
	}

	[Fact]
	public void nan_results_are_ignored() // test_self_sizing_nan
	{
		//Arrange
		var root = new Item(100, 100);

		var called = false;
		var child = new Item(10, 20);
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			called = true;
			w = float.NaN;
			h = float.NaN;
		};
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		Assert.True(called);
		AssertFrame(child, 0, 0, 10, 20);
	}

	// Simulates text that reflows to taller, narrower blocks as the available width shrinks.
	private static void SimulateWrappingText(Item item, ref float width, ref float height, bool inMeasureMode)
	{
		if (width >= 68)
		{
			width = 68;
			height = 16;
		}
		else if (width >= 50)
		{
			width = 50;
			height = 32;
		}
	}

	[Fact]
	public void wrapping_text_uses_its_natural_size_when_it_fits() // test_self_sizing1
	{
		//Arrange
		var root = new Item(100, 100) { AlignItems = AlignItems.Start };

		var child = new Item { SelfSizing = SimulateWrappingText };
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child, 0, 0, 68, 16);
	}

	[Fact]
	public void wrapping_text_reflows_when_the_container_is_narrow() // test_self_sizing2
	{
		//Arrange
		var root = new Item(55, 100) { AlignItems = AlignItems.Start };

		var child = new Item { SelfSizing = SimulateWrappingText };
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child, 0, 0, 50, 32);
	}

	[Fact]
	public void explicit_dimensions_win_over_the_callback() // test_self_sizing3
	{
		//Arrange
		var root = new Item(100, 100);

		var child = new Item { Width = 10, Height = 10, SelfSizing = SimulateWrappingText };
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child, 0, 0, 10, 10);
	}

	[Fact]
	public void grow_still_applies_to_a_self_sized_child() // test_self_sizing4
	{
		//Arrange
		var root = new Item(100, 100);

		var child = new Item { Width = 10, Height = 10, Grow = 1, SelfSizing = SimulateWrappingText };
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child, 0, 0, 10, 100);
	}

	[Fact]
	public void a_zero_main_axis_size_is_reported_to_a_row_child() // test_self_sizing5
	{
		//Arrange
		var root = new Item(100, 100) { Direction = Direction.Row };

		var child = new Item { SelfSizing = SimulateWrappingText };
		root.Add(child);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child, 0, 0, 0, 100);
	}

	[Fact]
	public void cross_axis_results_are_ignored_for_stretched_column_children() // test_self_sizing6
	{
		//Arrange
		var root = new Item(100, 100) { Direction = Direction.Column };

		var sizeIndex = 0;
		var sizeValue = 0f;
		var child = new Item { Height = 100, AlignSelf = AlignSelf.Stretch };
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			if (sizeIndex == 0)
				w = sizeValue;
			else
				h = sizeValue;
		};
		root.Add(child);

		//Act + Assert
		sizeIndex = 0;
		sizeValue = 10;
		Layout(root);
		AssertFrame(child, 0, 0, 100, 100);

		sizeIndex = 0;
		sizeValue = 0;
		Layout(root);
		AssertFrame(child, 0, 0, 100, 100);

		sizeIndex = 0;
		sizeValue = float.NaN;
		Layout(root);
		AssertFrame(child, 0, 0, 100, 100);

		child.Width = 10;
		Layout(root);
		AssertFrame(child, 0, 0, 10, 100);
	}

	[Fact]
	public void cross_axis_results_are_ignored_for_stretched_row_children() // test_self_sizing7
	{
		//Arrange
		var root = new Item(100, 100) { Direction = Direction.Row };

		var sizeIndex = 0;
		var sizeValue = 0f;
		var child = new Item { Width = 100, AlignSelf = AlignSelf.Stretch };
		child.SelfSizing = (Item it, ref float w, ref float h, bool inMeasureMode) =>
		{
			if (sizeIndex == 0)
				w = sizeValue;
			else
				h = sizeValue;
		};
		root.Add(child);

		Layout(root);

		//Act + Assert
		sizeIndex = 1;
		sizeValue = 10;
		Layout(root);
		AssertFrame(child, 0, 0, 100, 100);

		sizeIndex = 1;
		sizeValue = 0;
		Layout(root);
		AssertFrame(child, 0, 0, 100, 100);

		sizeIndex = 1;
		sizeValue = float.NaN;
		Layout(root);
		AssertFrame(child, 0, 0, 100, 100);

		child.Height = 10;
		Layout(root);
		AssertFrame(child, 0, 0, 100, 10);
	}
}
