#nullable enable

using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

#if false
using _UIImage = UIKit.UIImage;
#elif false
using Android.Graphics;
#endif

namespace CodeBrix.Platform.UI.Xaml.Media; //Was previously: Uno.UI.Xaml.Media

/// <summary>
/// Represents the raw data of an **opened** image source
/// </summary>
internal partial struct ImageData
{
	public static ImageData FromBytes(byte[] data) => new(data);

	private ImageData(byte[] data)
	{
		Kind = ImageDataKind.ByteArray;
		ByteArray = data ?? throw new ArgumentNullException(nameof(data));
	}

	public static ImageData FromError(Exception exception) => new(exception);

	private ImageData(Exception exception)
	{
		Kind = ImageDataKind.Error;
		Error = exception ?? throw new ArgumentNullException(nameof(exception));
	}

#if false
	public static ImageData FromNative(_UIImage uiImage) => new ImageData(uiImage);

	private ImageData(_UIImage uiImage)
	{
		Kind = ImageDataKind.NativeImage;
		NativeImage = uiImage ?? throw new ArgumentNullException(nameof(uiImage));
	}
#elif __SKIA__
	public static ImageData FromCompositionSurface(SkiaCompositionSurface compositionSurface) => new(compositionSurface);

	private ImageData(SkiaCompositionSurface compositionSurface)
	{
		Kind = ImageDataKind.CompositionSurface;
		CompositionSurface = compositionSurface;
	}
#elif false
	public static ImageData FromDataUri(string dataUri) => new ImageData(ImageDataKind.DataUri, dataUri);

	public static ImageData FromUrl(Uri url, ImageSource source) => new ImageData(url.ToString(), source);

	public static ImageData FromUrl(string url, ImageSource source) => new ImageData(url, source);

	private ImageData(ImageDataKind kind, string value)
	{
		Kind = kind;
		Value = value;
	}

	private ImageData(string url, ImageSource source)
	{
		Kind = ImageDataKind.Url;
		Value = url;
		Source = source;
	}
#elif false
	public static ImageData FromBitmap(Bitmap? bitmap)
	{
		if (bitmap is null)
		{
			return ImageData.Empty;
		}

		return new ImageData(bitmap);
	}

	private ImageData(Bitmap bitmap)
	{
		Kind = ImageDataKind.NativeImage;
		Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
	}
#endif

	public static ImageData Empty { get; }

	public bool HasData => Kind != ImageDataKind.Empty && Kind != ImageDataKind.Error;

	public ImageDataKind Kind { get; }

	public Exception? Error { get; } = null;

	public byte[]? ByteArray { get; } = null;

#if false
	public _UIImage? NativeImage { get; } = null;
#elif __SKIA__
	public SkiaCompositionSurface? CompositionSurface { get; } = null;
#elif false
	internal ImageSource? Source { get; } = null;

	public string? Value { get; } = null;
#elif false
	public Bitmap? Bitmap { get; } = null;
#endif

	public override string ToString() =>
		Kind switch
		{
			ImageDataKind.Empty => "Empty",
			ImageDataKind.Error => $"Error[{Error}]",
			ImageDataKind.ByteArray => $"Byte array: Length {ByteArray?.Length ?? -1}",
#if false
			ImageDataKind.NativeImage => $"Native UIImage: {NativeImage}",
#endif
#if __SKIA__
			ImageDataKind.CompositionSurface => $"CompositionSurface: {CompositionSurface}",
#endif
#if false
			ImageDataKind.DataUri => $"DataUri: {Value}",
			ImageDataKind.Url => $"Url: {Value}, Source: {Source}",
#endif
			_ => $"{Kind}"
		};
}
