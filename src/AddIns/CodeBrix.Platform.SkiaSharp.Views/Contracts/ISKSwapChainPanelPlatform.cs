using Windows.Foundation;

namespace SkiaSharp.Views.Windows.Contracts;

/// <summary>
/// The per-element platform hook of an <see cref="SKSwapChainPanel"/>: the hardware-accelerated (GPU) surface
/// behind the panel. The element (in the Core assembly) owns the API; this hook supplies what depends on the
/// platform's GPU support. One hook is created per panel, in its constructor, through
/// <see cref="PlatformContract.Create{TContract}"/>.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Platform (Skia): <c>SkiaSharp.Views.Windows.Skia.SKSwapChainPanelSkiaPlatform</c> in
/// CodeBrix.Platform.SkiaSharp.Views. The Skia heads have no swap chain, so the panel is not supported there: while
/// <see cref="SKSwapChainPanel.RaiseOnUnsupported"/> is true the constructor and the two surface properties throw
/// <see cref="System.NotSupportedException"/>, otherwise the panel is inert.
/// </para>
/// </remarks>
internal interface ISKSwapChainPanelPlatform
{
	/// <summary>
	/// Runs at the end of the panel's constructor and sets the panel up on this platform.
	/// </summary>
	void OnConstructed();

	/// <summary>
	/// The size of the drawing surface in pixels (the element's <c>CanvasSize</c>).
	/// </summary>
	/// <returns>The surface size; empty when there is no surface.</returns>
	Size GetCanvasSize();

	/// <summary>
	/// The GPU context the surface draws with (the element's <c>GRContext</c>), as an opaque handle: a
	/// <c>SkiaSharp.GRContext</c>, or null when there is none.
	/// </summary>
	/// <returns>The context, or null.</returns>
	object GetGRContext();

	/// <summary>
	/// Requests a repaint of the surface (the element dispatches the call to the UI thread).
	/// </summary>
	void Invalidate();
}
