#nullable enable

using Windows.UI;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionBrushPlatform"/> for a <see cref="CompositionColorBrush"/>.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionColorBrush.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionColorBrushSkiaPlatform : CompositionBrushSkiaPlatform
{
	// We don't call SKPaint.Reset() after usage, so make sure
	// that only SKPaint.Color is being set
	private static readonly SKPaint _tempPaint = new();

	private readonly CompositionColorBrush _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionColorBrushSkiaPlatform(CompositionColorBrush owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		_tempPaint.Color = _owner.Color.ToSKColor(opacity);
		canvas.DrawRect(bounds, _tempPaint);
	}

	/// <inheritdoc />
	public override bool CanPaint() => _owner.Color != Colors.Transparent;
}
