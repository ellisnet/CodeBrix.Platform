#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Composition.Skia;

/// <summary>
/// The Skia implementation of <see cref="Contracts.ICompositionBrushPlatform"/> shared by the gradient brushes: it
/// keeps the color stops as Skia arrays and paints the shader that the per-type subclass creates.
/// </summary>
/// <remarks>This is the painting code that lived in <c>CompositionGradientBrush.skia.cs</c>, moved verbatim.</remarks>
internal class CompositionGradientBrushSkiaPlatform : CompositionBrushSkiaPlatform
{
	private static readonly SKPaint _tempPaint = new();
	private bool _isColorStopsValid;

	private SKColor[]? _colors;
	private float[]? _colorPositions;
	private SKShaderTileMode _tileMode;

	private readonly CompositionGradientBrush _owner;

	/// <summary>
	/// Creates the platform state of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The brush this object paints.</param>
	internal CompositionGradientBrushSkiaPlatform(CompositionGradientBrush owner) : base(owner)
	{
		_owner = owner;
	}

	private protected SKColor[]? Colors => _colors;
	private protected float[]? ColorPositions => _colorPositions;
	private protected SKShaderTileMode TileMode => _tileMode;

	/// <inheritdoc />
	public override bool CanPaint() => true;

	/// <inheritdoc />
	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		if (!_isColorStopsValid)
		{
			UpdateColorStops(_owner.ColorStops);
		}
		var (shader, color) = GetPaintingParameters(bounds);
		_tempPaint.Reset();
		_tempPaint.IsAntialias = true;
		_tempPaint.Shader = shader;
		_tempPaint.Color = color;
		_tempPaint.ColorFilter = opacity.ToColorFilter();
		canvas.DrawRect(bounds, _tempPaint);
	}

	private protected virtual (SKShader? shader, SKColor color) GetPaintingParameters(SKRect bounds) => (null, SKColors.Transparent);

	private protected SKMatrix CreateTransformMatrix(SKRect bounds)
	{
		var CenterPoint = _owner.CenterPoint;
		var Scale = _owner.Scale;
		var RotationAngle = _owner.RotationAngle;
		var Offset = _owner.Offset;
		var TransformMatrix = _owner.TransformMatrix;
		var RelativeTransformMatrix = _owner.RelativeTransformMatrix;

		var transform = SKMatrix.Identity;

		// Translate to origin
		if (CenterPoint != Vector2.Zero)
		{
			transform = SKMatrix.CreateTranslation(-CenterPoint.X, -CenterPoint.Y);
		}

		// Scaling
		if (Scale != Vector2.One)
		{
			transform = transform.PostConcat(SKMatrix.CreateScale(Scale.X, Scale.Y));
		}

		// Rotating
		if (RotationAngle != 0)
		{
			transform = transform.PostConcat(SKMatrix.CreateRotation(RotationAngle));
		}

		// Translating
		if (Offset != Vector2.Zero)
		{
			transform = transform.PostConcat(SKMatrix.CreateTranslation(Offset.X, Offset.Y));
		}

		// Translate back
		if (CenterPoint != Vector2.Zero)
		{
			transform = transform.PostConcat(SKMatrix.CreateTranslation(CenterPoint.X, CenterPoint.Y));
		}

		if (!TransformMatrix.IsIdentity)
		{
			transform = transform.PostConcat(TransformMatrix.ToSKMatrix());
		}

		var relativeTransform = RelativeTransformMatrix.IsIdentity ? SKMatrix.Identity : RelativeTransformMatrix.ToSKMatrix();
		if (!relativeTransform.IsIdentity)
		{
			relativeTransform.TransX *= bounds.Width;
			relativeTransform.TransY *= bounds.Height;

			transform = transform.PostConcat(relativeTransform);
		}

		return transform;
	}

	private void UpdateColorStops(CompositionColorGradientStopCollection colorStops)
	{
		var stopCount = colorStops.Count;
		var colors = _colors;
		var colorPositions = _colorPositions;

		if (colors == null || colors.Length != stopCount)
		{
			colors = new SKColor[stopCount];
			colorPositions = new float[stopCount];
		}

		for (int i = 0; i < colorStops.Count; i++)
		{
			var gradientStop = colorStops[i];

			colors[i] = gradientStop.Color.ToSKColor();
			colorPositions![i] = gradientStop.Offset;
		}

		_colors = colors;
		_colorPositions = colorPositions;
		_isColorStopsValid = true;
	}

	/// <inheritdoc />
	public override void OnPropertyChanged(string? propertyName)
	{
		switch (propertyName)
		{
			case nameof(CompositionGradientBrush.ColorStops):
				_isColorStopsValid = false;
				break;
			case nameof(CompositionGradientBrush.ExtendMode):
				OnExtendModeChanged(_owner.ExtendMode);
				break;
		}
	}

	private void OnExtendModeChanged(CompositionGradientExtendMode extendMode)
	{
		SKShaderTileMode tileMode;
		switch (extendMode)
		{
			default:
			case CompositionGradientExtendMode.Clamp:
				tileMode = SKShaderTileMode.Clamp;
				break;
			case CompositionGradientExtendMode.Mirror:
				tileMode = SKShaderTileMode.Mirror;
				break;
			case CompositionGradientExtendMode.Wrap:
				tileMode = SKShaderTileMode.Repeat;
				break;
		}

		_tileMode = tileMode;
	}
}
