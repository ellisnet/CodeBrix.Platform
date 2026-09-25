#nullable enable

using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The items-host side of an element handler (<see cref="IElementHandler"/>) whose capabilities include
/// <see cref="ElementHandlerCapabilities.OwnsItemsHost"/>: the platform list (a recycling native list view) realizes
/// the containers of an ItemsControl, and Core asks it for them instead of managing an items panel.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile (on the same object as the list's <see cref="IElementHandler"/>). Platform (Skia):
/// not registered.
/// <para>
/// Core never creates, prepares or cleans up containers for an items-host control on its own: the handler does it
/// through the internal ItemsControl entry points (CreateContainerForItemsHost, PrepareContainerForItemsHost,
/// ReleaseContainerFromItemsHost), which run Core's normal container path (templates, ItemContainerStyle,
/// IsItemItsOwnContainer, selection state), and puts the containers into the live tree itself (under an element it
/// owns). A container is "materialized" while the handler reports it from <see cref="GetMaterializedContainers"/>.
/// </para>
/// </remarks>
internal interface IItemsHostHandler
{
	/// <summary>
	/// Returns the containers the platform list has realized right now, each prepared for its index (Core reads the
	/// index from the container). Core calls it for ContainerFromIndex / ContainerFromItem / IndexFromContainer,
	/// selection updates and index repair after a collection change, so it must be cheap and must not realize anything.
	/// </summary>
	/// <returns>The realized containers (never <see langword="null"/>).</returns>
	IEnumerable<DependencyObject> GetMaterializedContainers();

	/// <summary>
	/// Tells the handler that the items changed. <paramref name="args"/> is the collection change of the (flat or
	/// grouped) items source, or <see langword="null"/> for "everything may have changed" (a new ItemsSource, a new
	/// template, a reset). Called after Core updated its item bookkeeping and before it repairs the indexes of the
	/// materialized containers of a ListViewBase; containers of removed items are released by the handler.
	/// </summary>
	/// <param name="args">The change, or <see langword="null"/> for a full refresh.</param>
	void OnItemsChanged(NotifyCollectionChangedEventArgs? args);

	/// <summary>Gets the flat index of the first item the platform list shows, or -1 when it shows none.</summary>
	int FirstVisibleIndex { get; }

	/// <summary>Gets the flat index of the last item the platform list shows, or -1 when it shows none.</summary>
	int LastVisibleIndex { get; }
}
