#nullable enable

using System;
using CodeBrix.Platform.UI.AdvancedTextEdit.Contracts;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering.Internal;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Skia;

/// <summary>
/// The Skia platform's canvas supply for the AdvancedTextEdit add-in: the add-in's <see cref="RenderCanvas"/>, the software
/// present path (a pinned staging buffer drawn through a cached SKSurface, copied into a WriteableBitmap shown through
/// an ImageBrush background) the add-in has always drawn on.
/// </summary>
internal sealed class RenderCanvasSkiaPlatform : IRenderCanvasPlatform
{
	/// <inheritdoc />
	public FrameworkElement CreateRenderCanvas() => new RenderCanvas();

	/// <inheritdoc />
	public void AddPaintHandler(FrameworkElement renderCanvas, Action<object, Size> paint) =>
		((RenderCanvas)renderCanvas).Paint += (canvas, size) => paint(canvas, new Size(size.Width, size.Height));

	/// <inheritdoc />
	public void Invalidate(FrameworkElement renderCanvas) => ((RenderCanvas)renderCanvas).Invalidate();

	/// <inheritdoc />
	public double GetScale(FrameworkElement renderCanvas) => ((RenderCanvas)renderCanvas).Scale;
}
