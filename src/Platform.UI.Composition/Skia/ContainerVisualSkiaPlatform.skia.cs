#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.IVisualPlatform"/> for a <see cref="ContainerVisual"/>: it adds
/// the layout clip to the clipping that <see cref="VisualSkiaPlatform"/> applies before painting.
/// </summary>
/// <remarks>This is the rendering code that lived in <c>ContainerVisual.skia.cs</c>, moved verbatim.</remarks>
internal class ContainerVisualSkiaPlatform : VisualSkiaPlatform
{
	private static SKPath _sparePrePaintingClippingPath = new SKPath();

	private readonly ContainerVisual _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The visual this object renders.</param>
	internal ContainerVisualSkiaPlatform(ContainerVisual owner) : base(owner)
	{
		_owner = owner;
	}

	/// <returns>true if a ViewBox exists</returns>
	internal bool GetArrangeClipPathInElementCoordinateSpace(SKPath dst) // TODO: Do not use SKPath here, bad for perf and prevents usage for IDirectManipulationHandler.IsInBoundsForResume
	{
		if (_owner.LayoutClip is not { isAncestorClip: var isAncestorClip, rect: var rect })
		{
			return false;
		}

		var clipRect = rect.ToSKRect();
		using var clipRectBuilder = new SKPathBuilder();
		clipRectBuilder.AddRect(clipRect);
		using var clipRectPath = clipRectBuilder.Snapshot();
		dst.Op(clipRectPath, SKPathOp.Union, dst);
		if (isAncestorClip)
		{
			Matrix4x4.Invert(_owner.TotalMatrix, out var totalMatrixInverted);
			var childToParentTransform = _owner.Parent!.TotalMatrix * totalMatrixInverted;
			if (!childToParentTransform.IsIdentity)
			{
				dst.Transform(childToParentTransform.ToSKMatrix());
			}
		}

		return true;
	}

	internal override bool GetPrePaintingClipping(SKPath dst) // TODO: Do not use SKPath here, bad for perf and prevents usage for IDirectManipulationHandler.IsInBoundsForResume
	{
		var prePaintingClipPath = _sparePrePaintingClippingPath;

		prePaintingClipPath.Reset();

		if (base.GetPrePaintingClipping(dst))
		{
			// TODO: SKPath-less
			//if (GetArrangeClipPathInElementCoordinateSpace() is {} clipping)
			//{
			//	dst.AddRect(clipping.ToSKRect());
			//}

			if (GetArrangeClipPathInElementCoordinateSpace(prePaintingClipPath))
			{
				dst.Op(prePaintingClipPath, SKPathOp.Intersect, dst);
			}

			return true;
		}
		else
		{
			// TODO: SKPath-less
			//if (GetArrangeClipPathInElementCoordinateSpace() is {} clipping)
			//{
			//	dst.Reset();
			//	dst.AddRect(clipping.ToSKRect());

			//	return true;
			//}

			if (GetArrangeClipPathInElementCoordinateSpace(prePaintingClipPath))
			{
				dst.Reset();
				dst.Op(prePaintingClipPath, SKPathOp.Union, dst);

				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
