#nullable enable

using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Creates the platform handler of an element when it enters a live visual tree: the per-control seam through which
/// a platform shows elements as its own native controls instead of drawing Core's composition visuals.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered.
/// <para>
/// OPTIONAL service. When no implementation is registered (the Skia heads), <c>UIElement.AreHandlersActive</c> is
/// <see langword="false"/> and every hook in Core is one static check that is false: Core behaves exactly as it
/// does without the seam. A platform registers its factory through
/// <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/> from its bootstrap, before the first
/// element is created, and then calls <c>UIElement.RefreshElementHandlerServices()</c> (Core also resolves the
/// service lazily the first time an element type is used). Core resolves the factory once, with
/// <see cref="PlatformContract.TryResolve{TContract}"/>, into <see cref="PlatformServices.ElementHandlerFactory"/>.
/// </para>
/// </remarks>
internal interface IElementHandlerFactoryPlatform
{
	/// <summary>
	/// Creates the handler for <paramref name="element"/>. Called once each time the element enters a live visual
	/// tree (again after a Leave/Enter cycle); never for an element that enters as a resource.
	/// </summary>
	/// <param name="element">The element that just became live.</param>
	/// <returns>The handler, or <see langword="null"/> to leave the element on Core's own path (template expansion,
	/// composition visuals and managed input).</returns>
	IElementHandler? CreateHandler(UIElement element);
}
