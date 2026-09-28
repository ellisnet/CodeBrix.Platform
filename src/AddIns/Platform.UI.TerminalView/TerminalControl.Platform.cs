#nullable enable

using System;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.TerminalView;

//WPE1-18: the platform-facing caret seam. A platform whose soft keyboard pans the window to the focused native view's
//  rectangle lays its keyboard focus view on the terminal's cursor cell; these two members are how it asks where that
//  cell is and learns when it moved. Internal, reached through InternalsVisibleTo (AssemblyInfo.cs grants the Android
//  and Mobile TerminalView assemblies). The Skia heads do not use it.

public sealed partial class TerminalControl
{
    private EventHandler? _caretRectChangedForPlatform;

    /// <summary>
    /// The cursor cell's rectangle in this control's own coordinates (DIPs): the cell the cursor block is painted in,
    /// offset by where the terminal surface sits in the control. <see cref="Rect.Empty"/> (<see cref="Rect.IsEmpty"/>
    /// is true) when the cursor is hidden by the hosted application or its line is scrolled out of view.
    /// </summary>
    /// <remarks>Implementers: Android, Mobile. Platform (Skia): not used. Call on the UI thread.</remarks>
    /// <returns>The caret rectangle, or <see cref="Rect.Empty"/> when there is no visible caret.</returns>
    internal Rect GetCaretRectForPlatform()
    {
        var cell = _renderer.GetCaretRect();
        if (cell.IsEmpty) { return Rect.Empty; }

        //The surface is the template root's first column, so its offset is (0, 0) today; reading it keeps the rectangle
        //  right if the template ever places the surface elsewhere.
        var offset = _canvas.ActualOffset;
        return new Rect(offset.X + cell.Left, offset.Y + cell.Top, cell.Width, cell.Height);
    }

    /// <summary>
    /// Raised on the UI thread after the value of <see cref="GetCaretRectForPlatform"/> changed: the cursor moved (fed
    /// output, a reset), was shown or hidden, the view scrolled (scroll bar, wheel, keys, typing snapping back to the
    /// live tail), the grid was refitted to a new size, or the terminal font changed. The first change after the first
    /// handler is added is always reported. While no handler is attached the control computes nothing extra.
    /// </summary>
    /// <remarks>Implementers: Android, Mobile. Platform (Skia): not used.</remarks>
    internal event EventHandler? CaretRectChangedForPlatform
    {
        add
        {
            if (_caretRectChangedForPlatform is null) { _renderer.CaretRectChanged += OnRendererCaretRectChanged; }
            _caretRectChangedForPlatform += value;
        }
        remove
        {
            _caretRectChangedForPlatform -= value;
            if (_caretRectChangedForPlatform is null) { _renderer.CaretRectChanged -= OnRendererCaretRectChanged; }
        }
    }

    private void OnRendererCaretRectChanged() => _caretRectChangedForPlatform?.Invoke(this, EventArgs.Empty);
}
