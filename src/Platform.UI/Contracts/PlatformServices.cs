#nullable enable

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Holds the service contracts of this assembly, each resolved once (on first use) and then kept for the life of the
/// process. Platform-neutral code calls through these properties; it never looks a contract up per operation.
/// </summary>
internal static class PlatformServices
{
	private static IApplicationPlatform? _application;
	private static IFontPlatform? _fonts;
	private static IFocusPlatform? _focus;
	private static IRenderingPlatform? _rendering;
	private static IGeometryPlatform? _geometry;
	private static IImagingPlatform? _imaging;
	private static ITextPlatform? _text;

	/// <summary>
	/// Gets the platform side of the application object.
	/// </summary>
	internal static IApplicationPlatform Application => _application ??= PlatformContract.Resolve<IApplicationPlatform>();

	/// <summary>
	/// Gets the platform that loads fonts.
	/// </summary>
	internal static IFontPlatform Fonts => _fonts ??= PlatformContract.Resolve<IFontPlatform>();

	/// <summary>
	/// Gets the platform that mirrors the XAML focus onto the platform's native focus.
	/// </summary>
	internal static IFocusPlatform Focus => _focus ??= PlatformContract.Resolve<IFocusPlatform>();

	/// <summary>
	/// Gets the platform that renders the content of each XAML root.
	/// </summary>
	internal static IRenderingPlatform Rendering => _rendering ??= PlatformContract.Resolve<IRenderingPlatform>();

	/// <summary>
	/// Gets the platform that turns geometries and shape outlines into platform paths.
	/// </summary>
	internal static IGeometryPlatform Geometry => _geometry ??= PlatformContract.Resolve<IGeometryPlatform>();

	/// <summary>
	/// Gets the platform side of the imaging pipeline.
	/// </summary>
	internal static IImagingPlatform Imaging => _imaging ??= PlatformContract.Resolve<IImagingPlatform>();

	/// <summary>
	/// Gets the text engine: text layout, and the platform side of each text box.
	/// </summary>
	internal static ITextPlatform Text => _text ??= PlatformContract.Resolve<ITextPlatform>();

	/// <summary>
	/// Gets the platform's element handler factory, or <see langword="null"/> when the platform shows elements
	/// through Core's own visuals (the Skia heads). Set by <see cref="ResolveElementHandlerServices"/>.
	/// </summary>
	internal static IElementHandlerFactoryPlatform? ElementHandlerFactory { get; private set; }

	/// <summary>
	/// Gets the platform's dialog and flyout presenter, or <see langword="null"/> when dialogs and flyouts use Core's
	/// popups (the Skia heads). Set by <see cref="ResolveElementHandlerServices"/>.
	/// </summary>
	internal static IOverlayPresenterPlatform? OverlayPresenter { get; private set; }

	/// <summary>
	/// Resolves the two OPTIONAL element handler services (<see cref="IElementHandlerFactoryPlatform"/> and
	/// <see cref="IOverlayPresenterPlatform"/>) with <see cref="PlatformContract.TryResolve{TContract}"/>. Called
	/// once when <c>UIElement.AreHandlersActive</c> is first read, and again by
	/// <c>UIElement.RefreshElementHandlerServices()</c>; never per operation.
	/// </summary>
	/// <returns><see langword="true"/> when either service is registered.</returns>
	internal static bool ResolveElementHandlerServices()
	{
		ElementHandlerFactory = PlatformContract.TryResolve<IElementHandlerFactoryPlatform>();
		OverlayPresenter = PlatformContract.TryResolve<IOverlayPresenterPlatform>();

		return ElementHandlerFactory is not null || OverlayPresenter is not null;
	}
}
