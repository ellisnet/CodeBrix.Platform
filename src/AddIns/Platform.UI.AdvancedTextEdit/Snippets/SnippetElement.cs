#nullable enable

using System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Snippets;

//was previously: ICSharpCode.AvalonEdit/Snippets/SnippetElement.cs in the AvalonEdit repo (MIT).
//The ToTextRun method (returning a WPF System.Windows.Documents.Inline for snippet previews) has
//no counterpart in this framework and is not ported; consumers can build preview text from the
//elements themselves.

/// <summary>
/// An element inside a snippet.
/// </summary>
[Serializable]
public abstract class SnippetElement
{
	/// <summary>
	/// Performs insertion of the snippet.
	/// </summary>
	public abstract void Insert(InsertionContext context);
}
