#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class Image
{
#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
	private UIElement? _svgCanvas;
#endif
}
