#if !__NETSTD_REFERENCE__
#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using CodeBrix.Platform.UI.Composition;

namespace Microsoft.UI.Composition;

/// <summary>
/// A Visual that has a border and a background.
/// </summary>
/// <remarks>
/// The border and background geometry, the corner clips and the painting are built by the visual's platform state
/// (on Skia, <c>CodeBrix.Platform.UI.Composition.Skia.BorderVisualSkiaPlatform</c>) from the state kept here.
/// </remarks>
internal class BorderVisual(Compositor compositor) : ContainerVisual(compositor)
{
	// state set from outside and used inside the class
	internal CornerRadius _cornerRadius;
	internal Thickness _borderThickness;
	internal bool _useInnerBorderBoundsAsAreaForBackground = true;
	internal CompositionBrush? _backgroundBrush;
	internal CompositionBrush? _borderBrush;
	// State set and used inside the class
	internal bool _borderPathValid;
	internal bool _backgroundPathValid;
	internal CompositionSpriteShape? _backgroundShape; // Never null after _backgroundBrush is set
	internal CompositionSpriteShape? _borderShape; // Never null after _borderBrush is set
	internal CompositionClip? _backgroundClip;
	// state set here but affects children
	internal RectangleClip? _childClipCausedByCornerRadius;

	// We do this instead of a direct SetProperty call so that SetProperty automatically gets an accurate propertyName
	// we need the SetProperty calls to get notified on brush updates.
	// (<Border|Background>Brush internals change -> <Border|Background>Shape is notified through FillBrush -> render invalidation)
	private CompositionSpriteShape? BackgroundShape { set => SetProperty(ref _backgroundShape, value); }
	private CompositionSpriteShape? BorderShape { set => SetProperty(ref _borderShape, value); }

	internal bool IsMyBackgroundShape(CompositionSpriteShape shape) => _backgroundShape == shape;

	public CornerRadius CornerRadius
	{
		private get => _cornerRadius;
		set => SetObjectProperty(ref _cornerRadius, value);
	}

	public Thickness BorderThickness
	{
		private get => _borderThickness;
		set => SetObjectProperty(ref _borderThickness, value);
	}

	public bool UseInnerBorderBoundsAsAreaForBackground
	{
		private get => _useInnerBorderBoundsAsAreaForBackground;
		set => SetProperty(ref _useInnerBorderBoundsAsAreaForBackground, value);
	}

	public CompositionBrush? BackgroundBrush
	{
		private get => _backgroundBrush;
		set => SetProperty(ref _backgroundBrush, value);
	}

	public CompositionBrush? BorderBrush
	{
		private get => _borderBrush;
		set => SetProperty(ref _borderBrush, value);
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		// Call base implementation - Visual calls Compositor.InvalidateRender().
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

		switch (propertyName)
		{
			case nameof(CornerRadius) or nameof(BorderThickness) or nameof(UseInnerBorderBoundsAsAreaForBackground) or nameof(Size):
				_borderPathValid = false;
				_backgroundPathValid = false;
				break;
			// BackgroundShape and BorderShape are NOT added to this.Shapes, which both makes it easier
			// to reason about (no external tampering) and is also closer to what WinUI does.
			case nameof(BorderBrush):
				_borderPathValid = false;
				if (BorderBrush is not null && _borderShape is null)
				{
					var borderShape = Compositor.CreateSpriteShape();
					borderShape.Geometry = Compositor.CreatePathGeometry();
#if DEBUG
					borderShape.Comment = "#borderShape";
#endif
					borderShape.FillBrush = BorderBrush;
					BorderShape = borderShape;
				}
				else if (_borderShape is { })
				{
					_borderShape.FillBrush = BorderBrush;
				}
				break;
			case nameof(BackgroundBrush):
				_backgroundPathValid = false;
				if (BackgroundBrush is not null && _backgroundShape is null)
				{
					var backgroundShape = Compositor.CreateSpriteShape();

					backgroundShape.Geometry = Compositor.CreatePathGeometry();
#if DEBUG
					backgroundShape.Comment = "#backgroundShape";
#endif
					backgroundShape.FillBrush = BackgroundBrush;

					BackgroundShape = backgroundShape;
				}
				else if (_backgroundShape is { })
				{
					_backgroundShape.FillBrush = BackgroundBrush;
				}
				break;
		}
	}

	internal override bool HitTest(Point point) => Platform.HitTest(point);
}
#endif
