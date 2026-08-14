#nullable enable

using CodeBrix.Platform.UI.PlotterView.Input;
using CodeBrix.Plotter;
using SilverAssertions;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.UI.PlotterView.Tests;

public class VirtualKeyMapperTests
{
    [Fact]
    public void letters_map_to_the_letter_range()
    {
        //Assert
        VirtualKeyMapper.ToPlotterKey(VirtualKey.A).Should().Be(PlotterKey.A);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.M).Should().Be(PlotterKey.M);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Z).Should().Be(PlotterKey.Z);
    }

    [Fact]
    public void digit_row_maps_to_d_keys()
    {
        //Assert
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Number0).Should().Be(PlotterKey.D0);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Number5).Should().Be(PlotterKey.D5);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Number9).Should().Be(PlotterKey.D9);
    }

    [Fact]
    public void numpad_maps_to_numpad_keys()
    {
        //Assert
        VirtualKeyMapper.ToPlotterKey(VirtualKey.NumberPad0).Should().Be(PlotterKey.NumPad0);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.NumberPad9).Should().Be(PlotterKey.NumPad9);
    }

    [Fact]
    public void function_keys_map_to_the_f_range()
    {
        //Assert
        VirtualKeyMapper.ToPlotterKey(VirtualKey.F1).Should().Be(PlotterKey.F1);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.F6).Should().Be(PlotterKey.F6);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.F12).Should().Be(PlotterKey.F12);
    }

    [Fact]
    public void navigation_and_editing_keys_map_by_name()
    {
        //Assert
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Up).Should().Be(PlotterKey.Up);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Down).Should().Be(PlotterKey.Down);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Left).Should().Be(PlotterKey.Left);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Right).Should().Be(PlotterKey.Right);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Home).Should().Be(PlotterKey.Home);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.End).Should().Be(PlotterKey.End);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.PageUp).Should().Be(PlotterKey.PageUp);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.PageDown).Should().Be(PlotterKey.PageDown);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Insert).Should().Be(PlotterKey.Insert);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Delete).Should().Be(PlotterKey.Delete);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Back).Should().Be(PlotterKey.Backspace);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Enter).Should().Be(PlotterKey.Enter);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Escape).Should().Be(PlotterKey.Escape);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Space).Should().Be(PlotterKey.Space);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Tab).Should().Be(PlotterKey.Tab);
    }

    [Fact]
    public void arithmetic_keys_map_for_the_zoom_bindings()
    {
        //The default controller binds Add/Subtract (and their Ctrl chords) to zoom in/out

        //Assert
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Add).Should().Be(PlotterKey.Add);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Subtract).Should().Be(PlotterKey.Subtract);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Multiply).Should().Be(PlotterKey.Multiply);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Divide).Should().Be(PlotterKey.Divide);
    }

    [Fact]
    public void modifier_and_unmapped_keys_are_unknown()
    {
        //Modifiers travel in PlotterInputEventArgs.ModifierKeys, never as keys

        //Assert
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Shift).Should().Be(PlotterKey.Unknown);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Control).Should().Be(PlotterKey.Unknown);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Menu).Should().Be(PlotterKey.Unknown);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.CapitalLock).Should().Be(PlotterKey.Unknown);
        VirtualKeyMapper.ToPlotterKey(VirtualKey.Print).Should().Be(PlotterKey.Unknown);
    }
}
