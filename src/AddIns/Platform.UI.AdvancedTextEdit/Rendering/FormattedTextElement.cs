#nullable enable

using System;
using System.Text;
using CodeBrix.Platform.UI.TextLayout;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/FormattedTextElement.cs in the AvalonEdit repo
//(MIT). The three WPF display shapes (string / TextLine / FormattedText) collapse to the string
//constructor: the display text is contributed to the visual line's single engine layout via
//BuildLayoutText, so the FormattedTextRun embedded-object class, the PrepareText helper and the
//BreakBefore/BreakAfter line-break conditions (which only the WPF formatter consumed) are gone.

/// <summary>
/// A visual line element that displays text which is not the document text - for example a
/// newline marker or a collapsed folding section. Serves as base class for such elements.
/// </summary>
public class FormattedTextElement : VisualLineElement
{
	/// <summary>
	/// Creates a new FormattedTextElement that displays the specified text
	/// and occupies the specified length in the document.
	/// </summary>
	public FormattedTextElement(string text, int documentLength) : base(1, documentLength)
	{
		this.Text = text ?? throw new ArgumentNullException(nameof(text));
	}

	/// <summary>
	/// Gets the text displayed by this element in place of the document text it covers.
	/// </summary>
	public string Text { get; }

	/// <inheritdoc/>
	public override TextRunDescriptor BuildLayoutText(StringBuilder layoutText, ITextRunConstructionContext context)
	{
		if (layoutText == null)
			throw new ArgumentNullException(nameof(layoutText));
		if (context == null)
			throw new ArgumentNullException(nameof(context));
		layoutText.Append(Text);
		return CreateTextRunDescriptor(Text);
	}
}
