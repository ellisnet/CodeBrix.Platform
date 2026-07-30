#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/UndoStackTests.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Exercises undo grouping on <see cref="UndoStack"/>: continued groups merge with the previous
/// group only when that group actually contains undoable changes.
/// </summary>
public class UndoStackTests
{
	[Fact]
	public void continued_undo_group_merges_with_previous_group() // ContinueUndoGroup
	{
		//Arrange
		var doc = new TextDocument();
		doc.Insert(0, "a");

		//Act
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();

		//Assert
		Assert.Equal("", doc.Text);
	}

	[Fact]
	public void continued_undo_group_does_not_merge_past_an_empty_group() // ContinueEmptyUndoGroup
	{
		//Arrange
		var doc = new TextDocument();
		doc.Insert(0, "a");

		//Act
		doc.UndoStack.StartUndoGroup();
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();

		//Assert
		Assert.Equal("a", doc.Text);
	}

	[Fact]
	public void continued_undo_group_does_not_merge_past_a_group_with_only_optional_entries() // ContinueEmptyUndoGroup_WithOptionalEntries
	{
		//Arrange
		var doc = new TextDocument();
		doc.Insert(0, "a");

		//Act
		doc.UndoStack.StartUndoGroup();
		doc.UndoStack.PushOptional(new StubUndoableAction());
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();

		//Assert
		Assert.Equal("a", doc.Text);
	}

	[Fact]
	public void empty_continuation_group_still_allows_merging_into_the_previous_group() // EmptyContinuationGroup
	{
		//Arrange
		var doc = new TextDocument();
		doc.Insert(0, "a");

		//Act
		doc.UndoStack.StartContinuedUndoGroup();
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.StartContinuedUndoGroup();
		doc.Insert(1, "b");
		doc.UndoStack.EndUndoGroup();
		doc.UndoStack.Undo();

		//Assert
		Assert.Equal("", doc.Text);
	}

	sealed class StubUndoableAction : IUndoableOperation
	{
		public void Undo()
		{
		}

		public void Redo()
		{
		}
	}
}
