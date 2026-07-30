#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Utils;

//was previously: ICSharpCode.AvalonEdit.Tests/Utils/CaretNavigationTests.cs in the AvalonEdit repo (MIT).
//System.Windows.Documents.LogicalDirection is now the port's own LogicalDirection shim in the
//Document namespace. The combining-mark literals (U+0346 COMBINING BRIDGE ABOVE) are written as
//escapes so the source encoding cannot alter the test data.

/// <summary>
/// Exercises <see cref="TextUtilities.GetNextCaretPosition"/>: word starts/borders, surrogate
/// pairs outside the BMP, and combining marks.
/// </summary>
public class CaretNavigationTests
{
	int GetNextCaretStop(string text, int offset, CaretPositioningMode mode)
	{
		return TextUtilities.GetNextCaretPosition(new StringTextSource(text), offset, LogicalDirection.Forward, mode);
	}

	int GetPrevCaretStop(string text, int offset, CaretPositioningMode mode)
	{
		return TextUtilities.GetNextCaretPosition(new StringTextSource(text), offset, LogicalDirection.Backward, mode);
	}

	[Fact]
	public void caret_stops_in_empty_string_only_exist_for_normal_mode() // CaretStopInEmptyString
	{
		//Arrange + Act + Assert
		Assert.Equal(0, GetNextCaretStop("", -1, CaretPositioningMode.Normal));
		Assert.Equal(-1, GetNextCaretStop("", 0, CaretPositioningMode.Normal));
		Assert.Equal(-1, GetPrevCaretStop("", 0, CaretPositioningMode.Normal));
		Assert.Equal(0, GetPrevCaretStop("", 1, CaretPositioningMode.Normal));

		Assert.Equal(-1, GetNextCaretStop("", -1, CaretPositioningMode.WordStart));
		Assert.Equal(-1, GetNextCaretStop("", -1, CaretPositioningMode.WordBorder));
		Assert.Equal(-1, GetPrevCaretStop("", 1, CaretPositioningMode.WordStart));
		Assert.Equal(-1, GetPrevCaretStop("", 1, CaretPositioningMode.WordBorder));
	}

	[Fact]
	public void start_of_document_counts_as_word_start_when_a_word_begins_there() // StartOfDocumentWithWordStart
	{
		//Arrange + Act + Assert
		Assert.Equal(0, GetNextCaretStop("word", -1, CaretPositioningMode.Normal));
		Assert.Equal(0, GetNextCaretStop("word", -1, CaretPositioningMode.WordStart));
		Assert.Equal(0, GetNextCaretStop("word", -1, CaretPositioningMode.WordBorder));

		Assert.Equal(0, GetPrevCaretStop("word", 1, CaretPositioningMode.Normal));
		Assert.Equal(0, GetPrevCaretStop("word", 1, CaretPositioningMode.WordStart));
		Assert.Equal(0, GetPrevCaretStop("word", 1, CaretPositioningMode.WordBorder));
	}

	[Fact]
	public void start_of_document_is_no_word_start_when_it_begins_with_whitespace() // StartOfDocumentNoWordStart
	{
		//Arrange + Act + Assert
		Assert.Equal(0, GetNextCaretStop(" word", -1, CaretPositioningMode.Normal));
		Assert.Equal(1, GetNextCaretStop(" word", -1, CaretPositioningMode.WordStart));
		Assert.Equal(1, GetNextCaretStop(" word", -1, CaretPositioningMode.WordBorder));

		Assert.Equal(0, GetPrevCaretStop(" word", 1, CaretPositioningMode.Normal));
		Assert.Equal(-1, GetPrevCaretStop(" word", 1, CaretPositioningMode.WordStart));
		Assert.Equal(-1, GetPrevCaretStop(" word", 1, CaretPositioningMode.WordBorder));
	}

	[Fact]
	public void end_of_document_counts_as_word_border_when_a_word_ends_there() // EndOfDocumentWordBorder
	{
		//Arrange + Act + Assert
		Assert.Equal(4, GetNextCaretStop("word", 3, CaretPositioningMode.Normal));
		Assert.Equal(-1, GetNextCaretStop("word", 3, CaretPositioningMode.WordStart));
		Assert.Equal(4, GetNextCaretStop("word", 3, CaretPositioningMode.WordBorder));

		Assert.Equal(4, GetPrevCaretStop("word", 5, CaretPositioningMode.Normal));
		Assert.Equal(0, GetPrevCaretStop("word", 5, CaretPositioningMode.WordStart));
		Assert.Equal(4, GetPrevCaretStop("word", 5, CaretPositioningMode.WordBorder));
	}

	[Fact]
	public void end_of_document_is_no_word_border_when_it_ends_with_whitespace() // EndOfDocumentNoWordBorder
	{
		//Arrange + Act + Assert
		Assert.Equal(4, GetNextCaretStop("txt ", 3, CaretPositioningMode.Normal));
		Assert.Equal(-1, GetNextCaretStop("txt ", 3, CaretPositioningMode.WordStart));
		Assert.Equal(-1, GetNextCaretStop("txt ", 3, CaretPositioningMode.WordBorder));

		Assert.Equal(4, GetPrevCaretStop("txt ", 5, CaretPositioningMode.Normal));
		Assert.Equal(0, GetPrevCaretStop("txt ", 5, CaretPositioningMode.WordStart));
		Assert.Equal(3, GetPrevCaretStop("txt ", 5, CaretPositioningMode.WordBorder));
	}

	[Fact]
	public void surrogate_pair_outside_bmp_is_a_single_caret_step() // SingleCharacterOutsideBMP
	{
		//Arrange
		string c = "\U0001D49E";

		//Act + Assert
		Assert.Equal(2, GetNextCaretStop(c, 0, CaretPositioningMode.Normal));
		Assert.Equal(0, GetPrevCaretStop(c, 2, CaretPositioningMode.Normal));
	}

	[Fact]
	public void word_borders_are_detected_around_non_bmp_letters() // DetectWordBordersOutsideBMP
	{
		//Arrange
		string c = " a\U0001D49Eb ";

		//Act + Assert
		Assert.Equal(1, GetNextCaretStop(c, 0, CaretPositioningMode.WordBorder));
		Assert.Equal(5, GetNextCaretStop(c, 1, CaretPositioningMode.WordBorder));

		Assert.Equal(5, GetPrevCaretStop(c, 6, CaretPositioningMode.WordBorder));
		Assert.Equal(1, GetPrevCaretStop(c, 5, CaretPositioningMode.WordBorder));
	}

	[Fact]
	public void word_borders_are_detected_around_words_made_only_of_non_bmp_letters() // DetectWordBordersOutsideBMP2
	{
		//Arrange
		string c = " \U0001D49E\U0001D4AA ";

		//Act + Assert
		Assert.Equal(1, GetNextCaretStop(c, 0, CaretPositioningMode.WordBorder));
		Assert.Equal(5, GetNextCaretStop(c, 1, CaretPositioningMode.WordBorder));

		Assert.Equal(5, GetPrevCaretStop(c, 6, CaretPositioningMode.WordBorder));
		Assert.Equal(1, GetPrevCaretStop(c, 5, CaretPositioningMode.WordBorder));
	}

	[Fact]
	public void combining_mark_stays_attached_to_its_base_character() // CombiningMark
	{
		//Arrange
		string str = " x\u0346 ";

		//Act + Assert
		Assert.Equal(3, GetNextCaretStop(str, 1, CaretPositioningMode.Normal));
		Assert.Equal(1, GetPrevCaretStop(str, 3, CaretPositioningMode.Normal));
	}

	[Fact]
	public void stacked_combining_marks_stay_attached_to_their_base_character() // StackedCombiningMark
	{
		//Arrange
		string str = " x\u0346\u0346\u0346\u0346 ";

		//Act + Assert
		Assert.Equal(6, GetNextCaretStop(str, 1, CaretPositioningMode.Normal));
		Assert.Equal(1, GetPrevCaretStop(str, 6, CaretPositioningMode.Normal));
	}

	[Fact]
	public void single_closing_brace_at_line_end_is_a_word_start() // SingleClosingBraceAtLineEnd
	{
		//Arrange
		string str = "\t\t}";

		//Act + Assert
		Assert.Equal(2, GetNextCaretStop(str, 1, CaretPositioningMode.WordStart));
		Assert.Equal(-1, GetPrevCaretStop(str, 1, CaretPositioningMode.WordStart));
	}
}
