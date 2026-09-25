#nullable enable

using Microsoft.UI.Composition;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// The platform state of one <see cref="CompositionShape"/>: it draws the shape inside its
/// <see cref="ShapeVisual"/> (or border visual) and keeps the platform geometry it draws, which is also what the
/// shape is hit-tested against. One instance per shape, created by
/// <see cref="ICompositionPlatform.CreateShapePlatform"/> the first time the shape needs it and kept in a field.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// The Skia implementations are <c>CodeBrix.Platform.UI.Composition.Skia.CompositionShapeSkiaPlatform</c> and
/// <c>CompositionSpriteShapeSkiaPlatform</c>, created by <c>CompositionSkiaPlatform</c>.
/// </para>
/// </remarks>
internal interface ICompositionShapePlatform
{
	/// <summary>
	/// Gets a value indicating whether the shape paints anything (a sprite shape with neither a fill nor a stroke
	/// brush that paints does not).
	/// </summary>
	/// <returns><see langword="true"/> when the shape has something to paint.</returns>
	bool CanPaint();

	/// <summary>
	/// Hit-tests the shape's filled and stroked geometry.
	/// </summary>
	/// <param name="point">The point, in the coordinate space of the shape's visual.</param>
	/// <returns><see langword="true"/> when the point is inside what the shape paints.</returns>
	bool HitTest(Point point);

	/// <summary>
	/// Tells the platform state that the shape's geometry, fill geometry or combined transform has changed, so the
	/// platform geometry it draws must be rebuilt.
	/// </summary>
	void OnGeometryChanged();
}
