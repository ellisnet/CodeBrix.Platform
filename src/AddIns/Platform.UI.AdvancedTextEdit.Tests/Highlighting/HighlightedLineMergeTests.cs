#nullable enable

using System;
using System.Collections.Generic;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Highlighting;

//was previously: ICSharpCode.AvalonEdit.Tests/Highlighting/HighlightedLineMergeTests.cs in the AvalonEdit repo (MIT).
//Upstream also carries a fully commented-out "Automatic Test" region (an exhaustive merge
//combination sweep); it was dead code there and is not carried over.

/// <summary>
/// Exercises <see cref="HighlightedLine.MergeWith"/>: additional sections are split so they fit
/// the nesting structure of the base line's sections.
/// </summary>
public class HighlightedLineMergeTests
{
	readonly IDocument document = new TextDocument(new string(' ', 20));

	[Fact]
	public void merging_splits_the_additional_section_around_the_base_section() // SimpleMerge1
	{
		//Arrange
		HighlightedLine baseLine = new HighlightedLine(document, document.GetLineByNumber(1));
		baseLine.Sections.Add(MakeSection(0, 1, "B"));

		HighlightedLine additionalLine = new HighlightedLine(document, document.GetLineByNumber(1));
		additionalLine.Sections.Add(MakeSection(0, 2, "A"));

		//Act
		baseLine.MergeWith(additionalLine);

		//Assert - the additional section gets split up so that it fits into the tree structure
		Assert.Equal(
			new[]
			{
				MakeSection(0, 1, "B"),
				MakeSection(0, 1, "A"),
				MakeSection(1, 2, "A")
			},
			baseLine.Sections,
			new SectionComparer());
	}

	[Fact]
	public void merging_splits_the_additional_section_around_nested_base_sections() // SimpleMerge2
	{
		//Arrange
		HighlightedLine baseLine = new HighlightedLine(document, document.GetLineByNumber(1));
		baseLine.Sections.Add(MakeSection(0, 1, "B"));
		baseLine.Sections.Add(MakeSection(0, 1, "BN"));

		HighlightedLine additionalLine = new HighlightedLine(document, document.GetLineByNumber(1));
		additionalLine.Sections.Add(MakeSection(0, 2, "A"));

		//Act
		baseLine.MergeWith(additionalLine);

		//Assert - the additional section gets split up so that it fits into the tree structure
		Assert.Equal(
			new[]
			{
				MakeSection(0, 1, "B"),
				MakeSection(0, 1, "BN"),
				MakeSection(0, 1, "A"),
				MakeSection(1, 2, "A")
			},
			baseLine.Sections,
			new SectionComparer());
	}

	HighlightedSection MakeSection(int start, int end, string name)
	{
		return new HighlightedSection { Offset = start, Length = end - start, Color = new HighlightingColor { Name = name } };
	}

	sealed class SectionComparer : IEqualityComparer<HighlightedSection>
	{
		public bool Equals(HighlightedSection? a, HighlightedSection? b)
		{
			if (a is null || b is null)
			{
				return ReferenceEquals(a, b);
			}
			return a.Offset == b.Offset && a.Length == b.Length && a.Color?.Name == b.Color?.Name;
		}

		public int GetHashCode(HighlightedSection obj)
		{
			return obj.Offset;
		}
	}
}
