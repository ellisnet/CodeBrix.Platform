using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using System.Numerics;
using System.Linq;
using static System.Math;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.Contracts;

#if false
using UIKit;
using Path = UIKit.UIBezierPath;
using ObjCRuntime;
#elif false
using Android.Graphics.Drawables.Shapes;
using Path = Android.Graphics.Path;
using CodeBrix.Platform.UI;
#else
using Path = System.Object;
#endif

namespace CodeBrix.Platform.Media //Was previously: Uno.Media
{
	class PathStreamGeometryContext : StreamGeometryContext
	{
		private readonly List<Point> _points = new List<Point>();
		private readonly StreamGeometry _owner;
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
		private readonly IGeometryPathBuilder bezierPath = PlatformServices.Geometry.CreatePathBuilder();
#else
		private Path bezierPath = new Path();
#endif

		internal PathStreamGeometryContext(StreamGeometry owner)
		{
			_owner = owner;
		}

		public override void BeginFigure(Point startPoint, bool isFilled)
		{
#if false
			bezierPath.MoveTo(startPoint);
#elif false
			bezierPath.MoveTo((float)startPoint.X, (float)startPoint.Y);
#elif __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			bezierPath.MoveTo(startPoint);
#endif

			_points.Add(startPoint);
		}

		public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
		{
#if false
			bezierPath.AddLineTo(point);
#elif false
			bezierPath.LineTo((float)point.X, (float)point.Y);
#elif __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			bezierPath.LineTo(point);
#endif

			_points.Add(point);
		}

		public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
		{
#if false
			bezierPath.AddCurveToPoint(point3, point1, point2);
#elif false
			bezierPath.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
#elif __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			bezierPath.CubicTo(point1, point2, point3);
#endif
			_points.Add(point3);
		}

		public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
		{
#if false
			bezierPath.AddQuadCurveToPoint(point2, point1);
#elif false
			bezierPath.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
#elif __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			bezierPath.QuadTo(point1, point2);
#endif

			_points.Add(point2);
		}

		public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
		{
			if (size.Width != size.Height)
			{
				throw new NotImplementedException("The arc must be based on a circle, not an ellipse.");
			}

			var startPoint = _points.Last();
			var endPoint = point;
			var radius = size.Width;
			var sign = isLargeArc != (sweepDirection == SweepDirection.Clockwise);
			var center = CenterFromPointsAndRadius(startPoint, endPoint, radius, sign);
			var startAngle = Atan2(startPoint.Y - center.Y, startPoint.X - center.X);
			var endAngle = Atan2(endPoint.Y - center.Y, endPoint.X - center.X);
			var circle = new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2);

#if false
			bezierPath.AddArc(
				center,
				(nfloat)radius,
				(nfloat)startAngle,
				(nfloat)endAngle,
				sweepDirection == SweepDirection.Clockwise
			);
#elif false
			var sweepAngle = endAngle - startAngle;

			// Convert to degrees
			startAngle = startAngle * (180 / PI);
			sweepAngle = sweepAngle * (180 / PI);

			// Invert y-axis
			startAngle = (startAngle + 360) % 360;
			sweepAngle = (sweepAngle + 360) % 360;

			// Apply direction
			if (sweepDirection == SweepDirection.Counterclockwise)
			{
				sweepAngle -= 360;
			}

			bezierPath.ArcTo(
				circle.ToRectF(),
				(float)startAngle,
				(float)sweepAngle
			);
#elif __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			var sweepAngle = endAngle - startAngle;

			// Convert to degrees
			startAngle = startAngle * (180 / PI);
			sweepAngle = sweepAngle * (180 / PI);

			// Invert y-axis
			startAngle = (startAngle + 360) % 360;
			sweepAngle = (sweepAngle + 360) % 360;

			// Apply direction
			if (sweepDirection == SweepDirection.Counterclockwise)
			{
				sweepAngle -= 360;
			}

			bezierPath.ArcTo(circle, startAngle, sweepAngle);
#endif

			_points.Add(point);
		}

		private static Point CenterFromPointsAndRadius(Point point1, Point point2, double radius, bool sign)
		{
			// Find the center of a circle from 2 points and a radius
			// http://mathforum.org/library/drmath/view/53027.html

			var x1 = point1.X;
			var y1 = point1.Y;
			var x2 = point2.X;
			var y2 = point2.Y;

			var q = Sqrt(Pow(x2 - x1, 2) + Pow(y1 - y2, 2));

			var y3 = (y1 + y2) / 2;
			var x3 = (x1 + x2) / 2;

			var x = sign
				? x3 + Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (y1 - y2) / q
				: x3 - Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (y1 - y2) / q;

			var y = sign
				? y3 + Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (x2 - x1) / q
				: y3 - Sqrt(Max(0, Pow(radius, 2) - Pow((q / 2), 2))) * (x2 - x1) / q;

			return new Point(x, y);
		}

		public override void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			foreach (var point in points)
			{
				LineTo(point, isStroked, isSmoothJoin);
			}
		}

		public override void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException();
		}

		public override void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
		{
			throw new NotImplementedException();
		}

		public override void SetClosedState(bool closed)
		{
			if (bezierPath != null)
			{
				if (closed)
				{
#if false
					bezierPath.ClosePath();
#elif false
					bezierPath.Close();
#elif __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
					bezierPath.Close();
#elif false
					// TODO: In most cases, the path is handled by the browser.
					// But it might still be possible to hit this code path on Wasm?
					// This needs to be revisited.
#elif IS_UNIT_TESTS
					// Empty on unit tests.
#else
					throw new NotSupportedException("SetClosedState is not supported on this platform.");
#endif
				}
			}
		}

		public override void Dispose()
		{
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
			_owner.Close(bezierPath.Snapshot());
#else
			_owner.Close(bezierPath);
#endif
		}
	}
}
