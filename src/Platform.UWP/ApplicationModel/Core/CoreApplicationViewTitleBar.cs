#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Windows.ApplicationModel.Core
{
#if !__SKIA__
	[global::CodeBrix.Platform.NotImplemented]
#endif
	public partial class CoreApplicationViewTitleBar
	{
#pragma warning disable 67
		internal event Action ExtendViewIntoTitleBarChanged;
#pragma warning restore 67

#if !__SKIA__
		[global::CodeBrix.Platform.NotImplemented]
		public bool ExtendViewIntoTitleBar
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "bool CoreApplicationViewTitleBar.ExtendViewIntoTitleBar");
				return false;
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "bool CoreApplicationViewTitleBar.ExtendViewIntoTitleBar");
			}
		}
#endif

		[global::CodeBrix.Platform.NotImplemented]
		public double Height
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "bool CoreApplicationViewTitleBar.Height");
				return 0;
			}
		}

		[global::CodeBrix.Platform.NotImplemented]
		public bool IsVisible
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "bool CoreApplicationViewTitleBar.IsVisible");
				return false;
			}
		}

		[global::CodeBrix.Platform.NotImplemented]
		public double SystemOverlayLeftInset
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "bool CoreApplicationViewTitleBar.SystemOverlayLeftInset");
				return 0;
			}
		}

		[global::CodeBrix.Platform.NotImplemented]
		public double SystemOverlayRightInset
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "bool CoreApplicationViewTitleBar.SystemOverlayRightInset");
				return 0;
			}
		}

		[global::CodeBrix.Platform.NotImplemented]
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Core.CoreApplicationViewTitleBar, object> IsVisibleChanged
		{
			[global::CodeBrix.Platform.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.IsVisibleChanged");
			}
			[global::CodeBrix.Platform.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.IsVisibleChanged");
			}
		}

		[global::CodeBrix.Platform.NotImplemented]
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Core.CoreApplicationViewTitleBar, object> LayoutMetricsChanged
		{
			[global::CodeBrix.Platform.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.LayoutMetricsChanged");
			}
			[global::CodeBrix.Platform.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Core.CoreApplicationViewTitleBar", "event TypedEventHandler<CoreApplicationViewTitleBar, object> CoreApplicationViewTitleBar.LayoutMetricsChanged");
			}
		}
	}
}
