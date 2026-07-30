#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/TextAreaDefaultInputHandlers.cs in the AvalonEdit
//repo (MIT); the file is renamed to match its single class. The WPF ApplicationCommands.Undo/Redo
//bindings became EditorCommands.Undo/Redo bindings on the port's own command system. Two WPF-only
//helpers are NOT ported: CreateFrozenKeyBinding (KeyBindings were Freezables shared between editor
//instances; the port's KeyBinding is immutable, so plain shared instances need no freezing) and
//WorkaroundWPFMemoryLeak (WPF KeyBinding retained a reference to the first UIElement it was used
//in; the port's KeyBinding holds no element reference at all).

/// <summary>
/// Contains the predefined input handlers.
/// </summary>
public class TextAreaDefaultInputHandler : TextAreaInputHandler
{
	/// <summary>
	/// Gets the caret navigation input handler.
	/// </summary>
	public TextAreaInputHandler CaretNavigation { get; private set; }

	/// <summary>
	/// Gets the editing input handler.
	/// </summary>
	public TextAreaInputHandler Editing { get; private set; }

	/// <summary>
	/// Gets the mouse selection input handler.
	/// </summary>
	public ITextAreaInputHandler MouseSelection { get; private set; }

	/// <summary>
	/// Creates a new TextAreaDefaultInputHandler instance.
	/// </summary>
	public TextAreaDefaultInputHandler(TextArea textArea) : base(textArea)
	{
		this.NestedInputHandlers.Add(CaretNavigation = CaretNavigationCommandHandler.Create(textArea));
		this.NestedInputHandlers.Add(Editing = EditingCommandHandler.Create(textArea));
		this.NestedInputHandlers.Add(MouseSelection = new SelectionMouseHandler(textArea));

		this.CommandBindings.Add(new EditorCommandBinding(EditorCommands.Undo, ExecuteUndo, CanExecuteUndo));
		this.CommandBindings.Add(new EditorCommandBinding(EditorCommands.Redo, ExecuteRedo, CanExecuteRedo));
	}

	#region Undo / Redo
	UndoStack? GetUndoStack()
	{
		TextDocument? document = this.TextArea.Document;
		if (document != null)
			return document.UndoStack;
		else
			return null;
	}

	void ExecuteUndo(object sender, ExecutedEditorCommandEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			if (undoStack.CanUndo)
			{
				undoStack.Undo();
				this.TextArea.Caret.BringCaretToView();
			}
			e.Handled = true;
		}
	}

	void CanExecuteUndo(object sender, CanExecuteEditorCommandEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			e.Handled = true;
			e.CanExecute = undoStack.CanUndo;
		}
	}

	void ExecuteRedo(object sender, ExecutedEditorCommandEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			if (undoStack.CanRedo)
			{
				undoStack.Redo();
				this.TextArea.Caret.BringCaretToView();
			}
			e.Handled = true;
		}
	}

	void CanExecuteRedo(object sender, CanExecuteEditorCommandEventArgs e)
	{
		var undoStack = GetUndoStack();
		if (undoStack != null)
		{
			e.Handled = true;
			e.CanExecute = undoStack.CanRedo;
		}
	}
	#endregion
}
