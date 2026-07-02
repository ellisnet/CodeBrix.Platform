using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Graphics;
using SkiaSharp;

namespace CodeBrix.Platform.UI.MediaPlayer.Skia;

/// <summary>
/// The element placed inside the MediaPlayerPresenter. It paints the most recent video frame
/// produced by the LibVLC memory sink directly into the Skia scene, so the video is composited
/// like any other XAML content (clipping, transforms, and z-order all behave normally - there
/// is no native airspace involved). The XAML Stretch mode is applied here, at paint time,
/// instead of asking VLC to scale/crop natively.
/// </summary>
internal sealed class VideoFrameHostElement : FrameworkElement
{
	private readonly Compositor _compositor;
	private SKCanvasVisualBase? _canvasVisual;
	private SKImage? _frame;
	private readonly object _frameGate = new();
	private Stretch _stretch = Stretch.Uniform;

	/// <param name="compositor">
	/// The shared compositor, obtained from an existing visual (the hosting presenter's)
	/// rather than Compositor.GetSharedCompositor, which is internal to the Composition assembly -
	/// this AddIn only holds InternalsVisibleTo grants from Platform.UI and Platform.UWP.
	/// </param>
	public VideoFrameHostElement(Compositor compositor)
	{
		_compositor = compositor;
	}

	private protected override ContainerVisual CreateElementVisual()
	{
		if (ApiExtensibility.CreateInstance<SKCanvasVisualBaseFactory>(this, out var factory))
		{
			return _canvasVisual = factory.CreateInstance((o, size) => PaintFrame((SKCanvas)o, size), _compositor);
		}

		throw new InvalidOperationException($"Failed to create an instance of {nameof(SKCanvasVisualBase)} - the media player requires a Skia composition target.");
	}

	internal override bool IsViewHit() => true;

	/// <summary>
	/// The stretch mode to apply when painting frames. Set from the UI thread by the presenter
	/// extension; takes effect on the next repaint.
	/// </summary>
	internal Stretch Stretch
	{
		get => _stretch;
		set
		{
			_stretch = value;
			_canvasVisual?.Invalidate();
		}
	}

	/// <summary>
	/// Publishes a new video frame and requests a repaint. Called on the UI thread; ownership
	/// of <paramref name="frame"/> transfers to this element (the previous frame is disposed).
	/// </summary>
	internal void PresentFrame(SKImage frame)
	{
		SKImage? previous;
		lock (_frameGate)
		{
			previous = _frame;
			_frame = frame;
		}
		previous?.Dispose();
		_canvasVisual?.Invalidate();
	}

	/// <summary>
	/// Drops the current frame (e.g. when the media source changes), so a stale picture is not
	/// left on screen.
	/// </summary>
	internal void ClearFrame()
	{
		SKImage? previous;
		lock (_frameGate)
		{
			previous = _frame;
			_frame = null;
		}
		previous?.Dispose();
		_canvasVisual?.Invalidate();
	}

	private void PaintFrame(SKCanvas canvas, Size area)
	{
		lock (_frameGate)
		{
			// Keep the reference alive while drawing: SKImage disposal only happens in
			// PresentFrame/ClearFrame under the same gate, so drawing inside the lock is the
			// simple, safe option (the draw is a fast blit of an already-rasterized image).
			if (_frame is { } frame)
			{
				var dest = ComputeDestinationRect(frame.Width, frame.Height, area, _stretch);
				canvas.Save();
				canvas.ClipRect(new SKRect(0, 0, (float)area.Width, (float)area.Height));
				canvas.Clear(SKColors.Black);
				canvas.DrawImage(frame, dest, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
				canvas.Restore();
				return;
			}
		}

		// No frame yet (nothing playing, or an audio-only media): leave the area blank.
		canvas.Clear(SKColors.Transparent);
	}

	private static SKRect ComputeDestinationRect(int frameWidth, int frameHeight, Size area, Stretch stretch)
	{
		var areaWidth = (float)area.Width;
		var areaHeight = (float)area.Height;

		if (frameWidth <= 0 || frameHeight <= 0)
		{
			return new SKRect(0, 0, areaWidth, areaHeight);
		}

		float scaleX = areaWidth / frameWidth;
		float scaleY = areaHeight / frameHeight;

		var (width, height) = stretch switch
		{
			Stretch.None => ((float)frameWidth, (float)frameHeight),
			Stretch.Fill => (areaWidth, areaHeight),
			Stretch.UniformToFill => Scaled(Math.Max(scaleX, scaleY)),
			_ => Scaled(Math.Min(scaleX, scaleY)), // Stretch.Uniform (the default)
		};

		var left = (areaWidth - width) / 2;
		var top = (areaHeight - height) / 2;
		return new SKRect(left, top, left + width, top + height);

		(float, float) Scaled(float scale) => (frameWidth * scale, frameHeight * scale);
	}
}
