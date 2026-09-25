#nullable enable

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The platform side of one element: created by <see cref="IElementHandlerFactoryPlatform.CreateHandler"/> when the
/// element enters a live visual tree, connected to it, told about every effective property change and child change,
/// and disconnected when the element leaves the live tree (a new handler is created on the next entry).
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered.
/// <para>
/// All members are called on the UI thread, from inside Core's own tree, property and layout funnels, so a member
/// must not re-enter the same funnel for the same element synchronously in a loop (for example, writing back the
/// value it was just told about). Core stores the handler in the element (<c>UIElement.Handler</c>) and reads
/// <see cref="Capabilities"/> when it connects the handler and when the handler calls
/// <c>UIElement.NotifyHandlerCapabilitiesChanged()</c>.
/// </para>
/// </remarks>
internal interface IElementHandler
{
	/// <summary>
	/// Gets what the handler takes over from Core for its element. When the value changes, the handler must call
	/// <c>UIElement.NotifyHandlerCapabilitiesChanged()</c> on its element.
	/// </summary>
	ElementHandlerCapabilities Capabilities { get; }

	/// <summary>
	/// Gets the platform view of the element (an <c>Android.Views.View</c> on Android), or <see langword="null"/>.
	/// </summary>
	object? PlatformView { get; }

	/// <summary>
	/// Connects the handler to its element. Called once, right after the element became live and before its
	/// children enter the live tree (a parent connects before its children). Children that are already in the
	/// element's visual tree are visible through VisualTreeHelper; later ones arrive through
	/// <see cref="OnChildAdded"/>. A handler with <see cref="ElementHandlerCapabilities.OwnsVisuals"/> is connected
	/// after the element's already-applied template, if any, was released.
	/// </summary>
	/// <param name="element">The element the handler was created for.</param>
	void Connect(UIElement element);

	/// <summary>
	/// Disconnects the handler from its element. Called once, when the element leaves the live tree, after its
	/// children left (a child disconnects before its parent). The element forgets the handler right after.
	/// </summary>
	void Disconnect();

	/// <summary>
	/// Called after the effective value of <paramref name="property"/> changed on the element, whatever the source
	/// (a local value, a style setter, a binding, an inherited value or a theme change). The handler reads the new
	/// value from the element. Called after the element's own change callbacks ran.
	/// </summary>
	/// <param name="property">The property whose effective value changed (attached properties included).</param>
	void UpdateValue(DependencyProperty property);

	/// <summary>
	/// Runs an imperative command for which there is no property (see <see cref="ElementHandlerCommands"/>).
	/// </summary>
	/// <param name="command">One of the <see cref="ElementHandlerCommands"/> constants.</param>
	/// <param name="args">The command's argument (for <see cref="ElementHandlerCommands.ChangeView"/>, a
	/// <see cref="ChangeViewRequest"/>), or <see langword="null"/>.</param>
	/// <returns>The command's result; for <see cref="ElementHandlerCommands.ChangeView"/>, the value ChangeView
	/// returns to the application.</returns>
	bool Invoke(string command, object? args);

	/// <summary>
	/// Called after <paramref name="child"/> was added to the element's visual children (a panel child, a
	/// ContentTemplateRoot, a template root or a popup child) and entered the tree.
	/// </summary>
	/// <param name="child">The new visual child.</param>
	/// <param name="index">Its index among the element's visual children.</param>
	void OnChildAdded(UIElement child, int index);

	/// <summary>
	/// Called after <paramref name="child"/> was removed from the element's visual children and left the tree.
	/// </summary>
	/// <param name="child">The removed visual child.</param>
	void OnChildRemoved(UIElement child);

	/// <summary>
	/// Called after a visual child moved from <paramref name="oldIndex"/> to <paramref name="newIndex"/>.
	/// </summary>
	/// <param name="oldIndex">The child's previous index.</param>
	/// <param name="newIndex">The child's new index.</param>
	void OnChildMoved(int oldIndex, int newIndex);

	/// <summary>
	/// Measures the element (<see cref="ElementHandlerCapabilities.MeasuresNatively"/> only), in place of its
	/// MeasureOverride, with the same input.
	/// </summary>
	/// <param name="availableSize">The available size without margins, clamped to Min/Max, in DIPs.</param>
	/// <returns>The element's desired size without margins, in DIPs.</returns>
	Size Measure(Size availableSize);

	/// <summary>
	/// Called for every handler after Core arranged the element: the element's arranged rectangle, relative to
	/// its visual parent, in DIPs (the platform view is placed there). With
	/// <see cref="ElementHandlerCapabilities.MeasuresNatively"/> Core does not call ArrangeOverride; this call is
	/// the element's arrangement.
	/// </summary>
	/// <param name="finalRect">The element's position (layout offset, margins and alignment applied) and size.</param>
	void Arrange(Rect finalRect);

	/// <summary>
	/// Hit-tests the element (<see cref="ElementHandlerCapabilities.OwnsVisuals"/> only), in place of its
	/// composition visual. Clipping is not taken into account.
	/// </summary>
	/// <param name="relativeLocation">The point, in the element's coordinates.</param>
	/// <returns><see langword="true"/> when the point hits the element.</returns>
	bool HitTest(Point relativeLocation);

	/// <summary>
	/// Diagnostics: called once for each template Core did not materialize because the handler owns the visuals.
	/// </summary>
	/// <param name="template">The suppressed template.</param>
	void OnTemplateSuppressed(FrameworkTemplate? template);
}
