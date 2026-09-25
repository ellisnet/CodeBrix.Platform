using System;
using Windows.Foundation;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Graphics; //Was previously: Uno.UI.Graphics

/// <summary>
/// The platform-neutral base of a visual whose content is drawn by a render callback onto the platform's drawing
/// canvas: its lifecycle (it is created by the registered <see cref="SKCanvasVisualBaseFactory"/> and hooked into the
/// composition tree as a <see cref="ContainerVisual"/>), its size (<see cref="Visual.Size"/>) and its invalidation.
/// The platform part derives from it and owns the paint: on the Skia platform, <c>SKCanvasVisual</c>
/// (Graphics/SKCanvasVisual.skia.cs) calls the render callback with an <c>SKCanvas</c>, already clipped to the
/// visual's size and scaled to the target's rasterization scale.
/// </summary>
internal abstract class SKCanvasVisualBase : ContainerVisual
{
	/// <param name="renderCallback">Draws the content. The first parameter is the platform's drawing canvas (on the Skia platform, an <c>SKCanvas</c>); the second is the size to draw.</param>
	/// <param name="compositor">The compositor that owns the visual.</param>
	protected SKCanvasVisualBase(Action<object, Size> renderCallback, Compositor compositor) : base(compositor)
	{
		RenderCallback = renderCallback;
	}

	protected Action<object, Size> RenderCallback { get; }

	/// <summary>
	/// Requests a repaint of the visual: the render callback runs again on the next frame.
	/// </summary>
	public virtual void Invalidate() => Compositor.InvalidateRender(this);
}
