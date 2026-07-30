#nullable enable

using System;
using System.Linq;

using Microsoft.UI.Xaml.Media;
using SkiaSharp;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Search;

//was previously: ICSharpCode.AvalonEdit/Search/SearchResultBackgroundRenderer.cs in the AvalonEdit
//repo (MIT). Draw paints the geometry-builder output (rounded outline path) with SKPaint per the
//port's drawing rules, and the WPF MarkerPen became the MarkerBorderBrush/MarkerBorderThickness
//pair.

sealed class SearchResultBackgroundRenderer : IBackgroundRenderer
{
	readonly TextSegmentCollection<SearchResult> currentResults = new TextSegmentCollection<SearchResult>();

	public TextSegmentCollection<SearchResult> CurrentResults {
		get { return currentResults; }
	}

	public KnownLayer Layer {
		get {
			// draw behind selection
			return KnownLayer.Selection;
		}
	}

	public SearchResultBackgroundRenderer()
	{
		MarkerBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 144, 238, 144)); // LightGreen
		MarkerBorderBrush = null;
		MarkerBorderThickness = 1.0;
		MarkerCornerRadius = 3.0;
	}

	public Brush? MarkerBrush { get; set; }

	public Brush? MarkerBorderBrush { get; set; }

	public double MarkerBorderThickness { get; set; }

	public double MarkerCornerRadius { get; set; }

	public void Draw(TextView textView, SKCanvas canvas)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));
		if (canvas == null)
			throw new ArgumentNullException(nameof(canvas));

		if (!textView.VisualLinesValid)
			return;

		var visualLines = textView.VisualLines;
		if (visualLines.Count == 0)
			return;

		int viewStart = visualLines.First().FirstDocumentLine.Offset;
		int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

		SKColor? markerColor = VisualLineElementTextRunProperties.GetSolidColor(MarkerBrush);
		SKColor? borderColor = VisualLineElementTextRunProperties.GetSolidColor(MarkerBorderBrush);
		double borderThickness = borderColor != null ? MarkerBorderThickness : 0;
		if (markerColor == null && (borderColor == null || borderThickness <= 0))
			return;
		double markerCornerRadius = MarkerCornerRadius;

		foreach (SearchResult result in currentResults.FindOverlappingSegments(viewStart, viewEnd - viewStart))
		{
			BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
			geoBuilder.AlignToWholePixels = true;
			geoBuilder.BorderThickness = borderThickness;
			geoBuilder.CornerRadius = markerCornerRadius;
			geoBuilder.AddSegment(textView, result);
			using SKPath? path = geoBuilder.CreatePath();
			if (path == null)
				continue;
			if (markerColor != null)
			{
				using var fillPaint = new SKPaint {
					Color = markerColor.Value,
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
}
