#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.TextLayout;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLineElement.cs in the AvalonEdit repo (MIT).
//The WPF TextRun factory methods are replaced by the layout-contribution model: CreateTextRun became
//BuildLayoutText (each element appends its layout text and returns one engine run descriptor), and
//GetPrecedingText (WPF's bidi word-wrap hook) is gone because the engine wraps the whole line itself.
//LayoutStart/LayoutLength record where the element's contribution landed in the layout text; they sit
//next to VisualColumn/VisualLength, which keep their original semantics (a tab is ONE visual column).
//OnQueryCursor is dropped: cursor shaping is handled by the view's hover logic, not per element.
//OnMouseDown/OnMouseUp became OnPointerPressed/OnPointerReleased per the framework's pointer model.

/// <summary>
/// Represents a visual element in the document.
/// </summary>
public abstract class VisualLineElement
{
	/// <summary>
	/// Creates a new VisualLineElement.
	/// </summary>
	/// <param name="visualLength">The length of the element in VisualLine coordinates. Must be positive.</param>
	/// <param name="documentLength">The length of the element in the document. Must be non-negative.</param>
	protected VisualLineElement(int visualLength, int documentLength)
	{
		if (visualLength < 1)
			throw new ArgumentOutOfRangeException(nameof(visualLength), visualLength, "Value must be at least 1");
		if (documentLength < 0)
			throw new ArgumentOutOfRangeException(nameof(documentLength), documentLength, "Value must be at least 0");
		this.VisualLength = visualLength;
		this.DocumentLength = documentLength;
	}

	/// <summary>
	/// Gets the length of this element in visual columns.
	/// </summary>
	public int VisualLength { get; private set; }

	/// <summary>
	/// Gets the length of this element in the text document.
	/// </summary>
	public int DocumentLength { get; private set; }

	/// <summary>
	/// Gets the visual column where this element starts.
	/// </summary>
	public int VisualColumn { get; internal set; }

	/// <summary>
	/// Gets the text offset where this element starts, relative to the start text offset of the visual line.
	/// </summary>
	public int RelativeTextOffset { get; internal set; }

	/// <summary>
	/// Gets the index in the visual line's layout text where this element's contribution starts.
	/// Valid after the visual line has been formatted.
	/// </summary>
	public int LayoutStart { get; internal set; }

	/// <summary>
	/// Gets the number of characters this element contributed to the visual line's layout text.
	/// Valid after the visual line has been formatted. This can differ from <see cref="VisualLength"/>:
	/// a tab occupies one visual column but expands to several layout characters.
	/// </summary>
	public int LayoutLength { get; internal set; }

	/// <summary>
	/// Gets the text run properties.
	/// A unique <see cref="VisualLineElementTextRunProperties"/> instance is used for each
	/// <see cref="VisualLineElement"/>; colorizing code may assume that modifying the
	/// <see cref="VisualLineElementTextRunProperties"/> will affect only this
	/// <see cref="VisualLineElement"/>.
	/// Null until the visual line's construction phase assigns the properties.
	/// </summary>
	public VisualLineElementTextRunProperties? TextRunProperties { get; private set; }

	/// <summary>
	/// Gets/sets the brush used for the background of this <see cref="VisualLineElement" />.
	/// </summary>
	public Brush? BackgroundBrush { get; set; }

	internal void SetTextRunProperties(VisualLineElementTextRunProperties p)
	{
		this.TextRunProperties = p;
	}

	/// <summary>
	/// Contributes this element's span of layout text.
	/// </summary>
	/// <param name="layoutText">
	/// The visual line's layout text under construction. The implementation must append exactly the
	/// text of the returned run descriptor - nothing more, nothing less. The current length of the
	/// builder is the element's layout column, which is what tab expansion measures against.
	/// </param>
	/// <param name="context">
	/// Context object that contains information relevant for the construction of the layout text.
	/// </param>
	/// <returns>
	/// The engine run descriptor for the contributed span. Use
	/// <see cref="CreateTextRunDescriptor(string)"/> to build it from <see cref="TextRunProperties"/>.
	/// </returns>
	/// <remarks>
	/// Called once per element, in visual order, each time the visual line is formatted. The visual
	/// line records where the contribution landed as <see cref="LayoutStart"/>/<see cref="LayoutLength"/>.
	/// </remarks>
	public abstract TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context);

	/// <summary>
	/// Builds an engine run descriptor for the given text from this element's
	/// <see cref="TextRunProperties"/> (font family, size, weight, style, stretch and foreground color).
	/// </summary>
	/// <param name="text">The run's text - exactly what the caller appends to the layout text.</param>
	/// <exception cref="InvalidOperationException">The element's text run properties are not assigned yet.</exception>
	protected TextRunDescriptor CreateTextRunDescriptor(string text)
	{
		return CreateTextRunDescriptor(text, null);
	}

	/// <summary>
	/// Builds an engine run descriptor for the given text from this element's
	/// <see cref="TextRunProperties"/>, overriding the foreground color.
	/// </summary>
	/// <param name="text">The run's text - exactly what the caller appends to the layout text.</param>
	/// <param name="colorOverride">
	/// The color to paint the run with, or null to use the properties' foreground color (which may
	/// itself be null, deferring to the view's default text color at draw time).
	/// </param>
	/// <exception cref="InvalidOperationException">The element's text run properties are not assigned yet.</exception>
	protected TextRunDescriptor CreateTextRunDescriptor(string text, SKColor? colorOverride)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		var properties = TextRunProperties
			?? throw new InvalidOperationException("TextRunProperties is not assigned yet; the element has not finished construction.");
		return new TextRunDescriptor(
			text,
			properties.FontFamily,
			(float)properties.FontSize,
			VisualLineElementTextRunProperties.ToTextFontWeight(properties.FontWeight),
			VisualLineElementTextRunProperties.ToTextFontStyle(properties.FontStyle),
			VisualLineElementTextRunProperties.ToTextFontStretch(properties.FontStretch))
		{
			Color = colorOverride ?? properties.ForegroundColor,
		};
	}

	/// <summary>
	/// Gets if this VisualLineElement can be split.
	/// </summary>
	public virtual bool CanSplit {
		get { return false; }
	}

	/// <summary>
	/// Splits the element.
	/// </summary>
	/// <param name="splitVisualColumn">Position inside this element at which it should be broken</param>
	/// <param name="elements">The collection of line elements</param>
	/// <param name="elementIndex">The index at which this element is in the elements list.</param>
	public virtual void Split(int splitVisualColumn, IList<VisualLineElement> elements, int elementIndex)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Helper method for splitting this line element into two, correctly updating the
	/// <see cref="VisualLength"/>, <see cref="DocumentLength"/>, <see cref="VisualColumn"/>
	/// and <see cref="RelativeTextOffset"/> properties.
	/// </summary>
	/// <param name="firstPart">The element before the split position.</param>
	/// <param name="secondPart">The element after the split position.</param>
	/// <param name="splitVisualColumn">The split position as visual column.</param>
	/// <param name="splitRelativeTextOffset">The split position as text offset.</param>
	protected void SplitHelper(VisualLineElement firstPart, VisualLineElement secondPart, int splitVisualColumn, int splitRelativeTextOffset)
	{
		if (firstPart == null)
			throw new ArgumentNullException(nameof(firstPart));
		if (secondPart == null)
			throw new ArgumentNullException(nameof(secondPart));
		int relativeSplitVisualColumn = splitVisualColumn - VisualColumn;
		int relativeSplitRelativeTextOffset = splitRelativeTextOffset - RelativeTextOffset;

		if (relativeSplitVisualColumn <= 0 || relativeSplitVisualColumn >= VisualLength)
			throw new ArgumentOutOfRangeException(nameof(splitVisualColumn), splitVisualColumn, "Value must be between " + (VisualColumn + 1) + " and " + (VisualColumn + VisualLength - 1));
		if (relativeSplitRelativeTextOffset < 0 || relativeSplitRelativeTextOffset > DocumentLength)
			throw new ArgumentOutOfRangeException(nameof(splitRelativeTextOffset), splitRelativeTextOffset, "Value must be between " + (RelativeTextOffset) + " and " + (RelativeTextOffset + DocumentLength));
		int oldVisualLength = VisualLength;
		int oldDocumentLength = DocumentLength;
		int oldVisualColumn = VisualColumn;
		int oldRelativeTextOffset = RelativeTextOffset;
		firstPart.VisualColumn = oldVisualColumn;
		secondPart.VisualColumn = oldVisualColumn + relativeSplitVisualColumn;
		firstPart.RelativeTextOffset = oldRelativeTextOffset;
		secondPart.RelativeTextOffset = oldRelativeTextOffset + relativeSplitRelativeTextOffset;
		firstPart.VisualLength = relativeSplitVisualColumn;
		secondPart.VisualLength = oldVisualLength - relativeSplitVisualColumn;
		firstPart.DocumentLength = relativeSplitRelativeTextOffset;
		secondPart.DocumentLength = oldDocumentLength - relativeSplitRelativeTextOffset;
		if (firstPart.TextRunProperties == null)
			firstPart.TextRunProperties = TextRunProperties?.Clone();
		if (secondPart.TextRunProperties == null)
			secondPart.TextRunProperties = TextRunProperties?.Clone();
		firstPart.BackgroundBrush = BackgroundBrush;
		secondPart.BackgroundBrush = BackgroundBrush;
	}

	/// <summary>
	/// Gets the visual column of a text location inside this element.
	/// The text offset is given relative to the visual line start.
	/// </summary>
	public virtual int GetVisualColumn(int relativeTextOffset)
	{
		if (relativeTextOffset >= this.RelativeTextOffset + DocumentLength)
			return VisualColumn + VisualLength;
		else
			return VisualColumn;
	}

	/// <summary>
	/// Gets the text offset of a visual column inside this element.
	/// </summary>
	/// <returns>A text offset relative to the visual line start.</returns>
	public virtual int GetRelativeOffset(int visualColumn)
	{
		if (visualColumn >= this.VisualColumn + VisualLength)
			return RelativeTextOffset + DocumentLength;
		else
			return RelativeTextOffset;
	}

	/// <summary>
	/// Gets the next caret position inside this element.
	/// </summary>
	/// <param name="visualColumn">The visual column from which the search should be started.</param>
	/// <param name="direction">The search direction (forwards or backwards).</param>
	/// <param name="mode">Whether to stop only at word borders.</param>
	/// <returns>The visual column of the next caret position, or -1 if there is no next caret position.</returns>
	/// <remarks>
	/// In the space between two line elements, it is sufficient that one of them contains a caret position;
	/// though in many cases, both of them contain one.
	/// </remarks>
	public virtual int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
	{
		int stop1 = this.VisualColumn;
		int stop2 = this.VisualColumn + this.VisualLength;
		if (direction == LogicalDirection.Backward)
		{
			if (visualColumn > stop2 && mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol)
				return stop2;
			else if (visualColumn > stop1)
				return stop1;
		}
		else
		{
			if (visualColumn < stop1)
				return stop1;
			else if (visualColumn < stop2 && mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol)
				return stop2;
		}
		return -1;
	}

	/// <summary>
	/// Gets whether the specified offset in this element is considered whitespace.
	/// </summary>
	public virtual bool IsWhitespace(int visualColumn)
	{
		return false;
	}

	/// <summary>
	/// Gets whether the <see cref="GetNextCaretPosition"/> implementation handles line borders.
	/// If this property returns false, the caller of GetNextCaretPosition should handle the line
	/// borders (i.e. place caret stops at the start and end of the line).
	/// This property has an effect only for VisualLineElements that are at the start or end of a
	/// <see cref="VisualLine"/>.
	/// </summary>
	public virtual bool HandlesLineBorders {
		get { return false; }
	}

	/// <summary>
	/// Allows the visual line element to handle a pointer-pressed event.
	/// </summary>
	protected internal virtual void OnPointerPressed(PointerRoutedEventArgs e)
	{
	}

	/// <summary>
	/// Allows the visual line element to handle a pointer-released event.
	/// </summary>
	protected internal virtual void OnPointerReleased(PointerRoutedEventArgs e)
	{
	}
}
