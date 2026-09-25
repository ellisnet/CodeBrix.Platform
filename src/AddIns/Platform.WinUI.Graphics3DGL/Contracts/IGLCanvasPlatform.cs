using Microsoft.UI.Composition;

namespace CodeBrix.Platform.WinUI.Graphics3DGL.Contracts;

/// <summary>
/// The platform's part of a <see cref="GLCanvasElement"/>: the composition visual the element is drawn with. The
/// element itself (its OpenGL context from the head's <c>INativeOpenGLWrapper</c>, its off-screen framebuffer, its
/// render scheduling and the bitmap it reads the pixels back into) is platform-neutral; how its visual takes part in
/// the platform's painting is not.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The visual returned must call <see cref="GLCanvasElement.OnVisualPainting"/> each time it paints, before it
/// paints: that is how a render asked for by <see cref="GLCanvasElement.Invalidate"/> gets queued. Resolved once for
/// the process (a static field of <see cref="GLCanvasElement"/>), on the first element's visual.
/// </para>
/// </remarks>
internal interface IGLCanvasPlatform
{
	/// <summary>Creates the composition visual of <paramref name="owner"/>.</summary>
	/// <remarks>
	/// A border visual, as for every panel: the element is a <see cref="Microsoft.UI.Xaml.Controls.Grid"/>, whose
	/// background (the brush carrying the rendered bitmap) is drawn by its border visual.
	/// </remarks>
	/// <param name="owner">The element the visual draws.</param>
	/// <param name="compositor">The shared compositor.</param>
	/// <returns>The element's visual.</returns>
	BorderVisual CreateVisual(GLCanvasElement owner, Compositor compositor);
}
