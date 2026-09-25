#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Graphics;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// Turns the WinUI geometry model (<see cref="Geometry"/> and its subclasses, including
/// <see cref="CodeBrix.Platform.Media.StreamGeometry"/>) and the shapes' own outlines into platform paths. A platform
/// path crosses this contract as an opaque <see cref="IGeometrySource2D"/>, which is what the composition contracts
/// accept (<see cref="Microsoft.UI.Composition.CompositionPath"/>).
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once through <see cref="PlatformServices.Geometry"/>. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Skia.GeometrySkiaPlatform</c> (an <c>SKPath</c> in a <c>SkiaGeometrySource2D</c>),
/// registered by <c>CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IGeometryPlatform
{
	/// <summary>
	/// Creates an empty path builder, used by a stream geometry context.
	/// </summary>
	/// <returns>A new builder.</returns>
	IGeometryPathBuilder CreatePathBuilder();

	/// <summary>
	/// Builds the outline of <paramref name="geometry"/> (every figure, filled or not) as a new platform path that
	/// the caller owns.
	/// </summary>
	/// <param name="geometry">The geometry.</param>
	/// <returns>The platform path.</returns>
	/// <exception cref="System.NotSupportedException">The platform cannot build this kind of geometry.</exception>
	IGeometrySource2D CreateGeometrySource(Geometry geometry);

	/// <summary>
	/// Builds the filled part of <paramref name="geometry"/> (only the figures that are filled) as a new platform
	/// path, for geometries that distinguish filled from unfilled figures.
	/// </summary>
	/// <param name="geometry">The geometry.</param>
	/// <returns>The platform path, or <see langword="null"/> when the geometry has no separate filled outline.</returns>
	IGeometrySource2D? CreateFilledGeometrySource(Geometry geometry);

	/// <summary>
	/// Builds a closed ellipse that fills <paramref name="bounds"/>.
	/// </summary>
	/// <param name="bounds">The bounds of the ellipse.</param>
	/// <returns>The platform path.</returns>
	IGeometrySource2D CreateEllipse(Rect bounds);

	/// <summary>
	/// Builds a closed rectangle; when both radii are non-zero, its corners are rounded.
	/// </summary>
	/// <param name="rect">The rectangle.</param>
	/// <param name="radiusX">The horizontal corner radius.</param>
	/// <param name="radiusY">The vertical corner radius.</param>
	/// <returns>The platform path.</returns>
	IGeometrySource2D CreateRectangle(Rect rect, double radiusX, double radiusY);

	/// <summary>
	/// Returns the tight bounds of a platform path (the bounds of the curve, not of its control points).
	/// </summary>
	/// <param name="geometry">A platform path built by this platform.</param>
	/// <returns>The tight bounds.</returns>
	Rect GetTightBounds(IGeometrySource2D geometry);
}
