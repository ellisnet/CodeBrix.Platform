using System;
using System.Numerics;
using Windows.Foundation;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.UI.Composition.Skia;

namespace CodeBrix.Platform.UI.Graphics; //Was previously: Uno.UI.Graphics

/// <summary>
/// The Skia canvas visual that <see cref="SKCanvasVisualFactory"/> creates. Its platform state,
/// <see cref="CodeBrix.Platform.UI.Skia.SKCanvasVisualSkiaPlatform"/>, is registered for this type by the assembly's
/// <c>SkiaPlatformBootstrap</c> and paints it through <see cref="PaintContent"/>.
/// </summary>
internal class SKCanvasVisual(Action<object, Size> renderCallback, Compositor compositor) : SKCanvasVisualBase(renderCallback, compositor)
{
	/// <summary>
	/// Draws the render callback's content, clipped to the visual's size and faded by the session's opacity.
	/// </summary>
	/// <param name="session">The drawing session to use.</param>
	internal void PaintContent(in PaintingSession session)
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
}
