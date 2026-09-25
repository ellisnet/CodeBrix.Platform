#nullable enable

using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// The platform side of the composition tree: it creates the per-object platform state that renders each
/// <see cref="Visual"/>, paints each <see cref="CompositionBrush"/>, applies each <see cref="CompositionClip"/>,
/// draws each <see cref="CompositionShape"/> and backs each image surface, and it answers the compositor's
/// capability queries. The tree itself (properties, parenting, transforms, animations, invalidation) is
/// platform-neutral and stays in the composition types.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once through <see cref="CompositionPlatformServices.Composition"/> (lazily, on first use) from
/// <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Composition.Skia.CompositionSkiaPlatform</c>, registered by the assembly's
/// <c>SkiaPlatformBootstrap</c>. It resolves the platform type for an owner by the owner's runtime type
/// (once per type), so each create call is a cached lookup followed by one constructor call.
/// </para>
/// </remarks>
internal interface ICompositionPlatform
{
	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>. Called once, from the visual's constructor.
	/// </summary>
	/// <param name="owner">The visual that owns the returned object.</param>
	/// <returns>The platform state; the visual keeps it for its whole life.</returns>
	IVisualPlatform CreateVisualPlatform(Visual owner);

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>. Called once, the first time the brush needs it.
	/// </summary>
	/// <param name="owner">The brush that owns the returned object.</param>
	/// <returns>The platform state; the brush keeps it for its whole life.</returns>
	ICompositionBrushPlatform CreateBrushPlatform(CompositionBrush owner);

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>. Called once, the first time the clip needs it.
	/// </summary>
	/// <param name="owner">The clip that owns the returned object.</param>
	/// <returns>The platform state; the clip keeps it for its whole life.</returns>
	ICompositionClipPlatform CreateClipPlatform(CompositionClip owner);

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>. Called once, the first time the shape needs it.
	/// </summary>
	/// <param name="owner">The shape that owns the returned object.</param>
	/// <returns>The platform state; the shape keeps it for its whole life.</returns>
	ICompositionShapePlatform CreateShapePlatform(CompositionShape owner);

	/// <summary>
	/// Creates the platform image store of <paramref name="owner"/>. Called once, the first time the surface needs it.
	/// </summary>
	/// <param name="owner">The surface that owns the returned object.</param>
	/// <returns>The platform image store; the surface keeps it for its whole life.</returns>
	ICompositionSurfacePlatform CreateSurfacePlatform(PlatformCompositionSurface owner);

	/// <summary>
	/// Answers <see cref="CompositionCapabilities.AreEffectsSupported"/>.
	/// </summary>
	/// <param name="compositor">The compositor the capabilities object was created for, if any.</param>
	/// <returns><see langword="true"/> when the platform renders composition effects.</returns>
	bool AreEffectsSupported(Compositor? compositor);

	/// <summary>
	/// Answers <see cref="CompositionCapabilities.AreEffectsFast"/>.
	/// </summary>
	/// <param name="compositor">The compositor the capabilities object was created for, if any.</param>
	/// <returns><see langword="true"/> when the platform renders composition effects fast enough for animation.</returns>
	bool AreEffectsFast(Compositor? compositor);
}
