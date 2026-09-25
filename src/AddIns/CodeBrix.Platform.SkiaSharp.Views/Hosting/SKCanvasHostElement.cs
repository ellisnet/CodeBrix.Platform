using System;
using CodeBrix.Platform.UI.Graphics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace SkiaSharp.Views.Windows;

/// <summary>
/// The element half of the Skia-canvas host seam (see <see cref="SKCanvasHost"/>): a <see cref="FrameworkElement"/>
/// whose content is drawn with an <see cref="SKCanvas"/> by <see cref="OnPaint"/>. It owns its drawing surface (the
/// platform's canvas visual, created with the element), reports the paint area's size to every paint and the display
/// scale through <see cref="CanvasScale"/>, and repaints on <see cref="Invalidate"/>.
/// </summary>
/// <remarks>
/// It behaves as the Graphics2DSK add-in's public SKCanvasElement does - the same visual, the same hit testing, the
/// same "not supported" check - without making the drawing add-ins that build on it depend on that package.
/// </remarks>
internal abstract partial class SKCanvasHostElement : FrameworkElement
{
	private SKCanvasVisualBase _canvasVisual;

	/// <summary>
	/// Creates the element.
	/// </summary>
	/// <exception cref="PlatformNotSupportedException">The platform registered no Skia canvas visual.</exception>
	protected SKCanvasHostElement()
	{
		if (!SKCanvasHost.IsSupported)
		{
			throw new PlatformNotSupportedException(
				$"This platform does not support {nameof(SKCanvasHostElement)}: no {nameof(SKCanvasVisualBaseFactory)} is registered.");
		}
	}

	private protected override ContainerVisual CreateElementVisual() =>
		_canvasVisual = SKCanvasHost.CreateVisual(this, OnPaint);

	internal override bool IsViewHit() => true;

	/// <summary>
	/// The display scale the canvas is rasterized at (1.0 = 96 dpi). Paint code does not need it to draw - the canvas
	/// is already scaled, so one canvas unit is one device-independent pixel - only to pick pixel-exact detail.
	/// </summary>
	internal double CanvasScale => XamlRoot?.RasterizationScale ?? 1d;

	/// <summary>
	/// Requests a repaint: <see cref="OnPaint"/> runs again on the next frame.
	/// </summary>
	internal void Invalidate() => _canvasVisual?.Invalidate();

	/// <summary>
	/// Draws the element's content.
	/// </summary>
	/// <param name="canvas">The canvas, with the origin at the element's top-left corner, clipped to the element's size
	/// and scaled so that one canvas unit is one device-independent pixel.</param>
	/// <param name="area">The size of the area to draw, in device-independent pixels.</param>
	protected abstract void OnPaint(SKCanvas canvas, Size area);
}
