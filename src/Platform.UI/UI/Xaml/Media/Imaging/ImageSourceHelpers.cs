#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Xaml.Media;
using Windows.ApplicationModel;
using Windows.Storage;
using Microsoft.UI.Composition;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Contracts;

namespace CodeBrix.Platform.Helpers; //Was previously: Uno.Helpers

internal static partial class ImageSourceHelpers
{
	public static async Task<ImageData> ReadFromStreamAsBytesAsync(Stream stream, CancellationToken ct)
	{
		if (stream.CanSeek && stream.Position != 0)
		{
			stream.Position = 0;
		}

		var memoryStream = new MemoryStream();
		await stream.CopyToAsync(memoryStream, 81920, ct);
		var data = memoryStream.ToArray();
		return ImageData.FromBytes(data);
	}

#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
	public static async Task<ImageData> ReadFromStreamAsCompositionSurface(Stream imageStream, CancellationToken ct, bool attemptLoadingWithBrowserCanvasApi = true)
	{
		var buffer = new byte[imageStream.Length - imageStream.Position];
		await imageStream.ReadExactlyAsync(buffer, 0, buffer.Length, ct);

		if (OperatingSystem.IsBrowser() && attemptLoadingWithBrowserCanvasApi)
		{
			if (await PlatformServices.Imaging.TryDecodeWithBrowserAsync(buffer) is { } decoded)
			{
				return decoded;
			}
		}

		var surface = new PlatformCompositionSurface();
		var result = surface.LoadFromStream(new MemoryStream(buffer));

		if (result.success)
		{
			return ImageData.FromCompositionSurface(surface);
		}
		else
		{
			var exception = new InvalidOperationException($"Image load failed ({result.nativeResult})");
			return ImageData.FromError(exception);
		}
	}
#endif

	public static async Task<ImageData> GetImageDataFromUriAsBytes(Uri uri, CancellationToken ct)
	{
		try
		{
			using var stream = await AppDataUriEvaluator.ToStream(uri, ct);
			return await ReadFromStreamAsBytesAsync(stream, ct);
		}
		catch (Exception e)
		{
			return ImageData.FromError(e);
		}
	}

#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
	public static async Task<ImageData> GetImageDataFromUriAsCompositionSurface(Uri uri, CancellationToken ct)
	{
		try
		{
			var stream = await AppDataUriEvaluator.ToStream(uri, ct);
			// add more animation formats here if needed
			return await ReadFromStreamAsCompositionSurface(stream, ct, !uri.AbsolutePath.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase));
		}
		catch (Exception e)
		{
			return ImageData.FromError(e);
		}
	}
#endif
}
