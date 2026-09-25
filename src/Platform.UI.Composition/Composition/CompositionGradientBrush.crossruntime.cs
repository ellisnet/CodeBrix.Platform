#if !__NETSTD_REFERENCE__
#nullable enable

namespace Microsoft.UI.Composition
{
	public partial class CompositionGradientBrush
	{
		partial void OnColorStopsChanged(CompositionColorGradientStopCollection colorStops) => Platform.OnPropertyChanged(nameof(ColorStops));

		partial void OnExtendModeChanged(CompositionGradientExtendMode extendMode) => Platform.OnPropertyChanged(nameof(ExtendMode));
	}
}
#endif
