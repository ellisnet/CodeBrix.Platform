#nullable enable

using System;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// What a platform element handler (<see cref="IElementHandler"/>) takes over from the platform-neutral element
/// pipeline for the one element it is attached to.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered.
/// <para>
/// Core reads <see cref="IElementHandler.Capabilities"/> once when the handler connects and again each time the
/// handler calls <c>UIElement.NotifyHandlerCapabilitiesChanged()</c>; it keeps the value it read, so a handler that
/// changes its capabilities must always make that call.
/// </para>
/// </remarks>
[Flags]
internal enum ElementHandlerCapabilities
{
	/// <summary>
	/// The handler only mirrors the element (it receives <see cref="IElementHandler.UpdateValue"/>, the child
	/// notifications and <see cref="IElementHandler.Arrange"/>); everything Core does for the element is unchanged.
	/// </summary>
	None = 0,

	/// <summary>
	/// The platform draws the element: Core never materializes the element's ControlTemplate (a template that was
	/// already applied is released when the handler connects, through <c>Control.ReleaseTemplateForHandler()</c>),
	/// reports the suppressed template once through <see cref="IElementHandler.OnTemplateSuppressed"/>, and asks
	/// <see cref="IElementHandler.HitTest"/> instead of the element's composition visual.
	/// </summary>
	OwnsVisuals = 1 << 0,

	/// <summary>
	/// The platform views of the element's visual children are parented under this handler's platform view (a
	/// panel or a content host). Core itself does nothing different; the flag tells the platform's other handlers
	/// where to put their views.
	/// </summary>
	OwnsChildren = 1 << 1,

	/// <summary>
	/// For a ContentControl: its Content is hosted directly as the ContentTemplateRoot (Core's content presenter
	/// bypass, with the DataTemplate and the implicit TextBlock for non-element content) even though its default
	/// style sets a Template. Combine with <see cref="OwnsVisuals"/>: without it the Template still applies.
	/// </summary>
	HostsContent = 1 << 2,

	/// <summary>
	/// The element's MeasureOverride and ArrangeOverride are replaced by <see cref="IElementHandler.Measure"/> and
	/// the handler's own arrangement: Core passes the handler the same available size MeasureOverride would get,
	/// keeps margins, Min/Max clamping, alignment and layout rounding, and arranges the element at the size it
	/// arranged it to. The handler may measure and arrange children itself through the public
	/// UIElement.Measure/Arrange.
	/// </summary>
	MeasuresNatively = 1 << 3,

	/// <summary>
	/// For a ScrollViewer: the platform scrolls. ChangeView requests go to
	/// <see cref="IElementHandler.Invoke"/> with <see cref="ElementHandlerCommands.ChangeView"/> and a
	/// <see cref="ChangeViewRequest"/>, the managed ScrollContentPresenter stops translating its content, and the
	/// platform reports its scroll position through the existing internal ScrollViewer.OnPresenterScrolled.
	/// </summary>
	OwnsScrolling = 1 << 4,

	/// <summary>
	/// The platform consumes pointer input for the element: Core's managed gesture recognizer is not fed for it,
	/// so it produces no Tapped, DoubleTapped, RightTapped or Holding for it (a native click and a managed Tapped
	/// are never both produced for one tap). Routed Pointer* events, if wanted, are re-raised by the handler.
	/// </summary>
	OwnsInput = 1 << 5,

	/// <summary>
	/// For an ItemsControl (ItemsControl, Selector, ListViewBase and their subclasses): the platform realizes the item
	/// containers itself (a recycling native list). The handler must also implement <see cref="IItemsHostHandler"/>.
	/// Core then generates no containers into an items panel (ItemsControl.UpdateItems tells the handler instead,
	/// through <see cref="IItemsHostHandler.OnItemsChanged"/>), reads the materialized containers from
	/// <see cref="IItemsHostHandler.GetMaterializedContainers"/> (so ContainerFromIndex, ContainerFromItem,
	/// IndexFromContainer and selection state work on the handler's containers), sends ListViewBase.ScrollIntoView to
	/// <see cref="IElementHandler.Invoke"/> with <see cref="ElementHandlerCommands.ScrollIntoView"/> and a
	/// <see cref="ScrollIntoViewRequest"/>, and asks the handler instead of the items panel's layouter for the visible
	/// range (page size, incremental loading) and for item additions and removals. The handler creates and recycles
	/// containers through Core's internal ItemsControl.CreateContainerForItemsHost / PrepareContainerForItemsHost /
	/// ReleaseContainerFromItemsHost, so templates, ItemContainerStyle and selection stay Core's. Combine with
	/// <see cref="OwnsVisuals"/>: without it the template (and its items panel) still applies and Core's own panel
	/// would generate containers too.
	/// </summary>
	OwnsItemsHost = 1 << 6,

	/// <summary>
	/// The handler outlives a Leave: when the element leaves the live tree Core calls
	/// <see cref="IElementHandler.Disconnect"/> but keeps the handler attached, and when the element enters a live tree
	/// again Core connects the SAME handler (<see cref="IElementHandler.Connect"/>; the factory is not asked), so a
	/// recycled row keeps its native view. Core reads <see cref="IElementHandler.Capabilities"/> again at that
	/// Connect. While the element is out of the tree the retained handler still receives
	/// <see cref="IElementHandler.UpdateValue"/> and the child notifications (its native view stays current). A handler
	/// that is dropped for good (its native view destroyed) is released with the internal
	/// UIElement.ReleaseRetainedHandler(); a handler that no longer has this flag when the element leaves is
	/// disconnected and forgotten as usual.
	/// </summary>
	RetainedAcrossLeave = 1 << 7,
}
