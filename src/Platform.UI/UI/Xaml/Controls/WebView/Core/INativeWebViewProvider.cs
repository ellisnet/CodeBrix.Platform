using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

public interface INativeWebViewProvider
{
	internal INativeWebView CreateNativeWebView(ContentPresenter contentPresenter);
}
