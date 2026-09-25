#nullable enable

using Microsoft.UI.Composition;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// The platform state of one <see cref="Visual"/>: it renders the visual, keeps whatever the platform caches
/// between frames (the recorded content of the visual and of its subtree), and answers the questions about the
/// visual's rendered content that the platform-neutral tree needs. One instance per visual, created by
/// <see cref="ICompositionPlatform.CreateVisualPlatform"/> in the visual's constructor and kept in a field.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The frame itself is driven by the platform's renderer, which walks the tree from the root visual; the
/// platform-neutral tree never initiates painting, so painting is not part of this contract. The Skia
/// implementations are <c>CodeBrix.Platform.UI.Composition.Skia.VisualSkiaPlatform</c> and its per-type
/// subclasses (<c>ContainerVisualSkiaPlatform</c>, <c>ShapeVisualSkiaPlatform</c>, <c>SpriteVisualSkiaPlatform</c>,
/// <c>RedirectVisualSkiaPlatform</c>, <c>BorderVisualSkiaPlatform</c>), created by <c>CompositionSkiaPlatform</c>.
/// </para>
/// </remarks>
internal interface IVisualPlatform
{
	/// <summary>
	/// Discards the platform's cached rendering of the visual's own content. Called when the visual's content
	/// is invalidated, right before the visual marks its paint as dirty.
	/// </summary>
	void DiscardPaintCache();

	/// <summary>
	/// Discards the platform's cached rendering of the visual's children. Called when something in the visual's
	/// subtree changes, right before the visual marks its children as dirty.
	/// </summary>
	void DiscardChildrenCache();

	/// <summary>
	/// Gets a value indicating whether the visual paints anything by itself (a container that only holds children
	/// does not).
	/// </summary>
	/// <returns><see langword="true"/> when the visual has content of its own.</returns>
	bool CanPaint();

	/// <summary>
	/// Gets a value indicating whether the visual's content must be painted again on every frame (for example
	/// because a brush samples what is already drawn behind it), so it must not be cached.
	/// </summary>
	bool RequiresRepaintOnEveryFrame { get; }

	/// <summary>
	/// Hit-tests the visual's rendered geometry, for visuals whose hit-test area is the geometry they render
	/// (a border visual's background and border). Clipping is not taken into account.
	/// </summary>
	/// <param name="point">The point, in the visual's coordinate space.</param>
	/// <returns><see langword="true"/> when the point is inside the rendered geometry.</returns>
	bool HitTest(Point point);
}
