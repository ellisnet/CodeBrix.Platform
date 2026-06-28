using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.Extensions.Disposables;
using System.Text;
using System.Windows.Input;
using Foundation;
using CodeBrix.Platform.Client;
using CodeBrix.Platform.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UIKit;
using CodeBrix.Platform.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class MenuFlyout
	{
		private static DependencyProperty CancelTextIosOverrideProperty = ToolkitHelper.GetProperty("CodeBrix.Platform.UI.Toolkit.MenuFlyoutExtensions, CodeBrix.Platform.UI.Toolkit", "CancelTextIosOverride");

		private string LocalizedCancelString => NSBundle.FromIdentifier("com.apple.UIKit")
			.GetLocalizedString("Cancel", null);

		internal protected override void Open()
		{
			if (UseNativePopup)
			{

				if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
				{
					ShowAlert(Target);
				}
#if true
				else if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
				{
					ShowActionSheet(Target);
				}
#endif
			}
			else
			{
				base.Open();
			}
		}

		internal protected override void Close()
		{
			if (UseNativePopup)
			{

				if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
				{
					HideAlert();
				}
#if true
				else if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
				{
					HideActionSheet();
				}
#endif
			}
			else
			{
				base.Close();
			}
		}
	}
}
