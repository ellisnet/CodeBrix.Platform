using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI.Xaml.Controls;

[assembly: ApiExtension(
	typeof(INativeWebViewProvider),
	typeof(CodeBrix.Platform.UI.WebView.Skia.Linux.WpeNativeWebViewProvider),
	ownerType: typeof(CoreWebView2),
	operatingSystemCondition: "linux")]

namespace CodeBrix.Platform.UI.WebView.Skia.Linux;

public class WpeNativeWebViewProvider(CoreWebView2 coreWebView2) : INativeWebViewProvider
{
	INativeWebView INativeWebViewProvider.CreateNativeWebView(ContentPresenter contentPresenter) => new WpeNativeWebView(coreWebView2, contentPresenter);
}
