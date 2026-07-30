#nullable enable

using System;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Editing;

//was previously: ICSharpCode.AvalonEdit.Tests/Editing/TextSegmentReadOnlySectionTests.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Exercises <see cref="TextSegmentReadOnlySectionProvider{T}"/>: where insertion is allowed and
/// which parts of a requested deletion survive around read-only segments.
/// </summary>
public class TextSegmentReadOnlySectionTests
{
	readonly TextSegmentCollection<TextSegment> segments;
	readonly TextSegmentReadOnlySectionProvider<TextSegment> provider;

	public TextSegmentReadOnlySectionTests() // SetUp
	{
		segments = new TextSegmentCollection<TextSegment>();
		provider = new TextSegmentReadOnlySectionProvider<TextSegment>(segments);
	}

	[Fact]
	public void insertion_is_possible_when_nothing_is_read_only() // InsertionPossibleWhenNothingIsReadOnly
	{
		//Arrange (no read-only segments)

		//Act + Assert
		Assert.True(provider.CanInsert(0));
		Assert.True(provider.CanInsert(100));
	}

	[Fact]
	public void deletion_is_possible_when_nothing_is_read_only() // DeletionPossibleWhenNothingIsReadOnly
	{
		//Arrange (no read-only segments)

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(10, 20)).ToList();

		//Assert
		var segment = Assert.Single(result);
		Assert.Equal(10, segment.Offset);
		Assert.Equal(20, segment.Length);
	}

	[Fact]
	public void empty_deletion_is_possible_when_nothing_is_read_only() // EmptyDeletionPossibleWhenNothingIsReadOnly
	{
		//Arrange (no read-only segments)

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(10, 0)).ToList();

		//Assert
		var segment = Assert.Single(result);
		Assert.Equal(10, segment.Offset);
		Assert.Equal(0, segment.Length);
	}

	[Fact]
	public void insertion_is_possible_before_a_read_only_segment() // InsertionPossibleBeforeReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });

		//Act + Assert
		Assert.True(provider.CanInsert(5));
	}

	[Fact]
	public void insertion_is_possible_at_the_start_of_a_read_only_segment() // InsertionPossibleAtStartOfReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });

		//Act + Assert
		Assert.True(provider.CanInsert(10));
	}

	[Fact]
	public void insertion_is_impossible_inside_a_read_only_segment() // InsertionImpossibleInsideReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });

		//Act + Assert
		Assert.False(provider.CanInsert(11));
		Assert.False(provider.CanInsert(12));
		Assert.False(provider.CanInsert(13));
		Assert.False(provider.CanInsert(14));
	}

	[Fact]
	public void insertion_is_possible_at_the_end_of_a_read_only_segment() // InsertionPossibleAtEndOfReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });

		//Act + Assert
		Assert.True(provider.CanInsert(15));
	}

	[Fact]
	public void insertion_is_possible_between_two_read_only_segments() // InsertionPossibleBetweenReadOnlySegments
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
		segments.Add(new TextSegment { StartOffset = 15, EndOffset = 20 });

		//Act + Assert
		Assert.True(provider.CanInsert(15));
	}

	[Fact]
	public void deletion_is_impossible_inside_a_read_only_segment() // DeletionImpossibleInReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(11, 2)).ToList();

		//Assert
		Assert.Empty(result);
	}

	[Fact]
	public void empty_deletion_is_impossible_inside_a_read_only_segment() // EmptyDeletionImpossibleInReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(11, 0)).ToList();

		//Assert
		Assert.Empty(result);
	}

	[Fact]
	public void empty_deletion_is_possible_at_the_start_of_a_read_only_segment() // EmptyDeletionPossibleAtStartOfReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(10, 0)).ToList();

		//Assert
		var segment = Assert.Single(result);
		Assert.Equal(10, segment.Offset);
		Assert.Equal(0, segment.Length);
	}

	[Fact]
	public void empty_deletion_is_possible_at_the_end_of_a_read_only_segment() // EmptyDeletionPossibleAtEndOfReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 10, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(15, 0)).ToList();

		//Assert
		var segment = Assert.Single(result);
		Assert.Equal(15, segment.Offset);
		Assert.Equal(0, segment.Length);
	}

	[Fact]
	public void deletion_around_a_read_only_segment_returns_the_two_outer_parts() // DeletionAroundReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 20, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(15, 16)).ToList();

		//Assert
		Assert.Equal(2, result.Count);
		Assert.Equal(15, result[0].Offset);
		Assert.Equal(5, result[0].Length);
		Assert.Equal(25, result[1].Offset);
		Assert.Equal(6, result[1].Length);
	}

	[Fact]
	public void deleting_the_last_character_of_a_read_only_segment_is_impossible() // DeleteLastCharacterInReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 20, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(24, 1)).ToList();

		//Assert
		Assert.Empty(result);
		/* // we would need this result for the old Backspace code so that the last character doesn't get selected:
		Assert.Equal(1, result.Count);
		Assert.Equal(25, result[0].Offset);
		Assert.Equal(0, result[0].Length);*/
	}

	[Fact]
	public void deleting_the_first_character_of_a_read_only_segment_is_impossible() // DeleteFirstCharacterInReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 20, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(20, 1)).ToList();

		//Assert
		Assert.Empty(result);
		/* // we would need this result for the old Delete code so that the first character doesn't get selected:
		Assert.Equal(1, result.Count);
		Assert.Equal(2, result[0].Offset);
		Assert.Equal(0, result[0].Length);*/
	}

	[Fact]
	public void deleting_a_whole_read_only_segment_is_impossible() // DeleteWholeReadOnlySegment
	{
		//Arrange
		segments.Add(new TextSegment { StartOffset = 20, Length = 5 });

		//Act
		var result = provider.GetDeletableSegments(new SimpleSegment(20, 5)).ToList();

		//Assert
		Assert.Empty(result);
	}
}
