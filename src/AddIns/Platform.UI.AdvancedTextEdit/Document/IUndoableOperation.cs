#nullable enable

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Document;

//was previously: ICSharpCode.AvalonEdit/Document/IUndoableOperation.cs in the AvalonEdit repo (MIT).

/// <summary>
/// This interface describes the basic Undo/Redo operation;
/// all undo operations must implement this interface.
/// </summary>
public interface IUndoableOperation
{
	/// <summary>
	/// Undo the last operation
	/// </summary>
	void Undo();

	/// <summary>
	/// Redo the last operation
	/// </summary>
	void Redo();
}

interface IUndoableOperationWithContext : IUndoableOperation
{
	void Undo(UndoStack stack);
	void Redo(UndoStack stack);
}
