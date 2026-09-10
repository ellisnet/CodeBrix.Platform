using System;
using System.IO;
using SkiaSharp;
using Windows.UI;

namespace CodeBrix.Platform.UI.Core.UIReqs.Support;

/// <summary>
/// The picture the Image scenarios draw: a tiny two-colour PNG, made at run time rather than
/// carried as a file, so that what the scenario expects to see on the panel and what the file
/// holds cannot drift apart. Its left half is one colour and its right half another, which is
/// what makes a Stretch mode visible - Fill spreads the halves across the whole box, None
/// leaves them at their own size, and UniformToFill crops the halves rather than shrinking them.
/// </summary>
public static class TestImage
{
	/// <summary>The picture's width in pixels.</summary>
	public const int PictureWidth = 40;

	/// <summary>The picture's height in pixels.</summary>
	public const int PictureHeight = 20;

	/// <summary>Encodes the two-colour picture as a PNG.</summary>
	/// <param name="left">The colour of the picture's left half.</param>
	/// <param name="right">The colour of the picture's right half.</param>
	/// <returns>The PNG bytes.</returns>
	public static byte[] EncodePng(Color left, Color right)
	{
		using var bitmap = new SKBitmap(PictureWidth, PictureHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
		using (var canvas = new SKCanvas(bitmap))
		{
			using var leftPaint = new SKPaint { Color = ToSkia(left) };
			using var rightPaint = new SKPaint { Color = ToSkia(right) };
			canvas.DrawRect(SKRect.Create(0, 0, PictureWidth / 2, PictureHeight), leftPaint);
			canvas.DrawRect(SKRect.Create(PictureWidth / 2, 0, PictureWidth / 2, PictureHeight), rightPaint);
		}

		using var image = SKImage.FromBitmap(bitmap);
		using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
			?? throw new InvalidOperationException("The two-colour picture could not be encoded as a PNG.");

		return encoded.ToArray();
	}

	/// <summary>The two-colour picture as a stream a BitmapImage can be given.</summary>
	/// <param name="left">The colour of the picture's left half.</param>
	/// <param name="right">The colour of the picture's right half.</param>
	/// <returns>A stream over the PNG bytes.</returns>
	public static Stream OpenPng(Color left, Color right) => new MemoryStream(EncodePng(left, right), false);

	private static SKColor ToSkia(Color color) => new(color.R, color.G, color.B, color.A);
}
