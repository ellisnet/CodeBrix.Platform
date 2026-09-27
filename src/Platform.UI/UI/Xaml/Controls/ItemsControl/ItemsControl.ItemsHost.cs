#nullable enable

using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.UI.Contracts;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// The ItemsControl side of the items-host seam (<see cref="ElementHandlerCapabilities.OwnsItemsHost"/>,
	/// <see cref="IItemsHostHandler"/>): the platform list realizes the containers and Core asks it for them.
	/// </summary>
	/// <remarks>
	/// Every hook first reads <see cref="UIElement.AreHandlersActive"/>; with no handler service registered (the Skia
	/// heads) <see cref="ItemsHostHandler"/> is <see langword="null"/> and the control manages its items panel as before.
	/// </remarks>
	public partial class ItemsControl
	{
		/// <summary>
		/// Gets the items host of this control: its attached handler when that handler has
		/// <see cref="ElementHandlerCapabilities.OwnsItemsHost"/> and implements <see cref="IItemsHostHandler"/>;
		/// otherwise <see langword="null"/>.
		/// </summary>
		internal IItemsHostHandler? ItemsHostHandler
			=> AreHandlersActive && TryGetHandlerWith(ElementHandlerCapabilities.OwnsItemsHost, out var handler)
				? handler as IItemsHostHandler
				: null;

		/// <summary>
		/// Entry point for an items host: creates (or takes the item itself, or the template root, as) the container of
		/// the item at <paramref name="index"/> and prepares it for that index - Core's own container path
		/// (IsItemItsOwnContainer, ItemTemplate/ItemTemplateSelector, ItemContainerStyle, selection state).
		/// </summary>
		/// <param name="index">The flat index of the item.</param>
		/// <returns>The prepared container.</returns>
		internal DependencyObject CreateContainerForItemsHost(int index)
		{
			var container = GetContainerForIndex(index);
			PrepareContainerForIndex(container, index);
			return container;
		}

		/// <summary>
		/// Entry point for an items host: prepares a container the host recycled (it was released with
		/// <see cref="ReleaseContainerFromItemsHost"/>, or it is being re-bound after a Replace) for the item at
		/// <paramref name="index"/>.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="index">The flat index of the item it now shows.</param>
		internal void PrepareContainerForItemsHost(DependencyObject container, int index)
			=> PrepareContainerForIndex(container, index);

		/// <summary>
		/// Entry point for an items host: the container no longer shows an item (scrolled out and recycled, or its item
		/// was removed). Clears it the way Core clears the containers of its own panels and forgets its index.
		/// </summary>
		/// <param name="container">The container.</param>
		internal void ReleaseContainerFromItemsHost(DependencyObject container)
		{
			CleanUpContainer(container);
			container.ClearValue(IndexForItemContainerProperty);
		}

		/// <summary>
		/// Hook H14 (items changed): hands the change to the items host instead of the items panel.
		/// </summary>
		/// <param name="args">The collection change, or <see langword="null"/> for a full refresh.</param>
		/// <returns><see langword="true"/> when an items host took the change (Core does nothing more with its panel).</returns>
		private bool TryNotifyItemsHost(System.Collections.Specialized.NotifyCollectionChangedEventArgs? args)
		{
			if (ItemsHostHandler is { } host)
			{
				host.OnItemsChanged(args);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Hook H14 (materialized containers): the containers the items host realized, when there is one.
		/// </summary>
		/// <param name="containers">The host's containers, when the method returns <see langword="true"/>.</param>
		/// <returns><see langword="true"/> when an items host answered.</returns>
		private bool TryGetItemsHostContainers(out IEnumerable<DependencyObject> containers)
		{
			if (ItemsHostHandler is { } host)
			{
				containers = host.GetMaterializedContainers() ?? Enumerable.Empty<DependencyObject>();
				return true;
			}

			containers = Enumerable.Empty<DependencyObject>();
			return false;
		}

		// ---------------------------------------------------------------- group headers (WPE1-8)

		/// <summary>
		/// Entry point for an items host: whether the list shows group headers (a grouped items source AND a GroupStyle).
		/// When false the host shows the flat items only (a grouped source without a GroupStyle shows no headers).
		/// </summary>
		internal bool ItemsHostShowsGroupHeaders => ShowsGroupHeaders;

		/// <summary>Entry point for an items host: the number of displayed groups (0 when the list shows no group headers).</summary>
		/// <returns>The number of displayed groups (empty groups excluded when GroupStyle.HidesIfEmpty is true).</returns>
		internal int GetItemsHostGroupCount() => ShowsGroupHeaders ? NumberOfDisplayGroups : 0;

		/// <summary>
		/// Entry point for an items host: the displayed group at <paramref name="displayGroupIndex"/> - its first flat item
		/// index, its item count and its group object. The host draws the group's header before the item at FirstIndex (an
		/// empty group: a header with no items).
		/// </summary>
		/// <param name="displayGroupIndex">The display index of the group (0 .. <see cref="GetItemsHostGroupCount"/> - 1).</param>
		/// <returns>The group.</returns>
		internal ItemsHostGroup GetItemsHostGroup(int displayGroupIndex)
		{
			var group = GetGroupAtDisplaySection(displayGroupIndex);
			var firstIndex = GetIndexFromIndexPath(CodeBrix.Platform.UI.IndexPath.FromRowSection(0, displayGroupIndex));
			return new ItemsHostGroup(firstIndex, group?.GroupItems?.Count ?? 0, group?.Group);
		}

		/// <summary>
		/// Entry point for an items host: creates the header container of a displayed group (ListViewHeaderItem for a
		/// ListView, GridViewHeaderItem for a GridView) and prepares it the way Core's own panels do (Content/DataContext =
		/// the group, GroupStyle.HeaderTemplate / HeaderTemplateSelector / HeaderContainerStyle).
		/// </summary>
		/// <param name="displayGroupIndex">The display index of the group.</param>
		/// <returns>The prepared header container.</returns>
		internal DependencyObject CreateGroupHeaderContainerForItemsHost(int displayGroupIndex)
			=> CreateGroupHeaderContainer(displayGroupIndex);

		/// <summary>Entry point for an items host: re-binds a recycled header container to another displayed group.</summary>
		/// <param name="container">A header container from <see cref="CreateGroupHeaderContainerForItemsHost"/>.</param>
		/// <param name="displayGroupIndex">The display index of the group it now shows.</param>
		internal void PrepareGroupHeaderContainerForItemsHost(DependencyObject container, int displayGroupIndex)
		{
			if (container is ContentControl header)
			{
				PrepareGroupHeaderContainer(header, displayGroupIndex);
			}
		}

		/// <summary>Entry point for an items host: the header container no longer shows a group (scrolled out, or the groups changed).</summary>
		/// <param name="container">A header container from <see cref="CreateGroupHeaderContainerForItemsHost"/>.</param>
		internal void ReleaseGroupHeaderContainerFromItemsHost(DependencyObject container)
		{
			if (container is ContentControl header)
			{
				ClearGroupHeaderContainer(header);
			}
		}
	}
}
