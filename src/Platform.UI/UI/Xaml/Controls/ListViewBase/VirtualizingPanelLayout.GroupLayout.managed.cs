#if !IS_UNIT_TESTS
#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using static System.Math;
using static Microsoft.UI.Xaml.Controls.Primitives.GeneratorDirection;
using ElementPath = CodeBrix.Platform.UI.IndexPath;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// The group layout options of the managed virtualizing layout (ItemsStackPanel): GroupHeaderPlacement and GroupPadding.
/// <list type="bullet">
/// <item>Adjacent headers (GroupHeaderPlacement Left on a vertical panel, Top on a horizontal one): a group's header sits
/// beside its items, its start level with the group's first item; the items are laid out in the breadth that remains; the
/// group ends where the longer of the two ends.</item>
/// <item>GroupPadding: every group (header and items) is inset by the padding: the start/end sides in the scrolling
/// direction add space before and after the group, the other two sides narrow the group's breadth.</item>
/// </list>
/// </summary>
/// <remarks>
/// Every method here runs only when <see cref="GroupLayoutAdjusted"/> is true (the list shows group headers AND asks for
/// an adjacent header or a non-zero GroupPadding). A grouped list with the default placement (inline headers) and no
/// padding, and every ungrouped list, runs the layout's original code paths untouched.
/// </remarks>
public abstract partial class VirtualizingPanelLayout
{
	/// <summary>The breadth each display group's adjacent header was last measured to, by display group.</summary>
	private readonly Dictionary<int, double> _adjacentHeaderBreadths = new Dictionary<int, double>();

	/// <summary>The widest adjacent header breadth seen, used for a group whose header has not been measured yet.</summary>
	private double _adjacentHeaderBreadthEstimate;

	/// <summary>
	/// Whether the grouped layout has to honour an adjacent header placement or a group padding (the list shows group
	/// headers, and GroupHeaderPlacement is Adjacent for the scrolling direction or GroupPadding is not zero).
	/// </summary>
	internal bool GroupLayoutAdjusted =>
		_showsGroupHeaders && (RelativeGroupHeaderPlacement == RelativeHeaderPlacement.Adjacent || GroupPadding != default);

	private bool AdjacentGroupHeaders => RelativeGroupHeaderPlacement == RelativeHeaderPlacement.Adjacent;

	private double GroupPaddingExtentStart => IsHorizontal ? GroupPadding.Left : GroupPadding.Top;

	private double GroupPaddingExtentEnd => IsHorizontal ? GroupPadding.Right : GroupPadding.Bottom;

	private double GroupPaddingBreadthStart => IsHorizontal ? GroupPadding.Top : GroupPadding.Left;

	private double GroupPaddingBreadthEnd => IsHorizontal ? GroupPadding.Bottom : GroupPadding.Right;

	partial void OnGroupHeaderPlacementChangedPartialNative(GroupHeaderPlacement oldGroupHeaderPlacement, GroupHeaderPlacement newGroupHeaderPlacement)
		=> RefreshForGroupLayoutChange();

	partial void OnGroupPaddingChangedPartialNative(Thickness oldGroupPadding, Thickness newGroupPadding)
		=> RefreshForGroupLayoutChange();

	/// <summary>A group layout option changed: a list that shows group headers lays its lines out again.</summary>
	private void RefreshForGroupLayoutChange()
	{
		_adjacentHeaderBreadths.Clear();
		_adjacentHeaderBreadthEstimate = 0;

		if (_showsGroupHeaders && _ownerPanel is not null && _generator is not null)
		{
			Refresh();
		}
	}

	// ------------------------------------------------------------------ placing lines

	/// <summary>
	/// Places a new line (header or items) with the group layout options: forward after the last realized line, backward
	/// before the first one, or at <paramref name="extentOffset"/> when it is the first line.
	/// </summary>
	private void PlaceNewGroupedLine(Line line, GeneratorDirection fillDirection, double extentOffset)
	{
		double start;
		if (_materializedLines.Count == 0)
		{
			// The first line: at the seed's start (a rebuild, a large scroll, ScrollIntoView), or at the top of the list,
			// where the first group's leading padding comes first.
			start = _dynamicSeedStart.HasValue || !IsFirstElementLine(line)
				? extentOffset - (fillDirection == Backward ? MeasureGroupedLine(line) : 0)
				: GroupPaddingExtentStart;
		}
		else if (fillDirection == Forward)
		{
			start = GetGroupedStartAfter(_materializedLines.Count - 1, line);
		}
		else
		{
			start = GetGroupedStartBefore(_materializedLines[0], line);
		}

		PlaceGroupedLine(line, start);
	}

	/// <summary>
	/// Lays the realized lines out again from the first one, each after the one before it. A line added backward was
	/// placed before its neighbour from an estimate (the end of a group can be its adjacent header rather than its last
	/// item); this makes every line follow the group layout exactly.
	/// </summary>
	private void NormalizeGroupedLines()
	{
		for (var i = 1; i < _materializedLines.Count; i++)
		{
			var line = _materializedLines[i];
			var start = GetGroupedStartAfter(i - 1, line);
			if (!DoubleUtil.AreClose(start, GetMeasuredStart(line.FirstView)) || !IsPlacedInItsGroupBreadth(line))
			{
				PlaceGroupedLine(line, start);
			}
		}
	}

	/// <summary>Where a line starts when it follows the realized line at <paramref name="previousIndex"/>.</summary>
	private double GetGroupedStartAfter(int previousIndex, Line line)
	{
		var previous = _materializedLines[previousIndex];

		if (line.IsHeader)
		{
			// A new group starts after the previous group ends (its furthest line) and its trailing padding, plus its own
			// leading padding.
			return GetGroupEndAt(previousIndex) + GroupPaddingExtentEnd + GroupPaddingExtentStart;
		}

		if (previous.IsHeader && previous.Section == line.Section)
		{
			// The group's first item: beside an adjacent header, below (after) an inline one.
			return AdjacentGroupHeaders ? GetMeasuredStart(previous.FirstView) : GetMeasuredEnd(previous.FirstView);
		}

		return GetMeasuredEnd(previous.LastView);
	}

	/// <summary>Where a line starts when it precedes <paramref name="next"/> (a backward fill; an estimate, see <see cref="NormalizeGroupedLines"/>).</summary>
	private double GetGroupedStartBefore(Line next, Line line)
	{
		var nextStart = GetMeasuredStart(next.FirstView);
		var extent = MeasureGroupedLine(line);

		if (next.IsHeader)
		{
			// The line ends the previous group: before the next group's leading padding and its own group's trailing one.
			return nextStart - GroupPaddingExtentStart - GroupPaddingExtentEnd - extent;
		}

		if (line.IsHeader && AdjacentGroupHeaders)
		{
			// An adjacent header starts level with its group's first item.
			return nextStart;
		}

		return nextStart - extent;
	}

	/// <summary>The end of the group of the line at <paramref name="index"/>: the furthest end among its realized lines.</summary>
	private double GetGroupEndAt(int index)
	{
		var section = _materializedLines[index].Section;
		var end = double.NegativeInfinity;
		for (var i = index; i >= 0 && _materializedLines[i].Section == section; i--)
		{
			var line = _materializedLines[i];
			end = Max(end, GetMeasuredEnd(line.LastView));
			if (line.IsHeader)
			{
				break;
			}
		}

		return end;
	}

	/// <summary>Measures a line's elements in the breadth its group gives them and returns the line's extent.</summary>
	private double MeasureGroupedLine(Line line)
	{
		var (_, breadth) = GetGroupedLineBreadth(line);
		var extent = 0d;
		foreach (var (view, _) in line.Items)
		{
			view.Measure(IsHorizontal ? new Size(double.PositiveInfinity, breadth) : new Size(breadth, double.PositiveInfinity));
			extent = Max(extent, GetExtent(view.DesiredSize));
			if (line.IsHeader && AdjacentGroupHeaders)
			{
				RecordAdjacentHeaderBreadth(line.Section, GetBreadth(view.DesiredSize));
			}
		}

		return extent;
	}

	/// <summary>Measures a line in its group's breadth and sets its elements' bounds at <paramref name="start"/>.</summary>
	private void PlaceGroupedLine(Line line, double start)
	{
		MeasureGroupedLine(line);

		var (breadthStart, _) = GetGroupedLineBreadth(line);
		var breadthOffset = breadthStart;
		foreach (var (view, _) in line.Items)
		{
			var size = view.DesiredSize;
			var bounds = IsHorizontal
				? new Rect(start, breadthOffset, size.Width, size.Height)
				: new Rect(breadthOffset, start, size.Width, size.Height);
			SetBounds(view, bounds);
			breadthOffset += GetBreadth(size);
		}
	}

	/// <summary>Whether a line's first element starts at the breadth its group gives it.</summary>
	private bool IsPlacedInItsGroupBreadth(Line line)
	{
		var bounds = GetBoundsForElement(line.FirstView);
		var (breadthStart, _) = GetGroupedLineBreadth(line);
		return DoubleUtil.AreClose(IsHorizontal ? bounds.Y : bounds.X, breadthStart);
	}

	/// <summary>
	/// The breadth start and the breadth available to a line: the panel's breadth less the group padding's two breadth
	/// sides, and, for the items of a group with an adjacent header, less that header's breadth.
	/// </summary>
	private (double Start, double Breadth) GetGroupedLineBreadth(Line line)
	{
		var start = GroupPaddingBreadthStart;
		var breadth = Max(0, AvailableBreadth - GroupPaddingBreadthStart - GroupPaddingBreadthEnd);

		if (!line.IsHeader && AdjacentGroupHeaders)
		{
			var header = GetAdjacentHeaderBreadth(line.Section);
			start += header;
			breadth = Max(0, breadth - header);
		}

		return (start, breadth);
	}

	private void RecordAdjacentHeaderBreadth(int group, double breadth)
	{
		_adjacentHeaderBreadths[group] = breadth;
		_adjacentHeaderBreadthEstimate = Max(_adjacentHeaderBreadthEstimate, breadth);
	}

	private double GetAdjacentHeaderBreadth(int group)
		=> _adjacentHeaderBreadths.TryGetValue(group, out var breadth) ? breadth : _adjacentHeaderBreadthEstimate;

	// ------------------------------------------------------------------ arranging

	/// <summary>
	/// The arrange bounds of an element of a grouped line: the panel's stretch stops at the group padding's end side (and an
	/// adjacent header keeps its own breadth).
	/// </summary>
	private Rect AdjustGroupedArrangeBounds(Rect bounds, Rect arranged, bool isAdjacentHeader)
	{
		if (isAdjacentHeader)
		{
			return bounds;
		}

		var breadthStart = IsHorizontal ? bounds.Y : bounds.X;
		var breadth = Max(GetBreadth(bounds), GetBreadth(arranged) - breadthStart - GroupPaddingBreadthEnd);
		SetBreadth(ref arranged, breadth);
		return arranged;
	}

	/// <summary>
	/// The breadth the realized grouped lines take: each element's breadth start and breadth (desired, for the measure;
	/// arranged, for the arrange), plus the padding's end side.
	/// </summary>
	private double GetGroupedBreadth(bool arranged)
	{
		var breadth = 0d;
		foreach (var line in _materializedLines)
		{
			foreach (var (view, _) in line.Items)
			{
				var bounds = GetBoundsForElement(view);
				var elementBreadth = arranged ? GetActualBreadth(view) : GetBreadth(view.DesiredSize);
				breadth = Max(breadth, (IsHorizontal ? bounds.Y : bounds.X) + elementBreadth + GroupPaddingBreadthEnd);
			}
		}

		return breadth;
	}

	// ------------------------------------------------------------------ estimates

	/// <summary>The estimated extent of a whole group of <paramref name="itemCount"/> items, paddings included.</summary>
	private double EstimateAdjustedGroupExtent(int itemCount)
	{
		var items = itemCount * _averageLineHeight;
		var body = AdjacentGroupHeaders ? Max(EstimatedHeaderExtent, items) : EstimatedHeaderExtent + items;
		return GroupPaddingExtentStart + body + GroupPaddingExtentEnd;
	}

	/// <summary>The panel extent with the group layout options: the realized content, then the estimate of the rest.</summary>
	private double EstimateAdjustedGroupedPanelExtent()
	{
		if (_materializedLines.Count == 0 || ItemsControl is null)
		{
			return 0;
		}

		UpdateGroupedAverageExtents();

		var lastIndex = _materializedLines.Count - 1;
		var last = _materializedLines[lastIndex];
		var group = last.Section;
		var remainingInGroup = ItemCountOfGroup(group) - (last.IsHeader ? 0 : last.LastItem.Row + 1);
		var itemsEnd = (last.IsHeader && AdjacentGroupHeaders ? GetMeasuredStart(last.FirstView) : GetMeasuredEnd(last.LastView))
			+ Max(0, remainingInGroup) * _averageLineHeight;

		var extent = Max(GetGroupEndAt(lastIndex), itemsEnd) + GroupPaddingExtentEnd;
		for (var g = group + 1; g < DisplayGroupCount; g++)
		{
			extent += EstimateAdjustedGroupExtent(ItemCountOfGroup(g));
		}

		return extent;
	}

	/// <summary>The estimated start of an element (a header or an item) with the group layout options.</summary>
	private double EstimateAdjustedGroupedStart(ElementPath target)
	{
		var position = 0d;
		for (var group = 0; group < target.Section && group < DisplayGroupCount; group++)
		{
			position += EstimateAdjustedGroupExtent(ItemCountOfGroup(group));
		}

		position += GroupPaddingExtentStart;
		if (target.Row >= 0)
		{
			position += (AdjacentGroupHeaders ? 0 : EstimatedHeaderExtent) + target.Row * _averageLineHeight;
		}

		return position;
	}

	/// <summary>The element estimated at <paramref name="offset"/> with the group layout options; returns the element before it as the seed.</summary>
	private (ElementPath? Seed, double Start) EstimateAdjustedGroupedSeed(double offset)
	{
		var groups = DisplayGroupCount;
		var position = 0d;
		ElementPath? target = null;
		var start = 0d;

		for (var group = 0; group < groups; group++)
		{
			var count = ItemCountOfGroup(group);
			var groupExtent = EstimateAdjustedGroupExtent(count);
			if (offset >= position + groupExtent && group < groups - 1)
			{
				position += groupExtent;
				continue;
			}

			// The group reached: its header, or the item under the offset.
			var itemsStart = position + GroupPaddingExtentStart + (AdjacentGroupHeaders ? 0 : EstimatedHeaderExtent);
			if (count == 0 || _averageLineHeight <= 0 || offset < itemsStart)
			{
				target = HeaderPath(group);
				start = position + GroupPaddingExtentStart;
			}
			else
			{
				var row = Min(count - 1, (int)((offset - itemsStart) / _averageLineHeight));
				target = ElementPath.FromRowSection(row, group);
				start = itemsStart + row * _averageLineHeight;
			}

			break;
		}

		if (target is null)
		{
			return (null, 0);
		}

		return (GetNextGroupedElement(target, Backward), start);
	}
}

#endif
