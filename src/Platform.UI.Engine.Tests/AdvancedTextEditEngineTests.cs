#nullable enable

using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Engine;
using CodeBrix.Platform.UI.AdvancedTextEdit.Folding;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.TextLayout;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The AdvancedTextEdit engine (WPE1 C8, the adapters form): the document model, the highlighting (an embedded .xshd
/// definition loaded into neutral storage, a DocumentHighlighter over a TextDocument), folding (FoldingManager over an
/// IFoldingHost) and the completion list's CompletionFilter all run with only the engine Cores in the process - no WinUI
/// assembly loads.
/// </summary>
public class AdvancedTextEditEngineTests
{
	[Fact]
	public void When_A_Document_Is_Highlighted_Edited_And_Undone_Then_The_Neutral_Colours_Are_The_Definitions_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		const string code = "class C\n{\n    // a comment\n    bool M() { return true; }\n}\n";
		var document = new TextDocument(code);
		var definition = HighlightingManager.Instance.GetDefinition("C#")!;

		//Act
		var highlighter = new DocumentHighlighter(document, definition);
		var commentLine = highlighter.HighlightLine(3);
		var keywordLine = highlighter.HighlightLine(4);
		document.Insert(0, "using System;\n");
		var inserted = document.Text;
		document.UndoStack.Undo();
		var shiftedBack = highlighter.HighlightLine(3);

		//Assert: the colours come from the neutral storage (the xshd's "Green", "Red" + bold)
		var comment = commentLine.Sections.Single(s => s.Color?.Name == "Comment").Color!;
		comment.Foreground!.GetColorValue().Should().Be(new SKColor(0x00, 0x80, 0x00)); // Colors "green"
		comment.ToCss().Should().Be("color: #008000; ");
		var valueType = keywordLine.Sections.Select(s => s.Color!).First(c => c.Name == "ValueTypeKeywords");
		valueType.Foreground!.GetColorValue().Should().Be(new SKColor(0xFF, 0x00, 0x00));
		valueType.FontWeightValue.Should().Be((ushort)700);
		valueType.FontStyleValue.Should().BeNull();
		valueType.ToCss().Should().Be("color: #ff0000; font-weight: bold; ");
		inserted.Should().StartWith("using System;\n");
		document.Text.Should().Be(code);
		shiftedBack.Sections.Should().Contain(s => s.Color!.Name == "Comment");
		HighlightingManager.Instance.HighlightingDefinitions.Count.Should().BeGreaterThan(10);

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.AdvancedTextEdit.Core");
		EngineIsolation.AssertNoWinUILoaded("AdvancedTextEdit (document + highlighting)");
	}

	[Fact]
	public void When_Xml_Foldings_Are_Updated_And_Folded_In_A_Folding_Host_Then_Lines_Collapse_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var document = new TextDocument("<root>\n  <child>\n    text\n  </child>\n</root>\n");
		var host = new TestFoldingHost(document);
		var manager = new FoldingManager(document);
		manager.AddHost(host);

		//Act
		new XmlFoldingStrategy().UpdateFoldings(manager, document);
		var foldings = manager.AllFoldings.ToList();
		var outer = foldings.First();
		outer.IsFolded = true;
		var collapsedWhileFolded = host.Tree.GetIsCollapsed(3);
		outer.IsFolded = false;
		var collapsedAfterUnfold = host.Tree.GetIsCollapsed(3);
		manager.RemoveHost(host);

		//Assert
		foldings.Count.Should().Be(2);
		outer.StartOffset.Should().Be(0);
		host.Collapses.Should().Be(1);
		host.Redraws.Should().BeGreaterThan(0);
		collapsedWhileFolded.Should().BeTrue();
		collapsedAfterUnfold.Should().BeFalse();

		EngineIsolation.AssertNoWinUILoaded("AdvancedTextEdit (folding)");
	}

	[Fact]
	public void When_Completion_Items_Are_Filtered_And_Ranked_Then_The_List_Rules_Hold_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var items = new[] { new Item("ToString", 0), new Item("GetHashCode", 0), new Item("GetType", 1), new Item("Equals", 0), new Item("ReferenceEquals", 0) };

		//Act
		var filtered = CompletionFilter.Filter(items, i => i.Text, i => i.Priority, "get", null, out var bestIndex);
		var camel = CompletionFilter.Filter(items, i => i.Text, i => i.Priority, "GHC", null, out var camelBest);
		var substring = CompletionFilter.Filter(items, i => i.Text, i => i.Priority, "quals", items[3], out var substringBest);
		var startMatch = CompletionFilter.FindBestMatch(items, i => i.Text, i => i.Priority, "Eq", suggestedIndex: -1, isFiltering: false);
		var noMatch = CompletionFilter.FindBestMatch(items, i => i.Text, i => i.Priority, "zzz", suggestedIndex: -1, isFiltering: false);

		//Assert
		filtered.Select(i => i.Text).Should().Equal("GetHashCode", "GetType");
		filtered[bestIndex].Text.Should().Be("GetType"); // same quality (a case-insensitive start), higher priority
		camel.Select(i => i.Text).Should().Equal("GetHashCode");
		camelBest.Should().Be(0);
		substring.Select(i => i.Text).Should().Equal("Equals", "ReferenceEquals");
		substring[substringBest].Text.Should().Be("Equals");
		startMatch.Should().Be(3);
		noMatch.Should().Be(-1);
		CompletionFilter.GetMatchQuality("ToString", "ToString", isFiltering: true).Should().Be(8);
		CompletionFilter.GetMatchQuality("ToString", "tostring", isFiltering: true).Should().Be(7);
		CompletionFilter.GetMatchQuality("ToString", "Str", isFiltering: false).Should().Be(-1);
		CompletionFilter.GetMatchQuality("ToString", "Str", isFiltering: true).Should().Be(3);

		EngineIsolation.AssertNoWinUILoaded("AdvancedTextEdit (completion filter)");
	}

	[Fact]
	public void When_Highlighting_Colours_Are_Compared_And_Merged_Then_The_Neutral_Storage_Behaves_Like_The_Public_Members_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var a = new HighlightingColor { Name = "x" };
		a.FontWeightValue = 700;
		a.FontStyleValue = TextFontStyle.Italic;
		a.FontFamilyValue = FontFamilyValue.FromName("Consolas");
		var b = new HighlightingColor { Name = "x" };
		b.FontWeightValue = 700;
		b.FontStyleValue = TextFontStyle.Italic;
		b.FontFamilyValue = FontFamilyValue.FromName("Consolas");
		var c = new HighlightingColor();
		c.FontFamilyValue = FontFamilyValue.FromName("Courier");

		//Act
		var equal = a.Equals(b);
		var sameHash = a.GetHashCode() == b.GetHashCode();
		var merged = a.Clone();
		merged.MergeWith(c);

		//Assert
		equal.Should().BeTrue();
		sameHash.Should().BeTrue();
		a.Equals(c).Should().BeFalse();
		merged.FontFamilyValue!.Name.Should().Be("Courier");
		merged.FontWeightValue.Should().Be((ushort)700);
		a.ToCss().Should().Be("font-weight: bold; font-style: italic; ");

		EngineIsolation.AssertNoWinUILoaded("AdvancedTextEdit (highlighting colours)");
	}

	private sealed record Item(string Text, double Priority);

	/// <summary>A folding host with no view: collapses lines in its own height tree, as a TextView does.</summary>
	private sealed class TestFoldingHost : IFoldingHost
	{
		internal TestFoldingHost(TextDocument document) => Tree = new HeightTree(document, 16);

		internal HeightTree Tree { get; }

		internal int Redraws { get; private set; }

		internal int Collapses { get; private set; }

		public void Redraw() => Redraws++;

		public void Redraw(ISegment segment) => Redraws++;

		public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end)
		{
			Collapses++;
			return Tree.CollapseText(start, end);
		}
	}
}
