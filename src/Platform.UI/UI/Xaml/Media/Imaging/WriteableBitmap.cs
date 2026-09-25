using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using UwpBuffer = Windows.Storage.Streams.Buffer;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	public partial class WriteableBitmap : BitmapSource
	{
		private UwpBuffer _buffer;

		public IBuffer PixelBuffer => _buffer;

		public WriteableBitmap(int pixelWidth, int pixelHeight) : base()
		{
			PixelWidth = pixelWidth;
			PixelHeight = pixelHeight;
			UpdateBuffer();
		}

		private void UpdateBuffer()
		{
			var pixelsBufferSize = (uint)(PixelWidth * PixelHeight * 4);
			if (_buffer?.Capacity != pixelsBufferSize)
			{
				_buffer = new UwpBuffer(pixelsBufferSize)
				{
					Length = pixelsBufferSize
				};
			}
		}

		public void Invalidate()
		{
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			InvalidateSource();
#endif
			InvalidateImageSource();
		}

		private protected
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			unsafe
#endif
			override void OnSetSource()
		{
			UpdateBuffer();

#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__ // TODO: Other platforms.
			DecodeStreamIntoBuffer();
#endif
		}
	}
}
