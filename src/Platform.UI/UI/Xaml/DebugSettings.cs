using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml
{
	public sealed partial class DebugSettings
	{
#if HAS_CODEBRIX_WINUI
		public LayoutCycleTracingLevel LayoutCycleTracingLevel { get; set; }
#endif

#if !CODEBRIX_REFERENCE_API
		[CodeBrix.Platform.NotImplemented]
#endif
		public bool EnableFrameRateCounter { get; set; }

		[CodeBrix.Platform.NotImplemented]
		public bool EnableRedrawRegions { get; set; }

		[CodeBrix.Platform.NotImplemented]
		public bool IsBindingTracingEnabled { get; set; }

		[CodeBrix.Platform.NotImplemented]
		public bool IsOverdrawHeatMapEnabled { get; set; }

		[CodeBrix.Platform.NotImplemented]
		public bool IsTextPerformanceVisualizationEnabled { get; set; }

#pragma warning disable CS0067 // The event 'DebugSettings.BindingFailed' is never used
		public event BindingFailedEventHandler BindingFailed;
#pragma warning restore CS0067 // The event 'DebugSettings.BindingFailed' is never used
	}
}
