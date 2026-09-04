using System;
using System.Data.SqlTypes;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Xaml.Media.Imaging.Svg;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.Extensions.Disposables;
using Windows.Graphics.Display;
using System.Diagnostics.CodeAnalysis;


#if !__NETSTD_REFERENCE__
using CodeBrix.SkiaSvg.ShimSkiaSharp;
using CodeBrix.SkiaSvg;
using CodeBrix.SkiaSvg.Model;
using CodeBrix.Platform.UI.Xaml.Media;
using SkiaSharp;
using SKCanvas = SkiaSharp.SKCanvas;
using SKMatrix = SkiaSharp.SKMatrix;
#else
#pragma warning disable CS0067
#endif

namespace CodeBrix.Platform.UI.Svg; //Was previously: Uno.UI.Svg

public partial class SvgProvider : ISvgProvider
{
	//Keyed weakly by the source, because the CSS belongs to the SvgImageSource rather than to a
	//provider: it has to be readable at PARSE time, which happens on a background thread inside the
	//provider the framework created for that source, and it has to survive the provider being
	//recreated when the source is reloaded.
	private static readonly ConditionalWeakTable<SvgImageSource, string> _cssBySource = new();

	//The provider that most recently took ownership of a source, so the rasterised size can be
	//asked for from the source alone - which is all an application, or a demo's self-test, has.
	private static readonly ConditionalWeakTable<SvgImageSource, SvgProvider> _providersBySource = new();

	/// <summary>
	/// Sets the CSS applied to the SVG document behind <paramref name="source"/> the next time that
	/// source is parsed.
	/// </summary>
	/// <param name="source">The image source the CSS belongs to.</param>
	/// <param name="css">
	/// A CSS snippet, for example <c>svg { color: #2266DD; }</c>, or null to remove one that was set
	/// before. The rules are applied to the document as an author stylesheet, so a file that paints
	/// with <c>currentColor</c> can be themed without the file being rewritten.
	/// </param>
	/// <remarks>
	/// Set this BEFORE the source starts loading - before its <c>UriSource</c> is assigned, or before
	/// <c>SetSourceAsync</c> is called. Changing it afterwards has no effect until the source is
	/// parsed again, because parsing is what applies it.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
	public static void SetCss(SvgImageSource source, string? css)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}

		if (string.IsNullOrWhiteSpace(css))
		{
			_cssBySource.Remove(source);
		}
		else
		{
			_cssBySource.AddOrUpdate(source, css);
		}
	}

	/// <summary>Reads the CSS <see cref="SetCss"/> put on <paramref name="source"/>.</summary>
	/// <param name="source">The image source to read.</param>
	/// <returns>The CSS snippet, or null when none was set.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
	public static string? GetCss(SvgImageSource source)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}

		return _cssBySource.TryGetValue(source, out var css) ? css : null;
	}

	/// <summary>
	/// The size, in PHYSICAL pixels, of the bitmap the SVG route rasterised for
	/// <paramref name="source"/>.
	/// </summary>
	/// <param name="source">The image source to ask about.</param>
	/// <returns>
	/// The bitmap's size, or an empty size when the source is drawn from its vector picture instead
	/// - which is the case unless it sets both <c>RasterizePixelWidth</c> and
	/// <c>RasterizePixelHeight</c>.
	/// </returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
	public static Size GetRasterizedPixelSize(SvgImageSource source)
	{
		if (source is null)
		{
			throw new ArgumentNullException(nameof(source));
		}

		return _providersBySource.TryGetValue(source, out var provider) ? provider.RasterizedPixelSize : default;
	}

#if !__NETSTD_REFERENCE__
	private readonly SvgImageSource _owner;
	private readonly CompositeDisposable _disposables = new();

	private SKSvg? _skSvg;
	private SKBitmap? _skBitmap;
#endif

	public SvgProvider(object owner)
	{
#if __NETSTD_REFERENCE__
		throw new PlatformNotSupportedException();
#else

		if (owner is not SvgImageSource svgImageSource)
		{
			throw new InvalidOperationException("Owner must be a SvgImageSource instance.");
		}

		_owner = svgImageSource;
		_providersBySource.AddOrUpdate(svgImageSource, this);

		_disposables.Add(_owner.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelHeightProperty, SourcePropertyChanged));
		_disposables.Add(_owner.RegisterDisposablePropertyChangedCallback(SvgImageSource.RasterizePixelWidthProperty, SourcePropertyChanged));
#endif // __NETSTD_REFERENCE__
	}

	public event EventHandler? SourceLoaded;

#if !__NETSTD_REFERENCE__
	internal event EventHandler? SourceUpdated;

	internal SKSvg? SkSvg => _skSvg;

	internal SKBitmap? SkBitmap => _skBitmap;
#endif

	public bool IsParsed
#if __NETSTD_REFERENCE__
		=> throw new PlatformNotSupportedException();
#else
		=> _skSvg?.Picture is not null;
#endif

	public Size SourceSize
	{
		get
		{
#if __NETSTD_REFERENCE__
			throw new PlatformNotSupportedException();
#else
			if (_skSvg?.Picture?.CullRect is { } rect)
			{
				return new Size(rect.Width, rect.Height);
			}

			return default;
#endif
		}
	}

	/// <summary>
	/// The size, in PHYSICAL pixels, of the bitmap this provider rasterised for its source.
	/// </summary>
	/// <remarks>
	/// Empty unless the source sets both <c>RasterizePixelWidth</c> and <c>RasterizePixelHeight</c>;
	/// those are logical pixels, and the rasterised bitmap is that size multiplied by the display's
	/// scale, so an icon asked for at 24 logical pixels on a 125% display is 30 pixels across.
	/// Without them the source is drawn from its vector picture instead and there is no bitmap.
	/// </remarks>
	public Size RasterizedPixelSize
	{
		get
		{
#if __NETSTD_REFERENCE__
			throw new PlatformNotSupportedException();
#else
			return _skBitmap is { } bitmap ? new Size(bitmap.Width, bitmap.Height) : default;
#endif
		}
	}

	public UIElement GetCanvas()
#if __NETSTD_REFERENCE__
		=> throw new PlatformNotSupportedException();
#else
		=> new SvgCanvas(_owner, this);
#endif

	public object? TryGetLoadedDataAsPictureAsync()
#if __SKIA__
		=> _skSvg?.Picture;
#else
		=> null;
#endif

	public
#if !__NETSTD_REFERENCE__
	async
#endif
	Task<bool> TryLoadSvgDataAsync(byte[] svgBytes)
	{
#if __NETSTD_REFERENCE__
		return Task.FromResult(false);
#else
		var succeeded = false;
		try
		{
			CleanupSvg();
			var skSvg = await LoadSvgAsync(svgBytes);
			if (skSvg is not null)
			{
				_skSvg = skSvg;
				_owner.RaiseImageOpened();
				_skBitmap = null;
				UpdateBitmap();
				SourceLoaded?.Invoke(this, EventArgs.Empty);
				succeeded = true;
			}
			else
			{
				CleanupSvg();
				_owner.RaiseImageFailed(SvgImageSourceLoadStatus.InvalidFormat);
				succeeded = false;
			}
			SourceUpdated?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Failed to load SVG image.", ex);
			}
			CleanupSvg();
			succeeded = false;
		}

		return succeeded;
#endif
	}

	private void CleanupSvg()
	{
#if !__NETSTD_REFERENCE__
		_skSvg?.Dispose();
		_skBitmap?.Dispose();
		_skSvg = null;
		_skBitmap = null;
#endif
	}

#if !__NETSTD_REFERENCE__
	private Task<SKSvg?> LoadSvgAsync(byte[] svgBytes)
	{
		//Read on the calling thread: the parse itself runs on the thread pool, and the CSS belongs
		//to the source rather than to that thread.
		var css = GetCss(_owner);
		var parameters = css is null ? (SvgParameters?)null : new SvgParameters(null!, css);

		return Task.Run(() =>
		{
			var skSvg = new SKSvg();
			try
			{
				using var memoryStream = new MemoryStream(svgBytes);
				skSvg.Load(memoryStream, parameters);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("Failed to load SVG image.", ex);
				}
				skSvg.Dispose();
				skSvg = null;
			}

			return skSvg;
		});
	}

	private bool UpdateBitmap()
	{
		var scale = DisplayInformation.GetForCurrentView().LogicalDpi / DisplayInformation.BaseDpi;
		var desiredPhysicalWidth = (int)(scale * _owner.RasterizePixelWidth);
		var desiredPhysicalHeight = (int)(scale * _owner.RasterizePixelHeight);
		var changed = false;
		if (!double.IsNaN(_owner.RasterizePixelHeight) &&
			!double.IsNaN(_owner.RasterizePixelWidth) &&
			_skSvg is not null &&
			(_skBitmap is null || _skBitmap.Width != desiredPhysicalWidth || _skBitmap.Height != desiredPhysicalHeight))
		{
			var bitmap = new SKBitmap(desiredPhysicalWidth, desiredPhysicalHeight);
			using SKCanvas canvas = new SKCanvas(bitmap);

			SKMatrix scaleMatrix = default;
			if (_skSvg.Picture?.CullRect is { } rect)
			{
				scaleMatrix = SKMatrix.CreateScale(bitmap.Width / rect.Width, bitmap.Height / rect.Height);
			}

			canvas.Clear(SKColors.Transparent);
			canvas.DrawPicture(_skSvg.Picture, in scaleMatrix);
			_skBitmap = bitmap;
			changed = true;
		}
		else if (
			double.IsNaN(_owner.RasterizePixelHeight) &&
			double.IsNaN(_owner.RasterizePixelWidth) &&
			_skBitmap is not null)
		{
			_skBitmap?.Dispose();
			_skBitmap = null;
			changed = true;
		}
		return changed;
	}

	private void SourcePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		if (UpdateBitmap())
		{
			SourceUpdated?.Invoke(this, EventArgs.Empty);
		}
	}
#endif

	public void Unload() => CleanupSvg();
}
