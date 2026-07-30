#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: ICSharpCode.AvalonEdit/Rendering/BackgroundGeometryBuilder.cs in the AvalonEdit
//repo (MIT). The output changed with the drawing stack: instead of a WPF PathGeometry, the builder
//produces the rectangle list (CreateRectangles) and an optional rounded outline path (CreatePath,
//an SKPath) that consumers fill or stroke with an SKPaint. The rounded outline is the same shape
//upstream assembled by inserting WPF path segments; it is re-expressed as a forward walk (down the
//right side, across the bottom, up the left side) because an SKPath is append-only. Per-row text
//bounds now come from the visual line's single engine layout (GetSelectionRects over the layout
//range); wrapped rows no longer exclude the whitespace hidden by the wrap, so a selection may
//extend over it at a wrap boundary.

/// <summary>
/// Helper for creating background geometry (selection, search results, highlights) from text
/// segments: a list of rectangles, or a merged outline path with optional rounded corners.
/// </summary>
public sealed class BackgroundGeometryBuilder
{
	double cornerRadius;

	readonly List<SKRect> rectangles = new();
	readonly List<List<SKRect>> figures = new();
	List<SKRect>? figure;
	double lastBottom;

	/// <summary>
	/// Gets/sets the radius of the rounded corners.
	/// </summary>
	public double CornerRadius {
		get { return cornerRadius; }
		set { cornerRadius = value; }
	}

	/// <summary>
	/// Gets/Sets whether to align to whole pixels.
	///
	/// If BorderThickness is set to 0, the geometry is aligned to whole pixels.
	/// If BorderThickness is set to a non-zero value, the outer edge of the border is aligned
	/// to whole pixels.
	///
	/// The default value is <c>false</c>.
	/// </summary>
	public bool AlignToWholePixels { get; set; }

	/// <summary>
	/// Gets/sets the border thickness.
	///
	/// This property only has an effect if <c>AlignToWholePixels</c> is enabled.
	/// When using the resulting geometry to paint a border, set this property to the border thickness.
	/// Otherwise, leave the property set to the default value <c>0</c>.
	/// </summary>
	public double BorderThickness { get; set; }

	/// <summary>
	/// Gets/Sets whether to extend the rectangles to full width at line end.
	/// </summary>
	public bool ExtendToFullWidthAtLineEnd { get; set; }

	/// <summary>
	/// Creates a new BackgroundGeometryBuilder instance.
	/// </summary>
	public BackgroundGeometryBuilder()
	{
	}

	/// <summary>
	/// Adds the specified segment to the geometry.
	/// </summary>
	public void AddSegment(TextView textView, ISegment segment)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));
		Size pixelSize = PixelSnapHelpers.GetPixelSize(textView);
		foreach (Rect r in GetRectsForSegment(textView, segment, ExtendToFullWidthAtLineEnd))
		{
			AddRectangle(pixelSize, r);
		}
	}

	/// <summary>
	/// Adds a rectangle to the geometry.
	/// </summary>
	/// <remarks>
	/// This overload will align the coordinates according to
	/// <see cref="AlignToWholePixels"/>.
	/// Use the <see cref="AddRectangle(double,double,double,double)"/>-overload instead if the coordinates should not be aligned.
	/// </remarks>
	public void AddRectangle(TextView textView, Rect rectangle)
	{
		AddRectangle(PixelSnapHelpers.GetPixelSize(textView), rectangle);
	}

	void AddRectangle(Size pixelSize, Rect r)
	{
		if (AlignToWholePixels)
		{
			double halfBorder = 0.5 * BorderThickness;
			AddRectangle(PixelSnapHelpers.Round(r.Left - halfBorder, pixelSize.Width) + halfBorder,
						 PixelSnapHelpers.Round(r.Top - halfBorder, pixelSize.Height) + halfBorder,
						 PixelSnapHelpers.Round(r.Right + halfBorder, pixelSize.Width) - halfBorder,
						 PixelSnapHelpers.Round(r.Bottom + halfBorder, pixelSize.Height) - halfBorder);
		}
		else
		{
			AddRectangle(r.Left, r.Top, r.Right, r.Bottom);
		}
	}

	/// <summary>
	/// Calculates the list of rectangles where the segment is shown.
	/// This method usually returns one rectangle for each line inside the segment
	/// (but potentially more, e.g. when bidirectional text is involved).
	/// </summary>
	public static IEnumerable<Rect> GetRectsForSegment(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd = false)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));
		if (segment == null)
			throw new ArgumentNullException(nameof(segment));
		return GetRectsForSegmentImpl(textView, segment, extendToFullWidthAtLineEnd);
	}

	static IEnumerable<Rect> GetRectsForSegmentImpl(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd)
	{
		int segmentStart = segment.Offset;
		int segmentEnd = segment.Offset + segment.Length;

		segmentStart = segmentStart.CoerceValue(0, textView.Document.TextLength);
		segmentEnd = segmentEnd.CoerceValue(0, textView.Document.TextLength);

		TextViewPosition start;
		TextViewPosition end;

		if (segment is SelectionSegment)
		{
			SelectionSegment sel = (SelectionSegment)segment;
			start = new TextViewPosition(textView.Document.GetLocation(sel.StartOffset), sel.StartVisualColumn);
			end = new TextViewPosition(textView.Document.GetLocation(sel.EndOffset), sel.EndVisualColumn);
		}
		else
		{
			start = new TextViewPosition(textView.Document.GetLocation(segmentStart));
			end = new TextViewPosition(textView.Document.GetLocation(segmentEnd));
		}

		foreach (VisualLine vl in textView.VisualLines)
		{
			int vlStartOffset = vl.FirstDocumentLine.Offset;
			if (vlStartOffset > segmentEnd)
				break;
			int vlEndOffset = vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length;
			if (vlEndOffset < segmentStart)
				continue;

			int segmentStartVC;
			if (segmentStart < vlStartOffset)
				segmentStartVC = 0;
			else
				segmentStartVC = vl.ValidateVisualColumn(start, extendToFullWidthAtLineEnd);

			int segmentEndVC;
			if (segmentEnd > vlEndOffset)
				segmentEndVC = extendToFullWidthAtLineEnd ? int.MaxValue : vl.VisualLengthWithEndOfLineMarker;
			else
				segmentEndVC = vl.ValidateVisualColumn(end, extendToFullWidthAtLineEnd);

			foreach (var rect in ProcessTextLines(textView, vl, segmentStartVC, segmentEndVC))
				yield return rect;
		}
	}

	/// <summary>
	/// Calculates the rectangles for the visual column segment.
	/// This returns one rectangle for each row inside the segment.
	/// </summary>
	public static IEnumerable<Rect> GetRectsFromVisualSegment(TextView textView, VisualLine line, int startVC, int endVC)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));
		if (line == null)
			throw new ArgumentNullException(nameof(line));
		return ProcessTextLines(textView, line, startVC, endVC);
	}

	static IEnumerable<Rect> ProcessTextLines(TextView textView, VisualLine visualLine, int segmentStartVC, int segmentEndVC)
	{
		var textLines = visualLine.TextLines;
		TextLineLayout lastTextLine = textLines[textLines.Count - 1];
		double scrollX = textView.HorizontalOffset;
		double scrollY = textView.VerticalOffset;
		var layout = visualLine.LayoutResult;

		for (int i = 0; i < textLines.Count; i++)
		{
			TextLineLayout line = textLines[i];
			double y = visualLine.GetTextLineVisualYPosition(line, VisualYPosition.LineTop);
			int visualStartCol = line.FirstVisualColumn;
			//was previously: upstream subtracted the end-of-paragraph mark on the last row and the
			//wrap-hidden trailing whitespace on inner rows; engine rows carry neither mark, and the
			//wrap-eaten whitespace stays in the row's range (see the file header note).
			int visualEndCol = line.LastVisualColumn;

			if (segmentEndVC < visualStartCol)
				break;
			if (lastTextLine != line && segmentStartVC > visualEndCol)
				continue;
			int segmentStartVCInLine = Math.Max(segmentStartVC, visualStartCol);
			int segmentEndVCInLine = Math.Min(segmentEndVC, visualEndCol);
			y -= scrollY;
			Rect lastRect = Rect.Empty;
			if (segmentStartVCInLine == segmentEndVCInLine)
			{
				// Zero-length ranges still yield a small rectangle so empty lines stay visible.
				// Skip the duplicate produced when the same column belongs to two rows at a wrap
				// boundary and the selection continues across it.
				if (segmentEndVCInLine == visualEndCol && i < textLines.Count - 1 && segmentEndVC > segmentEndVCInLine)
					continue;
				if (segmentStartVCInLine == visualStartCol && i > 0 && segmentStartVC < segmentStartVCInLine)
					continue;
				double pos = visualLine.GetTextLineVisualXPosition(line, segmentStartVCInLine);
				pos -= scrollX;
				lastRect = new Rect(pos, y, textView.EmptyLineSelectionWidth, line.Height);
			}
			else if (layout != null && segmentStartVCInLine <= visualEndCol)
			{
				int layoutStart = Math.Clamp(
					visualLine.GetLayoutIndex(segmentStartVCInLine), line.LayoutStart, line.LayoutStart + line.LayoutLength);
				int layoutEnd = Math.Clamp(
					visualLine.GetLayoutIndex(segmentEndVCInLine), layoutStart, line.LayoutStart + line.LayoutLength);
				if (layoutEnd > layoutStart)
				{
					foreach (SKRect b in layout.GetSelectionRects(layoutStart, layoutEnd - layoutStart))
					{
						double mid = (b.Top + b.Bottom) / 2;
						if (mid < line.Top || mid >= line.Top + line.Height)
							continue;
						double left = b.Left - scrollX;
						double right = b.Right - scrollX;
						if (!lastRect.IsEmpty)
							yield return lastRect;
						// left>right is possible in right-to-left runs
						lastRect = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
					}
				}
			}
			// If the segment ends in virtual space, extend the last rectangle with the portion of
			// the selection after the line end. Also, when word-wrap is enabled and the segment
			// continues into the next row, extend lastRect up to the end of the row.
			if (segmentEndVC > visualEndCol)
			{
				double left, right;
				if (segmentStartVC > visualLine.VisualLengthWithEndOfLineMarker)
				{
					// segmentStartVC is in virtual space
					left = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentStartVC);
				}
				else
				{
					// We already processed the rects up to visualEndCol; the remainder starts at
					// the row's full text width (which includes trailing whitespace here).
					left = line.Width;
				}
				if (line != lastTextLine || segmentEndVC == int.MaxValue)
				{
					// Selection continuing into the next row, or extend-to-full-width:
					// select the full width of the viewport.
					right = Math.Max(textView.ExtentWidth, textView.ViewportWidth);
				}
				else
				{
					right = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentEndVC);
				}

				left -= scrollX;
				right -= scrollX;
				Rect extendSelection = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
				if (!lastRect.IsEmpty)
				{
					if (Intersects(extendSelection, lastRect))
					{
						yield return Union(lastRect, extendSelection);
					}
					else
					{
						// If the end of the line is in an RTL segment, keep lastRect and extendSelection separate.
						yield return lastRect;
						yield return extendSelection;
					}
				}
				else
				{
					yield return extendSelection;
				}
			}
			else if (!lastRect.IsEmpty)
			{
				yield return lastRect;
			}
		}
	}

	static bool Intersects(Rect a, Rect b)
	{
		return a.Left <= b.Right && b.Left <= a.Right && a.Top <= b.Bottom && b.Top <= a.Bottom;
	}

	static Rect Union(Rect a, Rect b)
	{
		double left = Math.Min(a.Left, b.Left);
		double top = Math.Min(a.Top, b.Top);
		double right = Math.Max(a.Right, b.Right);
		double bottom = Math.Max(a.Bottom, b.Bottom);
		return new Rect(left, top, right - left, bottom - top);
	}

	/// <summary>
	/// Adds a rectangle to the geometry.
	/// </summary>
	/// <remarks>
	/// This overload assumes that the coordinates are aligned properly
	/// (see <see cref="AlignToWholePixels"/>).
	/// Use the <see cref="AddRectangle(TextView,Rect)"/>-overload instead if the coordinates are not yet aligned.
	/// </remarks>
	public void AddRectangle(double left, double top, double right, double bottom)
	{
		if (!top.IsClose(lastBottom))
		{
			CloseFigure();
		}
		figure ??= new List<SKRect>();
		var rect = new SKRect((float)left, (float)top, (float)right, (float)bottom);
		figure.Add(rect);
		rectangles.Add(rect);
		this.lastBottom = bottom;
	}

	/// <summary>
	/// Closes the current figure. Vertically adjacent rectangles added after this call start a new
	/// merged outline in <see cref="CreatePath"/>.
	/// </summary>
	public void CloseFigure()
	{
		if (figure != null)
		{
			figures.Add(figure);
			figure = null;
		}
	}

	/// <summary>
	/// Gets the rectangles added so far, aligned according to <see cref="AlignToWholePixels"/>.
	/// Fill them with an SKPaint to paint square-cornered backgrounds.
	/// </summary>
	public IReadOnlyList<SKRect> CreateRectangles()
	{
		return new List<SKRect>(rectangles);
	}

	/// <summary>
	/// Creates the merged outline path: one closed contour per group of vertically adjacent
	/// rectangles, with corners rounded by <see cref="CornerRadius"/>.
	/// Returns null when the geometry is empty. The caller owns the path and must dispose it.
	/// </summary>
	public SKPath? CreatePath()
	{
		CloseFigure();
		if (figures.Count == 0)
			return null;
		//was previously: appended directly to an SKPath; SkiaSharp 4 obsoleted the mutable
		//SKPath surface, so the contours are assembled in an SKPathBuilder and detached.
		using var path = new SKPathBuilder();
		foreach (List<SKRect> f in figures)
		{
			AddFigureToPath(path, f);
		}
		return path.Detach();
	}

	void AddFigureToPath(SKPathBuilder path, List<SKRect> rects)
	{
		double cr = cornerRadius;
		int n = rects.Count;
		SKRect r0 = rects[0];
		SKRect rn = rects[n - 1];

		path.MoveTo(r0.Left, (float)(r0.Top + cr));
		// top edge
		if (Math.Abs(r0.Left - r0.Right) > cr)
		{
			ArcTo(path, (float)(r0.Left + cr), r0.Top, clockwise: true);
			path.LineTo((float)(r0.Right - cr), r0.Top);
			ArcTo(path, r0.Right, (float)(r0.Top + cr), clockwise: true);
		}
		// right side, downwards
		for (int i = 0; i < n; i++)
		{
			SKRect r = rects[i];
			path.LineTo(r.Right, (float)(r.Bottom - cr));
			if (i < n - 1)
			{
				SKRect next = rects[i + 1];
				if (!((double)next.Right).IsClose(r.Right))
				{
					double crS = next.Right < r.Right ? -cr : cr;
					bool firstClockwise = next.Right < r.Right;
					ArcTo(path, (float)(r.Right + crS), r.Bottom, firstClockwise);
					path.LineTo((float)(next.Right - crS), r.Bottom);
					ArcTo(path, next.Right, (float)(r.Bottom + cr), !firstClockwise);
				}
			}
		}
		// bottom edge
		if (Math.Abs(rn.Left - rn.Right) > cr)
		{
			ArcTo(path, (float)(rn.Right - cr), rn.Bottom, clockwise: true);
			path.LineTo((float)(rn.Left + cr), rn.Bottom);
			ArcTo(path, rn.Left, (float)(rn.Bottom - cr), clockwise: true);
		}
		// left side, upwards
		for (int i = n - 1; i >= 1; i--)
		{
			SKRect r = rects[i];
			path.LineTo(r.Left, (float)(r.Top + cr));
			SKRect upper = rects[i - 1];
			if (!((double)upper.Left).IsClose(r.Left))
			{
				double crS = r.Left < upper.Left ? cr : -cr;
				bool firstClockwise = r.Left < upper.Left;
				ArcTo(path, (float)(r.Left + crS), r.Top, firstClockwise);
				path.LineTo((float)(upper.Left - crS), r.Top);
				ArcTo(path, upper.Left, (float)(r.Top - cr), !firstClockwise);
			}
		}
		path.Close();
	}

	void ArcTo(SKPathBuilder path, float x, float y, bool clockwise)
	{
		float r = (float)cornerRadius;
		if (r > 0)
		{
			path.ArcTo(
				new SKPoint(r, r),
				0,
				SKPathArcSize.Small,
				clockwise ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
				new SKPoint(x, y));
		}
		else
		{
			path.LineTo(x, y);
		}
	}
}
