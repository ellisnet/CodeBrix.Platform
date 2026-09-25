#if !__NETSTD_REFERENCE__
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
		private PlatformCompositionSurface _surface;

		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			_surface ??= new PlatformCompositionSurface();

			_surface.CopyPixels(PixelWidth, PixelHeight, _buffer.AsReadOnlyMemory());

			image = ImageData.FromCompositionSurface(_surface);

			return true;
		}

		private void DecodeStreamIntoBuffer()
			=> PlatformServices.Imaging.DecodeToBgra8(_stream.AsStream(), _buffer.Span, PixelWidth * 4);
	}
}
#endif
