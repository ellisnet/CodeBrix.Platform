using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.Platform.UI;
using Windows.Foundation;
using Foundation;
using UIKit;
using Windows.UI.Input;

namespace Windows.UI.ViewManagement;

public partial class InputPane
{
	internal CodeBrix.Platform.UI.Controls.Window Window { get; set; }

	private bool TryShowPlatform() => InputPaneInterop.TryShow();

	private bool TryHidePlatform() => InputPaneInterop.TryShow();

	partial void EnsureFocusedElementInViewPartial()
	{
		if (Visible)
		{
			Window?.MakeFocusedViewVisible(isOpeningKeyboard: false);
		}
		else
		{
			Window?.RestoreFocusedViewVisibility();
		}
	}
}
