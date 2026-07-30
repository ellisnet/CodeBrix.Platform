#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Foundation;

using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering.Internal;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Folding;

//was previously: ICSharpCode.AvalonEdit/Folding/FoldingMargin.cs plus FoldingMarginMarker.cs in
//the AvalonEdit repo (MIT). The fold-line calculation is transliterated. Re-expressions:
//- The margin draws on a hosted RenderCanvas (child 0, the LineNumberMargin pattern) instead of
//  OnRender; the WPF Pens became (color, 1 DIP) pairs derived from the brush properties.
//- FoldingMarginMarker is no longer a UIElement child: the marker boxes are plain records with a
//  hit-test rectangle, drawn by this margin's paint pass; pointer press toggles the section and
//  pointer move/exit tracks the hovered section (the WPF IsMouseDirectlyOver equivalent).
//- The four brush properties were attached inheritable properties (settable on the editor and
//  inherited down the tree); this framework has no property-value inheritance for them, so they
//  are plain dependency properties here (the static Get/Set accessors are gone) and the editor
//  control pushes its values down.
//- The margin has no inherited font size (Panel base); the marker/margin size derives from the
//  text view's font size.

/// <summary>
/// A margin that shows markers for foldings and allows to expand/collapse the foldings.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class FoldingMargin : AbstractMargin
{
	/// <summary>
	/// Gets/Sets the folding manager from which the foldings should be shown.
	/// </summary>
	public FoldingManager? FoldingManager { get; set; }

	internal const double SizeFactor = Constants.PixelPerPoint;

	const double MarginSizeFactor = 0.7;

	/// <summary>
	/// The stroke thickness of the fold lines and marker boxes, in device-independent pixels.
	/// </summary>
	//was previously: the frozen WPF Pens were created with thickness 1.
	const float FoldLineThickness = 1f;

	readonly RenderCanvas renderCanvas = new RenderCanvas();

	/// <summary>
	/// Creates a new FoldingMargin instance.
	/// </summary>
	public FoldingMargin()
	{
		renderCanvas.Paint += RenderCanvasPaint;
		Children.Add(renderCanvas);

		//was previously: a HitTestCore override accepted clicks on the transparent background.
		Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 255, 255, 255));

		PointerPressed += FoldingMargin_PointerPressed;
		PointerMoved += FoldingMargin_PointerMoved;
		PointerExited += FoldingMargin_PointerExited;
	}

	#region Brushes
	static Brush CreateSolidBrush(byte r, byte g, byte b)
	{
		return new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, r, g, b));
	}

	/// <summary>
	/// FoldingMarkerBrush dependency property.
	/// </summary>
	public static readonly DependencyProperty FoldingMarkerBrushProperty =
		DependencyProperty.Register(nameof(FoldingMarkerBrush), typeof(Brush), typeof(FoldingMargin),
									new PropertyMetadata(CreateSolidBrush(128, 128, 128), OnUpdateBrushes));

	/// <summary>
	/// Gets/sets the Brush used for displaying the lines of folding markers.
	/// </summary>
	public Brush? FoldingMarkerBrush {
		get { return (Brush?)GetValue(FoldingMarkerBrushProperty); }
		set { SetValue(FoldingMarkerBrushProperty, value); }
	}

	/// <summary>
	/// FoldingMarkerBackgroundBrush dependency property.
	/// </summary>
	public static readonly DependencyProperty FoldingMarkerBackgroundBrushProperty =
		DependencyProperty.Register(nameof(FoldingMarkerBackgroundBrush), typeof(Brush), typeof(FoldingMargin),
									new PropertyMetadata(CreateSolidBrush(255, 255, 255), OnUpdateBrushes));

	/// <summary>
	/// Gets/sets the Brush used for displaying the background of folding markers.
	/// </summary>
	public Brush? FoldingMarkerBackgroundBrush {
		get { return (Brush?)GetValue(FoldingMarkerBackgroundBrushProperty); }
		set { SetValue(FoldingMarkerBackgroundBrushProperty, value); }
	}

	/// <summary>
	/// SelectedFoldingMarkerBrush dependency property.
	/// </summary>
	public static readonly DependencyProperty SelectedFoldingMarkerBrushProperty =
		DependencyProperty.Register(nameof(SelectedFoldingMarkerBrush), typeof(Brush), typeof(FoldingMargin),
									new PropertyMetadata(CreateSolidBrush(0, 0, 0), OnUpdateBrushes));

	/// <summary>
	/// Gets/sets the Brush used for displaying the lines of selected folding markers.
	/// </summary>
	public Brush? SelectedFoldingMarkerBrush {
		get { return (Brush?)GetValue(SelectedFoldingMarkerBrushProperty); }
		set { SetValue(SelectedFoldingMarkerBrushProperty, value); }
	}

	/// <summary>
	/// SelectedFoldingMarkerBackgroundBrush dependency property.
	/// </summary>
	public static readonly DependencyProperty SelectedFoldingMarkerBackgroundBrushProperty =
		DependencyProperty.Register(nameof(SelectedFoldingMarkerBackgroundBrush), typeof(Brush), typeof(FoldingMargin),
									new PropertyMetadata(CreateSolidBrush(255, 255, 255), OnUpdateBrushes));

	/// <summary>
	/// Gets/sets the Brush used for displaying the background of selected folding markers.
	/// </summary>
	public Brush? SelectedFoldingMarkerBackgroundBrush {
		get { return (Brush?)GetValue(SelectedFoldingMarkerBackgroundBrushProperty); }
		set { SetValue(SelectedFoldingMarkerBackgroundBrushProperty, value); }
	}

	static void OnUpdateBrushes(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		//was previously: rebuilt the frozen pens (and also resolved the margin from a TextEditor
		//for the attached-property route); colors are read at paint time here, so a repaint suffices.
		((FoldingMargin)d).renderCanvas.Invalidate();
	}

	SKColor FoldingMarkerColor {
		get { return VisualLineElementTextRunProperties.GetSolidColor(FoldingMarkerBrush) ?? new SKColor(128, 128, 128); }
	}

	SKColor SelectedFoldingMarkerColor {
		get { return VisualLineElementTextRunProperties.GetSolidColor(SelectedFoldingMarkerBrush) ?? SKColors.Black; }
	}
	#endregion

	/// <inheritdoc/>
	protected override Size MeasureOverride(Size availableSize)
	{
		renderCanvas.Measure(availableSize);
		//was previously: SizeFactor * (double)GetValue(TextBlock.FontSizeProperty) - the inherited
		//font size; this margin derives its size from the text view's font instead.
		TextView? textView = this.TextView;
		double fontSize = textView != null ? textView.FontSize : 12.0;
		double width = SizeFactor * fontSize;
		return new Size(PixelSnapHelpers.RoundToOdd(width, GetPixelSize().Width), 0);
	}

	/// <inheritdoc/>
	protected override Size ArrangeOverride(Size finalSize)
	{
		renderCanvas.Arrange(new Rect(new Point(0, 0), finalSize));
		renderCanvas.Invalidate();
		return finalSize;
	}

	Size GetPixelSize()
	{
		TextView? textView = this.TextView;
		return textView != null ? PixelSnapHelpers.GetPixelSize(textView) : new Size(1, 1);
	}

	/// <inheritdoc/>
	protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
	{
		if (oldTextView != null)
		{
			oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
		}
		base.OnTextViewChanged(oldTextView, newTextView);
		if (newTextView != null)
		{
			newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
		}
		TextViewVisualLinesChanged(null, EventArgs.Empty);
	}

	#region Markers
	//was previously: FoldingMarginMarker was a UIElement visual child with its own MeasureCore/
	//OnRender/OnMouseDown; the port keeps only its data (owning visual line, folding section) plus
	//the hit-test rectangle computed during the paint pass.
	sealed class FoldingMarginMarker
	{
		internal FoldingMarginMarker(VisualLine visualLine, FoldingSection foldingSection)
		{
			VisualLine = visualLine;
			FoldingSection = foldingSection;
		}

		internal VisualLine VisualLine { get; }
		internal FoldingSection FoldingSection { get; }
		internal Rect Rect { get; set; }
	}

	readonly List<FoldingMarginMarker> markers = new List<FoldingMarginMarker>();

	/// <summary>
	/// The folding section whose marker the pointer is currently over, or null.
	/// </summary>
	//was previously: each FoldingMarginMarker tracked WPF's IsMouseDirectlyOver.
	FoldingSection? hoveredSection;

	void TextViewVisualLinesChanged(object? sender, EventArgs e)
	{
		markers.Clear();
		TextView? textView = this.TextView;
		FoldingManager? foldingManager = this.FoldingManager;
		if (textView != null && foldingManager != null && textView.VisualLinesValid)
		{
			foreach (VisualLine line in textView.VisualLines)
			{
				FoldingSection? fs = foldingManager.GetNextFolding(line.FirstDocumentLine.Offset);
				if (fs == null)
					continue;
				if (fs.StartOffset <= line.LastDocumentLine.Offset + line.LastDocumentLine.Length)
				{
					markers.Add(new FoldingMarginMarker(line, fs));
				}
			}
		}
		if (hoveredSection != null && !markers.Any(m => m.FoldingSection == hoveredSection))
			hoveredSection = null;
		InvalidateMeasure();
		renderCanvas.Invalidate();
	}

	void UpdateMarkerRects(TextView textView, Size pixelSize, double marginWidth)
	{
		double markerSize = MarginSizeFactor * SizeFactor * textView.FontSize;
		markerSize = PixelSnapHelpers.RoundToOdd(markerSize, pixelSize.Width);
		foreach (FoldingMarginMarker m in markers)
		{
			int visualColumn = m.VisualLine.GetVisualColumn(m.FoldingSection.StartOffset - m.VisualLine.FirstDocumentLine.Offset);
			TextLineLayout textLine = m.VisualLine.GetTextLine(visualColumn);
			double yPos = m.VisualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextMiddle) - textView.VerticalOffset;
			yPos -= markerSize / 2;
			double xPos = (marginWidth - markerSize) / 2;
			m.Rect = new Rect(PixelSnapHelpers.Round(new Point(xPos, yPos), pixelSize),
							  new Size(markerSize, markerSize));
		}
	}

	FoldingMarginMarker? HitTestMarker(Point position)
	{
		foreach (FoldingMarginMarker m in markers)
		{
			if (m.Rect.Contains(position))
				return m;
		}
		return null;
	}

	void FoldingMargin_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (e.Handled)
			return;
		var point = e.GetCurrentPoint(this);
		if (!point.Properties.IsLeftButtonPressed)
			return;
		FoldingMarginMarker? marker = HitTestMarker(point.Position);
		if (marker != null)
		{
			//was previously: FoldingMarginMarker.OnMouseDown toggled IsExpanded on a left click.
			marker.FoldingSection.IsFolded = !marker.FoldingSection.IsFolded;
			e.Handled = true;
		}
	}

	void FoldingMargin_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		FoldingSection? newHoveredSection = HitTestMarker(e.GetCurrentPoint(this).Position)?.FoldingSection;
		if (newHoveredSection != hoveredSection)
		{
			hoveredSection = newHoveredSection;
			renderCanvas.Invalidate();
		}
	}

	void FoldingMargin_PointerExited(object sender, PointerRoutedEventArgs e)
	{
		if (hoveredSection != null)
		{
			hoveredSection = null;
			renderCanvas.Invalidate();
		}
	}
	#endregion

	#region Paint
	//was previously: OnRender(DrawingContext); one paint pass on the hosted canvas draws the fold
	//lines and the marker boxes.
	void RenderCanvasPaint(SKCanvas canvas, SKSize size)
	{
		TextView? textView = this.TextView;
		if (textView == null || !textView.VisualLinesValid)
			return;
		if (textView.VisualLines.Count == 0 || FoldingManager == null)
			return;

		Size pixelSize = PixelSnapHelpers.GetPixelSize(textView);
		UpdateMarkerRects(textView, pixelSize, size.Width);

		var allTextLines = textView.VisualLines.SelectMany(vl => vl.TextLines).ToList();
		SKColor?[] colors = new SKColor?[allTextLines.Count + 1];
		SKColor?[] endMarker = new SKColor?[allTextLines.Count];

		CalculateFoldLinesForFoldingsActiveAtStart(textView, allTextLines, colors, endMarker);
		CalculateFoldLinesForMarkers(textView, allTextLines, colors, endMarker);
		DrawFoldLines(textView, canvas, size, pixelSize, colors, endMarker);
		DrawMarkers(canvas, pixelSize);
	}

	/// <summary>
	/// Calculates fold lines for all folding sections that start in front of the current view
	/// and run into the current view.
	/// </summary>
	void CalculateFoldLinesForFoldingsActiveAtStart(TextView textView, List<TextLineLayout> allTextLines, SKColor?[] colors, SKColor?[] endMarker)
	{
		FoldingManager foldingManager = this.FoldingManager
			?? throw new InvalidOperationException("FoldingManager is not assigned");
		SKColor foldingColor = FoldingMarkerColor;
		int viewStartOffset = textView.VisualLines[0].FirstDocumentLine.Offset;
		int viewEndOffset = textView.VisualLines.Last().LastDocumentLine.EndOffset;
		var foldings = foldingManager.GetFoldingsContaining(viewStartOffset);
		int maxEndOffset = 0;
		foreach (FoldingSection fs in foldings)
		{
			int end = fs.EndOffset;
			if (end <= viewEndOffset && !fs.IsFolded)
			{
				int textLineNr = GetTextLineIndexFromOffset(textView, allTextLines, end);
				if (textLineNr >= 0)
				{
					endMarker[textLineNr] = foldingColor;
				}
			}
			if (end > maxEndOffset && fs.StartOffset < viewStartOffset)
			{
				maxEndOffset = end;
			}
		}
		if (maxEndOffset > 0)
		{
			if (maxEndOffset > viewEndOffset)
			{
				for (int i = 0; i < colors.Length; i++)
				{
					colors[i] = foldingColor;
				}
			}
			else
			{
				int maxTextLine = GetTextLineIndexFromOffset(textView, allTextLines, maxEndOffset);
				for (int i = 0; i <= maxTextLine; i++)
				{
					colors[i] = foldingColor;
				}
			}
		}
	}

	/// <summary>
	/// Calculates fold lines for all folding sections that start inside the current view.
	/// </summary>
	void CalculateFoldLinesForMarkers(TextView textView, List<TextLineLayout> allTextLines, SKColor?[] colors, SKColor?[] endMarker)
	{
		SKColor foldingColor = FoldingMarkerColor;
		SKColor selectedColor = SelectedFoldingMarkerColor;
		foreach (FoldingMarginMarker marker in markers)
		{
			bool isHovered = marker.FoldingSection == hoveredSection;
			int end = marker.FoldingSection.EndOffset;
			int endTextLineNr = GetTextLineIndexFromOffset(textView, allTextLines, end);
			if (!marker.FoldingSection.IsFolded && endTextLineNr >= 0)
			{
				if (isHovered)
					endMarker[endTextLineNr] = selectedColor;
				else if (endMarker[endTextLineNr] == null)
					endMarker[endTextLineNr] = foldingColor;
			}
			int startTextLineNr = GetTextLineIndexFromOffset(textView, allTextLines, marker.FoldingSection.StartOffset);
			if (startTextLineNr >= 0)
			{
				for (int i = startTextLineNr + 1; i < colors.Length && i - 1 != endTextLineNr; i++)
				{
					if (isHovered)
						colors[i] = selectedColor;
					else if (colors[i] == null)
						colors[i] = foldingColor;
				}
			}
		}
	}

	/// <summary>
	/// Draws the lines for the folding sections (vertical line with 'colors', horizontal lines
	/// with 'endMarker'). Each entry in the input arrays corresponds to one text line row.
	/// </summary>
	void DrawFoldLines(TextView textView, SKCanvas canvas, SKSize size, Size pixelSize, SKColor?[] colors, SKColor?[] endMarker)
	{
		// Because the strokes have flat caps, for vertical lines, Y coordinates must be on pixel
		// boundaries, whereas the X coordinate must be in the middle of a pixel (and the other way
		// round for horizontal lines).
		double markerXPos = PixelSnapHelpers.PixelAlign(size.Width / 2, pixelSize.Width);
		double startY = 0;
		SKColor? currentColor = colors[0];
		int tlNumber = 0;
		using var paint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = FoldLineThickness };
		foreach (VisualLine vl in textView.VisualLines)
		{
			foreach (TextLineLayout tl in vl.TextLines)
			{
				if (endMarker[tlNumber] is SKColor endColor)
				{
					double visualPos = GetVisualPos(textView, vl, tl, pixelSize.Height);
					paint.Color = endColor;
					canvas.DrawLine((float)(markerXPos - pixelSize.Width / 2), (float)visualPos,
									size.Width, (float)visualPos, paint);
				}
				if (colors[tlNumber + 1] != currentColor)
				{
					double visualPos = GetVisualPos(textView, vl, tl, pixelSize.Height);
					if (currentColor is SKColor lineColor)
					{
						paint.Color = lineColor;
						canvas.DrawLine((float)markerXPos, (float)(startY + pixelSize.Height / 2),
										(float)markerXPos, (float)(visualPos - pixelSize.Height / 2), paint);
					}
					currentColor = colors[tlNumber + 1];
					startY = visualPos;
				}
				tlNumber++;
			}
		}
		if (currentColor is SKColor tailColor)
		{
			paint.Color = tailColor;
			canvas.DrawLine((float)markerXPos, (float)(startY + pixelSize.Height / 2),
							(float)markerXPos, size.Height, paint);
		}
	}

	/// <summary>
	/// Draws the +/- marker boxes on top of the fold lines.
	/// </summary>
	//was previously: FoldingMarginMarker.OnRender.
	void DrawMarkers(SKCanvas canvas, Size pixelSize)
	{
		SKColor foldingColor = FoldingMarkerColor;
		SKColor selectedColor = SelectedFoldingMarkerColor;
		SKColor foldingBackground = VisualLineElementTextRunProperties.GetSolidColor(FoldingMarkerBackgroundBrush) ?? SKColors.White;
		SKColor selectedBackground = VisualLineElementTextRunProperties.GetSolidColor(SelectedFoldingMarkerBackgroundBrush) ?? SKColors.White;

		using var fillPaint = new SKPaint { Style = SKPaintStyle.Fill };
		using var borderPaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = FoldLineThickness };
		using var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = FoldLineThickness, StrokeCap = SKStrokeCap.Square };

		foreach (FoldingMarginMarker m in markers)
		{
			bool isHovered = m.FoldingSection == hoveredSection;
			Rect r = m.Rect;
			if (r.Width <= pixelSize.Width || r.Height <= pixelSize.Height)
				continue;
			var box = SKRect.Create(
				(float)(r.X + pixelSize.Width / 2),
				(float)(r.Y + pixelSize.Height / 2),
				(float)(r.Width - pixelSize.Width),
				(float)(r.Height - pixelSize.Height));

			fillPaint.Color = isHovered ? selectedBackground : foldingBackground;
			canvas.DrawRect(box, fillPaint);
			borderPaint.Color = isHovered ? selectedColor : foldingColor;
			canvas.DrawRect(box, borderPaint);

			float middleX = box.MidX;
			float middleY = box.MidY;
			float space = (float)(PixelSnapHelpers.Round(box.Width / 8, pixelSize.Width) + pixelSize.Width);
			strokePaint.Color = selectedColor;
			canvas.DrawLine(box.Left + space, middleY, box.Right - space, middleY, strokePaint);
			if (m.FoldingSection.IsFolded)
			{
				canvas.DrawLine(middleX, box.Top + space, middleX, box.Bottom - space, strokePaint);
			}
		}
	}

	double GetVisualPos(TextView textView, VisualLine vl, TextLineLayout tl, double pixelHeight)
	{
		double pos = vl.GetTextLineVisualYPosition(tl, VisualYPosition.TextMiddle) - textView.VerticalOffset;
		return PixelSnapHelpers.PixelAlign(pos, pixelHeight);
	}

	int GetTextLineIndexFromOffset(TextView textView, List<TextLineLayout> textLines, int offset)
	{
		int lineNumber = textView.Document.GetLineByOffset(offset).LineNumber;
		VisualLine? vl = textView.GetVisualLine(lineNumber);
		if (vl != null)
		{
			int relOffset = offset - vl.FirstDocumentLine.Offset;
			TextLineLayout line = vl.GetTextLine(vl.GetVisualColumn(relOffset));
			return textLines.IndexOf(line);
		}
		return -1;
	}
	#endregion
}
