using Microsoft.UI.Private.Controls;
using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a container control that provides a RefreshVisualizer and pull-to-refresh functionality for scrollable content.
/// </summary>
public partial class RefreshContainer : ContentControl
{
	private void SetDefaultRefreshInfoProviderAdapter()
	{
		//if (m_refreshInfoProviderAdapter is not null)
		//{
		//	// TODO Uno specific: We currently don't need to switch refresh info provider adapter.
		//	return;
		//}

#if true
		m_refreshInfoProviderAdapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection, null);
#else
		m_refreshInfoProviderAdapter = new NativeRefreshInfoProviderAdapter(this);
#endif
	}

	private void SetDefaultRefreshVisualizer()
	{
#if true
		Visualizer = new RefreshVisualizer();
#else
		Visualizer = new NativeRefreshVisualizer();
#endif
	}
}
