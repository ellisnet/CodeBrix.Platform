#nullable enable

using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using Windows.Foundation;
using Windows.Graphics;

namespace CodeBrix.Platform.UI.Composition.Contracts;

/// <summary>
/// Builds the platform geometry behind the composition geometries (<see cref="CompositionLineGeometry"/>,
/// <see cref="CompositionRectangleGeometry"/>, <see cref="CompositionRoundedRectangleGeometry"/>,
/// <see cref="CompositionEllipseGeometry"/>, <see cref="CompositionPathGeometry"/>). Each built geometry is an
/// opaque <see cref="IGeometrySource2D"/>; the composition geometry keeps it, returns it from
/// <see cref="CompositionGeometry.BuildGeometry"/>, and disposes it (when it is <see cref="System.IDisposable"/>)
/// when it is replaced.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once through <see cref="CompositionPlatformServices.Geometry"/> (lazily, on first use). The Skia
/// implementation is <c>CodeBrix.Platform.UI.Composition.Skia.CompositionGeometrySkiaPlatform</c> (an
/// <c>SKPath</c> in a <c>SkiaGeometrySource2D</c>), registered by the assembly's <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface ICompositionGeometryPlatform
{
	/// <summary>
	/// Builds a line from <paramref name="start"/> to <paramref name="end"/>.
	/// </summary>
	/// <param name="start">The start point.</param>
	/// <param name="end">The end point.</param>
	/// <returns>The built geometry.</returns>
	IGeometrySource2D CreateLine(Vector2 start, Vector2 end);

	/// <summary>
	/// Builds a closed rectangle.
	/// </summary>
	/// <param name="offset">The top-left corner.</param>
	/// <param name="size">The size.</param>
	/// <returns>The built geometry.</returns>
	IGeometrySource2D CreateRectangle(Vector2 offset, Vector2 size);

	/// <summary>
	/// Builds a closed rectangle whose corners are rounded with <paramref name="cornerRadius"/> (clamped to half
	/// the size). The caller decides whether a rectangle with a zero radius is built with
	/// <see cref="CreateRectangle"/> instead.
	/// </summary>
	/// <param name="offset">The top-left corner.</param>
	/// <param name="size">The size.</param>
	/// <param name="cornerRadius">The corner radii.</param>
	/// <returns>The built geometry.</returns>
	IGeometrySource2D CreateRoundedRectangle(Vector2 offset, Vector2 size, Vector2 cornerRadius);

	/// <summary>
	/// Builds a closed ellipse.
	/// </summary>
	/// <param name="center">The center.</param>
	/// <param name="radius">The horizontal and vertical radii.</param>
	/// <returns>The built geometry.</returns>
	IGeometrySource2D CreateEllipse(Vector2 center, Vector2 radius);

	/// <summary>
	/// Builds a path from the recorded Direct2D sink commands of a <see cref="CompositionPathGeometry"/>.
	/// </summary>
	/// <param name="commands">The commands, in order; the caller clears the list afterwards.</param>
	/// <returns>The built geometry.</returns>
	IGeometrySource2D CreatePath(List<CompositionPathCommand> commands);

	/// <summary>
	/// Gets a value indicating whether <paramref name="source"/> is a geometry this platform built (and so can be
	/// used by a <see cref="CompositionPath"/> as it is).
	/// </summary>
	/// <param name="source">The geometry source.</param>
	/// <returns><see langword="true"/> for a platform geometry.</returns>
	bool IsPlatformGeometry(IGeometrySource2D source);

	/// <summary>
	/// Returns the tight bounds of <paramref name="geometry"/>, for a <see cref="CompositionGeometricClip"/>.
	/// </summary>
	/// <param name="geometry">A geometry built by this platform.</param>
	/// <returns>The tight bounds.</returns>
	/// <exception cref="System.InvalidOperationException"><paramref name="geometry"/> is not a platform geometry.</exception>
	Rect GetClipBounds(IGeometrySource2D? geometry);
}
