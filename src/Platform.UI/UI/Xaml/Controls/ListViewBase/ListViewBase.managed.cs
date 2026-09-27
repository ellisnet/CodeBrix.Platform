#if CODEBRIX_REFERENCE_API
#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using CodeBrix.Platform.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ListViewBase
	{
		private int PageSize
		{
			get
			{
				if (ItemsHostHandler is { } host)
				{
					// Hook H14: the platform list knows what it shows (OwnsItemsHost).
					return host.LastVisibleIndex == -1 ? 0 : host.LastVisibleIndex - host.FirstVisibleIndex + 1;
				}

				if (VirtualizingPanel is null)
				{
					return 0;
				}

				var layouter = VirtualizingPanel.GetLayouter();
				var firstVisibleIndex = layouter.FirstVisibleIndex;
				var lastVisibleIndex = layouter.LastVisibleIndex;
				if (lastVisibleIndex == -1)
				{
					return 0;
				}

				return lastVisibleIndex - firstVisibleIndex + 1;
			}
		}

		private void AddItems(int firstItem, int count, int section)
		{
			if (ItemsHostHandler is not null)
			{
				// Hook H14: the items host is told through ItemsControl.UpdateItems (OwnsItemsHost).
				return;
			}

			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().AddItems(firstItem, count, section);
			}
			else
			{
				Refresh();
			}
		}

		private void RemoveItems(int firstItem, int count, int section)
		{
			if (ItemsHostHandler is not null)
			{
				// Hook H14: the items host is told through ItemsControl.UpdateItems (OwnsItemsHost).
				return;
			}

			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().RemoveItems(firstItem, count, section);
			}
			else
			{
				Refresh();
			}
		}

		private void AddGroup(int groupIndexInView)
		{
			Refresh();
		}

		private void RemoveGroup(int groupIndexInView)
		{
			Refresh();
		}

		private void ReplaceGroup(int groupIndexInView)
		{
			Refresh();
		}

		private ContentControl ContainerFromGroupIndex(int groupIndex)
			=> VirtualizingPanel?.GetLayouter()?.GetRealizedGroupHeader(groupIndex) ?? GetRealizedGroupHeader(groupIndex);

		private void TryLoadMoreItems()
		{
			if (ItemsHostHandler is { } host)
			{
				// Hook H14 (OwnsItemsHost); a host that scrolls calls the internal TryLoadMoreItems(int) itself.
				TryLoadMoreItems(host.LastVisibleIndex);
				return;
			}

			if (VirtualizingPanel.GetLayouter() is { } layouter)
			{
				TryLoadMoreItems(layouter.LastVisibleIndex);
			}
		}

		public void ScrollIntoView(object item) => ScrollIntoView(item, ScrollIntoViewAlignment.Default);

		public void ScrollIntoView(object item, ScrollIntoViewAlignment alignment)
		{
			if (ItemsHostHandler is not null && Handler is { } handler)
			{
				// Hook H15: the platform list scrolls (OwnsItemsHost); Core's ScrollViewer does not.
				handler.Invoke(
					CodeBrix.Platform.UI.Contracts.ElementHandlerCommands.ScrollIntoView,
					new CodeBrix.Platform.UI.Contracts.ScrollIntoViewRequest(item, IndexFromItem(item), alignment));
				return;
			}

			if (ShowsGroupHeaders && VirtualizingPanel?.GetLayouter() is { } groupedLayouter)
			{
				// Group headers (and a sticky header) take room in the list: the layout places the item.
				groupedLayouter.ScrollIntoView(item, alignment);
				return;
			}

			if (ContainerFromItem(item) is UIElement element)
			{
				// The container we want to jump to is already materialized, so just jump to it.
				// This means we're in a non-virtualizing panel or in a virtualizing panel where the container we want is materialized for some reason (e.g. partially in view)
				ScrollIntoViewFastPath(element, alignment);
			}
			else if (VirtualizingPanel?.GetLayouter() is { } layouter)
			{
				layouter.ScrollIntoView(item, alignment);
			}
		}

		private void ScrollIntoViewFastPath(UIElement element, ScrollIntoViewAlignment alignment)
		{
			if (ScrollViewer is { } sv && sv.Presenter is { } presenter)
			{
				var offsetXY = element.TransformToVisual(presenter).TransformPoint(
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__ // Skia correctly doesn't include the offsets in TransformToVisual
					new Point(presenter.HorizontalOffset, presenter.VerticalOffset)
#else
					Point.Zero
#endif
					);

				var orientation = ItemsPanelRoot?.PhysicalOrientation ?? Orientation.Vertical;

				var (elementOffset, elementLength, presenterOffset, presenterViewportLength) =
					orientation is Orientation.Vertical
						? (offsetXY.Y, element.ActualSize.Y, presenter.VerticalOffset, presenter.ViewportHeight)
						: (offsetXY.X, element.ActualSize.X, presenter.HorizontalOffset, presenter.ViewportWidth);

				if (presenterOffset <= elementOffset && elementOffset + elementLength <= presenterOffset + presenterViewportLength)
				{
					// if the element is within the visible viewport, do nothing.
					return;
				}

				// If we use the above offset directly, the item we want to jump to will be the start of the viewport, i.e. leading.
				// For the default alignment, we move the element to either of the viewport ends (i.e. to the top or the bottom of the
				// viewport. To move to the bottom, we scroll one "viewport page" less. This brings the element's start right after the
				// viewport's length ends we then scroll again by elementLength so that the end of the element is the end of the viewport.
				var newOffset = alignment is ScrollIntoViewAlignment.Default && presenterOffset < elementOffset
					? elementOffset - presenterViewportLength + elementLength
					: elementOffset;

				if (orientation is Orientation.Vertical)
				{
					sv.ScrollToVerticalOffset(newOffset);
				}
				else
				{
					sv.ScrollToHorizontalOffset(newOffset);
				}
			}
		}
	}
}
#endif
