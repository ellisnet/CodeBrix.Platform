#nullable enable

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/ITextRunConstructionContext.cs in the AvalonEdit
//repo (MIT). GlobalTextRunProperties is the port's own concrete class rather than the WPF
//TextRunProperties base type, which does not exist in this framework.

/// <summary>
/// The context a visual line's elements are constructed in: the document being rendered,
/// the view rendering it, the line under construction, and efficient access to the text.
/// </summary>
public interface ITextRunConstructionContext
{
	/// <summary>
	/// Gets the document being rendered.
	/// </summary>
	TextDocument Document { get; }

	/// <summary>
	/// Gets the text view for which the construction runs.
	/// </summary>
	TextView TextView { get; }

	/// <summary>
	/// Gets the visual line that is currently being constructed.
	/// </summary>
	VisualLine VisualLine { get; }

	/// <summary>
	/// Gets the global text run properties.
	/// </summary>
	GlobalTextRunProperties GlobalTextRunProperties { get; }

	/// <summary>
	/// Gets a piece of the document's text, using a cached buffer where possible.
	/// </summary>
	/// <param name="offset">The offset of the first character.</param>
	/// <param name="length">The number of characters.</param>
	/// <returns>The text, as a segment of a possibly larger buffer.</returns>
	/// <remarks>
	/// This is functionally equivalent to <c>Document.GetText(offset, length)</c>, but avoids
	/// allocating a new string for every requested segment while a line is being constructed.
	/// </remarks>
	StringSegment GetText(int offset, int length);
}
