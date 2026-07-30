#nullable enable

using System;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/ColumnRulerRenderer.cs in the AvalonEdit repo
//(MIT). The WPF Pen became a brush + thickness pair drawn with an SKPaint, and Draw paints onto
//the text view's SKCanvas instead of a DrawingContext; the column math and the pixel alignment
//are unchanged.

/// <summary>
/// Renders a ruler at a certain column.
/// </summary>
sealed class ColumnRulerRenderer : IBackgroundRenderer
{
	Brush? brush;
	double thickness;
	int column;
	readonly TextView textView;

	/// <summary>
	/// The default ruler color (light gray).
	/// </summary>
	public static readonly global::Windows.UI.Color DefaultForeground = global::Windows.UI.Color.FromArgb(255, 211, 211, 211);

	public ColumnRulerRenderer(TextView textView)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));

		this.brush = new SolidColorBrush(DefaultForeground);
		this.thickness = 1;
		this.textView = textView;
		this.textView.BackgroundRenderers.Add(this);
	}

	public KnownLayer Layer
	{
		get { return KnownLayer.Background; }
	}

	public void SetRuler(int column, Brush? brush, double thickness)
	{
		if (this.column != column)
		{
			this.column = column;
			textView.InvalidateLayer(this.Layer);
		}
		if (this.brush != brush || this.thickness != thickness)
		{
			this.brush = brush;
			this.thickness = thickness;
			textView.InvalidateLayer(this.Layer);
		}
	}

	public void Draw(TextView textView, SKCanvas canvas)
	{
		if (column < 1)
			return;
		SKColor? color = VisualLineElementTextRunProperties.GetSolidColor(brush);
		if (color == null || thickness <= 0)
			return;
		double offset = textView.WideSpaceWidth * column;
		Size pixelSize = PixelSnapHelpers.GetPixelSize(textView);
		double markerXPos = PixelSnapHelpers.PixelAlign(offset, pixelSize.Width);
		markerXPos -= textView.HorizontalOffset;
		float startY = 0;
		float endY = (float)Math.Max(textView.DocumentHeight, textView.ActualHeight);

		using var paint = new SKPaint
		{
			Color = color.Value,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = (float)thickness,
		};
		canvas.DrawLine((float)markerXPos, startY, (float)markerXPos, endY, paint);
	}
}
