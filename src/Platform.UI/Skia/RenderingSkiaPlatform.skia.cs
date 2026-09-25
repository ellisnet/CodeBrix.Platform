#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI.Contracts;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="IRenderingPlatform"/>: forwards to the render scheduling of the root's
/// <see cref="CompositionTarget"/>, and backs each composition target with a <see cref="CompositionTargetSkiaPlatform"/>.
/// </summary>
internal sealed class RenderingSkiaPlatform : IRenderingPlatform
{
	/// <inheritdoc />
	public void OnRenderFrameOpportunity(XamlRoot xamlRoot)
		=> (xamlRoot.Content?.Visual.CompositionTarget as CompositionTarget)?.OnRenderFrameOpportunity();

	/// <inheritdoc />
	public ICompositionTargetPlatform CreateCompositionTargetPlatform(CompositionTarget target) => new CompositionTargetSkiaPlatform(target);
}
