#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// A single positioned glyph produced by the layout engine, together with its outline.
/// </summary>
/// <param name="GlyphId">The glyph id within <paramref name="Font"/>.</param>
/// <param name="Path">
/// The glyph outline in font units at the layout's font size, positioned at the origin. It is the
/// caller's responsibility to dispose it. Glyphs with no outline (a space, or a bitmap/colour emoji)
/// yield an empty path rather than null.
/// </param>
/// <param name="Origin">The glyph origin (baseline left) in layout coordinates.</param>
/// <param name="Advance">The glyph's horizontal advance in layout coordinates.</param>
/// <param name="Font">The font the glyph id belongs to; may differ between glyphs because of font fallback.</param>
internal sealed record TextGlyphOutline(
	ushort GlyphId,
	SKPath? Path,
	SKPoint Origin,
	float Advance,
	SKFont Font);
