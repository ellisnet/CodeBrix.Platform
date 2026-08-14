#nullable enable

using CodeBrix.Platform.UI.PlotterView.Input;
using CodeBrix.Plotter;
using Microsoft.UI.Input;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.PlotterView.Tests;

public class PointerButtonMapperTests
{
    [Fact]
    public void pressed_kinds_map_to_their_buttons()
    {
        //Assert
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.LeftButtonPressed).Should().Be(PlotterMouseButton.Left);
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.MiddleButtonPressed).Should().Be(PlotterMouseButton.Middle);
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.RightButtonPressed).Should().Be(PlotterMouseButton.Right);
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.XButton1Pressed).Should().Be(PlotterMouseButton.XButton1);
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.XButton2Pressed).Should().Be(PlotterMouseButton.XButton2);
    }

    [Fact]
    public void non_press_updates_map_to_none()
    {
        //Assert
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.Other).Should().Be(PlotterMouseButton.None);
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.LeftButtonReleased).Should().Be(PlotterMouseButton.None);
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.MiddleButtonReleased).Should().Be(PlotterMouseButton.None);
        PointerButtonMapper.ToMouseButton(PointerUpdateKind.RightButtonReleased).Should().Be(PlotterMouseButton.None);
    }
}
