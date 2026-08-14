#nullable enable

using CodeBrix.Plotter;
using Microsoft.UI.Input;

namespace CodeBrix.Platform.UI.PlotterView.Rendering;

/// <summary>
/// Maps the CodeBrix.Plotter <see cref="CursorType"/> a manipulator asks for onto the
/// framework's <see cref="InputSystemCursorShape"/>.
/// </summary>
public static class CursorTypeMapper
{
    /// <summary>
    /// Maps a plotter cursor type to a cursor shape.
    /// </summary>
    /// <param name="cursorType">The plotter cursor type.</param>
    /// <returns>The cursor shape, or <c>null</c> for <see cref="CursorType.Default"/> -
    /// meaning "no cursor override", so the element falls back to the ambient cursor.</returns>
    public static InputSystemCursorShape? ToCursorShape(CursorType cursorType)
    {
        return cursorType switch
        {
            CursorType.Pan => InputSystemCursorShape.SizeAll,
            CursorType.ZoomRectangle => InputSystemCursorShape.Cross,
            CursorType.ZoomHorizontal => InputSystemCursorShape.SizeWestEast,
            CursorType.ZoomVertical => InputSystemCursorShape.SizeNorthSouth,
            _ => null,
        };
    }
}
