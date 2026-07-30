#nullable enable

using System;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Document;

//was previously: ICSharpCode.AvalonEdit.Tests/Document/ChangeTrackingTest.cs in the AvalonEdit repo (MIT).
//ITextSource.Version is annotated nullable in the port; document snapshots always carry a version,
//so the tests assert non-null before using it.

/// <summary>
/// Exercises snapshot version tracking on <see cref="TextDocument"/>: age comparison and forward /
/// backward change streams between snapshots.
/// </summary>
public class ChangeTrackingTest
{
	[Fact]
	public void snapshots_without_changes_share_the_same_version_age() // NoChanges
	{
		//Arrange
		TextDocument document = new TextDocument("initial text");

		//Act
		ITextSource snapshot1 = document.CreateSnapshot();
		ITextSource snapshot2 = document.CreateSnapshot();

		//Assert
		Assert.NotNull(snapshot1.Version);
		Assert.NotNull(snapshot2.Version);
		Assert.Equal(0, snapshot1.Version.CompareAge(snapshot2.Version));
		Assert.Empty(snapshot1.Version.GetChangesTo(snapshot2.Version));
		Assert.Equal(document.Text, snapshot1.Text);
		Assert.Equal(document.Text, snapshot2.Text);
	}

	[Fact]
	public void forward_changes_replay_the_edits_between_snapshots() // ForwardChanges
	{
		//Arrange
		TextDocument document = new TextDocument("initial text");
		ITextSource snapshot1 = document.CreateSnapshot();

		//Act
		document.Replace(0, 7, "nw");
		document.Insert(1, "e");
		ITextSource snapshot2 = document.CreateSnapshot();

		//Assert
		Assert.NotNull(snapshot1.Version);
		Assert.NotNull(snapshot2.Version);
		Assert.Equal(-1, snapshot1.Version.CompareAge(snapshot2.Version));
		TextChangeEventArgs[] arr = snapshot1.Version.GetChangesTo(snapshot2.Version).ToArray();
		Assert.Equal(2, arr.Length);
		Assert.Equal("nw", arr[0].InsertedText.Text);
		Assert.Equal("e", arr[1].InsertedText.Text);

		Assert.Equal("initial text", snapshot1.Text);
		Assert.Equal("new text", snapshot2.Text);
	}

	[Fact]
	public void backward_changes_replay_the_inverse_edits_between_snapshots() // BackwardChanges
	{
		//Arrange
		TextDocument document = new TextDocument("initial text");
		ITextSource snapshot1 = document.CreateSnapshot();

		//Act
		document.Replace(0, 7, "nw");
		document.Insert(1, "e");
		ITextSource snapshot2 = document.CreateSnapshot();

		//Assert
		Assert.NotNull(snapshot1.Version);
		Assert.NotNull(snapshot2.Version);
		Assert.Equal(1, snapshot2.Version.CompareAge(snapshot1.Version));
		TextChangeEventArgs[] arr = snapshot2.Version.GetChangesTo(snapshot1.Version).ToArray();
		Assert.Equal(2, arr.Length);
		Assert.Equal("", arr[0].InsertedText.Text);
		Assert.Equal("initial", arr[1].InsertedText.Text);

		Assert.Equal("initial text", snapshot1.Text);
		Assert.Equal("new text", snapshot2.Text);
	}
}
