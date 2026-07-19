#nullable enable

using System;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TextLayout;

/// <summary>
/// A single positioned glyph and its outline.
/// </summary>
/// <remarks>
/// This owns its <see cref="Path"/>; dispose it when done, or dispose the whole list returned by
/// <see cref="TextLayoutResult.GetGlyphOutlines"/>. Use
/// <see cref="TextLayoutResult.GetOutlinePath"/> instead when a single combined path is all that is
/// wanted.
/// </remarks>
public sealed class GlyphOutline : IDisposable
{
	private bool _disposed;

	internal GlyphOutline(ushort glyphId, SKPath? path, SKPoint origin, float advance, SKFont font)
	{
		GlyphId = glyphId;
		Path = path;
		Origin = origin;
		Advance = advance;
		Font = font;
	}

	/// <summary>The glyph id within <see cref="Font"/>.</summary>
	public ushort GlyphId { get; }

	/// <summary>
	/// The glyph outline, positioned at the origin rather than at <see cref="Origin"/>.
	/// </summary>
	/// <remarks>
	/// Glyphs with nothing to draw - a space, or a bitmap or colour emoji - yield an empty path.
	/// Translate by <see cref="Origin"/> to place it.
	/// </remarks>
	public SKPath? Path { get; }

	/// <summary>The glyph origin - baseline left - in layout coordinates.</summary>
	public SKPoint Origin { get; }

	/// <summary>The glyph's horizontal advance, in layout coordinates.</summary>
	public float Advance { get; }

	/// <summary>
	/// The font this glyph belongs to.
	/// </summary>
	/// <remarks>
	/// This can differ from glyph to glyph within one layout: when the requested face has no glyph
	/// for a character, the engine falls back to another face for that stretch of text. The font is
	/// owned by the engine's font cache and must not be disposed.
	/// </remarks>
	public SKFont Font { get; }

	/// <summary>Disposes the outline path.</summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		Path?.Dispose();
	}
}
