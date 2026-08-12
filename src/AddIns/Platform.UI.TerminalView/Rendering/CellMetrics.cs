#nullable enable

using System;
using CodeBrix.Platform.UI.TextLayout;

namespace CodeBrix.Platform.UI.TerminalView.Rendering;

//was previously: Lily.Shell.TerminalView.Rendering.CellMetrics (the author's original code,
//relicensed from that GPL-3 tool repo to Apache-2.0 for this add-in).

/// <summary>
/// The fixed cell geometry of the terminal grid, measured from a reference
/// glyph in the terminal font (the family's TextView recipe: measure "x" —
/// for a monospaced font its advance IS the cell advance). Re-measure on any
/// font family/size change.
/// </summary>
public readonly record struct CellMetrics
{
    internal CellMetrics(float width, float height, float baseline)
    {
        Width = width;
        Height = height;
        Baseline = baseline;
    }

    /// <summary>The cell advance (width of one column).</summary>
    public float Width { get; }

    /// <summary>The cell height (one row).</summary>
    public float Height { get; }

    /// <summary>The text baseline offset from the top of the cell.</summary>
    public float Baseline { get; }

    /// <summary>Measures the cell geometry for a font family + size (null = the engine default font).</summary>
    public static CellMetrics Measure(string? fontFamily, float fontSize)
    {
        var run = new TextRunDescriptor("x", fontFamily, fontSize);
        using var layout = TextLayoutEngine.Layout([run]);
        var metrics = layout.GetLineMetrics(0);

        return new CellMetrics(
            Math.Max(1f, layout.Size.Width),
            Math.Max(1f, metrics.Height),
            Math.Max(1f, metrics.BaselineOffset));
    }
}
