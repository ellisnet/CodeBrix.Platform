#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;
using static CodeBrix.Platform.UI.FlexPanel.Tests.FlexTestHelpers;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Ported from xamarin/flex tests/test_padding.c (MIT, Microsoft Corporation). These drive the
/// ENGINE's padding support (padding set directly on the root item); the FlexPanel control itself
/// applies its Padding property outside the engine, the same way the .NET MAUI FlexLayout does.
/// </summary>
public class PaddingTests
{
	private static Item MakePaddedRoot(Justify justifyContent, AlignItems alignItems)
		=> new Item(100, 100)
		{
			Direction = Direction.Column,
			JustifyContent = justifyContent,
			AlignItems = alignItems,
			PaddingTop = 15,
			PaddingLeft = 10,
			PaddingRight = 15,
			PaddingBottom = 10,
		};

	[Fact]
	public void padding_offsets_a_start_aligned_child() // test_padding1
	{
		//Arrange
		var root = MakePaddedRoot(Justify.Start, AlignItems.Start);

		var child1 = new Item(25, 25);
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 15, 25, 25);
	}

	[Fact]
	public void padding_is_respected_by_end_justification() // test_padding2
	{
		//Arrange
		var root = MakePaddedRoot(Justify.End, AlignItems.Start);

		var child1 = new Item(25, 25);
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 65, 25, 25);
	}

	[Fact]
	public void padding_is_respected_by_end_alignment_and_end_justification() // test_padding3
	{
		//Arrange
		var root = MakePaddedRoot(Justify.End, AlignItems.End);

		var child1 = new Item(25, 25);
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 60, 65, 25, 25);
	}

	[Fact]
	public void padding_is_respected_by_end_alignment() // test_padding4
	{
		//Arrange
		var root = MakePaddedRoot(Justify.Start, AlignItems.End);

		var child1 = new Item(25, 25);
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 60, 15, 25, 25);
	}

	[Fact]
	public void a_stretched_child_fills_the_padded_area_only() // test_padding5
	{
		//Arrange
		var root = MakePaddedRoot(Justify.Start, AlignItems.Start);

		var child1 = new Item(0, 25) { AlignSelf = AlignSelf.Stretch };
		root.Add(child1);

		//Act
		Layout(root);

		//Assert
		AssertFrame(child1, 10, 15, 75, 25);
	}
}
