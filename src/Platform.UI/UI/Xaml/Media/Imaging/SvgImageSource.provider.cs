#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Xaml.Media;
using CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Imaging;

partial class SvgImageSource
{
	// The published package id of the Svg add-in (the names this line used to print, CodeBrix.Platform.WinUI.Svg /
	// CodeBrix.Platform.UI.Svg, are not packages).
	private const string SvgPackageName = "CodeBrix.Platform.Svg.ApacheLicenseForever";

	private static int _svgPackageMissingReports;

	/// <summary>
	/// Gets how many times this process reported that no SVG provider is registered (at most once: the report is not
	/// repeated per image).
	/// </summary>
	internal static int SvgPackageMissingReports => Volatile.Read(ref _svgPackageMissingReports);

	private Task<ImageData>? _currentOpenTask;

	private ISvgProvider? _svgProvider;

	internal event EventHandler? SourceLoaded;

	private void InitSvgProvider()
	{
		if (!ApiExtensibility.CreateInstance(this, out _svgProvider))
		{
			LogSvgPackageError();
		}

		if (_svgProvider is not null)
		{
			_svgProvider.SourceLoaded += OnSourceLoaded;
		}
	}

	private bool TryOpenSvgImageData(CancellationToken ct, out Task<ImageData> asyncImage)
	{
		_currentOpenTask ??= LoadSvgImageAsync(ct);
		asyncImage = _currentOpenTask;
		return true;
	}

	private async Task<ImageData> LoadSvgImageAsync(CancellationToken ct)
	{
		if (_svgProvider is null)
		{
			LogSvgPackageError();
			return ImageData.Empty;
		}

		var imageData = await GetSvgImageDataAsync(ct);

		if (imageData.Kind == ImageDataKind.ByteArray &&
			imageData.ByteArray is not null &&
			await _svgProvider.TryLoadSvgDataAsync(imageData.ByteArray))
		{
			return imageData;
		}

		return ImageData.Empty;
	}

	internal UIElement? GetCanvas() => _svgProvider?.GetCanvas();

	internal bool IsParsed => _svgProvider?.IsParsed ?? false;

	internal Size SourceSize => _svgProvider?.SourceSize ?? default;

	private void OnSourceLoaded(object? sender, EventArgs e) => SourceLoaded?.Invoke(this, EventArgs.Empty);

	private void Unload() => _svgProvider?.Unload();

	private protected override void UnloadImageSourceData()
	{
		_currentOpenTask = null;
		Unload();
	}

	private void LogSvgPackageError()
	{
		// Once per process, as a warning: an app without the Svg add-in (or a platform whose Svg flavor is not
		// registered yet) used to get one ERROR per SvgImageSource, naming a package that does not exist.
		if (Interlocked.Exchange(ref _svgPackageMissingReports, 1) != 0)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().LogWarning($"SvgImageSource: no SVG provider is registered, SVG images stay empty. To use SVG on this platform, install the {SvgPackageName} package.");
		}
	}
}
#endif
