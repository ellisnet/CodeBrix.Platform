#nullable enable

using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Extensions;
using Windows.Foundation;


namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionShapePlatform"/> for a
/// <see cref="CompositionSpriteShape"/>: it keeps the shape's geometry (and fill geometry) transformed by the
/// shape's transform, fills and strokes it with the shape's brushes, and hit-tests it.
/// </summary>
/// <remarks>This is the drawing code that lived in <c>CompositionSpriteShape.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionSpriteShapeSkiaPlatform : CompositionShapeSkiaPlatform
{
	private static readonly SKPaint _spareHitTestPaint = new();
	private static readonly SKPathBuilder _spareHitTestPath = new();
	// We don't call SKPaint.Reset() after usage, so make sure
	// that only SKPaint.Color is being set
	private static readonly SKPaint _spareColorPaint = new();

	private SkiaGeometrySource2D? _geometryWithTransformations;
	private SkiaGeometrySource2D? _fillGeometryWithTransformations;

	private static readonly SKPaint _sparePaint = new SKPaint();
	private static readonly SKPathBuilder _sparePath = new SKPathBuilder();

	private readonly CompositionSpriteShape _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The shape this object draws.</param>
	internal CompositionSpriteShapeSkiaPlatform(CompositionSpriteShape owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	public override bool CanPaint() => (_owner.FillBrush?.CanPaint() ?? false) || (_owner.StrokeBrush?.CanPaint() ?? false);

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		var owner = _owner;
		if (_geometryWithTransformations is { } geometryWithTransformations)
		{
			if (owner.FillBrush is { } fill && _fillGeometryWithTransformations is { } finalFillGeometryWithTransformations)
			{
				var fillPaint = _sparePaint;
				PrepareTempPaint(fillPaint, isStroke: false);

				if (owner.Geometry is not null && (owner.Geometry.TrimStart != default || owner.Geometry.TrimEnd != default))
				{
					fillPaint.PathEffect = SKPathEffect.CreateTrim(owner.Geometry.TrimStart, owner.Geometry.TrimEnd);
				}

				finalFillGeometryWithTransformations.GetFillPath(fillPaint, _sparePath);
				using var fillPath = _sparePath.Detach();

				session.Canvas.Save();
				session.Canvas.ClipPath(fillPath, antialias: true);
				if (owner.Compositor.TryGetEffectiveBackgroundColor(owner, out var colorFromTransition))
				{
					_spareColorPaint.Color = colorFromTransition.ToSKColor(session.Opacity);
					session.Canvas.DrawRect(fillPath.Bounds, _spareColorPaint);
				}
				else
				{
					CompositionBrushSkiaPlatform.Of(fill).Paint(session.Canvas, session.Opacity, finalFillGeometryWithTransformations.Bounds);
				}
				session.Canvas.Restore();
			}

			if (owner.StrokeBrush is { } stroke && owner.StrokeThickness > 0)
			{
				var strokePaint = _sparePaint;
				PrepareTempPaint(strokePaint, isStroke: true);

				// Set stroke thickness
				strokePaint.StrokeWidth = owner.StrokeThickness;
				if (owner.StrokeDashArray is { Count: > 0 } strokeDashArray)
				{
					strokePaint.PathEffect = SKPathEffect.CreateDash(strokeDashArray.ToEvenArray(), 0);
				}

				if (owner.Geometry is not null && (owner.Geometry.TrimStart != default || owner.Geometry.TrimEnd != default))
				{
					var pathEffect = SKPathEffect.CreateTrim(owner.Geometry.TrimStart, owner.Geometry.TrimEnd);
					if (strokePaint.PathEffect is SKPathEffect effect)
					{
						pathEffect = SKPathEffect.CreateSum(effect, pathEffect);
					}

					strokePaint.PathEffect = pathEffect;
				}

				// Generate stroke geometry for bounds that will be passed to a brush.
				// - [Future]: This generated geometry should also be used for hit testing.

				// If we have something like this:
				// <Path Data="M 0 0 L 50 0 L 50 50 L 0 50 z"
				//		 Stroke="Red"
				//		 StrokeThickness="5"
				//		 Width="70"
				//		 Stretch="Fill"
				//		 HorizontalAlignment="Center"
				//		 VerticalAlignment="Center" />
				// The geometry itself is a 50x50 rectangle, and then we set the shape Width to 70 and let it
				// to stretch over the available height, and we have a stroke thickness as 1px
				// On Windows, the stroke is simply 1px, it doesn't scale with the height.
				// So, to get a correct stroke geometry, we must apply the transformations first.

				// Get the stroke geometry, after scaling has been applied.
				geometryWithTransformations.GetFillPath(strokePaint, _sparePath);
				using var strokeFillPath = _sparePath.Detach();

				session.Canvas.ClipPath(strokeFillPath, antialias: true);
				CompositionBrushSkiaPlatform.Of(stroke).Paint(session.Canvas, session.Opacity, strokeFillPath.Bounds);
				session.Canvas.Restore();
			}
		}
	}

	private static void PrepareTempPaint(SKPaint paint, bool isStroke)
	{
		paint.Reset();
		paint.IsAntialias = true;
		paint.IsStroke = isStroke;
		paint.Color = SKColors.White;   // Transparent color wouldn't draw anything
	}

	/// <inheritdoc />
	public override void OnGeometryChanged()
	{
		if (_owner.Geometry?.BuildGeometry() is SkiaGeometrySource2D geometry)
		{
			var transform = _owner.CombinedTransformMatrix;
			_geometryWithTransformations = transform.IsIdentity
				? geometry
				: geometry.Transform(transform.ToSKMatrix());
			if (_owner.FillGeometry?.BuildGeometry() is SkiaGeometrySource2D fillGeometry)
			{
				_fillGeometryWithTransformations = transform.IsIdentity
					? fillGeometry
					: fillGeometry.Transform(transform.ToSKMatrix());
			}
			else
			{
				_fillGeometryWithTransformations = _geometryWithTransformations;
			}
		}
		else
		{
			_geometryWithTransformations = null;
			_fillGeometryWithTransformations = null;
		}
	}

	/// <inheritdoc />
	public override bool HitTest(Point point)
	{
		if (_geometryWithTransformations is { } geometryWithTransformations)
		{
			point = _owner.CombinedTransformMatrix.Inverse().Transform(point);

			if (_owner.FillBrush is { } && geometryWithTransformations.Contains((float)point.X, (float)point.Y))
			{
				return true;
			}

			if (_owner.StrokeBrush is { } && _owner.StrokeThickness > 0)
			{
				var strokePaint = _spareHitTestPaint;
				PrepareTempPaint(strokePaint, isStroke: true);

				strokePaint.StrokeWidth = _owner.StrokeThickness;

				geometryWithTransformations.GetFillPath(strokePaint, _spareHitTestPath);
				using var hitTestStrokeFillPath = _spareHitTestPath.Detach();
				if (hitTestStrokeFillPath.Contains((float)point.X, (float)point.Y))
				{
					return true;
				}
			}
		}
		return false;
	}
}
