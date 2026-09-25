namespace SkiaSharp.Views.Windows.Contracts;

/// <summary>
/// The per-element platform hook of an <see cref="SKXamlCanvas"/>: the surface the canvas paints into and how the
/// painted pixels reach the screen. The element (in the Core assembly) owns the API - its size, DPI, visibility,
/// load state and the <see cref="SKXamlCanvas.PaintSurface"/> event - and calls this hook to repaint and to release
/// the surface. One hook is created per canvas, in its constructor, through <see cref="PlatformContract.Create{TContract}"/>.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Platform (Skia): <c>SkiaSharp.Views.Windows.Skia.SKXamlCanvasSkiaPlatform</c> in CodeBrix.Platform.SkiaSharp.Views
/// paints into a pixel buffer through a cached SKSurface and presents it through a WriteableBitmap shown by an
/// ImageBrush background, which the framework's Skia renderer composites.
/// </para>
/// </remarks>
internal interface ISKXamlCanvasPlatform
{
	/// <summary>
	/// Repaints the canvas now: sizes the surface to the element, raises the paint through the element and presents
	/// the result. Called on the UI thread (the element dispatches when it is called from another thread).
	/// </summary>
	void Invalidate();

	/// <summary>
	/// Releases the surface and the presented bitmap; called when the element is unloaded. The next
	/// <see cref="Invalidate"/> creates them again.
	/// </summary>
	void Unload();
}
