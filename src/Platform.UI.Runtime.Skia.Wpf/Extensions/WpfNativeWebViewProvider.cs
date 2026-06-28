extern alias WpfWebView;

using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using CodeBrix.Platform.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.Extensions; //Was previously: Uno.UI.Runtime.Skia.Wpf.Extensions

internal sealed class WpfNativeWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public WpfNativeWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var content = contentPresenter.Content as WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2;
		if (content is null)
		{
			content = new WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2();
			contentPresenter.Content = content;
		}

		return new WpfNativeWebView(content, _owner);
	}
}
