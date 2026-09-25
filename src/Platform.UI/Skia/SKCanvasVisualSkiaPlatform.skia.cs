#nullable enable

using CodeBrix.Platform.UI.Composition.Skia;
using CodeBrix.Platform.UI.Graphics;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia platform state of an <see cref="SKCanvasVisual"/>: a container visual that paints its render callback's
/// content. Registered for <see cref="SKCanvasVisual"/> in the composition platform's visual factories by
/// <see cref="SkiaPlatformBootstrap"/>.
/// </summary>
/// <remarks>This replaces the <c>Paint</c>/<c>CanPaint</c> overrides that <c>SKCanvasVisual.skia.cs</c> had on
/// <see cref="Visual"/>; the drawing code itself stays in <see cref="SKCanvasVisual.PaintContent"/>.</remarks>
internal sealed class SKCanvasVisualSkiaPlatform : ContainerVisualSkiaPlatform
{
	private readonly SKCanvasVisual _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The canvas visual.</param>
	internal SKCanvasVisualSkiaPlatform(SKCanvasVisual owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	public override bool CanPaint() => true;

	internal override void Paint(in PaintingSession session) => _owner.PaintContent(in session);
}
