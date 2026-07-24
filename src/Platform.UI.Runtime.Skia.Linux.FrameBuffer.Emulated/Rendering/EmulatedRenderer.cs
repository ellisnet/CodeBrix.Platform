using System;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

/// <summary>
/// The emulated head's renderer: CPU raster Skia at the device's one fixed
/// resolution, on a dedicated render thread woken by invalidation — the same
/// shape as the real FrameBuffer head's SoftwareRenderer, with the shared
/// memory slot standing in for /dev/fb0. No orientation transform (the IDE
/// chose the resolution's orientation) and no mouse-cursor indicator (the
/// emulated device is a touchscreen; the finger is the pointer).
/// </summary>
internal class EmulatedRenderer
{
	private readonly IXamlRootHost _host;
	private readonly EmulatorConnection _connection;
	private readonly AutoResetEvent _renderInvalidationEvent = new(false);
	private readonly SKImageInfo _frameInfo;
	private SKSurface? _surface;
	private long _sequence;
	private int _renderCount;

	public EmulatedRenderer(IXamlRootHost host, EmulatorConnection connection)
	{
		_host = host;
		_connection = connection;
		_frameInfo = new SKImageInfo(connection.Width, connection.Height,
			SKColorType.Bgra8888, SKAlphaType.Premul);

		FrameBufferWindowWrapper.Instance.SetSize(new Size(connection.Width, connection.Height));

		new Thread(_ =>
		{
			while (true)
			{
				try
				{
					_renderInvalidationEvent.WaitOne();
					Render();
					Publish();
				}
				catch (Exception ex)
				{
					this.LogError()?.Error("Error during emulated rendering", ex);
				}
			}
		})
		{
			IsBackground = true,
			Name = "FrameBuffer.Emulated rendering thread"
		}.Start();
	}

	public void InvalidateRender() => _renderInvalidationEvent.Set();

	private void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget ct)
		{
			throw new InvalidOperationException(
				$"CompositionTarget is not set on the {nameof(IXamlRootHost)} at the point of rendering.");
		}

		_surface ??= SKSurface.Create(_frameInfo);
		_surface.Canvas.Clear(SKColors.Transparent);

		ct.OnNativePlatformFrameRequested(_surface.Canvas, size =>
		{
			// The device never resizes, so the compositor can only ever ask
			// for the fixed resolution; recreate at that size regardless, per
			// the callback's contract.
			if ((int) size.Width != _frameInfo.Width || (int) size.Height != _frameInfo.Height)
			{
				this.LogError()?.Error(
					$"The compositor requested {size.Width}x{size.Height}; this emulated device is fixed at {_frameInfo.Width}x{_frameInfo.Height}.");
			}
			_surface?.Dispose();
			_surface = SKSurface.Create(_frameInfo);
			_surface.Canvas.Clear(SKColors.Transparent);
			return _surface.Canvas;
		});

		_surface?.Flush();
	}

	private void Publish()
	{
		if (_surface is null)
		{
			return;
		}
		var next = _sequence + 1;
		_surface.ReadPixels(_frameInfo, _connection.GetSlotPointer(next), _connection.Stride, 0, 0);
		_connection.PublishFrame(next);
		_sequence = next;
	}
}
