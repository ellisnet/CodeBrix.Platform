#nullable enable

namespace CodeBrix.Platform.UI.Xaml.Controls;

/// <summary>
/// The optional host-to-page message channel of a native web view, behind
/// Microsoft.Web.WebView2.Core.CoreWebView2.PostWebMessageAsString and PostWebMessageAsJson. A native web view MAY
/// implement it beside <see cref="INativeWebView"/>; one that does not keeps those two calls reporting that they are not
/// implemented. Additive on purpose: <see cref="INativeWebView"/> itself gains no member, so an implementation built
/// against an older CodeBrix.Platform keeps compiling.
/// </summary>
/// <remarks>
/// The page side follows WebView2: window.chrome.webview.addEventListener('message', listener) /
/// removeEventListener('message', listener) and the onmessage property; each listener is called with an event whose
/// data is the string (<c>isJson</c> false) or the parsed JSON value (<c>isJson</c> true). A message posted before the
/// page's message channel exists (the native view's document-start script has not run) is dropped.
/// </remarks>
internal interface INativeWebViewMessaging
{
	/// <summary>Delivers a message to the page's message listeners.</summary>
	/// <param name="payload">The message: the string itself, or JSON text when <paramref name="isJson"/> is true.</param>
	/// <param name="isJson">True for PostWebMessageAsJson (the page receives the parsed value), false for
	/// PostWebMessageAsString (the page receives the string).</param>
	/// <exception cref="System.ArgumentException"><paramref name="isJson"/> is true and <paramref name="payload"/> is not
	/// valid JSON.</exception>
	void PostWebMessage(string payload, bool isJson);
}
