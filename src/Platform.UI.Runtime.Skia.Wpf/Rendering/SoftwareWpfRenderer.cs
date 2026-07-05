#nullable enable

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.UI.Xaml;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Helpers;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.Hosting;
using Visibility = System.Windows.Visibility;
using WpfControl = global::System.Windows.Controls.Control;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Rendering; //Was previously: Uno.UI.Runtime.Skia.Wpf.Rendering

internal class SoftwareWpfRenderer : IWpfRenderer
{
	private readonly WpfControl _hostControl;
	private readonly IWpfXamlRootHost _host;
	private WriteableBitmap? _bitmap;
	private XamlRoot? _xamlRoot;

	public SoftwareWpfRenderer(IWpfXamlRootHost host)
	{
		_hostControl = host as WpfControl ?? throw new InvalidOperationException("Host should be a WPF control");
		_host = host;
	}

	public SKColor BackgroundColor { get; set; } = SKColors.White;

	public bool TryInitialize() => true;

	public void Dispose() { }

	public void Render(DrawingContext drawingContext)
	{
		if (_hostControl.ActualWidth == 0
			|| _hostControl.ActualHeight == 0
			|| double.IsNaN(_hostControl.ActualWidth)
			|| double.IsNaN(_hostControl.ActualHeight)
			|| double.IsInfinity(_hostControl.ActualWidth)
			|| double.IsInfinity(_hostControl.ActualHeight)
			|| _hostControl.Visibility != Visibility.Visible)
		{
			return;
		}

		_xamlRoot ??= XamlRootMap.GetRootForHost(_host) ?? throw new InvalidOperationException("XamlRoot must not be null when renderer is initialized");

		_bitmap?.Lock();
		var surface = _bitmap is not null ? SKSurface.Create(new SKImageInfo(_bitmap.PixelWidth, _bitmap.PixelHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul), _bitmap.BackBuffer, _bitmap.BackBufferStride) : null;
		try
		{
			var nativeElementClipPath = ((Microsoft.UI.Xaml.Media.CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(surface?.Canvas, size =>
			{
				_bitmap?.Unlock();
				_bitmap = new WriteableBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32, null);
				_bitmap.Lock();
				//Release the surface that wrapped the previous back buffer before replacing it
				surface?.Dispose();
				surface = SKSurface.Create(new SKImageInfo(_bitmap.PixelWidth, _bitmap.PixelHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul), _bitmap.BackBuffer, _bitmap.BackBufferStride);
				return surface.Canvas;
			});

			if (_host.NativeOverlayLayer is { } nativeLayer)
			{
				// Airspace: the WPF Clip geometry set below does NOT clip an HwndHost's native surface
				// (e.g. a WebView2). When the clip path collapses - i.e. the native elements are fully
				// occluded, such as behind a modal ContentDialog's full-window smoke layer - the native
				// surface would otherwise keep painting on top of the dialog, leaving it invisible and the
				// app unusable. WebView2 DOES honor Visibility, so hide the whole native overlay when
				// nothing native is visible, and show it again when the path reappears. This mirrors the
				// Win32 head, which clips the native window to the (now empty) region via SetWindowRgn.
				var clipBounds = nativeElementClipPath.Bounds;
				nativeLayer.Visibility = nativeElementClipPath.IsEmpty || clipBounds.Width < 1 || clipBounds.Height < 1
					? System.Windows.Visibility.Hidden
					: System.Windows.Visibility.Visible;

				nativeLayer.Clip ??= new PathGeometry();
				((PathGeometry)nativeLayer.Clip).Figures = PathFigureCollection.Parse(nativeElementClipPath.ToSvgPathData());
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Airspace clipping failed because ${nameof(_host.NativeOverlayLayer)} is null");
				}
			}

			// draw the bitmap to the screen
			if (_bitmap is not null)
			{
				_bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
				_bitmap.Unlock();
				drawingContext.DrawImage(_bitmap, new Rect(0, 0, _hostControl.ActualWidth, _hostControl.ActualHeight));
			}
		}
		finally
		{
			//A fresh SKSurface wraps the WriteableBitmap back buffer every frame and MUST be
			//  released each frame - even if a frame throws. Leaking it (the previous behavior)
			//  piled up native SKSurface objects whose finalizer-thread destruction races the
			//  render thread, corrupting native memory and surfacing as an access violation
			//  (ExecutionEngineException) after drawing (rendering many frames) for a while.
			surface?.Dispose();
		}
	}
}
