#if !__NETSTD_REFERENCE__
namespace Microsoft.UI.Xaml.Controls;

public partial class Canvas
{
	static partial void OnZIndexChangedPartial(UIElement element, int? zindex)
	{
		element.Visual.ZIndex = (int)zindex;
		element._children.ClearCachedReverseSortedList();
	}
}
#endif
