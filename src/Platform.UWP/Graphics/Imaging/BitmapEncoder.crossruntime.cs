#if !__NETSTD_REFERENCE__
using System;
using System.Threading.Tasks;
using CodeBrix.Platform.Contracts;
using Windows.Foundation;
using Windows.Storage.Streams;
#endif

namespace Windows.Graphics.Imaging
{
	partial class BitmapEncoder
	{
#if __NETSTD_REFERENCE__
		private BitmapEncoder() { }
#else
		private static IGraphicsImagingPlatform _imagingPlatform;

		private readonly Guid _encoderId;
		private readonly IRandomAccessStream _stream;
		private SoftwareBitmap _softwareBitmap;

		/// <summary>
		/// Gets the platform that encodes images, resolved once on first use.
		/// </summary>
		private static IGraphicsImagingPlatform ImagingPlatform => _imagingPlatform ??= PlatformContract.Resolve<IGraphicsImagingPlatform>();

		private BitmapEncoder(Guid encoderId, IRandomAccessStream stream)
		{
			_encoderId = encoderId;
			_stream = stream;
		}

		public static IAsyncOperation<BitmapEncoder> CreateAsync(Guid encoderId
			, IRandomAccessStream stream) =>
			AsyncOperation.FromTask(ct =>
			{
				if (!ImagingPlatform.IsEncoderSupported(encoderId))
				{
					throw new NotImplementedException($"Encoder {encoderId} in not implemented.", new ArgumentException(nameof(encoderId)));
				}
				return Task.FromResult(new BitmapEncoder(encoderId, stream));
			});

		public void SetSoftwareBitmap(SoftwareBitmap bitmap)
		{
			_softwareBitmap?.Dispose();
			_softwareBitmap = bitmap;
		}

		public IAsyncAction FlushAsync() =>
			AsyncAction.FromTask(ct =>
				{
					if (_softwareBitmap is { } softwareBitmap)
					{
						ImagingPlatform.Encode(softwareBitmap.PlatformBitmap, _encoderId, _stream);
					}
					return Task.CompletedTask;
				});

		public void SetPixelData(global::Windows.Graphics.Imaging.BitmapPixelFormat pixelFormat, global::Windows.Graphics.Imaging.BitmapAlphaMode alphaMode, uint width, uint height, double dpiX, double dpiY, byte[] pixels)
		{
			_softwareBitmap?.Dispose();
			_softwareBitmap = null;

			var bitmap = ImagingPlatform.CreateBitmapFromPixels(pixels, pixelFormat, alphaMode, (int)width, (int)height);
			if (bitmap is not null)
			{
				_softwareBitmap = new SoftwareBitmap(bitmap);
			}
		}
#endif
	}
}
