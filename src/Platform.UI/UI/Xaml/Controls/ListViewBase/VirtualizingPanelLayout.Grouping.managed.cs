#if !IS_UNIT_TESTS
#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using static System.Math;
using static Microsoft.UI.Xaml.Controls.Primitives.GeneratorDirection;
using ElementPath = CodeBrix.Platform.UI.IndexPath;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Group headers of the managed virtualizing layout (ItemsStackPanel): when the list shows group headers
/// (<see cref="ItemsControl.ShowsGroupHeaders"/>), each displayed group is preceded by a header line holding the list's
/// group header container (ListViewHeaderItem / GridViewHeaderItem). A header line's index is (-1, group), which sorts
/// before the group's items, so the fill walks header, items, header, items... (an empty group shown because
/// GroupStyle.HidesIfEmpty is false is a header line alone). Headers are pooled here (the item generator is keyed by flat
/// item index). With AreStickyGroupHeadersEnabled the header of the group under the top of the viewport stays there.
/// </summary>
/// <remarks>
/// Every method here is reached only when <see cref="_showsGroupHeaders"/> is true (or when a header line exists), so an
/// ungrouped list runs the layout's original code paths.
/// </remarks>
public abstract partial class VirtualizingPanelLayout
{
	/// <summary>The most header containers kept for reuse.</summary>
	private const int MaxPooledHeaders = 32;

	/// <summary>The z-index of header containers: a sticky header is drawn (and hit-tested) above the items under it.</summary>
	private const int HeaderZIndex = 1;

	/// <summary>Whether the list shows group headers; refreshed at the start of every layout entry point.</summary>
	private bool _showsGroupHeaders;

	/// <summary>Header containers that show no group, ready for reuse (kept in the panel, collapsed).</summary>
	private readonly Stack<ContentControl> _headerPool = new Stack<ContentControl>();

	/// <summary>Header containers scrapped by a lightweight rebuild, by display group, reused without being rebound.</summary>
	private readonly Dictionary<int, ContentControl> _headerScrap = new Dictionary<int, ContentControl>();

	/// <summary>The header that stands in for the header of the group under the viewport's top when that header's line is not realized.</summary>
	private ContentControl? _stickyHeader;

	/// <summary>The display group <see cref="_stickyHeader"/> is prepared for, or -1.</summary>
	private int _stickyHeaderGroup = -1;

	/// <summary>The average extent of the realized headers, used to estimate the unrealized ones.</summary>
	private double _averageHeaderExtent;

	/// <summary>Reads whether the list shows group headers now (called at each layout entry point).</summary>
	private void UpdateGroupHeaderState()
	{
		var shows = ItemsControl?.ShowsGroupHeaders == true;
		if (shows == _showsGroupHeaders)
		{
			return;
		}

		// Grouping switched on or off (a GroupStyle or a grouped source came or went): start the lines again.
		_showsGroupHeaders = shows;
		if (_materializedLines.Count > 0)
		{
			ClearLines(clearContainer: false);
		}

		if (!shows)
		{
			DiscardHeaderContainers();
		}
	}

	/// <summary>Gets whether this layout currently realizes group headers.</summary>
	internal bool ShowsGroupHeaders => _showsGroupHeaders;

	partial void OnAreStickyGroupHeadersEnabledChangedPartialNative(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled)
		=> _ownerPanel?.InvalidateArrange();

	// ------------------------------------------------------------------ element order

	private static ElementPath HeaderPath(int group) => ElementPath.FromRowSection(-1, group);

	private int DisplayGroupCount => ItemsControl?.NumberOfDisplayGroups ?? 0;

	private int ItemCountOfGroup(int group) => ItemsControl?.GetDisplayGroupCount(group) ?? 0;

	/// <summary>
	/// The element after (or before) <paramref name="current"/> in the grouped order: header of group 0, its items, header
	/// of group 1, ... Null when there is none. A null <paramref name="current"/> means "before the first element".
	/// </summary>
	private ElementPath? GetNextGroupedElement(ElementPath? current, GeneratorDirection direction)
	{
		var groups = DisplayGroupCount;
		if (groups == 0)
		{
			return null;
		}

		if (current is not { } path)
		{
			return direction == Forward ? HeaderPath(0) : null;
		}

		var group = path.Section;
		if (group >= groups)
		{
			// A stale path past the last group (the groups changed): step back to the last element.
			return direction == Forward ? null : GetLastGroupedElement();
		}

		var count = ItemCountOfGroup(group);

		if (direction == Forward)
		{
			if (path.Row + 1 < count)
			{
				return ElementPath.FromRowSection(path.Row + 1, group);
			}

			return group + 1 < groups ? HeaderPath(group + 1) : null;
		}

		if (path.Row == -1)
		{
			if (group == 0)
			{
				return null;
			}

			var previousCount = ItemCountOfGroup(group - 1);
			return previousCount > 0 ? ElementPath.FromRowSection(previousCount - 1, group - 1) : HeaderPath(group - 1);
		}

		if (path.Row > 0)
		{
			return ElementPath.FromRowSection(Min(path.Row, count) - 1, group);
		}

		return HeaderPath(group);
	}

	/// <summary>The last element in the grouped order (the last item, or the header of a trailing empty group).</summary>
	private ElementPath? GetLastGroupedElement()
	{
		var groups = DisplayGroupCount;
		if (groups == 0)
		{
			return null;
		}

		var count = ItemCountOfGroup(groups - 1);
		return count > 0 ? ElementPath.FromRowSection(count - 1, groups - 1) : HeaderPath(groups - 1);
	}

	// ------------------------------------------------------------------ header lines and containers

	/// <summary>Creates the line of the header of <paramref name="group"/> at <paramref name="extentOffset"/>.</summary>
	private Line CreateHeaderLine(GeneratorDirection fillDirection, double extentOffset, int group)
	{
		var header = DequeueHeader(group);
		AddView(header, fillDirection, extentOffset, 0);
		return new Line(group, header);
	}

	/// <summary>A header container prepared for <paramref name="group"/>: the scrapped one, a pooled one, or a new one.</summary>
	private ContentControl DequeueHeader(int group)
	{
		if (_headerScrap.Remove(group, out var scrapped))
		{
			return scrapped;
		}

		var itemsControl = ItemsControl!;
		ContentControl header;
		if (_headerPool.Count > 0)
		{
			header = _headerPool.Pop();
			header.Visibility = Visibility.Visible;
		}
		else
		{
			header = itemsControl.GetGroupHeaderContainer(itemsControl.GetGroupAtDisplaySection(group)?.Group);
			Canvas.SetZIndex(header, HeaderZIndex);
		}

		itemsControl.PrepareGroupHeaderContainer(header, group);
		return header;
	}

	/// <summary>Puts a header container back in the pool (or removes it when the pool is full).</summary>
	private void RecycleHeader(ContentControl header, bool clearContainer)
	{
		if (clearContainer || _headerPool.Count >= MaxPooledHeaders)
		{
			ItemsControl?.ClearGroupHeaderContainer(header);
		}

		if (_headerPool.Count < MaxPooledHeaders)
		{
			_headerPool.Push(header);
		}
		else
		{
			DiscardHeader(header);
		}
	}

	private void DiscardHeader(ContentControl header)
	{
		if (header.Parent is Panel parent)
		{
			parent.Children.Remove(header);
		}
	}

	/// <summary>Called at the end of an update: unused scrapped headers go to the pool, and every pooled header is collapsed.</summary>
	private void ClearScrappedHeaders()
	{
		if (_headerScrap.Count > 0)
		{
			foreach (var header in _headerScrap.Values)
			{
				RecycleHeader(header, clearContainer: false);
			}

			_headerScrap.Clear();
		}

		foreach (var header in _headerPool)
		{
			header.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>Drops every pooled header and the sticky header (the list no longer shows group headers).</summary>
	private void DiscardHeaderContainers()
	{
		foreach (var header in _headerScrap.Values)
		{
			ItemsControl?.ClearGroupHeaderContainer(header);
			DiscardHeader(header);
		}

		_headerScrap.Clear();

		while (_headerPool.Count > 0)
		{
			var header = _headerPool.Pop();
			ItemsControl?.ClearGroupHeaderContainer(header);
			DiscardHeader(header);
		}

		if (_stickyHeader is { } sticky)
		{
			ItemsControl?.ClearGroupHeaderContainer(sticky);
			DiscardHeader(sticky);
			_stickyHeader = null;
			_stickyHeaderGroup = -1;
		}
	}

	/// <summary>The header container realized for a display group (its line, else the sticky header), or null.</summary>
	/// <param name="group">The display index of the group.</param>
	/// <returns>The realized header container, or null.</returns>
	internal ContentControl? GetRealizedGroupHeader(int group)
	{
		foreach (var line in _materializedLines)
		{
			if (line.IsHeader && line.Section == group)
			{
				return line.FirstView as ContentControl;
			}
		}

		return _stickyHeaderGroup == group ? _stickyHeader : null;
	}

	// ------------------------------------------------------------------ line queries

	/// <summary>True when <paramref name="line"/> is the first element of the list (grouped: the header of group 0).</summary>
	private bool IsFirstElementLine(Line line)
		=> line.IsHeader ? line.Section == 0 : (!_showsGroupHeaders && line.FirstItemFlat == 0);

	private Line? GetFirstItemLine()
	{
		foreach (var line in _materializedLines)
		{
			if (!line.IsHeader)
			{
				return line;
			}
		}

		return null;
	}

	private Line? GetLastItemLine()
	{
		for (var i = _materializedLines.Count - 1; i >= 0; i--)
		{
			if (!_materializedLines[i].IsHeader)
			{
				return _materializedLines[i];
			}
		}

		return null;
	}

	/// <summary>Average extents of the realized item lines and header lines (headers do not count as item lines).</summary>
	private void UpdateGroupedAverageExtents()
	{
		double itemTotal = 0, headerTotal = 0;
		int itemLines = 0, headerLines = 0;
		foreach (var line in _materializedLines)
		{
			var extent = GetMeasuredExtent(line.FirstView);
			if (line.IsHeader)
			{
				headerTotal += extent;
				headerLines++;
			}
			else
			{
				itemTotal += extent;
				itemLines++;
			}
		}

		_averageLineHeight = itemLines > 0 ? itemTotal / itemLines : 0;
		if (headerLines > 0)
		{
			_averageHeaderExtent = headerTotal / headerLines;
		}
	}

	/// <summary>The extent assumed for an unrealized header.</summary>
	private double EstimatedHeaderExtent => _averageHeaderExtent > 0 ? _averageHeaderExtent : _averageLineHeight;

	// ------------------------------------------------------------------ estimates

	/// <summary>The panel extent: realized content plus the estimated extent of the elements after the last realized one.</summary>
	private double EstimateGroupedPanelExtent()
	{
		if (GetLastMaterializedIndexPath() is not { } last || ItemsControl is not { } itemsControl)
		{
			return 0;
		}

		UpdateGroupedAverageExtents();

		var groups = DisplayGroupCount;
		int remainingItems;
		if (last.Row == -1)
		{
			// Every item of the group whose header is last, and of the groups after it.
			remainingItems = itemsControl.NumberOfItems - GetFlatItemIndex(ElementPath.FromRowSection(0, last.Section));
		}
		else
		{
			remainingItems = itemsControl.NumberOfItems - GetFlatItemIndex(last) - 1;
		}

		var remainingHeaders = Max(0, groups - last.Section - 1);

		return GetContentEnd() + Max(0, remainingItems) * _averageLineHeight + remainingHeaders * EstimatedHeaderExtent;
	}

	/// <summary>The estimated start of an element from the top of the list, from the average extents.</summary>
	private double EstimateGroupedStart(ElementPath target)
	{
		var header = EstimatedHeaderExtent;
		var position = 0d;
		for (var group = 0; group < target.Section && group < DisplayGroupCount; group++)
		{
			position += header + ItemCountOfGroup(group) * _averageLineHeight;
		}

		if (target.Row >= 0)
		{
			position += header + target.Row * _averageLineHeight;
		}

		return position;
	}

	/// <summary>
	/// The element estimated to sit at <paramref name="offset"/> and its estimated start; returns the element BEFORE it as
	/// the seed (the fill starts after the seed), as the ungrouped large-scroll path does.
	/// </summary>
	private (ElementPath? Seed, double Start) EstimateGroupedSeed(double offset)
	{
		var header = EstimatedHeaderExtent;
		var line = _averageLineHeight;
		var groups = DisplayGroupCount;
		var position = 0d;
		ElementPath? target = null;
		var start = 0d;

		for (var group = 0; group < groups && target is null; group++)
		{
			if (offset < position + header)
			{
				target = HeaderPath(group);
				start = position;
				break;
			}

			position += header;
			var count = ItemCountOfGroup(group);
			if (line > 0 && count > 0 && offset < position + count * line)
			{
				var row = Min(count - 1, (int)((offset - position) / line));
				target = ElementPath.FromRowSection(row, group);
				start = position + row * line;
				break;
			}

			position += count * line;
		}

		if (target is null)
		{
			target = GetLastGroupedElement();
			start = Max(0, position - (target is { Row: -1 } ? header : line));
		}

		return (target is { } t ? GetNextGroupedElement(t, Backward) : null, start);
	}

	// ------------------------------------------------------------------ sticky headers

	/// <summary>
	/// With AreStickyGroupHeadersEnabled (Inline placement), arranges the header of the group under the top of the
	/// viewport at the top of the viewport (pushed up by the end of its group); when that header's line is not realized, a
	/// dedicated sticky header container stands in for it.
	/// </summary>
	private void ArrangeStickyHeader(Size finalSize, Size window)
	{
		if (!_showsGroupHeaders
			|| !AreStickyGroupHeadersEnabled
			|| RelativeGroupHeaderPlacement != RelativeHeaderPlacement.Inline
			|| ScrollViewer is null)
		{
			HideStickyHeader();
			return;
		}

		var viewportStart = ViewportStart;

		// The first item line that reaches into the viewport, and the group it belongs to.
		Line? current = null;
		var currentIndex = -1;
		for (var i = 0; i < _materializedLines.Count; i++)
		{
			var line = _materializedLines[i];
			if (!line.IsHeader && GetMeasuredEnd(line.FirstView) > viewportStart)
			{
				current = line;
				currentIndex = i;
				break;
			}
		}

		if (current is null)
		{
			HideStickyHeader();
			return;
		}

		var group = current.FirstItem.Section;

		// Where the group ends: the start of the next header, else the end of its last item when that is realized.
		var groupEnd = double.PositiveInfinity;
		for (var i = currentIndex + 1; i < _materializedLines.Count; i++)
		{
			var line = _materializedLines[i];
			if (line.IsHeader)
			{
				groupEnd = GetMeasuredStart(line.FirstView);
				break;
			}

			if (line.FirstItem.Section != group)
			{
				break;
			}

			if (line.LastItem.Row == ItemCountOfGroup(group) - 1)
			{
				groupEnd = GetMeasuredEnd(line.LastView);
			}
		}

		if (current.LastItem.Row == ItemCountOfGroup(group) - 1 && double.IsPositiveInfinity(groupEnd))
		{
			groupEnd = GetMeasuredEnd(current.LastView);
		}

		FrameworkElement header;
		Rect bounds;
		var headerLine = FindHeaderLine(group);
		if (headerLine is not null)
		{
			HideStickyHeader();
			header = headerLine.FirstView;
			bounds = GetBoundsForElement(header);
			if (GetMeasuredStart(header) >= viewportStart)
			{
				// The header is where it belongs (below the top of the viewport): nothing sticks.
				return;
			}
		}
		else
		{
			header = EnsureStickyHeader(group);
			bounds = new Rect(default, header.DesiredSize);
		}

		var extent = GetExtent(header.DesiredSize);
		var start = Min(viewportStart, groupEnd - extent);
		var stuck = IsHorizontal
			? new Rect(start, bounds.Y, bounds.Width, bounds.Height)
			: new Rect(bounds.X, start, bounds.Width, bounds.Height);

		if (GroupLayoutAdjusted)
		{
			// GroupPadding (WPE1-10): the sticky header keeps its group's breadth inset.
			if (headerLine is null)
			{
				stuck = IsHorizontal
					? new Rect(stuck.X, GroupPaddingBreadthStart, stuck.Width, stuck.Height)
					: new Rect(GroupPaddingBreadthStart, stuck.Y, stuck.Width, stuck.Height);
			}

			header.Arrange(AdjustGroupedArrangeBounds(stuck, GetElementArrangeBounds(-1, stuck, window, finalSize), isAdjacentHeader: false));
			return;
		}

		header.Arrange(GetElementArrangeBounds(-1, stuck, window, finalSize));
	}

	private Line? FindHeaderLine(int group)
	{
		foreach (var line in _materializedLines)
		{
			if (line.IsHeader && line.Section == group)
			{
				return line;
			}
		}

		return null;
	}

	/// <summary>The sticky header container, prepared for <paramref name="group"/> and measured.</summary>
	private ContentControl EnsureStickyHeader(int group)
	{
		var itemsControl = ItemsControl!;
		if (_stickyHeader is null)
		{
			_stickyHeader = itemsControl.GetGroupHeaderContainer(itemsControl.GetGroupAtDisplaySection(group)?.Group);
			Canvas.SetZIndex(_stickyHeader, HeaderZIndex);
			_stickyHeaderGroup = -1;
		}

		var interceptWas = OwnerPanel.ShouldInterceptInvalidate;
		OwnerPanel.ShouldInterceptInvalidate = true;
		try
		{
			if (_stickyHeader.Parent is null)
			{
				OwnerPanel.Children.Add(_stickyHeader);
			}

			if (_stickyHeaderGroup != group)
			{
				itemsControl.PrepareGroupHeaderContainer(_stickyHeader, group);
				_stickyHeaderGroup = group;
			}

			_stickyHeader.Visibility = Visibility.Visible;
			_stickyHeader.Measure(IsHorizontal
				? new Size(double.PositiveInfinity, AvailableBreadth)
				: new Size(AvailableBreadth, double.PositiveInfinity));
		}
		finally
		{
			OwnerPanel.ShouldInterceptInvalidate = interceptWas;
		}

		return _stickyHeader;
	}

	private void HideStickyHeader()
	{
		if (_stickyHeader is { Visibility: Visibility.Visible } sticky)
		{
			sticky.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>
	/// The extent a sticky header covers at the top of the viewport while <paramref name="target"/> is shown there (0
	/// without sticky headers).
	/// </summary>
	private double GetStickyHeaderExtent(ElementPath target)
	{
		if (!AreStickyGroupHeadersEnabled || RelativeGroupHeaderPlacement != RelativeHeaderPlacement.Inline)
		{
			return 0;
		}

		if (FindHeaderLine(target.Section) is { } headerLine)
		{
			return GetMeasuredExtent(headerLine.FirstView);
		}

		return EstimatedHeaderExtent;
	}
}

#endif
