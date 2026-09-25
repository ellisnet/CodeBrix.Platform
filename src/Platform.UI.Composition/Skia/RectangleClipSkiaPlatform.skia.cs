#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.Extensions;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionClipPlatform"/> for a <see cref="RectangleClip"/>: a
/// rectangle when no corner is rounded, a rounded rectangle otherwise.
/// </summary>
/// <remarks>This is the clipping code that lived in <c>RectangleClip.skia.cs</c>, moved verbatim.</remarks>
internal class RectangleClipSkiaPlatform : CompositionClipSkiaPlatform
{
	private SKRoundRect? _skRoundRect;
	private static readonly SKPathBuilder _spareClipPath = new();

	private readonly RectangleClip _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The clip this object applies.</param>
	internal RectangleClipSkiaPlatform(RectangleClip owner) : base(owner)
	{
		_owner = owner;
	}

	// The path returned here is reused, do not cache
	internal override SKPath GetClipPath(Visual visual)
	{
		var path = _spareClipPath;
		path.AddRoundRect(GetClipRoundedRect(visual));

		return path.Detach();
	}

	private protected override SKRect? GetClipRect(Visual visual)
	{
		var _topLeftRadius = _owner.TopLeftRadius;
		var _topRightRadius = _owner.TopRightRadius;
		var _bottomLeftRadius = _owner.BottomLeftRadius;
		var _bottomRightRadius = _owner.BottomRightRadius;

		if (_topLeftRadius.X is 0 && _topLeftRadius.Y is 0 &&
			_topRightRadius.X is 0 && _topRightRadius.Y is 0 &&
			_bottomLeftRadius.X is 0 && _bottomLeftRadius.Y is 0 &&
			_bottomRightRadius.X is 0 && _bottomRightRadius.Y is 0)
		{
			return _owner.GetBounds(visual)?.ToSKRect();
		}
		else
		{
			return null;
		}
	}

	private protected override SKRoundRect? GetClipRoundedRect(Visual visual)
	{
		if (_owner.GetBounds(visual) is { } bounds)
		{
			_skRoundRect ??= new SKRoundRect();

			var _topLeftRadius = _owner.TopLeftRadius;
			var _topRightRadius = _owner.TopRightRadius;
			var _bottomLeftRadius = _owner.BottomLeftRadius;
			var _bottomRightRadius = _owner.BottomRightRadius;

			Span<SKPoint> radii = stackalloc SKPoint[]
			{
				new SKPoint(_topLeftRadius.X, _topLeftRadius.Y),
				new SKPoint(_topRightRadius.X, _topRightRadius.Y),
				new SKPoint(_bottomRightRadius.X, _bottomRightRadius.Y),
				new SKPoint(_bottomLeftRadius.X, _bottomLeftRadius.Y),
			};

			_skRoundRect.SetRectRadii(bounds.ToSKRect(), radii);

			return _skRoundRect;
		}
		else
		{
			return null;
		}
	}
}
