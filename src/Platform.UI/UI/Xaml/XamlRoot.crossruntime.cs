#nullable enable
using System;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public sealed partial class XamlRoot
{
	internal void InvalidateMeasure()
	{
		VisualTree.RootElement.InvalidateMeasure();
#if CODEBRIX_HAS_ENHANCED_LIFECYCLE
		CoreServices.RequestAdditionalFrame();
#endif
	}

	internal void InvalidateArrange()
	{
		VisualTree.RootElement.InvalidateArrange();
#if CODEBRIX_HAS_ENHANCED_LIFECYCLE
		CoreServices.RequestAdditionalFrame();
#endif
	}

	/// <summary>
	/// This is used to adjust the sizing of managed vs. native elements on GTK, as it does not have built-in support for fractional scaling
	/// which is available on Windows. We can still emulate this by up-scaling native GTK controls by the ratio between the actual scale 
	/// and the emulated scale.
	/// </summary>
	internal double FractionalScaleAdjustment => RasterizationScale / Math.Truncate(RasterizationScale);
}
