using CodeBrix.Platform.ApplicationModel.DataTransfer;
using CodeBrix.Platform.Extensions.ApplicationModel.Core;
using CodeBrix.Platform.Extensions.ApplicationModel.DataTransfer;
using CodeBrix.Platform.Extensions.Networking.Connectivity;
using CodeBrix.Platform.Extensions.Storage.Pickers;
using CodeBrix.Platform.Extensions.System;
using CodeBrix.Platform.Extensions.System.Profile;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Helpers.Theming;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions.Helpers.Theming;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Xaml.Controls.Extensions;
using CodeBrix.Platform.UI.XamlHost.Skia.Wpf;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.Storage.Pickers;
using Windows.System.Profile.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Runtime.Skia.Extensions.System;
using CodeBrix.Platform.UI.Runtime.Skia.Wpf.Input;
using Microsoft.Web.WebView2.Core;
using CodeBrix.Platform.Graphics;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions; //Was previously: Uno.UI.Runtime.Skia.Wpf.Extensions

internal static class WpfExtensionsRegistrar
{
	private static bool _registered;

	internal static void Register()
	{
		if (_registered)
		{
			return;
		}

		ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), o => new NativeWindowFactoryExtension());
		ApiExtensibility.Register(typeof(CodeBrix.Platform.ApplicationModel.Core.ICoreApplicationExtension), o => new CoreApplicationExtension(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixKeyboardInputSource), o => new WpfKeyboardInputSource(o));
		ApiExtensibility.Register<IXamlRootHost>(typeof(Windows.UI.Core.ICodeBrixCorePointerInputSource), o => new WpfCorePointerInputSource(o));
		ApiExtensibility.Register<ContentPresenter>(typeof(ContentPresenter.INativeElementHostingExtension), o => new WpfNativeElementHostingExtension(o));
		ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), o => new WpfApplicationViewExtension(o));
		ApiExtensibility.Register(typeof(ISystemThemeHelperExtension), o => new WpfSystemThemeHelperExtension(o));
		ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new WpfDisplayInformationExtension(o));
		ApiExtensibility.Register<DragDropManager>(typeof(Windows.ApplicationModel.DataTransfer.DragDrop.Core.IDragDropExtension), o => new WpfDragDropExtension(o));
		ApiExtensibility.Register(typeof(IFileOpenPickerExtension), o => new FileOpenPickerExtension(o));
		ApiExtensibility.Register<FolderPicker>(typeof(IFolderPickerExtension), o => new FolderPickerExtension(o));
		ApiExtensibility.Register(typeof(IFileSavePickerExtension), o => new FileSavePickerExtension(o));
		ApiExtensibility.Register(typeof(IConnectionProfileExtension), o => new WindowsConnectionProfileExtension(o));
		ApiExtensibility.Register<TextBoxView>(typeof(IOverlayTextBoxViewExtension), o => new TextBoxViewExtension(o));
		ApiExtensibility.Register(typeof(ILauncherExtension), o => new WindowsLauncherExtension(o));
		ApiExtensibility.Register(typeof(IClipboardExtension), o => new ClipboardExtensions(o));
		ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => new AnalyticsInfoExtension());
		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new WpfNativeWebViewProvider(o));
		ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new WpfNativeOpenGLWrapper(xamlRoot));

		_registered = true;
	}
}
