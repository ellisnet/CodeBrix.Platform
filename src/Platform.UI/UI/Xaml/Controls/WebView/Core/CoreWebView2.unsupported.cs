#if !__SKIA__
#nullable enable

using CodeBrix.Platform.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal INativeWebView? GetNativeWebViewFromTemplate() => null;
}
#endif
