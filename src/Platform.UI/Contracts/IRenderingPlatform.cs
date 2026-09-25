#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Renders the content of the XAML roots: the platform's frame loop, as seen from the platform-neutral layout code,
/// and the platform side of each <see cref="CompositionTarget"/>.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The Skia implementation is <c>CodeBrix.Platform.UI.Skia.RenderingSkiaPlatform</c> (the render scheduling of the
/// Skia <see cref="Microsoft.UI.Xaml.Media.CompositionTarget"/>), registered by
/// <c>CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IRenderingPlatform
{
	/// <summary>
	/// Tells the platform that a layout pass of the root just completed, which is an opportunity to render a
	/// pending frame early. Called by the layout loop of <c>CodeBrix.Platform.UI.Xaml.Core.CoreServices</c>, on the UI
	/// thread, once per root per pass.
	/// </summary>
	/// <param name="xamlRoot">The root whose layout just completed.</param>
	void OnRenderFrameOpportunity(XamlRoot xamlRoot);

	/// <summary>
	/// Creates the platform side of a <see cref="CompositionTarget"/>. Called once, from the target's constructor.
	/// </summary>
	/// <param name="target">The composition target.</param>
	/// <returns>The platform side of <paramref name="target"/>.</returns>
	ICompositionTargetPlatform CreateCompositionTargetPlatform(CompositionTarget target);
}
