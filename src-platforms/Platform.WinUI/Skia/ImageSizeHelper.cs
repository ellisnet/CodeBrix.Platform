using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

namespace CodeBrix.Platform.WinUI.Skia;

/// <summary>
/// Stretch-aware scaling math shared by the CodeBrix.Platform.WinUI Skia-rendered
/// controls (SVG images, Lottie animations). Functionally identical to the
/// ImageSizeHelper used by the CodeBrix.Platform (Uno-based) rendering pipeline,
/// so both families compute the same scale factors for the same inputs.
/// </summary>
public static class ImageSizeHelper
{
    /// <summary>
    /// Computes the (x, y) scale factors that map <paramref name="sourceSize"/> onto
    /// <paramref name="destinationSize"/> for the given <paramref name="stretch"/> mode.
    /// Infinite destination dimensions fall back to the finite axis (or 1.0 when both
    /// are infinite).
    /// </summary>
    public static (double x, double y) BuildScale(Stretch stretch, Size destinationSize, Size sourceSize)
    {
        if (stretch != Stretch.None)
        {
            var scale = (
                x: destinationSize.Width / sourceSize.Width,
                y: destinationSize.Height / sourceSize.Height
            );

            if (double.IsInfinity(scale.x))
            {
                if (double.IsInfinity(scale.y))
                {
                    return (1.0d, 1.0d);
                }

                scale.x = scale.y;
            }
            else if (double.IsInfinity(scale.y))
            {
                scale.y = scale.x;
            }

            switch (stretch)
            {
                case Stretch.UniformToFill:
                    var max = Math.Max(scale.x, scale.y);
                    scale = (max, max);
                    break;

                case Stretch.Uniform:
                    var min = Math.Min(scale.x, scale.y);
                    scale = (min, min);
                    break;
            }

            var scaleX = double.IsNaN(scale.x) ? 1.0d : scale.x;
            var scaleY = double.IsNaN(scale.y) ? 1.0d : scale.y;

            return (scaleX, scaleY);
        }
        else
        {
            return (1.0d, 1.0d);
        }
    }

    /// <summary>
    /// Returns the size <paramref name="measuredSize"/> should occupy within
    /// <paramref name="availableSize"/> under the given <paramref name="stretch"/> mode,
    /// never exceeding the available size on a finite axis.
    /// </summary>
    public static Size AdjustSize(Stretch stretch, Size availableSize, Size measuredSize)
    {
        var (x, y) = BuildScale(stretch, availableSize, measuredSize);
        var adjusted = new Size(measuredSize.Width * x, measuredSize.Height * y);

        return new Size(
            double.IsInfinity(availableSize.Width) ? adjusted.Width : Math.Min(adjusted.Width, availableSize.Width),
            double.IsInfinity(availableSize.Height) ? adjusted.Height : Math.Min(adjusted.Height, availableSize.Height));
    }
}
