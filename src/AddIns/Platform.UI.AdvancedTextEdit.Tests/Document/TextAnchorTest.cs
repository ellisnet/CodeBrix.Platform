#nullable enable

using System;
using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/TextAnchorTest.cs in the AvalonEdit repo (MIT).
//NUnit [OneTimeSetUp] seeded a Random from Environment.TickCount once per fixture; kept via a static
//field (time-based seed, printed to the console, exactly as upstream). In the drag-drop churn test,
//GC.KeepAlive calls replace the bare unused locals to express the same anchor lifetimes without
//tripping the compiler's unused-value warning.

/// <summary>
/// Exercises <see cref="TextAnchor"/>: movement types, deletion survival, garbage collection of
/// unreferenced anchors, and randomized insert/remove/replace churn.
/// </summary>
public class TextAnchorTest
{
	static readonly int seed;
	static readonly Random rnd;

	static TextAnchorTest() // FixtureSetup
	{
		seed = Environment.TickCount;
		Console.WriteLine("TextAnchorTest Seed: " + seed);
		rnd = new Random(seed);
	}

	readonly TextDocument document;

	public TextAnchorTest() // SetUp
	{
		document = new TextDocument();
	}

	[Fact]
	public void anchors_in_empty_document_move_according_to_movement_type() // AnchorInEmptyDocument
	{
		//Arrange
		TextAnchor a1 = document.CreateAnchor(0);
		TextAnchor a2 = document.CreateAnchor(0);
		a1.MovementType = AnchorMovementType.BeforeInsertion;
		a2.MovementType = AnchorMovementType.AfterInsertion;
		Assert.Equal(0, a1.Offset);
		Assert.Equal(0, a2.Offset);

		//Act
		document.Insert(0, "x");

		//Assert
		Assert.Equal(0, a1.Offset);
		Assert.Equal(1, a2.Offset);
	}

	[Fact]
	public void anchors_with_survive_deletion_move_to_deletion_start_instead_of_dying() // AnchorsSurviveDeletion
	{
		//Arrange
		document.Text = new string(' ', 10);
		TextAnchor[] a1 = new TextAnchor[11];
		TextAnchor[] a2 = new TextAnchor[11];
		for (int i = 0; i < 11; i++)
		{
			a1[i] = document.CreateAnchor(i);
			a1[i].SurviveDeletion = true;
			a2[i] = document.CreateAnchor(i);
			a2[i].SurviveDeletion = false;
		}
		for (int i = 0; i < 11; i++)
		{
			Assert.Equal(i, a1[i].Offset);
			Assert.Equal(i, a2[i].Offset);
		}

		//Act
		document.Remove(1, 8);

		//Assert
		for (int i = 0; i < 11; i++)
		{
			if (i <= 1)
			{
				Assert.False(a1[i].IsDeleted);
				Assert.False(a2[i].IsDeleted);
				Assert.Equal(i, a1[i].Offset);
				Assert.Equal(i, a2[i].Offset);
			}
			else if (i <= 8)
			{
				Assert.False(a1[i].IsDeleted);
				Assert.True(a2[i].IsDeleted);
				Assert.Equal(1, a1[i].Offset);
			}
			else
			{
				Assert.False(a1[i].IsDeleted);
				Assert.False(a2[i].IsDeleted);
				Assert.Equal(i - 8, a1[i].Offset);
				Assert.Equal(i - 8, a2[i].Offset);
			}
		}
	}

	[Fact]
	public void created_anchors_keep_their_offsets() // CreateAnchors
	{
		//Arrange
		List<TextAnchor> anchors = new List<TextAnchor>();
		List<int> expectedOffsets = new List<int>();
		document.Text = new string(' ', 1000);

		//Act
		for (int i = 0; i < 1000; i++)
		{
			int offset = rnd.Next(1000);
			anchors.Add(document.CreateAnchor(offset));
			expectedOffsets.Add(offset);
		}

		//Assert
		for (int i = 0; i < anchors.Count; i++)
		{
			Assert.Equal(expectedOffsets[i], anchors[i].Offset);
		}
		GC.KeepAlive(anchors);
	}

	[Fact]
	public void surviving_anchors_keep_offsets_while_unreferenced_anchors_are_collected() // CreateAndGCAnchors
	{
		//Arrange
		List<TextAnchor> anchors = new List<TextAnchor>();
		List<int> expectedOffsets = new List<int>();
		document.Text = new string(' ', 1000);

		//Act + Assert
		for (int t = 0; t < 250; t++)
		{
			int c = rnd.Next(50);
			if (rnd.Next(2) == 0)
			{
				for (int i = 0; i < c; i++)
				{
					int offset = rnd.Next(1000);
					anchors.Add(document.CreateAnchor(offset));
					expectedOffsets.Add(offset);
				}
			}
			else if (c <= anchors.Count)
			{
				anchors.RemoveRange(0, c);
				expectedOffsets.RemoveRange(0, c);
				GC.Collect();
			}
			for (int j = 0; j < anchors.Count; j++)
			{
				Assert.Equal(expectedOffsets[j], anchors[j].Offset);
			}
		}
		GC.KeepAlive(anchors);
	}

	[Fact]
	public void replace_moves_anchors_according_to_movement_type_and_survival() // MoveAnchorsDuringReplace
	{
		//Arrange
		document.Text = "abcd";
		TextAnchor start = document.CreateAnchor(1);
		TextAnchor middleDeletable = document.CreateAnchor(2);
		TextAnchor middleSurvivorLeft = document.CreateAnchor(2);
		middleSurvivorLeft.SurviveDeletion = true;
		middleSurvivorLeft.MovementType = AnchorMovementType.BeforeInsertion;
		TextAnchor middleSurvivorRight = document.CreateAnchor(2);
		middleSurvivorRight.SurviveDeletion = true;
		middleSurvivorRight.MovementType = AnchorMovementType.AfterInsertion;
		TextAnchor end = document.CreateAnchor(3);

		//Act
		document.Replace(1, 2, "BxC");

		//Assert
		Assert.Equal(1, start.Offset);
		Assert.True(middleDeletable.IsDeleted);
		Assert.Equal(1, middleSurvivorLeft.Offset);
		Assert.Equal(4, middleSurvivorRight.Offset);
		Assert.Equal(4, end.Offset);
	}

	[Fact]
	public void randomized_document_changes_move_anchors_like_the_reference_model() // CreateAndMoveAnchors
	{
		//Arrange
		List<TextAnchor> anchors = new List<TextAnchor>();
		List<int> expectedOffsets = new List<int>();
		document.Text = new string(' ', 1000);

		//Act + Assert
		for (int t = 0; t < 250; t++)
		{
			int c = rnd.Next(50);
			switch (rnd.Next(5))
			{
				case 0:
					for (int i = 0; i < c; i++)
					{
						int offset = rnd.Next(document.TextLength);
						TextAnchor anchor = document.CreateAnchor(offset);
						if (rnd.Next(2) == 0)
						{
							anchor.MovementType = AnchorMovementType.BeforeInsertion;
						}
						else
						{
							anchor.MovementType = AnchorMovementType.AfterInsertion;
						}
						anchor.SurviveDeletion = rnd.Next(2) == 0;
						anchors.Add(anchor);
						expectedOffsets.Add(offset);
					}
					break;
				case 1:
					if (c <= anchors.Count)
					{
						anchors.RemoveRange(0, c);
						expectedOffsets.RemoveRange(0, c);
						GC.Collect();
					}
					break;
				case 2:
					int insertOffset = rnd.Next(document.TextLength);
					int insertLength = rnd.Next(1000);
					document.Insert(insertOffset, new string(' ', insertLength));
					for (int i = 0; i < anchors.Count; i++)
					{
						if (anchors[i].MovementType == AnchorMovementType.BeforeInsertion)
						{
							if (expectedOffsets[i] > insertOffset)
							{
								expectedOffsets[i] += insertLength;
							}
						}
						else
						{
							if (expectedOffsets[i] >= insertOffset)
							{
								expectedOffsets[i] += insertLength;
							}
						}
					}
					break;
				case 3:
					int removalOffset = rnd.Next(document.TextLength);
					int removalLength = rnd.Next(document.TextLength - removalOffset);
					document.Remove(removalOffset, removalLength);
					for (int i = anchors.Count - 1; i >= 0; i--)
					{
						if (expectedOffsets[i] > removalOffset && expectedOffsets[i] < removalOffset + removalLength)
						{
							if (anchors[i].SurviveDeletion)
							{
								expectedOffsets[i] = removalOffset;
							}
							else
							{
								Assert.True(anchors[i].IsDeleted);
								anchors.RemoveAt(i);
								expectedOffsets.RemoveAt(i);
							}
						}
						else if (expectedOffsets[i] > removalOffset)
						{
							expectedOffsets[i] -= removalLength;
						}
					}
					break;
				case 4:
					int replaceOffset = rnd.Next(document.TextLength);
					int replaceRemovalLength = rnd.Next(document.TextLength - replaceOffset);
					int replaceInsertLength = rnd.Next(1000);
					document.Replace(replaceOffset, replaceRemovalLength, new string(' ', replaceInsertLength));
					for (int i = anchors.Count - 1; i >= 0; i--)
					{
						if (expectedOffsets[i] > replaceOffset && expectedOffsets[i] < replaceOffset + replaceRemovalLength)
						{
							if (anchors[i].SurviveDeletion)
							{
								if (anchors[i].MovementType == AnchorMovementType.AfterInsertion)
								{
									expectedOffsets[i] = replaceOffset + replaceInsertLength;
								}
								else
								{
									expectedOffsets[i] = replaceOffset;
								}
							}
							else
							{
								Assert.True(anchors[i].IsDeleted);
								anchors.RemoveAt(i);
								expectedOffsets.RemoveAt(i);
							}
						}
						else if (expectedOffsets[i] > replaceOffset)
						{
							expectedOffsets[i] += replaceInsertLength - replaceRemovalLength;
						}
						else if (expectedOffsets[i] == replaceOffset && replaceRemovalLength == 0 && anchors[i].MovementType == AnchorMovementType.AfterInsertion)
						{
							expectedOffsets[i] += replaceInsertLength - replaceRemovalLength;
						}
					}
					break;
			}
			Assert.Equal(anchors.Count, expectedOffsets.Count);
			for (int j = 0; j < anchors.Count; j++)
			{
				Assert.Equal(expectedOffsets[j], anchors[j].Offset);
			}
		}
		GC.KeepAlive(anchors);
	}

	[Fact]
	public void repeated_drag_drop_style_churn_does_not_corrupt_the_anchor_tree() // RepeatedTextDragDrop
	{
		//Arrange
		document.Text = new string(' ', 1000);

		//Act + Assert (no exception; anchors are dropped and periodically collected)
		for (int i = 0; i < 20; i++)
		{
			TextAnchor? a = document.CreateAnchor(144);
			TextAnchor? b = document.CreateAnchor(157);
			document.Insert(128, new string('a', 13));
			document.Remove(157, 13);
			a = document.CreateAnchor(128);
			b = document.CreateAnchor(141);

			document.Insert(157, new string('b', 13));
			document.Remove(128, 13);

			GC.KeepAlive(a);
			GC.KeepAlive(b);
			a = null;
			b = null;
			if ((i % 5) == 0)
			{
				GC.Collect();
			}
		}
	}

	[Fact]
	public void character_replace_of_spaces_with_tab_moves_anchors_to_shortened_end() // ReplaceSpacesWithTab
	{
		//Arrange
		document.Text = "a    b";
		TextAnchor before = document.CreateAnchor(1);
		before.MovementType = AnchorMovementType.AfterInsertion;
		TextAnchor after = document.CreateAnchor(5);
		TextAnchor survivingMiddle = document.CreateAnchor(2);
		TextAnchor deletedMiddle = document.CreateAnchor(3);

		//Act
		document.Replace(1, 4, "\t", OffsetChangeMappingType.CharacterReplace);

		//Assert
		Assert.Equal("a\tb", document.Text);
		// yes, the movement is a bit strange; but that's how CharacterReplace works when the text gets shorter
		Assert.Equal(1, before.Offset);
		Assert.Equal(2, after.Offset);
		Assert.Equal(2, survivingMiddle.Offset);
		Assert.Equal(2, deletedMiddle.Offset);
	}

	[Fact]
	public void character_replace_of_two_characters_with_three_keeps_middle_anchors() // ReplaceTwoCharactersWithThree
	{
		//Arrange
		document.Text = "a12b";
		TextAnchor before = document.CreateAnchor(1);
		before.MovementType = AnchorMovementType.AfterInsertion;
		TextAnchor after = document.CreateAnchor(3);
		before.MovementType = AnchorMovementType.BeforeInsertion;
		TextAnchor middleB = document.CreateAnchor(2);
		before.MovementType = AnchorMovementType.BeforeInsertion;
		TextAnchor middleA = document.CreateAnchor(2);
		before.MovementType = AnchorMovementType.AfterInsertion;

		//Act
		document.Replace(1, 2, "123", OffsetChangeMappingType.CharacterReplace);

		//Assert
		Assert.Equal("a123b", document.Text);
		Assert.Equal(1, before.Offset);
		Assert.Equal(4, after.Offset);
		Assert.Equal(2, middleA.Offset);
		Assert.Equal(2, middleB.Offset);
	}
}
