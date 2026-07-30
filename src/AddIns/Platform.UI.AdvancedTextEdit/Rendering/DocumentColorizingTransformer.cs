#nullable enable

using System;
using System.Diagnostics;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/DocumentColorizingTransformer.cs in the
//AvalonEdit repo (MIT). CurrentContext and the current document line are honestly nullable
//because they are only set during a Colorize() call; ChangeLinePart records that contract as a
//Debug.Assert.

/// <summary>
/// Base class for <see cref="IVisualLineTransformer"/> that helps
/// colorizing the document. Derived classes can work with document lines
/// and text offsets and this class takes care of the visual lines and visual columns.
/// </summary>
public abstract class DocumentColorizingTransformer : ColorizingTransformer
{
	DocumentLine? currentDocumentLine;
	int firstLineStart;
	int currentDocumentLineStartOffset, currentDocumentLineEndOffset;

	/// <summary>
	/// Gets the current ITextRunConstructionContext.
	/// Only non-null during a <see cref="Colorize"/> call.
	/// </summary>
	protected ITextRunConstructionContext? CurrentContext { get; private set; }

	/// <inheritdoc/>
	protected override void Colorize(ITextRunConstructionContext context)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		this.CurrentContext = context;

		currentDocumentLine = context.VisualLine.FirstDocumentLine;
		firstLineStart = currentDocumentLineStartOffset = currentDocumentLine.Offset;
		currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
		int currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;

		if (context.VisualLine.FirstDocumentLine == context.VisualLine.LastDocumentLine)
		{
			ColorizeLine(currentDocumentLine);
		}
		else
		{
			ColorizeLine(currentDocumentLine);
			// ColorizeLine modifies the visual line elements, loop through a copy of the line elements
			foreach (VisualLineElement e in context.VisualLine.Elements.ToArray())
			{
				int elementOffset = firstLineStart + e.RelativeTextOffset;
				if (elementOffset >= currentDocumentLineTotalEndOffset)
				{
					currentDocumentLine = context.Document.GetLineByOffset(elementOffset);
					currentDocumentLineStartOffset = currentDocumentLine.Offset;
					currentDocumentLineEndOffset = currentDocumentLineStartOffset + currentDocumentLine.Length;
					currentDocumentLineTotalEndOffset = currentDocumentLineStartOffset + currentDocumentLine.TotalLength;
					ColorizeLine(currentDocumentLine);
				}
			}
		}
		currentDocumentLine = null;
		this.CurrentContext = null;
	}

	/// <summary>
	/// Override this method to colorize an individual document line.
	/// </summary>
	protected abstract void ColorizeLine(DocumentLine line);

	/// <summary>
	/// Changes a part of the current document line.
	/// </summary>
	/// <param name="startOffset">Start offset of the region to change</param>
	/// <param name="endOffset">End offset of the region to change</param>
	/// <param name="action">Action that changes an individual <see cref="VisualLineElement"/>.</param>
	protected void ChangeLinePart(int startOffset, int endOffset, Action<VisualLineElement> action)
	{
		if (startOffset < currentDocumentLineStartOffset || startOffset > currentDocumentLineEndOffset)
			throw new ArgumentOutOfRangeException(nameof(startOffset), startOffset, "Value must be between " + currentDocumentLineStartOffset + " and " + currentDocumentLineEndOffset);
		if (endOffset < startOffset || endOffset > currentDocumentLineEndOffset)
			throw new ArgumentOutOfRangeException(nameof(endOffset), endOffset, "Value must be between " + startOffset + " and " + currentDocumentLineEndOffset);
		Debug.Assert(this.CurrentContext != null, "ChangeLinePart may only be called during a Colorize() call");
		VisualLine vl = this.CurrentContext.VisualLine;
		int visualStart = vl.GetVisualColumn(startOffset - firstLineStart);
		int visualEnd = vl.GetVisualColumn(endOffset - firstLineStart);
		if (visualStart < visualEnd)
		{
			ChangeVisualElements(visualStart, visualEnd, action);
		}
	}
}
