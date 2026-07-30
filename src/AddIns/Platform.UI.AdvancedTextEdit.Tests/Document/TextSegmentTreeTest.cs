#nullable enable

using System;
using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/TextSegmentTreeTest.cs in the AvalonEdit repo (MIT).
//NUnit [OneTimeSetUp] seeded a Random from Environment.TickCount once per fixture; that behavior is
//kept via a static field (xUnit creates a new class instance per test). The seed stays time-based,
//exactly as upstream, and is written to the console for reproducing failures.

/// <summary>
/// Exercises <see cref="TextSegmentCollection{T}"/>: lookups, duplicate handling, offset updates
/// for document changes, and randomized add/remove/change stress runs.
/// </summary>
public class TextSegmentTreeTest
{
	static readonly int seed;
	static readonly Random rnd;

	static TextSegmentTreeTest() // FixtureSetup
	{
		seed = Environment.TickCount;
		Console.WriteLine("TextSegmentTreeTest Seed: " + seed);
		rnd = new Random(seed);
	}

	sealed class TestTextSegment : TextSegment
	{
		internal int ExpectedOffset, ExpectedLength;

		public TestTextSegment(int expectedOffset, int expectedLength)
		{
			this.ExpectedOffset = expectedOffset;
			this.ExpectedLength = expectedLength;
			this.StartOffset = expectedOffset;
			this.Length = expectedLength;
		}
	}

	readonly TextSegmentCollection<TestTextSegment> tree;
	readonly List<TestTextSegment> expectedSegments;

	public TextSegmentTreeTest() // SetUp
	{
		tree = new TextSegmentCollection<TestTextSegment>();
		expectedSegments = new List<TestTextSegment>();
	}

	[Fact]
	public void finding_in_empty_tree_returns_nothing() // FindInEmptyTree
	{
		//Arrange (empty tree from constructor)

		//Act + Assert
		Assert.Null(tree.FindFirstSegmentWithStartAfter(0));
		Assert.Empty(tree.FindSegmentsContaining(0));
		Assert.Empty(tree.FindOverlappingSegments(10, 20));
	}

	[Fact]
	public void find_first_segment_with_start_after_returns_next_segment_by_start_offset() // FindFirstSegmentWithStartAfter
	{
		//Arrange
		var s1 = new TestTextSegment(5, 10);
		var s2 = new TestTextSegment(10, 10);
		tree.Add(s1);
		tree.Add(s2);

		//Act + Assert
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(-100));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(0));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(4));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(5));
		Assert.Same(s2, tree.FindFirstSegmentWithStartAfter(6));
		Assert.Same(s2, tree.FindFirstSegmentWithStartAfter(9));
		Assert.Same(s2, tree.FindFirstSegmentWithStartAfter(10));
		Assert.Null(tree.FindFirstSegmentWithStartAfter(11));
		Assert.Null(tree.FindFirstSegmentWithStartAfter(100));
	}

	[Fact]
	public void find_first_segment_with_start_after_handles_duplicate_start_offsets() // FindFirstSegmentWithStartAfterWithDuplicates
	{
		//Arrange
		var s1 = new TestTextSegment(5, 10);
		var s1b = new TestTextSegment(5, 7);
		var s2 = new TestTextSegment(10, 10);
		var s2b = new TestTextSegment(10, 7);
		tree.Add(s1);
		tree.Add(s1b);
		tree.Add(s2);
		tree.Add(s2b);

		//Act + Assert
		Assert.Same(s1b, tree.GetNextSegment(s1));
		Assert.Same(s2b, tree.GetNextSegment(s2));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(-100));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(0));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(4));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(5));
		Assert.Same(s2, tree.FindFirstSegmentWithStartAfter(6));
		Assert.Same(s2, tree.FindFirstSegmentWithStartAfter(9));
		Assert.Same(s2, tree.FindFirstSegmentWithStartAfter(10));
		Assert.Null(tree.FindFirstSegmentWithStartAfter(11));
		Assert.Null(tree.FindFirstSegmentWithStartAfter(100));
	}

	[Fact]
	public void find_first_segment_with_start_after_handles_many_segments_at_same_offset() // FindFirstSegmentWithStartAfterWithDuplicates2
	{
		//Arrange
		var s1 = new TestTextSegment(5, 1);
		var s2 = new TestTextSegment(5, 2);
		var s3 = new TestTextSegment(5, 3);
		var s4 = new TestTextSegment(5, 4);
		tree.Add(s1);
		tree.Add(s2);
		tree.Add(s3);
		tree.Add(s4);

		//Act + Assert
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(0));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(1));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(4));
		Assert.Same(s1, tree.FindFirstSegmentWithStartAfter(5));
		Assert.Null(tree.FindFirstSegmentWithStartAfter(6));
	}

	TestTextSegment AddSegment(int offset, int length)
	{
		TestTextSegment s = new TestTextSegment(offset, length);
		tree.Add(s);
		expectedSegments.Add(s);
		return s;
	}

	void RemoveSegment(TestTextSegment s)
	{
		expectedSegments.Remove(s);
		tree.Remove(s);
	}

	void TestRetrieval(int offset, int length)
	{
		HashSet<TestTextSegment> actual = new HashSet<TestTextSegment>(tree.FindOverlappingSegments(offset, length));
		HashSet<TestTextSegment> expected = new HashSet<TestTextSegment>();
		foreach (TestTextSegment e in expectedSegments)
		{
			if (e.ExpectedOffset + e.ExpectedLength < offset)
			{
				continue;
			}
			if (e.ExpectedOffset > offset + length)
			{
				continue;
			}
			expected.Add(e);
		}
		Assert.True(actual.IsSubsetOf(expected));
		Assert.True(expected.IsSubsetOf(actual));
	}

	void CheckSegments()
	{
		Assert.Equal(expectedSegments.Count, tree.Count);
		foreach (TestTextSegment s in expectedSegments)
		{
			Assert.Equal(s.ExpectedOffset, s.StartOffset);
			Assert.Equal(s.ExpectedLength, s.Length);
		}
	}

	[Fact]
	public void adding_segments_keeps_their_offsets_and_lengths() // AddSegments
	{
		//Arrange + Act
		AddSegment(10, 20);
		AddSegment(15, 10);

		//Assert
		CheckSegments();
	}

	void ChangeDocument(OffsetChangeMapEntry change)
	{
		tree.UpdateOffsets(change);
		foreach (TestTextSegment s in expectedSegments)
		{
			int endOffset = s.ExpectedOffset + s.ExpectedLength;
			s.ExpectedOffset = change.GetNewOffset(s.ExpectedOffset, AnchorMovementType.AfterInsertion);
			s.ExpectedLength = Math.Max(0, change.GetNewOffset(endOffset, AnchorMovementType.BeforeInsertion) - s.ExpectedOffset);
		}
	}

	[Fact]
	public void insertion_before_all_segments_moves_them() // InsertionBeforeAllSegments
	{
		//Arrange
		AddSegment(10, 20);
		AddSegment(15, 10);

		//Act
		ChangeDocument(new OffsetChangeMapEntry(5, 0, 2));

		//Assert
		CheckSegments();
	}

	[Fact]
	public void replacement_before_all_segments_touching_first_segment_updates_offsets() // ReplacementBeforeAllSegmentsTouchingFirstSegment
	{
		//Arrange
		AddSegment(10, 20);
		AddSegment(15, 10);

		//Act
		ChangeDocument(new OffsetChangeMapEntry(5, 5, 2));

		//Assert
		CheckSegments();
	}

	[Fact]
	public void insertion_after_all_segments_leaves_them_unchanged() // InsertionAfterAllSegments
	{
		//Arrange
		AddSegment(10, 20);
		AddSegment(15, 10);

		//Act
		ChangeDocument(new OffsetChangeMapEntry(45, 0, 2));

		//Assert
		CheckSegments();
	}

	[Fact]
	public void replacement_overlapping_with_start_of_segment_updates_offsets() // ReplacementOverlappingWithStartOfSegment
	{
		//Arrange
		AddSegment(10, 20);
		AddSegment(15, 10);

		//Act
		ChangeDocument(new OffsetChangeMapEntry(9, 7, 2));

		//Assert
		CheckSegments();
	}

	[Fact]
	public void replacement_of_whole_segment_updates_offsets() // ReplacementOfWholeSegment
	{
		//Arrange
		AddSegment(10, 20);
		AddSegment(15, 10);

		//Act
		ChangeDocument(new OffsetChangeMapEntry(10, 20, 30));

		//Assert
		CheckSegments();
	}

	[Fact]
	public void replacement_at_end_of_segment_updates_offsets() // ReplacementAtEndOfSegment
	{
		//Arrange
		AddSegment(10, 20);
		AddSegment(15, 10);

		//Act
		ChangeDocument(new OffsetChangeMapEntry(24, 6, 10));

		//Assert
		CheckSegments();
	}

	[Fact]
	public void randomized_add_remove_without_document_changes_keeps_tree_consistent() // RandomizedNoDocumentChanges
	{
		//Arrange (seeded random, empty tree)

		//Act + Assert
		for (int i = 0; i < 1000; i++)
		{
			switch (rnd.Next(3))
			{
				case 0:
					AddSegment(rnd.Next(500), rnd.Next(30));
					break;
				case 1:
					AddSegment(rnd.Next(500), rnd.Next(300));
					break;
				case 2:
					if (tree.Count > 0)
					{
						RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
					}
					break;
			}
			CheckSegments();
		}
	}

	[Fact]
	public void randomized_add_remove_in_short_document_copes_with_identical_segments() // RandomizedCloseNoDocumentChanges
	{
		//Arrange (seeded random, empty tree)

		//Act + Assert - lots of segments in a short document, testing multiple identical segments
		for (int i = 0; i < 1000; i++)
		{
			switch (rnd.Next(3))
			{
				case 0:
					AddSegment(rnd.Next(20), rnd.Next(10));
					break;
				case 1:
					AddSegment(rnd.Next(20), rnd.Next(20));
					break;
				case 2:
					if (tree.Count > 0)
					{
						RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
					}
					break;
			}
			CheckSegments();
		}
	}

	[Fact]
	public void randomized_retrieval_finds_exactly_the_overlapping_segments() // RandomizedRetrievalTest
	{
		//Arrange
		for (int i = 0; i < 1000; i++)
		{
			AddSegment(rnd.Next(500), rnd.Next(300));
		}
		CheckSegments();

		//Act + Assert
		for (int i = 0; i < 1000; i++)
		{
			TestRetrieval(rnd.Next(1000) - 100, rnd.Next(500));
		}
	}

	[Fact]
	public void randomized_operations_with_document_changes_keep_tree_consistent() // RandomizedWithDocumentChanges
	{
		//Arrange (seeded random, empty tree)

		//Act + Assert
		for (int i = 0; i < 500; i++)
		{
			switch (rnd.Next(6))
			{
				case 0:
					AddSegment(rnd.Next(500), rnd.Next(30));
					break;
				case 1:
					AddSegment(rnd.Next(500), rnd.Next(300));
					break;
				case 2:
					if (tree.Count > 0)
					{
						RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
					}
					break;
				case 3:
					ChangeDocument(new OffsetChangeMapEntry(rnd.Next(800), rnd.Next(50), rnd.Next(50)));
					break;
				case 4:
					ChangeDocument(new OffsetChangeMapEntry(rnd.Next(800), 0, rnd.Next(50)));
					break;
				case 5:
					ChangeDocument(new OffsetChangeMapEntry(rnd.Next(800), rnd.Next(50), 0));
					break;
			}
			CheckSegments();
		}
	}

	[Fact]
	public void randomized_operations_with_document_changes_in_short_document_keep_tree_consistent() // RandomizedWithDocumentChangesClose
	{
		//Arrange (seeded random, empty tree)

		//Act + Assert
		for (int i = 0; i < 500; i++)
		{
			switch (rnd.Next(6))
			{
				case 0:
					AddSegment(rnd.Next(50), rnd.Next(30));
					break;
				case 1:
					AddSegment(rnd.Next(50), rnd.Next(3));
					break;
				case 2:
					if (tree.Count > 0)
					{
						RemoveSegment(expectedSegments[rnd.Next(tree.Count)]);
					}
					break;
				case 3:
					ChangeDocument(new OffsetChangeMapEntry(rnd.Next(80), rnd.Next(10), rnd.Next(10)));
					break;
				case 4:
					ChangeDocument(new OffsetChangeMapEntry(rnd.Next(80), 0, rnd.Next(10)));
					break;
				case 5:
					ChangeDocument(new OffsetChangeMapEntry(rnd.Next(80), rnd.Next(10), 0));
					break;
			}
			CheckSegments();
		}
	}
}
