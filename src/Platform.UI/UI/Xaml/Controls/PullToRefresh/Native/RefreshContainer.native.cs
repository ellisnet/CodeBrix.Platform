#if false
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class RefreshContainer : ContentControl
{
	private void OnNativeRefreshingChanged()
	{
		if (Visualizer is { } visualizer && visualizer.State != RefreshVisualizerState.Refreshing)
		{
			visualizer.RequestRefresh();
		}
	}
}
#endif
