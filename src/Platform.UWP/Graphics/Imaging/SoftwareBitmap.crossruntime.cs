#if !__NETSTD_REFERENCE__
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using CodeBrix.Platform.Contracts;

namespace Windows.Graphics.Imaging
{
	partial class SoftwareBitmap : IDisposable
	{
		private static IGraphicsImagingPlatform _imagingPlatform;

		/// <summary>
		/// The platform's pixel store behind this bitmap; opaque here (on Skia, an <c>SKBitmap</c>).
		/// </summary>
		private readonly object _bitmap;

		/// <summary>
		/// Gets the platform that backs bitmaps, resolved once on first use.
		/// </summary>
		private static IGraphicsImagingPlatform ImagingPlatform => _imagingPlatform ??= PlatformContract.Resolve<IGraphicsImagingPlatform>();

		internal SoftwareBitmap(object bitmap, bool isReadOnly = false)
		{
			_bitmap = bitmap;
			IsReadOnly = isReadOnly;
		}

		public SoftwareBitmap(global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height)
		{
			_bitmap = ImagingPlatform.CreateBitmap(format, width, height);
		}

		public SoftwareBitmap(global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height, global::Windows.Graphics.Imaging.BitmapAlphaMode alpha)
		{
			_bitmap = ImagingPlatform.CreateBitmap(format, width, height, alpha);
		}

		public BitmapAlphaMode BitmapAlphaMode =>
			ImagingPlatform.GetAlphaMode(_bitmap);

		public BitmapPixelFormat BitmapPixelFormat =>
			ImagingPlatform.GetPixelFormat(_bitmap);

		public bool IsReadOnly { get; }

		public int PixelHeight =>
			ImagingPlatform.GetPixelHeight(_bitmap);

		public int PixelWidth =>
			ImagingPlatform.GetPixelWidth(_bitmap);

		/// <summary>
		/// Gets the platform's pixel store behind this bitmap (on Skia, an <c>SKBitmap</c>).
		/// </summary>
		internal object PlatformBitmap => _bitmap;

		public SoftwareBitmap GetReadOnlyView() =>
			new SoftwareBitmap(_bitmap, true);

		public void CopyTo(SoftwareBitmap bitmap)
		{
			if (bitmap.IsReadOnly)
			{
				throw new ArgumentException("Destionanion is ReadOnly", nameof(bitmap));
			}
			ImagingPlatform.CopyPixels(_bitmap, bitmap._bitmap);
		}

		public static SoftwareBitmap Copy(SoftwareBitmap source) =>
			new SoftwareBitmap(ImagingPlatform.CopyBitmap(source._bitmap), false);

		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height)
		{
			return CreateCopyFromBuffer(source, format, width, height, global::Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);
		}
		public static global::Windows.Graphics.Imaging.SoftwareBitmap CreateCopyFromBuffer(global::Windows.Storage.Streams.IBuffer source, global::Windows.Graphics.Imaging.BitmapPixelFormat format, int width, int height, global::Windows.Graphics.Imaging.BitmapAlphaMode alpha)
		{
			// Get pixels
			var pixelArray = source.ToArray();

			var bitmap = ImagingPlatform.CreateBitmapFromPixels(pixelArray, format, alpha, width, height);
			if (bitmap is null)
			{
				throw new ArgumentException($"The pixel format of {nameof(source)} is not {format}.", nameof(source));
			}

			return new SoftwareBitmap(bitmap);
		}

		public void Dispose()
		{
			if (_bitmap is not null)
			{
				ImagingPlatform.DisposeBitmap(_bitmap);
			}
		}
	}
}
#endif
