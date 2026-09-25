#nullable enable

using CodeBrix.Platform.UI.Composition.Skia;
using Microsoft.UI.Composition;

namespace CodeBrix.Platform.UI.Skia;

/// <summary>
/// The Skia platform state of a <see cref="TextVisual"/>: a container visual that paints the text of its
/// <see cref="Microsoft.UI.Xaml.Controls.TextBlock"/>. Registered for <see cref="TextVisual"/> in the composition
/// platform's visual factories by <see cref="SkiaPlatformBootstrap"/>.
/// </summary>
/// <remarks>This is the painting code that lived in <c>TextVisual.skia.cs</c>, moved verbatim.</remarks>
internal sealed class TextVisualSkiaPlatform : ContainerVisualSkiaPlatform
{
	private readonly TextVisual _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The text visual.</param>
	internal TextVisualSkiaPlatform(TextVisual owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	public override bool CanPaint() => true;

	internal override void Paint(in PaintingSession session)
	{
		if (_owner.TryGetOwner(out var owner))
		{
			TextSkiaPlatform.Draw(owner, in session);
		}
	}
}
