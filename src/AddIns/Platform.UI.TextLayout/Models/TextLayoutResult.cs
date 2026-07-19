#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Documents;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// A completed text layout: its measurements, its caret and hit-testing geometry, its outlines, and
/// a way to paint it.
/// </summary>
/// <remarks>
/// <para>
/// Every index parameter and return value on this type is a TEXT index into <see cref="Text"/>, not
/// a glyph index. That distinction matters wherever shaping is not one-to-one - ligatures, combining
/// marks, and anything that forms a cluster - and getting it right is most of the reason to run text
/// through a shaper at all.
/// </para>
/// <para>
/// Disposal is not currently required: the layout holds no unmanaged resources of its own, and the
/// fonts it references belong to the engine's shared cache. <see cref="IDisposable"/> is implemented
/// so that callers can adopt <c>using</c> now and stay correct if that changes. Paths and outlines
/// handed out by <see cref="GetOutlinePath"/> and <see cref="GetGlyphOutlines"/> ARE owned by the
/// caller and must be disposed.
/// </para>
/// </remarks>
public sealed class TextLayoutResult : IDisposable
{
	private readonly UnicodeText _layout;
	private readonly SKSize _size;

	internal TextLayoutResult(UnicodeText layout, Size desiredSize)
	{
		_layout = layout;
		_size = new SKSize((float)desiredSize.Width, (float)desiredSize.Height);
	}

	/// <summary>The text this layout covers - every run's text, concatenated in order.</summary>
	public string Text => _layout.LayoutText;

	/// <summary>The measured size of the laid-out text.</summary>
	public SKSize Size => _size;

	/// <summary>The number of lines.</summary>
	public int LineCount => _layout.LineCount;

	/// <summary>The height of the first line, or the default font's line height when there is no text.</summary>
	public float LineHeight => _layout.GetLineHeight(0);

	/// <summary>Whether the resolved base direction is right-to-left.</summary>
	public bool IsBaseDirectionRightToLeft => _layout.IsRightToLeft;

	/// <summary>
	/// The caret rectangle for a text index.
	/// </summary>
	/// <param name="textIndex">A text index, from 0 to the length of <see cref="Text"/> inclusive.</param>
	/// <param name="caretThickness">The width to give the returned rectangle.</param>
	/// <returns>The caret rectangle, in layout coordinates.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="textIndex"/> is outside the text.</exception>
	public SKRect GetCaretRect(int textIndex, float caretThickness = 1f)
	{
		ValidateIndexInclusive(textIndex);
		return ToSKRect(_layout.GetCaretRectForIndex(textIndex, caretThickness));
	}

	/// <summary>
	/// The rectangle covering the cluster at a text index.
	/// </summary>
	/// <param name="textIndex">A text index, from 0 to the length of <see cref="Text"/> inclusive.</param>
	/// <returns>The rectangle, in layout coordinates.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="textIndex"/> is outside the text.</exception>
	public SKRect GetRectForIndex(int textIndex)
	{
		ValidateIndexInclusive(textIndex);
		return ToSKRect(_layout.GetRectForIndex(textIndex));
	}

	/// <summary>
	/// The text index at a point.
	/// </summary>
	/// <param name="point">A point in layout coordinates.</param>
	/// <returns>The text index, or -1 when the point falls outside the text.</returns>
	public int GetIndexAt(SKPoint point) =>
		_layout.GetIndexAt(new Point(point.X, point.Y), ignoreEndingNewLine: false, extendedSelection: false);

	/// <summary>
	/// The text index nearest to a point, clamped into the text rather than returning -1.
	/// </summary>
	/// <param name="point">A point in layout coordinates.</param>
	/// <returns>The nearest text index.</returns>
	/// <remarks>
	/// This is what a drag-selection wants: a point above, below, or beside the text still resolves
	/// to the closest caret position instead of failing.
	/// </remarks>
	public int GetNearestIndexAt(SKPoint point) =>
		_layout.GetIndexAt(new Point(point.X, point.Y), ignoreEndingNewLine: false, extendedSelection: true);

	/// <summary>
	/// Which line a text index falls on.
	/// </summary>
	/// <param name="textIndex">A text index, from 0 to the length of <see cref="Text"/> inclusive.</param>
	/// <returns>The line's position within the text.</returns>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="textIndex"/> is outside the text.</exception>
	public TextLineInfo GetLineAt(int textIndex)
	{
		ValidateIndexInclusive(textIndex);
		var (start, length, firstLine, lastLine, lineIndex) = _layout.GetLineAt(textIndex);
		return new TextLineInfo(start, length, lineIndex, firstLine, lastLine);
	}

	/// <summary>
	/// The rectangles covering a range of text - what a consumer paints behind selected text.
	/// </summary>
	/// <param name="start">The first text index in the range.</param>
	/// <param name="length">The number of characters in the range.</param>
	/// <returns>
	/// One rectangle per contiguous visual segment, in layout coordinates; empty when the range is
	/// empty.
	/// </returns>
	/// <remarks>
	/// A single logical range is often several rectangles: it can span lines, and within a line a
	/// bidi boundary splits it, so a right-to-left stretch inside left-to-right text is visually
	/// discontiguous. That is why this never returns one bounding box.
	/// </remarks>
	public IReadOnlyList<SKRect> GetSelectionRects(int start, int length)
	{
		var rects = _layout.GetSelectionRects(start, length);
		var result = new List<SKRect>(rects.Count);
		foreach (var rect in rects)
		{
			result.Add(ToSKRect(rect));
		}

		return result;
	}

	/// <summary>
	/// One path combining every glyph outline in the layout, already positioned.
	/// </summary>
	/// <returns>The combined path. The caller owns it and must dispose it.</returns>
	/// <remarks>
	/// Fill it to draw the text, stroke it to outline the text, or do both for outlined text - which
	/// is the case <see cref="Draw"/> cannot serve, because a text blob can only be filled.
	/// </remarks>
	public SKPath GetOutlinePath() => _layout.GetOutlinePath();

	/// <summary>
	/// Every positioned glyph in the layout, in visual order, each with its own outline.
	/// </summary>
	/// <returns>The glyphs. The caller owns each <see cref="GlyphOutline"/> and must dispose them.</returns>
	/// <remarks>
	/// Use this when glyphs need to be treated individually - per-glyph effects, animation, or
	/// hit-testing an outline. For plain outlined text, <see cref="GetOutlinePath"/> is cheaper.
	/// </remarks>
	public IReadOnlyList<GlyphOutline> GetGlyphOutlines()
	{
		var engineOutlines = _layout.GetGlyphOutlines();
		var result = new List<GlyphOutline>(engineOutlines.Count);
		foreach (var outline in engineOutlines)
		{
			result.Add(new GlyphOutline(outline.GlyphId, outline.Path, outline.Origin, outline.Advance, outline.Font));
		}

		return result;
	}

	/// <summary>
	/// Paints the layout onto a canvas.
	/// </summary>
	/// <param name="canvas">The destination canvas - a surface, a bitmap, a document layer, anything.</param>
	/// <param name="origin">Where the layout's top-left corner lands.</param>
	/// <param name="paint">The paint to draw with; its colour is used for the glyphs.</param>
	/// <exception cref="ArgumentNullException"><paramref name="canvas"/> or <paramref name="paint"/> is null.</exception>
	/// <remarks>
	/// This draws filled glyphs via text blobs, which is the fast path. For stroked or outlined text
	/// use <see cref="GetOutlinePath"/> and draw the path yourself.
	/// </remarks>
	public void Draw(SKCanvas canvas, SKPoint origin, SKPaint paint)
	{
		if (canvas is null)
		{
			throw new ArgumentNullException(nameof(canvas));
		}

		if (paint is null)
		{
			throw new ArgumentNullException(nameof(paint));
		}

		_layout.DrawToCanvas(canvas, origin, paint);
	}

	/// <summary>
	/// Paints the layout at the canvas origin.
	/// </summary>
	/// <param name="canvas">The destination canvas.</param>
	/// <param name="paint">The paint to draw with.</param>
	public void Draw(SKCanvas canvas, SKPaint paint) => Draw(canvas, SKPoint.Empty, paint);

	/// <summary>
	/// Releases the layout. See the type remarks: this is currently a no-op and exists so callers can
	/// safely adopt <c>using</c>.
	/// </summary>
	public void Dispose()
	{
		// The layout owns no unmanaged resources; fonts belong to the engine's shared cache and
		// outlives any one layout. Declared so consumers can wrap this in `using` without having to
		// revisit it if that ever changes.
	}

	private void ValidateIndexInclusive(int textIndex)
	{
		if (textIndex < 0 || textIndex > Text.Length)
		{
			throw new ArgumentOutOfRangeException(
				nameof(textIndex),
				textIndex,
				$"Text index must be between 0 and {Text.Length} inclusive.");
		}
	}

	private static SKRect ToSKRect(Rect rect) =>
		new((float)rect.Left, (float)rect.Top, (float)rect.Right, (float)rect.Bottom);
}
