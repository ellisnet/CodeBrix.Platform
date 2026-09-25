#if !__NETSTD_REFERENCE__
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Helpers;
using CodeBrix.Platform.UI.Xaml.Media;
using Windows.Application­Model;
using CodeBrix.Platform.UI.Contracts;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	private protected override bool TryOpenSourceAsync(CancellationToken ct, int? targetWidth, int? targetHeight, out Task<ImageData> asyncImage)
	{
		if (TryOpenSvgImageData(ct, out var imageTask))
		{
			asyncImage = imageTask.ContinueWith(task =>
			{
				var imageData = task.Result;
				if (imageData is { Kind: ImageDataKind.ByteArray, ByteArray: not null } &&
					_svgProvider?.TryGetLoadedDataAsPictureAsync() is { } picture &&
					PlatformServices.Imaging.CreateSurfaceFromPicture(picture, _svgProvider.SourceSize) is { } surface)
				{
					return ImageData.FromCompositionSurface(surface);
				}
				else
				{
					return ImageData.Empty;
				}
			}, ct);
			return true;
		}
		else
		{
			asyncImage = Task.FromResult(ImageData.Empty);
			return false;
		}
	}

	private async Task<ImageData> GetSvgImageDataAsync(CancellationToken ct)
	{
		try
		{
			ImageData imageData = await ImageSourceHelpers.GetImageDataFromUriAsBytes(AbsoluteUri, ct);
			if (!imageData.HasData && _stream is not null)
			{
				imageData = await ImageSourceHelpers.ReadFromStreamAsBytesAsync(_stream.AsStream(), ct);
			}

			return imageData;
		}
		catch (Exception e)
		{
			return ImageData.FromError(e);
		}
	}
}
#endif
