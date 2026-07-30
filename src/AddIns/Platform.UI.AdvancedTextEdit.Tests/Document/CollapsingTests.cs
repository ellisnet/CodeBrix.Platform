#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/CollapsingTests.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Exercises collapsed line sections in the height tree: collapse/uncollapse over every range,
/// and document edits inside, at the end of, and covering a collapsed section.
/// </summary>
public class CollapsingTests
{
	readonly TextDocument document;
	readonly HeightTree heightTree;

	public CollapsingTests() // Setup
	{
		document = new TextDocument();
		document.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
		heightTree = new HeightTree(document, 10);
		foreach (DocumentLine line in document.Lines)
		{
			heightTree.SetHeight(line, line.LineNumber);
		}
	}

	CollapsedLineSection SimpleCheck(int from, int to)
	{
		CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(from), document.GetLineByNumber(to));
		for (int i = 1; i < from; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		for (int i = from; i <= to; i++)
		{
			Assert.True(heightTree.GetIsCollapsed(i));
		}
		for (int i = to + 1; i <= 10; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
		return sec1;
	}

	[Fact]
	public void collapsing_a_range_marks_exactly_those_lines_collapsed() // SimpleCheck
	{
		//Arrange (tree from constructor)

		//Act + Assert
		SimpleCheck(4, 6);
	}

	[Fact]
	public void uncollapsing_a_section_restores_all_lines() // SimpleUncollapse
	{
		//Arrange
		CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(4), document.GetLineByNumber(6));

		//Act
		sec1.Uncollapse();

		//Assert
		for (int i = 1; i <= 10; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	[Fact]
	public void every_collapse_uncollapse_range_combination_is_consistent() // FullCheck
	{
		//Arrange (tree from constructor)

		//Act + Assert
		for (int from = 1; from <= 10; from++)
		{
			for (int to = from; to <= 10; to++)
			{
				try
				{
					SimpleCheck(from, to).Uncollapse();
					for (int i = 1; i <= 10; i++)
					{
						Assert.False(heightTree.GetIsCollapsed(i));
					}
					CheckHeights();
				}
				catch
				{
					Console.WriteLine("from = " + from + ", to = " + to);
					throw;
				}
			}
		}
	}

	[Fact]
	public void inserting_lines_inside_a_collapsed_section_extends_it() // InsertInCollapsedSection
	{
		//Arrange
		heightTree.CollapseText(document.GetLineByNumber(4), document.GetLineByNumber(6));

		//Act
		document.Insert(document.GetLineByNumber(5).Offset, "a\nb\nc");

		//Assert
		for (int i = 1; i < 4; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		for (int i = 4; i <= 8; i++)
		{
			Assert.True(heightTree.GetIsCollapsed(i));
		}
		for (int i = 9; i <= 12; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	[Fact]
	public void removing_lines_inside_a_collapsed_section_shrinks_it() // RemoveInCollapsedSection
	{
		//Arrange
		heightTree.CollapseText(document.GetLineByNumber(3), document.GetLineByNumber(7));
		int line4Offset = document.GetLineByNumber(4).Offset;
		int line6Offset = document.GetLineByNumber(6).Offset;

		//Act
		document.Remove(line4Offset, line6Offset - line4Offset);

		//Assert
		for (int i = 1; i < 3; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		for (int i = 3; i <= 5; i++)
		{
			Assert.True(heightTree.GetIsCollapsed(i));
		}
		for (int i = 6; i <= 8; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	[Fact]
	public void removing_lines_at_the_end_of_a_collapsed_section_shrinks_it() // RemoveEndOfCollapsedSection
	{
		//Arrange
		heightTree.CollapseText(document.GetLineByNumber(3), document.GetLineByNumber(6));
		int line5Offset = document.GetLineByNumber(5).Offset;
		int line8Offset = document.GetLineByNumber(8).Offset;

		//Act
		document.Remove(line5Offset, line8Offset - line5Offset);

		//Assert
		for (int i = 1; i < 3; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		for (int i = 3; i <= 5; i++)
		{
			Assert.True(heightTree.GetIsCollapsed(i));
		}
		for (int i = 6; i <= 7; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
	}

	[Fact]
	public void removing_the_lines_of_a_collapsed_section_uncollapses_it() // RemoveCollapsedSection
	{
		//Arrange
		CollapsedLineSection sec1 = heightTree.CollapseText(document.GetLineByNumber(3), document.GetLineByNumber(3));
		int line3Offset = document.GetLineByNumber(3).Offset;

		//Act
		document.Remove(line3Offset - 1, 1);

		//Assert
		for (int i = 1; i <= 9; i++)
		{
			Assert.False(heightTree.GetIsCollapsed(i));
		}
		CheckHeights();
		Assert.Null(sec1.Start);
		Assert.Null(sec1.End);
		// section gets uncollapsed when it is removed
		Assert.False(sec1.IsCollapsed);
	}

	void CheckHeights()
	{
		HeightTests.CheckHeights(document, heightTree);
	}
}
