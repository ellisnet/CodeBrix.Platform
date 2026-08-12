#nullable enable

using CodeBrix.Terminal.Engine;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TerminalView.Rendering;

//was previously: Lily.Shell.TerminalView.Rendering.AttributeDecoder (the author's original
//code, relicensed from that GPL-3 tool repo to Apache-2.0 for this add-in), updated to the
//CharacterAttribute.Unpack API that CodeBrix.Terminal 1.0.223+ exposes for exactly this job.

/// <summary>
/// Decodes a CodeBrix.Terminal packed cell attribute into a drawable
/// <see cref="CellStyle"/>, applying the view-level color policies the raw
/// engine values leave to the renderer: bold-as-bright palette promotion,
/// INVERSE swap, DIM darkening, and INVISIBLE-as-background.
/// </summary>
public static class AttributeDecoder
{
    /// <summary>
    /// Decodes a packed attribute against the view's default colors.
    /// </summary>
    public static CellStyle Decode(int attribute, SKColor defaultForeground, SKColor defaultBackground)
    {
        var (fgIndex, bgIndex, flags) = CharacterAttribute.Unpack(attribute);

        //Classic bold-as-bright: BOLD promotes the dark palette (0-7) to bright (8-15)
        if (flags.HasFlag(FLAGS.BOLD) && fgIndex < 8)
        {
            fgIndex += 8;
        }

        var foreground = Resolve(fgIndex, defaultForeground, defaultBackground);
        var background = Resolve(bgIndex, defaultBackground, defaultForeground);

        if (flags.HasFlag(FLAGS.INVERSE))
        {
            (foreground, background) = (background, foreground);
        }

        if (flags.HasFlag(FLAGS.DIM))
        {
            foreground = Dim(foreground);
        }

        if (flags.HasFlag(FLAGS.INVISIBLE))
        {
            foreground = background;
        }

        return new CellStyle(
            foreground,
            background,
            flags.HasFlag(FLAGS.BOLD),
            flags.HasFlag(FLAGS.ITALIC),
            flags.HasFlag(FLAGS.UNDERLINE),
            flags.HasFlag(FLAGS.CrossedOut));
    }

    private static SKColor Resolve(int index, SKColor defaultColor, SKColor invertedDefault)
    {
        if (index == CharacterAttribute.DefaultColorIndex) { return defaultColor; }
        if (index == CharacterAttribute.InvertedDefaultColorIndex) { return invertedDefault; }

        if (index >= 0 && index < Color.DefaultAnsiColors.Count)
        {
            var c = Color.DefaultAnsiColors[index];
            return new SKColor(c.Red, c.Green, c.Blue);
        }

        return defaultColor;
    }

    private static SKColor Dim(SKColor color) =>
        new((byte)(color.Red * 0.6), (byte)(color.Green * 0.6), (byte)(color.Blue * 0.6));
}
