#nullable enable

using System;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionClipPlatform"/> for a <see cref="CompositionGeometricClip"/>:
/// the geometry's path, transformed by the clip's transform.
/// </summary>
/// <remarks>This is the clipping code that lived in <c>CompositionGeometricClip.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionGeometricClipSkiaPlatform : CompositionClipSkiaPlatform
{
	private static readonly SKPath _spareTransformedPath = new();

	private readonly CompositionGeometricClip _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The clip this object applies.</param>
	internal CompositionGeometricClipSkiaPlatform(CompositionGeometricClip owner) : base(owner)
	{
		_owner = owner;
	}

	internal override SKPath? GetClipPath(Visual visual)
	{
		if (_owner.Geometry is { } Geometry)
		{
			var geometry = Geometry.BuildGeometry();

			if (geometry is SkiaGeometrySource2D geometrySource)
			{
				var path = geometrySource.Geometry;
				var TransformMatrix = _owner.TransformMatrix;
				if (!TransformMatrix.IsIdentity)
				{
					var transformedPath = _spareTransformedPath;
					transformedPath.Reset();
					path.Transform(TransformMatrix.ToSKMatrix(), transformedPath);
					path = transformedPath;
				}

				return path;
			}
			else
			{
				throw new InvalidOperationException($"Clipping with source {geometry} is not supported");
			}
		}

		return null;
	}
}
