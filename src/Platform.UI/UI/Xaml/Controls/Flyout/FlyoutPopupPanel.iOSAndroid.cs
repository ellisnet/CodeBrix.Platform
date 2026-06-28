#if false
using System;
using System.Collections.Generic;
using System.Text;
#if false
using View = UIKit.UIView;
#else
using Android.Views;
#endif
using CodeBrix.Platform.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	partial class FlyoutBasePopupPanel
	{
		protected override View NativeAnchor => _flyout.NativeTarget;
	}
}

#endif
