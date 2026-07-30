#nullable enable

using System;
using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/RandomizedLineManagerTest.cs in the AvalonEdit repo (MIT).
//NUnit [OneTimeSetUp] seeded a Random from Environment.TickCount once per fixture; kept via a static
//field (time-based seed, printed to the console, exactly as upstream).

/// <summary>
/// A randomized test for the line manager: random replacements are checked against a straightforward
/// line scan of the text, and a combined run also stresses collapsed sections in the height tree.
/// </summary>
public class RandomizedLineManagerTest
{
	static readonly int seed;
	static readonly Random rnd;

	static RandomizedLineManagerTest() // FixtureSetup
	{
		seed = Environment.TickCount;
		Console.WriteLine("RandomizedLineManagerTest Seed: " + seed);
		rnd = new Random(seed);
	}

	readonly TextDocument document;

	public RandomizedLineManagerTest() // Setup
	{
		document = new TextDocument();
	}

	[Fact]
	public void short_random_replacements_keep_lines_consistent() // ShortReplacements
	{
		//Arrange
		char[] chars = { 'a', 'b', '\r', '\n' };
		char[] buffer = new char[20];

		//Act + Assert
		for (int i = 0; i < 2500; i++)
		{
			int offset = rnd.Next(0, document.TextLength);
			int length = rnd.Next(0, document.TextLength - offset);
			int newTextLength = rnd.Next(0, 20);
			for (int j = 0; j < newTextLength; j++)
			{
				buffer[j] = chars[rnd.Next(0, chars.Length)];
			}

			document.Replace(offset, length, new string(buffer, 0, newTextLength));
			CheckLines();
		}
	}

	[Fact]
	public void large_random_replacements_keep_text_and_lines_consistent() // LargeReplacements
	{
		//Arrange
		char[] chars = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', '\r', '\n' };
		char[] buffer = new char[1000];

		//Act + Assert
		for (int i = 0; i < 20; i++)
		{
			int offset = rnd.Next(0, document.TextLength);
			int length = rnd.Next(0, (document.TextLength - offset) / 4);
			int newTextLength = rnd.Next(0, 1000);
			for (int j = 0; j < newTextLength; j++)
			{
				buffer[j] = chars[rnd.Next(0, chars.Length)];
			}

			string newText = new string(buffer, 0, newTextLength);
			string expectedText = document.Text.Remove(offset, length).Insert(offset, newText);
			document.Replace(offset, length, newText);
			Assert.Equal(expectedText, document.Text);
			CheckLines();
		}
	}

	void CheckLines()
	{
		string text = document.Text;
		int lineNumber = 1;
		int lineStart = 0;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
			{
				DocumentLine line = document.GetLineByNumber(lineNumber);
				Assert.Equal(lineNumber, line.LineNumber);
				Assert.Equal(2, line.DelimiterLength);
				Assert.Equal(lineStart, line.Offset);
				Assert.Equal(i - lineStart, line.Length);
				i++; // consume \n
				lineNumber++;
				lineStart = i + 1;
			}
			else if (c == '\r' || c == '\n')
			{
				DocumentLine line = document.GetLineByNumber(lineNumber);
				Assert.Equal(lineNumber, line.LineNumber);
				Assert.Equal(1, line.DelimiterLength);
				Assert.Equal(lineStart, line.Offset);
				Assert.Equal(i - lineStart, line.Length);
				lineNumber++;
				lineStart = i + 1;
			}
		}
		Assert.Equal(lineNumber, document.LineCount);
	}

	[Fact]
	public void random_edits_collapses_and_height_changes_keep_the_height_tree_consistent() // CollapsingTest
	{
		//Arrange
		char[] chars = { 'a', 'b', '\r', '\n' };
		char[] buffer = new char[20];
		HeightTree heightTree = new HeightTree(document, 10);
		List<CollapsedLineSection> collapsedSections = new List<CollapsedLineSection>();

		//Act + Assert
		for (int i = 0; i < 2500; i++)
		{
			switch (rnd.Next(0, 10))
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
					int offset = rnd.Next(0, document.TextLength);
					int length = rnd.Next(0, document.TextLength - offset);
					int newTextLength = rnd.Next(0, 20);
					for (int j = 0; j < newTextLength; j++)
					{
						buffer[j] = chars[rnd.Next(0, chars.Length)];
					}

					document.Replace(offset, length, new string(buffer, 0, newTextLength));
					break;
				case 6:
				case 7:
					int startLine = rnd.Next(1, document.LineCount + 1);
					int endLine = rnd.Next(startLine, document.LineCount + 1);
					collapsedSections.Add(heightTree.CollapseText(document.GetLineByNumber(startLine), document.GetLineByNumber(endLine)));
					break;
				case 8:
					if (collapsedSections.Count > 0)
					{
						CollapsedLineSection cs = collapsedSections[rnd.Next(0, collapsedSections.Count)];
						// unless the text section containing the CollapsedSection was deleted:
						if (cs.Start != null)
						{
							cs.Uncollapse();
						}
						collapsedSections.Remove(cs);
					}
					break;
				case 9:
					foreach (DocumentLine ls in document.Lines)
					{
						heightTree.SetHeight(ls, ls.LineNumber);
					}
					break;
			}
			var treeSections = new HashSet<CollapsedLineSection>(heightTree.GetAllCollapsedSections());
			int expectedCount = 0;
			foreach (CollapsedLineSection cs in collapsedSections)
			{
				if (cs.Start != null)
				{
					expectedCount++;
					Assert.Contains(cs, treeSections);
				}
			}
			Assert.Equal(expectedCount, treeSections.Count);
			CheckLines();
			HeightTests.CheckHeights(document, heightTree);
		}
	}
}
