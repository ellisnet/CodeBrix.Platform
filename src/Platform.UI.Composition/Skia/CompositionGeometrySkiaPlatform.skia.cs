#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Interop.Direct2D;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="ICompositionGeometryPlatform"/>: every composition geometry is an
/// <see cref="SKPath"/> in a <see cref="SkiaGeometrySource2D"/>. The static builders are also used directly by the
/// Skia shape and geometry code of Platform.UI (through the <c>CompositionGeometry</c> shims).
/// </summary>
/// <remarks>This is the geometry code that lived in <c>CompositionGeometry.skia.cs</c> and
/// <c>CompositionPathGeometry.skia.cs</c>, moved verbatim.</remarks>
internal sealed class CompositionGeometrySkiaPlatform : ICompositionGeometryPlatform
{
	/// <inheritdoc />
	public IGeometrySource2D CreateLine(Vector2 start, Vector2 end)
		=> new SkiaGeometrySource2D(BuildLineGeometry(start, end));

	/// <inheritdoc />
	public IGeometrySource2D CreateRectangle(Vector2 offset, Vector2 size)
		=> new SkiaGeometrySource2D(BuildRectangleGeometry(offset, size));

	/// <inheritdoc />
	public IGeometrySource2D CreateRoundedRectangle(Vector2 offset, Vector2 size, Vector2 cornerRadius)
		=> new SkiaGeometrySource2D(BuildRoundedRectangleGeometry(offset, size, cornerRadius));

	/// <inheritdoc />
	public IGeometrySource2D CreateEllipse(Vector2 center, Vector2 radius)
		=> new SkiaGeometrySource2D(BuildEllipseGeometry(center, radius));

	/// <inheritdoc />
	public IGeometrySource2D CreatePath(List<CompositionPathCommand> commands)
		=> InternalBuildPathGeometry(commands);

	/// <inheritdoc />
	public bool IsPlatformGeometry(IGeometrySource2D source)
		=> source is SkiaGeometrySource2D;

	/// <inheritdoc />
	public Rect GetClipBounds(IGeometrySource2D? geometry)
	{
		if (geometry is SkiaGeometrySource2D skiaGeometrySource)
		{
			return skiaGeometrySource.Geometry.TightBounds.ToRect();
		}
		else
		{
			throw new InvalidOperationException($"Clipping with source {geometry} is not supported");
		}
	}

	/// <summary>
	/// Kappa = (sqrt(2) - 1) * 4/3;
	//  Used to calculate bezier control points for each of the circle four arcs. 
	//  - Approximating a 1/4 circle with a bezier curve.
	/// </summary>
	private const double CIRCLE_BEZIER_KAPPA = 0.552284749830793398402251632279597438092895833835930764235;

	internal static SKPath BuildLineGeometry(Vector2 start, Vector2 end)
	{
		using var path = new SKPathBuilder();

		path.MoveTo(start.ToSKPoint());
		path.LineTo(end.ToSKPoint());

		return path.Snapshot();
	}

	internal static SKPath BuildRectangleGeometry(Vector2 offset, Vector2 size)
	{
		using var path = new SKPathBuilder();

		// Top left
		path.MoveTo(new SKPoint(offset.X, offset.Y));
		// Top right
		path.RLineTo(new SKPoint(size.X, 0));
		// Bottom right
		path.RLineTo(new SKPoint(0, size.Y));
		// Bottom left
		path.RLineTo(new SKPoint(-size.X, 0));
		// Top left
		path.Close();

		return path.Snapshot();
	}

	internal static SKPath BuildRoundedRectangleGeometry(Vector2 offset, Vector2 size, Vector2 cornerRadius)
	{
		float radiusX = Clamp(cornerRadius.X, 0, size.X * 0.5f);
		float radiusY = Clamp(cornerRadius.Y, 0, size.Y * 0.5f);

		float bezierX = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusX);
		float bezierY = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusY);

		using var path = new SKPathBuilder();
		var lastPoint = new SKPoint(offset.X + radiusX, offset.Y);

		path.MoveTo(lastPoint);
		// Top line
		path.LineTo(lastPoint + new SKPoint(size.X - 2 * radiusX, 0));
		lastPoint += new SKPoint(size.X - 2 * radiusX, 0);
		// Top-right Arc
		path.CubicTo(
			lastPoint + new SKPoint(radiusX - bezierX, 0),   // 1st control point
			lastPoint + new SKPoint(radiusX, bezierY),       // 2nd control point
			lastPoint + new SKPoint(radiusX, radiusY));      // End point
		lastPoint += new SKPoint(radiusX, radiusY);

		// Right line
		path.LineTo(lastPoint + new SKPoint(0, size.Y - 2 * radiusY));
		lastPoint += new SKPoint(0, size.Y - 2 * radiusY);
		// Bottom-right Arc
		path.CubicTo(
			lastPoint + new SKPoint(0, bezierY),             // 1st control point
			lastPoint + new SKPoint(-bezierX, radiusY),      // 2nd control point
			lastPoint + new SKPoint(-radiusX, radiusY));     // End point
		lastPoint += new SKPoint(-radiusX, radiusY);

		// Bottom line
		path.LineTo(lastPoint + new SKPoint(-(size.X - 2 * radiusX), 0));
		lastPoint = lastPoint + new SKPoint(-(size.X - 2 * radiusX), 0);
		// Bottom-left Arc
		path.CubicTo(
			lastPoint + new SKPoint(-radiusX + bezierX, 0),  // 1st control point
			lastPoint + new SKPoint(-radiusX, -bezierY),     // 2nd control point
			lastPoint + new SKPoint(-radiusX, -radiusY));    // End point
		lastPoint += new SKPoint(-radiusX, -radiusY);

		// Left line
		path.LineTo(lastPoint + new SKPoint(0, -(size.Y - 2 * radiusY)));
		lastPoint += new SKPoint(0, -(size.Y - 2 * radiusY));
		// Top-left Arc
		path.CubicTo(
			lastPoint + new SKPoint(0, -radiusY + bezierY),  // 1st control point
			lastPoint + new SKPoint(bezierX, -radiusY),      // 2nd control point
			lastPoint + new SKPoint(radiusX, -radiusY));     // End point

		path.Close();

		return path.Snapshot();
	}

	internal static SKPath BuildEllipseGeometry(Vector2 center, Vector2 radius)
	{
		SKRect rect = SKRect.Create(center.X - radius.X, center.Y - radius.Y, radius.X * 2, radius.Y * 2);

		float bezierX = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radius.X);
		float bezierY = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radius.Y);

		// IMPORTANT:
		// - The order of following operations is important for dashed strokes.
		// - Stroke might get merged in the end.
		// - WPF starts with bottom right ellipse arc.
		// - TODO: Verify UWP behavior

		using var path = new SKPathBuilder();

		path.MoveTo(new SKPoint(rect.Right, rect.Top + radius.Y));
		// Bottom-right Arc
		path.CubicTo(
			new SKPoint(rect.Right, rect.Bottom - bezierY),  // 1st control point
			new SKPoint(rect.Right - bezierX, rect.Bottom),  // 2nd control point
			new SKPoint(rect.Right - radius.X, rect.Bottom)); // End point

		// Bottom-left Arc
		path.CubicTo(
			new SKPoint(rect.Left + bezierX, rect.Bottom),      // 1st control point
			new SKPoint(rect.Left, rect.Bottom - bezierY),      // 2nd control point
			new SKPoint(rect.Left, rect.Bottom - radius.Y));     // End point

		// Top-left Arc
		path.CubicTo(
			new SKPoint(rect.Left, rect.Top + bezierY),           // 1st control point
			new SKPoint(rect.Left + bezierX, rect.Top),           // 2nd control point
			new SKPoint(rect.Left + radius.X, rect.Top));          // End point

		// Top-right Arc
		path.CubicTo(
			new SKPoint(rect.Right - bezierX, rect.Top),       // 1st control point
			new SKPoint(rect.Right, rect.Top + bezierY),       // 2nd control point
			new SKPoint(rect.Right, rect.Top + radius.Y));      // End point

		path.Close();

		return path.Snapshot();
	}

	private static float Clamp(float value, float minValue, float maxValue)
	{
		return Math.Min(Math.Max(Math.Abs(value), minValue), maxValue);
	}

	private static SkiaGeometrySource2D InternalBuildPathGeometry(List<CompositionPathCommand> commands)
	{
		using var path = new SKPathBuilder();
		foreach (var command in commands)
		{
			switch (command.Type)
			{
				case CompositionPathCommandType.SetFillMode:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						path.FillType = ((D2D1FillMode)parameters[0]).ToSkia();
						break;
					}
				case CompositionPathCommandType.SetSegmentFlags:
					break; // TODO
				case CompositionPathCommandType.BeginFigure:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 2);
						var point = (Point)parameters[0];
						path.MoveTo(point.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddLine:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var point = (Point)parameters[0];
						path.LineTo(point.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddLines:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var points = (Point[])parameters[0];

						foreach (var point in points)
						{
							path.LineTo(point.ToSkia());
						}

						break;
					}
				case CompositionPathCommandType.AddBezier:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var bezier = (D2D1BezierSegment)parameters[0];
						path.CubicTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia(), bezier.Point3.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddBeziers:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var beziers = (D2D1BezierSegment[])parameters[0];

						foreach (var bezier in beziers)
						{
							path.CubicTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia(), bezier.Point3.ToSkia());
						}

						break;
					}
				case CompositionPathCommandType.AddQuadraticBezier:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var bezier = (D2D1QuadraticBezierSegment)parameters[0];
						path.QuadTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia());
						break;
					}
				case CompositionPathCommandType.AddQuadraticBeziers:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var beziers = (D2D1QuadraticBezierSegment[])parameters[0];

						foreach (var bezier in beziers)
						{
							path.QuadTo(bezier.Point1.ToSkia(), bezier.Point2.ToSkia());
						}

						break;
					}
				case CompositionPathCommandType.AddArc:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var arc = (D2D1ArcSegment)parameters[0];
						path.ArcTo(new((float)arc.Size.Width, (float)arc.Size.Height), arc.RotationAngle, arc.ArcSize.ToSkia(), arc.SweepDirection.ToSkia(), arc.Point.ToSkia());
						break;
					}
				case CompositionPathCommandType.EndFigure:
					{
						var parameters = ValidateCommandParameters(command, expectedParameterCount: 1);
						var end = (D2D1FigureEnd)parameters[0];

						if (end is D2D1FigureEnd.Closed)
						{
							path.Close();
						}

						break;
					}
				case CompositionPathCommandType.Close:
					break; // We don't actually have a sink to close, so we can ignore this
			}
		}

		return new SkiaGeometrySource2D(path.Snapshot());
	}

	private static object[] ValidateCommandParameters(CompositionPathCommand command, int expectedParameterCount)
	{
		if (command.Parameters is null || command.Parameters.Length != expectedParameterCount)
		{
			throw new InvalidOperationException("Unexpected path command parameters value");
		}

		return command.Parameters;
	}
}
