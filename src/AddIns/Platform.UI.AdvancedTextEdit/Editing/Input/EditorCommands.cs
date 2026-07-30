#nullable enable

using Windows.System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: no direct counterpart file - replaces the WPF built-in commands the editor
//consumed: System.Windows.Input.ApplicationCommands (Copy/Cut/Paste/Delete/SelectAll/Undo/Redo/
//Find) and System.Windows.Documents.EditingCommands (the caret movement/selection set, Backspace,
//DeletePreviousWord/DeleteNextWord, EnterParagraphBreak/EnterLineBreak, TabForward/TabBackward,
//ToggleInsert). Default gestures match the WPF defaults, which are also exactly the gestures the
//upstream caret-navigation and editing command handlers registered explicitly.
//Two renames forced by merging both WPF classes into one:
// - EditingCommands.Delete became DeleteNextCharacter (ApplicationCommands.Delete keeps the
//   Delete name; they are distinct commands with different can-execute rules).
// - ApplicationCommands.Delete carries no default gesture here (WPF gave it Del); the Del key
//   press is claimed by DeleteNextCharacter's explicit key binding, and keeping a Del default
//   gesture on the selection-only Delete command could otherwise swallow the key press in
//   handlers that scan default gestures before nested handlers run.

/// <summary>
/// The built-in commands understood by the text area's default input handlers. Each command is an
/// identity token; the handlers in <see cref="TextAreaDefaultInputHandler"/> provide the
/// implementations. The listed gestures are the commands' <see cref="EditorCommand.DefaultGestures"/>;
/// they take effect wherever a command binding for the command is registered.
/// </summary>
public static class EditorCommands
{
	#region Application-level commands

	/// <summary>
	/// Copies the current selection to the clipboard. Default gestures: Ctrl+C, Ctrl+Insert.
	/// </summary>
	public static readonly EditorCommand Copy = new EditorCommand("Copy",
		new KeyGesture(VirtualKey.C, VirtualKeyModifiers.Control),
		new KeyGesture(VirtualKey.Insert, VirtualKeyModifiers.Control));

	/// <summary>
	/// Cuts the current selection to the clipboard. Default gestures: Ctrl+X, Shift+Delete.
	/// </summary>
	public static readonly EditorCommand Cut = new EditorCommand("Cut",
		new KeyGesture(VirtualKey.X, VirtualKeyModifiers.Control),
		new KeyGesture(VirtualKey.Delete, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Pastes clipboard text at the caret / over the selection. Default gestures: Ctrl+V, Shift+Insert.
	/// </summary>
	public static readonly EditorCommand Paste = new EditorCommand("Paste",
		new KeyGesture(VirtualKey.V, VirtualKeyModifiers.Control),
		new KeyGesture(VirtualKey.Insert, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Deletes the current selection. Can execute only while the selection is not empty; the
	/// plain Del key press is handled by <see cref="DeleteNextCharacter"/> instead.
	/// </summary>
	public static readonly EditorCommand Delete = new EditorCommand("Delete");

	/// <summary>
	/// Selects the whole document. Default gesture: Ctrl+A.
	/// </summary>
	public static readonly EditorCommand SelectAll = new EditorCommand("SelectAll",
		new KeyGesture(VirtualKey.A, VirtualKeyModifiers.Control));

	/// <summary>
	/// Undoes the most recent document change. Default gesture: Ctrl+Z.
	/// </summary>
	public static readonly EditorCommand Undo = new EditorCommand("Undo",
		new KeyGesture(VirtualKey.Z, VirtualKeyModifiers.Control));

	/// <summary>
	/// Redoes the most recently undone document change. Default gesture: Ctrl+Y.
	/// </summary>
	public static readonly EditorCommand Redo = new EditorCommand("Redo",
		new KeyGesture(VirtualKey.Y, VirtualKeyModifiers.Control));

	/// <summary>
	/// Shows the search panel. Default gesture: Ctrl+F.
	/// </summary>
	public static readonly EditorCommand Find = new EditorCommand("Find",
		new KeyGesture(VirtualKey.F, VirtualKeyModifiers.Control));

	#endregion

	#region Caret movement by character / word

	/// <summary>
	/// Moves the caret one character to the left. Default gesture: Left.
	/// </summary>
	public static readonly EditorCommand MoveLeftByCharacter = new EditorCommand("MoveLeftByCharacter",
		new KeyGesture(VirtualKey.Left));

	/// <summary>
	/// Extends the selection one character to the left. Default gesture: Shift+Left.
	/// </summary>
	public static readonly EditorCommand SelectLeftByCharacter = new EditorCommand("SelectLeftByCharacter",
		new KeyGesture(VirtualKey.Left, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret one character to the right. Default gesture: Right.
	/// </summary>
	public static readonly EditorCommand MoveRightByCharacter = new EditorCommand("MoveRightByCharacter",
		new KeyGesture(VirtualKey.Right));

	/// <summary>
	/// Extends the selection one character to the right. Default gesture: Shift+Right.
	/// </summary>
	public static readonly EditorCommand SelectRightByCharacter = new EditorCommand("SelectRightByCharacter",
		new KeyGesture(VirtualKey.Right, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret one word to the left. Default gesture: Ctrl+Left.
	/// </summary>
	public static readonly EditorCommand MoveLeftByWord = new EditorCommand("MoveLeftByWord",
		new KeyGesture(VirtualKey.Left, VirtualKeyModifiers.Control));

	/// <summary>
	/// Extends the selection one word to the left. Default gesture: Ctrl+Shift+Left.
	/// </summary>
	public static readonly EditorCommand SelectLeftByWord = new EditorCommand("SelectLeftByWord",
		new KeyGesture(VirtualKey.Left, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret one word to the right. Default gesture: Ctrl+Right.
	/// </summary>
	public static readonly EditorCommand MoveRightByWord = new EditorCommand("MoveRightByWord",
		new KeyGesture(VirtualKey.Right, VirtualKeyModifiers.Control));

	/// <summary>
	/// Extends the selection one word to the right. Default gesture: Ctrl+Shift+Right.
	/// </summary>
	public static readonly EditorCommand SelectRightByWord = new EditorCommand("SelectRightByWord",
		new KeyGesture(VirtualKey.Right, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));

	#endregion

	#region Caret movement by line / page

	/// <summary>
	/// Moves the caret up one line. Default gesture: Up.
	/// </summary>
	public static readonly EditorCommand MoveUpByLine = new EditorCommand("MoveUpByLine",
		new KeyGesture(VirtualKey.Up));

	/// <summary>
	/// Extends the selection up one line. Default gesture: Shift+Up.
	/// </summary>
	public static readonly EditorCommand SelectUpByLine = new EditorCommand("SelectUpByLine",
		new KeyGesture(VirtualKey.Up, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret down one line. Default gesture: Down.
	/// </summary>
	public static readonly EditorCommand MoveDownByLine = new EditorCommand("MoveDownByLine",
		new KeyGesture(VirtualKey.Down));

	/// <summary>
	/// Extends the selection down one line. Default gesture: Shift+Down.
	/// </summary>
	public static readonly EditorCommand SelectDownByLine = new EditorCommand("SelectDownByLine",
		new KeyGesture(VirtualKey.Down, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret up one page. Default gesture: PageUp.
	/// </summary>
	public static readonly EditorCommand MoveUpByPage = new EditorCommand("MoveUpByPage",
		new KeyGesture(VirtualKey.PageUp));

	/// <summary>
	/// Extends the selection up one page. Default gesture: Shift+PageUp.
	/// </summary>
	public static readonly EditorCommand SelectUpByPage = new EditorCommand("SelectUpByPage",
		new KeyGesture(VirtualKey.PageUp, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret down one page. Default gesture: PageDown.
	/// </summary>
	public static readonly EditorCommand MoveDownByPage = new EditorCommand("MoveDownByPage",
		new KeyGesture(VirtualKey.PageDown));

	/// <summary>
	/// Extends the selection down one page. Default gesture: Shift+PageDown.
	/// </summary>
	public static readonly EditorCommand SelectDownByPage = new EditorCommand("SelectDownByPage",
		new KeyGesture(VirtualKey.PageDown, VirtualKeyModifiers.Shift));

	#endregion

	#region Caret movement to line / document boundaries

	/// <summary>
	/// Moves the caret to the start of the line. Default gesture: Home.
	/// </summary>
	public static readonly EditorCommand MoveToLineStart = new EditorCommand("MoveToLineStart",
		new KeyGesture(VirtualKey.Home));

	/// <summary>
	/// Extends the selection to the start of the line. Default gesture: Shift+Home.
	/// </summary>
	public static readonly EditorCommand SelectToLineStart = new EditorCommand("SelectToLineStart",
		new KeyGesture(VirtualKey.Home, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret to the end of the line. Default gesture: End.
	/// </summary>
	public static readonly EditorCommand MoveToLineEnd = new EditorCommand("MoveToLineEnd",
		new KeyGesture(VirtualKey.End));

	/// <summary>
	/// Extends the selection to the end of the line. Default gesture: Shift+End.
	/// </summary>
	public static readonly EditorCommand SelectToLineEnd = new EditorCommand("SelectToLineEnd",
		new KeyGesture(VirtualKey.End, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret to the start of the document. Default gesture: Ctrl+Home.
	/// </summary>
	public static readonly EditorCommand MoveToDocumentStart = new EditorCommand("MoveToDocumentStart",
		new KeyGesture(VirtualKey.Home, VirtualKeyModifiers.Control));

	/// <summary>
	/// Extends the selection to the start of the document. Default gesture: Ctrl+Shift+Home.
	/// </summary>
	public static readonly EditorCommand SelectToDocumentStart = new EditorCommand("SelectToDocumentStart",
		new KeyGesture(VirtualKey.Home, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));

	/// <summary>
	/// Moves the caret to the end of the document. Default gesture: Ctrl+End.
	/// </summary>
	public static readonly EditorCommand MoveToDocumentEnd = new EditorCommand("MoveToDocumentEnd",
		new KeyGesture(VirtualKey.End, VirtualKeyModifiers.Control));

	/// <summary>
	/// Extends the selection to the end of the document. Default gesture: Ctrl+Shift+End.
	/// </summary>
	public static readonly EditorCommand SelectToDocumentEnd = new EditorCommand("SelectToDocumentEnd",
		new KeyGesture(VirtualKey.End, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift));

	#endregion

	#region Editing commands

	/// <summary>
	/// Deletes the selection, or the next character when the selection is empty.
	/// Default gesture: Delete.
	/// </summary>
	public static readonly EditorCommand DeleteNextCharacter = new EditorCommand("DeleteNextCharacter",
		new KeyGesture(VirtualKey.Delete));

	/// <summary>
	/// Deletes the selection, or the next word when the selection is empty.
	/// Default gesture: Ctrl+Delete.
	/// </summary>
	public static readonly EditorCommand DeleteNextWord = new EditorCommand("DeleteNextWord",
		new KeyGesture(VirtualKey.Delete, VirtualKeyModifiers.Control));

	/// <summary>
	/// Deletes the selection, or the previous character when the selection is empty.
	/// Default gesture: Back.
	/// </summary>
	public static readonly EditorCommand Backspace = new EditorCommand("Backspace",
		new KeyGesture(VirtualKey.Back));

	/// <summary>
	/// Deletes the selection, or the previous word when the selection is empty.
	/// Default gesture: Ctrl+Back.
	/// </summary>
	public static readonly EditorCommand DeletePreviousWord = new EditorCommand("DeletePreviousWord",
		new KeyGesture(VirtualKey.Back, VirtualKeyModifiers.Control));

	/// <summary>
	/// Inserts a line break at the caret. Default gesture: Enter.
	/// </summary>
	public static readonly EditorCommand EnterParagraphBreak = new EditorCommand("EnterParagraphBreak",
		new KeyGesture(VirtualKey.Enter));

	/// <summary>
	/// Inserts a line break at the caret. Default gesture: Shift+Enter.
	/// </summary>
	public static readonly EditorCommand EnterLineBreak = new EditorCommand("EnterLineBreak",
		new KeyGesture(VirtualKey.Enter, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Indents the selected lines, or inserts an indentation string at the caret.
	/// Default gesture: Tab.
	/// </summary>
	public static readonly EditorCommand TabForward = new EditorCommand("TabForward",
		new KeyGesture(VirtualKey.Tab));

	/// <summary>
	/// Removes one level of indentation from the selected lines. Default gesture: Shift+Tab.
	/// </summary>
	public static readonly EditorCommand TabBackward = new EditorCommand("TabBackward",
		new KeyGesture(VirtualKey.Tab, VirtualKeyModifiers.Shift));

	/// <summary>
	/// Toggles between insert and overtype mode. Default gesture: Insert. Not bound by the
	/// default input handlers (the editor's own
	/// <see cref="AdvancedTextEditCommands.ToggleOverstrike"/> claims the Insert key instead);
	/// provided for consumers that wire their own binding.
	/// </summary>
	public static readonly EditorCommand ToggleInsert = new EditorCommand("ToggleInsert",
		new KeyGesture(VirtualKey.Insert));

	#endregion
}
