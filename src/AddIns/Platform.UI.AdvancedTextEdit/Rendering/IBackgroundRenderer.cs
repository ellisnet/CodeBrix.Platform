#nullable enable

using SkiaSharp;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/IBackgroundRenderer.cs in the AvalonEdit repo
//(MIT). Upstream drew through a WPF DrawingContext supplied by per-layer UIElements; in this
//port every layer is a draw phase on the text view's single Skia surface, so Draw receives the
//SKCanvas of the current paint pass (pre-scaled to device-independent pixels).

/// <summary>
/// Background renderers draw in the background of a known layer.
/// You can use background renderers to draw non-interactive decorations on the TextView
/// without introducing new visual elements.
/// </summary>
public interface IBackgroundRenderer
{
	/// <summary>
	/// Gets the layer on which this background renderer should draw.
	/// </summary>
	KnownLayer Layer { get; }

	/// <summary>
	/// Causes the background renderer to draw onto the text view's canvas for the current
	/// paint pass. The canvas is in device-independent pixels.
	/// </summary>
	void Draw(TextView textView, SKCanvas canvas);
}
