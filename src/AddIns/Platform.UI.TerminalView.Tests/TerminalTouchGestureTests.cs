#nullable enable

using CodeBrix.Platform.UI.TerminalView.Engine;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

/// <summary>
/// WPE1-21 (FIXLIST_codebrix_android_buildout [AP7-B TerminalView] no touch gestures): a finger dragged up/down scrolls
/// the history, a finger dragged sideways selects, a long press then a drag selects, a long press alone asks for the
/// Copy/Paste menu, a tap is a click. Host-free: a bare TerminalRenderer driven through TerminalTouchGesture with
/// explicit times (TerminalControl forwards its touch pointer events here with Environment.TickCount64).
/// </summary>
public class TerminalTouchGestureTests
{
    private const double SurfaceWidth = 800;
    private const long Quick = 50;
    private const long Held = TerminalTouchGesture.LongPressMilliseconds + 100;

    [Fact]
    public void a_finger_dragged_down_scrolls_back_one_line_per_row_height_travelled()
    {
        //Arrange - history above a live tail
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed(Lines(40));
        var live = renderer.Terminal.Buffer.YDisp;
        var gesture = new TerminalTouchGesture(renderer);

        //Act - down by three and a half rows, in steps
        gesture.Press(100, 2 * cell.Height, 0);
        gesture.Move(100, 3 * cell.Height, Quick).Should().Be(TerminalTouchState.Scrolling);
        gesture.Move(100, 4.5 * cell.Height, Quick + 10);
        var outcome = gesture.Release(100, 5.5 * cell.Height, Quick + 20);

        //Assert
        outcome.Should().Be(TerminalTouchOutcome.ScrollEnded);
        renderer.Terminal.Buffer.YDisp.Should().Be(live - 3);
        renderer.Selection.Active.Should().BeFalse();
    }

    [Fact]
    public void a_finger_dragged_up_after_scrolling_back_scrolls_forward_again()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed(Lines(40));
        var live = renderer.Terminal.Buffer.YDisp;
        renderer.ScrollLines(-5);
        var gesture = new TerminalTouchGesture(renderer);

        //Act
        gesture.Press(100, 8 * cell.Height, 0);
        gesture.Move(100, 6 * cell.Height, Quick);
        gesture.Release(100, 6 * cell.Height, Quick + 10);

        //Assert
        renderer.Terminal.Buffer.YDisp.Should().Be(live - 3);
    }

    [Fact]
    public void a_finger_dragged_sideways_selects_from_where_it_went_down()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed("ABCDEFGHIJ");
        var gesture = new TerminalTouchGesture(renderer);
        var row = 0.5 * cell.Height;

        //Act - down in cell 2, one move per cell to cell 5, lift in cell 5
        gesture.Press(2.5 * cell.Width, row, 0);
        for (var column = 3; column <= 5; column++)
        {
            gesture.Move((column + 0.5) * cell.Width, row, Quick);
        }
        gesture.State.Should().Be(TerminalTouchState.Selecting);
        var outcome = gesture.Release(5.5 * cell.Width, row, Quick + 10);

        //Assert
        outcome.Should().Be(TerminalTouchOutcome.SelectionEnded);
        renderer.GetSelectedText().Should().Be("CDE");
        renderer.IsSelecting.Should().BeFalse();
    }

    [Fact]
    public void a_selection_that_lifts_beyond_its_last_move_runs_to_the_lift_point()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed("ABCDEFGHIJ");
        var gesture = new TerminalTouchGesture(renderer);
        var row = 0.5 * cell.Height;

        //Act
        gesture.Press(0.5 * cell.Width, row, 0);
        gesture.Move(2.5 * cell.Width, row, Quick);
        gesture.Release(6.5 * cell.Width, row, Quick + 10);

        //Assert
        renderer.GetSelectedText().Should().Be("ABCDEF");
    }

    [Fact]
    public void a_long_press_then_a_vertical_drag_selects_instead_of_scrolling()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed(Lines(40));
        var live = renderer.Terminal.Buffer.YDisp;
        var gesture = new TerminalTouchGesture(renderer);

        //Act - rest past the long-press time, then drag down two rows
        gesture.Press(0.5 * cell.Width, 1.5 * cell.Height, 0);
        gesture.Move(0.5 * cell.Width + 1, 1.5 * cell.Height + 1, Held).Should().Be(TerminalTouchState.Pending, "a move inside the slop decides nothing");
        gesture.Move(0.5 * cell.Width, 3.5 * cell.Height, Held + 10).Should().Be(TerminalTouchState.Selecting);
        var outcome = gesture.Release(0.5 * cell.Width, 3.5 * cell.Height, Held + 20);

        //Assert
        outcome.Should().Be(TerminalTouchOutcome.SelectionEnded);
        renderer.Terminal.Buffer.YDisp.Should().Be(live);
        renderer.Selection.Active.Should().BeTrue();
        renderer.GetSelectedText().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void a_long_press_without_a_drag_asks_for_the_menu_and_keeps_the_selection()
    {
        //Arrange - something selected first
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed("ABCDEFGHIJ");
        var gesture = new TerminalTouchGesture(renderer);
        var row = 0.5 * cell.Height;
        gesture.Press(0.5 * cell.Width, row, 0);
        gesture.Move(3.5 * cell.Width, row, Quick);
        gesture.Release(3.5 * cell.Width, row, Quick + 10);

        //Act - a long press elsewhere, with a little jitter, and a lift
        gesture.Press(7.5 * cell.Width, row, 1000);
        gesture.Move(7.5 * cell.Width + 3, row - 2, 1000 + Held);
        var outcome = gesture.Release(7.5 * cell.Width + 3, row - 2, 1000 + Held + 10);

        //Assert
        outcome.Should().Be(TerminalTouchOutcome.ContextMenu);
        renderer.GetSelectedText().Should().Be("ABC");
    }

    [Fact]
    public void a_short_tap_is_a_click_and_clears_the_selection()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed("ABCDEFGHIJ");
        var gesture = new TerminalTouchGesture(renderer);
        var row = 0.5 * cell.Height;
        gesture.Press(0.5 * cell.Width, row, 0);
        gesture.Move(3.5 * cell.Width, row, Quick);
        gesture.Release(3.5 * cell.Width, row, Quick + 10);

        //Act
        gesture.Press(7.5 * cell.Width, row, 5000);
        var outcome = gesture.Release(7.5 * cell.Width, row, 5000 + Quick);

        //Assert
        outcome.Should().Be(TerminalTouchOutcome.Tapped);
        renderer.GetSelectedText().Should().BeNullOrEmpty("a tap, like a click, leaves nothing selected");
        renderer.IsSelecting.Should().BeFalse();
    }

    [Fact]
    public void a_cancelled_selection_ends_where_it_is()
    {
        //Arrange
        var renderer = CreateRenderer(rows: 10, out var cell);
        renderer.Feed("ABCDEFGHIJ");
        var gesture = new TerminalTouchGesture(renderer);
        var row = 0.5 * cell.Height;
        gesture.Press(0.5 * cell.Width, row, 0);
        gesture.Move(2.5 * cell.Width, row, Quick);

        //Act
        gesture.Cancel();

        //Assert
        gesture.State.Should().Be(TerminalTouchState.None);
        renderer.IsSelecting.Should().BeFalse();
        gesture.Release(2.5 * cell.Width, row, Quick + 10).Should().Be(TerminalTouchOutcome.None);
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
