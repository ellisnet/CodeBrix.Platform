using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS; //Was previously: Uno.UI.Runtime.Skia.MacOS

internal class MacOSNativeWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public static unsafe void Register()
	{
		NativeCodeBrix.codebrix_set_execute_callback(&MacOSNativeWebView.ExecuteScriptCallback);
		NativeCodeBrix.codebrix_set_invoke_callback(&MacOSNativeWebView.InvokeScriptCallback);
		NativeCodeBrix.codebrix_set_webview_unsupported_scheme_identified_callback(&MacOSNativeWebView.OnUnsupportedUriSchemeIdentified);
		NativeCodeBrix.codebrix_set_webview_navigation_callbacks(
			&MacOSNativeWebView.NavigationStartingCallback,
			&MacOSNativeWebView.NavigationFinishingCallback,
			&MacOSNativeWebView.DidChangeValue,
			&MacOSNativeWebView.DidReceiveScriptMessage,
			&MacOSNativeWebView.NavigationFailingCallback);

		NativeCodeBrix.codebrix_set_webview_new_window_requested_callback(&MacOSNativeWebView.NewWindowRequestedCallback);

		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new MacOSNativeWebViewProvider(o));
	}

	private MacOSNativeWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var window = contentPresenter.XamlRoot?.HostWindow?.NativeWindow as MacOSWindowNative;
		if (contentPresenter.Content is not MacOSNativeWebView content)
		{
			content = new MacOSNativeWebView(window!, _owner);
			contentPresenter.Content = content;
		}
		return content;
	}
}
