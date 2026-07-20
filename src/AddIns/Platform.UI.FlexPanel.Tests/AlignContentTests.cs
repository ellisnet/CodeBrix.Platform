#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_align_content.c (MIT, Microsoft Corporation).
/// Column direction, so wrapped "lines" are columns and the cross axis is horizontal.
/// </summary>
public class AlignContentTests
{
	private static Item MakeWrappingRoot(float width, AlignContent alignContent)
	{
		var root = new Item(width, 120)
		{
			Wrap = Wrap.Wrap,
			AlignContent = alignContent,
		};

		root.Add(new Item(50, 50));
		root.Add(new Item(60, 50));
		root.Add(new Item(40, 50));

		return root;
	}

	[Fact]
	public void start_packs_lines_at_the_cross_axis_start() // test_align_content1
	{
		//Arrange
		var root = MakeWrappingRoot(200, AlignContent.Start);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 0, 0, 50, 50);
		AssertFrame(root[1], 0, 50, 60, 50);
		AssertFrame(root[2], 60, 0, 40, 50);
	}

	[Fact]
	public void center_packs_lines_around_the_cross_axis_center() // test_align_content2
	{
		//Arrange
		var root = MakeWrappingRoot(200, AlignContent.Center);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 50, 0, 50, 50);
		AssertFrame(root[1], 50, 50, 60, 50);
		AssertFrame(root[2], 110, 0, 40, 50);
	}

	[Fact]
	public void end_packs_lines_at_the_cross_axis_end() // test_align_content3
	{
		//Arrange
		var root = MakeWrappingRoot(200, AlignContent.End);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 100, 0, 50, 50);
		AssertFrame(root[1], 100, 50, 60, 50);
		AssertFrame(root[2], 160, 0, 40, 50);
	}

	[Fact]
	public void space_between_pushes_the_first_and_last_lines_to_the_edges() // test_align_content4
	{
		//Arrange
		var root = MakeWrappingRoot(200, AlignContent.SpaceBetween);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 0, 0, 50, 50);
		AssertFrame(root[1], 0, 50, 60, 50);
		AssertFrame(root[2], 160, 0, 40, 50);
	}

	[Fact]
	public void space_around_gives_every_line_equal_surrounding_space() // test_align_content5
	{
		//Arrange
		var root = MakeWrappingRoot(200, AlignContent.SpaceAround);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 25, 0, 50, 50);
		AssertFrame(root[1], 25, 50, 60, 50);
		AssertFrame(root[2], 135, 0, 40, 50);
	}

	[Fact]
	public void space_evenly_gives_equal_space_between_all_lines_and_edges() // test_align_content6
	{
		//Arrange
		var root = MakeWrappingRoot(250, AlignContent.SpaceEvenly);

		//Act
		Layout(root);

		//Assert
		AssertFrame(root[0], 50, 0, 50, 50);
		AssertFrame(root[1], 50, 50, 60, 50);
		AssertFrame(root[2], 160, 0, 40, 50);
	}
}
