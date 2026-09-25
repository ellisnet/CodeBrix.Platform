#if !__NETSTD_REFERENCE__
#nullable enable
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		private static ImageData Open(UnmanagedArrayOfBytes buffer, int bufferLength, int width, int height)
			=> PlatformServices.Imaging.CreateImageFromBgra8Premul(buffer.Pointer, width, height);

		private static (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref UnmanagedArrayOfBytes? buffer, Size? scaledSize = null)
			=> PlatformServices.Imaging.RenderToBgra8Premul(element, ref buffer, scaledSize);
	}
}
#endif
