#nullable enable

using Windows.Foundation;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Builds one platform path, figure by figure, for a <see cref="CodeBrix.Platform.Media.StreamGeometry"/>. The
/// WinUI figure and segment model (and the arc arithmetic) stays in the platform-neutral
/// <c>PathStreamGeometryContext</c>; this builder only records the resulting path commands.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Created by <see cref="IGeometryPlatform.CreatePathBuilder"/>, one per opened stream geometry context. The Skia
/// implementation records into an <c>SKPathBuilder</c> (<c>CodeBrix.Platform.UI.Skia.GeometrySkiaPlatform</c>).
/// </para>
/// </remarks>
internal interface IGeometryPathBuilder
{
	/// <summary>
	/// Starts a new figure at <paramref name="point"/>.
	/// </summary>
	/// <param name="point">The start point of the figure.</param>
	void MoveTo(Point point);

	/// <summary>
	/// Adds a straight line from the current point to <paramref name="point"/>.
	/// </summary>
	/// <param name="point">The end point of the line.</param>
	void LineTo(Point point);

	/// <summary>
	/// Adds a cubic Bezier curve from the current point.
	/// </summary>
	/// <param name="point1">The first control point.</param>
	/// <param name="point2">The second control point.</param>
	/// <param name="point3">The end point.</param>
	void CubicTo(Point point1, Point point2, Point point3);

	/// <summary>
	/// Adds a quadratic Bezier curve from the current point.
	/// </summary>
	/// <param name="point1">The control point.</param>
	/// <param name="point2">The end point.</param>
	void QuadTo(Point point1, Point point2);

	/// <summary>
	/// Adds an arc of the ellipse bounded by <paramref name="oval"/>, connected to the current point with a line.
	/// </summary>
	/// <param name="oval">The bounds of the ellipse the arc lies on.</param>
	/// <param name="startAngle">The start angle, in degrees.</param>
	/// <param name="sweepAngle">The sweep angle, in degrees (negative sweeps counterclockwise).</param>
	void ArcTo(Rect oval, double startAngle, double sweepAngle);

	/// <summary>
	/// Closes the current figure.
	/// </summary>
	void Close();

	/// <summary>
	/// Returns the path built so far, as an opaque platform path handle that the geometry platform understands.
	/// </summary>
	/// <returns>The platform path.</returns>
	object Snapshot();
}
