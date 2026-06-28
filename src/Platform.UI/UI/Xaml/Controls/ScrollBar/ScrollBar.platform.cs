using System;
using System.Linq;
using CodeBrix.Platform.Extensions.Disposables;
using static CodeBrix.Platform.UI.FeatureConfiguration;

#if !HAS_CODEBRIX_WINUI
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollBar
{
	private static void DetachEvents(object snd, RoutedEventArgs args) // OnUnloaded
		=> (snd as ScrollBar)?.DetachEvents();

#if !CODEBRIX_HAS_ENHANCED_LIFECYCLE
	private static void OnLayoutUpdated(
		object pSender,
		object pArgs)
	{
		(pSender as ScrollBar)?.UpdateTrackLayout();
	}
#endif
}
