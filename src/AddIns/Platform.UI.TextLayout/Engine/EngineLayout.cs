#nullable enable

using System.Collections.Generic;
using CodeBrix.Platform.UI.TextLayout.Contracts;
using CodeBrix.Platform.UI.TextLayout.Internal;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout.Engine;

/// <summary>
/// One layout of this assembly's copy of the shared text engine (<see cref="UnicodeText"/>, a struct), exposed to
/// <see cref="TextLayoutResult"/> through <see cref="IEngineLayout"/>. Every member forwards to the engine; the only
/// work done here is converting the engine's geometry to SkiaSharp's.
/// </summary>
/// <remarks>
/// Formerly the Skia twin's EngineLayoutSkiaPlatform, moved here at the text-engine home change (WPE1 C5).
/// </remarks>
internal sealed class EngineLayout : IEngineLayout
{
	private readonly UnicodeText _layout;

	/// <summary>Wraps a completed engine layout.</summary>
	/// <param name="layout">The layout.</param>
	internal EngineLayout(UnicodeText layout) => _layout = layout;

	/// <inheritdoc />
	public string LayoutText => _layout.LayoutText;

	/// <inheritdoc />
	public int LineCount => _layout.LineCount;

	/// <inheritdoc />
	public bool IsRightToLeft => _layout.IsRightToLeft;

	/// <inheritdoc />
	public float GetLineHeight(int lineIndex) => _layout.GetLineHeight(lineIndex);

	/// <inheritdoc />
	public float GetLineTop(int lineIndex) => _layout.GetLineTop(lineIndex);

	/// <inheritdoc />
	public float GetLineBaselineOffset(int lineIndex) => _layout.GetLineBaselineOffset(lineIndex);

	/// <inheritdoc />
	public int GetLineStartInText(int lineIndex) => _layout.GetLineStartInText(lineIndex);

	/// <inheritdoc />
	public int GetLineEndInText(int lineIndex) => _layout.GetLineEndInText(lineIndex);

	/// <inheritdoc />
	public SKRect GetCaretRectForIndex(int index, float caretThickness) =>
		EngineGeometry.ToSKRect(_layout.GetCaretRectForIndex(index, caretThickness));

	/// <inheritdoc />
	public SKRect GetRectForIndex(int index) => EngineGeometry.ToSKRect(_layout.GetRectForIndex(index));

	/// <inheritdoc />
	public int GetIndexAt(SKPoint point, bool ignoreEndingNewLine, bool extendedSelection) =>
		_layout.GetIndexAt(new EnginePoint(point.X, point.Y), ignoreEndingNewLine, extendedSelection);

	/// <inheritdoc />
	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index) => _layout.GetLineAt(index);

	/// <inheritdoc />
	public IReadOnlyList<SKRect> GetSelectionRects(int start, int length)
	{
		var rects = _layout.GetSelectionRects(start, length);
		var result = new List<SKRect>(rects.Count);
		foreach (var rect in rects)
		{
			result.Add(EngineGeometry.ToSKRect(rect));
		}

		return result;
	}

	/// <inheritdoc />
	public SKPath GetOutlinePath() => _layout.GetOutlinePath();

	//Moved verbatim from the Skia twin's EngineLayoutSkiaPlatform.GetGlyphOutlines (formerly
	//TextLayoutResult.GetGlyphOutlines): it copies each engine outline into the public GlyphOutline.
	/// <inheritdoc />
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

	/// <inheritdoc />
	public void Draw(SKCanvas canvas, SKPoint origin, SKPaint paint) => _layout.DrawToCanvas(canvas, origin, paint);
}
