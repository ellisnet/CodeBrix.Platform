using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Graphics; //Was previously: Uno.UI.Graphics

internal class SKCanvasVisual(Action<object, Size> renderCallback, Compositor compositor) : SKCanvasVisualBase(renderCallback, compositor)
{
	internal override void Paint(in PaintingSession session)
	{
		// We save and restore the canvas state ourselves so that the inheritor doesn't accidentally forget to.
		session.Canvas.Save();
		// clipping here guarantees that drawing doesn't get outside the intended area
		session.Canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y), antialias: true);

		// The session's opacity is the one accumulated down the visual tree (the element's own
		// Opacity times its ancestors'). Every other visual applies it to what it paints; the
		// render callback draws whatever it likes straight onto the canvas, so the only way to
		// fade ITS output is to draw it into a layer and composite that layer with the opacity.
		// A layer costs an intermediate surface, so it is only taken when there is something to
		// fade - the common, fully opaque case still draws directly. (Opacity 0 never gets here:
		// Visual.Render skips an invisible visual before Paint is called.)
		// MEASURED (UIReqs Graphics2DSK, 2026-09-10): before this, Opacity 0.5 on an
		// SKCanvasElement left its drawing at full strength (100 % of the region pure blue).
		var fades = session.Opacity < 1.0f;
		if (fades)
		{
			using var layerPaint = new SKPaint { Color = SKColors.Black.WithAlpha((byte)Math.Round(session.Opacity * byte.MaxValue)) };
			session.Canvas.SaveLayer(layerPaint);
		}

		RenderCallback(session.Canvas, Size.ToSize());

		if (fades)
		{
			session.Canvas.Restore();
		}

		session.Canvas.Restore();
	}

	internal override bool CanPaint() => true;
	public override void Invalidate() => Compositor.InvalidateRender(this);
}
