#nullable enable

using CodeBrix.Plotter;

namespace CodeBrix.Platform.UI.PlotterView.Rendering;

/// <summary>
/// Places the tracker's text box relative to the tracked point: centered above it with a small
/// gap, flipped below when there is no room above, and always clamped into the client area.
/// Pure geometry, so the placement rules are unit-testable.
/// </summary>
public static class TrackerBoxLayout
{
    /// <summary>
    /// Calculates the tracker box rectangle.
    /// </summary>
    /// <param name="anchor">The tracked point, in client coordinates.</param>
    /// <param name="contentSize">The measured size of the tracker text.</param>
    /// <param name="padding">The padding added around the text on every side.</param>
    /// <param name="gap">The distance kept between the anchor point and the box edge.</param>
    /// <param name="clientArea">The area the box must stay inside.</param>
    /// <returns>The box rectangle; the text's top-left corner is the box corner inset by
    /// <paramref name="padding"/>.</returns>
    public static PlotterRect Calculate(
        ScreenPoint anchor, PlotterSize contentSize, double padding, double gap, PlotterRect clientArea)
    {
        var width = contentSize.Width + (2 * padding);
        var height = contentSize.Height + (2 * padding);

        var left = anchor.X - (width / 2);
        var top = anchor.Y - gap - height;

        //No room above the point: flip below it
        if (top < clientArea.Top)
        {
            top = anchor.Y + gap;
        }

        //Clamp into the client area; when the box is larger than the area, pin to its origin
        //  edge so at least the text's start stays readable
        if (left + width > clientArea.Right)
        {
            left = clientArea.Right - width;
        }

        if (left < clientArea.Left)
        {
            left = clientArea.Left;
        }

        if (top + height > clientArea.Bottom)
        {
            top = clientArea.Bottom - height;
        }

        if (top < clientArea.Top)
        {
            top = clientArea.Top;
        }

        return new PlotterRect(left, top, width, height);
    }
}
