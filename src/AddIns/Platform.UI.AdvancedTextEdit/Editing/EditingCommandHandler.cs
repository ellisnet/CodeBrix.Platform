#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/EditingCommandHandler.cs in the AvalonEdit repo
//(MIT). The text transformation/deletion/tab/case logic is transliterated exactly; the binding
//registration header changed to the port's own command system (EditorCommand/EditorCommandBinding/
//KeyBinding in Editing/Input), where WPF's EditingCommands.Delete became
//EditorCommands.DeleteNextCharacter (name collision with the ApplicationCommands.Delete
//equivalent). Clipboard access moved from System.Windows.Clipboard/DataObject to
//Windows.ApplicationModel.DataTransfer (DataPackage/DataPackageView/Clipboard.SetContent+Flush/
//Clipboard.GetContent); reading clipboard text is asynchronous in this framework, so the paste
//command completes in an awaited continuation (see OnPaste). The WPF DataObject.Copying/
//SettingData/Pasting attached events are replaced by equivalent events on the TextArea surface,
//raised through textArea.OnDataObjectCopying/OnDataObjectSettingData/OnDataObjectPasting with
//the port's DataObjectCopyingEventArgs/DataObjectSettingDataEventArgs/DataObjectPastingEventArgs.

/// <summary>
/// We re-use the EditorCommandBinding and KeyBinding instances between multiple text areas,
/// so this class is static.
/// </summary>
static class EditingCommandHandler
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

	static EditingCommandHandler()
	{
		CommandBindings.Add(new EditorCommandBinding(EditorCommands.Delete, OnDelete(CaretMovementType.None), CanDelete));
		AddBinding(EditorCommands.DeleteNextCharacter, VirtualKeyModifiers.None, VirtualKey.Delete, OnDelete(CaretMovementType.CharRight));
		AddBinding(EditorCommands.DeleteNextWord, VirtualKeyModifiers.Control, VirtualKey.Delete, OnDelete(CaretMovementType.WordRight));
		AddBinding(EditorCommands.Backspace, VirtualKeyModifiers.None, VirtualKey.Back, OnDelete(CaretMovementType.Backspace));
		InputBindings.Add(new KeyBinding(EditorCommands.Backspace, VirtualKey.Back, VirtualKeyModifiers.Shift)); // make Shift-Backspace do the same as plain backspace
		AddBinding(EditorCommands.DeletePreviousWord, VirtualKeyModifiers.Control, VirtualKey.Back, OnDelete(CaretMovementType.WordLeft));
		AddBinding(EditorCommands.EnterParagraphBreak, VirtualKeyModifiers.None, VirtualKey.Enter, OnEnter);
		AddBinding(EditorCommands.EnterLineBreak, VirtualKeyModifiers.Shift, VirtualKey.Enter, OnEnter);
		AddBinding(EditorCommands.TabForward, VirtualKeyModifiers.None, VirtualKey.Tab, OnTab);
		AddBinding(EditorCommands.TabBackward, VirtualKeyModifiers.Shift, VirtualKey.Tab, OnShiftTab);

		CommandBindings.Add(new EditorCommandBinding(EditorCommands.Copy, OnCopy, CanCutOrCopy));
		CommandBindings.Add(new EditorCommandBinding(EditorCommands.Cut, OnCut, CanCutOrCopy));
		CommandBindings.Add(new EditorCommandBinding(EditorCommands.Paste, OnPaste, CanPaste));

		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ToggleOverstrike, OnToggleOverstrike));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.DeleteLine, OnDeleteLine));

		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.RemoveLeadingWhitespace, OnRemoveLeadingWhitespace));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.RemoveTrailingWhitespace, OnRemoveTrailingWhitespace));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ConvertToUppercase, OnConvertToUpperCase));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ConvertToLowercase, OnConvertToLowerCase));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ConvertToTitleCase, OnConvertToTitleCase));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.InvertCase, OnInvertCase));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ConvertTabsToSpaces, OnConvertTabsToSpaces));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ConvertSpacesToTabs, OnConvertSpacesToTabs));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ConvertLeadingTabsToSpaces, OnConvertLeadingTabsToSpaces));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.ConvertLeadingSpacesToTabs, OnConvertLeadingSpacesToTabs));
		CommandBindings.Add(new EditorCommandBinding(AdvancedTextEditCommands.IndentSelection, OnIndentSelection));
	}

	static TextArea? GetTextArea(object target)
	{
		return target as TextArea;
	}

	#region Text Transformation Helpers
	enum DefaultSegmentType
	{
		None,
		WholeDocument,
		CurrentLine
	}

	/// <summary>
	/// Calls transformLine on all lines in the selected range.
	/// transformLine needs to handle read-only segments!
	/// </summary>
	static void TransformSelectedLines(Action<TextArea, DocumentLine> transformLine, object target, ExecutedEditorCommandEventArgs args, DefaultSegmentType defaultSegmentType)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			using (textArea.Document.RunUpdate())
			{
				DocumentLine? start, end;
				if (textArea.Selection.IsEmpty)
				{
					if (defaultSegmentType == DefaultSegmentType.CurrentLine)
					{
						start = end = textArea.Document.GetLineByNumber(textArea.Caret.Line);
					}
					else if (defaultSegmentType == DefaultSegmentType.WholeDocument)
					{
						start = textArea.Document.Lines.First();
						end = textArea.Document.Lines.Last();
					}
					else
					{
						start = end = null;
					}
				}
				//was previously: read SurroundingSegment unconditionally; the port's property is
				//null for the empty selection, so the (unreachable) null case falls back to no-op.
				else if (textArea.Selection.SurroundingSegment is ISegment segment)
				{
					start = textArea.Document.GetLineByOffset(segment.Offset);
					end = textArea.Document.GetLineByOffset(segment.EndOffset);
					// don't include the last line if no characters on it are selected
					if (start != end && end.Offset == segment.EndOffset)
						end = end.PreviousLine;
				}
				else
				{
					start = end = null;
				}
				if (start != null)
				{
					transformLine(textArea, start);
					while (start != end)
					{
						start = start.NextLine;
						if (start == null)
							break;
						transformLine(textArea, start);
					}
				}
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	/// <summary>
	/// Calls transformLine on all writable segment in the selected range.
	/// </summary>
	static void TransformSelectedSegments(Action<TextArea, ISegment> transformSegment, object target, ExecutedEditorCommandEventArgs args, DefaultSegmentType defaultSegmentType)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			using (textArea.Document.RunUpdate())
			{
				IEnumerable<ISegment>? segments;
				if (textArea.Selection.IsEmpty)
				{
					if (defaultSegmentType == DefaultSegmentType.CurrentLine)
					{
						segments = new ISegment[] { textArea.Document.GetLineByNumber(textArea.Caret.Line) };
					}
					else if (defaultSegmentType == DefaultSegmentType.WholeDocument)
					{
						segments = textArea.Document.Lines.Cast<ISegment>();
					}
					else
					{
						segments = null;
					}
				}
				else
				{
					segments = textArea.Selection.Segments.Cast<ISegment>();
				}
				if (segments != null)
				{
					foreach (ISegment segment in segments.Reverse())
					{
						foreach (ISegment writableSegment in textArea.GetDeletableSegments(segment).Reverse())
						{
							transformSegment(textArea, writableSegment);
						}
					}
				}
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}
	#endregion

	#region EnterLineBreak
	static void OnEnter(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.IsKeyboardFocused)
		{
			textArea.PerformTextInput("\n");
			args.Handled = true;
		}
	}
	#endregion

	#region Tab
	static void OnTab(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			using (textArea.Document.RunUpdate())
			{
				//was previously: if (textArea.Selection.IsMultiline) followed by an unconditional
				//SurroundingSegment read; the pattern makes the port's nullable property safe (a
				//multiline selection always has a surrounding segment).
				if (textArea.Selection.IsMultiline && textArea.Selection.SurroundingSegment is ISegment segment)
				{
					DocumentLine start = textArea.Document.GetLineByOffset(segment.Offset);
					DocumentLine? end = textArea.Document.GetLineByOffset(segment.EndOffset);
					// don't include the last line if no characters on it are selected
					if (start != end && end.Offset == segment.EndOffset)
						end = end.PreviousLine;
					DocumentLine current = start;
					while (true)
					{
						int offset = current.Offset;
						if (textArea.ReadOnlySectionProvider.CanInsert(offset))
							textArea.Document.Replace(offset, 0, textArea.Options.IndentationString, OffsetChangeMappingType.KeepAnchorBeforeInsertion);
						if (current == end)
							break;
						DocumentLine? next = current.NextLine;
						if (next == null)
							break;
						current = next;
					}
				}
				else
				{
					string indentationString = textArea.Options.GetIndentationString(textArea.Caret.Column);
					textArea.ReplaceSelectionWithText(indentationString);
				}
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	static void OnShiftTab(object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedLines(
			delegate (TextArea textArea, DocumentLine line) {
				int offset = line.Offset;
				ISegment s = TextUtilities.GetSingleIndentationSegment(textArea.Document, offset, textArea.Options.IndentationSize);
				if (s.Length > 0)
				{
					ISegment? deletable = textArea.GetDeletableSegments(s).FirstOrDefault();
					if (deletable != null && deletable.Length > 0)
					{
						textArea.Document.Remove(deletable.Offset, deletable.Length);
					}
				}
			}, target, args, DefaultSegmentType.CurrentLine);
	}
	#endregion

	#region Delete
	static ExecutedEditorCommandEventHandler OnDelete(CaretMovementType caretMovement)
	{
		return (target, args) => {
			TextArea? textArea = GetTextArea(target);
			if (textArea != null && textArea.Document != null)
			{
				if (textArea.Selection.IsEmpty)
				{
					TextViewPosition startPos = textArea.Caret.Position;
					bool enableVirtualSpace = textArea.Options.EnableVirtualSpace;
					// When pressing delete; don't move the caret further into virtual space - instead delete the newline
					if (caretMovement == CaretMovementType.CharRight)
						enableVirtualSpace = false;
					double desiredXPos = textArea.Caret.DesiredXPos;
					TextViewPosition endPos = CaretNavigationCommandHandler.GetNewCaretPosition(
						textArea.TextView, startPos, caretMovement, enableVirtualSpace, ref desiredXPos);
					// GetNewCaretPosition may return (0,0) as new position,
					// thus we need to validate endPos before using it in the selection.
					if (endPos.Line < 1 || endPos.Column < 1)
						endPos = new TextViewPosition(Math.Max(endPos.Line, 1), Math.Max(endPos.Column, 1));
					// Don't do anything if the number of lines of a rectangular selection would be changed by the deletion.
					if (textArea.Selection is RectangleSelection && startPos.Line != endPos.Line)
						return;
					// Don't select the text to be deleted; just reuse the ReplaceSelectionWithText logic
					// Reuse the existing selection, so that we continue using the same logic
					textArea.Selection.StartSelectionOrSetEndpoint(startPos, endPos)
						.ReplaceSelectionWithText(string.Empty);
				}
				else
				{
					textArea.RemoveSelectedText();
				}
				textArea.Caret.BringCaretToView();
				args.Handled = true;
			}
		};
	}

	static void CanDelete(object target, CanExecuteEditorCommandEventArgs args)
	{
		// HasSomethingSelected for delete command
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			args.CanExecute = !textArea.Selection.IsEmpty;
			args.Handled = true;
		}
	}
	#endregion

	#region Clipboard commands
	static void CanCutOrCopy(object target, CanExecuteEditorCommandEventArgs args)
	{
		// HasSomethingSelected for copy and cut commands
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			args.CanExecute = textArea.Options.CutCopyWholeLine || !textArea.Selection.IsEmpty;
			args.Handled = true;
		}
	}

	static void OnCopy(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			if (textArea.Selection.IsEmpty && textArea.Options.CutCopyWholeLine)
			{
				DocumentLine currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
				CopyWholeLine(textArea, currentLine);
			}
			else
			{
				CopySelectedText(textArea);
			}
			args.Handled = true;
		}
	}

	static void OnCut(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			if (textArea.Selection.IsEmpty && textArea.Options.CutCopyWholeLine)
			{
				DocumentLine currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
				if (CopyWholeLine(textArea, currentLine))
				{
					ISegment[] segmentsToDelete = textArea.GetDeletableSegments(new SimpleSegment(currentLine.Offset, currentLine.TotalLength));
					for (int i = segmentsToDelete.Length - 1; i >= 0; i--)
					{
						textArea.Document.Remove(segmentsToDelete[i]);
					}
				}
			}
			else
			{
				if (CopySelectedText(textArea))
					textArea.RemoveSelectedText();
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}

	static bool CopySelectedText(TextArea textArea)
	{
		var data = textArea.Selection.CreateDataObject(textArea);
		var copyingEventArgs = new DataObjectCopyingEventArgs(data, false);
		textArea.OnDataObjectCopying(copyingEventArgs);
		if (copyingEventArgs.CommandCancelled)
			return false;

		try
		{
			//was previously: Clipboard.SetDataObject(data, copy: true); this framework's clipboard
			//equivalent is SetContent followed by Flush.
			Clipboard.SetContent(data);
			Clipboard.Flush();
		}
		catch (ExternalException)
		{
			// Apparently this exception sometimes happens randomly.
			// The MS controls just ignore it, so we'll do the same.
		}

		string text = textArea.Selection.GetText();
		text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);
		textArea.OnTextCopied(new TextEventArgs(text));
		return true;
	}

	const string LineSelectedType = "MSDEVLineSelect";  // This is the type VS 2003 and 2005 use for flagging a whole line copy

	public static bool ConfirmDataFormat(TextArea textArea, DataPackage dataObject, string format)
	{
		var e = new DataObjectSettingDataEventArgs(dataObject, format);
		textArea.OnDataObjectSettingData(e);
		return !e.CommandCancelled;
	}

	static bool CopyWholeLine(TextArea textArea, DocumentLine line)
	{
		ISegment wholeLine = new SimpleSegment(line.Offset, line.TotalLength);
		string text = textArea.Document.GetText(wholeLine);
		// Ensure we use the appropriate newline sequence for the OS
		text = TextUtilities.NormalizeNewLines(text, Environment.NewLine);
		DataPackage data = new DataPackage();
		if (ConfirmDataFormat(textArea, data, StandardDataFormats.Text))
			data.SetText(text);

		// Also copy text in HTML format to clipboard - good for pasting text into Word
		// or into other rich text editors.
		if (ConfirmDataFormat(textArea, data, StandardDataFormats.Html))
		{
			IHighlighter? highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
			HtmlClipboard.SetHtml(data, HtmlClipboard.CreateHtmlFragment(textArea.Document, highlighter, wholeLine, new HtmlOptions(textArea.Options)));
		}

		if (ConfirmDataFormat(textArea, data, LineSelectedType))
		{
			//was previously: a 1-byte MemoryStream written through DataObject.SetData; only the
			//PRESENCE of this format matters to the paste side, so the data package stores a
			//simple boolean marker instead.
			data.SetData(LineSelectedType, true);
		}

		var copyingEventArgs = new DataObjectCopyingEventArgs(data, false);
		textArea.OnDataObjectCopying(copyingEventArgs);
		if (copyingEventArgs.CommandCancelled)
			return false;

		try
		{
			//was previously: Clipboard.SetDataObject(data, copy: true); this framework's clipboard
			//equivalent is SetContent followed by Flush.
			Clipboard.SetContent(data);
			Clipboard.Flush();
		}
		catch (ExternalException)
		{
			// Apparently this exception sometimes happens randomly.
			// The MS controls just ignore it, so we'll do the same.
			return false;
		}
		textArea.OnTextCopied(new TextEventArgs(text));
		return true;
	}

	static void CanPaste(object target, CanExecuteEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			//was previously: Clipboard.ContainsText(); this framework peeks at the current
			//clipboard content's available formats instead (GetContent can return null when no
			//clipboard is available on the platform).
			DataPackageView? content = Clipboard.GetContent();
			args.CanExecute = textArea.ReadOnlySectionProvider.CanInsert(textArea.Caret.Offset)
				&& content != null && content.Contains(StandardDataFormats.Text);
			args.Handled = true;
		}
	}

	static void OnPaste(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			DataPackageView? dataObject = Clipboard.GetContent();
			if (dataObject == null)
				return;

			var pastingEventArgs = new DataObjectPastingEventArgs(dataObject, false, StandardDataFormats.Text);
			textArea.OnDataObjectPasting(pastingEventArgs);
			if (pastingEventArgs.CommandCancelled)
				return;

			//was previously: reading the clipboard text was synchronous in WPF, so the whole
			//paste ran inline here. Clipboard reads are asynchronous in this framework: the
			//command marks itself handled once the pasting event was allowed through, and the
			//actual text retrieval + document change complete in the awaited continuation below
			//(on the UI thread, via the dispatcher's async completion). The insert semantics
			//inside the continuation are unchanged from upstream.
			args.Handled = true;
			PerformPasteAsync(textArea, pastingEventArgs);
		}
	}

	static async void PerformPasteAsync(TextArea textArea, DataObjectPastingEventArgs pastingEventArgs)
	{
		string? text = await GetTextToPasteAsync(pastingEventArgs, textArea);

		if (!string.IsNullOrEmpty(text))
		{
			DataPackageView dataObject = pastingEventArgs.DataObject;
			bool fullLine = textArea.Options.CutCopyWholeLine && dataObject.Contains(LineSelectedType);
			bool rectangular = dataObject.Contains(RectangleSelection.RectangularSelectionDataType);

			if (fullLine)
			{
				DocumentLine currentLine = textArea.Document.GetLineByNumber(textArea.Caret.Line);
				if (textArea.ReadOnlySectionProvider.CanInsert(currentLine.Offset))
				{
					textArea.Document.Insert(currentLine.Offset, text);
				}
			}
			else if (rectangular && textArea.Selection.IsEmpty && !(textArea.Selection is RectangleSelection))
			{
				if (!RectangleSelection.PerformRectangularPaste(textArea, textArea.Caret.Position, text, false))
					textArea.ReplaceSelectionWithText(text);
			}
			else
			{
				textArea.ReplaceSelectionWithText(text);
			}
		}
		textArea.Caret.BringCaretToView();
	}

	internal static async Task<string?> GetTextToPasteAsync(DataObjectPastingEventArgs pastingEventArgs, TextArea textArea)
	{
		var dataObject = pastingEventArgs.DataObject;
		try
		{
			string? text;
			// Try retrieving the text as one of:
			//  - the FormatToApply
			//  - the plain text format
			// (but don't try the same format twice)
			//was previously: WPF distinguished DataFormats.UnicodeText and DataFormats.Text, so
			//the fallback chain had three steps; this framework has a single
			//StandardDataFormats.Text, so it has two.
			if (dataObject.Contains(pastingEventArgs.FormatToApply))
			{
				text = pastingEventArgs.FormatToApply == StandardDataFormats.Text
					? await dataObject.GetTextAsync()
					: await dataObject.GetDataAsync(pastingEventArgs.FormatToApply) as string;
			}
			else if (pastingEventArgs.FormatToApply != StandardDataFormats.Text && dataObject.Contains(StandardDataFormats.Text))
			{
				text = await dataObject.GetTextAsync();
			}
			else
			{
				return null; // no text data format
			}
			if (text == null)
				return null;
			// convert text back to correct newlines for this document
			string newLine = TextUtilities.GetNewLineFromDocument(textArea.Document, textArea.Caret.Line);
			text = TextUtilities.NormalizeNewLines(text, newLine);
			text = textArea.Options.ConvertTabsToSpaces ? text.Replace("\t", new string(' ', textArea.Options.IndentationSize)) : text;
			return text;
		}
		catch (OutOfMemoryException)
		{
			// may happen when trying to paste a huge string
			return null;
		}
		catch (COMException)
		{
			// may happen with incorrect data => Data on clipboard is invalid
			return null;
		}
	}
	#endregion

	#region Toggle Overstrike
	static void OnToggleOverstrike(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Options.AllowToggleOverstrikeMode)
			textArea.OverstrikeMode = !textArea.OverstrikeMode;
	}
	#endregion

	#region DeleteLine
	static void OnDeleteLine(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null)
		{
			int firstLineIndex, lastLineIndex;
			if (textArea.Selection.Length == 0)
			{
				// There is no selection, simply delete current line
				firstLineIndex = lastLineIndex = textArea.Caret.Line;
			}
			else
			{
				// There is a selection, remove all lines affected by it (use Min/Max to be independent from selection direction)
				firstLineIndex = Math.Min(textArea.Selection.StartPosition.Line, textArea.Selection.EndPosition.Line);
				lastLineIndex = Math.Max(textArea.Selection.StartPosition.Line, textArea.Selection.EndPosition.Line);
			}
			DocumentLine startLine = textArea.Document.GetLineByNumber(firstLineIndex);
			DocumentLine endLine = textArea.Document.GetLineByNumber(lastLineIndex);
			textArea.Selection = Selection.Create(textArea, startLine.Offset, endLine.Offset + endLine.TotalLength);
			textArea.RemoveSelectedText();
			args.Handled = true;
		}
	}
	#endregion

	#region Remove..Whitespace / Convert Tabs-Spaces
	static void OnRemoveLeadingWhitespace(object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedLines(
			delegate (TextArea textArea, DocumentLine line) {
				textArea.Document.Remove(TextUtilities.GetLeadingWhitespace(textArea.Document, line));
			}, target, args, DefaultSegmentType.WholeDocument);
	}

	static void OnRemoveTrailingWhitespace(object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedLines(
			delegate (TextArea textArea, DocumentLine line) {
				textArea.Document.Remove(TextUtilities.GetTrailingWhitespace(textArea.Document, line));
			}, target, args, DefaultSegmentType.WholeDocument);
	}

	static void OnConvertTabsToSpaces(object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedSegments(ConvertTabsToSpaces, target, args, DefaultSegmentType.WholeDocument);
	}

	static void OnConvertLeadingTabsToSpaces(object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedLines(
			delegate (TextArea textArea, DocumentLine line) {
				ConvertTabsToSpaces(textArea, TextUtilities.GetLeadingWhitespace(textArea.Document, line));
			}, target, args, DefaultSegmentType.WholeDocument);
	}

	static void ConvertTabsToSpaces(TextArea textArea, ISegment segment)
	{
		TextDocument document = textArea.Document;
		int endOffset = segment.EndOffset;
		string indentationString = new string(' ', textArea.Options.IndentationSize);
		for (int offset = segment.Offset; offset < endOffset; offset++)
		{
			if (document.GetCharAt(offset) == '\t')
			{
				document.Replace(offset, 1, indentationString, OffsetChangeMappingType.CharacterReplace);
				endOffset += indentationString.Length - 1;
			}
		}
	}

	static void OnConvertSpacesToTabs(object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedSegments(ConvertSpacesToTabs, target, args, DefaultSegmentType.WholeDocument);
	}

	static void OnConvertLeadingSpacesToTabs(object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedLines(
			delegate (TextArea textArea, DocumentLine line) {
				ConvertSpacesToTabs(textArea, TextUtilities.GetLeadingWhitespace(textArea.Document, line));
			}, target, args, DefaultSegmentType.WholeDocument);
	}

	static void ConvertSpacesToTabs(TextArea textArea, ISegment segment)
	{
		TextDocument document = textArea.Document;
		int endOffset = segment.EndOffset;
		int indentationSize = textArea.Options.IndentationSize;
		int spacesCount = 0;
		for (int offset = segment.Offset; offset < endOffset; offset++)
		{
			if (document.GetCharAt(offset) == ' ')
			{
				spacesCount++;
				if (spacesCount == indentationSize)
				{
					document.Replace(offset - (indentationSize - 1), indentationSize, "\t", OffsetChangeMappingType.CharacterReplace);
					spacesCount = 0;
					offset -= indentationSize - 1;
					endOffset -= indentationSize - 1;
				}
			}
			else
			{
				spacesCount = 0;
			}
		}
	}
	#endregion

	#region Convert...Case
	static void ConvertCase(Func<string, string> transformText, object target, ExecutedEditorCommandEventArgs args)
	{
		TransformSelectedSegments(
			delegate (TextArea textArea, ISegment segment) {
				string oldText = textArea.Document.GetText(segment);
				string newText = transformText(oldText);
				textArea.Document.Replace(segment.Offset, segment.Length, newText, OffsetChangeMappingType.CharacterReplace);
			}, target, args, DefaultSegmentType.WholeDocument);
	}

	static void OnConvertToUpperCase(object target, ExecutedEditorCommandEventArgs args)
	{
		ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToUpper, target, args);
	}

	static void OnConvertToLowerCase(object target, ExecutedEditorCommandEventArgs args)
	{
		ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToLower, target, args);
	}

	static void OnConvertToTitleCase(object target, ExecutedEditorCommandEventArgs args)
	{
		ConvertCase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase, target, args);
	}

	static void OnInvertCase(object target, ExecutedEditorCommandEventArgs args)
	{
		ConvertCase(InvertCase, target, args);
	}

	static string InvertCase(string text)
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		char[] buffer = text.ToCharArray();
		for (int i = 0; i < buffer.Length; ++i)
		{
			char c = buffer[i];
			buffer[i] = char.IsUpper(c) ? char.ToLower(c, culture) : char.ToUpper(c, culture);
		}
		return new string(buffer);
	}
	#endregion

	static void OnIndentSelection(object target, ExecutedEditorCommandEventArgs args)
	{
		TextArea? textArea = GetTextArea(target);
		if (textArea != null && textArea.Document != null && textArea.IndentationStrategy != null)
		{
			using (textArea.Document.RunUpdate())
			{
				int start, end;
				//was previously: the non-empty branch read SurroundingSegment unconditionally;
				//the port's property is null for the empty selection, so the pattern guards it
				//(the whole-document fallback stays for the unreachable null case).
				if (!textArea.Selection.IsEmpty && textArea.Selection.SurroundingSegment is ISegment surroundingSegment)
				{
					start = textArea.Document.GetLineByOffset(surroundingSegment.Offset).LineNumber;
					end = textArea.Document.GetLineByOffset(surroundingSegment.EndOffset).LineNumber;
				}
				else
				{
					start = 1;
					end = textArea.Document.LineCount;
				}
				textArea.IndentationStrategy.IndentLines(textArea.Document, start, end);
			}
			textArea.Caret.BringCaretToView();
			args.Handled = true;
		}
	}
}
