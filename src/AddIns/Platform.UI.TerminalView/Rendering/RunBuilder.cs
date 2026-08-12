#nullable enable

using System.Collections.Generic;
using System.Text;
using CodeBrix.Terminal.Engine;

namespace CodeBrix.Platform.UI.TerminalView.Rendering;

//was previously: Lily.Shell.TerminalView.Rendering.RunBuilder (the author's original code,
//relicensed from that GPL-3 tool repo to Apache-2.0 for this add-in), updated to the
//CharData.IsBlank API that CodeBrix.Terminal 1.0.223+ exposes for the null-cell check.

/// <summary>
/// Turns one terminal <see cref="BufferLine"/> into drawable
/// <see cref="TextRunSegment"/>s: consecutive single-width cells sharing an
/// attribute coalesce into one segment; each wide character becomes its own
/// two-cell segment; zero-width continuation cells are skipped.
/// </summary>
public static class RunBuilder
{
    /// <summary>
    /// Builds the segments for a line. Only the content up to the line's
    /// trimmed length is considered — cells erased with a colored background
    /// but no character are not yet rendered (accepted v1 limitation).
    /// </summary>
    public static List<TextRunSegment> BuildRuns(BufferLine? line)
    {
        var segments = new List<TextRunSegment>();
        if (line == null) { return segments; }

        var length = line.GetTrimmedLength();
        var text = new StringBuilder();
        var runStart = 0;
        var runCells = 0;
        var runAttribute = CharData.DefaultAttr;

        void Flush()
        {
            if (runCells > 0)
            {
                segments.Add(new TextRunSegment(runStart, runCells, text.ToString(),
                    runAttribute, isWide: false));
                text.Clear();
                runCells = 0;
            }
        }

        for (var col = 0; col < length; col++)
        {
            var cell = line[col];

            if (cell.Width == 0)
            {
                //Continuation half of a wide character
                continue;
            }

            if (cell.Width > 1)
            {
                Flush();
                segments.Add(new TextRunSegment(col, cell.Width, CellText(cell),
                    cell.Attribute, isWide: true));
                continue;
            }

            if (runCells > 0 && cell.Attribute != runAttribute)
            {
                Flush();
            }

            if (runCells == 0)
            {
                runStart = col;
                runAttribute = cell.Attribute;
            }

            text.Append(CellText(cell));
            runCells++;
        }

        Flush();
        return segments;
    }

    internal static string CellText(CharData cell)
    {
        //A blank cell (never written / erased) carries rune U+0200, not a
        //space - drawing its Rune verbatim paints a stray glyph
        if (cell.IsBlank) { return " "; }

        var value = cell.Rune.Value;
        return value <= 0xffff
            ? ((char)value).ToString()
            : char.ConvertFromUtf32((int)value);
    }
}
