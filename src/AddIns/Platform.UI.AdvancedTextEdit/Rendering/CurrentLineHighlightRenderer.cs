#nullable enable

using System;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/CurrentLineHighlightRenderer.cs in the
//AvalonEdit repo (MIT). The WPF BorderPen became a BorderBrush + BorderThickness pair, and Draw
//fills/strokes the BackgroundGeometryBuilder's SKPath with SKPaints on the text view's SKCanvas
//instead of drawing a Geometry through a DrawingContext.

sealed class CurrentLineHighlightRenderer : IBackgroundRenderer
{
	#region Fields

	int line;
	readonly TextView textView;

	/// <summary>
	/// The default fill color of the current-line highlight.
	/// </summary>
	public static readonly global::Windows.UI.Color DefaultBackground = global::Windows.UI.Color.FromArgb(22, 20, 220, 224);

	/// <summary>
	/// The default border color of the current-line highlight.
	/// </summary>
	public static readonly global::Windows.UI.Color DefaultBorder = global::Windows.UI.Color.FromArgb(52, 0, 255, 110);

	#endregion

	#region Properties

	public int Line
	{
		get { return this.line; }
		set
		{
			if (this.line != value)
			{
				this.line = value;
				this.textView.InvalidateLayer(this.Layer);
			}
		}
	}

	public KnownLayer Layer
	{
		get { return KnownLayer.Selection; }
	}

	public Brush? BackgroundBrush { get; set; }

	public Brush? BorderBrush { get; set; }

	public double BorderThickness { get; set; }

	#endregion

	public CurrentLineHighlightRenderer(TextView textView)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));

		this.BorderBrush = new SolidColorBrush(DefaultBorder);
		this.BorderThickness = 1;

		this.BackgroundBrush = new SolidColorBrush(DefaultBackground);

		this.textView = textView;
		this.textView.BackgroundRenderers.Add(this);

		this.line = 0;
	}

	public void Draw(TextView textView, SKCanvas canvas)
	{
		if (!this.textView.Options.HighlightCurrentLine)
			return;

		BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();

		VisualLine? visualLine = this.textView.GetVisualLine(line);
		if (visualLine == null)
			return;

		double linePosY = visualLine.VisualTop - this.textView.VerticalOffset;

		builder.AddRectangle(textView, new Rect(0, linePosY, textView.ActualWidth, visualLine.Height));

		using SKPath? geometry = builder.CreatePath();
		if (geometry == null)
			return;

		SKColor? backgroundColor = VisualLineElementTextRunProperties.GetSolidColor(this.BackgroundBrush);
		if (backgroundColor != null)
		{
			using var fillPaint = new SKPaint
			{
				Color = backgroundColor.Value,
				Style = SKPaintStyle.Fill,
				IsAntialias = true,
			};
			canvas.DrawPath(geometry, fillPaint);
		}

		SKColor? borderColor = VisualLineElementTextRunProperties.GetSolidColor(this.BorderBrush);
		if (borderColor != null && this.BorderThickness > 0)
		{
			using var strokePaint = new SKPaint
			{
				Color = borderColor.Value,
				Style = SKPaintStyle.Stroke,
				StrokeWidth = (float)this.BorderThickness,
				IsAntialias = true,
			};
			canvas.DrawPath(geometry, strokePaint);
		}
	}
}
