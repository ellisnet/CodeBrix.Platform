#nullable enable

using System;
using CodeBrix.Platform.UI.TerminalView.Contracts;
using Microsoft.UI.Xaml;
using SkiaSharp;

namespace CodeBrix.Platform.UI.TerminalView.Internal;

/// <summary>
/// The add-in's drawing surfaces, through the platform's canvas supply (<see cref="IRenderCanvasPlatform"/>, resolved
/// once for the process on first use). A surface is an ordinary <see cref="FrameworkElement"/> for layout and input;
/// only its creation, its paint callback, its repaint and its scale go through the platform.
/// </summary>
internal static class RenderCanvasSupply
{
	private static IRenderCanvasPlatform? _platform;

	private static IRenderCanvasPlatform Platform => _platform ??= PlatformContract.Resolve<IRenderCanvasPlatform>();

	/// <summary>Creates a drawing surface. Painting starts once it is loaded and has a size.</summary>
	/// <returns>The surface.</returns>
	internal static FrameworkElement Create() => Platform.CreateRenderCanvas();

	/// <summary>
	/// Adds a paint handler, called on each repaint with the surface's canvas, already scaled so that one canvas unit
	/// is one device-independent pixel, and the paintable size in device-independent pixels.
	/// </summary>
	/// <param name="renderCanvas">A surface <see cref="Create"/> made.</param>
	/// <param name="paint">The handler.</param>
	internal static void AddPaintHandler(FrameworkElement renderCanvas, Action<SKCanvas, SKSize> paint) =>
		Platform.AddPaintHandler(
			renderCanvas,
			(canvas, size) => paint((SKCanvas)canvas, new SKSize((float)size.Width, (float)size.Height)));

	/// <summary>Repaints a surface. Safe to call from any thread; the paint happens on the UI thread.</summary>
	/// <param name="renderCanvas">A surface <see cref="Create"/> made.</param>
	internal static void Invalidate(FrameworkElement renderCanvas) => Platform.Invalidate(renderCanvas);

	/// <summary>The display scale factor the surface's last paint used (1.0 = 96 dpi).</summary>
	/// <param name="renderCanvas">A surface <see cref="Create"/> made.</param>
	/// <returns>The scale factor.</returns>
	internal static double GetScale(FrameworkElement renderCanvas) => Platform.GetScale(renderCanvas);
}
