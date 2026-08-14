#nullable enable

using CodeBrix.Plotter;
using Microsoft.UI.Input;

namespace CodeBrix.Platform.UI.PlotterView.Input;

/// <summary>
/// Maps the framework's <see cref="PointerUpdateKind"/> - the "what changed" of a pointer
/// press - onto the <see cref="PlotterMouseButton"/> the CodeBrix.Plotter controller binds
/// gestures to.
/// </summary>
public static class PointerButtonMapper
{
    /// <summary>
    /// Maps a pointer update kind to the mouse button it presses.
    /// </summary>
    /// <param name="kind">The pointer update kind of a pressed event.</param>
    /// <returns>The pressed button, or <see cref="PlotterMouseButton.None"/> when the update
    /// is not a button press (releases, wheel, and other updates).</returns>
    public static PlotterMouseButton ToMouseButton(PointerUpdateKind kind)
    {
        return kind switch
        {
            PointerUpdateKind.LeftButtonPressed => PlotterMouseButton.Left,
            PointerUpdateKind.MiddleButtonPressed => PlotterMouseButton.Middle,
            PointerUpdateKind.RightButtonPressed => PlotterMouseButton.Right,
            PointerUpdateKind.XButton1Pressed => PlotterMouseButton.XButton1,
            PointerUpdateKind.XButton2Pressed => PlotterMouseButton.XButton2,
            _ => PlotterMouseButton.None,
        };
    }
}
