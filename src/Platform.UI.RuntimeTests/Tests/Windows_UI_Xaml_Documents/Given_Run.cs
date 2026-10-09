#if __SKIA__

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;

namespace CodeBrix.Platform.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents //Was previously: Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Documents
{
	[TestClass]
	public class Given_Run
	{

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_SingleLine()
		{
			var expected = new ExpectedSegment[] {
				new("  Test ", 2, 1, 0),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.GetSegments());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_SingleLineWith1CharLineBreak()
		{
			var expected = new ExpectedSegment[] {
				new("  Test \n", 2, 1, 1),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.GetSegments());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_SingleLineWith2CharLineBreak()
		{
			var expected = new ExpectedSegment[] {
				new("Test  \r\n", 0, 2, 2),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.GetSegments());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_Multiline()
		{
			var expected = new ExpectedSegment[] {
				new("Test1\n", 0, 0, 1),
				new(" Test2  ", 1, 2, 0),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.GetSegments());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_MultiwordWithSpaces()
		{
			var expected = new ExpectedSegment[] {
				new(" Test1 ", 1, 1, 0),
				new("Test2 ", 0, 1, 0),
				new("Test3   ", 0, 3, 0),
				new("Test4", 0, 0, 0),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.GetSegments());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_MultiwordWithHyphens()
		{
			var expected = new ExpectedSegment[] {
				new(" Test1- ", 1, 1, 0),
				new("Test2---", 0, 0, 0),
				new("Test3 ", 0, 1, 0),
				new("-", 0, 0, 0),
				new("Test4", 0, 0, 0),
			};

			Run run = new() { Text = GetText(expected) };

			AssertSegmentsMatch(expected, run.GetSegments());
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_GetSegments_MultiwordWithNumericHyphens()
		{
			var expected = new ExpectedSegment[] {
				new("Test1-$123-", 0, 0, 0),
				new("Test2 ", 0, 1, 0),
				new("-.456", 0, 0, 0),
			};

			Run run = new() { Text = GetText(expected) };
			AssertSegmentsMatch(expected, run.GetSegments());
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GetSegments_PrivateUseCharacter_Then_SymbolsFont_Is_Used()
		{
			// U+E700 (GlobalNavigationButton) is a Private Use Area code point the framework's symbols font draws and
			// a text font does not. The run segmenter must resolve it the way UnicodeText does: symbols font first.
			const int PrivateUseCodepoint = 0xE700;
			var symbolsFontFamily = global::CodeBrix.Platform.UI.FeatureConfiguration.Font.SymbolsFont;
			await FontFamilyHelper.PreloadAsync(symbolsFontFamily, global::Microsoft.UI.Text.FontWeights.Normal, global::Windows.UI.Text.FontStretch.Normal, global::Windows.UI.Text.FontStyle.Normal);

			Run run = new() { Text = "a\uE700" };
			var symbolsFont = FontDetailsCache.GetFont(symbolsFontFamily, (float)run.FontSize, run.FontWeight, run.FontStretch, run.FontStyle).details;
			if (!symbolsFont.SKFont.ContainsGlyph(PrivateUseCodepoint) || run.GetFontInfo().SKFont.ContainsGlyph(PrivateUseCodepoint))
			{
				Assert.Inconclusive("The symbols font must have U+E700 and the run's own font must not.");
			}

			var segments = run.GetSegments();

			Assert.AreEqual(2, segments.Count);
			Assert.AreEqual("\uE700", segments[1].Text.ToString());
			Assert.IsNull(segments[0].FallbackFont);
			Assert.IsNotNull(segments[1].FallbackFont);
			Assert.AreSame(symbolsFont.SKFont.Typeface, segments[1].FallbackFont.SKFont.Typeface);
		}

		private static string GetText(ExpectedSegment[] expectedSegments) => string.Concat(expectedSegments.Select(s => s.Text));

		private static void AssertSegmentsMatch(ExpectedSegment[] expectedSegments, IReadOnlyList<Segment> resultSegments)
		{
			Assert.AreEqual(expectedSegments.Length, resultSegments.Count);

			int start = 0;

			foreach (var (expected, result) in expectedSegments.Zip(resultSegments, (t, r) => (t, r)))
			{
				Assert.AreEqual(start, result.Start);
				Assert.AreEqual(expected.Text.Length, result.Length);

				Assert.AreEqual(expected.LeadingSpaces, result.LeadingSpaces);
				Assert.AreEqual(expected.TrailingSpaces, result.TrailingSpaces);

				Assert.AreEqual(expected.LineBreakLength, result.LineBreakLength);
				Assert.AreEqual(expected.LineBreakAfter, result.LineBreakAfter);

				if (expected.LineBreakLength == 2)
				{
					Assert.AreEqual(expected.Text.Length - 1, result.Glyphs.Count);
					Assert.AreEqual(start + expected.Text.Length - 2, result.Glyphs[result.Glyphs.Count - 1].Cluster);
				}
				else
				{
					Assert.AreEqual(expected.Text.Length, result.Glyphs.Count);
					Assert.AreEqual(start + expected.Text.Length - 1, result.Glyphs[result.Glyphs.Count - 1].Cluster);
				}

				Assert.AreEqual(start, result.Glyphs[0].Cluster);

				start += expected.Text.Length;
			}
		}

		[DebuggerDisplay("{Text}")]
		private class ExpectedSegment
		{
			public string Text { get; }

			public int LeadingSpaces { get; }

			public int TrailingSpaces { get; }

			public int LineBreakLength { get; }

			public bool LineBreakAfter => LineBreakLength > 0;

			public ExpectedSegment(string text, int leadingSpaces, int trailingSpaces, int lineBreakLength)
			{
				Text = text;
				LeadingSpaces = leadingSpaces;
				TrailingSpaces = trailingSpaces;
				LineBreakLength = lineBreakLength;
			}
		}
	}
}
#endif
