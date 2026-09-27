#nullable enable

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// One displayed group of a grouped list, as Core reports it to an items host
/// (<see cref="ElementHandlerCapabilities.OwnsItemsHost"/>) that draws group headers itself: where the group's items sit in
/// the flat item indexes, and the group object its header shows.
/// </summary>
/// <remarks>
/// Implementers: none (a value passed to Android and Mobile items hosts). Platform (Skia): not used - Core's own panels
/// realize the headers. Read with ItemsControl.GetItemsHostGroup; the header container itself comes from
/// ItemsControl.CreateGroupHeaderContainerForItemsHost.
/// </remarks>
internal readonly struct ItemsHostGroup
{
	/// <summary>Initializes a new instance of the <see cref="ItemsHostGroup"/> struct.</summary>
	/// <param name="firstIndex">The flat index of the group's first item (for an empty group: the index its first item would have).</param>
	/// <param name="count">The number of items in the group (0 for an empty group that is shown).</param>
	/// <param name="group">The group object (ICollectionViewGroup.Group) - the header's Content and DataContext.</param>
	internal ItemsHostGroup(int firstIndex, int count, object? group)
	{
		FirstIndex = firstIndex;
		Count = count;
		Group = group;
	}

	/// <summary>Gets the flat index of the group's first item (for an empty group: the index its first item would have).</summary>
	internal int FirstIndex { get; }

	/// <summary>Gets the number of items in the group (0 for an empty group, shown when GroupStyle.HidesIfEmpty is false).</summary>
	internal int Count { get; }

	/// <summary>Gets the group object (ICollectionViewGroup.Group): the header's Content and DataContext.</summary>
	internal object? Group { get; }
}
