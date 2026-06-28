#if CODEBRIX_REFERENCE_API
using CodeBrix.Platform.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeBrix.Platform.Extensions;
using System.Collections.Specialized;
using CodeBrix.Platform.Extensions.Specialized;
using System.Diagnostics;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.Extensions.Disposables;
using Microsoft.UI.Xaml.Data;
using CodeBrix.Platform.UI.DataBinding;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	partial class Selector
	{
		private protected override bool ShouldItemsControlManageChildren => !(ItemsPanelRoot is IVirtualizingPanel);

		partial void RefreshPartial()
		{
			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().Refresh();

				InvalidateMeasure();
			}
		}
	}
}
#endif
