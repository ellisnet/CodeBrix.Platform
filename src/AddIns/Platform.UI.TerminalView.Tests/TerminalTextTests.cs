#nullable enable

using CodeBrix.Platform.UI.TerminalView.Engine;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

/// <summary>
/// Reading the terminal's text back: TerminalRenderer.GetVisibleLines and TerminalRenderer.GetText, which
/// TerminalControl exposes publicly under the same names. Host-free: a bare renderer, no surface.
/// </summary>
public class TerminalTextTests
{
    [Fact]
    public void visible_lines_are_the_viewport_rows_without_trailing_blanks()
    {
        //Arrange
        var renderer = CreateRenderer(columns: 40, rows: 5);

        //Act
        renderer.Feed("first   \r\nsecond\r\n$ ");
        var lines = renderer.GetVisibleLines();

        //Assert
        lines.Should().Equal("first", "second", "$", "", "");
    }

    [Fact]
    public void visible_lines_follow_the_scroll_position()
    {
        //Arrange - twelve lines in a five-row terminal leave seven in the scrollback
        var renderer = CreateRenderer(columns: 40, rows: 5);
        renderer.Feed(Lines(12));

        //Act
        var live = renderer.GetVisibleLines();
        renderer.ScrollLines(-3);
        var scrolled = renderer.GetVisibleLines();

        //Assert
        live.Should().Equal("line 8", "line 9", "line 10", "line 11", "");
        scrolled.Should().Equal("line 5", "line 6", "line 7", "line 8", "line 9");
    }

    [Fact]
    public void text_covers_the_scrollback_and_joins_wrapped_rows()
    {
        //Arrange - a 10-column terminal wraps the long line onto a second row
        var renderer = CreateRenderer(columns: 10, rows: 4);

        //Act
        renderer.Feed("one\r\ntwo\r\nthree\r\nabcdefghijklmn\r\nend");
        var text = renderer.GetText();

        //Assert
        text.Should().Be("one\ntwo\nthree\nabcdefghijklmn\nend");
    }

    [Fact]
    public void text_of_a_reset_terminal_is_empty()
    {
        //Arrange
        var renderer = CreateRenderer(columns: 40, rows: 5);
        renderer.Feed("something");

        //Act
        renderer.Reset();

        //Assert
        renderer.GetText().Should().BeEmpty();
        renderer.GetVisibleLines().Should().HaveCount(5);
    }

    private static TerminalRenderer CreateRenderer(int columns, int rows)
    {
        var renderer = new TerminalRenderer { FontSize = 14f };
        var cell = renderer.Metrics;
        renderer.FitToSize(columns * cell.Width + cell.Width / 2, rows * cell.Height + cell.Height / 2, out var fittedColumns, out var fittedRows);
        fittedColumns.Should().Be(columns);
        fittedRows.Should().Be(rows);
        return renderer;
    }

    private static string Lines(int count)
    {
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < count; i++) { text.Append("line ").Append(i).Append("\r\n"); }
        return text.ToString();
    }
}
