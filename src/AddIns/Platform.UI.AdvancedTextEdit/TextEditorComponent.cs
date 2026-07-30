#nullable enable

using System;
using System.ComponentModel;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

namespace CodeBrix.Platform.UI.AdvancedTextEdit;

//was previously: ICSharpCode.AvalonEdit/TextEditorComponent.cs in the AvalonEdit repo (MIT).
//The Document property is annotated nullable: a component may not have a document attached yet.
//The summary named the three concrete implementing types with cref links; those types port in
//later waves, so the text is reworded and the links will be restored when the types exist.

/// <summary>
/// Represents a text editor component: the editor control, its text area, or its text view.
/// </summary>
public interface ITextEditorComponent : IServiceProvider
{
	/// <summary>
	/// Gets the document being edited. May be null when no document is attached.
	/// </summary>
	TextDocument? Document { get; }

	/// <summary>
	/// Occurs when the Document property changes (when the text editor is connected to another
	/// document - not when the document content changes).
	/// </summary>
	event EventHandler? DocumentChanged;

	/// <summary>
	/// Gets the options of the text editor.
	/// </summary>
	AdvancedTextEditOptions Options { get; }

	/// <summary>
	/// Occurs when the Options property changes, or when an option inside the current option list
	/// changes.
	/// </summary>
	event PropertyChangedEventHandler? OptionChanged;
}
