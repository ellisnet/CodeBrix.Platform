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

	private readonly CoreWebView2 _coreWebView;
	private readonly ContentPresenter _presenter;
	private readonly WpeWebViewHostElement _element;
	private readonly WpeWebView _wpe;
	private readonly Control? _focusTarget;
	private double _scale = 1.0;

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

		_wpe.NavigationStarting += uri => _presenter.DispatcherQueue.TryEnqueue(() =>
		{
			if (uri is null)
			{
				return;
			}
			_coreWebView.RaiseNavigationStarting(uri, out var cancel);
			if (cancel)
			{
				_wpe.StopLoading();
			}
		});

		_wpe.NavigationCompleted += (uri, isSuccess, canGoBack, canGoForward) => _presenter.DispatcherQueue.TryEnqueue(() =>
		{
			_coreWebView.SetHistoryProperties(canGoBack, canGoForward);
			_coreWebView.RaiseHistoryChanged();
			_coreWebView.RaiseNavigationCompleted(uri, isSuccess, httpStatusCode: isSuccess ? 200 : 0, errorStatus: CoreWebView2WebErrorStatus.Unknown, shouldSetSource: true);
		});

		_wpe.TitleChanged += _ => _presenter.DispatcherQueue.TryEnqueue(() => _coreWebView.OnDocumentTitleChanged());

		_wpe.WebMessageReceived += message => _presenter.DispatcherQueue.TryEnqueue(() => _coreWebView.RaiseWebMessageReceived(message));
	}

	// ---------------------------------------------------------------------
	// XAML input → WPE
	// ---------------------------------------------------------------------

	private void WireInput()
	{
		_element.PointerMoved += (_, e) =>
		{
			var point = e.GetCurrentPoint(_element);
			var (x, y) = ToPhysical(point.Position);
			_wpe.DispatchPointerMotion(x, y, GetModifiers(e, point));
			e.Handled = true;
		};

		_element.PointerPressed += (_, e) =>
		{
			_element.CapturePointer(e.Pointer);
			_focusTarget?.Focus(FocusState.Pointer);

			var point = e.GetCurrentPoint(_element);
			if (TryGetButtonTransition(point.Properties.PointerUpdateKind, out var button, out var pressed))
			{
				var (x, y) = ToPhysical(point.Position);
				_wpe.DispatchPointerButton(x, y, button, pressed, GetModifiers(e, point));
			}
			e.Handled = true;
		};

		_element.PointerReleased += (_, e) =>
		{
			var point = e.GetCurrentPoint(_element);
			if (TryGetButtonTransition(point.Properties.PointerUpdateKind, out var button, out var pressed))
			{
				var (x, y) = ToPhysical(point.Position);
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

	public void Reload() => _wpe.Reload();

	public void ProcessNavigation(Uri uri)
	{
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

	public void ProcessNavigation(string html) => _wpe.LoadHtml(html);

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
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
