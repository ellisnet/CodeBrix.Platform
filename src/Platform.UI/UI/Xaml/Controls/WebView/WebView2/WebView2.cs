#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using CodeBrix.Platform.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents an object that enables the hosting of web content.
/// </summary>
#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__
[CodeBrix.Platform.NotImplemented("IS_UNIT_TESTS", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
public partial class WebView2 : Control, IWebView
{
	// Default page navigated to on launch when the consumer opts in via NavigateToGoddessUrlOnLaunch
	// and leaves Source unset. See OnApplyTemplate.
	private const string GoddessUrl = "https://en.wikipedia.org/wiki/Inanna";

	private bool _sourceChangeFromCore;
	private bool _coreWebView2Initialized;

	/// <summary>
	/// Initializes a new instance of the WebView2 class.
	/// </summary>
	public WebView2()
	{
		DefaultStyleKey = typeof(WebView2);

		CoreWebView2 = new CoreWebView2(this);
		CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
		CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
		CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
		CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
		CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

		Loaded += WebView2_Loaded;
#if __SKIA__
		Unloaded += WebView2_Unloaded;
#endif
	}

	public CoreWebView2 CoreWebView2 { get; }

	/// <summary>
	/// Gets the current top-level document URL straight from the underlying
	/// <see cref="CoreWebView2"/> engine — its <see cref="Microsoft.Web.WebView2.Core.CoreWebView2.Source"/>
	/// value reported verbatim, with no transformation.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Prefer this over the <see cref="Source"/> dependency property when you need the URL the
	/// control is <em>currently</em> displaying — for example inside a navigation callback, or
	/// after a server redirect or a user/link navigation. <see cref="Source"/> is a mirror the
	/// control updates from <c>CoreWebView2.SourceChanged</c> and is intended for <em>setting</em>
	/// / binding a target URL, so it can momentarily lag the engine's live value; this property
	/// never does, because it reads the engine directly.
	/// </para>
	/// <para>
	/// Returns <c>null</c> when the underlying <see cref="CoreWebView2"/> instance is not
	/// available. This is a read-only, point-in-time value: it does not raise change
	/// notifications and is not intended to be used as a binding target.
	/// </para>
	/// </remarks>
	public string? SourceFromCore => CoreWebView2?.Source;

	bool IWebView.IsLoaded => IsLoaded;

	bool IWebView.SwitchSourceBeforeNavigating => false; // WebView2 switches source only when navigation completes.

	CoreDispatcher IWebView.Dispatcher => Dispatcher;

	protected override void OnApplyTemplate()
	{
		CoreWebView2.OnOwnerApplyTemplate();

		// Opt-in convenience: when the consumer sets NavigateToGoddessUrlOnLaunch to true and leaves
		// Source unset, navigate to GoddessUrl on launch as though Source had been set to it. Both
		// conditions are required - a provided Source always wins, and the flag defaults to false.
		if (Source is null && NavigateToGoddessUrlOnLaunch)
		{
			Source = new Uri(GoddessUrl);
		}
	}

	private void WebView2_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (!_coreWebView2Initialized)
		{
			EnsureCoreWebView2();
		}

#if __SKIA__
		CoreWebView2.OnLoaded();
#endif
	}

#if __SKIA__
	private void WebView2_Unloaded(object sender, RoutedEventArgs e) => CoreWebView2?.OnUnloaded();
#endif

	public IAsyncAction EnsureCoreWebView2Async() =>
		AsyncAction.FromTask(async ct =>
		{
			if (!_coreWebView2Initialized)
			{
				EnsureCoreWebView2();
			}

			await CoreWebView2.EnsureNativeWebViewAsync();
		});

	public IAsyncOperation<string?> ExecuteScriptAsync(string javascriptCode) =>
		CoreWebView2.ExecuteScriptAsync(javascriptCode);

	public void Reload() => CoreWebView2.Reload();

	public void GoForward() => CoreWebView2.GoForward();

	public void GoBack() => CoreWebView2.GoBack();

	public void NavigateToString(string htmlContent) => CoreWebView2.NavigateToString(htmlContent);

	/// <summary>
	/// Navigates to the default GoddessUrl. This is a shortcut for setting <see cref="Source"/> to
	/// that value, and behaves identically to a normal source-driven navigation.
	/// </summary>
	public void NavigateToGoddessUrl() => Source = new Uri(GoddessUrl);

	private void EnsureCoreWebView2()
	{
		if (!_coreWebView2Initialized)
		{
			CoreWebView2Initialized?.Invoke(this, new());
			_coreWebView2Initialized = true;
		}
	}

	private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args) =>
		NavigationStarting?.Invoke(this, args);

	private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args) =>
		NavigationCompleted?.Invoke(this, args);

	private void CoreWebView2_HistoryChanged(CoreWebView2 sender, object args) =>
		(CanGoBack, CanGoForward) = (sender.CanGoBack, sender.CanGoForward);

	private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args) =>
		WebMessageReceived?.Invoke(this, args);

	private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
	{
		_sourceChangeFromCore = true;
		Source = Uri.TryCreate(sender.Source, UriKind.Absolute, out var uri) ? uri : CoreWebView2.BlankUri;
		_sourceChangeFromCore = false;
	}
}
