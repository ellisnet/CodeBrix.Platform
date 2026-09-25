#if !__NETSTD_REFERENCE__
#nullable enable

using Windows.Graphics.Effects;

namespace Microsoft.UI.Composition;

public partial class CompositionEffectBrush : CompositionBrush
{
	private bool _hasBackdropBrushInput;

	/// <summary>
	/// Gets the effect graph this brush renders.
	/// </summary>
	internal IGraphicsEffect Effect => _effect;

	/// <summary>
	/// Gets whether the effect graph samples a <see cref="CompositionBackdropBrush"/> (what is already drawn behind
	/// the brush). The platform sets it when it builds the effect; a change invalidates whatever the brush paints.
	/// </summary>
	internal bool HasBackdropBrushInput
	{
		get => _hasBackdropBrushInput;
		set => SetProperty(ref _hasBackdropBrushInput, value);
	}

	internal bool UseBlurPadding { get; set; }
}
#endif
