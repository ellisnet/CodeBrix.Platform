#if HAS_CODEBRIX
using Microsoft.UI.Xaml;
using Windows.UI;

namespace CodeBrix.Platform.UI.Toolkit.Contracts;

/// <summary>
/// Renders the drop shadow behind an element that the Toolkit's elevation API
/// (<see cref="UIElementExtensions.SetElevation"/> and <see cref="ElevatedView"/>) asks for.
/// The elevation arithmetic that the platform needs is part of the implementation, because each
/// platform expresses shadows differently.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once by <see cref="UIElementExtensions"/> (lazily, on first use) through
/// <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Toolkit.Skia.ElevationSkiaPlatform</c>, registered by the assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IElevationPlatform
{
	/// <summary>
	/// Applies (or, for an elevation of zero, clears) the shadow of <paramref name="element"/>.
	/// </summary>
	/// <param name="element">The element that casts the shadow.</param>
	/// <param name="elevation">The elevation, in logical pixels; zero means no shadow.</param>
	/// <param name="shadowColor">The shadow color, including its alpha.</param>
	void SetElevation(UIElement element, double elevation, Color shadowColor);
}
#endif
