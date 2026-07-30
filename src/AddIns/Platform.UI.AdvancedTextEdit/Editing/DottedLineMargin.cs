#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Foundation;

using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering.Internal;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/DottedLineMargin.cs in the AvalonEdit repo (MIT),
//where Create() returned a WPF Line shape (StrokeDashArray 0/2, round caps, thickness 1) tagged
//for IsDottedLineMargin. This framework has no dash-styled Line shape, so Create() returns an
//internal element that draws the same dotted 1 DIP rule on a hosted RenderCanvas; the tag test
//became a type test. The editor control sets the element's Stroke brush (upstream bound
//Shape.Stroke to LineNumbersForeground).

/// <summary>
/// Margin for use with the text area.
/// A vertical dotted line to separate the line numbers from the text view.
/// </summary>
public static class DottedLineMargin
{
	/// <summary>
	/// Creates a vertical dotted line to separate the line numbers from the text view.
	/// </summary>
	public static UIElement Create()
	{
		return new DottedLineMarginElement();
	}

	/// <summary>
	/// Gets whether the specified UIElement is the result of a DottedLineMargin.Create call.
	/// </summary>
	public static bool IsDottedLineMargin(UIElement element)
	{
		return element is DottedLineMarginElement;
	}
}

/// <summary>
/// The element created by <see cref="DottedLineMargin.Create"/>: a 1 DIP wide vertical dotted
/// rule (dots of 1 DIP diameter every 2 DIPs, round caps) with a 2 DIP margin on either side.
/// </summary>
internal sealed partial class DottedLineMarginElement : Panel
{
	readonly RenderCanvas renderCanvas = new RenderCanvas();
	Brush? stroke;

	public DottedLineMarginElement()
	{
		Margin = new Thickness(2, 0, 2, 0);
		renderCanvas.Paint += RenderCanvasPaint;
		Children.Add(renderCanvas);
	}

	/// <summary>
	/// Gets/Sets the brush of the dotted rule; nothing is drawn while null.
	/// The editor control keeps this in sync with its line-numbers foreground.
	/// </summary>
	public Brush? Stroke {
		get { return stroke; }
		set {
			if (stroke != value)
			{
				stroke = value;
				renderCanvas.Invalidate();
			}
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		renderCanvas.Measure(availableSize);
		return new Size(1, 0);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		renderCanvas.Arrange(new Rect(new Point(0, 0), finalSize));
		renderCanvas.Invalidate();
		return finalSize;
	}

	void RenderCanvasPaint(SKCanvas canvas, SKSize size)
	{
		SKColor? color = VisualLineElementTextRunProperties.GetSolidColor(stroke);
		if (color == null)
			return;
		using var paint = new SKPaint { Color = color.Value, IsAntialias = true, Style = SKPaintStyle.Fill };
		float x = size.Width / 2;
		for (float y = 0.5f; y < size.Height; y += 2f)
		{
			canvas.DrawCircle(x, y, 0.5f, paint);
		}
	}
}
