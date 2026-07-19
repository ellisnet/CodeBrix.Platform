#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents;

/// <summary>
/// The host-free surface of the layout engine, consumed by the CodeBrix.Platform.UI.TextLayout
/// add-in.
/// </summary>
/// <remarks>
/// Everything here reads the layout the constructor already produced; none of it is used by
/// TextBlock or TextBox, so the rendering hot path is unaffected. The members live in their own
/// partial so that the engine proper stays as it was.
/// </remarks>
internal readonly partial struct UnicodeText
{
	/// <summary>
	/// Loads the native ICU library if no application head has already done so.
	/// </summary>
	/// <remarks>
	/// Bidi resolution, line breaking and word breaking all call into ICU, which an application head
	/// initialises from a generated module initializer. A host-free caller has no head, so it must
	/// ask for initialisation itself before laying any non-empty text out. This is idempotent and a
	/// no-op inside an application.
	/// </remarks>
	internal static void EnsureEngineInitialized() => ICU.EnsureInitialized();

	/// <summary>
	/// Resolves the base direction of <paramref name="text"/> from its content, the same way the
	/// TextBox construction path does when no explicit alignment is supplied.
	/// </summary>
	/// <remarks>
	/// This lets a caller offer an "auto" direction without reimplementing the UAX #9 paragraph-level
	/// rule. Empty text is left-to-right.
	/// </remarks>
	internal static bool DetectIsRightToLeft(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}

		using var bidiHandle = ICU.CreateBiDiAndSetPara(text, 0, text.Length, UBIDI_DEFAULT_LTR, out var bidi);
		ICU.GetMethod<ICU.ubidi_getLogicalRun>()(bidi, 0, out _, out var level);
		CI.Assert(level is UBIDI_LTR or UBIDI_RTL);
		return level is UBIDI_RTL;
	}

	/// <summary>The number of laid-out lines.</summary>
	internal int LineCount => _lines.Count;

	/// <summary>Whether the layout's resolved base direction is right-to-left.</summary>
	internal bool IsRightToLeft => _rtl;

	/// <summary>The text this layout was built over, after any <c>maxLines</c> clamping.</summary>
	internal string LayoutText => _text;

	/// <summary>The height of the given line, or the default font's line height when there are no lines.</summary>
	internal float GetLineHeight(int lineIndex) =>
		lineIndex >= 0 && lineIndex < _lines.Count ? _lines[lineIndex].lineHeight : _defaultFontDetails.LineHeight;

	/// <summary>The vertical offset of the given line's top edge.</summary>
	internal float GetLineTop(int lineIndex) =>
		lineIndex >= 0 && lineIndex < _lines.Count ? _lines[lineIndex].y : 0f;

	/// <summary>The distance from the given line's top edge down to its baseline.</summary>
	internal float GetLineBaselineOffset(int lineIndex) =>
		lineIndex >= 0 && lineIndex < _lines.Count ? _lines[lineIndex].baselineOffset : -_defaultFontDetails.SKFontMetrics.Ascent;

	/// <summary>
	/// The rectangles covering the text in the logical range <paramref name="start"/> ..
	/// <paramref name="start"/> + <paramref name="length"/>.
	/// </summary>
	/// <remarks>
	/// A logical range can be visually discontiguous - it may span several lines, and within a line
	/// a bidi run boundary splits it - so this returns one rectangle per contiguous visual segment
	/// rather than a single bounding box. The per-run geometry mirrors what the compositor Draw
	/// overload paints for a selection, so highlight and text agree exactly.
	/// </remarks>
	internal IReadOnlyList<Rect> GetSelectionRects(int start, int length)
	{
		var rects = new List<Rect>();
		if (length <= 0 || _text.Length == 0 || _textIndexToGlyph.Length == 0)
		{
			return rects;
		}

		start = Math.Max(0, start);
		var end = Math.Min(start + length, _text.Length);
		if (start >= end)
		{
			return rects;
		}

		var startCluster = _textIndexToGlyph[start];
		var endCluster = _textIndexToGlyph[Math.Min(_textIndexToGlyph.Length - 1, end)];

		foreach (var line in _lines)
		{
			foreach (var run in line.runs)
			{
				var runStartInText = run.startInInline + run.inline.StartIndex;
				var runEndInText = run.endInInline + run.inline.StartIndex;

				// Same containment test the selection painting path uses.
				if (!(startCluster.sourceTextStart < runEndInText && (end == _text.Length || runStartInText < endCluster.sourceTextStart)))
				{
					continue;
				}

				int selectionLeft;
				int selectionRight; // the selection ends to the left of glyphs[selectionRight]
				if (run.rtl)
				{
					selectionLeft = endCluster.layoutedRun == run && end != _text.Length ? endCluster.glyphInRunIndexEnd : 0;
					selectionRight = startCluster.layoutedRun == run ? startCluster.glyphInRunIndexStart + 1 : run.glyphs.Length;
				}
				else
				{
					selectionLeft = startCluster.layoutedRun == run ? startCluster.glyphInRunIndexStart : 0;
					selectionRight = endCluster.layoutedRun == run && end != _text.Length ? endCluster.glyphInRunIndexStart : run.glyphs.Length;
				}

				if (selectionRight <= selectionLeft)
				{
					continue;
				}

				var runX = line.xAlignmentOffset + run.xPosInLine;
				var leftX = runX + run.glyphs[selectionLeft].xPosInRun;
				var lastGlyph = run.glyphs[selectionRight - 1];
				var rightX = runX + lastGlyph.xPosInRun + GlyphWidth(lastGlyph.position, run.fontDetails);

				rects.Add(new Rect(leftX, line.y, rightX - leftX, line.lineHeight));
			}
		}

		return rects;
	}

	/// <summary>
	/// Every positioned glyph in the layout, in visual order, each with its outline.
	/// </summary>
	/// <remarks>
	/// The caller owns the returned <see cref="SKPath"/> instances and must dispose them. Use
	/// <see cref="GetOutlinePath"/> instead when a single combined path is wanted - it manages the
	/// per-glyph paths itself.
	/// </remarks>
	internal IReadOnlyList<TextGlyphOutline> GetGlyphOutlines()
	{
		var outlines = new List<TextGlyphOutline>();
		foreach (var (glyphId, origin, advance, fontDetails) in EnumeratePositionedGlyphs())
		{
			outlines.Add(new TextGlyphOutline(glyphId, fontDetails.SKFont.GetGlyphPath(glyphId), origin, advance, fontDetails.SKFont));
		}

		return outlines;
	}

	/// <summary>
	/// A single path combining every glyph outline in the layout, positioned in layout coordinates.
	/// </summary>
	/// <remarks>
	/// This is what a consumer fills and/or strokes to render outlined text. Glyphs with no outline
	/// contribute nothing. The caller owns the returned path.
	/// </remarks>
	internal SKPath GetOutlinePath()
	{
		using var builder = new SKPathBuilder();
		foreach (var (glyphId, origin, _, fontDetails) in EnumeratePositionedGlyphs())
		{
			using var glyphPath = fontDetails.SKFont.GetGlyphPath(glyphId);
			if (glyphPath is null || glyphPath.PointCount == 0)
			{
				continue;
			}

			var translation = SKMatrix.CreateTranslation(origin.X, origin.Y);
			using var positioned = new SKPath();
			// The 'in' overload is required: the by-value Transform overloads are obsolete in 4.150.1.
			glyphPath.Transform(in translation, positioned);
			builder.AddPath(positioned, SKPathAddMode.Append);
		}

		return builder.Snapshot();
	}

	/// <summary>
	/// Paints the layout into an arbitrary <see cref="SKCanvas"/>.
	/// </summary>
	/// <param name="canvas">The destination canvas.</param>
	/// <param name="origin">Where the layout's top-left corner lands on the canvas.</param>
	/// <param name="paint">The paint to draw the glyphs with. Its colour and style are honoured.</param>
	/// <remarks>
	/// The compositor overload needs a <see cref="Visual.PaintingSession"/>, which only a
	/// <see cref="Visual"/> can create and which carries an opacity and a root transform. This
	/// overload exists so a consumer can paint into any canvas - an offscreen surface, a document
	/// layer, a bitmap - with no visual tree involved.
	/// </remarks>
	internal void DrawToCanvas(SKCanvas canvas, SKPoint origin, SKPaint paint)
	{
		foreach (var line in _lines)
		{
			var currentLineX = line.xAlignmentOffset;
			foreach (var run in line.runs)
			{
				using var textBlobBuilder = new SKTextBlobBuilder();
				var glyphs = new ushort[run.glyphs.Length];
				var positions = new SKPoint[run.glyphs.Length];
				var (textScaleX, textScaleY) = run.fontDetails.TextScale;
				for (var i = 0; i < run.glyphs.Length; i++)
				{
					var glyph = run.glyphs[i];
					glyphs[i] = (ushort)glyph.info.Codepoint;
					positions[i] = new SKPoint(
						glyph.xPosInRun + glyph.position.GlyphPosition.XOffset * textScaleX,
						line.y + glyph.position.GlyphPosition.YOffset * textScaleY);
				}

				textBlobBuilder.AddPositionedRun(glyphs, run.fontDetails.SKFont, positions);
				using var blob = textBlobBuilder.Build();
				if (blob is not null)
				{
					canvas.DrawText(blob, origin.X + currentLineX, origin.Y + line.baselineOffset, paint);
				}

				currentLineX += run.width;
			}
		}
	}

	/// <summary>
	/// Walks every glyph in the layout in visual order, resolving each one's absolute position.
	/// </summary>
	/// <remarks>
	/// The x/y arithmetic here is the same as the painting path's: a glyph sits at the line's
	/// alignment offset, plus its run's offset within the line, plus its own offset within the run,
	/// plus the shaper's per-glyph offset; vertically it sits on the line's baseline.
	/// </remarks>
	private IEnumerable<(ushort glyphId, SKPoint origin, float advance, FontDetails fontDetails)> EnumeratePositionedGlyphs()
	{
		foreach (var line in _lines)
		{
			foreach (var run in line.runs)
			{
				var runX = line.xAlignmentOffset + run.xPosInLine;
				var baselineY = line.y + line.baselineOffset;
				var (textScaleX, textScaleY) = run.fontDetails.TextScale;
				foreach (var glyph in run.glyphs)
				{
					var origin = new SKPoint(
						runX + glyph.xPosInRun + glyph.position.GlyphPosition.XOffset * textScaleX,
						baselineY + glyph.position.GlyphPosition.YOffset * textScaleY);
					yield return ((ushort)glyph.info.Codepoint, origin, GlyphWidth(glyph.position, run.fontDetails), run.fontDetails);
				}
			}
		}
	}
}
