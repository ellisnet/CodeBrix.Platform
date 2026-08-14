#nullable enable

using CodeBrix.Platform.UI.PlotterView.Input;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.PlotterView.Tests;

public class ClickCounterTests
{
    [Fact]
    public void first_press_is_a_single_click()
    {
        //Arrange
        var counter = new ClickCounter();

        //Assert
        counter.Register(1000, 50, 50).Should().Be(1);
    }

    [Fact]
    public void quick_presses_at_the_same_spot_count_up()
    {
        //Arrange
        var counter = new ClickCounter();

        //Act + Assert
        counter.Register(1000, 50, 50).Should().Be(1);
        counter.Register(1200, 51, 50).Should().Be(2);
        counter.Register(1400, 50, 51).Should().Be(3);
    }

    [Fact]
    public void a_slow_second_press_starts_over()
    {
        //Arrange
        var counter = new ClickCounter();
        counter.Register(1000, 50, 50);

        //Assert
        counter.Register(1000 + counter.MaximumIntervalMilliseconds + 1, 50, 50).Should().Be(1);
    }

    [Fact]
    public void a_press_far_from_the_run_starts_over()
    {
        //Arrange
        var counter = new ClickCounter();
        counter.Register(1000, 50, 50);

        //Assert
        counter.Register(1100, 50 + counter.MaximumDistance + 1, 50).Should().Be(1);
    }

    [Fact]
    public void drift_is_measured_from_the_first_press_of_the_run()
    {
        //Arrange: each press drifts within tolerance of the previous one, but the third has
        //  drifted beyond tolerance of the FIRST - so it starts a new run
        var counter = new ClickCounter { MaximumDistance = 4 };
        counter.Register(1000, 50, 50);
        counter.Register(1100, 53, 50);

        //Assert
        counter.Register(1200, 56, 50).Should().Be(1);
    }

    [Fact]
    public void the_interval_applies_between_consecutive_presses()
    {
        //Arrange: three presses each 400ms apart - the third is 800ms after the first, but
        //  only the gap to the PREVIOUS press matters
        var counter = new ClickCounter { MaximumIntervalMilliseconds = 500 };
        counter.Register(1000, 50, 50);
        counter.Register(1400, 50, 50);

        //Assert
        counter.Register(1800, 50, 50).Should().Be(3);
    }

    [Fact]
    public void reset_forgets_the_run()
    {
        //Arrange
        var counter = new ClickCounter();
        counter.Register(1000, 50, 50);

        //Act
        counter.Reset();

        //Assert
        counter.Register(1100, 50, 50).Should().Be(1);
    }
}
