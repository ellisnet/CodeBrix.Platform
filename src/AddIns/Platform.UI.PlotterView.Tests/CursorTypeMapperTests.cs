#nullable enable

using CodeBrix.Platform.UI.PlotterView.Rendering;
using CodeBrix.Plotter;
using Microsoft.UI.Input;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.PlotterView.Tests;

public class CursorTypeMapperTests
{
    [Fact]
    public void manipulator_cursors_map_to_their_shapes()
    {
        //Assert
        CursorTypeMapper.ToCursorShape(CursorType.Pan).Should().Be(InputSystemCursorShape.SizeAll);
        CursorTypeMapper.ToCursorShape(CursorType.ZoomRectangle).Should().Be(InputSystemCursorShape.Cross);
        CursorTypeMapper.ToCursorShape(CursorType.ZoomHorizontal).Should().Be(InputSystemCursorShape.SizeWestEast);
        CursorTypeMapper.ToCursorShape(CursorType.ZoomVertical).Should().Be(InputSystemCursorShape.SizeNorthSouth);
    }

    [Fact]
    public void the_default_cursor_maps_to_no_override()
    {
        //Assert
        CursorTypeMapper.ToCursorShape(CursorType.Default).Should().BeNull();
    }
}
