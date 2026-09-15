#nullable enable

using System;
using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionRadialGradientBrush : CompositionGradientBrush
	{
		private protected override (SKShader? shader, SKColor color) GetPaintingParameters(SKRect bounds)
		{
			var center = EllipseCenter.ToSKPoint();
			var gradientOrigin = GradientOriginOffset.ToSKPoint();
			var radius = EllipseRadius.ToSKPoint();
			var transform = CreateTransformMatrix(bounds);

			// Transform the points into absolute coordinates.
			if (MappingMode == CompositionMappingMode.Relative)
			{
				// If the point are provided relative they must be multiplied by bounds.
				center.X *= bounds.Width;
				center.Y *= bounds.Height;

				gradientOrigin.X *= bounds.Width;
				gradientOrigin.Y *= bounds.Height;

				radius.X *= bounds.Width;
				radius.Y *= bounds.Height;
			}

			// Translate gradient points by bounds offset.
			center.X += bounds.Left;
			center.Y += bounds.Top;

			gradientOrigin.X += bounds.Left;
			gradientOrigin.Y += bounds.Top;
			//

			// SkiaSharp does not allow explicit definition of RadiusX and RadiusY.
			// Compute transformation matrix to compensate.
			ComputeRadiusAndScale(center, radius.X, radius.Y, out float gradientRadius, out SKMatrix matrix);

			if (gradientRadius > 0)
			{
				SKShader shader;
				if (EllipseCenter == GradientOriginOffset)
				{
					// Fast path for when ellipse center is the same as gradient origin
					shader =
						SKShader.CreateRadialGradient(
							center, gradientRadius, Colors, ColorPositions, TileMode, transform.PreConcat(matrix));
				}
				else
				{
					// The two-point conical gradient runs the other way round: its start circle is the
					// ellipse (centre, radius) at t = 0 and its end circle is the origin (radius 0) at
					// t = 1, so the LAST stop is what the outer circle carries and the FIRST stop is what
					// the focal point carries. An original offset p therefore sits at t = 1 - p, which is
					// the stop list read backwards: both the colour AND the position at index i come from
					// index (length - 1 - i). Mirroring each position in place instead would only be right
					// for a [0, a, 1] stop set - [0, 0.25, 0.75, 1] would come out as [0, 0.75, 0.25, 1],
					// which Skia forces monotonic and the middle colours land in the wrong places.
					var reversedColors = new SKColor[Colors!.Length];
					for (int i = 0; i < reversedColors.Length; i++)
					{
						reversedColors[i] = Colors[reversedColors.Length - 1 - i];
					}

					var reversedColorPositions = new float[ColorPositions!.Length];
					for (var i = 0; i < ColorPositions.Length; i++)
					{
						reversedColorPositions[i] = 1f - ColorPositions[ColorPositions.Length - 1 - i];
					}

					var totalMatrix = transform.PreConcat(matrix);
					gradientOrigin = totalMatrix.Invert().MapPoint(gradientOrigin);

					shader = SKShader.CreateCompose(
						SKShader.CreateColor(GetLastColorOrTransparent()),
						SKShader.CreateTwoPointConicalGradient(center, gradientRadius, gradientOrigin, 0, reversedColors, reversedColorPositions, TileMode, totalMatrix)
					);
				}

				return (shader, SKColors.Black);
			}
			else
			{
				// With radius equal to 0, SkiaSharp does not draw anything.
				// But we expect last gradient color.

				// If there are no gradient stops available, use transparent.

				SKColor color = GetLastColorOrTransparent();
				double alpha = (color.Alpha / 255.0);
				return (null, color.WithAlpha((byte)(alpha * 255)));
			}
		}

		private SKColor GetLastColorOrTransparent()
		{
			if (Colors!.Length > 0)
			{
				return Colors[Colors.Length - 1];
			}

			return SKColors.Transparent;
		}

		private void ComputeRadiusAndScale(SKPoint center, float radiusX, float radiusY, out float radius, out SKMatrix matrix)
		{
			matrix = SKMatrix.Identity;
			if (radiusX == 0 || radiusY == 0)
			{
				// Handle this specific case as zero division would cause us troubles.
				radius = 0;
				return;
			}

			float scaleDownRatio;
			if (radiusX >= radiusY)
			{
				// radiusX is larger, use it and scale down radiusY.
				radius = radiusX;

				scaleDownRatio = radiusY / radiusX;

				SetScaleTranslate(
					ref matrix,
					/* scale x */ 1,
					/* scale y */ scaleDownRatio,
					/* translate x */ 0,
					/* translate y */ center.Y - scaleDownRatio * center.Y);
			}
			else
			{
				// radiusY is larger, use it and scale down radiusX.
				radius = radiusY;

				scaleDownRatio = radiusX / radiusY;

				SetScaleTranslate(
					ref matrix,
					/* scale x */ scaleDownRatio,
					/* scale y */ 1,
					/* translate x */ center.X - scaleDownRatio * center.X,
					/* translate y */ 0);
			}
		}

		private void SetScaleTranslate(ref SKMatrix matrix, float sx, float sy, float tx, float ty)
		{
			matrix.ScaleX = sx;
			matrix.SkewX = 0;
			matrix.TransX = tx;

			matrix.SkewY = 0;
			matrix.ScaleY = sy;
			matrix.TransY = ty;

			matrix.Persp0 = 0;
			matrix.Persp1 = 0;
			matrix.Persp2 = 1;
		}
	}
}
