#nullable enable

using Microsoft.UI.Composition;
using SkiaSharp;


namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.IVisualPlatform"/> for a <see cref="SpriteVisual"/>: it paints the
/// visual's brush over the visual's size.
/// </summary>
/// <remarks>This is the rendering code that lived in <c>SpriteVisual.skia.cs</c>, moved verbatim.</remarks>
internal class SpriteVisualSkiaPlatform : ContainerVisualSkiaPlatform
{
	private readonly SpriteVisual _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The visual this object renders.</param>
	internal SpriteVisualSkiaPlatform(SpriteVisual owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		if (_owner.Brush is { } brush)
		{
			CompositionBrushSkiaPlatform.Of(brush).Paint(session.Canvas, session.Opacity, new SKRect(left: 0, top: 0, right: _owner.Size.X, bottom: _owner.Size.Y));
		}
	}

	/// <inheritdoc />
	public override bool CanPaint() => _owner.Brush?.CanPaint() ?? false;

	/// <inheritdoc />
	public override bool RequiresRepaintOnEveryFrame => _owner.Brush?.RequiresRepaintOnEveryFrame ?? false;
}
