#nullable enable

using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// The platform state of one <see cref="CompositionClip"/>: it applies the clip while a visual is rendered and
/// keeps whatever the platform caches for it (clip paths, rounded rectangles). One instance per clip, created by
/// <see cref="ICompositionPlatform.CreateClipPlatform"/> the first time the platform's renderer needs it and kept
/// in a field of the clip.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The platform-neutral clip computes its bounds itself (<c>CompositionClip.GetBounds</c>), so it never calls into
/// this object; only the platform's renderer does. The Skia implementations are
/// <c>CodeBrix.Platform.UI.Composition.Skia.CompositionClipSkiaPlatform</c> and its per-type subclasses (inset,
/// rectangle, geometric), created by <c>CompositionSkiaPlatform</c>.
/// </para>
/// </remarks>
internal interface ICompositionClipPlatform
{
}
