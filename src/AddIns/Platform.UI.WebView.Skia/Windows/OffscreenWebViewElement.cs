using System;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Graphics;
using SkiaSharp;

namespace CodeBrix.Platform.UI.WebView.Skia.Offscreen;

internal sealed class OffscreenWebViewElement(Compositor compositor) : FrameworkElement
{
    private SKCanvasVisualBase? _canvas;
    private SKImage? _frame;
    private readonly object _gate = new();

    private protected override ContainerVisual CreateElementVisual()
    {
        if (ApiExtensibility.CreateInstance<SKCanvasVisualBaseFactory>(this, out var factory))
            return _canvas = factory.CreateInstance((canvas, area) => Paint((SKCanvas)canvas, area), compositor);
        throw new InvalidOperationException("The offscreen WebView requires a Skia composition target.");
    }

    internal override bool IsViewHit() => true;

    internal void PresentFrame(SKImage frame)
    {
        lock (_gate) { _frame?.Dispose(); _frame = frame; }
        _canvas?.Invalidate();
    }

    internal void ClearFrame()
    {
        lock (_gate) { _frame?.Dispose(); _frame = null; }
        _canvas?.Invalidate();
    }

    private void Paint(SKCanvas canvas, Size area)
    {
        lock (_gate)
        {
            if (_frame is not null)
                canvas.DrawImage(_frame, new SKRect(0, 0, (float)area.Width, (float)area.Height),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        }
    }
}
