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

/// <summary>
/// Supplies the Linux heads with a WPE WebKit web view for a <see cref="CoreWebView2"/>.
/// </summary>
/// <remarks>
/// The framework asks for this through its extensibility registry, which the assembly-level
/// <c>ApiExtension</c> attribute above registers for Linux; an application never constructs one.
/// </remarks>
/// <param name="coreWebView2">The core web view the created native view belongs to.</param>
public class WpeNativeWebViewProvider(CoreWebView2 coreWebView2) : INativeWebViewProvider
{
	INativeWebView INativeWebViewProvider.CreateNativeWebView(ContentPresenter contentPresenter) => new WpeNativeWebView(coreWebView2, contentPresenter);
}
