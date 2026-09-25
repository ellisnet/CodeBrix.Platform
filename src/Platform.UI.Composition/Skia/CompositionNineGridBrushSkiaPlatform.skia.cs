#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;
using CodeBrix.Platform.UI.Composition;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionBrushPlatform"/> for a
/// <see cref="CompositionNineGridBrush"/>: it paints the source brush into a bitmap and draws it as a nine-patch.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionNineGridBrush.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionNineGridBrushSkiaPlatform : CompositionBrushSkiaPlatform
{
	private static readonly SKPaint _tempPaint = new();
	private SKBitmap? _bitmap;
	private SKCanvas? _bitmapCanvas;
	private SKRectI _insetRect;

	private readonly CompositionNineGridBrush _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionNineGridBrushSkiaPlatform(CompositionNineGridBrush owner) : base(owner)
	{
		_owner = owner;
	}

	/// <inheritdoc />
	public override bool RequiresRepaintOnEveryFrame => _owner.Source?.RequiresRepaintOnEveryFrame ?? false;

	/// <inheritdoc />
	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		var owner = _owner;
		if (owner.Source is not { } Source)
		{
			return;
		}

		SKRect sourceBounds;
		if (Source is ISizedBrush sizedBrush && sizedBrush.Size is Vector2 sourceSize)
		{
			sourceBounds = new(0, 0, sourceSize.X, sourceSize.Y);
		}
		else
		{
			sourceBounds = bounds;
		}

		var newSize = new SKSizeI((int)sourceBounds.Width, (int)sourceBounds.Height);
		var info = new SKImageInfo(newSize.Width, newSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
		if (_bitmap is null || _bitmapCanvas is null || _bitmap.Info.Size != newSize)
		{
			_bitmap?.Dispose();
			_bitmapCanvas?.Dispose();
			_bitmap = new SKBitmap(info);
			_bitmapCanvas = new SKCanvas(_bitmap);
		}
		else
		{
			_bitmapCanvas.Clear(SKColors.Transparent);
		}

		Of(Source).Paint(_bitmapCanvas, opacity, sourceBounds);
		_bitmapCanvas.Flush();
		var image = SKImage.FromPixels(info, _bitmap.GetPixels());

		_insetRect.Top = (int)(owner.TopInset * owner.TopInsetScale);
		_insetRect.Bottom = (int)(sourceBounds.Height - (owner.BottomInset * owner.BottomInsetScale));
		_insetRect.Right = (int)(sourceBounds.Width - (owner.RightInset * owner.RightInsetScale));
		_insetRect.Left = (int)(owner.LeftInset * owner.LeftInsetScale);

		_tempPaint.Reset();
		_tempPaint.IsAntialias = true;
		_tempPaint.IsDither = true;
		if (owner.IsCenterHollow)
		{
			canvas.Save();
			canvas.ClipRect(_insetRect, SKClipOperation.Difference, antialias: true);
			canvas.DrawImageNinePatch(image, _insetRect, bounds, _tempPaint);
			canvas.Restore();
		}
		else
		{
			canvas.DrawBitmapNinePatch(_bitmap, _insetRect, bounds, _tempPaint);
		}
	}

	/// <inheritdoc />
	public override bool CanPaint() => _owner.Source?.CanPaint() ?? false;
}
