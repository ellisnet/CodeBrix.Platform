#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Composition;
using CodeBrix.Platform.UI.Composition.Skia;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using CodeBrix.Platform.UI.Contracts;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents;

// The framework-only, XAML-typed part of the shared text engine (WPE1 C5): building a layout from the XAML inline model
// (TextBlock, TextBox), the ITextLayout/IParsedText implementation the framework's text controls query, drawing with the
// inlines' brushes into a painting session, and hyperlink hit-testing. The engine itself (UnicodeText.skia.cs and the
// files beside it) is compiled into CodeBrix.Platform.UI.TextLayout.Core as well; this file is compiled only here. The
// code is the engine's former inline path, moved verbatim apart from the conversions to and from the engine's own types
// at this boundary (TextFormatting/TextEngineXamlConversions.skia.cs).
internal readonly partial struct UnicodeText : IParsedText
{
	private partial class ReadonlyInlineCopy
	{
		// Null when this copy was built from a TextRunSpec rather than from a XAML Inline, i.e. on the
		// host-free layout path (TextRunSpec, used by the TextLayout add-in). Every read of either member
		// must therefore be null-safe; see Draw() and GetHyperlinkAt().
		public Inline? Inline { get; }
		public Brush? Foreground { get; }

		public ReadonlyInlineCopy(Inline inline, int startIndex, EngineFlowDirection defaultFlowDirection, bool forceDefaultFlowDirection = false)
		{
			CI.Assert(inline is Run or LineBreak);
			Inline = inline;
			Text = inline.GetText();
			Foreground = inline.Foreground;
			FlowDirection = forceDefaultFlowDirection ? defaultFlowDirection : (inline as Run)?.FlowDirection.ToEngine() ?? defaultFlowDirection;
			FontDetails = inline.GetFontInfo();
			FontSize = inline.FontSize;
			FontWeight = inline.FontWeight.Weight;
			FontStretch = inline.FontStretch.ToEngine();
			FontStyle = inline.FontStyle.ToEngine();
			StartIndex = startIndex;
			EndIndex = startIndex + Text.Length;
		}
	}

	private static readonly SKPaint _spareDrawPaint = new();

	bool ITextLayout.IsBaseDirectionRightToLeft => _rtl;

	internal UnicodeText(
		Size availableSize,
		Inline[] inlines, // only leaf nodes
		FontDetails defaultFontDetails, // only used for a final empty line, otherwise the FontDetails are read from the inline
		int maxLines,
		float lineHeight,
		LineStackingStrategy lineStackingStrategy,
		FlowDirection flowDirection,
		TextAlignment? textAlignment, // null to determine from text. This will also infer the directionality of the text from the content
		TextWrapping textWrapping,
		out Size desiredSize)
	{
		CI.Assert(maxLines >= 0);
		_size = availableSize.ToEngine();
		_defaultFontDetails = defaultFontDetails;

		var prepared = PrepareFromInlines(inlines, flowDirection.ToEngine(), textAlignment?.ToEngine());
		_rtl = prepared.FlowDirection == EngineFlowDirection.RightToLeft;
		_textAlignment = prepared.TextAlignment;

		var core = ComputeCore(_size, prepared, _rtl, defaultFontDetails, maxLines, lineHeight, lineStackingStrategy.ToEngine(), textWrapping.ToEngine());
		_lines = core.Lines;
		_textIndexToGlyph = core.TextIndexToGlyph;
		_inlines = core.Inlines;
		_wordBoundaries = core.WordBoundaries;
		_text = core.Text;
		_desiredSize = core.DesiredSizeField;
		desiredSize = core.DesiredSizeOut.ToXaml();
	}

	private static PreparedInlines PrepareFromInlines(Inline[] inlines, EngineFlowDirection flowDirection, EngineTextAlignment? textAlignment)
	{
		List<ReadonlyInlineCopy> copies;
		string text;
		if (textAlignment is null)
		{
			// TODO: can we make this cleaner instead of implicitly assuming that this is a code path coming from TextBox?
			CI.Assert(inlines.Length == 1);
			var inline = (Run)inlines[0];
			var inlineText = inline.GetText();
			if (inlineText.Length == 0)
			{
				flowDirection = inline.FlowDirection.ToEngine();
			}
			else
			{
				var firstInlineText = inlines[0].GetText();
				using var _1 = ICU.CreateBiDiAndSetPara(firstInlineText, 0, firstInlineText.Length, UBIDI_DEFAULT_LTR, out var bidi);
				ICU.GetMethod<ICU.ubidi_getLogicalRun>()(bidi, 0, out _, out var level);
				CI.Assert(level is UBIDI_LTR or UBIDI_RTL);
				flowDirection = level is UBIDI_RTL ? EngineFlowDirection.RightToLeft : EngineFlowDirection.LeftToRight;
			}
			textAlignment = flowDirection is EngineFlowDirection.LeftToRight ? EngineTextAlignment.Left : EngineTextAlignment.Right;
			var copy = new ReadonlyInlineCopy(inline, 0, flowDirection, true);
			var length = copy.Text.Length;
			copies = length == 0 ? [] : [copy];
			text = copy.Text;
		}
		else
		{
			copies = new();
			var lastEnd = 0;
			var builder = new StringBuilder();
			foreach (var inline in inlines)
			{
				var copy = new ReadonlyInlineCopy(inline, lastEnd, flowDirection);
				var length = copy.Text.Length;
				if (length != 0)
				{
					copies.Add(copy);
				}
				lastEnd = copy.EndIndex;
				builder.Append(copy.Text);
			}
			text = builder.ToString();
		}

		return new PreparedInlines(copies, text, flowDirection, textAlignment.Value);
	}

	Rect ITextLayout.GetRectForIndex(int adjustedIndex) => GetRectForIndex(adjustedIndex).ToXaml();

	int ITextLayout.GetIndexAt(Point p, bool ignoreEndingNewLine, bool extendedSelection) =>
		GetIndexAt(p.ToEngine(), ignoreEndingNewLine, extendedSelection);

	public Hyperlink? GetHyperlinkAt(Point point)
	{
		var run = GetIndexAndRunAt(point.ToEngine(), ignoreEndingNewLine: false, extendedSelection: false).run;
		DependencyObject? parent = run?.inline.Inline;
		while (parent is TextElement textElement)
		{
			if (parent is Hyperlink h)
			{
				return h;
			}
			parent = textElement.GetParent() as DependencyObject;
		}

		return null;
	}

	public void Draw(in PaintingSession session, (int index, CompositionBrush brush, float thickness)? caret,
		(int selectionStart, int selectionEnd, CompositionBrush selectedTextBackgroundBrush, Brush selectedTextForegroundBrush)? selection)
	{
		// if selection is out of range, this means that the parent TextBlock/TextBox updated the text and the
		// selection but a new UnicodeText instance has not been created yet. In that case, skip rendering
		// the selection this frame and wait to be called again after measuring.
		(int selectionIndexStart, int selectionIndexEnd, Cluster selectionClusterStart, Cluster selectionClusterEnd, CompositionBrush background, Brush foreground)? selectionDetails = null;
		if (selection is { } s && s.selectionStart != s.selectionEnd && s.selectionStart <= _text.Length && s.selectionEnd <= _text.Length && _text.Length > 0)
		{
			selectionDetails = (s.selectionStart, s.selectionEnd, _textIndexToGlyph[s.selectionStart], _textIndexToGlyph[Math.Min(_textIndexToGlyph.Length - 1, s.selectionEnd)], s.selectedTextBackgroundBrush, s.selectedTextForegroundBrush);
		}

		for (var index = 0; index < _lines.Count; index++)
		{
			var line = _lines[index];
			var currentLineX = line.xAlignmentOffset;
			foreach (var run in line.runs)
			{
				// Ideally, we would want to get the path of the glyphs and then draw them using CompositionBrush.Paint, but this
				// does not work for Emojis which don't have a path and instead must be drawn directly with SKCanvas.DrawText
				// var path = new SKPath();
				// for (var i = 0; i < run.glyphs.Length; i++)
				// {
				// 	var glyph = run.glyphs[i];
				// 	var p = run.fontDetails.SKFont.GetGlyphPath((ushort)glyph.info.Codepoint);
				// 	p.Transform(SKMatrix.CreateTranslation(glyph.xPosInRun + glyph.position.XOffset * run.fontDetails.TextScale.textScaleX, glyph.position.YOffset * run.fontDetails.TextScale.textScaleY), p);
				// 	path.AddPath(p);
				// }
				// path.Transform(SKMatrix.CreateTranslation(currentLineX, line.y + line.baselineOffset), path);
				//
				// session.Canvas.Save();
				// session.Canvas.ClipPath(path, antialias: true);
				// run.inline.Foreground.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor()).Paint(session.Canvas, session.Opacity, path.Bounds);
				// session.Canvas.Restore();

				using (var textBlobBuilder = new SKTextBlobBuilder())
				{
					var glyphs = new ushort[run.glyphs.Length];
					var positions = new SKPoint[run.glyphs.Length];
					for (var i = 0; i < run.glyphs.Length; i++)
					{
						var glyph = run.glyphs[i];
						glyphs[i] = (ushort)glyph.info.Codepoint;
						positions[i] = new SKPoint(glyph.xPosInRun + glyph.position.GlyphPosition.XOffset * run.fontDetails.TextScale.textScaleX, line.y + glyph.position.GlyphPosition.YOffset * run.fontDetails.TextScale.textScaleY);
					}

					void DrawText(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<SKPoint> positions, PaintingSession session, Brush? brush)
					{
						textBlobBuilder.AddPositionedRun(glyphs, run.fontDetails.SKFont, positions);
						var blob1 = textBlobBuilder.Build(); // Build resets the blob builder
						var paint = SetupPaint(brush, session.Opacity);
						session.Canvas.DrawText(blob1, currentLineX, line.baselineOffset, paint);
					}

					if (selectionDetails is { } sd && (sd.selectionClusterStart.sourceTextStart < run.endInInline + run.inline.StartIndex && (selection!.Value.selectionEnd == _text.Length || run.startInInline + run.inline.StartIndex < sd.selectionClusterEnd.sourceTextStart)))
					{
						int selectionLeft;
						int selectionRight; // the selection ends to the left of positions[selectionRight].X
						if (run.rtl)
						{
							selectionLeft = sd.selectionClusterEnd.layoutedRun == run && selection!.Value.selectionEnd != _text.Length ? sd.selectionClusterEnd.glyphInRunIndexEnd : 0;
							selectionRight = sd.selectionClusterStart.layoutedRun == run ? sd.selectionClusterStart.glyphInRunIndexStart + 1 : run.glyphs.Length;
						}
						else
						{
							selectionLeft = sd.selectionClusterStart.layoutedRun == run ? sd.selectionClusterStart.glyphInRunIndexStart : 0;
							selectionRight = sd.selectionClusterEnd.layoutedRun == run && selection!.Value.selectionEnd != _text.Length ? sd.selectionClusterEnd.glyphInRunIndexStart : run.glyphs.Length;
						}

						var leftX = positions[selectionLeft].X;
						var rightX = positions[selectionRight - 1].X + GlyphWidth(run.glyphs[selectionRight - 1].position, run.fontDetails);
						var selectionRect = new SKRect(currentLineX + leftX, line.y, currentLineX + rightX, line.y + line.lineHeight);
						CompositionBrushSkiaPlatform.Of(sd.background).Paint(session.Canvas, session.Opacity, selectionRect);

						var glyphsSpan = glyphs.AsSpan();
						var positionsSpan = positions.AsSpan();
						if (selectionLeft > 0)
						{
							DrawText(glyphsSpan[..selectionLeft], positionsSpan[..selectionLeft], session, run.inline.Foreground);
						}
						DrawText(glyphsSpan[selectionLeft..selectionRight], positionsSpan[selectionLeft..selectionRight], session, sd.foreground);
						if (selectionRight < run.glyphs.Length)
						{
							DrawText(glyphsSpan[selectionRight..], positionsSpan[selectionRight..], session, run.inline.Foreground);
						}
					}
					else
					{
						DrawText(glyphs, positions, session, run.inline.Foreground);
					}

					// The Underline / Strikethrough line of a run whose inline carries TextDecorations (set on the
					// Run itself, or inherited from an Underline span or the TextBlock). Host-free TextRunSpec runs
					// have no inline and so never carry decorations.
					if (run.inline.Inline is { } decoratedInline && run.width > 0)
					{
						var decorations = decoratedInline.TextDecorations;
						if ((decorations & (TextDecorations.Underline | TextDecorations.Strikethrough)) != 0)
						{
							var metrics = run.fontDetails.SKFontMetrics;
							var fontSize = run.fontDetails.SKFontSize;
							var baselineY = line.y + line.baselineOffset;
							var decorationPaint = SetupPaint(run.inline.Foreground, session.Opacity);

							if ((decorations & TextDecorations.Underline) != 0)
							{
								var thickness = Math.Max(1f, metrics.UnderlineThickness ?? fontSize / 14f);
								var y = baselineY + (metrics.UnderlinePosition ?? fontSize / 10f);
								session.Canvas.DrawRect(new SKRect(currentLineX, y, currentLineX + run.width, y + thickness), decorationPaint);
							}

							if ((decorations & TextDecorations.Strikethrough) != 0)
							{
								var thickness = Math.Max(1f, metrics.StrikeoutThickness ?? fontSize / 14f);
								var y = baselineY + (metrics.StrikeoutPosition ?? fontSize / -3.5f);
								session.Canvas.DrawRect(new SKRect(currentLineX, y - thickness / 2, currentLineX + run.width, y + thickness / 2), decorationPaint);
							}
						}
					}

					currentLineX += run.width;
				}
			}
		}

		// if the caret index is out of range, this means that the parent TextBlock/TextBox updated the text and the
		// caret position but a new UnicodeText instance has not been created yet. In that case, skip care rendering
		// this frame and wait to be called again after measuring.
		if (caret is { } c && caret.Value.index <= _text.Length)
		{
			CompositionBrushSkiaPlatform.Of(c.brush).Paint(session.Canvas, session.Opacity, GetCaretRectForIndex(c.index, c.thickness).ToXaml().ToSKRect());
		}
	}

	// foreground is null only on the host-free TextRunSpec construction path, where no XAML brush
	// exists. Reset() leaves the paint opaque black, which is the documented fallback for that path.
	private static SKPaint SetupPaint(Brush? foreground, float opacity)
	{
		var paint = _spareDrawPaint;
		paint.Reset();
		paint.IsStroke = false;
		paint.IsAntialias = true;

		if (foreground is SolidColorBrush scb)
		{
			var scbColor = scb.Color;
			paint.Color = new SKColor(
				red: scbColor.R,
				green: scbColor.G,
				blue: scbColor.B,
				alpha: (byte)(scbColor.A * scb.Opacity * opacity));
		}
		else if (foreground is GradientBrush gb)
		{
			var gbColor = gb.FallbackColorWithOpacity;
			paint.Color = new SKColor(
				red: gbColor.R,
				green: gbColor.G,
				blue: gbColor.B,
				alpha: (byte)(gbColor.A * opacity));
		}
		else if (foreground is XamlCompositionBrushBase xcbb)
		{
			var gbColor = xcbb.FallbackColorWithOpacity;
			paint.Color = new SKColor(
				red: gbColor.R,
				green: gbColor.G,
				blue: gbColor.B,
				alpha: (byte)(gbColor.A * opacity));
		}

		return paint;
	}
}
