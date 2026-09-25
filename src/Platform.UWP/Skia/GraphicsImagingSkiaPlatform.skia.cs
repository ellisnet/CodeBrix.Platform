using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Contracts;
using SkiaSharp;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace CodeBrix.Platform.Skia;

/// <summary>
/// The Skia implementation of <see cref="IGraphicsImagingPlatform"/>: a <see cref="SoftwareBitmap"/> is backed by
/// an <see cref="SKBitmap"/>, and <see cref="BitmapEncoder"/> encodes through <see cref="SKBitmap.Encode(SKEncodedImageFormat, int)"/>.
/// </summary>
internal sealed class GraphicsImagingSkiaPlatform : IGraphicsImagingPlatform
{
	private static readonly Dictionary<Guid, SKEncodedImageFormat> _encoderMap =
		new Dictionary<Guid, SKEncodedImageFormat>()
		{
			{BitmapEncoder.BmpEncoderId, SKEncodedImageFormat.Bmp},
			{BitmapEncoder.GifEncoderId, SKEncodedImageFormat.Gif},
			{BitmapEncoder.JpegEncoderId, SKEncodedImageFormat.Jpeg},
			{BitmapEncoder.PngEncoderId, SKEncodedImageFormat.Png},
			{BitmapEncoder.HeifEncoderId, SKEncodedImageFormat.Heif},
		};

	/// <inheritdoc />
	public object CreateBitmap(BitmapPixelFormat format, int width, int height)
	{
		var info = new SKImageInfo(width, height, ToSKColorType(format));
		return new SKBitmap(info);
	}

	/// <inheritdoc />
	public object CreateBitmap(BitmapPixelFormat format, int width, int height, BitmapAlphaMode alphaMode)
	{
		var info = new SKImageInfo(width, height, ToSKColorType(format), ToSKAlphaType(alphaMode));
		return new SKBitmap(info);
	}

	/// <inheritdoc />
	public object CreateBitmapFromPixels(byte[] pixels, BitmapPixelFormat format, BitmapAlphaMode alphaMode, int width, int height)
	{
		var info = new SKImageInfo(width, height, ToSKColorType(format), ToSKAlphaType(alphaMode));

		// create an empty bitmap
		var destination = new SKBitmap();

		// pin the managed array so that the GC doesn't move it
		var gcHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

		// install the pixels with the color type of the pixel data
		var success = destination.
			InstallPixels(info
			, gcHandle.AddrOfPinnedObject()
			, info.RowBytes
			, (address, context) => ((GCHandle)context).Free(), gcHandle);

		if (!success)
		{
			// Skia runs the release proc itself when InstallPixels fails (it frees the pin); only the empty bitmap is
			// left, and it was never disposed (WPA1 FIXLIST).
			destination.Dispose();
			return null;
		}

		return destination;
	}

	/// <inheritdoc />
	public BitmapPixelFormat GetPixelFormat(object bitmap) => ToBitmapPixelFormat(((SKBitmap)bitmap).ColorType);

	/// <inheritdoc />
	public BitmapAlphaMode GetAlphaMode(object bitmap) => ToBitmapAlphaMode(((SKBitmap)bitmap).AlphaType);

	/// <inheritdoc />
	public int GetPixelWidth(object bitmap) => ((SKBitmap)bitmap).Width;

	/// <inheritdoc />
	public int GetPixelHeight(object bitmap) => ((SKBitmap)bitmap).Height;

	/// <inheritdoc />
	public void CopyPixels(object source, object destination)
	{
		using var canvas = new SKCanvas((SKBitmap)destination);
		canvas.DrawBitmap((SKBitmap)source, new SKPoint(0, 0), SKSamplingOptions.Default);
		canvas.Flush();
	}

	/// <inheritdoc />
	public object CopyBitmap(object source) => ((SKBitmap)source).Copy();

	/// <inheritdoc />
	public void DisposeBitmap(object bitmap) => ((SKBitmap)bitmap).Dispose();

	/// <inheritdoc />
	public bool IsEncoderSupported(Guid encoderId) => _encoderMap.ContainsKey(encoderId);

	/// <inheritdoc />
	public void Encode(object bitmap, Guid encoderId, IRandomAccessStream destination)
	{
		using var data = ((SKBitmap)bitmap)?.Encode(_encoderMap[encoderId], 100);
		data?.SaveTo(destination.AsStream());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static SKColorType ToSKColorType(BitmapPixelFormat format) =>
		format switch
		{
			BitmapPixelFormat.Unknown => SKColorType.Unknown,
			BitmapPixelFormat.Rgba16 => SKColorType.Rgba16161616,
			BitmapPixelFormat.Rgba8 => SKColorType.Rgba8888,
			BitmapPixelFormat.Gray8 => SKColorType.Gray8,
			BitmapPixelFormat.Bgra8 => SKColorType.Bgra8888,
			_ => throw new NotSupportedException(nameof(format))
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static BitmapPixelFormat ToBitmapPixelFormat(SKColorType format) =>
		format switch
		{
			SKColorType.Unknown => BitmapPixelFormat.Unknown,
			SKColorType.Rgba16161616 => BitmapPixelFormat.Rgba16,
			SKColorType.Rgba8888 => BitmapPixelFormat.Rgba8,
			SKColorType.Gray8 => BitmapPixelFormat.Gray8,
			SKColorType.Bgra8888 => BitmapPixelFormat.Bgra8,
			_ => throw new NotSupportedException(nameof(format))
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static SKAlphaType ToSKAlphaType(BitmapAlphaMode alpha) =>
		alpha switch
		{
			BitmapAlphaMode.Ignore => SKAlphaType.Opaque,
			BitmapAlphaMode.Straight => SKAlphaType.Unpremul,
			BitmapAlphaMode.Premultiplied => SKAlphaType.Premul,
			_ => throw new NotSupportedException(nameof(alpha))
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static BitmapAlphaMode ToBitmapAlphaMode(SKAlphaType alpha) =>
		alpha switch
		{
			SKAlphaType.Opaque => BitmapAlphaMode.Ignore,
			SKAlphaType.Unpremul => BitmapAlphaMode.Straight,
			SKAlphaType.Premul => BitmapAlphaMode.Premultiplied,
			_ => throw new NotSupportedException(nameof(alpha))
		};
}
