#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/CaretNavigationCommandHandler.cs in the AvalonEdit
//repo (MIT). The movement/selection logic is transliterated exactly; only the binding registration
//header changed to the port's own command system (EditorCommand/EditorCommandBinding/KeyBinding in
//Editing/Input): the WPF EditingCommands/ApplicationCommands become EditorCommands members, Alt is
//VirtualKeyModifiers.Menu, and the upstream CreateFrozenKeyBinding/WorkaroundWPFMemoryLeak calls
//are gone because the port's shared binding instances are plainly immutable. WPF TextLines are the
//port's TextLineLayout rows; where upstream subtracted TextLine.NewlineLength, the port's rows
//never contain the line delimiter, so there is nothing to subtract.

/// <summary>
/// Specifies the type of caret movement performed by the caret navigation commands.
/// </summary>
enum CaretMovementType
{
	/// <summary>No movement (used to delete the current selection without moving the caret).</summary>
	None,
	/// <summary>Move one character to the left.</summary>
	CharLeft,
	/// <summary>Move one character to the right.</summary>
	CharRight,
	/// <summary>Move one codepoint backwards (backspace semantics).</summary>
	Backspace,
	/// <summary>Move one word to the left.</summary>
	WordLeft,
	/// <summary>Move one word to the right.</summary>
	WordRight,
	/// <summary>Move up one line.</summary>
	LineUp,
	/// <summary>Move down one line.</summary>
	LineDown,
	/// <summary>Move up one page.</summary>
	PageUp,
	/// <summary>Move down one page.</summary>
	PageDown,
	/// <summary>Move to the start of the line.</summary>
	LineStart,
	/// <summary>Move to the end of the line.</summary>
	LineEnd,
	/// <summary>Move to the start of the document.</summary>
	DocumentStart,
	/// <summary>Move to the end of the document.</summary>
	DocumentEnd
}

static class CaretNavigationCommandHandler
{
	/// <summary>
	/// Creates a new <see cref="TextAreaInputHandler"/> for the text area.
	/// </summary>
	public static TextAreaInputHandler Create(TextArea textArea)
	{
		TextAreaInputHandler handler = new TextAreaInputHandler(textArea);
		foreach (EditorCommandBinding binding in CommandBindings)
			handler.CommandBindings.Add(binding);
		foreach (KeyBinding binding in InputBindings)
			handler.InputBindings.Add(binding);
		return handler;
	}

	static readonly List<EditorCommandBinding> CommandBindings = new List<EditorCommandBinding>();
	static readonly List<KeyBinding> InputBindings = new List<KeyBinding>();

	static void AddBinding(EditorCommand command, VirtualKeyModifiers modifiers, VirtualKey key, ExecutedEditorCommandEventHandler handler)
	{
		CommandBindings.Add(new EditorCommandBinding(command, handler));
		InputBindings.Add(new KeyBinding(command, key, modifiers));
	}

	static CaretNavigationCommandHandler()
	{
		const VirtualKeyModifiers None = VirtualKeyModifiers.None;
		const VirtualKeyModifiers Ctrl = VirtualKeyModifiers.Control;
		const VirtualKeyModifiers Shift = VirtualKeyModifiers.Shift;
		const VirtualKeyModifiers Alt = VirtualKeyModifiers.Menu;

		AddBinding(EditorCommands.MoveLeftByCharacter, None, VirtualKey.Left, OnMoveCaret(CaretMovementType.CharLeft));
		AddBinding(EditorCommands.SelectLeftByCharacter, Shift, VirtualKey.Left, OnMoveCaretExtendSelection(CaretMovementType.CharLeft));
		AddBinding(RectangleSelection.BoxSelectLeftByCharacter, Alt | Shift, VirtualKey.Left, OnMoveCaretBoxSelection(CaretMovementType.CharLeft));
		AddBinding(EditorCommands.MoveRightByCharacter, None, VirtualKey.Right, OnMoveCaret(CaretMovementType.CharRight));
		AddBinding(EditorCommands.SelectRightByCharacter, Shift, VirtualKey.Right, OnMoveCaretExtendSelection(CaretMovementType.CharRight));
		AddBinding(RectangleSelection.BoxSelectRightByCharacter, Alt | Shift, VirtualKey.Right, OnMoveCaretBoxSelection(CaretMovementType.CharRight));

		AddBinding(EditorCommands.MoveLeftByWord, Ctrl, VirtualKey.Left, OnMoveCaret(CaretMovementType.WordLeft));
		AddBinding(EditorCommands.SelectLeftByWord, Ctrl | Shift, VirtualKey.Left, OnMoveCaretExtendSelection(CaretMovementType.WordLeft));
		AddBinding(RectangleSelection.BoxSelectLeftByWord, Ctrl | Alt | Shift, VirtualKey.Left, OnMoveCaretBoxSelection(CaretMovementType.WordLeft));
		AddBinding(EditorCommands.MoveRightByWord, Ctrl, VirtualKey.Right, OnMoveCaret(CaretMovementType.WordRight));
		AddBinding(EditorCommands.SelectRightByWord, Ctrl | Shift, VirtualKey.Right, OnMoveCaretExtendSelection(CaretMovementType.WordRight));
		AddBinding(RectangleSelection.BoxSelectRightByWord, Ctrl | Alt | Shift, VirtualKey.Right, OnMoveCaretBoxSelection(CaretMovementType.WordRight));

		AddBinding(EditorCommands.MoveUpByLine, None, VirtualKey.Up, OnMoveCaret(CaretMovementType.LineUp));
		AddBinding(EditorCommands.SelectUpByLine, Shift, VirtualKey.Up, OnMoveCaretExtendSelection(CaretMovementType.LineUp));
		AddBinding(RectangleSelection.BoxSelectUpByLine, Alt | Shift, VirtualKey.Up, OnMoveCaretBoxSelection(CaretMovementType.LineUp));
		AddBinding(EditorCommands.MoveDownByLine, None, VirtualKey.Down, OnMoveCaret(CaretMovementType.LineDown));
		AddBinding(EditorCommands.SelectDownByLine, Shift, VirtualKey.Down, OnMoveCaretExtendSelection(CaretMovementType.LineDown));
		AddBinding(RectangleSelection.BoxSelectDownByLine, Alt | Shift, VirtualKey.Down, OnMoveCaretBoxSelection(CaretMovementType.LineDown));

		AddBinding(EditorCommands.MoveDownByPage, None, VirtualKey.PageDown, OnMoveCaret(CaretMovementType.PageDown));
		AddBinding(EditorCommands.SelectDownByPage, Shift, VirtualKey.PageDown, OnMoveCaretExtendSelection(CaretMovementType.PageDown));
		AddBinding(EditorCommands.MoveUpByPage, None, VirtualKey.PageUp, OnMoveCaret(CaretMovementType.PageUp));
		AddBinding(EditorCommands.SelectUpByPage, Shift, VirtualKey.PageUp, OnMoveCaretExtendSelection(CaretMovementType.PageUp));

		AddBinding(EditorCommands.MoveToLineStart, None, VirtualKey.Home, OnMoveCaret(CaretMovementType.LineStart));
		AddBinding(EditorCommands.SelectToLineStart, Shift, VirtualKey.Home, OnMoveCaretExtendSelection(CaretMovementType.LineStart));
		AddBinding(RectangleSelection.BoxSelectToLineStart, Alt | Shift, VirtualKey.Home, OnMoveCaretBoxSelection(CaretMovementType.LineStart));
		AddBinding(EditorCommands.MoveToLineEnd, None, VirtualKey.End, OnMoveCaret(CaretMovementType.LineEnd));
		AddBinding(EditorCommands.SelectToLineEnd, Shift, VirtualKey.End, OnMoveCaretExtendSelection(CaretMovementType.LineEnd));
		AddBinding(RectangleSelection.BoxSelectToLineEnd, Alt | Shift, VirtualKey.End, OnMoveCaretBoxSelection(CaretMovementType.LineEnd));

		AddBinding(EditorCommands.MoveToDocumentStart, Ctrl, VirtualKey.Home, OnMoveCaret(CaretMovementType.DocumentStart));
		AddBinding(EditorCommands.SelectToDocumentStart, Ctrl | Shift, VirtualKey.Home, OnMoveCaretExtendSelection(CaretMovementType.DocumentStart));
		AddBinding(EditorCommands.MoveToDocumentEnd, Ctrl, VirtualKey.End, OnMoveCaret(CaretMovementType.DocumentEnd));
		AddBinding(EditorCommands.SelectToDocumentEnd, Ctrl | Shift, VirtualKey.End, OnMoveCaretExtendSelection(CaretMovementType.DocumentEnd));

		CommandBindings.Add(new EditorCommandBinding(EditorCommands.SelectAll, OnSelectAll));
	}

	static void OnSelectAll(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			args.Handled = true;
			textArea.Caret.Offset = textArea.Document.TextLength;
			textArea.Selection = SimpleSelection.Create(textArea, 0, textArea.Document.TextLength);
		}
	}

	static TextArea? GetTextArea(object target)
	{
		return target as TextArea;
	}

	static ExecutedEditorCommandEventHandler OnMoveCaret(CaretMovementType direction)
	{
		return (target, args) => {
			TextArea? textArea = GetTextArea(target);
			if (textArea != null && textArea.Document != null)
			{
				args.Handled = true;
				textArea.ClearSelection();
				MoveCaret(textArea, direction);
				textArea.Caret.BringCaretToView();
			}
		};
	}

	static ExecutedEditorCommandEventHandler OnMoveCaretExtendSelection(CaretMovementType direction)
	{
		return (target, args) => {
			TextArea? textArea = GetTextArea(target);
			if (textArea != null && textArea.Document != null)
			{
				args.Handled = true;
				TextViewPosition oldPosition = textArea.Caret.Position;
				MoveCaret(textArea, direction);
				textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
				textArea.Caret.BringCaretToView();
			}
		};
	}

	static ExecutedEditorCommandEventHandler OnMoveCaretBoxSelection(CaretMovementType direction)
	{
		return (target, args) => {
			TextArea? textArea = GetTextArea(target);
			if (textArea != null && textArea.Document != null)
			{
				args.Handled = true;
				// First, convert the selection into a rectangle selection
				// (this is required so that virtual space gets enabled for the caret movement)
				if (textArea.Options.EnableRectangularSelection && !(textArea.Selection is RectangleSelection))
				{
					if (textArea.Selection.IsEmpty)
					{
						textArea.Selection = new RectangleSelection(textArea, textArea.Caret.Position, textArea.Caret.Position);
					}
					else
					{
						// Convert normal selection to rectangle selection
						textArea.Selection = new RectangleSelection(textArea, textArea.Selection.StartPosition, textArea.Caret.Position);
					}
				}
				// Now move the caret and extend the selection
				TextViewPosition oldPosition = textArea.Caret.Position;
				MoveCaret(textArea, direction);
				textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
				textArea.Caret.BringCaretToView();
			}
		};
	}

	#region Caret movement
	internal static void MoveCaret(TextArea textArea, CaretMovementType direction)
	{
		double desiredXPos = textArea.Caret.DesiredXPos;

		if (textArea.FlowDirection == FlowDirection.RightToLeft)
		{
			if (direction == CaretMovementType.CharLeft)
			{
				direction = CaretMovementType.CharRight;
			}
			else if (direction == CaretMovementType.CharRight)
			{
				direction = CaretMovementType.CharLeft;
			}
			else if (direction == CaretMovementType.WordRight)
			{
				direction = CaretMovementType.WordLeft;
			}
			else if (direction == CaretMovementType.WordLeft)
			{
				direction = CaretMovementType.WordRight;
			}
		}

		textArea.Caret.Position = GetNewCaretPosition(textArea.TextView, textArea.Caret.Position, direction, textArea.Selection.EnableVirtualSpace, ref desiredXPos);
		textArea.Caret.DesiredXPos = desiredXPos;
	}

	internal static TextViewPosition GetNewCaretPosition(TextView textView, TextViewPosition caretPosition, CaretMovementType direction, bool enableVirtualSpace, ref double desiredXPos)
	{
		switch (direction)
		{
			case CaretMovementType.None:
				return caretPosition;
			case CaretMovementType.DocumentStart:
				desiredXPos = double.NaN;
				return new TextViewPosition(0, 0);
			case CaretMovementType.DocumentEnd:
				desiredXPos = double.NaN;
				return new TextViewPosition(textView.Document.GetLocation(textView.Document.TextLength));
		}
		DocumentLine caretLine = textView.Document.GetLineByNumber(caretPosition.Line);
		VisualLine visualLine = textView.GetOrConstructVisualLine(caretLine);
		TextLineLayout textLine = visualLine.GetTextLine(caretPosition.VisualColumn, caretPosition.IsAtEndOfLine);
		switch (direction)
		{
			case CaretMovementType.CharLeft:
				desiredXPos = double.NaN;
				// do not move caret to previous line in virtual space
				if (caretPosition.VisualColumn == 0 && enableVirtualSpace)
					return caretPosition;
				return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.Normal, enableVirtualSpace);
			case CaretMovementType.Backspace:
				desiredXPos = double.NaN;
				return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.EveryCodepoint, enableVirtualSpace);
			case CaretMovementType.CharRight:
				desiredXPos = double.NaN;
				return GetNextCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.Normal, enableVirtualSpace);
			case CaretMovementType.WordLeft:
				desiredXPos = double.NaN;
				return GetPrevCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.WordStart, enableVirtualSpace);
			case CaretMovementType.WordRight:
				desiredXPos = double.NaN;
				return GetNextCaretPosition(textView, caretPosition, visualLine, CaretPositioningMode.WordStart, enableVirtualSpace);
			case CaretMovementType.LineUp:
			case CaretMovementType.LineDown:
			case CaretMovementType.PageUp:
			case CaretMovementType.PageDown:
				return GetUpDownCaretPosition(textView, caretPosition, direction, visualLine, textLine, enableVirtualSpace, ref desiredXPos);
			case CaretMovementType.LineStart:
				desiredXPos = double.NaN;
				return GetStartOfLineCaretPosition(caretPosition.VisualColumn, visualLine, textLine, enableVirtualSpace);
			case CaretMovementType.LineEnd:
				desiredXPos = double.NaN;
				return GetEndOfLineCaretPosition(visualLine, textLine);
			default:
				throw new NotSupportedException(direction.ToString());
		}
	}
	#endregion

	#region Home/End
	static TextViewPosition GetStartOfLineCaretPosition(int oldVC, VisualLine visualLine, TextLineLayout textLine, bool enableVirtualSpace)
	{
		int newVC = visualLine.GetTextLineVisualStartColumn(textLine);
		if (newVC == 0)
			newVC = visualLine.GetNextCaretPosition(newVC - 1, LogicalDirection.Forward, CaretPositioningMode.WordStart, enableVirtualSpace);
		if (newVC < 0)
			throw ThrowUtil.NoValidCaretPosition();
		// when the caret is already at the start of the text, jump to start before whitespace
		if (newVC == oldVC)
			newVC = 0;
		return visualLine.GetTextViewPosition(newVC);
	}

	static TextViewPosition GetEndOfLineCaretPosition(VisualLine visualLine, TextLineLayout textLine)
	{
		//was previously: upstream computed GetTextLineVisualStartColumn(textLine) + textLine.Length
		//- textLine.NewlineLength; the port's layout rows never contain the line delimiter, so
		//NewlineLength is always zero here.
		int newVC = visualLine.GetTextLineVisualStartColumn(textLine) + textLine.Length;
		TextViewPosition pos = visualLine.GetTextViewPosition(newVC);
		pos.IsAtEndOfLine = true;
		return pos;
	}
	#endregion

	#region By-character / By-word movement
	static TextViewPosition GetNextCaretPosition(TextView textView, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, bool enableVirtualSpace)
	{
		int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Forward, mode, enableVirtualSpace);
		if (pos >= 0)
		{
			return visualLine.GetTextViewPosition(pos);
		}
		else
		{
			// move to start of next line
			DocumentLine? nextDocumentLine = visualLine.LastDocumentLine.NextLine;
			if (nextDocumentLine != null)
			{
				VisualLine nextLine = textView.GetOrConstructVisualLine(nextDocumentLine);
				pos = nextLine.GetNextCaretPosition(-1, LogicalDirection.Forward, mode, enableVirtualSpace);
				if (pos < 0)
					throw ThrowUtil.NoValidCaretPosition();
				return nextLine.GetTextViewPosition(pos);
			}
			else
			{
				// at end of document
				Debug.Assert(visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength == textView.Document.TextLength);
				return new TextViewPosition(textView.Document.GetLocation(textView.Document.TextLength));
			}
		}
	}

	static TextViewPosition GetPrevCaretPosition(TextView textView, TextViewPosition caretPosition, VisualLine visualLine, CaretPositioningMode mode, bool enableVirtualSpace)
	{
		int pos = visualLine.GetNextCaretPosition(caretPosition.VisualColumn, LogicalDirection.Backward, mode, enableVirtualSpace);
		if (pos >= 0)
		{
			return visualLine.GetTextViewPosition(pos);
		}
		else
		{
			// move to end of previous line
			DocumentLine? previousDocumentLine = visualLine.FirstDocumentLine.PreviousLine;
			if (previousDocumentLine != null)
			{
				VisualLine previousLine = textView.GetOrConstructVisualLine(previousDocumentLine);
				pos = previousLine.GetNextCaretPosition(previousLine.VisualLength + 1, LogicalDirection.Backward, mode, enableVirtualSpace);
				if (pos < 0)
					throw ThrowUtil.NoValidCaretPosition();
				return previousLine.GetTextViewPosition(pos);
			}
			else
			{
				// at start of document
				Debug.Assert(visualLine.FirstDocumentLine.Offset == 0);
				return new TextViewPosition(0, 0);
			}
		}
	}
	#endregion

	#region Line+Page up/down
	static TextViewPosition GetUpDownCaretPosition(TextView textView, TextViewPosition caretPosition, CaretMovementType direction, VisualLine visualLine, TextLineLayout textLine, bool enableVirtualSpace, ref double xPos)
	{
		// moving up/down happens using the desired visual X position
		if (double.IsNaN(xPos))
			xPos = visualLine.GetTextLineVisualXPosition(textLine, caretPosition.VisualColumn);
		// now find the TextLineLayout+VisualLine where the caret will end up in
		VisualLine targetVisualLine = visualLine;
		TextLineLayout? targetLine;
		int textLineIndex = visualLine.TextLines.IndexOf(textLine);
		switch (direction)
		{
			case CaretMovementType.LineUp:
			{
				// Move up: move to the previous TextLineLayout in the same visual line
				// or move to the last TextLineLayout of the previous visual line
				int prevLineNumber = visualLine.FirstDocumentLine.LineNumber - 1;
				if (textLineIndex > 0)
				{
					targetLine = visualLine.TextLines[textLineIndex - 1];
				}
				else if (prevLineNumber >= 1)
				{
					DocumentLine prevLine = textView.Document.GetLineByNumber(prevLineNumber);
					targetVisualLine = textView.GetOrConstructVisualLine(prevLine);
					targetLine = targetVisualLine.TextLines[targetVisualLine.TextLines.Count - 1];
				}
				else
				{
					targetLine = null;
				}
				break;
			}
			case CaretMovementType.LineDown:
			{
				// Move down: move to the next TextLineLayout in the same visual line
				// or move to the first TextLineLayout of the next visual line
				int nextLineNumber = visualLine.LastDocumentLine.LineNumber + 1;
				if (textLineIndex < visualLine.TextLines.Count - 1)
				{
					targetLine = visualLine.TextLines[textLineIndex + 1];
				}
				else if (nextLineNumber <= textView.Document.LineCount)
				{
					DocumentLine nextLine = textView.Document.GetLineByNumber(nextLineNumber);
					targetVisualLine = textView.GetOrConstructVisualLine(nextLine);
					targetLine = targetVisualLine.TextLines[0];
				}
				else
				{
					targetLine = null;
				}
				break;
			}
			case CaretMovementType.PageUp:
			case CaretMovementType.PageDown:
			{
				// Page up/down: find the target line using its visual position
				double yPos = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.LineMiddle);
				if (direction == CaretMovementType.PageUp)
					yPos -= textView.RenderSize.Height;
				else
					yPos += textView.RenderSize.Height;
				DocumentLine newLine = textView.GetDocumentLineByVisualTop(yPos);
				targetVisualLine = textView.GetOrConstructVisualLine(newLine);
				targetLine = targetVisualLine.GetTextLineByVisualYPosition(yPos);
				break;
			}
			default:
				throw new NotSupportedException(direction.ToString());
		}
		if (targetLine != null)
		{
			double yPos = targetVisualLine.GetTextLineVisualYPosition(targetLine, VisualYPosition.LineMiddle);
			int newVisualColumn = targetVisualLine.GetVisualColumn(new Point(xPos, yPos), enableVirtualSpace);

			// prevent wrapping to the next line; TODO: could 'IsAtEnd' help here?
			int targetLineStartCol = targetVisualLine.GetTextLineVisualStartColumn(targetLine);
			if (newVisualColumn >= targetLineStartCol + targetLine.Length)
			{
				if (newVisualColumn <= targetVisualLine.VisualLength)
					newVisualColumn = targetLineStartCol + targetLine.Length - 1;
			}
			return targetVisualLine.GetTextViewPosition(newVisualColumn);
		}
		else
		{
			return caretPosition;
		}
	}
	#endregion
}
