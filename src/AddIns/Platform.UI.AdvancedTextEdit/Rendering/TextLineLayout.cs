#nullable enable

using System;
using SkiaSharp;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

//was previously: no direct counterpart - this is the port's replacement for the WPF TextLine objects
//a visual line exposed. A visual line now runs ONE engine layout for all of its text; each wrapped
//engine row is exposed as one TextLineLayout view over that shared layout. Geometry queries delegate
//to the owning VisualLine's layout, so a row holds no text of its own. Unlike WPF's TextLine there
//is no Width/WidthIncludingTrailingWhitespace pair: <see cref="Width"/> includes trailing whitespace
//(it is the right edge of the row's covered text).

/// <summary>
/// One rendered row of a <see cref="VisualLine"/>. An unwrapped visual line has exactly one row;
/// word-wrapping produces one row per wrapped segment.
/// </summary>
public sealed class TextLineLayout
{
	readonly VisualLine owner;
	double? cachedWidth;

	internal TextLineLayout(
		VisualLine owner,
		int layoutStart,
		int layoutLength,
		int firstVisualColumn,
		int lastVisualColumn,
		double top,
		double height,
		double baseline)
	{
		this.owner = owner;
		this.LayoutStart = layoutStart;
		this.LayoutLength = layoutLength;
		this.FirstVisualColumn = firstVisualColumn;
		this.LastVisualColumn = lastVisualColumn;
		this.Top = top;
		this.Height = height;
		this.Baseline = baseline;
	}

	/// <summary>
	/// Gets the visual line this row belongs to.
	/// </summary>
	public VisualLine VisualLine {
		get { return owner; }
	}

	/// <summary>
	/// Gets the index in the visual line's layout text where this row starts.
	/// </summary>
	internal int LayoutStart { get; }

	/// <summary>
	/// Gets the number of layout text characters on this row.
	/// </summary>
	internal int LayoutLength { get; }

	/// <summary>
	/// Gets the first visual column on this row.
	/// </summary>
	public int FirstVisualColumn { get; }

	/// <summary>
	/// Gets the visual column at which this row ends. At a wrap boundary this is the same value as
	/// the next row's <see cref="FirstVisualColumn"/>.
	/// </summary>
	public int LastVisualColumn { get; }

	/// <summary>
	/// Gets the length of this row in visual columns.
	/// </summary>
	public int Length {
		get { return LastVisualColumn - FirstVisualColumn; }
	}

	/// <summary>
	/// Gets the vertical offset of this row's top edge, relative to the top of the visual line.
	/// </summary>
	public double Top { get; }

	/// <summary>
	/// Gets the height of this row.
	/// </summary>
	public double Height { get; }

	/// <summary>
	/// Gets the distance from this row's top edge down to its baseline.
	/// </summary>
	public double Baseline { get; }

	/// <summary>
	/// Gets the width of the text on this row, including trailing whitespace.
	/// </summary>
	public double Width {
		get {
			if (cachedWidth == null)
			{
				cachedWidth = owner.ComputeRowWidth(this);
			}
			return cachedWidth.Value;
		}
	}

	/// <summary>
	/// Gets the distance from the left edge of the visual line to the given visual column.
	/// The visual column should belong to this row.
	/// </summary>
	public double GetDistanceFromVisualColumn(int visualColumn)
	{
		return owner.GetTextLineVisualXPosition(this, visualColumn);
	}

	/// <summary>
	/// Gets the visual column nearest to the given distance from the left edge of the visual line.
	/// </summary>
	public int GetVisualColumnFromDistance(double distance)
	{
		return owner.GetVisualColumn(this, distance, allowVirtualSpace: false);
	}

	/// <summary>
	/// Paints this row onto a canvas, with the row's top-left corner at <paramref name="origin"/>.
	/// </summary>
	/// <param name="canvas">The destination canvas.</param>
	/// <param name="origin">Where the row's top-left corner lands.</param>
	/// <param name="paint">The paint used for text without a per-run color.</param>
	/// <remarks>
	/// The visual line's whole layout is drawn clipped to this row's band; per-run colors set by
	/// colorizers are baked into the layout and need no handling here.
	/// </remarks>
	public void DrawRow(SKCanvas canvas, SKPoint origin, SKPaint paint)
	{
		if (canvas == null)
			throw new ArgumentNullException(nameof(canvas));
		if (paint == null)
			throw new ArgumentNullException(nameof(paint));
		var layout = owner.LayoutResult;
		if (layout == null)
			return;
		canvas.Save();
		try
		{
			canvas.ClipRect(SKRect.Create(origin.X, origin.Y, layout.Size.Width, (float)Height));
			layout.Draw(canvas, new SKPoint(origin.X, origin.Y - (float)Top), paint);
		}
		finally
		{
			canvas.Restore();
		}
	}
}
