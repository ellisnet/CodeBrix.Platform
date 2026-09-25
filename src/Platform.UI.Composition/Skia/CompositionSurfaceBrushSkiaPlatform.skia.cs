#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.UI.Composition;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionBrushPlatform"/> for a
/// <see cref="CompositionSurfaceBrush"/>: it draws the surface's image (stretched, aligned and transformed) or, for
/// a <see cref="CompositionVisualSurface"/>, renders the source visual.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionSurfaceBrush.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionSurfaceBrushSkiaPlatform : CompositionBrushSkiaPlatform
{
	private static readonly SKPaint _tempPaint = new();

	private readonly CompositionSurfaceBrush _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionSurfaceBrushSkiaPlatform(CompositionSurfaceBrush owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	public override bool RequiresRepaintOnEveryFrame => _owner.Surface is CompositionVisualSurface;

	/// <inheritdoc />
	public override Vector2? Size => _owner.Surface switch
	{
		PlatformCompositionSurface scs when CompositionSurfaceSkiaPlatform.Of(scs).Image is SKImage img => new(img.Width, img.Height),
		CompositionVisualSurface visualSurface => CompositionVisualSurfaceSkiaPlatform.GetSize(visualSurface),
		IPlatformCompositionSurfaceProvider { PlatformCompositionSurface: PlatformCompositionSurface pscs } when CompositionSurfaceSkiaPlatform.Of(pscs).Image is SKImage img => new(img.Width, img.Height),
		_ => null
	};

	private Rect GetArrangedImageRect(Size sourceSize, SKRect targetRect)
	{
		var size = GetArrangedImageSize(sourceSize, targetRect.Size.ToSize());

		var point = new Point(targetRect.Left, targetRect.Top);
		point.X += (targetRect.Width - size.Width) * _owner.HorizontalAlignmentRatio;
		point.Y += (targetRect.Height - size.Height) * _owner.VerticalAlignmentRatio;
		return new Rect(point, size);
	}

	private Size GetArrangedImageSize(Size sourceSize, Size targetSize)
	{
		var sourceAspectRatio = sourceSize.AspectRatio();
		var targetAspectRatio = targetSize.AspectRatio();
		switch (_owner.Stretch)
		{
			default:
			case CompositionStretch.None:
				return sourceSize;
			case CompositionStretch.Fill:
				return targetSize;
			case CompositionStretch.Uniform:
				return targetAspectRatio > sourceAspectRatio
					? new Size(sourceSize.Width * targetSize.Height / sourceSize.Height, targetSize.Height)
					: new Size(targetSize.Width, sourceSize.Height * targetSize.Width / sourceSize.Width);
			case CompositionStretch.UniformToFill:
				return targetAspectRatio < sourceAspectRatio
					? new Size(sourceSize.Width * targetSize.Height / sourceSize.Height, targetSize.Height)
					: new Size(targetSize.Width, sourceSize.Height * targetSize.Width / sourceSize.Width);
		}
	}

	private static bool TryGetPlatformCompositionSurface(ICompositionSurface? surface, [NotNullWhen(true)] out PlatformCompositionSurface? skiaCompositionSurface)
	{
		if (surface is PlatformCompositionSurface scs)
		{
			skiaCompositionSurface = scs;
			return true;
		}
		else if (surface is IPlatformCompositionSurfaceProvider scsp && scsp.PlatformCompositionSurface is PlatformCompositionSurface scsps)
		{
			skiaCompositionSurface = scsps;
			return true;
		}

		skiaCompositionSurface = null;
		return false;
	}

	/// <inheritdoc />
	public override bool CanPaint() => TryGetPlatformCompositionSurface(_owner.Surface, out _) || _owner.Surface is CompositionVisualSurface;

	/// <inheritdoc />
	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		if (bounds.IsEmpty)
		{
			return;
		}

		var Surface = _owner.Surface;
		if (Surface is CompositionVisualSurface visualSurface)
		{
			canvas.Save();
			canvas.ClipRect(bounds, antialias: true);
			CompositionVisualSurfaceSkiaPlatform.Paint(visualSurface, canvas, opacity);
			canvas.Restore();
		}
		else if (TryGetPlatformCompositionSurface(Surface, out var scs))
		{
			var backgroundArea = GetArrangedImageRect(new Size(CompositionSurfaceSkiaPlatform.Of(scs).Image!.Width, CompositionSurfaceSkiaPlatform.Of(scs).Image!.Height), bounds);

			if (backgroundArea.Width <= 0 || backgroundArea.Height <= 0)
			{
				return;
			}

			// Relevant doc snippet from WPF: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/brush-transformation-overview#differences-between-the-transform-and-relativetransform-properties
			// When you apply a transform to a brush's RelativeTransform property, that transform is applied to the brush before its output is mapped to the painted area. The following list describes the order in which a brush’s contents are processed and transformed.
			//  * Process the brush’s contents. For a GradientBrush, this means determining the gradient area. For a TileBrush, the Viewbox is mapped to the Viewport. This becomes the brush’s output.
			// 	* Project the brush’s output onto the 1 x 1 transformation rectangle.
			// 	* Apply the brush’s RelativeTransform, if it has one.
			// 	* Project the transformed output onto the area to paint.
			// 	* Apply the brush’s Transform, if it has one.
			var matrix = Matrix3x2.Identity;
			matrix *= Matrix3x2.CreateScale((float)(backgroundArea.Width / CompositionSurfaceSkiaPlatform.Of(scs).Image!.Width),
				(float)(backgroundArea.Height / CompositionSurfaceSkiaPlatform.Of(scs).Image!.Height));
			matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);
			matrix *= _owner.TransformMatrix;
			matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height).Inverse();
			matrix *= _owner.RelativeTransform;
			matrix *= Matrix3x2.CreateScale(bounds.Width, bounds.Height);

			_tempPaint.Reset();
			_tempPaint.IsAntialias = true;
			if (_owner.MonochromeTint is { } tint)
			{
				var color = tint.ToSKColor();
				_tempPaint.ColorFilter = SKColorFilter.CreateBlendMode(color.WithAlpha((byte)(color.Alpha * opacity)), SKBlendMode.SrcIn);
			}
			else
			{
				_tempPaint.ColorFilter = opacity.ToColorFilter();
			}

			canvas.Save();
			canvas.Concat(matrix.ToSKMatrix());
			// Ideally, we would use a CatmullRom sampler when upscaling (i.e. bounds.Size > scs.Image.Size) and
			// a Lanczos sampler when downscaling. However, profiling shows that CatmullRom chokes when the
			// drawing are (i.e. bounds) is large and the improvement over linear sampling is almost imperceptible.
			// For downsampling, Lanczos is slightly better than linear filtering with mipmapping but chokes when
			// the downscaling ratio is too big. Linear filtering with mipmapping is mostly okay but in the most
			// extreme cases with tons of images it's quite a bit slower than a linear filter without improving
			// the output that much.
			canvas.DrawImage(CompositionSurfaceSkiaPlatform.Of(scs).Image!, 0, 0, new SKSamplingOptions(SKFilterMode.Linear), _tempPaint);
			canvas.Restore();
		}
	}
}
