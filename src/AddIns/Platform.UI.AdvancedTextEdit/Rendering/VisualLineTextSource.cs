#nullable enable

using System;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/VisualLineTextSource.cs in the AvalonEdit repo
//(MIT), where it was also the WPF TextSource that manufactured TextRuns on demand for the line
//formatter. The engine lays out whole visual lines at once (each element contributes its span via
//BuildLayoutText), so the TextSource half - GetTextRun, GetPrecedingText, the end-of-line and
//end-of-paragraph runs - is gone; what remains is the construction context plus its text cache.

/// <summary>
/// The <see cref="ITextRunConstructionContext"/> implementation used while constructing and
/// formatting a <see cref="Rendering.VisualLine"/>: it carries the participants and caches the
/// document text of the line being built.
/// </summary>
internal sealed class VisualLineTextSource : ITextRunConstructionContext
{
	public VisualLineTextSource(VisualLine visualLine)
	{
		if (visualLine == null)
			throw new ArgumentNullException(nameof(visualLine));
		this.VisualLine = visualLine;
	}

	/// <summary>
	/// Gets the visual line that is currently being constructed.
	/// </summary>
	public VisualLine VisualLine { get; }

	/// <summary>
	/// Gets the text view for which the construction runs.
	/// </summary>
	public required TextView TextView { get; init; }

	/// <summary>
	/// Gets the document being rendered.
	/// </summary>
	public required TextDocument Document { get; init; }

	/// <summary>
	/// Gets the global text run properties.
	/// </summary>
	public required GlobalTextRunProperties GlobalTextRunProperties { get; init; }

	string? cachedString;
	int cachedStringOffset;

	/// <inheritdoc/>
	public StringSegment GetText(int offset, int length)
	{
		if (cachedString != null)
		{
			if (offset >= cachedStringOffset && offset + length <= cachedStringOffset + cachedString.Length)
			{
				return new StringSegment(cachedString, offset - cachedStringOffset, length);
			}
		}
		cachedStringOffset = offset;
		return new StringSegment(cachedString = this.Document.GetText(offset, length));
	}
}
