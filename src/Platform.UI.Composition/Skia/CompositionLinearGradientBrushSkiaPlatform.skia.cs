#nullable enable

using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionBrushPlatform"/> for a
/// <see cref="CompositionLinearGradientBrush"/>.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionLinearGradientBrush.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionLinearGradientBrushSkiaPlatform : CompositionGradientBrushSkiaPlatform
{
	private readonly CompositionLinearGradientBrush _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionLinearGradientBrushSkiaPlatform(CompositionLinearGradientBrush owner) : base(owner)
	{
		_owner = owner;
	}

	private protected override (SKShader? shader, SKColor color) GetPaintingParameters(SKRect bounds)
	{
		var startPoint = _owner.StartPoint.ToSKPoint();
		var endPoint = _owner.EndPoint.ToSKPoint();
		var transform = CreateTransformMatrix(bounds);

		// Transform the points into absolute coordinates.
		if (_owner.MappingMode == CompositionMappingMode.Relative)
		{
			// If mapping is relative to bounding box, multiply points by bounds.
			startPoint.X *= (float)bounds.Width;
			startPoint.Y *= (float)bounds.Height;

			endPoint.X *= (float)bounds.Width;
			endPoint.Y *= (float)bounds.Height;
		}

		// Translate gradient points by bounds offset.
		startPoint.X += bounds.Left;
		startPoint.Y += bounds.Top;

		endPoint.X += bounds.Left;
		endPoint.Y += bounds.Top;

		// Create linear gradient shader.
		var shader = SKShader.CreateLinearGradient(
			startPoint, endPoint,
			Colors, ColorPositions,
			TileMode, transform);

		return (shader, SKColors.Black);
	}
}
