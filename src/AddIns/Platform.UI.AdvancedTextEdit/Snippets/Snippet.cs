#nullable enable

using System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Snippets;

//was previously: ICSharpCode.AvalonEdit/Snippets/Snippet.cs in the AvalonEdit repo (MIT).
//Transliterated unchanged.

/// <summary>
/// A code snippet that can be inserted into the text editor.
/// </summary>
[Serializable]
public class Snippet : SnippetContainerElement
{
	/// <summary>
	/// Inserts the snippet into the text area.
	/// </summary>
	public void Insert(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));

		ISegment? selection = textArea.Selection.SurroundingSegment;
		int insertionPosition = textArea.Caret.Offset;

		if (selection != null) // if something is selected
			// use selection start instead of caret position,
			// because caret could be at end of selection or anywhere inside.
			// Removal of the selected text causes the caret position to be invalid.
			insertionPosition = selection.Offset + TextUtilities.GetWhitespaceAfter(textArea.Document, selection.Offset).Length;

		InsertionContext context = new InsertionContext(textArea, insertionPosition);

		using (context.Document.RunUpdate())
		{
			if (selection != null)
				textArea.Document.Remove(insertionPosition, selection.EndOffset - insertionPosition);
			Insert(context);
			context.RaiseInsertionCompleted(EventArgs.Empty);
		}
	}
}
