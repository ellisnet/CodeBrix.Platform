using System;
using System.Threading;
using Windows.Foundation;
using Windows.Graphics.Display;
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
/// memory slot standing in for /dev/fb0. No mouse-cursor indicator (the
/// emulated device is a touchscreen; the finger is the pointer).
/// <para>
/// The mounted orientation is honored exactly as on the real head. It is a
/// canvas transform, never a resize: the frame buffer keeps the dimensions the
/// IDE created it with for the whole life of the process, and a portrait mount
/// draws the transposed application rotated into that same fixed buffer.
/// </para>
/// </summary>
internal class EmulatedRenderer
{
	private readonly IXamlRootHost _host;
	private readonly IEmulatorTransport _connection;
	private readonly AutoResetEvent _renderInvalidationEvent = new(false);
	private readonly SKImageInfo _frameInfo;
	private SKSurface? _surface;
	private long _sequence;
	private long _invalidationGeneration;
	private int _renderCount;

	public EmulatedRenderer(IXamlRootHost host, IEmulatorTransport connection)
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
					// Read the generation BEFORE drawing: anything applied before the
					// invalidation that produced this number is necessarily in the frame
					// this pass is about to draw.
					var generation = Volatile.Read(ref _invalidationGeneration);
					Render();
					Publish(generation);
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

	public void InvalidateRender() => InvalidateRenderAndGetGeneration();

	/// <summary>
	/// Asks for a new frame and returns the invalidation generation this request
	/// moved the renderer to. A published frame whose render generation is that
	/// number or larger was rendered entirely after this call returned, so every
	/// change made before the call is in it.
	/// </summary>
	internal long InvalidateRenderAndGetGeneration()
	{
		var generation = Interlocked.Increment(ref _invalidationGeneration);
		_renderInvalidationEvent.Set();
		return generation;
	}

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

		// Bounds is the application's space — the frame buffer's dimensions,
		// transposed by SetSize for the portrait mounts. The transform below
		// puts that space back into the buffer, and matches the real head's
		// FrameBufferRenderer row for row.
		var bounds = FrameBufferWindowWrapper.Instance.Size;
		var orientation = FrameBufferWindowWrapper.Instance.Orientation;
		var (degrees, transX, transY) = orientation switch
		{
			DisplayOrientations.None => (0, 0, 0),
			DisplayOrientations.Landscape => (0, 0, 0),
			DisplayOrientations.Portrait => (90, bounds.Height, 0),
			DisplayOrientations.LandscapeFlipped => (180, bounds.Width, bounds.Height),
			DisplayOrientations.PortraitFlipped => (-90, 0, bounds.Width),
			_ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation,
				"Unknown display orientation")
		};

		_surface ??= SKSurface.Create(_frameInfo);
		_surface.Canvas.Save();
		_surface.Canvas.Translate(transX, transY);
		_surface.Canvas.RotateDegrees(degrees);
		_surface.Canvas.Clear(SKColors.Transparent);

		ct.OnNativePlatformFrameRequested(_surface.Canvas, size =>
		{
			// The device never resizes, so the compositor can only ever ask
			// for the application's space — the fixed resolution, transposed
			// for a portrait mount. Recreate at the buffer's size regardless,
			// per the callback's contract.
			if (orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped)
			{
				size = new Size(size.Height, size.Width);
			}
			if ((int) size.Width != _frameInfo.Width || (int) size.Height != _frameInfo.Height)
			{
				this.LogError()?.Error(
					$"The compositor requested {size.Width}x{size.Height}; this emulated device is fixed at {_frameInfo.Width}x{_frameInfo.Height}.");
			}
			_surface?.Dispose();
			_surface = SKSurface.Create(_frameInfo);
			_surface.Canvas.Save();
			_surface.Canvas.Translate(transX, transY);
			_surface.Canvas.RotateDegrees(degrees);
			_surface.Canvas.Clear(SKColors.Transparent);
			return _surface.Canvas;
		});

		_surface?.Canvas.Restore();
		_surface?.Flush();
	}

	private void Publish(long renderGeneration)
	{
		if (_surface is null)
		{
			return;
		}
		var next = _sequence + 1;
		_surface.ReadPixels(_frameInfo, _connection.GetSlotPointer(next), _connection.Stride, 0, 0);
		_connection.PublishFrame(next, renderGeneration);
		_sequence = next;
	}
}
