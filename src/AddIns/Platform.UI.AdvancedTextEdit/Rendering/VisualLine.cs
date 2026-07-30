#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using CodeBrix.Platform.UI.TextLayout;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLine.cs in the AvalonEdit repo (MIT).
//The construction/transform lifecycle and the caret/column mathematics are transliterated. The WPF
//TextFormatter machinery is replaced by ONE engine layout per visual line: Format() collects each
//element's layout-text contribution (recording LayoutStart/LayoutLength as the visual-column <->
//layout-index mapping table), runs TextLayoutEngine.Layout once, and exposes the wrapped engine
//rows as TextLineLayout objects in place of WPF TextLines. The VisualLineDrawingVisual/Render()
//pair collapsed into Draw(SKCanvas, SKPoint), which paints element backgrounds, the layout (per-run
//colors baked), and underline/strikethrough decorations. The end-of-line marker is appended by
//Format() as a trailing run ('¶', or '¤' for an unrecognized delimiter) instead of being produced
//by the WPF text source; paragraph base direction is locked left-to-right in this version.

/// <summary>
/// Represents a visual line in the document.
/// A visual line usually corresponds to one DocumentLine, but it can span multiple lines if
/// all but the first are collapsed.
/// </summary>
public sealed class VisualLine
{
	enum LifetimePhase : byte
	{
		Generating,
		Transforming,
		Live,
		Disposed
	}

	const double Epsilon = 0.0001;

	readonly TextView textView;
	List<VisualLineElement> elements;
	internal bool hasInlineObjects;
	LifetimePhase phase;

	ReadOnlyCollection<TextLineLayout>? textLines;
	TextLayoutResult? layoutResult;
	int elementLayoutLength;
	int eolLayoutStart;
	int eolLayoutLength;
	SKColor defaultTextColor = SKColors.Black;

	/// <summary>
	/// Gets the document to which this VisualLine belongs.
	/// </summary>
	public TextDocument Document { get; private set; }

	/// <summary>
	/// Gets the first document line displayed by this visual line.
	/// </summary>
	public DocumentLine FirstDocumentLine { get; private set; }

	/// <summary>
	/// Gets the last document line displayed by this visual line.
	/// </summary>
	public DocumentLine LastDocumentLine { get; private set; }

	/// <summary>
	/// Gets a read-only collection of line elements.
	/// </summary>
	public ReadOnlyCollection<VisualLineElement> Elements { get; private set; }

	/// <summary>
	/// Gets a read-only collection of rendered rows.
	/// </summary>
	/// <exception cref="InvalidOperationException">The visual line has not been formatted yet.</exception>
	public ReadOnlyCollection<TextLineLayout> TextLines {
		get {
			if (phase < LifetimePhase.Live || textLines == null)
				throw new InvalidOperationException();
			return textLines;
		}
	}

	/// <summary>
	/// Gets the completed engine layout of this visual line, or null before the line was formatted.
	/// </summary>
	internal TextLayoutResult? LayoutResult {
		get { return layoutResult; }
	}

	/// <summary>
	/// Gets the start offset of the VisualLine inside the document.
	/// This is equivalent to <c>FirstDocumentLine.Offset</c>.
	/// </summary>
	public int StartOffset {
		get {
			return FirstDocumentLine.Offset;
		}
	}

	/// <summary>
	/// Length in visual line coordinates.
	/// </summary>
	public int VisualLength { get; private set; }

	/// <summary>
	/// Length in visual line coordinates including the end of line marker, if
	/// <see cref="AdvancedTextEditOptions.ShowEndOfLine"/> is enabled.
	/// </summary>
	public int VisualLengthWithEndOfLineMarker {
		get {
			int length = VisualLength;
			if (textView.Options.ShowEndOfLine && LastDocumentLine.NextLine != null)
				length++;
			return length;
		}
	}

	/// <summary>
	/// Gets the height of the visual line in device-independent pixels.
	/// </summary>
	public double Height { get; private set; }

	/// <summary>
	/// Gets the Y position of the line. This is measured in device-independent pixels relative to the start of the document.
	/// </summary>
	public double VisualTop { get; internal set; }

	internal VisualLine(TextView textView, DocumentLine firstDocumentLine)
	{
		Debug.Assert(textView != null);
		Debug.Assert(firstDocumentLine != null);
		this.textView = textView;
		this.Document = textView.Document;
		this.FirstDocumentLine = firstDocumentLine;
		this.LastDocumentLine = firstDocumentLine;
		this.elements = new List<VisualLineElement>();
		this.Elements = elements.AsReadOnly();
	}

	internal void ConstructVisualElements(ITextRunConstructionContext context, VisualLineElementGenerator[] generators)
	{
		Debug.Assert(phase == LifetimePhase.Generating);
		foreach (VisualLineElementGenerator g in generators)
		{
			g.StartGeneration(context);
		}
		elements = new List<VisualLineElement>();
		PerformVisualElementConstruction(generators);
		foreach (VisualLineElementGenerator g in generators)
		{
			g.FinishGeneration();
		}

		var globalTextRunProperties = context.GlobalTextRunProperties;
		foreach (var element in elements)
		{
			element.SetTextRunProperties(new VisualLineElementTextRunProperties(globalTextRunProperties));
		}
		this.Elements = elements.AsReadOnly();
		CalculateOffsets();
		phase = LifetimePhase.Transforming;
	}

	void PerformVisualElementConstruction(VisualLineElementGenerator[] generators)
	{
		TextDocument document = this.Document;
		int offset = FirstDocumentLine.Offset;
		int currentLineEnd = offset + FirstDocumentLine.Length;
		LastDocumentLine = FirstDocumentLine;
		int askInterestOffset = 0; // 0 or 1
		while (offset + askInterestOffset <= currentLineEnd)
		{
			int textPieceEndOffset = currentLineEnd;
			foreach (VisualLineElementGenerator g in generators)
			{
				g.cachedInterest = g.GetFirstInterestedOffset(offset + askInterestOffset);
				if (g.cachedInterest != -1)
				{
					if (g.cachedInterest < offset)
						throw new ArgumentOutOfRangeException(g.GetType().Name + ".GetFirstInterestedOffset",
															  g.cachedInterest,
															  "GetFirstInterestedOffset must not return an offset less than startOffset. Return -1 to signal no interest.");
					if (g.cachedInterest < textPieceEndOffset)
						textPieceEndOffset = g.cachedInterest;
				}
			}
			Debug.Assert(textPieceEndOffset >= offset);
			if (textPieceEndOffset > offset)
			{
				int textPieceLength = textPieceEndOffset - offset;
				elements.Add(new VisualLineText(this, textPieceLength));
				offset = textPieceEndOffset;
			}
			// If no elements constructed / only zero-length elements constructed:
			// do not asking the generators again for the same location (would cause endless loop)
			askInterestOffset = 1;
			foreach (VisualLineElementGenerator g in generators)
			{
				if (g.cachedInterest == offset)
				{
					VisualLineElement? element = g.ConstructElement(offset);
					if (element != null)
					{
						elements.Add(element);
						if (element.DocumentLength > 0)
						{
							// a non-zero-length element was constructed
							askInterestOffset = 0;
							offset += element.DocumentLength;
							if (offset > currentLineEnd)
							{
								DocumentLine newEndLine = document.GetLineByOffset(offset);
								currentLineEnd = newEndLine.Offset + newEndLine.Length;
								this.LastDocumentLine = newEndLine;
								if (currentLineEnd < offset)
								{
									throw new InvalidOperationException(
										"The VisualLineElementGenerator " + g.GetType().Name +
										" produced an element which ends within the line delimiter");
								}
							}
							break;
						}
					}
				}
			}
		}
	}

	void CalculateOffsets()
	{
		int visualOffset = 0;
		int textOffset = 0;
		foreach (VisualLineElement element in elements)
		{
			element.VisualColumn = visualOffset;
			element.RelativeTextOffset = textOffset;
			visualOffset += element.VisualLength;
			textOffset += element.DocumentLength;
		}
		VisualLength = visualOffset;
		Debug.Assert(textOffset == LastDocumentLine.EndOffset - FirstDocumentLine.Offset);
	}

	internal void RunTransformers(ITextRunConstructionContext context, IVisualLineTransformer[] transformers)
	{
		Debug.Assert(phase == LifetimePhase.Transforming);
		foreach (IVisualLineTransformer transformer in transformers)
		{
			transformer.Transform(context, elements);
		}
		//was previously: upstream patched WPF typography properties here (all-or-none rule);
		//the port has no typography properties, so nothing to patch.
		phase = LifetimePhase.Live;
	}

	/// <summary>
	/// Replaces the single element at <paramref name="elementIndex"/> with the specified elements.
	/// The replacement operation must preserve the document length, but may change the visual length.
	/// </summary>
	/// <remarks>
	/// This method may only be called by line transformers.
	/// </remarks>
	public void ReplaceElement(int elementIndex, params VisualLineElement[] newElements)
	{
		ReplaceElement(elementIndex, 1, newElements);
	}

	/// <summary>
	/// Replaces <paramref name="count"/> elements starting at <paramref name="elementIndex"/> with the specified elements.
	/// The replacement operation must preserve the document length, but may change the visual length.
	/// </summary>
	/// <remarks>
	/// This method may only be called by line transformers.
	/// </remarks>
	public void ReplaceElement(int elementIndex, int count, params VisualLineElement[] newElements)
	{
		if (phase != LifetimePhase.Transforming)
			throw new InvalidOperationException("This method may only be called by line transformers.");
		int oldDocumentLength = 0;
		for (int i = elementIndex; i < elementIndex + count; i++)
		{
			oldDocumentLength += elements[i].DocumentLength;
		}
		int newDocumentLength = 0;
		foreach (var newElement in newElements)
		{
			newDocumentLength += newElement.DocumentLength;
		}
		if (oldDocumentLength != newDocumentLength)
			throw new InvalidOperationException("Old elements have document length " + oldDocumentLength + ", but new elements have length " + newDocumentLength);
		elements.RemoveRange(elementIndex, count);
		elements.InsertRange(elementIndex, newElements);
		CalculateOffsets();
	}

	/// <summary>
	/// Lays this visual line out: collects each element's layout-text contribution, runs the text
	/// engine once, and builds the <see cref="TextLines"/> rows.
	/// </summary>
	/// <param name="context">The construction context.</param>
	/// <param name="wrapWidth">The width to wrap within, or null when word wrap is disabled.</param>
	internal void Format(ITextRunConstructionContext context, float? wrapWidth)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		Debug.Assert(phase == LifetimePhase.Live);
		layoutResult?.Dispose();
		layoutResult = null;
		textLines = null;

		var builder = new StringBuilder();
		var runs = new List<TextRunDescriptor>(elements.Count + 1);
		foreach (VisualLineElement element in elements)
		{
			int start = builder.Length;
			TextRunDescriptor run = element.BuildLayoutText(builder, context)
				?? throw new InvalidOperationException(element.GetType().Name + ".BuildLayoutText must not return null.");
			int appended = builder.Length - start;
			if (appended != run.Text.Length)
			{
				throw new InvalidOperationException(
					element.GetType().Name + ".BuildLayoutText must append exactly the returned run's text (appended "
					+ appended + " characters, but the returned run has " + run.Text.Length + ").");
			}
			element.LayoutStart = start;
			element.LayoutLength = appended;
			if (appended > 0)
				runs.Add(run);
		}
		elementLayoutLength = builder.Length;
		eolLayoutStart = builder.Length;
		eolLayoutLength = 0;

		var global = context.GlobalTextRunProperties;
		if (textView.Options.ShowEndOfLine && LastDocumentLine.NextLine != null)
		{
			string marker = GetEndOfLineMarkerText();
			if (marker.Length > 0)
			{
				eolLayoutLength = marker.Length;
				builder.Append(marker);
				SKColor markerColor = VisualLineElementTextRunProperties.GetSolidColor(textView.NonPrintableCharacterBrush)
					?? new SKColor(128, 128, 128, 200);
				runs.Add(CreateRunFromGlobalProperties(global, marker, markerColor));
			}
		}

		defaultTextColor = VisualLineElementTextRunProperties.GetSolidColor(global.ForegroundBrush) ?? SKColors.Black;

		if (runs.Count == 0)
		{
			// The engine requires at least one run; an empty run produces an empty layout.
			runs.Add(CreateRunFromGlobalProperties(global, string.Empty, null));
		}

		var layoutOptions = new TextLayoutOptions
		{
			MaxWidth = wrapWidth,
			Alignment = TextAlign.Left,
			//was previously: WPF resolved the paragraph's base direction per content; this version
			//locks left-to-right paragraph bases (right-to-left runs inside still render correctly).
			BaseDirection = TextDirection.LeftToRight,
		};
		layoutResult = TextLayoutEngine.Layout(runs, layoutOptions);
		BuildRows();
	}

	static TextRunDescriptor CreateRunFromGlobalProperties(GlobalTextRunProperties global, string text, SKColor? color)
	{
		return new TextRunDescriptor(
			text,
			global.FontFamily,
			(float)global.FontSize,
			VisualLineElementTextRunProperties.ToTextFontWeight(global.FontWeight),
			VisualLineElementTextRunProperties.ToTextFontStyle(global.FontStyle),
			VisualLineElementTextRunProperties.ToTextFontStretch(global.FontStretch))
		{
			Color = color ?? VisualLineElementTextRunProperties.GetSolidColor(global.ForegroundBrush),
		};
	}

	string GetEndOfLineMarkerText()
	{
		//was previously: upstream showed "¶" only for two-character delimiters and the strings
		//"\r"/"\n" for single-character ones; the port shows "¶" for every recognized delimiter
		//and "¤" for an unrecognized one.
		DocumentLine lastDocumentLine = LastDocumentLine;
		if (lastDocumentLine.DelimiterLength == 2)
			return "¶";
		if (lastDocumentLine.DelimiterLength == 1)
		{
			char newlineChar = Document.GetCharAt(lastDocumentLine.Offset + lastDocumentLine.Length);
			return (newlineChar == '\r' || newlineChar == '\n') ? "¶" : "¤";
		}
		return "";
	}

	void BuildRows()
	{
		var layout = layoutResult!;
		var rows = new List<TextLineLayout>();
		int lineCount = layout.Text.Length == 0 ? 0 : layout.LineCount;
		if (lineCount <= 0)
		{
			// Empty document line: one synthetic row at the default line height, caret x = 0.
			double height = layout.LineHeight;
			double baseline = textView.DefaultBaseline;
			if (baseline <= 0 || baseline > height)
				baseline = 0.8 * height;
			rows.Add(new TextLineLayout(this, 0, 0, 0, 0, 0, height, baseline));
		}
		else
		{
			for (int i = 0; i < lineCount; i++)
			{
				TextLineMetrics metrics = layout.GetLineMetrics(i);
				int firstVisualColumn = GetVisualColumnFromLayoutIndexCore(metrics.Start, floor: true);
				int lastVisualColumn = GetVisualColumnFromLayoutIndexCore(metrics.Start + metrics.Length, floor: false);
				rows.Add(new TextLineLayout(
					this,
					metrics.Start,
					metrics.Length,
					firstVisualColumn,
					lastVisualColumn,
					metrics.Top,
					metrics.Height,
					metrics.BaselineOffset));
			}
		}
		double totalHeight = 0;
		foreach (TextLineLayout row in rows)
			totalHeight += row.Height;
		Height = totalHeight;
		textLines = rows.AsReadOnly();
	}

	/// <summary>
	/// Gets the visual column from a document offset relative to the first line start.
	/// </summary>
	public int GetVisualColumn(int relativeTextOffset)
	{
		ThrowUtil.CheckNotNegative(relativeTextOffset, "relativeTextOffset");
		foreach (VisualLineElement element in elements)
		{
			if (element.RelativeTextOffset <= relativeTextOffset
				&& element.RelativeTextOffset + element.DocumentLength >= relativeTextOffset)
			{
				return element.GetVisualColumn(relativeTextOffset);
			}
		}
		return VisualLength;
	}

	/// <summary>
	/// Gets the document offset (relative to the first line start) from a visual column.
	/// </summary>
	public int GetRelativeOffset(int visualColumn)
	{
		ThrowUtil.CheckNotNegative(visualColumn, "visualColumn");
		int documentLength = 0;
		foreach (VisualLineElement element in elements)
		{
			if (element.VisualColumn <= visualColumn
				&& element.VisualColumn + element.VisualLength > visualColumn)
			{
				return element.GetRelativeOffset(visualColumn);
			}
			documentLength += element.DocumentLength;
		}
		return documentLength;
	}

	/// <summary>
	/// Gets the layout text index for a visual column.
	/// Inside an element that expands to multiple layout characters (a tab, an inline object), the
	/// element's layout start is returned; the element's end column maps to its layout end.
	/// </summary>
	public int GetLayoutIndex(int visualColumn)
	{
		if (visualColumn <= 0)
			return 0;
		foreach (VisualLineElement element in elements)
		{
			if (visualColumn < element.VisualColumn)
				return element.LayoutStart;
			int elementEnd = element.VisualColumn + element.VisualLength;
			if (visualColumn < elementEnd)
			{
				if (element.LayoutLength == element.VisualLength)
					return element.LayoutStart + (visualColumn - element.VisualColumn);
				return element.LayoutStart;
			}
		}
		if (eolLayoutLength > 0 && visualColumn > VisualLength)
			return eolLayoutStart + eolLayoutLength;
		return elementLayoutLength;
	}

	/// <summary>
	/// Gets the visual column for a layout text index.
	/// Inside an element that expands to multiple layout characters, the index is clamped to the
	/// element edge nearest to it.
	/// </summary>
	public int GetVisualColumnFromLayoutIndex(int layoutIndex)
	{
		return GetVisualColumnFromLayoutIndexCore(layoutIndex, floor: false);
	}

	int GetVisualColumnFromLayoutIndexCore(int layoutIndex, bool floor)
	{
		if (layoutIndex <= 0)
			return 0;
		foreach (VisualLineElement element in elements)
		{
			if (layoutIndex < element.LayoutStart + element.LayoutLength)
			{
				if (layoutIndex <= element.LayoutStart)
					return element.VisualColumn;
				if (element.LayoutLength == element.VisualLength)
					return element.VisualColumn + (layoutIndex - element.LayoutStart);
				if (floor)
					return element.VisualColumn;
				int offsetInElement = layoutIndex - element.LayoutStart;
				return (offsetInElement * 2 >= element.LayoutLength)
					? element.VisualColumn + element.VisualLength
					: element.VisualColumn;
			}
		}
		if (eolLayoutLength > 0 && layoutIndex > eolLayoutStart)
		{
			if (floor && layoutIndex < eolLayoutStart + eolLayoutLength)
				return VisualLength;
			return VisualLength + 1;
		}
		return VisualLength;
	}

	/// <summary>
	/// Gets the row containing the specified visual column.
	/// </summary>
	public TextLineLayout GetTextLine(int visualColumn)
	{
		return GetTextLine(visualColumn, false);
	}

	/// <summary>
	/// Gets the row containing the specified visual column.
	/// </summary>
	public TextLineLayout GetTextLine(int visualColumn, bool isAtEndOfLine)
	{
		if (visualColumn < 0)
			throw new ArgumentOutOfRangeException(nameof(visualColumn));
		var lines = TextLines;
		if (visualColumn >= VisualLengthWithEndOfLineMarker)
			return lines[lines.Count - 1];
		foreach (TextLineLayout line in lines)
		{
			if (isAtEndOfLine ? visualColumn <= line.LastVisualColumn : visualColumn < line.LastVisualColumn)
				return line;
		}
		return lines[lines.Count - 1];
	}

	/// <summary>
	/// Gets the visual top from the specified row.
	/// </summary>
	/// <returns>Distance in device-independent pixels
	/// from the top of the document to the top of the specified row.</returns>
	public double GetTextLineVisualYPosition(TextLineLayout textLine, VisualYPosition yPositionMode)
	{
		if (textLine == null)
			throw new ArgumentNullException(nameof(textLine));
		CheckRowOwnership(textLine);
		double pos = VisualTop + textLine.Top;
		switch (yPositionMode)
		{
			case VisualYPosition.LineTop:
				return pos;
			case VisualYPosition.LineMiddle:
				return pos + textLine.Height / 2;
			case VisualYPosition.LineBottom:
				return pos + textLine.Height;
			case VisualYPosition.TextTop:
				return pos + textLine.Baseline - textView.DefaultBaseline;
			case VisualYPosition.TextBottom:
				return pos + textLine.Baseline - textView.DefaultBaseline + textView.DefaultLineHeight;
			case VisualYPosition.TextMiddle:
				return pos + textLine.Baseline - textView.DefaultBaseline + textView.DefaultLineHeight / 2;
			case VisualYPosition.Baseline:
				return pos + textLine.Baseline;
			default:
				throw new ArgumentException("Invalid yPositionMode:" + yPositionMode);
		}
	}

	/// <summary>
	/// Gets the start visual column from the specified row.
	/// </summary>
	public int GetTextLineVisualStartColumn(TextLineLayout textLine)
	{
		if (textLine == null)
			throw new ArgumentNullException(nameof(textLine));
		CheckRowOwnership(textLine);
		return textLine.FirstVisualColumn;
	}

	/// <summary>
	/// Gets a row by the visual position.
	/// </summary>
	public TextLineLayout GetTextLineByVisualYPosition(double visualTop)
	{
		var lines = TextLines;
		double pos = this.VisualTop;
		foreach (TextLineLayout tl in lines)
		{
			pos += tl.Height;
			if (visualTop + Epsilon < pos)
				return tl;
		}
		return lines[lines.Count - 1];
	}

	/// <summary>
	/// Gets the visual position from the specified visualColumn.
	/// </summary>
	/// <returns>Position in device-independent pixels
	/// relative to the top left of the document.</returns>
	public Point GetVisualPosition(int visualColumn, VisualYPosition yPositionMode)
	{
		TextLineLayout textLine = GetTextLine(visualColumn);
		double xPos = GetTextLineVisualXPosition(textLine, visualColumn);
		double yPos = GetTextLineVisualYPosition(textLine, yPositionMode);
		return new Point(xPos, yPos);
	}

	internal Point GetVisualPosition(int visualColumn, bool isAtEndOfLine, VisualYPosition yPositionMode)
	{
		TextLineLayout textLine = GetTextLine(visualColumn, isAtEndOfLine);
		double xPos = GetTextLineVisualXPosition(textLine, visualColumn);
		double yPos = GetTextLineVisualYPosition(textLine, yPositionMode);
		return new Point(xPos, yPos);
	}

	/// <summary>
	/// Gets the distance to the left border of the text area of the specified visual column.
	/// The visual column must belong to the specified row.
	/// </summary>
	public double GetTextLineVisualXPosition(TextLineLayout textLine, int visualColumn)
	{
		if (textLine == null)
			throw new ArgumentNullException(nameof(textLine));
		CheckRowOwnership(textLine);
		int clampedColumn = Math.Min(visualColumn, VisualLengthWithEndOfLineMarker);
		double xPos = GetCaretXInRow(textLine, clampedColumn);
		if (visualColumn > VisualLengthWithEndOfLineMarker)
		{
			xPos += (visualColumn - VisualLengthWithEndOfLineMarker) * textView.WideSpaceWidth;
		}
		return xPos;
	}

	double GetCaretXInRow(TextLineLayout row, int visualColumn)
	{
		var layout = layoutResult;
		if (layout == null || layout.Text.Length == 0)
			return 0;
		int index = GetLayoutIndex(Math.Max(visualColumn, 0));
		index = Math.Clamp(index, row.LayoutStart, row.LayoutStart + row.LayoutLength);
		SKRect rect = layout.GetCaretRect(index);
		double mid = (rect.Top + rect.Bottom) / 2;
		if (mid < row.Top - Epsilon || mid >= row.Top + row.Height + Epsilon)
		{
			// A layout index on a wrap boundary belongs to two rows; the engine answered for the
			// other one, so pin the position to this row's matching edge.
			return index <= row.LayoutStart ? 0 : row.Width;
		}
		return rect.Left;
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, rounds to the nearest column.
	/// </summary>
	public int GetVisualColumn(Point point)
	{
		return GetVisualColumn(point, textView.Options.EnableVirtualSpace);
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, rounds to the nearest column.
	/// </summary>
	public int GetVisualColumn(Point point, bool allowVirtualSpace)
	{
		return GetVisualColumn(GetTextLineByVisualYPosition(point.Y), point.X, allowVirtualSpace);
	}

	internal int GetVisualColumn(Point point, bool allowVirtualSpace, out bool isAtEndOfLine)
	{
		var textLine = GetTextLineByVisualYPosition(point.Y);
		int vc = GetVisualColumn(textLine, point.X, allowVirtualSpace);
		isAtEndOfLine = (vc >= GetTextLineVisualStartColumn(textLine) + textLine.Length);
		return vc;
	}

	/// <summary>
	/// Gets the visual column from a distance to the left border of the row.
	/// If the position is between two visual columns, rounds to the nearest column.
	/// </summary>
	public int GetVisualColumn(TextLineLayout textLine, double xPos, bool allowVirtualSpace)
	{
		if (textLine == null)
			throw new ArgumentNullException(nameof(textLine));
		CheckRowOwnership(textLine);
		var lines = TextLines;
		if (xPos > textLine.Width)
		{
			if (allowVirtualSpace && textLine == lines[lines.Count - 1])
			{
				int virtualX = (int)Math.Round((xPos - textLine.Width) / textView.WideSpaceWidth, MidpointRounding.AwayFromZero);
				return VisualLengthWithEndOfLineMarker + virtualX;
			}
		}
		var layout = layoutResult;
		if (layout == null || textLine.LayoutLength == 0)
			return textLine.FirstVisualColumn;
		int index = layout.GetNearestIndexAt(new SKPoint((float)xPos, (float)(textLine.Top + textLine.Height / 2)));
		if (index < 0)
			index = textLine.LayoutStart;
		index = Math.Clamp(index, textLine.LayoutStart, textLine.LayoutStart + textLine.LayoutLength);
		return GetVisualColumnFromLayoutIndexCore(index, floor: false);
	}

	/// <summary>
	/// Validates the visual column and returns the correct one.
	/// </summary>
	public int ValidateVisualColumn(TextViewPosition position, bool allowVirtualSpace)
	{
		return ValidateVisualColumn(Document.GetOffset(position.Location), position.VisualColumn, allowVirtualSpace);
	}

	/// <summary>
	/// Validates the visual column and returns the correct one.
	/// </summary>
	public int ValidateVisualColumn(int offset, int visualColumn, bool allowVirtualSpace)
	{
		int firstDocumentLineOffset = this.FirstDocumentLine.Offset;
		if (visualColumn < 0)
		{
			return GetVisualColumn(offset - firstDocumentLineOffset);
		}
		else
		{
			int offsetFromVisualColumn = GetRelativeOffset(visualColumn);
			offsetFromVisualColumn += firstDocumentLineOffset;
			if (offsetFromVisualColumn != offset)
			{
				return GetVisualColumn(offset - firstDocumentLineOffset);
			}
			else
			{
				if (visualColumn > VisualLength && !allowVirtualSpace)
				{
					return VisualLength;
				}
			}
		}
		return visualColumn;
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, returns the first of those columns.
	/// </summary>
	public int GetVisualColumnFloor(Point point)
	{
		return GetVisualColumnFloor(point, textView.Options.EnableVirtualSpace);
	}

	/// <summary>
	/// Gets the visual column from a document position (relative to top left of the document).
	/// If the user clicks between two visual columns, returns the first of those columns.
	/// </summary>
	public int GetVisualColumnFloor(Point point, bool allowVirtualSpace)
	{
		bool tmp;
		return GetVisualColumnFloor(point, allowVirtualSpace, out tmp);
	}

	internal int GetVisualColumnFloor(Point point, bool allowVirtualSpace, out bool isAtEndOfLine)
	{
		TextLineLayout textLine = GetTextLineByVisualYPosition(point.Y);
		var lines = TextLines;
		if (point.X > textLine.Width)
		{
			isAtEndOfLine = true;
			if (allowVirtualSpace && textLine == lines[lines.Count - 1])
			{
				// clicking virtual space in the last line
				int virtualX = (int)((point.X - textLine.Width) / textView.WideSpaceWidth);
				return VisualLengthWithEndOfLineMarker + virtualX;
			}
			else
			{
				// Clicking behind the line ends returns the line's end column.
				return GetTextLineVisualStartColumn(textLine) + textLine.Length;
			}
		}
		else
		{
			isAtEndOfLine = false;
		}
		var layout = layoutResult;
		if (layout == null || textLine.LayoutLength == 0)
			return textLine.FirstVisualColumn;
		var hitPoint = new SKPoint((float)point.X, (float)(textLine.Top + textLine.Height / 2));
		int index = layout.GetIndexAt(hitPoint);
		if (index < 0)
			index = layout.GetNearestIndexAt(hitPoint);
		index = Math.Clamp(index, textLine.LayoutStart, textLine.LayoutStart + textLine.LayoutLength);
		return GetVisualColumnFromLayoutIndexCore(index, floor: true);
	}

	/// <summary>
	/// Gets the text view position from the specified visual column.
	/// </summary>
	public TextViewPosition GetTextViewPosition(int visualColumn)
	{
		int documentOffset = GetRelativeOffset(visualColumn) + this.FirstDocumentLine.Offset;
		return new TextViewPosition(this.Document.GetLocation(documentOffset), visualColumn);
	}

	/// <summary>
	/// Gets the text view position from the specified visual position.
	/// If the position is within a character, it is rounded to the next character boundary.
	/// </summary>
	/// <param name="visualPosition">The position in device-independent pixels relative
	/// to the top left corner of the document.</param>
	/// <param name="allowVirtualSpace">Controls whether positions in virtual space may be returned.</param>
	public TextViewPosition GetTextViewPosition(Point visualPosition, bool allowVirtualSpace)
	{
		bool isAtEndOfLine;
		int visualColumn = GetVisualColumn(visualPosition, allowVirtualSpace, out isAtEndOfLine);
		int documentOffset = GetRelativeOffset(visualColumn) + this.FirstDocumentLine.Offset;
		TextViewPosition pos = new TextViewPosition(this.Document.GetLocation(documentOffset), visualColumn);
		pos.IsAtEndOfLine = isAtEndOfLine;
		return pos;
	}

	/// <summary>
	/// Gets the text view position from the specified visual position.
	/// If the position is inside a character, the position in front of the character is returned.
	/// </summary>
	/// <param name="visualPosition">The position in device-independent pixels relative
	/// to the top left corner of the document.</param>
	/// <param name="allowVirtualSpace">Controls whether positions in virtual space may be returned.</param>
	public TextViewPosition GetTextViewPositionFloor(Point visualPosition, bool allowVirtualSpace)
	{
		bool isAtEndOfLine;
		int visualColumn = GetVisualColumnFloor(visualPosition, allowVirtualSpace, out isAtEndOfLine);
		int documentOffset = GetRelativeOffset(visualColumn) + this.FirstDocumentLine.Offset;
		TextViewPosition pos = new TextViewPosition(this.Document.GetLocation(documentOffset), visualColumn);
		pos.IsAtEndOfLine = isAtEndOfLine;
		return pos;
	}

	/// <summary>
	/// Gets whether the visual line was disposed.
	/// </summary>
	public bool IsDisposed {
		get { return phase == LifetimePhase.Disposed; }
	}

	internal void Dispose()
	{
		if (phase == LifetimePhase.Disposed)
			return;
		Debug.Assert(phase == LifetimePhase.Live);
		phase = LifetimePhase.Disposed;
		layoutResult?.Dispose();
		layoutResult = null;
	}

	/// <summary>
	/// Gets the next possible caret position after visualColumn, or -1 if there is no caret position.
	/// </summary>
	public int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode, bool allowVirtualSpace)
	{
		if (!HasStopsInVirtualSpace(mode))
			allowVirtualSpace = false;

		if (elements.Count == 0)
		{
			// special handling for empty visual lines:
			if (allowVirtualSpace)
			{
				if (direction == LogicalDirection.Forward)
					return Math.Max(0, visualColumn + 1);
				else if (visualColumn > 0)
					return visualColumn - 1;
				else
					return -1;
			}
			else
			{
				// even though we don't have any elements,
				// there's a single caret stop at visualColumn 0
				if (visualColumn < 0 && direction == LogicalDirection.Forward)
					return 0;
				else if (visualColumn > 0 && direction == LogicalDirection.Backward)
					return 0;
				else
					return -1;
			}
		}

		int i;
		if (direction == LogicalDirection.Backward)
		{
			// Search Backwards:
			// If the last element doesn't handle line borders, return the line end as caret stop

			if (visualColumn > this.VisualLength && !elements[elements.Count - 1].HandlesLineBorders && HasImplicitStopAtLineEnd(mode))
			{
				if (allowVirtualSpace)
					return visualColumn - 1;
				else
					return this.VisualLength;
			}
			// skip elements that start after or at visualColumn
			for (i = elements.Count - 1; i >= 0; i--)
			{
				if (elements[i].VisualColumn < visualColumn)
					break;
			}
			// search last element that has a caret stop
			for (; i >= 0; i--)
			{
				int pos = elements[i].GetNextCaretPosition(
					Math.Min(visualColumn, elements[i].VisualColumn + elements[i].VisualLength + 1),
					direction, mode);
				if (pos >= 0)
					return pos;
			}
			// If we've found nothing, and the first element doesn't handle line borders,
			// return the line start as normal caret stop.
			if (visualColumn > 0 && !elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode))
				return 0;
		}
		else
		{
			// Search Forwards:
			// If the first element doesn't handle line borders, return the line start as caret stop
			if (visualColumn < 0 && !elements[0].HandlesLineBorders && HasImplicitStopAtLineStart(mode))
				return 0;
			// skip elements that end before or at visualColumn
			for (i = 0; i < elements.Count; i++)
			{
				if (elements[i].VisualColumn + elements[i].VisualLength > visualColumn)
					break;
			}
			// search first element that has a caret stop
			for (; i < elements.Count; i++)
			{
				int pos = elements[i].GetNextCaretPosition(
					Math.Max(visualColumn, elements[i].VisualColumn - 1),
					direction, mode);
				if (pos >= 0)
					return pos;
			}
			// if we've found nothing, and the last element doesn't handle line borders,
			// return the line end as caret stop
			if ((allowVirtualSpace || !elements[elements.Count - 1].HandlesLineBorders) && HasImplicitStopAtLineEnd(mode))
			{
				if (visualColumn < this.VisualLength)
					return this.VisualLength;
				else if (allowVirtualSpace)
					return visualColumn + 1;
			}
		}
		// we've found nothing, return -1 and let the caret search continue in the next line
		return -1;
	}

	static bool HasStopsInVirtualSpace(CaretPositioningMode mode)
	{
		return mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint;
	}

	static bool HasImplicitStopAtLineStart(CaretPositioningMode mode)
	{
		return mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint;
	}

	static bool HasImplicitStopAtLineEnd(CaretPositioningMode mode)
	{
		// consistent with HasImplicitStopAtLineStart; might depend on mode in the future
		return true;
	}

	/// <summary>
	/// Paints this visual line onto a canvas, with the line's top-left corner at
	/// <paramref name="origin"/>: element backgrounds first, then the text (per-run colors baked
	/// into the layout), then underline/strikethrough decorations.
	/// </summary>
	internal void Draw(SKCanvas canvas, SKPoint origin)
	{
		if (canvas == null)
			throw new ArgumentNullException(nameof(canvas));
		var layout = layoutResult;
		if (layout == null)
			return;

		// (a) element backgrounds - the element-level BackgroundBrush wins over the run-level one.
		foreach (VisualLineElement element in elements)
		{
			if (element.LayoutLength == 0)
				continue;
			SKColor? background = VisualLineElementTextRunProperties.GetSolidColor(element.BackgroundBrush)
				?? VisualLineElementTextRunProperties.GetSolidColor(element.TextRunProperties?.BackgroundBrush);
			if (background == null)
				continue;
			using var backgroundPaint = new SKPaint { Color = background.Value, Style = SKPaintStyle.Fill };
			foreach (SKRect r in layout.GetSelectionRects(element.LayoutStart, element.LayoutLength))
			{
				canvas.DrawRect(SKRect.Create(origin.X + r.Left, origin.Y + r.Top, r.Width, r.Height), backgroundPaint);
			}
		}

		// (b) the text itself.
		using (var textPaint = new SKPaint { Color = defaultTextColor, IsAntialias = true })
		{
			layout.Draw(canvas, origin, textPaint);
		}

		// (c) underline/strikethrough decorations from the row metrics.
		foreach (VisualLineElement element in elements)
		{
			var properties = element.TextRunProperties;
			if (properties == null || element.LayoutLength == 0)
				continue;
			if (!properties.Underline && !properties.Strikethrough)
				continue;
			SKColor color = properties.ForegroundColor ?? defaultTextColor;
			float thickness = Math.Max(1f, (float)(properties.FontSize / 16.0));
			using var decorationPaint = new SKPaint { Color = color, Style = SKPaintStyle.Fill };
			foreach (SKRect r in layout.GetSelectionRects(element.LayoutStart, element.LayoutLength))
			{
				TextLineLayout? row = FindRowForLayoutY((r.Top + r.Bottom) / 2);
				if (row == null)
					continue;
				if (properties.Underline)
				{
					float y = origin.Y + (float)(row.Top + row.Baseline) + thickness;
					canvas.DrawRect(SKRect.Create(origin.X + r.Left, y, r.Width, thickness), decorationPaint);
				}
				if (properties.Strikethrough)
				{
					float y = origin.Y + (float)(row.Top + row.Baseline - properties.FontSize * 0.3);
					canvas.DrawRect(SKRect.Create(origin.X + r.Left, y, r.Width, thickness), decorationPaint);
				}
			}
		}
	}

	TextLineLayout? FindRowForLayoutY(double layoutY)
	{
		if (textLines == null || textLines.Count == 0)
			return null;
		foreach (TextLineLayout row in textLines)
		{
			if (layoutY < row.Top + row.Height)
				return row;
		}
		return textLines[textLines.Count - 1];
	}

	internal double ComputeRowWidth(TextLineLayout row)
	{
		var layout = layoutResult;
		if (layout == null || row.LayoutLength == 0)
			return 0;
		double width = 0;
		foreach (SKRect r in layout.GetSelectionRects(row.LayoutStart, row.LayoutLength))
		{
			double mid = (r.Top + r.Bottom) / 2;
			if (mid < row.Top - Epsilon || mid >= row.Top + row.Height + Epsilon)
				continue;
			if (r.Right > width)
				width = r.Right;
		}
		return width;
	}

	void CheckRowOwnership(TextLineLayout textLine)
	{
		if (textLine.VisualLine != this)
			throw new ArgumentException("textLine is not a line in this VisualLine");
	}
}
