#nullable enable

using System;

using SkiaSharp;

using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/SelectionLayer.cs in the AvalonEdit repo (MIT),
//a UIElement Layer whose OnRender drew a WPF geometry. In this port the selection layer is an
//IBackgroundRenderer draw phase on the text view's paint pass: the geometry comes from
//BackgroundGeometryBuilder (rectangles merged into an outline path, rounded when
//TextArea.SelectionCornerRadius > 0) and is filled/stroked with SKPaint using the text area's
//SelectionBrush and SelectionBorderBrush/-Thickness (the WPF SelectionBorder Pen equivalent).
//The upstream weak-event subscriptions (VisualLinesChanged/ScrollOffsetChanged -> re-render)
//are unnecessary: the paint pass re-runs this renderer on every repaint.

sealed class SelectionLayer : IBackgroundRenderer
{
	readonly TextArea textArea;

	public SelectionLayer(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		this.textArea = textArea;
	}

	public KnownLayer Layer {
		get { return KnownLayer.Selection; }
	}

	public void Draw(TextView textView, SKCanvas canvas)
	{
		SKColor? fillColor = VisualLineElementTextRunProperties.GetSolidColor(textArea.SelectionBrush);
		SKColor? borderColor = VisualLineElementTextRunProperties.GetSolidColor(textArea.SelectionBorderBrush);
		double borderThickness = borderColor != null ? textArea.SelectionBorderThickness : 0;
		if (fillColor == null && (borderColor == null || borderThickness <= 0))
			return;

		BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
		geoBuilder.AlignToWholePixels = true;
		geoBuilder.BorderThickness = borderThickness;
		geoBuilder.ExtendToFullWidthAtLineEnd = textArea.Selection.EnableVirtualSpace;
		geoBuilder.CornerRadius = textArea.SelectionCornerRadius;
		foreach (var segment in textArea.Selection.Segments)
		{
			geoBuilder.AddSegment(textView, segment);
		}
		using SKPath? path = geoBuilder.CreatePath();
		if (path == null)
			return;

		if (fillColor != null)
		{
			using var fillPaint = new SKPaint {
				Color = fillColor.Value,
				Style = SKPaintStyle.Fill,
				IsAntialias = true,
			};
			canvas.DrawPath(path, fillPaint);
		}
		if (borderColor != null && borderThickness > 0)
		{
			using var borderPaint = new SKPaint {
				Color = borderColor.Value,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = (float)borderThickness,
				IsAntialias = true,
			};
			canvas.DrawPath(path, borderPaint);
		}
	}
}
