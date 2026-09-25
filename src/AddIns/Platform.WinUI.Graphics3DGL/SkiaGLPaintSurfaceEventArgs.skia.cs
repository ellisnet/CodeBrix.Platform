using System;
using SkiaSharp;

namespace CodeBrix.Platform.WinUI.Graphics3DGL; //Was previously: Uno.WinUI.Graphics3DGL

/// <summary>
/// Carries the GPU-backed <see cref="SKSurface"/> (and the <see cref="GRContext"/> it lives on) for a
/// single <see cref="SkiaGLCanvasElement.PaintSurface"/> frame. Draw into <see cref="Surface"/>'s
/// canvas; the surface's GPU pixels are read back and shown on screen after the handler returns.
/// </summary>
public sealed class SkiaGLPaintSurfaceEventArgs : EventArgs
{
	/// <summary>Creates the event arguments for one GPU-Skia paint.</summary>
	/// <param name="surface">The GPU-backed surface to draw into.</param>
	/// <param name="context">The GPU context the surface lives on.</param>
	/// <param name="info">The pixel geometry (size and color type) of the surface.</param>
	public SkiaGLPaintSurfaceEventArgs(SKSurface surface, GRContext context, SKImageInfo info)
	{
		Surface = surface;
		Context = context;
		Info = info;
	}

	/// <summary>The GPU-backed surface to draw into. The GL context is current for the duration of the event.</summary>
	public SKSurface Surface { get; }

	/// <summary>The GPU context <see cref="Surface"/> is created on.</summary>
	public GRContext Context { get; }

	/// <summary>The pixel geometry (size and color type) of <see cref="Surface"/>.</summary>
	public SKImageInfo Info { get; }
}
