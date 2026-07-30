#nullable enable

using System;

using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/SelectionMouseHandler.cs in the AvalonEdit repo
//(MIT). The selection state machine (MouseSelectionMode) and the position mathematics are
//transliterated onto pointer events (PointerPressed/PointerMoved/PointerReleased with explicit
//pointer capture). Divergences:
//- ALL drag'n'drop paths are DROPPED (dropping/dragging text, GiveFeedback, QueryContinueDrag,
//  the drag undo-group descriptor): Options.EnableTextDragDrop is ignored, and a click inside
//  the selection positions the caret like any other click instead of entering
//  MouseSelectionMode.PossibleDragStart.
//- There is no ClickCount on pointer events; an own click counter reproduces double/triple
//  click detection (500 ms window, 4 DIP box).
//- The WPF QueryCursor handler (IBeam/Arrow shaping) has no counterpart seam and is dropped.
//- The WPF class handler for LostMouseCapture became a PointerCaptureLost subscription.
//- Programmatically setting MouseSelectionMode to Normal/Rectangular requires an already
//  captured pointer (WPF could capture the mouse device without an event).

/// <summary>
/// Handles selection of text using the mouse.
/// </summary>
sealed class SelectionMouseHandler : ITextAreaInputHandler
{
	readonly TextArea textArea;
	readonly ClickCounter clickCounter = new ClickCounter();

	MouseSelectionMode mode;
	AnchorSegment? startWord;
	Pointer? capturedPointer;

	#region Constructor + Attach + Detach
	internal SelectionMouseHandler(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		this.textArea = textArea;
	}

	TextArea ITextAreaInputHandler.TextArea {
		get { return textArea; }
	}

	void ITextAreaInputHandler.Attach()
	{
		textArea.PointerPressed += TextArea_PointerPressed;
		textArea.PointerMoved += TextArea_PointerMoved;
		textArea.PointerReleased += TextArea_PointerReleased;
		textArea.PointerCaptureLost += TextArea_PointerCaptureLost;
		textArea.DocumentChanged += TextArea_DocumentChanged;
		//was previously: also subscribed QueryCursor and OptionChanged (the latter to toggle the
		//drag'n'drop event subscriptions); both are drag'n'drop/cursor-shaping concerns without a
		//counterpart here.
	}

	void ITextAreaInputHandler.Detach()
	{
		mode = MouseSelectionMode.None;
		textArea.PointerPressed -= TextArea_PointerPressed;
		textArea.PointerMoved -= TextArea_PointerMoved;
		textArea.PointerReleased -= TextArea_PointerReleased;
		textArea.PointerCaptureLost -= TextArea_PointerCaptureLost;
		textArea.DocumentChanged -= TextArea_DocumentChanged;
	}

	void TextArea_DocumentChanged(object? sender, EventArgs e)
	{
		if (mode != MouseSelectionMode.None)
		{
			mode = MouseSelectionMode.None;
			ReleaseCapturedPointer();
		}
		startWord = null;
	}

	void TextArea_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
		//was previously: a class handler for Mouse.LostMouseCaptureEvent that reset the mode
		//when another element grabbed the capture.
		capturedPointer = null;
		mode = MouseSelectionMode.None;
	}

	bool CapturePointer(Pointer pointer)
	{
		if (textArea.CapturePointer(pointer))
		{
			capturedPointer = pointer;
			return true;
		}
		return false;
	}

	void ReleaseCapturedPointer()
	{
		Pointer? pointer = capturedPointer;
		capturedPointer = null;
		if (pointer != null)
		{
			textArea.ReleasePointerCapture(pointer);
		}
	}
	#endregion

	#region PointerPressed
	void TextArea_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		mode = MouseSelectionMode.None;
		if (textArea.Document == null)
		{
			// Avoid entering any selection mode when there's no document attached.
			return;
		}
		if (e.Handled)
			return;
		var point = e.GetCurrentPoint(textArea);
		if (!point.Properties.IsLeftButtonPressed)
			return;

		int clickCount = clickCounter.RegisterClick(point.Position);
		VirtualKeyModifiers modifiers = e.KeyModifiers;
		bool shift = (modifiers & VirtualKeyModifiers.Shift) == VirtualKeyModifiers.Shift;

		//was previously: with Options.EnableTextDragDrop, a single unshifted click inside the
		//selection captured the mouse and entered MouseSelectionMode.PossibleDragStart to allow
		//dragging the selected text; drag'n'drop is out of scope, so every click just moves the
		//caret.

		var oldPosition = textArea.Caret.Position;
		SetCaretOffsetToMousePosition(e);

		if (!shift)
		{
			textArea.ClearSelection();
		}
		if (CapturePointer(e.Pointer))
		{
			if ((modifiers & VirtualKeyModifiers.Menu) == VirtualKeyModifiers.Menu && textArea.Options.EnableRectangularSelection)
			{
				mode = MouseSelectionMode.Rectangular;
				if (shift && textArea.Selection is RectangleSelection)
				{
					textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
				}
			}
			else if (clickCount == 1 && (modifiers & VirtualKeyModifiers.Control) == 0)
			{
				mode = MouseSelectionMode.Normal;
				if (shift && !(textArea.Selection is RectangleSelection))
				{
					textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
				}
			}
			else
			{
				SimpleSegment startWord;
				if (clickCount == 3)
				{
					mode = MouseSelectionMode.WholeLine;
					startWord = GetLineAtMousePosition(e);
				}
				else
				{
					mode = MouseSelectionMode.WholeWord;
					startWord = GetWordAtMousePosition(e);
				}
				if (startWord == SimpleSegment.Invalid)
				{
					mode = MouseSelectionMode.None;
					ReleaseCapturedPointer();
					return;
				}
				if (shift && !textArea.Selection.IsEmpty && textArea.Selection.SurroundingSegment is ISegment surroundingSegment)
				{
					if (startWord.Offset < surroundingSegment.Offset)
					{
						textArea.Selection = textArea.Selection.SetEndpoint(new TextViewPosition(textArea.Document.GetLocation(startWord.Offset)));
					}
					else if (startWord.EndOffset > surroundingSegment.EndOffset)
					{
						textArea.Selection = textArea.Selection.SetEndpoint(new TextViewPosition(textArea.Document.GetLocation(startWord.EndOffset)));
					}
					ISegment? extendedSegment = textArea.Selection.SurroundingSegment;
					this.startWord = extendedSegment != null
						? new AnchorSegment(textArea.Document, extendedSegment)
						: new AnchorSegment(textArea.Document, startWord.Offset, startWord.Length);
				}
				else
				{
					textArea.Selection = Selection.Create(textArea, startWord.Offset, startWord.EndOffset);
					this.startWord = new AnchorSegment(textArea.Document, startWord.Offset, startWord.Length);
				}
			}
		}
		e.Handled = true;
	}

	/// <summary>
	/// Gets/sets the active mouse selection mode. See <see cref="TextArea.MouseSelectionMode"/>.
	/// </summary>
	public MouseSelectionMode MouseSelectionMode {
		get { return mode; }
		set {
			if (mode == value)
				return;
			if (value == MouseSelectionMode.None)
			{
				mode = MouseSelectionMode.None;
				ReleaseCapturedPointer();
			}
			else if (capturedPointer != null)
			{
				//was previously: textArea.CaptureMouse() could acquire the mouse device without
				//an active pointer event; this framework captures per pointer, so activating a
				//mode programmatically requires an already captured pointer.
				switch (value)
				{
					case MouseSelectionMode.Normal:
					case MouseSelectionMode.Rectangular:
						mode = value;
						break;
					default:
						throw new NotImplementedException("Programmatically starting mouse selection is only supported for normal and rectangular selections.");
				}
			}
		}
	}
	#endregion

	#region Mouse Position <-> Text coordinates
	SimpleSegment GetWordAtMousePosition(PointerRoutedEventArgs e)
	{
		TextView textView = textArea.TextView;
		if (textView == null)
			return SimpleSegment.Invalid;
		Point pos = e.GetCurrentPoint(textView).Position;
		if (pos.Y < 0)
			pos.Y = 0;
		if (pos.Y > textView.ActualHeight)
			pos.Y = textView.ActualHeight;
		pos = new Point(pos.X + textView.HorizontalOffset, pos.Y + textView.VerticalOffset);
		VisualLine? line = textView.GetVisualLineFromVisualTop(pos.Y);
		if (line != null)
		{
			int visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);
			int wordStartVC = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
			if (wordStartVC == -1)
				wordStartVC = 0;
			int wordEndVC = line.GetNextCaretPosition(wordStartVC, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);
			if (wordEndVC == -1)
				wordEndVC = line.VisualLength;
			int relOffset = line.FirstDocumentLine.Offset;
			int wordStartOffset = line.GetRelativeOffset(wordStartVC) + relOffset;
			int wordEndOffset = line.GetRelativeOffset(wordEndVC) + relOffset;
			return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
		}
		else
		{
			return SimpleSegment.Invalid;
		}
	}

	SimpleSegment GetLineAtMousePosition(PointerRoutedEventArgs e)
	{
		TextView textView = textArea.TextView;
		if (textView == null)
			return SimpleSegment.Invalid;
		Point pos = e.GetCurrentPoint(textView).Position;
		if (pos.Y < 0)
			pos.Y = 0;
		if (pos.Y > textView.ActualHeight)
			pos.Y = textView.ActualHeight;
		pos = new Point(pos.X + textView.HorizontalOffset, pos.Y + textView.VerticalOffset);
		VisualLine? line = textView.GetVisualLineFromVisualTop(pos.Y);
		if (line != null)
		{
			return new SimpleSegment(line.StartOffset, line.LastDocumentLine.EndOffset - line.StartOffset);
		}
		else
		{
			return SimpleSegment.Invalid;
		}
	}

	int GetOffsetFromMousePosition(PointerRoutedEventArgs e, out int visualColumn, out bool isAtEndOfLine)
	{
		return GetOffsetFromMousePosition(e.GetCurrentPoint(textArea.TextView).Position, out visualColumn, out isAtEndOfLine);
	}

	int GetOffsetFromMousePosition(Point positionRelativeToTextView, out int visualColumn, out bool isAtEndOfLine)
	{
		visualColumn = 0;
		TextView textView = textArea.TextView;
		Point pos = positionRelativeToTextView;
		if (pos.Y < 0)
			pos.Y = 0;
		if (pos.Y > textView.ActualHeight)
			pos.Y = textView.ActualHeight;
		pos = new Point(pos.X + textView.HorizontalOffset, pos.Y + textView.VerticalOffset);
		if (pos.Y >= textView.DocumentHeight)
			pos.Y = textView.DocumentHeight - ExtensionMethods.Epsilon;
		VisualLine? line = textView.GetVisualLineFromVisualTop(pos.Y);
		if (line != null)
		{
			visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace, out isAtEndOfLine);
			return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
		}
		isAtEndOfLine = false;
		return -1;
	}

	int GetOffsetFromMousePositionFirstTextLineOnly(Point positionRelativeToTextView, out int visualColumn)
	{
		visualColumn = 0;
		TextView textView = textArea.TextView;
		Point pos = positionRelativeToTextView;
		if (pos.Y < 0)
			pos.Y = 0;
		if (pos.Y > textView.ActualHeight)
			pos.Y = textView.ActualHeight;
		pos = new Point(pos.X + textView.HorizontalOffset, pos.Y + textView.VerticalOffset);
		if (pos.Y >= textView.DocumentHeight)
			pos.Y = textView.DocumentHeight - ExtensionMethods.Epsilon;
		VisualLine? line = textView.GetVisualLineFromVisualTop(pos.Y);
		if (line != null)
		{
			visualColumn = line.GetVisualColumn(line.TextLines[0], pos.X, textArea.Selection.EnableVirtualSpace);
			return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
		}
		return -1;
	}
	#endregion

	#region PointerMoved
	void TextArea_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (e.Handled)
			return;
		if (mode == MouseSelectionMode.Normal || mode == MouseSelectionMode.WholeWord || mode == MouseSelectionMode.WholeLine || mode == MouseSelectionMode.Rectangular)
		{
			e.Handled = true;
			if (textArea.TextView.VisualLinesValid)
			{
				// If the visual lines are not valid, don't extend the selection.
				// Extending the selection forces a VisualLine refresh, and it is sufficient
				// to do that on PointerReleased, we don't have to do it every PointerMoved.
				ExtendSelectionToMouse(e);
			}
		}
	}
	#endregion

	#region ExtendSelection
	void SetCaretOffsetToMousePosition(PointerRoutedEventArgs e)
	{
		SetCaretOffsetToMousePosition(e, null);
	}

	void SetCaretOffsetToMousePosition(PointerRoutedEventArgs e, ISegment? allowedSegment)
	{
		int visualColumn;
		bool isAtEndOfLine;
		int offset;
		if (mode == MouseSelectionMode.Rectangular)
		{
			offset = GetOffsetFromMousePositionFirstTextLineOnly(e.GetCurrentPoint(textArea.TextView).Position, out visualColumn);
			isAtEndOfLine = true;
		}
		else
		{
			offset = GetOffsetFromMousePosition(e, out visualColumn, out isAtEndOfLine);
		}
		if (allowedSegment != null)
		{
			offset = offset.CoerceValue(allowedSegment.Offset, allowedSegment.EndOffset);
		}
		if (offset >= 0)
		{
			textArea.Caret.Position = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
			textArea.Caret.DesiredXPos = double.NaN;
		}
	}

	void ExtendSelectionToMouse(PointerRoutedEventArgs e)
	{
		TextViewPosition oldPosition = textArea.Caret.Position;
		if (mode == MouseSelectionMode.Normal || mode == MouseSelectionMode.Rectangular)
		{
			SetCaretOffsetToMousePosition(e);
			if (mode == MouseSelectionMode.Normal && textArea.Selection is RectangleSelection)
				textArea.Selection = new SimpleSelection(textArea, oldPosition, textArea.Caret.Position);
			else if (mode == MouseSelectionMode.Rectangular && !(textArea.Selection is RectangleSelection))
				textArea.Selection = new RectangleSelection(textArea, oldPosition, textArea.Caret.Position);
			else
				textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
		}
		else if (mode == MouseSelectionMode.WholeWord || mode == MouseSelectionMode.WholeLine)
		{
			var newWord = (mode == MouseSelectionMode.WholeLine) ? GetLineAtMousePosition(e) : GetWordAtMousePosition(e);
			if (newWord != SimpleSegment.Invalid && startWord != null)
			{
				textArea.Selection = Selection.Create(textArea,
													  Math.Min(newWord.Offset, startWord.Offset),
													  Math.Max(newWord.EndOffset, startWord.EndOffset));
				// moves caret to start or end of selection
				if (newWord.Offset < startWord.Offset)
					textArea.Caret.Offset = newWord.Offset;
				else
					textArea.Caret.Offset = Math.Max(newWord.EndOffset, startWord.EndOffset);
			}
		}
		textArea.Caret.BringCaretToView(5.0);
	}
	#endregion

	#region PointerReleased
	void TextArea_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (mode == MouseSelectionMode.None || e.Handled)
			return;
		e.Handled = true;
		if (mode == MouseSelectionMode.PossibleDragStart)
		{
			//was previously: reached when a click on the selection did not become a drag; the
			//drag paths are dropped, so this state only occurs if a consumer sets it manually.
			SetCaretOffsetToMousePosition(e);
			textArea.ClearSelection();
		}
		else if (mode == MouseSelectionMode.Normal || mode == MouseSelectionMode.WholeWord || mode == MouseSelectionMode.WholeLine || mode == MouseSelectionMode.Rectangular)
		{
			ExtendSelectionToMouse(e);
		}
		mode = MouseSelectionMode.None;
		ReleaseCapturedPointer();
	}
	#endregion

	#region ClickCounter
	//was previously: WPF supplied MouseButtonEventArgs.ClickCount; this framework's pointer
	//events carry no click count, so double/triple clicks are detected here: clicks within
	//500 ms of each other and within a 4x4 DIP box continue the streak.
	sealed class ClickCounter
	{
		const long MultiClickTimeMilliseconds = 500;
		const double MultiClickBoxSize = 4;

		int count;
		long lastClickTime;
		Point lastClickPosition;

		/// <summary>
		/// Registers a click at the given position (in text-area coordinates) and returns the
		/// click count: 1 for a single click, 2 for a double click, 3 for a triple click, ...
		/// </summary>
		internal int RegisterClick(Point position)
		{
			long now = Environment.TickCount64;
			if (count > 0
				&& now - lastClickTime <= MultiClickTimeMilliseconds
				&& Math.Abs(position.X - lastClickPosition.X) <= MultiClickBoxSize / 2
				&& Math.Abs(position.Y - lastClickPosition.Y) <= MultiClickBoxSize / 2)
			{
				count++;
			}
			else
			{
				count = 1;
			}
			lastClickTime = now;
			lastClickPosition = position;
			return count;
		}
	}
	#endregion
}
