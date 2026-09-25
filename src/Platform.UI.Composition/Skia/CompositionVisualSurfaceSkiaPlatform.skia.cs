#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// Paints a <see cref="CompositionVisualSurface"/> (its source visual, rendered as a root, clipped to the surface
/// size) for the <see cref="CompositionSurfaceBrush"/> that shows it. The surface itself keeps no platform state.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionVisualSurface.skia.cs</c> (as its <c>ISkiaSurface</c>
/// implementation), moved verbatim.</remarks>
internal static class CompositionVisualSurfaceSkiaPlatform
{
	/// <summary>
	/// Paints <paramref name="surface"/> onto <paramref name="canvas"/>.
	/// </summary>
	/// <param name="surface">The surface.</param>
	/// <param name="canvas">The canvas.</param>
	/// <param name="opacity">The accumulated opacity (unused: the source visual carries its own).</param>
	internal static void Paint(CompositionVisualSurface surface, SKCanvas canvas, float opacity)
	{
		if (surface.SourceVisual is { } sourceVisual)
		{
			int save = canvas.Save();
			// Note that this is applied before the SourceOffset translates the canvas' matrix, so
			canvas.ClipRect(new SKRect(0, 0, GetSize(surface).X, GetSize(surface).X), antialias: true);

			VisualSkiaPlatform.Of(sourceVisual).RenderRootVisual(canvas, surface.SourceOffset);
			canvas.RestoreToCount(save);
		}
	}

	/// <summary>
	/// Returns the size of <paramref name="surface"/>: its source size, else its source visual's size, else 1000x1000.
	/// </summary>
	/// <param name="surface">The surface.</param>
	/// <returns>The size.</returns>
	internal static Vector2 GetSize(CompositionVisualSurface surface) => surface.SourceSize switch
	{
		{ X: > 0.0f, Y: > 0.0f } => surface.SourceSize,
		_ => surface.SourceVisual switch
		{
			{ Size: { X: > 0.0f, Y: > 0.0f } } => surface.SourceVisual.Size,
			_ => new Vector2(1000, 1000)
		}
	};
}
