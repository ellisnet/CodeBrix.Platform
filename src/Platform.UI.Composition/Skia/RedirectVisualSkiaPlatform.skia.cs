#nullable enable

using Microsoft.UI.Composition;
using SkiaSharp;


namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.IVisualPlatform"/> for a <see cref="RedirectVisual"/>: it renders
/// the source visual, as a root, in place of its own content.
/// </summary>
/// <remarks>This is the rendering code that lived in <c>RedirectVisual.skia.cs</c>, moved verbatim.</remarks>
internal class RedirectVisualSkiaPlatform : ContainerVisualSkiaPlatform
{
	private readonly RedirectVisual _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The visual this object renders.</param>
	internal RedirectVisualSkiaPlatform(RedirectVisual owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		base.Paint(in session);

		if (_owner.Source is { } source && session.Canvas is { } canvas)
		{
			Of(source).RenderRootVisual(canvas, null);
		}
	}

	/// <inheritdoc />
	public override bool CanPaint() => _owner.Source?.CanPaint() ?? false;

	/// <inheritdoc />
	public override bool RequiresRepaintOnEveryFrame => true;
}
