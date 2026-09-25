#nullable enable

using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionBrushPlatform"/> for a <see cref="CompositionMaskBrush"/>.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionMaskBrush.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionMaskBrushSkiaPlatform : CompositionBrushSkiaPlatform
{
	private readonly CompositionMaskBrush _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionMaskBrushSkiaPlatform(CompositionMaskBrush owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	public override bool RequiresRepaintOnEveryFrame => _owner.Source is not null && _owner.Mask is not null && (_owner.Source.RequiresRepaintOnEveryFrame || _owner.Mask.RequiresRepaintOnEveryFrame);

	/// <inheritdoc />
	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		if (_owner.Source is not { } source || _owner.Mask is not { } mask)
		{
			return;
		}
		_spareResultPaint.Reset();
		_spareResultPaint.IsAntialias = true;
		_spareResultPaint.BlendMode = SKBlendMode.SrcOver;
		_spareResultPaint2.Reset();
		_spareResultPaint2.IsAntialias = true;
		_spareResultPaint2.BlendMode = SKBlendMode.DstIn;
		// The first SaveLayer call along with DrawColor(Transparent) basically create a clean secondary drawing surface
		// but without having to call SKSurface.Create and having to deal with all the details like HWA.
		canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = _spareResultPaint });
		canvas.ClipRect(bounds, antialias: true);
		canvas.DrawColor(SKColors.Transparent);
		Of(source).Paint(canvas, opacity, bounds);
		// The second SaveLayer call with SKBlendMode.DstIn creates the masking effect
		canvas.SaveLayer(new SKCanvasSaveLayerRec { Paint = _spareResultPaint2 });
		Of(mask).Paint(canvas, opacity, bounds);
		canvas.Restore();
		canvas.Restore();
	}

	/// <inheritdoc />
	public override bool CanPaint() => (_owner.Source?.CanPaint() ?? false) || (_owner.Mask?.CanPaint() ?? false);

	private static readonly SKPaint _spareResultPaint = new SKPaint();
	private static readonly SKPaint _spareResultPaint2 = new SKPaint();
}
