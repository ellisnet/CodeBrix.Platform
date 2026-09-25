#nullable enable

using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// One laid-out block of text, produced by <see cref="ITextPlatform.CreateLayout"/>: it answers the hit-testing,
/// caret and selection geometry, word and line queries that the platform-neutral text controls need. All
/// coordinates are in the block's own space, without its padding.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// <see cref="Microsoft.UI.Xaml.Controls.TextBlock"/> keeps the layout of its last measure or arrange pass and queries
/// it for pointer selection, hyperlinks and the TextBox caret. A layout is immutable. The Skia implementations are the
/// text engine's <c>UnicodeText</c> and <c>ParsedText</c> (through <c>IParsedText</c>, which adds the drawing).
/// </para>
/// </remarks>
internal interface ITextLayout
{
	/// <summary>
	/// Returns the caret rectangle of the character at <paramref name="adjustedIndex"/>.
	/// </summary>
	/// <param name="adjustedIndex">A text index (0 to the text length).</param>
	/// <returns>The caret rectangle.</returns>
	Rect GetRectForIndex(int adjustedIndex);

	/// <summary>
	/// Returns the text index at <paramref name="p"/>.
	/// </summary>
	/// <param name="p">The point, in the block's space.</param>
	/// <param name="ignoreEndingNewLine">Whether a trailing line break is skipped when the point is past a line's end.</param>
	/// <param name="extendedSelection">Whether a point outside every character gives the nearest index instead of -1.</param>
	/// <returns>The text index, or -1.</returns>
	int GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection);

	/// <summary>
	/// Returns the hyperlink under <paramref name="point"/>.
	/// </summary>
	/// <param name="point">The point, in the block's space.</param>
	/// <returns>The hyperlink, or <see langword="null"/>.</returns>
	Hyperlink? GetHyperlinkAt(Point point);

	/// <summary>
	/// Returns the word that contains <paramref name="index"/>.
	/// </summary>
	/// <param name="index">A text index.</param>
	/// <param name="right">When on a word boundary, decides whether to return the left or the right word.</param>
	/// <returns>The start and length of the word.</returns>
	(int start, int length) GetWordAt(int index, bool right);

	/// <summary>
	/// Returns the rendered line (after wrapping) that contains <paramref name="index"/>.
	/// </summary>
	/// <param name="index">A text index.</param>
	/// <returns>The line's start and length, whether it is the first or the last line, and its index.</returns>
	(int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index);

	/// <summary>
	/// Gets a value indicating whether the base direction of the text is right to left.
	/// </summary>
	bool IsBaseDirectionRightToLeft { get; }
}
