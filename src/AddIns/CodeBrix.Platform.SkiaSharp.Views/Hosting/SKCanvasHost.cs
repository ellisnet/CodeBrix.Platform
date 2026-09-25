using System;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Graphics;
using Microsoft.UI.Composition;
using Windows.Foundation;

namespace SkiaSharp.Views.Windows;

/// <summary>
/// The Skia-canvas host seam (plan rule R7, decision P7): the SKCanvas-typed paint callback that drawing add-ins
/// build on, over the framework's Skia-agnostic canvas visual.
/// </summary>
/// <remarks>
/// <para>
/// The framework's Core assembly carries <see cref="SKCanvasVisualBase"/>, a composition visual whose content is drawn
/// by a render callback that receives the platform's drawing canvas as an <see cref="object"/>, and the contract
/// <see cref="SKCanvasVisualBaseFactory"/> that a platform registers to create it. The framework's Core never
/// references SkiaSharp, so it cannot type that canvas; this class is the one place that does. On CodeBrix.Platform the
/// factory is registered by the framework's Skia assembly (SKCanvasVisualFactory), whose visual clips to the visual's
/// size, applies the accumulated opacity and hands its paint callback the renderer's own SKCanvas, already scaled to
/// the rasterization scale. Android and Mobile register their own factory, backed by their SkiaSharp views.
/// </para>
/// <para>
/// A drawing add-in's Core references this assembly (and SkiaSharp) instead of the framework's Skia assembly. It either
/// derives its element from <see cref="SKCanvasHostElement"/>, or - when its element needs another base class or its
/// surface is supplied elsewhere - converts its paint method with <see cref="ToPlatformCallback"/>. It needs this
/// assembly's InternalsVisibleTo grant (InternalsVisibleTo.cs).
/// </para>
/// </remarks>
internal static class SKCanvasHost
{
	/// <summary>
	/// Whether the platform can host a Skia canvas: true when it registered an <see cref="SKCanvasVisualBaseFactory"/>.
	/// </summary>
	internal static bool IsSupported => ApiExtensibility.IsRegistered<SKCanvasVisualBaseFactory>();

	/// <summary>
	/// Wraps an SKCanvas-typed paint method as the platform's render callback, which receives the canvas as an
	/// <see cref="object"/>. Allocates once, when it is called; the returned callback allocates nothing per paint.
	/// </summary>
	/// <param name="paint">Draws the content: the canvas, and the size of the area to draw in device-independent pixels.</param>
	/// <returns>The callback to give to the platform.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="paint"/> is null.</exception>
	internal static Action<object, Size> ToPlatformCallback(Action<SKCanvas, Size> paint)
	{
		ArgumentNullException.ThrowIfNull(paint);

		return (canvas, area) => paint((SKCanvas)canvas, area);
	}

	/// <summary>
	/// Creates the visual that paints with <paramref name="paint"/>, through the platform's registered
	/// <see cref="SKCanvasVisualBaseFactory"/>. Call it from an element's <c>CreateElementVisual</c> override, once per
	/// element, and keep the visual to invalidate it.
	/// </summary>
	/// <param name="owner">The element the visual belongs to.</param>
	/// <param name="paint">Draws the content: the canvas, already clipped to the visual's size and scaled to the
	/// rasterization scale, and the size of the area to draw in device-independent pixels.</param>
	/// <returns>The visual; its <see cref="SKCanvasVisualBase.Invalidate"/> requests a repaint.</returns>
	/// <exception cref="InvalidOperationException">No <see cref="SKCanvasVisualBaseFactory"/> is registered.</exception>
	internal static SKCanvasVisualBase CreateVisual(object owner, Action<SKCanvas, Size> paint)
	{
		if (ApiExtensibility.CreateInstance<SKCanvasVisualBaseFactory>(owner, out var factory))
		{
			return factory.CreateInstance(ToPlatformCallback(paint), Compositor.GetSharedCompositor());
		}

		throw new InvalidOperationException($"Failed to create an instance of {nameof(SKCanvasVisualBase)}");
	}
}
