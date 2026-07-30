#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using CodeBrix.Platform.UI.TextLayout;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLineText.cs in the AvalonEdit repo (MIT).
//CreateTextRun became BuildLayoutText: the element contributes the document substring to the visual
//line's layout text (one layout character per visual column), and the engine handles word-wrapping
//itself - so the partial-run construction for wrapped lines and GetPrecedingText (WPF's bidi
//word-wrap hook) are gone.

/// <summary>
/// VisualLineElement that represents a piece of text.
/// </summary>
public class VisualLineText : VisualLineElement
{
	readonly VisualLine parentVisualLine;

	/// <summary>
	/// Gets the parent visual line.
	/// </summary>
	public VisualLine ParentVisualLine {
		get { return parentVisualLine; }
	}

	/// <summary>
	/// Creates a visual line text element with the specified length.
	/// It uses the <see cref="ITextRunConstructionContext.VisualLine"/> and its
	/// <see cref="VisualLineElement.RelativeTextOffset"/> to find the actual text string.
	/// </summary>
	public VisualLineText(VisualLine parentVisualLine, int length) : base(length, length)
	{
		if (parentVisualLine == null)
			throw new ArgumentNullException(nameof(parentVisualLine));
		this.parentVisualLine = parentVisualLine;
	}

	/// <summary>
	/// Override this method to control the type of new VisualLineText instances when
	/// the visual line is split due to syntax highlighting.
	/// </summary>
	protected virtual VisualLineText CreateInstance(int length)
	{
		return new VisualLineText(parentVisualLine, length);
	}

	/// <inheritdoc/>
	public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
	{
		if (layoutText == null)
			throw new ArgumentNullException(nameof(layoutText));
		if (context == null)
			throw new ArgumentNullException(nameof(context));

		StringSegment text = context.GetText(
			context.VisualLine.FirstDocumentLine.Offset + RelativeTextOffset, DocumentLength);
		layoutText.Append(text.Text, text.Offset, text.Count);
		return CreateTextRunDescriptor(text.Text.Substring(text.Offset, text.Count));
	}

	/// <inheritdoc/>
	public override bool IsWhitespace(int visualColumn)
	{
		int offset = visualColumn - this.VisualColumn + parentVisualLine.FirstDocumentLine.Offset + this.RelativeTextOffset;
		return char.IsWhiteSpace(parentVisualLine.Document.GetCharAt(offset));
	}

	/// <inheritdoc/>
	public override bool CanSplit {
		get { return true; }
	}

	/// <inheritdoc/>
	public override void Split(int splitVisualColumn, IList<VisualLineElement> elements, int elementIndex)
	{
		if (splitVisualColumn <= VisualColumn || splitVisualColumn >= VisualColumn + VisualLength)
			throw new ArgumentOutOfRangeException(nameof(splitVisualColumn), splitVisualColumn, "Value must be between " + (VisualColumn + 1) + " and " + (VisualColumn + VisualLength - 1));
		if (elements == null)
			throw new ArgumentNullException(nameof(elements));
		if (elements[elementIndex] != this)
			throw new ArgumentException("Invalid elementIndex - couldn't find this element at the index");
		int relativeSplitPos = splitVisualColumn - VisualColumn;
		VisualLineText splitPart = CreateInstance(DocumentLength - relativeSplitPos);
		SplitHelper(this, splitPart, splitVisualColumn, relativeSplitPos + RelativeTextOffset);
		elements.Insert(elementIndex + 1, splitPart);
	}

	/// <inheritdoc/>
	public override int GetRelativeOffset(int visualColumn)
	{
		return this.RelativeTextOffset + visualColumn - this.VisualColumn;
	}

	/// <inheritdoc/>
	public override int GetVisualColumn(int relativeTextOffset)
	{
		return this.VisualColumn + relativeTextOffset - this.RelativeTextOffset;
	}

	/// <inheritdoc/>
	public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		int textOffset = parentVisualLine.StartOffset + this.RelativeTextOffset;
		int pos = TextUtilities.GetNextCaretPosition(parentVisualLine.Document, textOffset + visualColumn - this.VisualColumn, direction, mode);
		if (pos < textOffset || pos > textOffset + this.DocumentLength)
			return -1;
		else
			return this.VisualColumn + pos - textOffset;
	}
}
