using System;
using System.Text.Json;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux;

/// <summary>
/// The host-to-page message channel of the Linux web view, as pure text (host-free tested): the document-start script
/// that gives window.chrome.webview its WebView2 'message' listeners, and the scripts that deliver
/// CoreWebView2.PostWebMessageAsString / PostWebMessageAsJson to them. The page-side API and the delivery scripts are
/// the same shape as CodeBrix.Android's web view, so a page behaves the same on both.
/// </summary>
internal static class WebViewScripts
{
	/// <summary>
	/// The name of the page-side function (a non-enumerable member of window.chrome.webview) that delivers a host message
	/// to the page's 'message' listeners and returns how many there were.
	/// </summary>
	internal const string DispatchFunctionName = "__codebrixDispatchMessage";

	/// <summary>The value a delivery script evaluates to when the page has no message channel (the document-start script did not run).</summary>
	internal const string NoChannelResult = "-1";

	/// <summary>
	/// The document-start script of the host-to-page channel (it runs after the page-to-host bridge script, which created
	/// window.chrome.webview): window.chrome.webview.addEventListener / removeEventListener('message', listener) and the
	/// onmessage property, as WebView2 offers them. Listeners are kept page-side; each is called with an event whose data
	/// is the message (the string of PostWebMessageAsString, the parsed value of PostWebMessageAsJson). A listener that
	/// throws does not stop the others (its error is reported asynchronously).
	/// </summary>
	internal const string MessageListenerScript =
		"(function(){var v=window.chrome&&window.chrome.webview;if(!v||typeof v." + DispatchFunctionName + "==='function'){return;}"
		+ "var ls=[];"
		+ "v.addEventListener=function(t,f){if(t==='message'&&typeof f==='function'&&ls.indexOf(f)<0){ls.push(f);}};"
		+ "v.removeEventListener=function(t,f){if(t==='message'){var i=ls.indexOf(f);if(i>=0){ls.splice(i,1);}}};"
		+ "if(!('onmessage' in v)){v.onmessage=null;}"
		+ "function call(f,e){try{f.call(v,e);}catch(x){setTimeout(function(){throw x;},0);}}"
		+ "Object.defineProperty(v,'" + DispatchFunctionName + "',{value:function(d){var e={type:'message',data:d,source:v},n=0;"
		+ "if(typeof v.onmessage==='function'){n++;call(v.onmessage,e);}"
		+ "ls.slice().forEach(function(f){n++;call(f,e);});return n;}});})();";

	/// <summary>
	/// The script that delivers PostWebMessageAsString(<paramref name="message"/>) to the page: event.data is the string.
	/// The text reaches the page only as a JSON string literal (quotes, line breaks, U+2028/U+2029, "&lt;/script&gt;" and
	/// every non-ASCII character escaped by the default JSON encoder), never as script text. Evaluates to the number of
	/// listeners that received it, or <see cref="NoChannelResult"/> when the page has no channel.
	/// </summary>
	/// <param name="message">The message (null is sent as an empty string, as WebView2 marshals it).</param>
	/// <returns>The script.</returns>
	internal static string StringMessageDelivery(string? message) => Delivery(JsonSerializer.Serialize(message ?? string.Empty));

	/// <summary>
	/// The script that delivers PostWebMessageAsJson(<paramref name="json"/>) to the page: event.data is the parsed JSON
	/// value. The JSON text is validated here and handed to the page as a JSON string literal that the page parses with
	/// JSON.parse - it is never evaluated as script.
	/// </summary>
	/// <param name="json">The message as JSON text.</param>
	/// <returns>The script.</returns>
	/// <exception cref="ArgumentException"><paramref name="json"/> is null or not valid JSON (WebView2 refuses it too).</exception>
	internal static string JsonMessageDelivery(string? json)
	{
		if (json is null)
		{
			throw new ArgumentException("The message is not valid JSON: it is null.", nameof(json));
		}

		try
		{
			using var document = JsonDocument.Parse(json);
		}
		catch (JsonException exception)
		{
			throw new ArgumentException("The message is not valid JSON: " + exception.Message, nameof(json), exception);
		}

		return Delivery("JSON.parse(" + JsonSerializer.Serialize(json) + ")");
	}

	private static string Delivery(string dataExpression) =>
		"(function(){var v=window.chrome&&window.chrome.webview;"
		+ "if(!v||typeof v." + DispatchFunctionName + "!=='function'){return " + NoChannelResult + ";}"
		+ "return v." + DispatchFunctionName + "(" + dataExpression + ");})()";
}
