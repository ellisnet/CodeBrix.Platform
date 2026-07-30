#nullable enable

using System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Snippets;

//was previously: ICSharpCode.AvalonEdit/Snippets/SnippetTextElement.cs in the AvalonEdit repo
//(MIT). The ToTextRun override is not ported (see SnippetElement.cs).

/// <summary>
/// Represents a text element in a snippet.
/// </summary>
[Serializable]
public class SnippetTextElement : SnippetElement
{
	string? text;

	/// <summary>
	/// The text to be inserted.
	/// </summary>
	public string? Text {
		get { return text; }
		set { text = value; }
	}

	/// <inheritdoc/>
	public override void Insert(InsertionContext context)
	{
		if (text != null)
			context.InsertText(text);
	}
}
