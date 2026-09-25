#nullable enable

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Foundation;


namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="ICompositionShapePlatform"/> for a shape that draws nothing, and the base of
/// <see cref="CompositionSpriteShapeSkiaPlatform"/>: it positions the shape (its offset) before the per-type drawing.
/// </summary>
/// <remarks>This is the drawing code that lived in <c>CompositionShape.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionShapeSkiaPlatform : ICompositionShapePlatform
{
	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The shape this object draws.</param>
	internal CompositionShapeSkiaPlatform(CompositionShape owner)
	{
		Owner = owner;
	}

	/// <summary>
	/// Gets the shape this object draws.
	/// </summary>
	internal CompositionShape Owner { get; }

	/// <summary>
	/// Returns the Skia platform state of <paramref name="shape"/> (creating it on first use).
	/// </summary>
	/// <param name="shape">The shape.</param>
	/// <returns>The shape's platform state.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static CompositionShapeSkiaPlatform Of(CompositionShape shape)
	{
		var platform = shape.Platform;
		Debug.Assert(platform is CompositionShapeSkiaPlatform);
		return Unsafe.As<CompositionShapeSkiaPlatform>(platform);
	}

	internal virtual void Render(in PaintingSession session)
	{
		var offset = Owner.Offset;
		var transform = Owner.CombinedTransformMatrix;

		if (offset != Vector2.Zero || transform is not { IsIdentity: true })
		{
			session.Canvas.Save();

			if (offset != Vector2.Zero)
			{
				session.Canvas.Translate(offset.X, offset.Y);
			}

			// Intentionally not applying transform here.
			// Derived classes should be responsible to call GetTransform and use it appropriately.
			// For example, CompositionSpriteShape shouldn't "scale" the stroke thickness.
		}

		Paint(in session);

		if (offset != Vector2.Zero || transform is not { IsIdentity: true })
		{
			session.Canvas.Restore();
		}
	}

	internal virtual void Paint(in PaintingSession session)
	{
	}

	/// <inheritdoc />
	public virtual bool CanPaint() => false;

	/// <inheritdoc />
	public virtual bool HitTest(Point point) => false;

	/// <inheritdoc />
	public virtual void OnGeometryChanged()
	{
	}
}
