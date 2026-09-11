using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Windows.ApplicationModel;
using Windows.System;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.WebView.Skia.Linux.Input;
using CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;
using CodeBrix.Platform.UI.Xaml.Controls;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux;

/// <summary>
/// The INativeWebView implementation for the Linux Skia heads (X11, Wayland, FrameBuffer).
/// Bridges the CoreWebView2 control seam to an offscreen WPE WebKit view: frames are composited
/// into the Skia scene through <see cref="WpeWebViewHostElement"/>, and XAML routed input is
/// translated into WPE input events.
/// </summary>
internal sealed class WpeNativeWebView : ICleanableNativeWebView
{
	// Pixels of scroll per 120-unit wheel detent, matching common browser behavior.
	private const double WheelPixelsPerTick = 53.0;

	// The URI the engine gives a document built from text: webkit_web_view_load_html is handed no
	// base URI of its own, so the document it builds is about:blank as far as the engine knows.
	private const string TextDocumentUri = "about:blank";

	private readonly CoreWebView2 _coreWebView;
	private readonly ContentPresenter _presenter;
	private readonly WpeWebViewHostElement _element;
	private readonly WpeWebView _wpe;
	private readonly Control? _focusTarget;
	private double _scale = 1.0;

	// The page most recently handed over as TEXT, kept until the load it starts is announced.
	// The engine only knows the base URI such a load was given (none, i.e. about:blank), but the
	// control's contract - and every other head - announces it as the data: document the text
	// becomes, which is what CoreWebView2.RaiseNavigationStarting builds when it is given the
	// html rather than a Uri. Written and read on the UI thread only.
	private string? _pendingHtml;

	// The page the control is SHOWING because it was handed over as text, or null when the
	// document showing came from a URI. Reload re-issues it: see Reload(). Written and read on
	// the UI thread only.
	private string? _lastHtml;

	// Where the engine says the document showing came from, as of the last navigation that
	// completed. Written and read on the UI thread only.
	private string? _currentUri;

	public WpeNativeWebView(CoreWebView2 coreWebView2, ContentPresenter presenter)
	{
		if (WpeThread.EnsureStarted() is { } initException)
		{
			throw initException;
		}

		_coreWebView = coreWebView2;
		_presenter = presenter;

		_element = new WpeWebViewHostElement(presenter.Visual.Compositor);
		presenter.Content = _element;

		var initialWidth = (uint)Math.Max(1, presenter.ActualWidth);
		var initialHeight = (uint)Math.Max(1, presenter.ActualHeight);
		_wpe = new WpeWebView(initialWidth == 1 ? 1280 : initialWidth, initialHeight == 1 ? 720 : initialHeight);

		WireEngineEvents();

		_focusTarget = _coreWebView.Owner as Control;
		WireInput();

		_element.SizeChanged += (_, _) => UpdateSizeAndScale();
		_element.Loaded += (_, _) => UpdateSizeAndScale();
	}

	~WpeNativeWebView()
	{
		_wpe.Dispose();
	}

	// ---------------------------------------------------------------------
	// Engine events → CoreWebView2 (marshaled to the UI thread)
	// ---------------------------------------------------------------------

	private void WireEngineEvents()
	{
		_wpe.FrameArrived += image =>
		{
			if (!_presenter.DispatcherQueue.TryEnqueue(() => _element.PresentFrame(image)))
			{
				image.Dispose();
			}
		};

		// The engine asks before it commits to a navigation, and waits for the answer: the event
		// is raised on the UI thread - where a XAML application's handler has to run - and the
		// engine is answered afterwards, so a handler that sets Cancel REFUSES the navigation
		// rather than chasing one that has already happened. Nothing blocks the engine thread
		// while the handler runs; that one navigation is what waits.
		_wpe.NavigationPolicyRequested += (uri, decision) =>
		{
			if (!_presenter.DispatcherQueue.TryEnqueue(() => DecideNavigation(uri, decision)))
			{
				// No UI thread left to ask: the engine must not be left holding a navigation.
				_wpe.DecideNavigation(decision, allow: true);
			}
		};

		_wpe.NavigationCompleted += (uri, isSuccess, canGoBack, canGoForward) => _presenter.DispatcherQueue.TryEnqueue(() =>
		{
			_currentUri = uri?.ToString();
			_coreWebView.SetHistoryProperties(canGoBack, canGoForward);
			_coreWebView.RaiseHistoryChanged();
			_coreWebView.RaiseNavigationCompleted(uri, isSuccess, httpStatusCode: isSuccess ? 200 : 0, errorStatus: CoreWebView2WebErrorStatus.Unknown, shouldSetSource: true);
		});

		_wpe.TitleChanged += _ => _presenter.DispatcherQueue.TryEnqueue(() => _coreWebView.OnDocumentTitleChanged());

		_wpe.WebMessageReceived += message => _presenter.DispatcherQueue.TryEnqueue(() => _coreWebView.RaiseWebMessageReceived(message));

		_wpe.DownloadStarting += (download, suggestedFileName) => _presenter.DispatcherQueue.TryEnqueue(() =>
		{
			var defaultPath = DownloadDefaults.GetCollisionFreePath(DownloadDefaults.GetDownloadsFolder(), suggestedFileName);
			var operation = new CoreWebView2DownloadOperation(
				download.Uri,
				download.ContentDisposition,
				download.MimeType,
				download.TotalBytesToReceive,
				defaultPath,
				download.Cancel);

			download.ProgressChanged += bytesReceived =>
				_presenter.DispatcherQueue.TryEnqueue(() => operation.ReportProgress(bytesReceived));
			download.Completed += () =>
				_presenter.DispatcherQueue.TryEnqueue(() => operation.ReportStateChanged(CoreWebView2DownloadState.Completed));
			download.Failed += (wasCanceled, isDestinationFailure, message) => _presenter.DispatcherQueue.TryEnqueue(() =>
			{
				if (!wasCanceled && this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"WebView download failed: {message}");
				}
				operation.ReportStateChanged(
					CoreWebView2DownloadState.Interrupted,
					wasCanceled ? CoreWebView2DownloadInterruptReason.UserCanceled
						: isDestinationFailure ? CoreWebView2DownloadInterruptReason.FileFailed
						: CoreWebView2DownloadInterruptReason.NetworkFailed);
			});

			// The engine download stays parked until the app's DownloadStarting decision
			// (including any deferral) resolves to a destination or a cancellation.
			_coreWebView.RaiseDownloadStarting(operation, args =>
			{
				if (args.Cancel)
				{
					download.Cancel();
				}
				else
				{
					operation.SetResultFilePath(args.ResultFilePath);
					download.SetDestination(args.ResultFilePath);
				}
			});
		});
	}

	/// <summary>
	/// Announces one navigation on the UI thread and answers the engine with what the application
	/// decided. A page handed over as text is announced as the data: document it becomes; anything
	/// else is announced as the URI the engine asked about, which is the page being navigated TO.
	/// </summary>
	/// <param name="uri">The URI the engine asked about, when it parsed as one.</param>
	/// <param name="decision">The engine's parked decision, answered exactly once here.</param>
	private void DecideNavigation(Uri? uri, IntPtr decision)
	{
		var cancel = false;
		try
		{
			var html = _pendingHtml;
			_pendingHtml = null;
			if (((object?)html ?? uri) is { } navigation)
			{
				_coreWebView.RaiseNavigationStarting(navigation, out cancel);
			}

			if (html is not null && !cancel)
			{
				// The document about to show came from text, so Reload has to be given it again:
				// the engine has nowhere to fetch about:blank back from.
				_lastHtml = html;
			}
		}
		finally
		{
			_wpe.DecideNavigation(decision, allow: !cancel);
		}
	}

	// ---------------------------------------------------------------------
	// XAML input → WPE
	// ---------------------------------------------------------------------

	private void WireInput()
	{
		// Touch pointers (the FrameBuffer head's touchscreen, and the emulated head's
		// mouse-as-finger) are handed to WPE as TOUCH events, so the engine's own touch
		// behavior applies: a swipe pans the page (finger drags the content), a tap
		// clicks, a long-press long-presses. Everything else stays a mouse.
		_element.PointerMoved += (_, e) =>
		{
			var point = e.GetCurrentPoint(_element);
			var (x, y) = ToPhysical(point.Position);
			if (IsTouch(e))
			{
				// A finger that is not touching the screen does not exist, so only
				// in-contact motion is forwarded — there is no touch hover.
				if (point.IsInContact)
				{
					_wpe.DispatchTouch(LibWpe.TouchEventTypeMotion, (int)e.Pointer.PointerId, x, y, GetModifiers(e, point));
				}
			}
			else
			{
				_wpe.DispatchPointerMotion(x, y, GetModifiers(e, point));
			}
			e.Handled = true;
		};

		_element.PointerPressed += (_, e) =>
		{
			_element.CapturePointer(e.Pointer);
			_focusTarget?.Focus(FocusState.Pointer);

			var point = e.GetCurrentPoint(_element);
			var (x, y) = ToPhysical(point.Position);
			if (IsTouch(e))
			{
				_wpe.DispatchTouch(LibWpe.TouchEventTypeDown, (int)e.Pointer.PointerId, x, y, GetModifiers(e, point));
			}
			else if (TryGetButtonTransition(point.Properties.PointerUpdateKind, out var button, out var pressed))
			{
				_wpe.DispatchPointerButton(x, y, button, pressed, GetModifiers(e, point));
			}
			e.Handled = true;
		};

		_element.PointerReleased += (_, e) =>
		{
			var point = e.GetCurrentPoint(_element);
			var (x, y) = ToPhysical(point.Position);
			if (IsTouch(e))
			{
				_wpe.DispatchTouch(LibWpe.TouchEventTypeUp, (int)e.Pointer.PointerId, x, y, GetModifiers(e, point));
			}
			else if (TryGetButtonTransition(point.Properties.PointerUpdateKind, out var button, out var pressed))
			{
				_wpe.DispatchPointerButton(x, y, button, pressed, GetModifiers(e, point));
			}
			_element.ReleasePointerCapture(e.Pointer);
			e.Handled = true;
		};

		_element.PointerWheelChanged += (_, e) =>
		{
			var point = e.GetCurrentPoint(_element);
			var (x, y) = ToPhysical(point.Position);
			var deltaPixels = point.Properties.MouseWheelDelta / 120.0 * WheelPixelsPerTick * _scale;
			var horizontal = point.Properties.IsHorizontalMouseWheel;
			_wpe.DispatchWheel(x, y, horizontal ? deltaPixels : 0, horizontal ? 0 : deltaPixels, GetModifiers(e, point));
			e.Handled = true;
		};

		var keySource = (UIElement?)_focusTarget ?? _element;
		keySource.KeyDown += (_, e) => OnKey(e, pressed: true);
		keySource.KeyUp += (_, e) => OnKey(e, pressed: false);

		if (_focusTarget is not null)
		{
			_focusTarget.GotFocus += (_, _) => _wpe.SetFocused(true);
			_focusTarget.LostFocus += (_, _) => _wpe.SetFocused(false);
		}
	}

	private void OnKey(KeyRoutedEventArgs e, bool pressed)
	{
		var keysym = XkbKeyMapper.KeysymFromVirtualKey(e.OriginalKey);
		if (keysym == 0 && e.UnicodeKey is { } c)
		{
			keysym = XkbKeyMapper.KeysymFromChar(c);
		}
		if (keysym == 0)
		{
			return;
		}

		// The Linux heads fill ScanCode with the xkb keycode (evdev + 8), which is exactly
		// the X11-style hardware keycode WPE expects.
		var hardware = e.KeyStatus.ScanCode;
		_wpe.DispatchKey(keysym, hardware, pressed, ToWpeModifiers(e.KeyboardModifiers));
		e.Handled = true;
	}

	private (int X, int Y) ToPhysical(Windows.Foundation.Point position)
		=> ((int)Math.Round(position.X * _scale), (int)Math.Round(position.Y * _scale));

	private static bool IsTouch(PointerRoutedEventArgs e)
		=> e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch;

	private static bool TryGetButtonTransition(PointerUpdateKind kind, out uint button, out bool pressed)
	{
		// WPE/WebKit button numbering: 1 = Left, 2 = Right, 3 = Middle.
		(button, pressed) = kind switch
		{
			PointerUpdateKind.LeftButtonPressed => (1u, true),
			PointerUpdateKind.LeftButtonReleased => (1u, false),
			PointerUpdateKind.RightButtonPressed => (2u, true),
			PointerUpdateKind.RightButtonReleased => (2u, false),
			PointerUpdateKind.MiddleButtonPressed => (3u, true),
			PointerUpdateKind.MiddleButtonReleased => (3u, false),
			_ => (0u, false),
		};
		return button != 0;
	}

	private static uint GetModifiers(PointerRoutedEventArgs e, PointerPoint point)
	{
		var modifiers = ToWpeModifiers(e.KeyModifiers);
		var properties = point.Properties;
		if (properties.IsLeftButtonPressed)
		{
			modifiers |= Interop.LibWpe.ModifierPointerButton1;
		}
		if (properties.IsRightButtonPressed)
		{
			modifiers |= Interop.LibWpe.ModifierPointerButton2;
		}
		if (properties.IsMiddleButtonPressed)
		{
			modifiers |= Interop.LibWpe.ModifierPointerButton3;
		}
		return modifiers;
	}

	private static uint ToWpeModifiers(VirtualKeyModifiers modifiers)
	{
		var result = 0u;
		if (modifiers.HasFlag(VirtualKeyModifiers.Control))
		{
			result |= Interop.LibWpe.ModifierControl;
		}
		if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
		{
			result |= Interop.LibWpe.ModifierShift;
		}
		if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
		{
			result |= Interop.LibWpe.ModifierAlt;
		}
		if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
		{
			result |= Interop.LibWpe.ModifierMeta;
		}
		return result;
	}

	private void UpdateSizeAndScale()
	{
		var newScale = _element.XamlRoot?.RasterizationScale ?? 1.0;
		if (Math.Abs(newScale - _scale) > 0.001)
		{
			_scale = newScale;
			_wpe.SetScale((float)newScale);
		}

		if (_element.ActualWidth >= 1 && _element.ActualHeight >= 1)
		{
			_wpe.Resize((uint)_element.ActualWidth, (uint)_element.ActualHeight);
		}
	}

	// ---------------------------------------------------------------------
	// INativeWebView / ICleanableNativeWebView
	// ---------------------------------------------------------------------

	public string DocumentTitle => _wpe.DocumentTitle ?? string.Empty;

	public void GoBack() => _wpe.GoBack();

	public void GoForward() => _wpe.GoForward();

	public void Stop() => _wpe.StopLoading();

	public void Reload()
	{
		// MEASURED: webkit_web_view_load_html gives the document it builds the URI about:blank,
		// and the text itself is nowhere the engine can get back to - so asking the engine to
		// reload while THAT document is showing re-fetches about:blank and the page comes back
		// empty. A page handed over as text is therefore handed over again, which is what every
		// other head's reload of such a page amounts to. Anything the engine can fetch for itself
		// - a file, a request, a page reached from one of them - is reloaded by the engine.
		if (_lastHtml is { } html && (_currentUri is null || string.Equals(_currentUri, TextDocumentUri, StringComparison.Ordinal)))
		{
			_pendingHtml = html;
			_wpe.LoadHtml(html);
			return;
		}

		_wpe.Reload();
	}

	public void ProcessNavigation(Uri uri)
	{
		_pendingHtml = null;
		_lastHtml = null;
		if (_coreWebView.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName))
		{
			// Virtual-host-to-folder mapping resolves into the app's install directory.
			var relativePath = uri.PathAndQuery;
			var baseUrl = Package.Current.InstalledPath;
			_wpe.LoadUri($"file://{Path.Join(baseUrl, folderName, relativePath)}");
		}
		else
		{
			_wpe.LoadUri(uri.ToString());
		}
	}

	public void ProcessNavigation(string html)
	{
		_pendingHtml = html;
		_wpe.LoadHtml(html);
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
		_pendingHtml = null;
		_lastHtml = null;
		var url = httpRequestMessage.RequestUri?.ToString();
		if (url is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(ProcessNavigation)} received an {nameof(HttpRequestMessage)} with a null uri.");
			}
			return;
		}

		_wpe.LoadRequest(url, httpRequestMessage.Headers);
	}

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token) => _wpe.EvaluateScriptAsync(script);

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
	{
		// JsonSerializer.Serialize safely escapes quotes and concatenates the arguments (with a
		// comma) to be passed to eval; the [1..^1] part removes the surrounding [ and ].
		var argumentString = arguments is not null ? JsonSerializer.Serialize(arguments)[1..^1] : "";
		return ExecuteScriptAsync($"{script}({argumentString})", token);
	}

	public void SetUserAgent(string userAgent) => _wpe.SetUserAgent(userAgent);

	public void SetScrollingEnabled(bool isScrollingEnabled)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"{nameof(SetScrollingEnabled)} is not supported on the Linux WPE WebView.");
		}
	}

	public void OnLoaded()
	{
		_wpe.SetVisible(true);
		UpdateSizeAndScale();
	}

	public void OnUnloaded() => _wpe.SetVisible(false);
}
