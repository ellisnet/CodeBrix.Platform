#nullable enable

using System.Linq;
using Microsoft.UI.Composition;
using SkiaSharp;


namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.IVisualPlatform"/> for a <see cref="ShapeVisual"/>: it applies the
/// view box and draws the visual's shapes.
/// </summary>
/// <remarks>This is the rendering code that lived in <c>ShapeVisual.skia.cs</c>, moved verbatim.</remarks>
internal class ShapeVisualSkiaPlatform : ContainerVisualSkiaPlatform
{
	private readonly ShapeVisual _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The visual this object renders.</param>
	internal ShapeVisualSkiaPlatform(ShapeVisual owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		var canvas = session.Canvas;
		var size = _owner.Size;
		var viewBox = _owner.ViewBox;

		if (size.X == 0 || size.Y == 0)
		{
			return;
		}

		// TODO: ShapeVisuals should be clipping to the size rect. However, this breaks shapes for us because
		// we implement them with ShapeVisuals and they don't clip anything. The problem is that
		// the WinUI implementation doesn't use ShapeVisuals for shapes, but a combination of ContainerVisuals and
		// SpriteVisuals. When_StrokeThickness_Is_GreaterThan_Or_Equals_Width and
		// When_Border_CornerRadius_HitTesting fail when you uncomment the following line.
		// canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y));

		// TODO: ViewBox.Stretch, ViewBox.HorizontalAlignmentRatio and ViewBox.VerticalAlignmentRatio
		if (viewBox is not null)
		{
			canvas.Scale(
				viewBox.Size.X > 0 ? size.X / viewBox.Size.X : 1,
				viewBox.Size.Y > 0 ? size.Y / viewBox.Size.Y : 1);
			canvas.Translate(-viewBox.Offset.X, -viewBox.Offset.Y); // translate before scaling
		}

		if (_owner.ShapesIfCreated is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				CompositionShapeSkiaPlatform.Of(shapes[i]).Render(in session);
			}
		}

		base.Paint(in session);
	}

	/// <inheritdoc />
	public override bool RequiresRepaintOnEveryFrame => _owner._needsContinuousUpdates;

	/// <inheritdoc />
	public override bool CanPaint() => base.CanPaint() || (_owner.ShapesIfCreated?.Any(s => s.CanPaint()) ?? false);
}
