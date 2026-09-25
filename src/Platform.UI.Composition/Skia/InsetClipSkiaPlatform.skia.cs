using Microsoft.UI.Composition;
using SkiaSharp;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionClipPlatform"/> for an <see cref="InsetClip"/>.
/// </summary>
/// <remarks>This is the clipping code that lived in <c>InsetClip.skia.cs</c>, moved verbatim.</remarks>
internal class InsetClipSkiaPlatform : CompositionClipSkiaPlatform
{
	private (Rect? bounds, SKPath path)? _clipPath;

	private readonly InsetClip _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The clip this object applies.</param>
	internal InsetClipSkiaPlatform(InsetClip owner) : base(owner)
	{
		_owner = owner;
	}

	internal override SKPath GetClipPath(Visual visual)
	{
		if (_owner.GetBounds(visual) is not { } bounds)
		{
			return null;
		}
		if (_clipPath is null || _clipPath.Value.bounds != bounds)
		{
			using var pathBuilder = new SKPathBuilder();
			var rect = bounds.ToSKRect();
			pathBuilder.AddRect(rect);
			_clipPath = (bounds, pathBuilder.Snapshot());
		}
		return _clipPath.Value.path;
	}

	private protected override SKRect? GetClipRect(Visual visual)
	{
		return _owner.GetBounds(visual)?.ToSKRect();
	}
}
