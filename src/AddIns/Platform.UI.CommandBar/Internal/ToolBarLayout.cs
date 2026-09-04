using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.CommandBar.Internal;

/// <summary>
/// The one-line (or, when wrapping, several-line) layout every tool bar container uses: children
/// laid end to end along an axis with a gap between them, aligned across it, with filling spacers
/// taking whatever is left over.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ToolBarPanel"/>, <see cref="ToolBarGroup"/> and <see cref="ToolBarTray"/> all lay out
/// this way, and the overflow flyout uses it a fourth time with the axis turned. Keeping the
/// arithmetic here rather than in four measure/arrange pairs is what makes "a spacer fills", "a
/// collapsed item takes no space" and "a separator lands on a device pixel" one behaviour instead
/// of four.
/// </para>
/// <para>
/// Measure and arrange both build the same list of lines from the same desired sizes, so what was
/// measured is what is arranged.
/// </para>
/// </remarks>
internal static class ToolBarLayout
{
	/// <summary>One run of children that share a line.</summary>
	/// <param name="Start">Index of the line's first child in the children list.</param>
	/// <param name="End">Index one past the line's last child.</param>
	/// <param name="Main">The extent the line's children need along the axis.</param>
	/// <param name="Cross">The extent the line needs across the axis.</param>
	internal readonly record struct Line(int Start, int End, double Main, double Cross);

	/// <summary>
	/// Measures <paramref name="children"/> end to end along <paramref name="orientation"/> and
	/// returns the extent they need.
	/// </summary>
	/// <param name="children">The children, in layout order.</param>
	/// <param name="orientation">The axis the children run along.</param>
	/// <param name="spacing">The gap between two adjacent visible children.</param>
	/// <param name="availableSize">The space offered by the parent.</param>
	/// <param name="wrap">True to continue on a further line when the axis runs out.</param>
	/// <returns>The size the children need.</returns>
	internal static Size Measure(
		IReadOnlyList<UIElement> children,
		Orientation orientation,
		double spacing,
		Size availableSize,
		bool wrap = false)
	{
		var horizontal = orientation == Orientation.Horizontal;

		//A run that does NOT wrap offers each child all the room in the world along the axis, so
		//nothing is squeezed and a bar can decide for itself what overflows. A run that DOES wrap
		//offers the line's width instead - that is what lets a ToolBar inside a ToolBarTray see
		//how much room the tray actually has and move its trailing items into its chevron flyout.
		//MEASURED: with an unbounded offer here, a bar in a 260-wide tray never overflowed at all.
		var childAvailable = horizontal
			? new Size(wrap ? availableSize.Width : double.PositiveInfinity, availableSize.Height)
			: new Size(availableSize.Width, wrap ? availableSize.Height : double.PositiveInfinity);

		for (var i = 0; i < children.Count; i++)
		{
			var child = children[i];
			if (child.Visibility == Visibility.Collapsed)
			{
				//A collapsed item still gets a measure pass - the framework's contract - but it is
				//given nothing and contributes nothing, which is what "skipped by the bar" means.
				child.Measure(new Size(0, 0));
				continue;
			}

			child.Measure(childAvailable);
		}

		var limit = horizontal ? availableSize.Width : availableSize.Height;
		var lines = BuildLines(children, horizontal, spacing, wrap ? limit : double.PositiveInfinity);

		double main = 0;
		double cross = 0;
		for (var i = 0; i < lines.Count; i++)
		{
			main = Math.Max(main, lines[i].Main);
			if (i > 0)
			{
				cross += spacing;
			}

			cross += lines[i].Cross;
		}

		return horizontal ? new Size(main, cross) : new Size(cross, main);
	}

	/// <summary>
	/// Arranges <paramref name="children"/> end to end along <paramref name="orientation"/>,
	/// handing every filling spacer an equal share of the space its line has left over.
	/// </summary>
	/// <param name="children">The children, in layout order.</param>
	/// <param name="orientation">The axis the children run along.</param>
	/// <param name="spacing">The gap between two adjacent visible children.</param>
	/// <param name="finalSize">The space the parent settled on.</param>
	/// <param name="rasterizationScale">
	/// The display scale, used to land a separator on a device pixel boundary.
	/// </param>
	/// <param name="wrap">True to continue on a further line when the axis runs out.</param>
	internal static void Arrange(
		IReadOnlyList<UIElement> children,
		Orientation orientation,
		double spacing,
		Size finalSize,
		double rasterizationScale,
		bool wrap = false)
	{
		var horizontal = orientation == Orientation.Horizontal;
		var limit = horizontal ? finalSize.Width : finalSize.Height;
		var lines = BuildLines(children, horizontal, spacing, wrap ? limit : double.PositiveInfinity);

		double crossOffset = 0;

		for (var l = 0; l < lines.Count; l++)
		{
			var line = lines[l];
			if (l > 0)
			{
				crossOffset += spacing;
			}

			var crossExtent = lines.Count == 1
				? Math.Max(line.Cross, horizontal ? finalSize.Height : finalSize.Width)
				: line.Cross;

			var fillers = 0;
			for (var i = line.Start; i < line.End; i++)
			{
				if (children[i].Visibility != Visibility.Collapsed && children[i] is ToolBarSpacer { Fill: true })
				{
					fillers++;
				}
			}

			var leftOver = Math.Max(0, limit - line.Main);
			var perFiller = fillers > 0 ? leftOver / fillers : 0;

			double offset = 0;
			var arranged = 0;

			for (var i = line.Start; i < line.End; i++)
			{
				var child = children[i];
				if (child.Visibility == Visibility.Collapsed)
				{
					child.Arrange(new Rect(0, 0, 0, 0));
					continue;
				}

				if (arranged > 0)
				{
					offset += spacing;
				}

				var mainExtent = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
				if (child is ToolBarSpacer { Fill: true })
				{
					mainExtent += perFiller;
				}

				var start = offset;
				if (child is ToolBarSeparator)
				{
					//A hairline that starts halfway across a device pixel is drawn as two grey
					//pixels instead of one dark one. Rounding the separator's own offset onto the
					//device grid is what keeps it a hairline at a fractional scale; the line's
					//width is already an exact device pixel (see ToolBarSeparator).
					start = SnapToDevicePixel(offset, rasterizationScale);
				}

				var childCross = horizontal ? child.DesiredSize.Height : child.DesiredSize.Width;
				var alignment = GetCrossAlignment(child, horizontal);
				var (crossStart, crossSize) = AlignAcross(alignment, childCross, crossExtent);

				child.Arrange(horizontal
					? new Rect(start, crossOffset + crossStart, mainExtent, crossSize)
					: new Rect(crossOffset + crossStart, start, crossSize, mainExtent));

				offset = start + mainExtent;
				arranged++;
			}

			crossOffset += crossExtent;
		}
	}

	/// <summary>
	/// Works out how many of a bar's items fit before the overflow chevron has to be shown.
	/// </summary>
	/// <param name="extents">Each visible item's extent along the bar, in layout order.</param>
	/// <param name="spacing">The gap between two adjacent items.</param>
	/// <param name="available">The extent the bar has to lay items in.</param>
	/// <param name="chevronExtent">The extent the chevron button needs when it is shown.</param>
	/// <param name="hasOverflow">Set to true when the chevron has to be shown.</param>
	/// <returns>The number of leading items that stay in the bar.</returns>
	/// <remarks>
	/// The chevron's own extent is reserved BEFORE the items are counted, so the answer is stable:
	/// showing the chevron never changes the space the items are measured against, and the bar
	/// cannot oscillate between "one more item fits" and "then the chevron does not".
	/// </remarks>
	internal static int ComputeVisibleCount(
		IReadOnlyList<double> extents,
		double spacing,
		double available,
		double chevronExtent,
		out bool hasOverflow)
	{
		hasOverflow = false;

		var count = extents.Count;
		if (count == 0 || double.IsInfinity(available) || double.IsNaN(available))
		{
			return count;
		}

		double total = 0;
		for (var i = 0; i < count; i++)
		{
			if (i > 0)
			{
				total += spacing;
			}

			total += extents[i];
		}

		if (total <= available)
		{
			return count;
		}

		var usable = available - chevronExtent - spacing;
		double used = 0;
		var fitted = 0;

		for (var i = 0; i < count; i++)
		{
			var step = extents[i] + (fitted > 0 ? spacing : 0);
			if (used + step > usable)
			{
				break;
			}

			used += step;
			fitted++;
		}

		if (fitted >= count)
		{
			return count;
		}

		hasOverflow = true;
		return fitted;
	}

	/// <summary>Rounds <paramref name="value"/> onto the display's pixel grid.</summary>
	/// <param name="value">A logical-pixel offset.</param>
	/// <param name="scale">The display's rasterization scale.</param>
	/// <returns>The nearest logical offset that is a whole number of device pixels.</returns>
	internal static double SnapToDevicePixel(double value, double scale)
		=> scale > 0 ? Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale : value;

	/// <summary>Reads the display scale an element is being rendered at.</summary>
	/// <param name="element">The element to read the scale for.</param>
	/// <returns>The rasterization scale, or 1 when the element is not in a window yet.</returns>
	internal static double GetRasterizationScale(UIElement element)
	{
		var scale = element.XamlRoot?.RasterizationScale ?? 1d;
		return scale > 0 ? scale : 1d;
	}

	private static List<Line> BuildLines(
		IReadOnlyList<UIElement> children,
		bool horizontal,
		double spacing,
		double limit)
	{
		var lines = new List<Line>(1);
		var start = 0;
		double main = 0;
		double cross = 0;
		var counted = 0;

		for (var i = 0; i < children.Count; i++)
		{
			var child = children[i];
			if (child.Visibility == Visibility.Collapsed)
			{
				continue;
			}

			var extent = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
			var step = extent + (counted > 0 ? spacing : 0);

			if (counted > 0 && !double.IsInfinity(limit) && main + step > limit)
			{
				lines.Add(new Line(start, i, main, cross));
				start = i;
				main = extent;
				cross = horizontal ? child.DesiredSize.Height : child.DesiredSize.Width;
				counted = 1;
				continue;
			}

			main += step;
			cross = Math.Max(cross, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
			counted++;
		}

		lines.Add(new Line(start, children.Count, main, cross));

		return lines;
	}

	private static int GetCrossAlignment(UIElement child, bool horizontal)
	{
		//0 = start, 1 = centre, 2 = end, 3 = stretch. One code for both axes keeps the arrange loop
		//free of a second switch.
		if (child is not FrameworkElement fe)
		{
			return 3;
		}

		if (horizontal)
		{
			return fe.VerticalAlignment switch
			{
				VerticalAlignment.Top => 0,
				VerticalAlignment.Center => 1,
				VerticalAlignment.Bottom => 2,
				_ => 3,
			};
		}

		return fe.HorizontalAlignment switch
		{
			HorizontalAlignment.Left => 0,
			HorizontalAlignment.Center => 1,
			HorizontalAlignment.Right => 2,
			_ => 3,
		};
	}

	private static (double Start, double Size) AlignAcross(int alignment, double childCross, double crossExtent)
	{
		if (alignment == 3 || childCross >= crossExtent)
		{
			return (0, Math.Max(childCross, crossExtent));
		}

		return alignment switch
		{
			0 => (0, childCross),
			2 => (crossExtent - childCross, childCross),
			_ => ((crossExtent - childCross) / 2, childCross),
		};
	}
}
