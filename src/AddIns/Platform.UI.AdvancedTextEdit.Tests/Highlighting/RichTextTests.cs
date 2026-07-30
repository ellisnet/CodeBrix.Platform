#nullable enable

using System;

using Microsoft.UI;
using Microsoft.UI.Text;

using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Highlighting;

//was previously: ICSharpCode.AvalonEdit.Tests/Highlighting/RichTextTests.cs in the AvalonEdit repo (MIT).
//System.Windows.Media.Colors and System.Windows.FontWeights became the Microsoft.UI framework
//equivalents, matching the port's HighlightingBrush/RichTextModel signatures.

/// <summary>
/// Exercises <see cref="RichText"/>: concatenation keeps per-range highlighting, and HTML export
/// renders background, foreground and font-weight styling.
/// </summary>
public class RichTextTests
{
	[Fact]
	public void concatenation_keeps_the_highlighting_of_each_part() // ConcatTest
	{
		//Arrange
		var textModel = new RichTextModel();
		textModel.SetHighlighting(0, 5, new HighlightingColor { Name = "text1" });
		var text1 = new RichText("text1", textModel);

		var textModel2 = new RichTextModel();
		textModel2.SetHighlighting(0, 5, new HighlightingColor { Name = "text2" });
		var text2 = new RichText("text2", textModel2);

		//Act
		RichText text3 = RichText.Concat(text1, RichText.Empty, text2);

		//Assert
		Assert.Equal(text1.GetHighlightingAt(0), text3.GetHighlightingAt(0));
		Assert.NotEqual(text1.GetHighlightingAt(0), text3.GetHighlightingAt(5));
		Assert.Equal(text2.GetHighlightingAt(0), text3.GetHighlightingAt(5));
	}

	[Fact]
	public void html_export_renders_spans_for_background_foreground_and_font_weight() // ToHtmlTest
	{
		//Arrange
		var textModel = new RichTextModel();
		textModel.SetBackground(5, 3, new SimpleHighlightingBrush(Colors.Yellow));
		textModel.SetForeground(9, 6, new SimpleHighlightingBrush(Colors.Blue));
		textModel.SetFontWeight(15, 1, FontWeights.Bold);
		var text = new RichText("This has spaces!", textModel);

		//Act
		var html = text.ToHtml(new HtmlOptions());

		//Assert
		Assert.Equal("This&nbsp;<span style=\"background-color: #ffff00; \">has</span>&nbsp;<span style=\"color: #0000ff; \">spaces</span><span style=\"font-weight: bold; \">!</span>", html);
	}
}
