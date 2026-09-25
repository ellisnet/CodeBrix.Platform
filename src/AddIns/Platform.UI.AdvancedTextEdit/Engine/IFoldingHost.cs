#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Engine;

/// <summary>
/// A view that shows a <see cref="Folding.FoldingManager"/>'s foldings (WPE1 C8): the manager collapses lines in each of
/// its hosts and asks them to redraw, and names no view type. On CodeBrix.Platform every host is a TextView (through
/// Folding/TextViewFoldingHost); a CodeBrix.Mobile editor view implements this itself.
/// </summary>
internal interface IFoldingHost
{
	/// <summary>Redraws everything.</summary>
	void Redraw();

	/// <summary>Redraws the lines a segment covers.</summary>
	/// <param name="segment">The segment (a folding).</param>
	void Redraw(ISegment segment);

	/// <summary>Collapses the lines from <paramref name="start"/> to <paramref name="end"/> (both inclusive).</summary>
	/// <param name="start">The first collapsed line.</param>
	/// <param name="end">The last collapsed line.</param>
	/// <returns>The collapsed section (uncollapse it to show the lines again).</returns>
	CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end);
}
