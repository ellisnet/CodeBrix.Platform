#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/TextUtilitiesTests.cs in the AvalonEdit repo (MIT).
//GetWhitespaceAfter/GetWhitespaceBefore return ISegment (boxing a SimpleSegment); the unbox cast in
//each assertion keeps the upstream value-equality comparison.

/// <summary>
/// Exercises <see cref="TextUtilities.GetWhitespaceAfter"/> and
/// <see cref="TextUtilities.GetWhitespaceBefore"/>.
/// </summary>
public class TextUtilitiesTests
{
	#region GetWhitespaceAfter
	[Fact]
	public void get_whitespace_after_returns_the_whitespace_run() // TestGetWhitespaceAfter
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(2, 3), (SimpleSegment)TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \tb"), 2));
	}

	[Fact]
	public void get_whitespace_after_does_not_skip_newline() // TestGetWhitespaceAfterDoesNotSkipNewLine
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(2, 3), (SimpleSegment)TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \tb"), 2));
	}

	[Fact]
	public void get_whitespace_after_returns_empty_segment_when_no_whitespace_follows() // TestGetWhitespaceAfterEmptyResult
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(2, 0), (SimpleSegment)TextUtilities.GetWhitespaceAfter(new StringTextSource("a b"), 2));
	}

	[Fact]
	public void get_whitespace_after_at_end_of_string_returns_empty_segment() // TestGetWhitespaceAfterEndOfString
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(2, 0), (SimpleSegment)TextUtilities.GetWhitespaceAfter(new StringTextSource("a "), 2));
	}

	[Fact]
	public void get_whitespace_after_runs_until_end_of_string() // TestGetWhitespaceAfterUntilEndOfString
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(2, 3), (SimpleSegment)TextUtilities.GetWhitespaceAfter(new StringTextSource("a \t \t"), 2));
	}
	#endregion

	#region GetWhitespaceBefore
	[Fact]
	public void get_whitespace_before_returns_the_whitespace_run() // TestGetWhitespaceBefore
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(1, 3), (SimpleSegment)TextUtilities.GetWhitespaceBefore(new StringTextSource("a\t \t b"), 4));
	}

	[Fact]
	public void get_whitespace_before_does_not_skip_newline() // TestGetWhitespaceBeforeDoesNotSkipNewLine
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(2, 1), (SimpleSegment)TextUtilities.GetWhitespaceBefore(new StringTextSource("a\n b"), 3));
	}

	[Fact]
	public void get_whitespace_before_returns_empty_segment_when_no_whitespace_precedes() // TestGetWhitespaceBeforeEmptyResult
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(2, 0), (SimpleSegment)TextUtilities.GetWhitespaceBefore(new StringTextSource(" a b"), 2));
	}

	[Fact]
	public void get_whitespace_before_at_start_of_string_returns_empty_segment() // TestGetWhitespaceBeforeStartOfString
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(0, 0), (SimpleSegment)TextUtilities.GetWhitespaceBefore(new StringTextSource(" a"), 0));
	}

	[Fact]
	public void get_whitespace_before_runs_until_start_of_string() // TestGetWhitespaceBeforeUntilStartOfString
	{
		//Arrange + Act + Assert
		Assert.Equal(new SimpleSegment(0, 2), (SimpleSegment)TextUtilities.GetWhitespaceBefore(new StringTextSource(" \t a"), 2));
	}
	#endregion
}
