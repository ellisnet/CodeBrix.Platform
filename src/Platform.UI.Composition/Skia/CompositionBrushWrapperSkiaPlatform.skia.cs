#nullable enable

using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionBrushPlatform"/> for a <see cref="CompositionBrushWrapper"/>:
/// it paints the wrapped brush.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionBrushWrapper.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionBrushWrapperSkiaPlatform : CompositionBrushSkiaPlatform
{
	private readonly CompositionBrushWrapper _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionBrushWrapperSkiaPlatform(CompositionBrushWrapper owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		if (_owner.WrappedBrush is { } wrappedBrush)
		{
			Of(wrappedBrush).Paint(canvas, opacity, bounds);
		}
	}

	/// <inheritdoc />
	public override bool CanPaint() => _owner.WrappedBrush?.CanPaint() ?? false;
}
