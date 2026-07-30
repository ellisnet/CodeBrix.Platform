#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/LineManagerTests.cs in the AvalonEdit repo (MIT).
//NUnit [TestFixture]/[SetUp]/[Test] became an xUnit v3 class with a constructor and [Fact]s; the
//original test names are kept as trailing comments on each renamed snake_case method.

/// <summary>
/// Exercises the document's line manager: line splitting/merging across every combination of
/// CR, LF and CRLF delimiters, offset mapping, and argument validation.
/// </summary>
public class LineManagerTests
{
	readonly TextDocument document;

	public LineManagerTests() // SetUp
	{
		document = new TextDocument();
	}

	[Fact]
	public void empty_document_has_empty_text_zero_length_and_one_line() // CheckEmptyDocument
	{
		//Arrange (empty document from constructor)

		//Act + Assert
		Assert.Equal("", document.Text);
		Assert.Equal(0, document.TextLength);
		Assert.Equal(1, document.LineCount);
	}

	[Fact]
	public void clearing_document_keeps_first_line_and_deletes_the_rest() // CheckClearingDocument
	{
		//Arrange
		document.Text = "Hello,\nWorld!";
		Assert.Equal(2, document.LineCount);
		var oldLines = document.Lines.ToArray();

		//Act
		document.Text = "";

		//Assert
		Assert.Equal("", document.Text);
		Assert.Equal(0, document.TextLength);
		Assert.Equal(1, document.LineCount);
		Assert.Same(oldLines[0], document.Lines.Single());
		Assert.False(oldLines[0].IsDeleted);
		Assert.True(oldLines[1].IsDeleted);
		Assert.Null(oldLines[0].NextLine);
		Assert.Null(oldLines[1].PreviousLine);
	}

	[Fact]
	public void get_line_in_empty_document_returns_the_single_line() // CheckGetLineInEmptyDocument
	{
		//Arrange + Act
		List<DocumentLine> lines = new List<DocumentLine>(document.Lines);
		DocumentLine line = document.Lines[0];

		//Assert
		Assert.Single(document.Lines);
		Assert.Single(lines);
		Assert.Same(line, lines[0]);
		Assert.Same(line, document.GetLineByNumber(1));
		Assert.Same(line, document.GetLineByOffset(0));
	}

	[Fact]
	public void line_segment_in_empty_document_has_zero_lengths() // CheckLineSegmentInEmptyDocument
	{
		//Arrange + Act
		DocumentLine line = document.GetLineByNumber(1);

		//Assert
		Assert.Equal(1, line.LineNumber);
		Assert.Equal(0, line.Offset);
		Assert.False(line.IsDeleted);
		Assert.Equal(0, line.Length);
		Assert.Equal(0, line.TotalLength);
		Assert.Equal(0, line.DelimiterLength);
	}

	[Fact]
	public void lines_index_of_finds_lines_and_returns_minus_one_for_foreign_or_deleted_lines() // LineIndexOfTest
	{
		//Arrange
		DocumentLine line = document.GetLineByNumber(1);

		//Act + Assert
		Assert.Equal(0, document.Lines.IndexOf(line));
		DocumentLine lineFromOtherDocument = new TextDocument().GetLineByNumber(1);
		Assert.Equal(-1, document.Lines.IndexOf(lineFromOtherDocument));
		document.Text = "a\nb\nc";
		DocumentLine middleLine = document.GetLineByNumber(2);
		Assert.Equal(1, document.Lines.IndexOf(middleLine));
		document.Remove(1, 3);
		Assert.True(middleLine.IsDeleted);
		Assert.Equal(-1, document.Lines.IndexOf(middleLine));
	}

	[Fact]
	public void inserting_into_empty_document_creates_single_line_text() // InsertInEmptyDocument
	{
		//Arrange + Act
		document.Insert(0, "a");

		//Assert
		Assert.Equal(1, document.LineCount);
		DocumentLine line = document.GetLineByNumber(1);
		Assert.Equal("a", document.GetText(line));
	}

	[Fact]
	public void setting_text_creates_single_line() // SetText
	{
		//Arrange + Act
		document.Text = "a";

		//Assert
		Assert.Equal(1, document.LineCount);
		DocumentLine line = document.GetLineByNumber(1);
		Assert.Equal("a", document.GetText(line));
	}

	[Fact]
	public void inserting_empty_string_changes_nothing() // InsertNothing
	{
		//Arrange + Act
		document.Insert(0, "");

		//Assert
		Assert.Equal(1, document.LineCount);
		Assert.Equal(0, document.TextLength);
	}

	[Fact]
	public void inserting_null_text_throws_argument_null_exception() // InsertNull
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentNullException>(() => document.Insert(0, (string)null!));
	}

	[Fact]
	public void setting_text_to_null_throws_argument_null_exception() // SetTextNull
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentNullException>(() => document.Text = null!);
	}

	[Fact]
	public void removing_zero_characters_changes_nothing() // RemoveNothing
	{
		//Arrange + Act
		document.Remove(0, 0);

		//Assert
		Assert.Equal(1, document.LineCount);
		Assert.Equal(0, document.TextLength);
	}

	[Fact]
	public void get_char_at_zero_in_empty_document_throws() // GetCharAt0EmptyDocument
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() => document.GetCharAt(0));
	}

	[Fact]
	public void get_char_at_negative_offset_throws() // GetCharAtNegativeOffset
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.GetCharAt(-1);
		});
	}

	[Fact]
	public void get_char_at_end_offset_throws() // GetCharAtEndOffset
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.GetCharAt(document.TextLength);
		});
	}

	[Fact]
	public void inserting_at_negative_offset_throws() // InsertAtNegativeOffset
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.Insert(-1, "text");
		});
	}

	[Fact]
	public void inserting_after_end_offset_throws() // InsertAfterEndOffset
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.Insert(4, "text");
		});
	}

	[Fact]
	public void removing_negative_amount_throws() // RemoveNegativeAmount
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "abcd";
			document.Remove(2, -1);
		});
	}

	[Fact]
	public void removing_past_end_of_document_throws() // RemoveTooMuch
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "abcd";
			document.Remove(2, 10);
		});
	}

	[Fact]
	public void get_line_by_negative_number_throws() // GetLineByNumberNegative
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.GetLineByNumber(-1);
		});
	}

	[Fact]
	public void get_line_by_too_high_number_throws() // GetLineByNumberTooHigh
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.GetLineByNumber(3);
		});
	}

	[Fact]
	public void get_line_by_negative_offset_throws() // GetLineByOffsetNegative
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.GetLineByOffset(-1);
		});
	}

	[Fact]
	public void get_line_by_too_high_offset_throws() // GetLineByOffsetToHigh
	{
		//Arrange + Act + Assert
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			document.Text = "a\nb";
			document.GetLineByOffset(10);
		});
	}

	[Fact]
	public void inserting_at_end_offset_appends_to_last_line() // InsertAtEndOffset
	{
		//Arrange
		document.Text = "a\nb";
		CheckDocumentLines("a",
						   "b");

		//Act
		document.Insert(3, "text");

		//Assert
		CheckDocumentLines("a",
						   "btext");
	}

	[Fact]
	public void get_char_at_returns_characters_including_delimiters() // GetCharAt
	{
		//Arrange
		document.Text = "a\r\nb";

		//Act + Assert
		Assert.Equal('a', document.GetCharAt(0));
		Assert.Equal('\r', document.GetCharAt(1));
		Assert.Equal('\n', document.GetCharAt(2));
		Assert.Equal('b', document.GetCharAt(3));
	}

	[Fact]
	public void mixed_newlines_produce_expected_lines_and_delimiter_lengths() // CheckMixedNewLineTest
	{
		//Arrange
		const string mixedNewlineText = "line 1\nline 2\r\nline 3\rline 4";

		//Act
		document.Text = mixedNewlineText;

		//Assert
		Assert.Equal(mixedNewlineText, document.Text);
		Assert.Equal(4, document.LineCount);
		for (int i = 1; i < 4; i++)
		{
			DocumentLine line = document.GetLineByNumber(i);
			Assert.Equal(i, line.LineNumber);
			Assert.Equal("line " + i, document.GetText(line));
		}
		Assert.Equal(1, document.GetLineByNumber(1).DelimiterLength);
		Assert.Equal(2, document.GetLineByNumber(2).DelimiterLength);
		Assert.Equal(1, document.GetLineByNumber(3).DelimiterLength);
		Assert.Equal(0, document.GetLineByNumber(4).DelimiterLength);
	}

	[Fact]
	public void lf_cr_counts_as_two_newlines() // LfCrIsTwoNewLinesTest
	{
		//Arrange + Act
		document.Text = "a\n\rb";

		//Assert
		Assert.Equal("a\n\rb", document.Text);
		CheckDocumentLines("a",
						   "",
						   "b");
	}

	[Fact]
	public void removing_first_part_of_crlf_delimiter_leaves_lf() // RemoveFirstPartOfDelimiter
	{
		//Arrange
		document.Text = "a\r\nb";

		//Act
		document.Remove(1, 1);

		//Assert
		Assert.Equal("a\nb", document.Text);
		CheckDocumentLines("a",
						   "b");
	}

	[Fact]
	public void removing_line_content_joins_cr_and_lf_into_one_delimiter() // RemoveLineContentAndJoinDelimiters
	{
		//Arrange
		document.Text = "a\rb\nc";

		//Act
		document.Remove(2, 1);

		//Assert
		Assert.Equal("a\r\nc", document.Text);
		CheckDocumentLines("a",
						   "c");
	}

	[Fact]
	public void removing_multiple_lines_joins_cr_and_lf_into_one_delimiter() // RemoveLineContentAndJoinDelimiters2
	{
		//Arrange
		document.Text = "a\rb\nc\nd";

		//Act
		document.Remove(2, 3);

		//Assert
		Assert.Equal("a\r\nd", document.Text);
		CheckDocumentLines("a",
						   "d");
	}

	[Fact]
	public void removing_line_content_and_cr_joins_remaining_cr_and_lf() // RemoveLineContentAndJoinDelimiters3
	{
		//Arrange
		document.Text = "a\rb\r\nc";

		//Act
		document.Remove(2, 2);

		//Assert
		Assert.Equal("a\r\nc", document.Text);
		CheckDocumentLines("a",
						   "c");
	}

	[Fact]
	public void removing_line_content_between_two_lf_delimiters_keeps_them_separate() // RemoveLineContentAndJoinNonMatchingDelimiters
	{
		//Arrange
		document.Text = "a\nb\nc";

		//Act
		document.Remove(2, 1);

		//Assert
		Assert.Equal("a\n\nc", document.Text);
		CheckDocumentLines("a",
						   "",
						   "c");
	}

	[Fact]
	public void removing_line_content_between_lf_and_cr_keeps_them_separate() // RemoveLineContentAndJoinNonMatchingDelimiters2
	{
		//Arrange
		document.Text = "a\nb\rc";

		//Act
		document.Remove(2, 1);

		//Assert
		Assert.Equal("a\n\rc", document.Text);
		CheckDocumentLines("a",
						   "",
						   "c");
	}

	[Fact]
	public void removing_multiline_up_to_first_part_of_delimiter() // RemoveMultilineUpToFirstPartOfDelimiter
	{
		//Arrange
		document.Text = "0\n1\r\n2";

		//Act
		document.Remove(1, 3);

		//Assert
		Assert.Equal("0\n2", document.Text);
		CheckDocumentLines("0",
						   "2");
	}

	[Fact]
	public void removing_second_part_of_crlf_delimiter_leaves_cr() // RemoveSecondPartOfDelimiter
	{
		//Arrange
		document.Text = "a\r\nb";

		//Act
		document.Remove(2, 1);

		//Assert
		Assert.Equal("a\rb", document.Text);
		CheckDocumentLines("a",
						   "b");
	}

	[Fact]
	public void removing_from_second_part_of_delimiter_to_next_line() // RemoveFromSecondPartOfDelimiter
	{
		//Arrange
		document.Text = "a\r\nb\nc";

		//Act
		document.Remove(2, 3);

		//Assert
		Assert.Equal("a\rc", document.Text);
		CheckDocumentLines("a",
						   "c");
	}

	[Fact]
	public void removing_from_second_part_of_delimiter_to_document_end() // RemoveFromSecondPartOfDelimiterToDocumentEnd
	{
		//Arrange
		document.Text = "a\r\nb";

		//Act
		document.Remove(2, 2);

		//Assert
		Assert.Equal("a\r", document.Text);
		CheckDocumentLines("a",
						   "");
	}

	[Fact]
	public void removing_up_to_matching_lf_delimiter_merges_them() // RemoveUpToMatchingDelimiter1
	{
		//Arrange
		document.Text = "a\r\nb\nc";

		//Act
		document.Remove(2, 2);

		//Assert
		Assert.Equal("a\r\nc", document.Text);
		CheckDocumentLines("a",
						   "c");
	}

	[Fact]
	public void removing_up_to_matching_crlf_delimiter_merges_them() // RemoveUpToMatchingDelimiter2
	{
		//Arrange
		document.Text = "a\r\nb\r\nc";

		//Act
		document.Remove(2, 3);

		//Assert
		Assert.Equal("a\r\nc", document.Text);
		CheckDocumentLines("a",
						   "c");
	}

	[Fact]
	public void removing_up_to_non_matching_delimiter_keeps_lines_separate() // RemoveUpToNonMatchingDelimiter
	{
		//Arrange
		document.Text = "a\r\nb\rc";

		//Act
		document.Remove(2, 2);

		//Assert
		Assert.Equal("a\r\rc", document.Text);
		CheckDocumentLines("a",
						   "",
						   "c");
	}

	[Fact]
	public void removing_two_char_delimiter_joins_lines() // RemoveTwoCharDelimiter
	{
		//Arrange
		document.Text = "a\r\nb";

		//Act
		document.Remove(1, 2);

		//Assert
		Assert.Equal("ab", document.Text);
		CheckDocumentLines("ab");
	}

	[Fact]
	public void removing_one_char_delimiter_joins_lines() // RemoveOneCharDelimiter
	{
		//Arrange
		document.Text = "a\nb";

		//Act
		document.Remove(1, 1);

		//Assert
		Assert.Equal("ab", document.Text);
		CheckDocumentLines("ab");
	}

	void CheckDocumentLines(params string[] lines)
	{
		Assert.Equal(lines.Length, document.LineCount);
		for (int i = 0; i < lines.Length; i++)
		{
			Assert.Equal(lines[i], document.GetText(document.Lines[i]));
		}
	}

	[Fact]
	public void replacing_lf_with_cr_before_lf_forms_single_crlf() // FixUpFirstPartOfDelimiter
	{
		//Arrange
		document.Text = "a\n\nb";

		//Act
		document.Replace(1, 1, "\r");

		//Assert
		Assert.Equal("a\r\nb", document.Text);
		CheckDocumentLines("a",
						   "b");
	}

	[Fact]
	public void replacing_cr_with_lf_after_cr_forms_single_crlf() // FixUpSecondPartOfDelimiter
	{
		//Arrange
		document.Text = "a\r\rb";

		//Act
		document.Replace(2, 1, "\n");

		//Assert
		Assert.Equal("a\r\nb", document.Text);
		CheckDocumentLines("a",
						   "b");
	}

	[Fact]
	public void inserting_inside_crlf_delimiter_splits_it() // InsertInsideDelimiter
	{
		//Arrange
		document.Text = "a\r\nc";

		//Act
		document.Insert(2, "b");

		//Assert
		Assert.Equal("a\rb\nc", document.Text);
		CheckDocumentLines("a",
						   "b",
						   "c");
	}

	[Fact]
	public void inserting_multiline_text_inside_crlf_delimiter_splits_it() // InsertInsideDelimiter2
	{
		//Arrange
		document.Text = "a\r\nd";

		//Act
		document.Insert(2, "b\nc");

		//Assert
		Assert.Equal("a\rb\nc\nd", document.Text);
		CheckDocumentLines("a",
						   "b",
						   "c",
						   "d");
	}

	[Fact]
	public void inserting_text_ending_with_cr_inside_crlf_delimiter() // InsertInsideDelimiter3
	{
		//Arrange
		document.Text = "a\r\nc";

		//Act
		document.Insert(2, "b\r");

		//Assert
		Assert.Equal("a\rb\r\nc", document.Text);
		CheckDocumentLines("a",
						   "b",
						   "c");
	}

	[Fact]
	public void inserting_text_ending_with_cr_before_lf_extends_delimiter() // ExtendDelimiter1
	{
		//Arrange
		document.Text = "a\nc";

		//Act
		document.Insert(1, "b\r");

		//Assert
		Assert.Equal("ab\r\nc", document.Text);
		CheckDocumentLines("ab",
						   "c");
	}

	[Fact]
	public void inserting_lf_after_cr_extends_delimiter() // ExtendDelimiter2
	{
		//Arrange
		document.Text = "a\rc";

		//Act
		document.Insert(2, "\nb");

		//Assert
		Assert.Equal("a\r\nbc", document.Text);
		CheckDocumentLines("a",
						   "bc");
	}

	[Fact]
	public void replacing_line_content_between_matching_delimiters() // ReplaceLineContentBetweenMatchingDelimiters
	{
		//Arrange
		document.Text = "a\rb\nc";

		//Act
		document.Replace(2, 1, "x");

		//Assert
		Assert.Equal("a\rx\nc", document.Text);
		CheckDocumentLines("a",
						   "x",
						   "c");
	}

	[Fact]
	public void get_offset_maps_line_and_column_to_offset() // GetOffset
	{
		//Arrange
		document.Text = "Hello,\nWorld!";

		//Act + Assert
		Assert.Equal(0, document.GetOffset(1, 1));
		Assert.Equal(1, document.GetOffset(1, 2));
		Assert.Equal(5, document.GetOffset(1, 6));
		Assert.Equal(6, document.GetOffset(1, 7));
		Assert.Equal(7, document.GetOffset(2, 1));
		Assert.Equal(8, document.GetOffset(2, 2));
		Assert.Equal(12, document.GetOffset(2, 6));
		Assert.Equal(13, document.GetOffset(2, 7));
	}

	[Fact]
	public void get_offset_clamps_negative_columns_to_line_start() // GetOffsetIgnoreNegativeColumns
	{
		//Arrange
		document.Text = "Hello,\nWorld!";

		//Act + Assert
		Assert.Equal(0, document.GetOffset(1, -1));
		Assert.Equal(0, document.GetOffset(1, -100));
		Assert.Equal(0, document.GetOffset(1, 0));
		Assert.Equal(7, document.GetOffset(2, -1));
		Assert.Equal(7, document.GetOffset(2, -100));
		Assert.Equal(7, document.GetOffset(2, 0));
	}

	[Fact]
	public void get_offset_clamps_too_high_columns_to_line_end() // GetOffsetIgnoreTooHighColumns
	{
		//Arrange
		document.Text = "Hello,\nWorld!";

		//Act + Assert
		Assert.Equal(6, document.GetOffset(1, 8));
		Assert.Equal(6, document.GetOffset(1, 100));
		Assert.Equal(13, document.GetOffset(2, 8));
		Assert.Equal(13, document.GetOffset(2, 100));
	}
}
