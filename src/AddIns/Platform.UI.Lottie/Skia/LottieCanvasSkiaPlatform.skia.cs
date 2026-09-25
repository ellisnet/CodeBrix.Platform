using System;
using CodeBrix.Platform.UI.Lottie.Contracts;
using CodeBrix.Platform.WinUI.Graphics2DSK;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Lottie.Skia;

/// <summary>
/// The Skia platform's canvas supply for Lottie animations: a Graphics2DSK <see cref="SKCanvasElement"/>, whose canvas
/// visual the framework's Skia renderer composites, clipped to the element and scaled to the rasterization scale.
/// </summary>
internal sealed class LottieCanvasSkiaPlatform : ILottieCanvasPlatform
{
	/// <inheritdoc />
	public UIElement CreateRenderSurface(Action<object, Size> render) => new LottieSKCanvasElement(render);

	/// <inheritdoc />
	public void Invalidate(UIElement renderSurface) => ((SKCanvasElement)renderSurface).Invalidate();

	//was previously: LottieVisualSourceBase.LottieSKCanvasElement (LottieVisualSource.Skottie.cs, __SKIA__ only), which
	//called the source's OnRenderOverride directly; the source now hands its render callback over.
	private sealed partial class LottieSKCanvasElement(Action<object, Size> render) : SKCanvasElement
	{
		protected override void RenderOverride(SKCanvas canvas, Size area) => render(canvas, area);
	}
}
