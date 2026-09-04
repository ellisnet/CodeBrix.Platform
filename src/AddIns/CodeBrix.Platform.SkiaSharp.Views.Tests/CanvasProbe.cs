using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.Foundation;
using UwpBuffer = Windows.Storage.Streams.Buffer;

namespace CodeBrix.Platform.SkiaSharp.Views.Tests;

/// <summary>
/// Reads back what a canvas actually presented.
/// </summary>
/// <remarks>
/// <para>
/// <c>SKXamlCanvas</c> presents by setting its <c>Background</c> to an <c>ImageBrush</c> over a
/// <c>WriteableBitmap</c>, so "what the user would see" is the bitmap's pixel buffer. Reading it
/// needs the framework's internal <c>Buffer</c> accessor, which the test assembly is granted in
/// src/Platform.UWP/AssemblyInfo.cs - the same grant the add-in itself has, for the same reason.
/// </para>
/// <para>
/// The buffer is BGRA, premultiplied, one row after another with no padding beyond the width.
/// </para>
/// </remarks>
internal static class CanvasProbe
{
	/// <summary>Lays a canvas out at the given size, the way a parent panel would.</summary>
	internal static void LayOutAt(this SKXamlCanvas canvas, double width, double height)
	{
		canvas.Measure(new Size(width, height));
		canvas.Arrange(new Rect(0, 0, width, height));
	}

	/// <summary>The bitmap the canvas is presenting through, or null before its first paint.</summary>
	internal static WriteableBitmap? PresentedBitmap(this SKXamlCanvas canvas) =>
		(canvas.Background as ImageBrush)?.ImageSource as WriteableBitmap;

	/// <summary>The brush the canvas presents through, or null before its first paint.</summary>
	internal static ImageBrush? PresentedBrush(this SKXamlCanvas canvas) =>
		canvas.Background as ImageBrush;

	/// <summary>The BGRA premultiplied bytes the canvas last presented.</summary>
	internal static byte[] PresentedPixels(this SKXamlCanvas canvas)
	{
		var bitmap = canvas.PresentedBitmap()
			?? throw new InvalidOperationException("The canvas has not presented a bitmap yet.");
		return UwpBuffer.Cast(bitmap.PixelBuffer).Span.ToArray();
	}

	/// <summary>The colour of one presented pixel, unpremultiplied back into an SKColor.</summary>
	/// <param name="canvas">The canvas to read.</param>
	/// <param name="x">Column, in raw device pixels.</param>
	/// <param name="y">Row, in raw device pixels.</param>
	internal static SKColor PresentedPixelAt(this SKXamlCanvas canvas, int x, int y)
	{
		var bitmap = canvas.PresentedBitmap()
			?? throw new InvalidOperationException("The canvas has not presented a bitmap yet.");
		var pixels = canvas.PresentedPixels();
		return PixelAt(pixels, bitmap.PixelWidth, x, y);
	}

	/// <summary>The colour of one pixel in a BGRA premultiplied buffer.</summary>
	/// <param name="pixels">The buffer.</param>
	/// <param name="stridePixels">How many pixels make one row.</param>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	internal static SKColor PixelAt(byte[] pixels, int stridePixels, int x, int y)
	{
		var offset = ((y * stridePixels) + x) * 4;
		var b = pixels[offset];
		var g = pixels[offset + 1];
		var r = pixels[offset + 2];
		var a = pixels[offset + 3];
		return new SKColor(r, g, b, a);
	}

	/// <summary>Fills the whole surface an event handler was given with one colour.</summary>
	internal static void FillWith(this SKPaintSurfaceEventArgs e, SKColor colour) =>
		e.Surface.Canvas.Clear(colour);

	/// <summary>Fills an axis-aligned rectangle on the surface an event handler was given.</summary>
	internal static void FillRect(this SKPaintSurfaceEventArgs e, SKRect rect, SKColor colour)
	{
		using var paint = new SKPaint { Color = colour, Style = SKPaintStyle.Fill, IsAntialias = false };
		e.Surface.Canvas.DrawRect(rect, paint);
	}
}
