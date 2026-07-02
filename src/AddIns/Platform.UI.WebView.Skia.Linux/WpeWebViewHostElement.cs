using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Graphics;
using SkiaSharp;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux;

/// <summary>
/// The element placed inside the WebView2 control's template root. It paints the most recent
/// frame produced by the offscreen WPE WebKit view directly into the Skia scene, so the web
/// content is composited like any other XAML content (clipping, transforms, and z-order all
/// behave normally - there is no native airspace involved).
/// </summary>
internal sealed class WpeWebViewHostElement : FrameworkElement
{
	private readonly Compositor _compositor;
	private SKCanvasVisualBase? _canvasVisual;
	private SKImage? _frame;
	private readonly object _frameGate = new();

	/// <param name="compositor">
	/// The shared compositor, obtained from an existing visual (the hosting ContentPresenter's)
	/// rather than Compositor.GetSharedCompositor, which is internal to the Composition assembly -
	/// this AddIn only holds InternalsVisibleTo grants from Platform.UI and Platform.UWP.
	/// </param>
	public WpeWebViewHostElement(Compositor compositor)
	{
		_compositor = compositor;
	}

	private protected override ContainerVisual CreateElementVisual()
	{
		if (ApiExtensibility.CreateInstance<SKCanvasVisualBaseFactory>(this, out var factory))
		{
			return _canvasVisual = factory.CreateInstance((o, size) => PaintFrame((SKCanvas)o, size), _compositor);
		}

		throw new InvalidOperationException($"Failed to create an instance of {nameof(SKCanvasVisualBase)} - the WPE WebView requires a Skia composition target.");
	}

	internal override bool IsViewHit() => true;

	/// <summary>
	/// Publishes a new web-content frame and requests a repaint. May be called from any thread;
	/// ownership of <paramref name="frame"/> transfers to this element (the previous frame is disposed).
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

	private void PaintFrame(SKCanvas canvas, Size area)
	{
		SKImage? frame;
		lock (_frameGate)
		{
			frame = _frame;
			// Keep the reference alive while drawing: SKImage disposal only happens in
			// PresentFrame under the same gate, so drawing inside the lock is the simple,
			// safe option (the draw is a fast blit of an already-rasterized image).
			if (frame is not null)
			{
				canvas.DrawImage(frame, new SKRect(0, 0, (float)area.Width, (float)area.Height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
			}
		}

		if (frame is null)
		{
			// No frame yet (engine still starting or page not painted): leave the area blank.
			canvas.Clear(SKColors.Transparent);
		}
	}
}
