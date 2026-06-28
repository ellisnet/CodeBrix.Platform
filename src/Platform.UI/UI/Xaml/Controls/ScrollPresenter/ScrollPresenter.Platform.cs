using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollPresenter : IBorderInfoProvider
{
#if !CODEBRIX_HAS_BORDER_VISUAL
	private BorderLayerRenderer _borderRenderer;
#endif

#if !CODEBRIX_HAS_BORDER_VISUAL
	partial void InitializePartial()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}
#endif

#if CODEBRIX_HAS_BORDER_VISUAL
	private protected override ContainerVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();
#endif

	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

	Brush IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => default;

	CornerRadius IBorderInfoProvider.CornerRadius => default;

#if CODEBRIX_HAS_BORDER_VISUAL
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");
#endif
}
