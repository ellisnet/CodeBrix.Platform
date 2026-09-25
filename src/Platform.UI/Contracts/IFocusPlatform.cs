#nullable enable

using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Mirrors the XAML focus onto the platform's own notion of focus (for example the accessibility focus of a
/// browser's semantic DOM, or a native view's focus).
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Called by <see cref="Microsoft.UI.Xaml.Input.FocusManager"/> each time the focused element changes. The Skia
/// implementation is <c>CodeBrix.Platform.UI.Skia.FocusSkiaPlatform</c>, registered by
/// <c>CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IFocusPlatform
{
	/// <summary>
	/// Moves the platform's native focus to the element that just received the XAML focus.
	/// </summary>
	/// <param name="element">The newly focused element, or <see langword="null"/> when it is not a <see cref="UIElement"/>.</param>
	void FocusNative(UIElement? element);
}
