#nullable enable

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.Platform.UI.Xaml.Controls; //Was previously: Uno.UI.Xaml.Controls

internal partial interface INativeWebView
{
	string DocumentTitle { get; }

	void GoBack();

	void GoForward();

	void Stop();

	void Reload();

	void ProcessNavigation(Uri uri);

	void ProcessNavigation(string html);

	void ProcessNavigation(HttpRequestMessage httpRequestMessage);

	Task<string?> ExecuteScriptAsync(string script, CancellationToken token);

	Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token);

	void SetScrollingEnabled(bool isScrollingEnabled);

	/// <summary>
	/// Applies the value of Microsoft.Web.WebView2.Core.CoreWebView2Settings.UserAgent.
	/// Called when the native web view is created (if a custom value was already set) and
	/// again whenever the value changes. An empty string restores the native engine's
	/// default User-Agent. Implementations whose native web view cannot change its
	/// User-Agent should log a warning and otherwise no-op.
	/// </summary>
	void SetUserAgent(string userAgent);
}
