#nullable enable

using System;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.PlotterView.Contracts;

/// <summary>
/// The PlotterView add-in's canvas supply: the element the add-in's controls draw on. The controls (in the Core assembly)
/// own the drawing - they paint their content on the canvas the element hands them - and ask the platform for the
/// element that supplies that canvas.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once, into a static field (<c>Internal.RenderCanvasSupply</c>), through
/// <see cref="PlatformContract.Resolve{TContract}"/>. Platform (Skia):
/// <c>CodeBrix.Platform.UI.PlotterView.Skia.RenderCanvasSkiaPlatform</c> in CodeBrix.Platform.UI.PlotterView, which supplies the add-in's software RenderCanvas (a pinned
/// staging buffer drawn through an SKSurface and presented through a WriteableBitmap).
/// </para>
/// </remarks>
internal interface IRenderCanvasPlatform
{
	/// <summary>
	/// Creates a drawing element. It paints once it is loaded and has a size, whenever it is invalidated.
	/// </summary>
	/// <returns>The element, to be added to the control's tree.</returns>
	FrameworkElement CreateRenderCanvas();

	/// <summary>
	/// Adds a paint handler to an element <see cref="CreateRenderCanvas"/> created.
	/// </summary>
	/// <param name="renderCanvas">The element.</param>
	/// <param name="paint">Draws the content: the platform's drawing canvas (on every current platform an SKCanvas,
	/// passed as an object), already scaled so that one canvas unit is one device-independent pixel, and the paintable
	/// size in device-independent pixels.</param>
	void AddPaintHandler(FrameworkElement renderCanvas, Action<object, Size> paint);

	/// <summary>
	/// Repaints an element <see cref="CreateRenderCanvas"/> created. Safe to call from any thread; the paint happens on
	/// the UI thread.
	/// </summary>
	/// <param name="renderCanvas">The element.</param>
	void Invalidate(FrameworkElement renderCanvas);

	/// <summary>
	/// The display scale factor the element's last paint used (1.0 = 96 dpi).
	/// </summary>
	/// <param name="renderCanvas">The element.</param>
	/// <returns>The scale factor.</returns>
	double GetScale(FrameworkElement renderCanvas);
}
