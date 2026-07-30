#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Windows.ApplicationModel;
using Windows.Graphics.Display;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering.Internal;

//was previously: the editor's WPF ancestor drew every layer through UIElement.OnRender and
//DrawingContext, which this framework does not provide. This element re-creates the proven
//software present path of the family's SKXamlCanvas (src/AddIns/CodeBrix.Platform.SkiaSharp.Views,
//SKXamlCanvas.Skia.cs): draw into a pinned staging buffer through a cached SKSurface, copy into a
//WriteableBitmap shown via an ImageBrush background. It is deliberately internal - consumers who
//want a general-purpose Skia canvas should use the CodeBrix.Platform.SkiaSharp.Views package -
//and deliberately smaller: one paint callback, always in device-independent pixels (the canvas is
//pre-scaled by the display's scale factor, so paint code never sees raw pixels).

/// <summary>
/// The editor's drawing surface: a XAML element whose content is painted with an
/// <see cref="SKCanvas"/> in device-independent pixels.
/// </summary>
internal sealed partial class RenderCanvas : Canvas
{
	private const float DpiBase = 96.0f;

	private static readonly bool _designMode = DesignMode.DesignModeEnabled;

	private byte[]? _pixels;
	private GCHandle _pixelsHandle;
	private int _pixelWidth;
	private int _pixelHeight;
	private WriteableBitmap? _bitmap;
	private SKSurface? _stagingSurface;
	private bool _isVisible = true;
	private int _loadUnloadCounter;

	private static readonly DependencyProperty _proxyVisibilityProperty =
		DependencyProperty.Register(
			"ProxyVisibility",
			typeof(Visibility),
			typeof(RenderCanvas),
			new PropertyMetadata(Visibility.Visible, OnVisibilityChanged));

	/// <summary>
	/// Creates the surface. Painting starts once the element is loaded and has a size.
	/// </summary>
	public RenderCanvas()
	{
		if (_designMode)
		{
			return;
		}

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		SizeChanged += (_, _) => Invalidate();

		var binding = new Microsoft.UI.Xaml.Data.Binding
		{
			Path = new PropertyPath(nameof(Visibility)),
			Source = this,
		};
		SetBinding(_proxyVisibilityProperty, binding);
	}

	/// <summary>The display scale factor the last paint used (1.0 = 96 dpi).</summary>
	/// <remarks>Intentionally hides <see cref="UIElement.Scale"/>: paint code deals in the
	/// display's DIP scale factor, not the composition scale vector.</remarks>
	public new double Scale { get; private set; } = 1;

	/// <summary>
	/// Called on each repaint with the surface's canvas, already scaled so that one canvas unit is
	/// one device-independent pixel, and the paintable size in device-independent pixels.
	/// </summary>
	public event Action<SKCanvas, SKSize>? Paint;

	/// <summary>
	/// Repaints the surface. Safe to call from any thread; the paint happens on the UI thread.
	/// </summary>
	public void Invalidate()
	{
		if (DispatcherQueue is null)
		{
			return;
		}

		if (DispatcherQueue.HasThreadAccess)
		{
			DoInvalidate();
		}
		else
		{
			DispatcherQueue.TryEnqueue(DoInvalidate);
		}
	}

	private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is RenderCanvas canvas && e.NewValue is Visibility visibility)
		{
			canvas._isVisible = visibility == Visibility.Visible;
			canvas.Invalidate();
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_loadUnloadCounter++;
		if (_loadUnloadCounter != 1)
		{
			return;
		}

		var display = DisplayInformation.GetForCurrentView();
		display.DpiChanged += OnDpiChanged;
		OnDpiChanged(display);
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_loadUnloadCounter--;
		if (_loadUnloadCounter != 0)
		{
			return;
		}

		var display = DisplayInformation.GetForCurrentView();
		display.DpiChanged -= OnDpiChanged;
		FreeBitmap();
	}

	private void OnDpiChanged(DisplayInformation sender, object? args = null)
	{
		Scale = sender.LogicalDpi / DpiBase;
		Invalidate();
	}

	private void DoInvalidate()
	{
		if (_designMode || !_isVisible || Paint is null)
		{
			return;
		}

		var width = ActualWidth;
		var height = ActualHeight;
		if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0
			|| double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
		{
			return;
		}

		var scale = (float)Scale;
		var info = new SKImageInfo(
			Math.Max(1, (int)(width * scale)),
			Math.Max(1, (int)(height * scale)),
			SKColorType.Bgra8888,
			SKAlphaType.Premul);

		EnsureBitmap(info);
		if (_bitmap is null || _pixels is null)
		{
			return;
		}

		// The surface is cached while the staging buffer is unchanged (see EnsureBitmap/FreeBitmap):
		// allocating one per paint causes measurable GC churn when the caret blink repaints.
		_stagingSurface ??= SKSurface.Create(info, _pixelsHandle.AddrOfPinnedObject(), info.RowBytes);
		var canvas = _stagingSurface.Canvas;
		canvas.RestoreToCount(1);
		canvas.ResetMatrix();
		// The staging buffer persists across frames (see the caching note above), so every frame
		// starts by clearing it - otherwise pixels from the previous frame survive wherever the
		// new frame paints nothing, and edited text draws over its old glyphs.
		canvas.Clear(SKColors.Transparent);
		canvas.Scale(scale);
		canvas.Save();

		Paint(canvas, new SKSize((float)width, (float)height));

		_stagingSurface.Flush();

		// SKXamlCanvas reaches Windows.Storage.Streams.Buffer's internal raw-pointer accessor
		// through InternalsVisibleTo, which this assembly does not have. The public CopyTo
		// extension performs the same single copy into the bitmap's backing buffer.
		_pixels.CopyTo(_bitmap.PixelBuffer);
		_bitmap.PixelBuffer.Length = (uint)_pixels.Length;
		_bitmap.Invalidate();
	}

	private void EnsureBitmap(SKImageInfo info)
	{
		if (_bitmap?.PixelWidth != info.Width || _bitmap?.PixelHeight != info.Height)
		{
			FreeBitmap();
		}

		if (_bitmap is null && info.Width > 0 && info.Height > 0)
		{
			_bitmap = new WriteableBitmap(info.Width, info.Height);
			Background = new ImageBrush
			{
				ImageSource = _bitmap,
				AlignmentX = AlignmentX.Left,
				AlignmentY = AlignmentY.Top,
				Stretch = Stretch.Fill,
			};
		}

		if (_pixels is null || _pixelWidth != info.Width || _pixelHeight != info.Height)
		{
			if (_pixels is not null)
			{
				_stagingSurface?.Dispose();
				_stagingSurface = null;
				_pixelsHandle.Free();
			}

			_pixels = new byte[info.BytesSize];
			_pixelsHandle = GCHandle.Alloc(_pixels, GCHandleType.Pinned);
			_pixelWidth = info.Width;
			_pixelHeight = info.Height;
		}
	}

	private void FreeBitmap()
	{
		_stagingSurface?.Dispose();
		_stagingSurface = null;

		if (_pixels is not null)
		{
			_pixelsHandle.Free();
			_pixels = null;
		}

		_bitmap = null;
	}
}
