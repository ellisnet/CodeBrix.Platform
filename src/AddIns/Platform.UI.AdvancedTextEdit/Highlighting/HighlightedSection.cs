#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;

//was previously: ICSharpCode.AvalonEdit/Highlighting/HighlightedSection.cs in the AvalonEdit repo (MIT).

/// <summary>
/// A text section with syntax highlighting information.
/// </summary>
public class HighlightedSection : ISegment
{
	/// <summary>
	/// Gets/sets the document offset of the section.
	/// </summary>
	public int Offset { get; set; }

	/// <summary>
	/// Gets/sets the length of the section.
	/// </summary>
	public int Length { get; set; }

	int ISegment.EndOffset
	{
		get { return this.Offset + this.Length; }
	}

	/// <summary>
	/// Gets the highlighting color associated with the highlighted section.
	/// </summary>
	public HighlightingColor? Color { get; set; }

	/// <inheritdoc/>
	public override string ToString()
	{
		return string.Format("[HighlightedSection ({0}-{1})={2}]", Offset, Offset + Length, Color);
	}
}
