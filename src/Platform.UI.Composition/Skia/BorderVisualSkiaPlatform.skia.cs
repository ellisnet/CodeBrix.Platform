#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;


namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.IVisualPlatform"/> for a <see cref="BorderVisual"/>: it builds the
/// background and border geometry (and the clips that rounded corners impose on the background and the children),
/// paints the background and the border, and hit-tests them.
/// </summary>
/// <remarks>This is the rendering code that lived in <c>BorderVisual.skia.cs</c>, moved verbatim.</remarks>
internal class BorderVisualSkiaPlatform : ContainerVisualSkiaPlatform
{
	private static readonly SKPathBuilder _sparePrePaintingClippingPath = new SKPathBuilder();

	private readonly BorderVisual _owner;

	private SKRoundRect? _borderPathOuterRect;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The visual this object renders.</param>
	internal BorderVisualSkiaPlatform(BorderVisual owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		UpdatePathsAndCornerClip();

		if (_owner._backgroundShape is { } backgroundShape)
		{
			session.Canvas.Save();
			// it's necessary to clip the background because not all backgrounds are simple rounded rectangles with a solid color.
			// E.g. effect brushes will draw outside the intended area if they're not clipped.
			if (_owner._backgroundClip is { } backgroundClip)
			{
				CompositionClipSkiaPlatform.Of(backgroundClip).ApplyClip(_owner, session.Canvas);
			}
			CompositionShapeSkiaPlatform.Of(backgroundShape).Render(in session);
			session.Canvas.Restore();
		}

		base.Paint(in session);

		if (_owner._borderShape is { } borderShape)
		{
			CompositionShapeSkiaPlatform.Of(borderShape).Render(in session);
		}
	}

	internal override bool GetPrePaintingClipping(SKPath dst)
	{
		// This method is only important for airspace (to accurately deal with corner radii, etc.),
		// other than that it doesn't really do anything.
		UpdatePathsAndCornerClip();

		if (_owner._cornerRadius != CornerRadius.None && _borderPathOuterRect is { } rect)
		{
			if (base.GetPrePaintingClipping(dst))
			{
				_sparePrePaintingClippingPath.AddRoundRect(rect);

				using var path = _sparePrePaintingClippingPath.Detach();

				dst.Op(path, SKPathOp.Intersect, dst);

				return true;
			}
			else
			{
				dst.Reset();
				_sparePrePaintingClippingPath.AddRoundRect(rect);
				using var elsePath = _sparePrePaintingClippingPath.Detach();
				dst.Op(elsePath, SKPathOp.Union, dst);
				return true;
			}
		}
		else
		{
			return base.GetPrePaintingClipping(dst);
		}
	}

	private protected override SKPath? GetPostPaintingClipping()
	{
		UpdatePathsAndCornerClip();
		return (_owner._childClipCausedByCornerRadius is { } childClip ? CompositionClipSkiaPlatform.Of(childClip).GetClipPath(_owner) : null) is { } path
			? base.GetPostPaintingClipping() is { } baseClip
				? path.Op(baseClip, SKPathOp.Intersect)
				: path
			: base.GetPostPaintingClipping();
	}

	private protected override void ApplyPostPaintingClipping(SKCanvas canvas)
	{
		if (base.GetPostPaintingClipping() is null)
		{
			// At the time of writing, this branch is always taken
			UpdatePathsAndCornerClip();
			if (_owner._childClipCausedByCornerRadius is { } childClip)
			{
				CompositionClipSkiaPlatform.Of(childClip).ApplyClip(_owner, canvas);
			}
		}
		else if (GetPostPaintingClipping() is { } clip)
		{
			canvas.ClipPath(clip);
		}
	}

	private void UpdatePathsAndCornerClip()
	{
		var owner = _owner;
		if (owner._borderPathValid && owner._backgroundPathValid)
		{
			return;
		}

		var Size = owner.Size;
		var Compositor = owner.Compositor;

		// clear old state
		owner._childClipCausedByCornerRadius = null;
		owner._backgroundClip = null;

		var outerArea = new SKRect(0, 0, Size.X, Size.Y);
		var innerArea = new SKRect(
			(float)owner._borderThickness.Left,
			(float)owner._borderThickness.Top,
			(float)(owner._borderThickness.Left + Math.Max(0, Size.X - (owner._borderThickness.Left + owner._borderThickness.Right))),
			(float)(owner._borderThickness.Top + Math.Max(0, Size.Y - (owner._borderThickness.Top + owner._borderThickness.Bottom))));

		// note that we're sending (the full) Size, not size
		var fullCornerRadius = owner._cornerRadius.GetRadii(Size.ToSize(), owner._borderThickness);

		unsafe
		{
			var outerRadii = stackalloc SKPoint[4];
			var innerRadii = stackalloc SKPoint[4];
			fullCornerRadius.Outer.GetRadii(outerRadii);
			fullCornerRadius.Inner.GetRadii(innerRadii);

			if (!owner._backgroundPathValid)
			{
				owner._backgroundPathValid = true;
				if (owner._backgroundBrush is not null)
				{
					// We don't pass down <inner|outer>Area directly, since it contains the thickness offsets.
					// Instead, we only pass the Size (without the X and Y offsets).
					// The offsets shouldn't be part of the background path calculations, but should be done
					// at the point of rendering by translation the final output by the thickness.
					// This matters because if the path is for an image with a scaling RelativeTransform.
					// In that case, if you factor the thickness in the path itself (i.e. include it in SKPath.Bounds),
					// the shader will sample from the image after the offset is applied.
					// E.g., if we have a border with a 20px border thickness and 100x100 background area for an ImageBrush with a
					// RelativeTransform = ScaleTransform { ScaleX = 3, ScaleY = 3, CenterX = 0.5, CenterY = 0.5 }, here's what we want:
					// |-----------------300px---------------------|
					// |                                           |
					// |<-100px->                        <-100px-> |
					// |         |---------100px--------|          |
					// |         |                      |<---------/---- what we want the shader to sample.
					// |         |      final           |          | <-- image scaled to 100*3 x 100*3
					// |         |      drawing         |          |
					// 300px   100px    area          100px      300px
					// |         |                      |          |
					// |         |                      |          |
					// |         |                      |          |
					// |         |---------100px--------|          |
					// |                                           |
					// |                                           |
					// |-----------------300px---------------------|

					// Here's what we don't want:
					//    |-----------------300px---------------------|
					//    |                                           |
					//    |<80px>                         <--120px--> |
					//    |      |---------100px--------|             |
					//    |      |                      |<------------/---- same exact final drawing area (in absolute window coordinates)
					//    |      |      final           |             | <-- but outer image shifted by 20px to the right
					//    |      |      drawing         |             |
					// 300px   100px    area          100px         300px
					//    |      |                      |             |
					//    |      |                      |             |
					//    |      |                      |             |
					//    |      |---------100px--------|             |
					//    |                                           |
					//    |                                           |
					//    |-----------------300px---------------------|

					var backgroundPath = CreateBackgroundPath(owner._useInnerBorderBoundsAsAreaForBackground, innerArea.Size,
						outerArea.Size, outerRadii, innerRadii);
					((CompositionPathGeometry)owner._backgroundShape!.Geometry!).Path =
						new CompositionPath(new SkiaGeometrySource2D(backgroundPath));
					owner._backgroundShape!.Offset = owner._useInnerBorderBoundsAsAreaForBackground
						? new Vector2((float)owner._borderThickness.Left, (float)owner._borderThickness.Top)
						: Vector2.Zero;
				}
				else if (owner._backgroundShape is not null) // reset values
				{
					((CompositionPathGeometry)owner._backgroundShape!.Geometry!).Path = null;
					owner._backgroundShape!.Offset = Vector2.Zero;
				}
			}

			if (!owner._borderPathValid)
			{
				owner._borderPathValid = true;
				if (owner._borderBrush is not null)
				{
					var borderPath = CreateBorderPath(innerArea, outerArea, outerRadii, innerRadii);
					((CompositionPathGeometry)owner._borderShape!.Geometry!).Path =
						new CompositionPath(new SkiaGeometrySource2D(borderPath));
				}
				else if (owner._borderShape is not null)
				{
					((CompositionPathGeometry)owner._borderShape!.Geometry!).Path = null;
				}
			}
		}

		// Note: The clipping is used to determine the location where the children of current element can be rendered.
		//		 So its has to be the "inner" area (i.e. the area without the border).
		//		 The border and the background shapes are already clipped properly and will be drawn without this clipping property set.
		// Note 2: This only applies when there is at least one corner with a corner radius. This means that a child
		//         that draws outside the bounds of this visual might not be clipped normally, but merely adding
		//         a non-empty CornerRadius will clip the child(ren). This matches WinUI even though it's not intuitive.
		if (!fullCornerRadius.IsEmpty)
		{
			owner._childClipCausedByCornerRadius = Compositor.CreateRectangleClip(
				innerArea.Left, innerArea.Top, innerArea.Right, innerArea.Bottom,
				fullCornerRadius.Inner.TopLeft, fullCornerRadius.Inner.TopRight, fullCornerRadius.Inner.BottomRight, fullCornerRadius.Inner.BottomLeft);

			if (owner._useInnerBorderBoundsAsAreaForBackground)
			{
				owner._backgroundClip = Compositor.CreateRectangleClip(
					innerArea.Left, innerArea.Top, innerArea.Right, innerArea.Bottom,
					fullCornerRadius.Inner.TopLeft, fullCornerRadius.Inner.TopRight, fullCornerRadius.Inner.BottomRight, fullCornerRadius.Inner.BottomLeft);
			}
			else
			{
				owner._backgroundClip = Compositor.CreateRectangleClip(
					outerArea.Left, outerArea.Top, outerArea.Right, outerArea.Bottom,
					fullCornerRadius.Outer.TopLeft, fullCornerRadius.Outer.TopRight, fullCornerRadius.Outer.BottomRight, fullCornerRadius.Outer.BottomLeft);
			}
		}
	}

	private static unsafe SKPath CreateBackgroundPath(bool useInnerBorderBoundsAsAreaForBackground, SKSize innerArea, SKSize outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		using var backgroundPath = new SKPathBuilder();
		var roundRect = new SKRoundRect();
		var rect = useInnerBorderBoundsAsAreaForBackground
			? new SKRect(0, 0, innerArea.Width, innerArea.Height)
			: new SKRect(0, 0, outerArea.Width, outerArea.Height);
		CodeBrixSkiaApi.sk_rrect_set_rect_radii(
			roundRect.Handle,
			&rect,
			useInnerBorderBoundsAsAreaForBackground ? innerRadii : outerRadii);
		backgroundPath.AddRoundRect(roundRect);
		backgroundPath.Close();

		return backgroundPath.Snapshot();
	}

	private unsafe SKPath CreateBorderPath(SKRect innerArea, SKRect outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		using var borderPath = new SKPathBuilder();

		borderPath.FillType = SKPathFillType.EvenOdd;

		// The order here (outer then inner) is important because of the SKPathFillType.
		{
			var outerRect = new SKRoundRect();
			CodeBrixSkiaApi.sk_rrect_set_rect_radii(outerRect.Handle, &outerArea, outerRadii);
			_borderPathOuterRect = outerRect;
			borderPath.AddRoundRect(outerRect);
			borderPath.Close();
		}
		{
			var innerRect = new SKRoundRect();
			CodeBrixSkiaApi.sk_rrect_set_rect_radii(innerRect.Handle, &innerArea, innerRadii);
			borderPath.AddRoundRect(innerRect);
			borderPath.Close();
		}

		return borderPath.Snapshot();
	}

	/// <inheritdoc />
	public override bool CanPaint() =>
		(_owner._backgroundBrush?.CanPaint() ?? false) ||
		(_owner._borderBrush?.CanPaint() ?? false) ||
		base.CanPaint();

	/// <inheritdoc />
	public override bool RequiresRepaintOnEveryFrame => (_owner._backgroundBrush?.RequiresRepaintOnEveryFrame ?? false) || (_owner._borderBrush?.RequiresRepaintOnEveryFrame ?? false);

	/// <inheritdoc />
	public override bool HitTest(Point point)
	{
		UpdatePathsAndCornerClip();
		return (_owner._borderShape?.HitTest(point) ?? false) || (_owner._backgroundShape?.HitTest(point) ?? false);
	}
}
