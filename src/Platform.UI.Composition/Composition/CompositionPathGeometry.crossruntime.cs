#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using CodeBrix.Platform.UI.Composition;
using CodeBrix.Platform.UI.Composition.Contracts;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Interop;
using Windows.Graphics.Interop.Direct2D;

namespace Microsoft.UI.Composition;

public partial class CompositionPathGeometry : CompositionGeometry, ID2D1GeometrySink
{
	private IGeometrySource2D? _geometrySource2D;
	private List<CompositionPathCommand> _commands = new();

	internal override IGeometrySource2D? BuildGeometry() => _geometrySource2D;

	private void InternalBuildGeometry()
	{
		IGeometrySource2D? geometrySource = null;
		var geometryPlatform = CompositionPlatformServices.Geometry;

		if (Path?.GeometrySource is IGeometrySource2DInterop geometrySourceInterop)
		{
			switch (geometrySourceInterop.GetGeometry())
			{
				case ID2D1RectangleGeometry rectangleGeometry:
					{
						var rect = rectangleGeometry.GetRect();
						geometrySource = geometryPlatform.CreateRectangle(rect.Location.ToVector2(), rect.Size.ToVector2());
						break;
					}
				case ID2D1RoundedRectangleGeometry roundedRectangleGeometry:
					{
						var rect = roundedRectangleGeometry.GetRoundedRect();
						geometrySource = geometryPlatform.CreateRoundedRectangle(rect.Rect.Location.ToVector2(), rect.Rect.Size.ToVector2(), new(rect.RadiusX, rect.RadiusY));
						break;
					}
				case ID2D1EllipseGeometry ellipseGeometry:
					{
						var ellipse = ellipseGeometry.GetEllipse();
						geometrySource = geometryPlatform.CreateEllipse(ellipse.Point.ToVector2(), new(ellipse.RadiusX, ellipse.RadiusY));
						break;
					}
				case ID2D1PathGeometry pathGeometry:
					{
						if (pathGeometry is ICompositionPathCommandsProvider { Commands: List<CompositionPathCommand> commands })
						{
							_commands.AddRange(commands);
						}
						else
						{
							pathGeometry.Stream(this);
						}

						geometrySource = geometryPlatform.CreatePath(_commands);
						_commands.Clear();

						break;
					}
				default:
					throw new InvalidOperationException($"Path geometry source type {geometrySourceInterop.GetType().Name} is no supported");
			}

			(_geometrySource2D as IDisposable)?.Dispose();
		}
		else if (Path?.GeometrySource is { } platformGeometry && geometryPlatform.IsPlatformGeometry(platformGeometry))
		{
			geometrySource = platformGeometry;
		}
		else
		{
			throw new InvalidOperationException($"Path geometry source type doesn't implement IGeometrySource2DInterop");
		}

		_geometrySource2D = geometrySource;
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		if (propertyName is nameof(Path))
		{
			_commands.Clear();

			if (Path is not null)
			{
				InternalBuildGeometry();
			}
			else
			{
				(_geometrySource2D as IDisposable)?.Dispose();
				_geometrySource2D = null;
			}
		}

		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
	}

	private protected override void DisposeInternal()
	{
		(_geometrySource2D as IDisposable)?.Dispose();
		base.DisposeInternal();

		_geometrySource2D = null;
	}

	void ID2D1GeometrySink.AddLine(Point point) => _commands.Add(CompositionPathCommand.Create(point));

	void ID2D1GeometrySink.AddBezier(D2D1BezierSegment bezier) => _commands.Add(CompositionPathCommand.Create(bezier));

	void ID2D1GeometrySink.AddQuadraticBezier(D2D1QuadraticBezierSegment bezier) => _commands.Add(CompositionPathCommand.Create(bezier));

	void ID2D1GeometrySink.AddQuadraticBeziers(D2D1QuadraticBezierSegment[] beziers) => _commands.Add(CompositionPathCommand.Create(beziers));

	void ID2D1GeometrySink.AddArc(D2D1ArcSegment arc) => _commands.Add(CompositionPathCommand.Create(arc));

	void ID2D1SimplifiedGeometrySink.SetFillMode(D2D1FillMode fillMode) => _commands.Add(CompositionPathCommand.Create(fillMode));

	void ID2D1SimplifiedGeometrySink.SetSegmentFlags(D2D1PathSegment vertexFlags) => _commands.Add(CompositionPathCommand.Create(vertexFlags));

	void ID2D1SimplifiedGeometrySink.BeginFigure(Point startPoint, D2D1FigureBegin figureBegin) => _commands.Add(CompositionPathCommand.Create(startPoint, figureBegin));

	void ID2D1SimplifiedGeometrySink.AddLines(Point[] points) => _commands.Add(CompositionPathCommand.Create(points));

	void ID2D1SimplifiedGeometrySink.AddBeziers(D2D1BezierSegment[] beziers) => _commands.Add(CompositionPathCommand.Create(beziers));

	void ID2D1SimplifiedGeometrySink.EndFigure(D2D1FigureEnd figureEnd) => _commands.Add(CompositionPathCommand.Create(figureEnd));

	void ID2D1SimplifiedGeometrySink.Close() => _commands.Add(CompositionPathCommand.Create());
}
#endif
