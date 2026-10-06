using SkiaSharp.Views.Windows.Contracts;

namespace SkiaSharp.Views.Windows;

//was previously: the constructor, DoInvalidate and DoUnloaded were in SKXamlCanvas.Skia.cs together with the whole
//present path. The present path is platform code and moved to the Skia assembly behind ISKXamlCanvasPlatform
//(Skia/SKXamlCanvasSkiaPlatform.skia.cs); this file connects the vendored shared half to it.
public partial class SKXamlCanvas
{
	//The platform's surface for this canvas; created before Initialize(), whose first DPI update already repaints.
	private readonly ISKXamlCanvasPlatform _platform;

	/// <summary>Initializes a new instance of the <see cref="SKXamlCanvas" /> class.</summary>
	/// <remarks>The canvas paints through the running platform's SkiaSharp views implementation, which the
	/// CodeBrix.Platform.SkiaSharp.Views package provides for the Skia-based platforms.</remarks>
	public SKXamlCanvas()
	{
		_platform = PlatformContract.Create<ISKXamlCanvasPlatform>(this);

		Initialize();
	}

	partial void DoUnloaded() =>
		_platform.Unload();

	private void DoInvalidate() =>
		_platform.Invalidate();

	/// <summary>Whether the canvas was created by a designer, which never paints.</summary>
	internal static bool IsInDesignMode => designMode;

	/// <summary>Whether the canvas is visible (its Visibility, tracked through the proxy binding).</summary>
	internal bool IsShown => isVisible;

	/// <summary>Sets <see cref="CanvasSize"/>, the size the last paint reported to the application.</summary>
	/// <param name="size">The user-visible size of the last paint.</param>
	internal void SetCanvasSize(SKSize size) => CanvasSize = size;

	/// <summary>The pixel size of the surface for the element's current size and DPI.</summary>
	/// <param name="unscaledSize">The element's size in device-independent pixels.</param>
	/// <param name="dpi">The display scale used.</param>
	/// <returns>The surface size in pixels; empty when the element has no positive size.</returns>
	internal SKSizeI GetSurfaceSize(out SKSizeI unscaledSize, out float dpi) => CreateSize(out unscaledSize, out dpi);

	/// <summary>Raises the paint through the element's overridable <see cref="OnPaintSurface"/>.</summary>
	/// <param name="e">The surface and its image info.</param>
	internal void RaisePaintSurface(SKPaintSurfaceEventArgs e) => OnPaintSurface(e);
}
