#nullable enable

using System.Collections.Generic;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout.Contracts;

/// <summary>
/// One completed layout of the text engine, as <see cref="ITextLayoutPlatform.CreateLayout"/> returns it: the
/// measurements, caret and hit-testing geometry, outlines and drawing that <see cref="TextLayoutResult"/> exposes.
/// Every index is a text index into <see cref="LayoutText"/>. The geometry is SkiaSharp's (WPE1 C4).
/// </summary>
/// <remarks>
/// Implementers: this assembly (<c>Engine.EngineLayout</c>, which wraps a layout of its copy of the shared text engine).
/// <para>
/// Returned by <see cref="ITextLayoutPlatform.CreateLayout"/> and held by one <see cref="TextLayoutResult"/>; never
/// registered.
/// </para>
/// </remarks>
internal interface IEngineLayout
{
	/// <summary>The text the layout covers.</summary>
	string LayoutText { get; }

	/// <summary>The number of lines.</summary>
	int LineCount { get; }

	/// <summary>Whether the resolved base direction is right-to-left.</summary>
	bool IsRightToLeft { get; }

	/// <summary>The height of a line (for line 0 of an empty layout, the default font's line height).</summary>
	/// <param name="lineIndex">The zero-based line index.</param>
	/// <returns>The height.</returns>
	float GetLineHeight(int lineIndex);

	/// <summary>The top of a line.</summary>
	/// <param name="lineIndex">The zero-based line index.</param>
	/// <returns>The top, in layout coordinates.</returns>
	float GetLineTop(int lineIndex);

	/// <summary>The baseline of a line, from its top.</summary>
	/// <param name="lineIndex">The zero-based line index.</param>
	/// <returns>The baseline offset.</returns>
	float GetLineBaselineOffset(int lineIndex);

	/// <summary>The first text index of a line.</summary>
	/// <param name="lineIndex">The zero-based line index.</param>
	/// <returns>The text index.</returns>
	int GetLineStartInText(int lineIndex);

	/// <summary>The text index just past a line.</summary>
	/// <param name="lineIndex">The zero-based line index.</param>
	/// <returns>The text index.</returns>
	int GetLineEndInText(int lineIndex);

	/// <summary>The caret rectangle for a text index.</summary>
	/// <param name="index">The text index.</param>
	/// <param name="caretThickness">The width of the rectangle.</param>
	/// <returns>The rectangle, in layout coordinates.</returns>
	SKRect GetCaretRectForIndex(int index, float caretThickness);

	/// <summary>The rectangle covering the cluster at a text index.</summary>
	/// <param name="index">The text index.</param>
	/// <returns>The rectangle, in layout coordinates.</returns>
	SKRect GetRectForIndex(int index);

	/// <summary>The text index at a point.</summary>
	/// <param name="point">The point, in layout coordinates.</param>
	/// <param name="ignoreEndingNewLine">Whether a trailing new line is skipped.</param>
	/// <param name="extendedSelection">True to clamp into the text instead of returning -1 outside it.</param>
	/// <returns>The text index, or -1.</returns>
	int GetIndexAt(SKPoint point, bool ignoreEndingNewLine, bool extendedSelection);

	/// <summary>The line a text index falls on.</summary>
	/// <param name="index">The text index.</param>
	/// <returns>The line's start, length, whether it is the first and the last line, and its index.</returns>
	(int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index);

	/// <summary>The rectangles covering a range of text, one per contiguous visual segment.</summary>
	/// <param name="start">The first text index.</param>
	/// <param name="length">The number of characters.</param>
	/// <returns>A new list of the rectangles, in layout coordinates.</returns>
	IReadOnlyList<SKRect> GetSelectionRects(int start, int length);

	/// <summary>One path combining every positioned glyph outline; the caller owns it.</summary>
	/// <returns>The path.</returns>
	SKPath GetOutlinePath();

	/// <summary>Every positioned glyph in visual order, each with its own outline; the caller owns them.</summary>
	/// <returns>The glyphs.</returns>
	IReadOnlyList<GlyphOutline> GetGlyphOutlines();

	/// <summary>Paints the layout.</summary>
	/// <param name="canvas">The destination canvas.</param>
	/// <param name="origin">Where the layout's top-left corner lands.</param>
	/// <param name="paint">The paint to draw with.</param>
	void Draw(SKCanvas canvas, SKPoint origin, SKPaint paint);
}
