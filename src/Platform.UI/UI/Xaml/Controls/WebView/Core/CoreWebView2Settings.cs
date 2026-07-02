#nullable enable

using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Defines properties that enable, disable, or modify WebView features.
/// </summary>
public partial class CoreWebView2Settings
{
	private string _userAgent = string.Empty;

	internal CoreWebView2Settings()
	{
	}

	/// <summary>
	/// Determines whether communication from the host
	/// to the top-level HTML document of the WebView is allowed.
	/// </summary>
	public bool IsWebMessageEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the custom User-Agent string the WebView sends with web requests.
	/// An empty string (the default) means the native web engine's own default
	/// User-Agent is used; setting it back to empty restores that default.
	/// </summary>
	/// <remarks>
	/// Every native WebView implementation receives this value (at creation and on change);
	/// implementations whose native web engine cannot change its User-Agent log a warning
	/// and ignore it.
	/// </remarks>
	public string UserAgent
	{
		get => _userAgent;
		set
		{
			value ??= string.Empty;
			if (_userAgent != value)
			{
				_userAgent = value;
				UserAgentChanged?.Invoke(value);
			}
		}
	}

	internal event Action<string>? UserAgentChanged;
}
