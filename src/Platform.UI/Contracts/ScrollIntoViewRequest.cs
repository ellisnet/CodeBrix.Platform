#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The argument of <see cref="ElementHandlerCommands.ScrollIntoView"/>: the parameters of the ListViewBase.ScrollIntoView
/// call Core forwards to a handler that has <see cref="ElementHandlerCapabilities.OwnsItemsHost"/>.
/// </summary>
/// <remarks>
/// Implementers: none (a value passed to Android and Mobile handlers). Platform (Skia): not used.
/// </remarks>
internal sealed class ScrollIntoViewRequest
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ScrollIntoViewRequest"/> class.
	/// </summary>
	/// <param name="item">The item to bring into view.</param>
	/// <param name="index">The item's flat index in the list, or -1 when the item is not in the list.</param>
	/// <param name="alignment">Where the item should end up in the viewport.</param>
	internal ScrollIntoViewRequest(object? item, int index, ScrollIntoViewAlignment alignment)
	{
		Item = item;
		Index = index;
		Alignment = alignment;
	}

	/// <summary>Gets the item to bring into view.</summary>
	internal object? Item { get; }

	/// <summary>Gets the item's flat index in the list (ItemsControl's own index), or -1 when the item is not in the list.</summary>
	internal int Index { get; }

	/// <summary>Gets where the item should end up: Default = the nearest edge (nothing when already fully visible), Leading = the start of the viewport.</summary>
	internal ScrollIntoViewAlignment Alignment { get; }
}
