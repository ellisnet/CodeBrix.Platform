#nullable enable

using System;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

//was previously: ICSharpCode.AvalonEdit/Utils/PixelSnapHelpers.cs in the AvalonEdit repo (MIT).
//The pure alignment/rounding math is unchanged. GetPixelSize is rewritten on the render scale:
//upstream read WPF's PresentationSource device transform from any Visual; here the pixel size
//comes from the TextView's render surface scale factor (1/scale device-independent pixels per
//device pixel), so the parameter is the TextView instead of a Visual.

/// <summary>
/// Contains static helper methods for aligning stuff on a whole number of pixels.
/// </summary>
public static class PixelSnapHelpers
{
	/// <summary>
	/// Gets the size of one device pixel, in device-independent pixels, on the display the
	/// text view renders to.
	/// </summary>
	public static Size GetPixelSize(TextView textView)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));
		double scale = textView.RenderScale;
		if (scale <= 0)
			scale = 1;
		return new Size(1.0 / scale, 1.0 / scale);
	}

	/// <summary>
	/// Aligns <paramref name="value"/> on the next middle of a pixel.
	/// </summary>
	/// <param name="value">The value that should be aligned</param>
	/// <param name="pixelSize">The size of one pixel</param>
	public static double PixelAlign(double value, double pixelSize)
	{
		// 0 -> 0.5
		// 0.1 -> 0.5
		// 0.5 -> 0.5
		// 0.9 -> 0.5
		// 1 -> 1.5
		return pixelSize * (Math.Round((value / pixelSize) + 0.5, MidpointRounding.AwayFromZero) - 0.5);
	}

	/// <summary>
	/// Aligns the borders of rect on the middles of pixels.
	/// </summary>
	public static Rect PixelAlign(Rect rect, Size pixelSize)
	{
		rect.X = PixelAlign(rect.X, pixelSize.Width);
		rect.Y = PixelAlign(rect.Y, pixelSize.Height);
		rect.Width = Round(rect.Width, pixelSize.Width);
		rect.Height = Round(rect.Height, pixelSize.Height);
		return rect;
	}

	/// <summary>
	/// Rounds <paramref name="point"/> to whole number of pixels.
	/// </summary>
	public static Point Round(Point point, Size pixelSize)
	{
		return new Point(Round(point.X, pixelSize.Width), Round(point.Y, pixelSize.Height));
	}

	/// <summary>
	/// Rounds val to whole number of pixels.
	/// </summary>
	public static Rect Round(Rect rect, Size pixelSize)
	{
		return new Rect(Round(rect.X, pixelSize.Width), Round(rect.Y, pixelSize.Height),
						Round(rect.Width, pixelSize.Width), Round(rect.Height, pixelSize.Height));
	}

	/// <summary>
	/// Rounds <paramref name="value"/> to a whole number of pixels.
	/// </summary>
	public static double Round(double value, double pixelSize)
	{
		return pixelSize * Math.Round(value / pixelSize, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// Rounds <paramref name="value"/> to an whole odd number of pixels.
	/// </summary>
	public static double RoundToOdd(double value, double pixelSize)
	{
		return Round(value - pixelSize, pixelSize * 2) + pixelSize;
	}
}
