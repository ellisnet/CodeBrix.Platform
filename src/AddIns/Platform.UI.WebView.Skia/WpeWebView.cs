using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Extensions.Logging;
using CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;
using SkiaSharp;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux;

/// <summary>
/// Owns one offscreen WPE WebKit web view: the fdo exportable backend, the WebKitWebView GObject,
/// its settings/session/user-content-manager, and the SHM frame-export pipeline. All engine calls
/// run on the WPE thread; the public members marshal there. Events are raised on the WPE thread -
/// the consumer (WpeNativeWebView) re-dispatches to the UI thread.
/// </summary>
internal sealed unsafe class WpeWebView
{
	private const string ScriptMessageHandlerName = "codebrixWebView";

	// Maps window.chrome.webview.postMessage (the WebView2 contract) onto the WebKit handler,
	// so app code written for the Windows heads works unchanged on Linux.
	private const string ChromeWebViewBridgeScript =
		"""
		if (!window.chrome) { window.chrome = {}; }
		if (!window.chrome.webview) {
			window.chrome.webview = {
				postMessage: function (m) {
					window.webkit.messageHandlers.codebrixWebView.postMessage(typeof m === 'string' ? m : JSON.stringify(m));
				}
			};
		}
		""";

	private IntPtr _exportable;
	private IntPtr _backend;
	private IntPtr _webView;
	private IntPtr _settings;
	private IntPtr _session;
	private IntPtr _ucm;
	private WpeBackendFdo.ExportableClient* _client;
	private GCHandle _selfHandle;
	private volatile bool _disposed;
	private bool _suppressNextNavigationCompleted;

	// Raised on the WPE thread.
	public event Action<SKImage>? FrameArrived;
	public event Action<Uri?>? NavigationStarting;
	public event Action<Uri?, bool, bool, bool>? NavigationCompleted; // uri, isSuccess, canGoBack, canGoForward
	public event Action<string?>? TitleChanged;
	public event Action<string>? WebMessageReceived;

	public string? DocumentTitle { get; private set; }

	public WpeWebView(uint initialWidth, uint initialHeight)
	{
		_selfHandle = GCHandle.Alloc(this);

		WpeThread.Post(() =>
		{
			// The client struct is retained by the fdo backend: keep it in unmanaged memory
			// for the exportable's whole lifetime.
			_client = (WpeBackendFdo.ExportableClient*)NativeMemory.AllocZeroed((nuint)sizeof(WpeBackendFdo.ExportableClient));
			_client->ExportBufferResource = &OnExportBufferResourceNative;
			_client->ExportShmBuffer = &OnExportShmBufferNative;

			_exportable = WpeBackendFdo.wpe_view_backend_exportable_fdo_create(_client, GCHandle.ToIntPtr(_selfHandle), Math.Max(1, initialWidth), Math.Max(1, initialHeight));
			_backend = WpeBackendFdo.wpe_view_backend_exportable_fdo_get_view_backend(_exportable);

			_session = WebKitInterop.webkit_network_session_new(null, null);
			_settings = WebKitInterop.webkit_settings_new();
			WebKitInterop.webkit_settings_set_enable_javascript(_settings, 1);
			WebKitInterop.webkit_settings_set_allow_file_access_from_file_urls(_settings, 1);
			WebKitInterop.webkit_settings_set_enable_smooth_scrolling(_settings, 1);
#if DEBUG
			WebKitInterop.webkit_settings_set_enable_developer_extras(_settings, 1);
#endif

			_ucm = WebKitInterop.webkit_user_content_manager_new();
			_ = WebKitInterop.webkit_user_content_manager_register_script_message_handler(_ucm, ScriptMessageHandlerName, null);
			ConnectSignal(_ucm, $"script-message-received::{ScriptMessageHandlerName}", (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&OnScriptMessageNative);
			var bridgeScript = WebKitInterop.webkit_user_script_new(ChromeWebViewBridgeScript, WebKitInterop.InjectedFramesAll, WebKitInterop.InjectionTimeStart, IntPtr.Zero, IntPtr.Zero);
			WebKitInterop.webkit_user_content_manager_add_script(_ucm, bridgeScript);
			WebKitInterop.webkit_user_script_unref(bridgeScript);

			// The boxed backend wrapper transfers to the view at construction; the destroy
			// notify tears the exportable down when the view goes away.
			var backendWrapper = WebKitInterop.webkit_web_view_backend_new(_backend, &OnBackendDestroyedNative, _exportable);
			_webView = CreateWebView(backendWrapper);

			ConnectSignal(_webView, "load-changed", (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, void>)&OnLoadChangedNative);
			ConnectSignal(_webView, "load-failed", (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, IntPtr, IntPtr, int>)&OnLoadFailedNative);
			ConnectSignal(_webView, "notify::title", (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&OnTitleNotifyNative);
			ConnectSignal(_webView, "create", (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr>)&OnCreateNative);
			ConnectSignal(_webView, "web-process-terminated", (delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, void>)&OnWebProcessTerminatedNative);

			LibWpe.wpe_view_backend_add_activity_state(_backend, LibWpe.ActivityStateVisible | LibWpe.ActivityStateInWindow | LibWpe.ActivityStateFocused);
		});
	}

	private IntPtr CreateWebView(IntPtr backendWrapper)
	{
		// g_object_new is varargs; build the construct properties with GValues instead.
		var names = new IntPtr[3];
		var values = new GLibInterop.GValue[3];
		try
		{
			names[0] = Marshal.StringToCoTaskMemUTF8("backend");
			GLibInterop.g_value_init(ref values[0], WebKitInterop.webkit_web_view_backend_get_type());
			GLibInterop.g_value_set_boxed(ref values[0], backendWrapper);

			names[1] = Marshal.StringToCoTaskMemUTF8("network-session");
			GLibInterop.g_value_init(ref values[1], WebKitInterop.webkit_network_session_get_type());
			GLibInterop.g_value_set_object(ref values[1], _session);

			names[2] = Marshal.StringToCoTaskMemUTF8("user-content-manager");
			GLibInterop.g_value_init(ref values[2], WebKitInterop.webkit_user_content_manager_get_type());
			GLibInterop.g_value_set_object(ref values[2], _ucm);

			var view = GLibInterop.g_object_new_with_properties(WebKitInterop.webkit_web_view_get_type(), (uint)names.Length, names, values);

			// "settings" is CONSTRUCT (not construct-only), so the plain setter works post-construction.
			WebKitInterop.webkit_web_view_set_settings(view, _settings);
			return view;
		}
		finally
		{
			for (var i = 0; i < names.Length; i++)
			{
				if (names[i] != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(names[i]);
				}
				if (values[i].GType != 0)
				{
					GLibInterop.g_value_unset(ref values[i]);
				}
			}
		}
	}

	private void ConnectSignal(IntPtr instance, string signal, void* handler)
		=> GLibInterop.g_signal_connect_data(instance, signal, (IntPtr)handler, GCHandle.ToIntPtr(_selfHandle), IntPtr.Zero, 0);

	private static WpeWebView? FromUserData(IntPtr userData)
	{
		if (userData == IntPtr.Zero)
		{
			return null;
		}
		var view = GCHandle.FromIntPtr(userData).Target as WpeWebView;
		return view is { _disposed: false } ? view : null;
	}

	private static uint NowMs() => (uint)Environment.TickCount64;

	// ---------------------------------------------------------------------
	// Frame export (WPE thread)
	// ---------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnExportShmBufferNative(IntPtr userData, IntPtr buffer)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return;
		}

		try
		{
			var shm = WpeBackendFdo.wpe_fdo_shm_exported_buffer_get_shm_buffer(buffer);
			if (shm != IntPtr.Zero)
			{
				LibWaylandServer.wl_shm_buffer_begin_access(shm);
				try
				{
					var data = LibWaylandServer.wl_shm_buffer_get_data(shm);
					var stride = LibWaylandServer.wl_shm_buffer_get_stride(shm);
					var width = LibWaylandServer.wl_shm_buffer_get_width(shm);
					var height = LibWaylandServer.wl_shm_buffer_get_height(shm);
					var format = LibWaylandServer.wl_shm_buffer_get_format(shm);

					if (data != IntPtr.Zero && width > 0 && height > 0)
					{
						var alphaType = format == LibWaylandServer.WlShmFormatXrgb8888 ? SKAlphaType.Opaque : SKAlphaType.Premul;
						var info = new SKImageInfo(width, height, SKColorType.Bgra8888, alphaType);
						var image = SKImage.FromPixelCopy(info, data, stride);
						if (image is not null)
						{
							self.FrameArrived?.Invoke(image);
						}
					}
				}
				finally
				{
					LibWaylandServer.wl_shm_buffer_end_access(shm);
				}
			}
		}
		catch (Exception e)
		{
			if (typeof(WpeWebView).Log().IsEnabled(LogLevel.Error))
			{
				typeof(WpeWebView).Log().Error("Failed to import a WPE WebKit frame.", e);
			}
		}
		finally
		{
			// Both calls are mandatory: release returns the buffer to the web process,
			// frame-complete allows it to produce the next frame.
			WpeBackendFdo.wpe_view_backend_exportable_fdo_dispatch_release_shm_exported_buffer(self._exportable, buffer);
			WpeBackendFdo.wpe_view_backend_exportable_fdo_dispatch_frame_complete(self._exportable);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnExportBufferResourceNative(IntPtr userData, IntPtr bufferResource)
	{
		// Should not happen in SHM mode; log so a surprise is visible rather than a black view.
		if (typeof(WpeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(WpeWebView).Log().Warn("WPE exported a non-SHM buffer; the frame was dropped.");
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnBackendDestroyedNative(IntPtr exportable)
	{
		if (exportable != IntPtr.Zero)
		{
			WpeBackendFdo.wpe_view_backend_exportable_fdo_destroy(exportable);
		}
	}

	// ---------------------------------------------------------------------
	// WebKit signals (WPE thread)
	// ---------------------------------------------------------------------

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnLoadChangedNative(IntPtr view, int loadEvent, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return;
		}

		try
		{
			switch (loadEvent)
			{
				case WebKitInterop.LoadStarted:
					self.NavigationStarting?.Invoke(TryGetUri(view));
					break;

				case WebKitInterop.LoadFinished:
					if (self._suppressNextNavigationCompleted)
					{
						self._suppressNextNavigationCompleted = false;
					}
					else
					{
						var canGoBack = WebKitInterop.webkit_web_view_can_go_back(view) != 0;
						var canGoForward = WebKitInterop.webkit_web_view_can_go_forward(view) != 0;
						self.NavigationCompleted?.Invoke(TryGetUri(view), true, canGoBack, canGoForward);
					}
					break;
			}
		}
		catch (Exception e)
		{
			typeof(WpeWebView).Log().Error("load-changed handler failed.", e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static int OnLoadFailedNative(IntPtr view, int loadEvent, IntPtr failingUri, IntPtr error, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return 0;
		}

		try
		{
			self._suppressNextNavigationCompleted = true;
			Uri.TryCreate(Marshal.PtrToStringUTF8(failingUri), UriKind.Absolute, out var uri);
			var canGoBack = WebKitInterop.webkit_web_view_can_go_back(view) != 0;
			var canGoForward = WebKitInterop.webkit_web_view_can_go_forward(view) != 0;
			self.NavigationCompleted?.Invoke(uri, false, canGoBack, canGoForward);
		}
		catch (Exception e)
		{
			typeof(WpeWebView).Log().Error("load-failed handler failed.", e);
		}
		return 0; // allow default handling too
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnTitleNotifyNative(IntPtr obj, IntPtr pspec, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return;
		}

		self.DocumentTitle = Marshal.PtrToStringUTF8(WebKitInterop.webkit_web_view_get_title(obj));
		self.TitleChanged?.Invoke(self.DocumentTitle);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static IntPtr OnCreateNative(IntPtr view, IntPtr navigationAction, IntPtr userData)
	{
		// Popup/new-window requests navigate the current view (documented v1 behavior).
		try
		{
			var request = WebKitInterop.webkit_navigation_action_get_request(navigationAction);
			if (request != IntPtr.Zero)
			{
				var uri = Marshal.PtrToStringUTF8(WebKitInterop.webkit_uri_request_get_uri(request));
				if (!string.IsNullOrEmpty(uri))
				{
					WebKitInterop.webkit_web_view_load_uri(view, uri);
				}
			}
		}
		catch (Exception e)
		{
			typeof(WpeWebView).Log().Error("create (new window) handler failed.", e);
		}
		return IntPtr.Zero; // refuse the separate window
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnWebProcessTerminatedNative(IntPtr view, int reason, IntPtr userData)
	{
		if (typeof(WpeWebView).Log().IsEnabled(LogLevel.Error))
		{
			typeof(WpeWebView).Log().Error($"The WPE web process terminated unexpectedly (reason {reason}).");
		}
		FromUserData(userData)?.NavigationCompleted?.Invoke(null, false, false, false);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnScriptMessageNative(IntPtr manager, IntPtr jscValue, IntPtr userData)
	{
		var self = FromUserData(userData);
		if (self is null)
		{
			return;
		}

		try
		{
			self.WebMessageReceived?.Invoke(JscValueToString(jscValue));
		}
		catch (Exception e)
		{
			typeof(WpeWebView).Log().Error("script-message-received handler failed.", e);
		}
	}

	private static Uri? TryGetUri(IntPtr view)
	{
		Uri.TryCreate(Marshal.PtrToStringUTF8(WebKitInterop.webkit_web_view_get_uri(view)), UriKind.Absolute, out var uri);
		return uri;
	}

	private static string JscValueToString(IntPtr value)
	{
		if (WebKitInterop.jsc_value_is_null(value) != 0)
		{
			return "null";
		}
		if (WebKitInterop.jsc_value_is_undefined(value) != 0)
		{
			return "undefined";
		}

		var jsonPtr = WebKitInterop.jsc_value_to_json(value, 0);
		try
		{
			var json = Marshal.PtrToStringUTF8(jsonPtr) ?? "null";
			if (WebKitInterop.jsc_value_is_string(value) != 0)
			{
				// Matches the string-quoting normalization used by the other CodeBrix WebView
				// implementations: Unescape negates the double escaping due to Encode + ToJson.
				return Regex.Unescape(System.Text.Json.JsonEncodedText.Encode(json, JavaScriptEncoder.UnsafeRelaxedJsonEscaping).ToString());
			}
			return json;
		}
		finally
		{
			GLibInterop.g_free(jsonPtr);
		}
	}

	// ---------------------------------------------------------------------
	// Commands (any thread; marshaled to the WPE thread)
	// ---------------------------------------------------------------------

	public void LoadUri(string uri) => WpeThread.Post(() => WebKitInterop.webkit_web_view_load_uri(_webView, uri));

	public void LoadHtml(string html) => WpeThread.Post(() => WebKitInterop.webkit_web_view_load_html(_webView, html, null));

	public void LoadRequest(string uri, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers) => WpeThread.Post(() =>
	{
		var request = WebKitInterop.webkit_uri_request_new(uri);
		var native = WebKitInterop.webkit_uri_request_get_http_headers(request);
		if (native != IntPtr.Zero)
		{
			foreach (var header in headers)
			{
				foreach (var value in header.Value)
				{
					WebKitInterop.soup_message_headers_append(native, header.Key, value);
				}
			}
		}
		else if (typeof(WpeWebView).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(WpeWebView).Log().Warn("The URI request exposed no header table; custom headers were not applied.");
		}

		WebKitInterop.webkit_web_view_load_request(_webView, request);
		GLibInterop.g_object_unref(request);
	});

	public void GoBack() => WpeThread.Post(() => WebKitInterop.webkit_web_view_go_back(_webView));
	public void GoForward() => WpeThread.Post(() => WebKitInterop.webkit_web_view_go_forward(_webView));
	public void StopLoading() => WpeThread.Post(() => WebKitInterop.webkit_web_view_stop_loading(_webView));
	public void Reload() => WpeThread.Post(() => WebKitInterop.webkit_web_view_reload(_webView));

	public void SetUserAgent(string userAgent) => WpeThread.Post(() =>
		WebKitInterop.webkit_settings_set_user_agent(_settings, string.IsNullOrEmpty(userAgent) ? null : userAgent));

	public void Resize(uint logicalWidth, uint logicalHeight) => WpeThread.Post(() =>
		LibWpe.wpe_view_backend_dispatch_set_size(_backend, Math.Max(1, logicalWidth), Math.Max(1, logicalHeight)));

	public void SetScale(float scale) => WpeThread.Post(() =>
		LibWpe.wpe_view_backend_dispatch_set_device_scale_factor(_backend, Math.Clamp(scale, 0.05f, 5.0f)));

	public void SetVisible(bool visible) => WpeThread.Post(() =>
	{
		if (visible)
		{
			LibWpe.wpe_view_backend_add_activity_state(_backend, LibWpe.ActivityStateVisible | LibWpe.ActivityStateInWindow);
		}
		else
		{
			LibWpe.wpe_view_backend_remove_activity_state(_backend, LibWpe.ActivityStateVisible);
		}
	});

	public void SetFocused(bool focused) => WpeThread.Post(() =>
	{
		if (focused)
		{
			LibWpe.wpe_view_backend_add_activity_state(_backend, LibWpe.ActivityStateFocused);
		}
		else
		{
			LibWpe.wpe_view_backend_remove_activity_state(_backend, LibWpe.ActivityStateFocused);
		}
	});

	// ---------------------------------------------------------------------
	// Input (coordinates in physical pixels)
	// ---------------------------------------------------------------------

	public void DispatchPointerMotion(int x, int y, uint modifiers) => WpeThread.Post(() =>
	{
		var e = new LibWpe.WpeInputPointerEvent
		{
			Type = LibWpe.PointerEventTypeMotion,
			Time = NowMs(),
			X = x,
			Y = y,
			Modifiers = modifiers,
		};
		LibWpe.wpe_view_backend_dispatch_pointer_event(_backend, ref e);
	});

	public void DispatchPointerButton(int x, int y, uint button, bool pressed, uint modifiers) => WpeThread.Post(() =>
	{
		var e = new LibWpe.WpeInputPointerEvent
		{
			Type = LibWpe.PointerEventTypeButton,
			Time = NowMs(),
			X = x,
			Y = y,
			Button = button,
			State = pressed ? 1u : 0u,
			Modifiers = modifiers,
		};
		LibWpe.wpe_view_backend_dispatch_pointer_event(_backend, ref e);
	});

	public void DispatchWheel(int x, int y, double deltaX, double deltaY, uint modifiers) => WpeThread.Post(() =>
	{
		var e = new LibWpe.WpeInputAxis2DEvent
		{
			Base = new LibWpe.WpeInputAxisEvent
			{
				Type = LibWpe.AxisEventTypeMask2D | LibWpe.AxisEventTypeMotionSmooth,
				Time = NowMs(),
				X = x,
				Y = y,
				Modifiers = modifiers,
			},
			XAxis = deltaX,
			YAxis = deltaY,
		};
		LibWpe.wpe_view_backend_dispatch_axis_event(_backend, ref e);
	});

	public void DispatchKey(uint keysym, uint hardwareKeyCode, bool pressed, uint modifiers) => WpeThread.Post(() =>
	{
		var e = new LibWpe.WpeInputKeyboardEvent
		{
			Time = NowMs(),
			KeyCode = keysym,
			HardwareKeyCode = hardwareKeyCode,
			Pressed = pressed ? (byte)1 : (byte)0,
			Modifiers = modifiers,
		};
		LibWpe.wpe_view_backend_dispatch_keyboard_event(_backend, ref e);
	});

	// ---------------------------------------------------------------------
	// JavaScript
	// ---------------------------------------------------------------------

	public Task<string?> EvaluateScriptAsync(string script)
	{
		var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
		WpeThread.Post(() =>
		{
			var state = GCHandle.Alloc((this, tcs));
			WebKitInterop.webkit_web_view_evaluate_javascript(_webView, script, -1, null, null, IntPtr.Zero, &OnEvaluateReadyNative, GCHandle.ToIntPtr(state));
		});
		return tcs.Task;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void OnEvaluateReadyNative(IntPtr source, IntPtr result, IntPtr userData)
	{
		var handle = GCHandle.FromIntPtr(userData);
		var (self, tcs) = ((WpeWebView, TaskCompletionSource<string?>))handle.Target!;
		handle.Free();

		try
		{
			var value = WebKitInterop.webkit_web_view_evaluate_javascript_finish(source, result, out var error);
			if (value == IntPtr.Zero)
			{
				var message = GLibInterop.GetErrorMessage(error) ?? "Script evaluation failed.";
				if (error != IntPtr.Zero)
				{
					GLibInterop.g_error_free(error);
				}
				tcs.SetException(new InvalidOperationException(message));
				return;
			}

			var text = JscValueToString(value);
			GLibInterop.g_object_unref(value);
			tcs.SetResult(text);
		}
		catch (Exception e)
		{
			tcs.SetException(e);
		}
	}

	// ---------------------------------------------------------------------
	// Teardown
	// ---------------------------------------------------------------------

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;

		WpeThread.Post(() =>
		{
			// Unreffing the view runs the backend-wrapper destroy notify, which destroys the
			// exportable; afterwards no export callbacks can fire and the client memory and
			// self handle can be released.
			if (_webView != IntPtr.Zero)
			{
				GLibInterop.g_object_unref(_webView);
			}
			if (_ucm != IntPtr.Zero)
			{
				GLibInterop.g_object_unref(_ucm);
			}
			if (_settings != IntPtr.Zero)
			{
				GLibInterop.g_object_unref(_settings);
			}
			if (_session != IntPtr.Zero)
			{
				GLibInterop.g_object_unref(_session);
			}
			if (_client is not null)
			{
				NativeMemory.Free(_client);
				_client = null;
			}
			if (_selfHandle.IsAllocated)
			{
				_selfHandle.Free();
			}
		});
	}
}
