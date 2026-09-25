#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using CodeBrix.Platform.Media;
using CodeBrix.Platform.UI.Composition.Skia;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia implementation of <see cref="IGeometryPlatform"/>: builds an <see cref="SKPath"/> per geometry and hands
/// it out in a <see cref="SkiaGeometrySource2D"/>.
/// </summary>
internal sealed class GeometrySkiaPlatform : IGeometryPlatform
{
	/// <inheritdoc />
	public IGeometryPathBuilder CreatePathBuilder() => new PathBuilder();

	/// <inheritdoc />
	public IGeometrySource2D CreateGeometrySource(Geometry geometry) => new SkiaGeometrySource2D(new SKPath(GetSKPath(geometry)));

	/// <inheritdoc />
	public IGeometrySource2D? CreateFilledGeometrySource(Geometry geometry)
		=> GetFilledSKPath(geometry) is { } filledPath ? new SkiaGeometrySource2D(filledPath) : null;

	/// <inheritdoc />
	public IGeometrySource2D CreateEllipse(Rect renderingArea)
	{
		using var path = new SKPathBuilder();
		path.AddOval(new SKRect((float)renderingArea.X, (float)renderingArea.Y, (float)renderingArea.Right, (float)renderingArea.Bottom));
		var geometry = new SkiaGeometrySource2D(path.Snapshot());

		return geometry;
	}

	/// <inheritdoc />
	public IGeometrySource2D CreateRectangle(Rect finalRect, double radiusX, double radiusY)
	{
		var offset = new Vector2((float)finalRect.Left, (float)finalRect.Top);
		var size = new Vector2((float)finalRect.Width, (float)finalRect.Height);

		var geometry = radiusX is 0 || radiusY is 0
			? CompositionGeometrySkiaPlatform.BuildRectangleGeometry(offset, size)
			: CompositionGeometrySkiaPlatform.BuildRoundedRectangleGeometry(offset, size, new Vector2((float)radiusX, (float)radiusY));

		return new SkiaGeometrySource2D(geometry);
	}

	/// <inheritdoc />
	public Rect GetTightBounds(IGeometrySource2D geometry) => ((SkiaGeometrySource2D)geometry).TightBounds.ToRect();

	/// <summary>
	/// Returns the <see cref="SKPath"/> of <paramref name="geometry"/>. For a <see cref="StreamGeometry"/> this is the
	/// path the geometry holds (with its fill type updated), not a copy.
	/// </summary>
	/// <param name="geometry">The geometry.</param>
	/// <returns>The path.</returns>
	/// <exception cref="NotSupportedException">The geometry is of a kind that has no Skia path.</exception>
	internal static SKPath GetSKPath(Geometry geometry)
		=> geometry switch
		{
			StreamGeometry streamGeometry => GetSKPath(streamGeometry),
			PathGeometry pathGeometry => GetSKPath(pathGeometry, false),
			GeometryGroup geometryGroup => GetSKPath(geometryGroup),
			LineGeometry lineGeometry => CompositionGeometrySkiaPlatform.BuildLineGeometry(lineGeometry.StartPoint.ToVector2(), lineGeometry.EndPoint.ToVector2()),
			RectangleGeometry rectangleGeometry => CompositionGeometrySkiaPlatform.BuildRectangleGeometry(offset: new Vector2((float)rectangleGeometry.Rect.X, (float)rectangleGeometry.Rect.Y), size: new Vector2((float)rectangleGeometry.Rect.Width, (float)rectangleGeometry.Rect.Height)),
			EllipseGeometry ellipseGeometry => CompositionGeometrySkiaPlatform.BuildEllipseGeometry(ellipseGeometry.Center.ToVector2(), new Vector2((float)ellipseGeometry.RadiusX, (float)ellipseGeometry.RadiusY)),
			// TODO: Can we mark Geometry as abstract?
			// While this will diverge from UWP, it doesn't seem to matter whether it's abstract or not because
			// this class doesn't have public constructors in UWP, which makes it not-inheritable either way.
			_ => throw new NotSupportedException($"Geometry {geometry} is not supported"),
		};

	/// <remarks>
	/// Note: Try not to depend on this. See the note in <see cref="CompositionSpriteShape.FillGeometry"/>
	/// </remarks>
	private static SKPath? GetFilledSKPath(Geometry geometry)
		=> geometry is PathGeometry pathGeometry ? GetSKPath(pathGeometry, true) : null;

	private static SKPath GetSKPath(StreamGeometry streamGeometry)
	{
		var bezierPath = (SKPath)streamGeometry.PlatformPath;
		bezierPath.FillType = streamGeometry.FillRule.ToSkiaFillType();
		return bezierPath;
	}

	private static SKPath GetSKPath(GeometryGroup geometryGroup)
	{
		using var path = new SKPathBuilder();

		foreach (var geometry in geometryGroup.Children)
		{
			var geometryPath = GetSKPath(geometry);
			path.AddPath(geometryPath);
		}

		path.FillType = geometryGroup.FillRule.ToSkiaFillType();
		return path.Snapshot();
	}

	private static SKPath GetSKPath(PathGeometry pathGeometry, bool skipUnfilled)
	{
		using var path = new SKPathBuilder();

		foreach (PathFigure figure in pathGeometry.Figures)
		{
			if (skipUnfilled && !figure.IsFilled)
			{
				continue;
			}

			path.MoveTo((float)figure.StartPoint.X, (float)figure.StartPoint.Y);

			foreach (PathSegment segment in figure.Segments)
			{
				if (segment is LineSegment lineSegment)
				{
					path.LineTo((float)lineSegment.Point.X, (float)lineSegment.Point.Y);
				}
				else if (segment is BezierSegment bezierSegment)
				{
					path.CubicTo(
						 (float)bezierSegment.Point1.X, (float)bezierSegment.Point1.Y,
						 (float)bezierSegment.Point2.X, (float)bezierSegment.Point2.Y,
						 (float)bezierSegment.Point3.X, (float)bezierSegment.Point3.Y);
				}
				else if (segment is QuadraticBezierSegment quadraticBezierSegment)
				{
					path.QuadTo(
						 (float)quadraticBezierSegment.Point1.X, (float)quadraticBezierSegment.Point1.Y,
						 (float)quadraticBezierSegment.Point2.X, (float)quadraticBezierSegment.Point2.Y);
				}
				else if (segment is ArcSegment arcSegment)
				{
					path.ArcTo(
						 (float)arcSegment.Size.Width, (float)arcSegment.Size.Height,
						 (float)arcSegment.RotationAngle,
						 arcSegment.IsLargeArc ? SkiaSharp.SKPathArcSize.Large : SkiaSharp.SKPathArcSize.Small,
						 (arcSegment.SweepDirection == SweepDirection.Clockwise ? SkiaSharp.SKPathDirection.Clockwise : SkiaSharp.SKPathDirection.CounterClockwise),
						 (float)arcSegment.Point.X, (float)arcSegment.Point.Y);
				}
			}

			if (figure.IsClosed)
			{
				path.Close();
			}
		}

		path.FillType = pathGeometry.FillRule.ToSkiaFillType();

		return path.Snapshot();
	}

	/// <summary>
	/// Records the commands of a stream geometry context into an <see cref="SKPathBuilder"/>.
	/// </summary>
	private sealed class PathBuilder : IGeometryPathBuilder
	{
		private readonly SKPathBuilder bezierPath = new SKPathBuilder();

		public void MoveTo(Point startPoint) => bezierPath.MoveTo(new SkiaSharp.SKPoint((float)startPoint.X, (float)startPoint.Y));

		public void LineTo(Point point) => bezierPath.LineTo((float)point.X, (float)point.Y);

		public void CubicTo(Point point1, Point point2, Point point3)
			=> bezierPath.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);

		public void QuadTo(Point point1, Point point2)
			=> bezierPath.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);

		public void ArcTo(Rect circle, double startAngle, double sweepAngle)
			=> bezierPath.ArcTo(
				new SkiaSharp.SKRect((float)circle.Left, (float)circle.Top, (float)circle.Right, (float)circle.Bottom),
				(float)startAngle,
				(float)sweepAngle,
				false
			);

		public void Close() => bezierPath.Close();

		public object Snapshot() => bezierPath.Snapshot();
	}
}
