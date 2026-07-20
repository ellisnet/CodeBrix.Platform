#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_default_values.c (MIT, Microsoft Corporation).
/// </summary>
public class DefaultValuesTests
{
	[Fact]
	public void new_item_has_engine_defaults() // test_default_values1
	{
		//Arrange + Act
		var item = new Item();

		//Assert
		Assert.True(float.IsNaN(item.Width));
		Assert.True(float.IsNaN(item.Height));
		Assert.Null(item.SelfSizing);

		Assert.True(float.IsNaN(item.Left));
		Assert.True(float.IsNaN(item.Right));
		Assert.True(float.IsNaN(item.Top));
		Assert.True(float.IsNaN(item.Bottom));

		Assert.Equal(0f, item.PaddingLeft);
		Assert.Equal(0f, item.PaddingRight);
		Assert.Equal(0f, item.PaddingTop);
		Assert.Equal(0f, item.PaddingBottom);

		Assert.Equal(0f, item.MarginLeft);
		Assert.Equal(0f, item.MarginRight);
		Assert.Equal(0f, item.MarginTop);
		Assert.Equal(0f, item.MarginBottom);

		Assert.Equal(Justify.Start, item.JustifyContent);
		Assert.Equal(AlignContent.Stretch, item.AlignContent);
		Assert.Equal(AlignItems.Stretch, item.AlignItems);
		Assert.Equal(AlignSelf.Auto, item.AlignSelf);

		Assert.Equal(Position.Relative, item.Position);
		Assert.Equal(Direction.Column, item.Direction);
		Assert.Equal(Wrap.NoWrap, item.Wrap);

		Assert.Equal(0f, item.Grow);
		Assert.Equal(1f, item.Shrink);
		Assert.Equal(0, item.Order);
		// The C engine models an auto basis as NaN; the .NET engine models it as Basis.Auto.
		Assert.True(item.Basis.IsAuto);
	}

	[Fact]
	public void unsized_children_default_to_zero_main_axis_and_parent_cross_axis_in_column() // test_default_values2
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.Column };

		var child1 = new Item { Width = 100 };
		root.Add(child1);

		var child2 = new Item { Height = 100 };
		root.Add(child2);

		var child3 = new Item();
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		Assert.Equal(100f, child1.Frame[2]);
		Assert.Equal(0f, child1.Frame[3]);

		Assert.Equal(200f, child2.Frame[2]);
		Assert.Equal(100f, child2.Frame[3]);

		Assert.Equal(200f, child3.Frame[2]);
		Assert.Equal(0f, child3.Frame[3]);
	}

	[Fact]
	public void unsized_children_default_to_zero_main_axis_and_parent_cross_axis_in_row() // test_default_values3
	{
		//Arrange
		var root = new Item(200, 200) { Direction = Direction.Row };

		var child1 = new Item { Width = 100 };
		root.Add(child1);

		var child2 = new Item { Height = 100 };
		root.Add(child2);

		var child3 = new Item();
		root.Add(child3);

		//Act
		Layout(root);

		//Assert
		Assert.Equal(100f, child1.Frame[2]);
		Assert.Equal(200f, child1.Frame[3]);

		Assert.Equal(0f, child2.Frame[2]);
		Assert.Equal(100f, child2.Frame[3]);

		Assert.Equal(0f, child3.Frame[2]);
		Assert.Equal(200f, child3.Frame[3]);
	}
}
