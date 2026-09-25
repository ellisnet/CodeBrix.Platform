#nullable enable

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The platform side of <see cref="Microsoft.UI.Xaml.Application"/>: what the application object asks of the platform
/// it runs on, at the points where it asks.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The Skia implementation is <c>CodeBrix.Platform.UI.Skia.ApplicationSkiaPlatform</c>, registered by
/// <c>CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IApplicationPlatform
{
	/// <summary>
	/// Registers the platform's <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/> extensions
	/// that exist only once an application type is in use. Called once, from the static constructor of
	/// <see cref="Microsoft.UI.Xaml.Application"/>, right after the platform-neutral extensions are registered.
	/// </summary>
	void RegisterExtensions();
}
