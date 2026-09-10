using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.TestTarget;

/// <summary>
/// One immutable frame captured from the test target's panel: BGRA8888
/// premultiplied pixels, top-down rows, tightly packed. A frame is a snapshot —
/// it is never rewritten — so it can be held, compared with another frame, and
/// saved long after the panel has moved on.
/// </summary>
public sealed class TestFrame
{
	private readonly byte[] _pixels;

	internal TestFrame(int width, int height, long sequence, long renderGeneration, byte[] pixels)
	{
		Width = width;
		Height = height;
		Sequence = sequence;
		RenderGeneration = renderGeneration;
		_pixels = pixels;
	}

	/// <summary>The frame's width, in device pixels.</summary>
	public int Width { get; }

	/// <summary>The frame's height, in device pixels.</summary>
	public int Height { get; }

	/// <summary>
	/// The frame's sequence number. Sequence numbers increase by one per
	/// published frame, so a larger number is a later frame.
	/// </summary>
	public long Sequence { get; }

	/// <summary>
	/// The invalidation generation this frame's rendering started from.
	/// Generations only grow, and a frame whose generation is <c>g</c> or larger
	/// was drawn entirely after invalidation <c>g</c> was requested — so it shows
	/// everything that had already been applied when that request was made. A
	/// frame that was still in flight when the request was made carries a smaller
	/// generation, which is what makes a stale frame recognizable.
	/// </summary>
	public long RenderGeneration { get; }

	/// <summary>
	/// The color of one pixel, un-premultiplied so that what is returned is the
	/// color as authored rather than as composited.
	/// </summary>
	/// <param name="x">The pixel's x coordinate, in device pixels.</param>
	/// <param name="y">The pixel's y coordinate, in device pixels.</param>
	/// <returns>The pixel's color.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The coordinates are outside the frame.</exception>
	public SKColor GetPixel(int x, int y)
	{
		if (x < 0 || x >= Width)
		{
			throw new ArgumentOutOfRangeException(nameof(x), x,
				$"The frame is {Width} x {Height} device pixels.");
		}
		if (y < 0 || y >= Height)
		{
			throw new ArgumentOutOfRangeException(nameof(y), y,
				$"The frame is {Width} x {Height} device pixels.");
		}

		var offset = (y * Width + x) * 4;
		var blue = _pixels[offset];
		var green = _pixels[offset + 1];
		var red = _pixels[offset + 2];
		var alpha = _pixels[offset + 3];
		if (alpha == 0)
		{
			return new SKColor(0, 0, 0, 0);
		}
		if (alpha < 255)
		{
			red = Unpremultiply(red, alpha);
			green = Unpremultiply(green, alpha);
			blue = Unpremultiply(blue, alpha);
		}
		return new SKColor(red, green, blue, alpha);
	}

	/// <summary>
	/// Copies the frame into a new <see cref="SKBitmap"/>. The caller owns it.
	/// </summary>
	/// <returns>A bitmap holding this frame's pixels.</returns>
	public SKBitmap ToBitmap()
	{
		var bitmap = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul));
		try
		{
			Marshal.Copy(_pixels, 0, bitmap.GetPixels(), _pixels.Length);
		}
		catch
		{
			bitmap.Dispose();
			throw;
		}
		return bitmap;
	}

	/// <summary>
	/// Saves the frame as a PNG, creating the containing directory when it does
	/// not exist yet.
	/// </summary>
	/// <param name="path">The file to write.</param>
	public void SavePng(string path)
	{
		ArgumentException.ThrowIfNullOrEmpty(path);

		var directory = Path.GetDirectoryName(Path.GetFullPath(path));
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		using var bitmap = ToBitmap();
		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		using var stream = File.Create(path);
		data.SaveTo(stream);
	}

	private static byte Unpremultiply(byte channel, byte alpha)
	{
		var value = (channel * 255 + alpha / 2) / alpha;
		return (byte) (value > 255 ? 255 : value);
	}
}
