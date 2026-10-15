using System;
using SkiaSharp;

namespace CodeBrix.Platform.UI.WebGpu;

/// <summary>
/// Event args for <see cref="WebGpuRenderView.Overlay"/>. Raised while the head paints the view (not on the frame's thread); the
/// view reuses ONE instance for every paint, so keep nothing (the canvas in particular is valid only for the call).
/// </summary>
public sealed class WebGpuOverlayEventArgs : EventArgs
{
	internal WebGpuOverlayEventArgs()
	{
		Canvas = null!;
	}

	/// <summary>The canvas, clipped to the view, in the view's effective-pixel coordinates. Valid only for the call.</summary>
	public SKCanvas Canvas { get; internal set; }

	/// <summary>The view's size in effective pixels.</summary>
	public SKSize Size { get; internal set; }
}
