using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Extensions;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.DataBinding;
using CodeBrix.Platform.UI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase
	{
		partial void InitializePopupPanelPartial()
		{
			_popup.PopupPanel = new FlyoutBasePopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent,
			};
		}

		internal PopupPanel GetPopupPanel() => _popup.PopupPanel;
	}
}
