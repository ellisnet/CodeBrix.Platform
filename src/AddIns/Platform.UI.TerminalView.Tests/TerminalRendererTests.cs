#nullable enable

using System.Collections.Generic;
using CodeBrix.Platform.UI.TerminalView.Engine;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

/// <summary>
/// The caret rectangle a platform places its keyboard focus view on (WPE1-18): TerminalRenderer.GetCaretRect and
/// TerminalRenderer.CaretRectChanged, which TerminalControl forwards (plus the surface's offset in the control) as
/// GetCaretRectForPlatform and CaretRectChangedForPlatform. Host-free: a bare renderer, no surface (a TerminalControl
/// cannot be constructed without a running application: its XAML type initializers need one).
/// </summary>
public class TerminalRendererTests
{
    private const double SurfaceWidth = 800;

    [Fact]
    public void after_feeding_lines_the_caret_rect_is_the_cursor_cell()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);

        //Act - three lines; the cursor ends after "ccc" on the third row
        renderer.Feed("a\r\nb\r\nccc");
        var rect = renderer.GetCaretRect();

        //Assert
        rect.Should().Be(SKRect.Create(3 * cell.Width, 2 * cell.Height, cell.Width, cell.Height));
    }

    [Fact]
    public void scrolling_into_history_takes_a_bottom_row_caret_out_of_view_and_typing_brings_it_back()
    {
        //Arrange - more lines than rows, so the cursor sits on the last row with history above
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed(Lines(30) + "$ ");
        var live = renderer.GetCaretRect();

        //Act
        renderer.ScrollLines(-1);
        var oneBack = renderer.GetCaretRect();
        renderer.ScrollLines(-9);
        var outOfView = renderer.GetCaretRect();
        renderer.PrepareForInput();
        var snappedBack = renderer.GetCaretRect();

        //Assert
        live.Should().Be(SKRect.Create(2 * cell.Width, 9 * cell.Height, cell.Width, cell.Height));
        oneBack.IsEmpty.Should().BeTrue(); // the cursor's line (the last row) moved below the viewport
        outOfView.IsEmpty.Should().BeTrue();
        snappedBack.Should().Be(live);
    }

    [Fact]
    public void scrolling_back_while_the_cursor_is_mid_screen_moves_the_caret_rect_down_by_whole_rows()
    {
        //Arrange - history above, then a cleared screen with the cursor homed to row 2
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed(Lines(30) + "\u001b[2J\u001b[3;5H");
        var live = renderer.GetCaretRect();

        //Act
        renderer.ScrollLines(-3);
        var threeBack = renderer.GetCaretRect();

        //Assert
        live.Should().Be(SKRect.Create(4 * cell.Width, 2 * cell.Height, cell.Width, cell.Height));
        threeBack.Should().Be(SKRect.Create(4 * cell.Width, 5 * cell.Height, cell.Width, cell.Height));
    }

    [Fact]
    public void a_cursor_hidden_by_the_application_has_an_empty_caret_rect_until_shown_again()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out _);
        renderer.Feed("prompt");
        var shown = renderer.GetCaretRect();

        //Act
        renderer.Feed("\u001b[?25l");
        var hidden = renderer.GetCaretRect();
        renderer.Feed("\u001b[?25h");
        var shownAgain = renderer.GetCaretRect();

        //Assert
        shown.IsEmpty.Should().BeFalse();
        hidden.IsEmpty.Should().BeTrue();
        shownAgain.Should().Be(shown);
    }

    [Fact]
    public void the_caret_changed_event_fires_when_the_cursor_moves_and_not_when_it_stays()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        var seen = new List<SKRect>();
        renderer.CaretRectChanged += () => seen.Add(renderer.GetCaretRect());

        //Act
        renderer.Feed("ab");              // first report after subscribing, cursor at column 2
        renderer.Feed("\u001b[31m");      // colour only: the cursor stays
        renderer.Feed("c");               // column 3
        renderer.Feed("\u001b[?25l");     // hidden
        renderer.ScrollLines(-1);         // nothing to scroll into: no change

        //Assert
        seen.Should().Equal(
            SKRect.Create(2 * cell.Width, 0, cell.Width, cell.Height),
            SKRect.Create(3 * cell.Width, 0, cell.Width, cell.Height),
            SKRect.Empty);
    }

    [Fact]
    public void the_caret_changed_event_fires_on_refitting_scrolling_and_a_font_change()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed(Lines(30) + "\u001b[2J\u001b[3;5H");
        var count = 0;
        renderer.CaretRectChanged += () => count++;
        renderer.Feed("x");               // the first report after subscribing
        var afterFirst = count;

        //Act
        //Narrower than the cursor column (5): the refit grid clamps the cursor to its last column
        renderer.FitToSize(4 * cell.Width + cell.Width / 2, 10 * cell.Height + cell.Height / 2, out var columns, out _);
        var afterRefit = count;
        renderer.ScrollLines(-2);
        var afterScroll = count;
        renderer.FontSize = renderer.FontSize * 2;
        var afterFont = count;

        //Assert
        columns.Should().Be(4);
        afterFirst.Should().Be(1);
        afterRefit.Should().Be(2);
        afterScroll.Should().Be(3);
        afterFont.Should().Be(4);
        renderer.GetCaretRect().Width.Should().BeGreaterThan(cell.Width);
    }

    [Fact]
    public void an_unsubscribed_renderer_reports_again_after_a_new_subscriber_appears()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out _);
        var first = 0;
        void First() => first++;
        renderer.CaretRectChanged += First;
        renderer.Feed("a");
        renderer.CaretRectChanged -= First;
        renderer.Feed("b");

        //Act - the rect is unchanged by this feed, yet the new subscriber hears the current one
        var second = 0;
        renderer.CaretRectChanged += () => second++;
        renderer.Feed("\u001b[0m");

        //Assert
        first.Should().Be(1);
        second.Should().Be(1);
    }

    private static TerminalRenderer CreateRenderer(int rows, out Rendering.CellMetrics cell)
    {
        var renderer = new TerminalRenderer { FontSize = 14f };
        cell = renderer.Metrics;
        renderer.FitToSize(SurfaceWidth, rows * cell.Height + cell.Height / 2, out _, out var fitted);
        fitted.Should().Be(rows);
        return renderer;
    }

    private static string Lines(int count)
    {
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < count; i++) { text.Append("line ").Append(i).Append("\r\n"); }
        return text.ToString();
    }
}
