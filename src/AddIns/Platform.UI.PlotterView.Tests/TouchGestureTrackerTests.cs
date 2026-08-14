#nullable enable

using CodeBrix.Platform.UI.PlotterView.Input;
using CodeBrix.Plotter;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.PlotterView.Tests;

public class TouchGestureTrackerTests
{
    [Fact]
    public void the_first_contact_starts_the_gesture()
    {
        //Arrange
        var tracker = new TouchGestureTracker();

        //Assert
        tracker.Down(1, new ScreenPoint(10, 10)).Should().Be(true);
        tracker.Down(2, new ScreenPoint(90, 10)).Should().Be(false);
        tracker.Count.Should().Be(2);
    }

    [Fact]
    public void move_snapshots_before_and_after_in_press_order()
    {
        //Arrange
        var tracker = new TouchGestureTracker();
        tracker.Down(7, new ScreenPoint(10, 20));
        tracker.Down(3, new ScreenPoint(100, 20));

        //Act: the SECOND finger moves; the first stays put
        var moved = tracker.Move(3, new ScreenPoint(120, 25), out var current, out var previous);

        //Assert: arrays stay in press order (id 7 first), regardless of which finger moved
        moved.Should().Be(true);
        previous.Length.Should().Be(2);
        previous[0].X.Should().Be(10);
        previous[1].X.Should().Be(100);
        current[0].X.Should().Be(10);
        current[1].X.Should().Be(120);
        current[1].Y.Should().Be(25);
    }

    [Fact]
    public void moving_an_untracked_pointer_reports_nothing()
    {
        //Arrange
        var tracker = new TouchGestureTracker();
        tracker.Down(1, new ScreenPoint(10, 10));

        //Act
        var moved = tracker.Move(99, new ScreenPoint(50, 50), out var current, out var previous);

        //Assert
        moved.Should().Be(false);
        current.Length.Should().Be(0);
        previous.Length.Should().Be(0);
    }

    [Fact]
    public void lifting_the_last_contact_ends_the_gesture()
    {
        //Arrange
        var tracker = new TouchGestureTracker();
        tracker.Down(1, new ScreenPoint(10, 10));
        tracker.Down(2, new ScreenPoint(90, 10));

        //Assert
        tracker.Up(1).Should().Be(false);
        tracker.Up(2).Should().Be(true);
        tracker.Count.Should().Be(0);
    }

    [Fact]
    public void lifting_an_untracked_pointer_changes_nothing()
    {
        //Arrange
        var tracker = new TouchGestureTracker();
        tracker.Down(1, new ScreenPoint(10, 10));

        //Assert
        tracker.Up(99).Should().Be(false);
        tracker.Count.Should().Be(1);
    }

    [Fact]
    public void a_repeated_down_updates_the_position_without_reordering()
    {
        //Arrange
        var tracker = new TouchGestureTracker();
        tracker.Down(1, new ScreenPoint(10, 10));
        tracker.Down(2, new ScreenPoint(90, 10));

        //Act: a duplicate press for the first finger (a head re-reporting the contact)
        tracker.Down(1, new ScreenPoint(15, 12));
        var snapshot = tracker.Snapshot();

        //Assert
        tracker.Count.Should().Be(2);
        snapshot[0].X.Should().Be(15);
        snapshot[1].X.Should().Be(90);
    }

    [Fact]
    public void clear_forgets_every_contact()
    {
        //Arrange
        var tracker = new TouchGestureTracker();
        tracker.Down(1, new ScreenPoint(10, 10));
        tracker.Down(2, new ScreenPoint(90, 10));

        //Act
        tracker.Clear();

        //Assert
        tracker.Count.Should().Be(0);
        tracker.Down(3, new ScreenPoint(5, 5)).Should().Be(true);
    }

    [Fact]
    public void the_snapshots_feed_pinch_math_that_scales_correctly()
    {
        //Arrange: two fingers 80 apart move to 160 apart - a 2x pinch-out
        var tracker = new TouchGestureTracker();
        tracker.Down(1, new ScreenPoint(100, 100));
        tracker.Down(2, new ScreenPoint(180, 100));
        tracker.Move(1, new ScreenPoint(60, 100), out _, out _);

        //Act
        tracker.Move(2, new ScreenPoint(220, 100), out var current, out var previous);
        var args = new PlotterTouchEventArgs(current, previous);

        //Assert: (220-60)/(180-60) = 1.333...
        args.DeltaScale.X.Should().BeApproximately(160.0 / 120.0, 1e-9);
    }
}
