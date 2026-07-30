#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/NoReadOnlySections.cs in the AvalonEdit repo (MIT).
//Relies on ExtensionMethods.Sequence from this port's Utils namespace (ported in the Utils wave).

/// <summary>
/// <see cref="IReadOnlySectionProvider"/> that has no read-only sections; all text is editable.
/// </summary>
internal sealed class NoReadOnlySections : IReadOnlySectionProvider
{
	public static readonly NoReadOnlySections Instance = new NoReadOnlySections();

	public bool CanInsert(int offset)
	{
		return true;
	}

	public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
	{
		if (segment == null)
		{
			throw new ArgumentNullException(nameof(segment));
		}

		// the segment is always deletable
		return ExtensionMethods.Sequence(segment);
	}
}

/// <summary>
/// <see cref="IReadOnlySectionProvider"/> that completely disables editing.
/// </summary>
internal sealed class ReadOnlySectionDocument : IReadOnlySectionProvider
{
	public static readonly ReadOnlySectionDocument Instance = new ReadOnlySectionDocument();

	public bool CanInsert(int offset)
	{
		return false;
	}

	public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
	{
		return Enumerable.Empty<ISegment>();
	}
}
