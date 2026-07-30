#nullable enable

using System;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/HeightTests.cs in the AvalonEdit repo (MIT).
//The internal static CheckHeights helper is shared with CollapsingTests and
//RandomizedLineManagerTest, exactly as upstream.

/// <summary>
/// Exercises the height tree's visual-position bookkeeping: line removal, per-line height
/// changes, and line insertion all keep <c>GetVisualPosition</c>/<c>TotalHeight</c> consistent.
/// </summary>
public class HeightTests
{
	readonly TextDocument document;
	readonly HeightTree heightTree;

	public HeightTests() // Setup
	{
		document = new TextDocument();
		document.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
		heightTree = new HeightTree(document, 10);
		foreach (DocumentLine line in document.Lines)
		{
			heightTree.SetHeight(line, line.LineNumber);
		}
	}

	[Fact]
	public void fresh_tree_has_consistent_heights() // SimpleCheck
	{
		//Arrange (tree from constructor)

		//Act + Assert
		CheckHeights();
	}

	[Fact]
	public void removing_lines_keeps_heights_consistent() // TestLinesRemoved
	{
		//Arrange (tree from constructor)

		//Act
		document.Remove(5, 4);

		//Assert
		CheckHeights();
	}

	[Fact]
	public void changing_a_line_height_keeps_heights_consistent() // TestHeightChanged
	{
		//Arrange (tree from constructor)

		//Act
		heightTree.SetHeight(document.GetLineByNumber(4), 100);

		//Assert
		CheckHeights();
	}

	[Fact]
	public void inserting_lines_keeps_heights_consistent() // TestLinesInserted
	{
		//Arrange (tree from constructor)

		//Act
		document.Insert(0, "x\ny\n");
		heightTree.SetHeight(document.Lines[0], 100);
		heightTree.SetHeight(document.Lines[1], 1000);
		heightTree.SetHeight(document.Lines[2], 10000);

		//Assert
		CheckHeights();
	}

	void CheckHeights()
	{
		CheckHeights(document, heightTree);
	}

	internal static void CheckHeights(TextDocument document, HeightTree heightTree)
	{
		double[] heights = document.Lines.Select(l => heightTree.GetIsCollapsed(l.LineNumber) ? 0 : heightTree.GetHeight(l)).ToArray();
		double[] visualPositions = new double[document.LineCount + 1];
		for (int i = 0; i < heights.Length; i++)
		{
			visualPositions[i + 1] = visualPositions[i] + heights[i];
		}
		foreach (DocumentLine ls in document.Lines)
		{
			Assert.Equal(visualPositions[ls.LineNumber - 1], heightTree.GetVisualPosition(ls));
		}
		Assert.Equal(visualPositions[document.LineCount], heightTree.TotalHeight);
	}
}
