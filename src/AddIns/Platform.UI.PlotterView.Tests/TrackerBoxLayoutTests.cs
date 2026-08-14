#nullable enable

using CodeBrix.Platform.UI.PlotterView.Rendering;
using CodeBrix.Plotter;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.PlotterView.Tests;

public class TrackerBoxLayoutTests
{
    private static readonly PlotterRect _clientArea = new PlotterRect(0, 0, 400, 300);

    [Fact]
    public void the_box_sits_centered_above_the_anchor()
    {
        //Act: content 100x40, padding 5 -> box 110x50; gap 10 above the anchor
        var box = TrackerBoxLayout.Calculate(
            new ScreenPoint(200, 150), new PlotterSize(100, 40), 5, 10, _clientArea);

        //Assert
        box.Left.Should().Be(145);   //200 - 110/2
        box.Top.Should().Be(90);     //150 - 10 - 50
        box.Width.Should().Be(110);
        box.Height.Should().Be(50);
    }

    [Fact]
    public void near_the_top_edge_the_box_flips_below_the_anchor()
    {
        //Act: anchor at y=30 leaves no room for a 50-high box plus gap above
        var box = TrackerBoxLayout.Calculate(
            new ScreenPoint(200, 30), new PlotterSize(100, 40), 5, 10, _clientArea);

        //Assert
        box.Top.Should().Be(40);     //30 + 10
    }

    [Fact]
    public void the_box_clamps_inside_the_left_and_right_edges()
    {
        //Act
        var atLeft = TrackerBoxLayout.Calculate(
            new ScreenPoint(10, 150), new PlotterSize(100, 40), 5, 10, _clientArea);
        var atRight = TrackerBoxLayout.Calculate(
            new ScreenPoint(395, 150), new PlotterSize(100, 40), 5, 10, _clientArea);

        //Assert
        atLeft.Left.Should().Be(0);
        atRight.Left.Should().Be(290);   //400 - 110
    }

    [Fact]
    public void the_box_clamps_inside_the_bottom_edge_when_flipped()
    {
        //Act: anchor near the top forces the flip below; a tall box then hits the bottom
        var box = TrackerBoxLayout.Calculate(
            new ScreenPoint(200, 20), new PlotterSize(100, 280), 5, 10, _clientArea);

        //Assert: flipped top would be 30, but 30 + 290 > 300, so it clamps (and then pins
        //  back to the top edge because the box is taller than the space below)
        (box.Top + box.Height <= _clientArea.Bottom || box.Top == _clientArea.Top).Should().Be(true);
    }

    [Fact]
    public void a_box_wider_than_the_client_area_pins_to_the_left_edge()
    {
        //Act
        var box = TrackerBoxLayout.Calculate(
            new ScreenPoint(200, 150), new PlotterSize(500, 40), 5, 10, _clientArea);

        //Assert: the clamp order runs right-edge first, then left-edge, so the text start
        //  stays visible
        box.Left.Should().Be(0);
    }
}
