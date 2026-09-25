#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Engine;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Folding;

/// <summary>
/// A <see cref="TextView"/> as a folding host (WPE1 C8): the adapter FoldingManager keeps for each text view it is added
/// to, so the manager itself names no view type.
/// </summary>
internal sealed class TextViewFoldingHost : IFoldingHost
{
	internal TextViewFoldingHost(TextView textView) => TextView = textView;

	/// <summary>The text view.</summary>
	internal TextView TextView { get; }

	/// <inheritdoc/>
	public void Redraw() => TextView.Redraw();

	/// <inheritdoc/>
	public void Redraw(ISegment segment) => TextView.Redraw(segment);

	/// <inheritdoc/>
	public CollapsedLineSection CollapseLines(DocumentLine start, DocumentLine end) => TextView.CollapseLines(start, end);
}
