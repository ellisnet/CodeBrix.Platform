#nullable enable

using CodeBrix.Plotter;
using Windows.System;

namespace CodeBrix.Platform.UI.PlotterView.Input;

/// <summary>
/// Maps the framework's <see cref="VirtualKey"/> values onto the
/// <see cref="PlotterKey"/> values the CodeBrix.Plotter controller understands.
/// </summary>
public static class VirtualKeyMapper
{
    /// <summary>
    /// Maps a virtual key to the corresponding plotter key.
    /// </summary>
    /// <param name="key">The framework key.</param>
    /// <returns>The plotter key, or <see cref="PlotterKey.Unknown"/> when the key has no
    /// plotter equivalent (modifier keys included - the controller receives those through
    /// <see cref="PlotterInputEventArgs.ModifierKeys"/> instead).</returns>
    public static PlotterKey ToPlotterKey(VirtualKey key)
    {
        //The contiguous ranges first: letters, digit row, and number pad
        if (key >= VirtualKey.A && key <= VirtualKey.Z)
        {
            return PlotterKey.A + (key - VirtualKey.A);
        }

        if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
        {
            return PlotterKey.D0 + (key - VirtualKey.Number0);
        }

        if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
        {
            return PlotterKey.NumPad0 + (key - VirtualKey.NumberPad0);
        }

        if (key >= VirtualKey.F1 && key <= VirtualKey.F12)
        {
            return PlotterKey.F1 + (key - VirtualKey.F1);
        }

        return key switch
        {
            VirtualKey.Space => PlotterKey.Space,
            VirtualKey.Enter => PlotterKey.Enter,
            VirtualKey.Escape => PlotterKey.Escape,
            VirtualKey.Tab => PlotterKey.Tab,
            VirtualKey.Back => PlotterKey.Backspace,
            VirtualKey.Insert => PlotterKey.Insert,
            VirtualKey.Delete => PlotterKey.Delete,
            VirtualKey.Home => PlotterKey.Home,
            VirtualKey.End => PlotterKey.End,
            VirtualKey.Up => PlotterKey.Up,
            VirtualKey.Down => PlotterKey.Down,
            VirtualKey.Left => PlotterKey.Left,
            VirtualKey.Right => PlotterKey.Right,
            VirtualKey.PageUp => PlotterKey.PageUp,
            VirtualKey.PageDown => PlotterKey.PageDown,
            VirtualKey.Add => PlotterKey.Add,
            VirtualKey.Subtract => PlotterKey.Subtract,
            VirtualKey.Multiply => PlotterKey.Multiply,
            VirtualKey.Divide => PlotterKey.Divide,
            _ => PlotterKey.Unknown,
        };
    }
}
