#if !__NETSTD_REFERENCE__
using System.IO;
using CodeBrix.Platform.UI.Contracts;

namespace Microsoft.UI.Xaml.Media.Imaging;

public partial class BitmapSource
{
	partial void UpdatePixelWidthAndHeightPartial(Stream stream)
	{
		var (width, height) = PlatformServices.Imaging.GetEncodedImageSize(stream);
		PixelWidth = width;
		PixelHeight = height;
	}
}
#endif
