#nullable enable

using System;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/RichTextColorizer.cs in the AvalonEdit repo (MIT).

/// <summary>
/// A colorizer that applies the highlighting from a <see cref="RichTextModel"/> to the editor.
/// </summary>
public class RichTextColorizer : DocumentColorizingTransformer
{
	readonly RichTextModel richTextModel;

	/// <summary>
	/// Creates a new RichTextColorizer instance.
	/// </summary>
	public RichTextColorizer(RichTextModel richTextModel)
	{
		if (richTextModel == null)
			throw new ArgumentNullException(nameof(richTextModel));
		this.richTextModel = richTextModel;
	}

	/// <inheritdoc/>
	protected override void ColorizeLine(DocumentLine line)
	{
		var sections = richTextModel.GetHighlightedSections(line.Offset, line.Length);
		foreach (HighlightedSection section in sections)
		{
			HighlightingColor? color = section.Color;
			if (color == null || HighlightingColorizer.IsEmptyColor(color))
				continue;
			ChangeLinePart(section.Offset, section.Offset + section.Length,
						   visualLineElement => HighlightingColorizer.ApplyColorToElement(visualLineElement, color, CurrentContext));
		}
	}
}
