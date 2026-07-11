//CodeBrix warning-cleanup 2026-07-10: explicit static constructor retained deliberately (native/platform init, ordered initialization, or precise before-first-use timing); CA1810 suppressed rather than converting to field initializers.
#pragma warning disable CA1810
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class SwipeTestHooks
	{
		static SwipeTestHooks()
		{
			s_testHooks = new SwipeTestHooks();
		}
	}
}
