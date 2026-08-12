#nullable enable

using SkiaSharp;

namespace CodeBrix.Platform.UI.TerminalView.Rendering;

//was previously: Lily.Shell.TerminalView.Rendering.CellStyle (the author's original code,
//relicensed from that GPL-3 tool repo to Apache-2.0 for this add-in).

/// <summary>
/// The resolved drawing style of one terminal cell (or run of cells sharing
/// an attribute): concrete colors plus the type-face and decoration flags.
/// Produced by <see cref="AttributeDecoder"/>.
/// </summary>
public readonly record struct CellStyle(
    SKColor Foreground,
    SKColor Background,
    bool Bold,
    bool Italic,
    bool Underline,
    bool CrossedOut)
{
    /// <summary>True when the background differs from the terminal default and needs a fill rect.</summary>
    public bool HasVisibleBackground(SKColor defaultBackground) => Background != defaultBackground;
}
