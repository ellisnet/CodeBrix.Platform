#nullable enable

using Microsoft.UI.Composition;
using CodeBrix.Platform.UI.Composition.Contracts;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="ICompositionPlatform"/>: it creates the Skia platform state of each
/// composition object through the per-type factories that <see cref="SkiaPlatformBootstrap"/> registers.
/// </summary>
internal sealed class CompositionSkiaPlatform : ICompositionPlatform
{
	/// <summary>
	/// Gets the factories of the platform state of visuals, keyed by visual type.
	/// </summary>
	internal SkiaPlatformFactoryRegistry<Visual, VisualSkiaPlatform> Visuals { get; } = new();

	/// <summary>
	/// Gets the factories of the platform state of brushes, keyed by brush type.
	/// </summary>
	internal SkiaPlatformFactoryRegistry<CompositionBrush, CompositionBrushSkiaPlatform> Brushes { get; } = new();

	/// <summary>
	/// Gets the factories of the platform state of clips, keyed by clip type.
	/// </summary>
	internal SkiaPlatformFactoryRegistry<CompositionClip, CompositionClipSkiaPlatform> Clips { get; } = new();

	/// <summary>
	/// Gets the factories of the platform state of shapes, keyed by shape type.
	/// </summary>
	internal SkiaPlatformFactoryRegistry<CompositionShape, CompositionShapeSkiaPlatform> Shapes { get; } = new();

	/// <inheritdoc />
	public IVisualPlatform CreateVisualPlatform(Visual owner) => Visuals.Create(owner);

	/// <inheritdoc />
	public ICompositionBrushPlatform CreateBrushPlatform(CompositionBrush owner) => Brushes.Create(owner);

	/// <inheritdoc />
	public ICompositionClipPlatform CreateClipPlatform(CompositionClip owner) => Clips.Create(owner);

	/// <inheritdoc />
	public ICompositionShapePlatform CreateShapePlatform(CompositionShape owner) => Shapes.Create(owner);

	/// <inheritdoc />
	public ICompositionSurfacePlatform CreateSurfacePlatform(PlatformCompositionSurface owner) => new CompositionSurfaceSkiaPlatform(owner);

	/// <inheritdoc />
	public bool AreEffectsSupported(Compositor? compositor) => true;

	/// <inheritdoc />
	public bool AreEffectsFast(Compositor? compositor) => compositor?.IsSoftwareRenderer is not true;
}
