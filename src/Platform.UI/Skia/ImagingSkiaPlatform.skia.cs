#nullable enable

using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Helpers;
using CodeBrix.Platform.UI.Composition.Skia;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics.Display;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="IImagingPlatform"/>: decodes with <see cref="SKCodec"/> and
/// <see cref="SKImage"/>, renders elements into an <see cref="SKSurface"/>, and keeps decoded images as
/// <see cref="SKImage"/>s inside the composition surfaces.
/// </summary>
internal sealed partial class ImagingSkiaPlatform : IImagingPlatform
{
	private const int _bitsPerPixel = 32;
	private const int _bitsPerComponent = 8;
	private const int _bytesPerPixel = _bitsPerPixel / _bitsPerComponent;

	private static MethodInfo? _fromPictureMethod;

	/// <inheritdoc />
	public async Task<ImageData?> TryDecodeWithBrowserAsync(byte[] buffer)
	{
		var decodedBufferObject = await NativeMethods.LoadFromArray(buffer);

		if (decodedBufferObject.GetPropertyAsString("error") is { } errorMessage)
		{
			typeof(ImageSourceHelpers).LogError()?.Error($"Failed to load image with the browser Canvas API. Falling back to SKCodec-based loading/decoding: {errorMessage}");
			return null;
		}
		else
		{
			var width = decodedBufferObject.GetPropertyAsInt32("width");
			var height = decodedBufferObject.GetPropertyAsInt32("height");

			if (width == 0 || height == 0)
			{
				return ImageData.Empty;
			}

			var bytes = decodedBufferObject.GetPropertyAsByteArray("bytes");
			return FromBrowserPixels(bytes, width, height);
		}
	}

	private static unsafe ImageData FromBrowserPixels(byte[]? bytes, int width, int height)
	{
		SKImage image;
		var gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		fixed (void* ptr = bytes)
		{
			try
			{
				image = SKImage.FromPixels(new SKPixmap(new SKImageInfo(width, height, SKColorType.Rgba8888), new IntPtr(ptr)), static (_, gcHandle) =>
				{
					((GCHandle)gcHandle).Free();
				}, gcHandle);
				if (image == null)
				{
					throw new InvalidOperationException($"{nameof(SKImage)}.{nameof(SKImage.FromPixels)} returned null.");
				}
				return ImageData.FromCompositionSurface(CreateSurface(image));
			}
			catch (Exception e)
			{
				gcHandle.Free();
				return ImageData.FromError(e);
			}
		}
	}

	/// <inheritdoc />
	public (int Width, int Height) GetEncodedImageSize(Stream stream)
	{
		using var codec = SKCodec.Create(stream);
		var info = codec.Info;
		return (info.Width, info.Height);
	}

	/// <inheritdoc />
	public unsafe void DecodeToBgra8(Stream encodedImage, Span<byte> destination, int rowBytes)
	{
		using var img = SKImage.FromEncodedData(encodedImage);
		var info = img.Info;

		fixed (byte* data = &MemoryMarshal.GetReference(destination))
		{
			img.ReadPixels(info.WithColorType(SKColorType.Bgra8888), (nint)data, rowBytes);
		}
	}

	/// <inheritdoc />
	public ImageData CreateImageFromBgra8Premul(IntPtr pixels, int width, int height)
	{
		try
		{
			// Note: We use the FromPixelCopy which will create a clone of the buffer, so we are ready to be re-used to render another UIElement.
			// (It's needed also if we swapped the buffer since we are not maintaining a ref on the swappedBuffer)
			var bytesPerRow = width * _bytesPerPixel;
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			var image = SKImage.FromPixelCopy(info, pixels, bytesPerRow);

			return ImageData.FromCompositionSurface(CreateSurface(image));
		}
		catch (Exception error)
		{
			return ImageData.FromError(error);
		}
	}

	/// <inheritdoc />
	public (int ByteCount, int Width, int Height) RenderToBgra8Premul(UIElement element, ref RenderTargetBitmap.UnmanagedArrayOfBytes? buffer, Size? scaledSize)
	{
		var compositor = Compositor.GetSharedCompositor();

		bool? previousCompMode = compositor.IsSoftwareRenderer;
		compositor.IsSoftwareRenderer = true;

		var renderSize = element.RenderSize;
		var visual = element.Visual;

		var previousClip = visual.LayoutClip;

		try
		{
			// Remove any existing layout clip, we want to render the full element, not
			// the clipped part based on the existing parent's layout slot.
			visual.LayoutClip = null;

			if (renderSize is { IsEmpty: true } or { Width: 0, Height: 0 })
			{
				return (0, 0, 0);
			}

			// Note: RenderTargetBitmap returns images with the current DPI (a 50x50 Border rendered on WinUI will return a 75x75 image)
			var dpi = element.XamlRoot?.VisualTree.RootScale.GetEffectiveRasterizationScale() ?? DisplayInformation.GetForCurrentView()?.RawPixelsPerViewPixel ?? 1;
			var (width, height) = ((int)(renderSize.Width * dpi), (int)(renderSize.Height * dpi));
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			using var surface = SKSurface.Create(info);
			//Ensure Clear
			var canvas = surface.Canvas;
			canvas.Clear(SKColors.Transparent);
			canvas.Scale((float)dpi);
			VisualSkiaPlatform.Of(visual).RenderRootVisual(canvas, offsetOverride: Vector2.Zero);

			var img = surface.Snapshot();

			var bitmap = img.ToSKBitmap();
			if (scaledSize.HasValue)
			{
				var scaledBitmap = bitmap.Resize(
					new SKImageInfo((int)scaledSize.Value.Width, (int)scaledSize.Value.Height, SKColorType.Bgra8888, SKAlphaType.Premul),
					new SKSamplingOptions(SKCubicResampler.CatmullRom));
				bitmap.Dispose();
				bitmap = scaledBitmap;
				(width, height) = (bitmap.Width, bitmap.Height);
			}

			var byteCount = bitmap.ByteCount;
			RenderTargetBitmap.EnsureBuffer(ref buffer, byteCount);
			unsafe
			{
				bitmap.GetPixelSpan().CopyTo(new Span<byte>(buffer!.Pointer.ToPointer(), byteCount));
			}
			bitmap?.Dispose();

			return (byteCount, width, height);
		}
		finally
		{
			visual.LayoutClip = previousClip;
			compositor.IsSoftwareRenderer = previousCompMode;
		}
	}

	/// <inheritdoc />
	public unsafe PlatformCompositionSurface? CreateSurfaceFromPicture(object svgPicture, Size sourceSize)
	{
		if (svgPicture is not SKPicture picture)
		{
			return null;
		}

		_fromPictureMethod ??= typeof(SKImage).GetMethod(
			"FromPicture",
			BindingFlags.NonPublic | BindingFlags.Static,
			new[] {
				typeof(SKPicture),
				typeof(SKSizeI),
				typeof(SKMatrix).MakePointerType(),
				typeof(SKPaint),
				typeof(bool),
				typeof(SKColorSpace),
				typeof(SKSurfaceProperties) });

		if (_fromPictureMethod is null)
		{
			throw new InvalidOperationException("Unable to find the 'FromPicture' method on SKImage");
		}

		var matrix = SKMatrix.Identity;

		var skImage = (SKImage)_fromPictureMethod.Invoke(
			null,
			[
				picture,
				new SKSizeI((int)sourceSize.Width, (int)sourceSize.Height),
				Pointer.Box(&matrix, typeof(SKMatrix*)),
				new SKPaint(),
				false,
				SKColorSpace.CreateSrgb(),
				new SKSurfaceProperties(SKPixelGeometry.Unknown)
		])!;

		return CreateSurface(skImage);
	}

	/// <inheritdoc />
	public bool TryGetImageSize(PlatformCompositionSurface surface, out int width, out int height)
	{
		if (CompositionSurfaceSkiaPlatform.Of(surface).Image is { } image)
		{
			width = image.Width;
			height = image.Height;
			return true;
		}

		width = 0;
		height = 0;
		return false;
	}

	/// <inheritdoc />
	public void DisposeImage(PlatformCompositionSurface surface) => CompositionSurfaceSkiaPlatform.Of(surface).Image?.Dispose();

	private static PlatformCompositionSurface CreateSurface(SKImage image)
	{
		var surface = new PlatformCompositionSurface();
		CompositionSurfaceSkiaPlatform.Of(surface).SetImage(image);
		return surface;
	}

	private static partial class NativeMethods
	{
		// https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/6.0/byte-array-interop#receive-byte-array-in-javascript-from-net-1
		[JSImport($"globalThis.CodeBrix.Platform.UI.Runtime.Skia.ImageLoader.loadFromArray")]
		internal static partial Task<JSObject> LoadFromArray(byte[] array);
	}
}
