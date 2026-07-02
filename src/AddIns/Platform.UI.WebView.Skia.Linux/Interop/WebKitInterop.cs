using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;

/// <summary>
/// Bindings for the wpe-webkit-2.0 GObject API (WebKit 2.48.x). All webkit_* and jsc_* symbols
/// are exported by libWPEWebKit-2.0.so.1 (JavaScriptCore is linked into the same library on the
/// WPE port). Returned "const char*" strings are borrowed; returned "char*" strings are owned
/// and must be released with g_free.
/// </summary>
internal static class WebKitInterop
{
	// WebKitLoadEvent
	public const int LoadStarted = 0;
	public const int LoadRedirected = 1;
	public const int LoadCommitted = 2;
	public const int LoadFinished = 3;

	// WebKitUserContentInjectedFrames / WebKitUserScriptInjectionTime
	public const int InjectedFramesAll = 0;
	public const int InjectionTimeStart = 0;

	// type getters (GType = 8-byte gsize)
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern nuint webkit_web_view_get_type();

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern nuint webkit_web_view_backend_get_type();

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern nuint webkit_network_session_get_type();

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern nuint webkit_settings_get_type();

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern nuint webkit_user_content_manager_get_type();

	// backend wrapper (boxed; ownership passes to the web view at construction)
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern unsafe IntPtr webkit_web_view_backend_new(IntPtr wpeBackend, delegate* unmanaged[Cdecl]<IntPtr, void> notify, IntPtr userData);

	// session / settings / user content
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_network_session_new([MarshalAs(UnmanagedType.LPUTF8Str)] string? dataDirectory, [MarshalAs(UnmanagedType.LPUTF8Str)] string? cacheDirectory);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_settings_new();

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_settings_set_enable_javascript(IntPtr settings, int enabled);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_settings_set_allow_file_access_from_file_urls(IntPtr settings, int allowed);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_settings_set_enable_smooth_scrolling(IntPtr settings, int enabled);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_settings_set_enable_developer_extras(IntPtr settings, int enabled);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_settings_set_user_agent(IntPtr settings, [MarshalAs(UnmanagedType.LPUTF8Str)] string? userAgent); // NULL restores the engine default

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_set_settings(IntPtr webView, IntPtr settings);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_user_content_manager_new();

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern int webkit_user_content_manager_register_script_message_handler(IntPtr manager, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string? worldName);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_user_script_new([MarshalAs(UnmanagedType.LPUTF8Str)] string source, int injectedFrames, int injectionTime, IntPtr allowList, IntPtr blockList);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_user_content_manager_add_script(IntPtr manager, IntPtr script);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_user_script_unref(IntPtr script);

	// navigation & state
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_load_uri(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string uri);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_load_html(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string content, [MarshalAs(UnmanagedType.LPUTF8Str)] string? baseUri);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_load_request(IntPtr webView, IntPtr request);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_stop_loading(IntPtr webView);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_reload(IntPtr webView);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_go_back(IntPtr webView);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern void webkit_web_view_go_forward(IntPtr webView);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern int webkit_web_view_can_go_back(IntPtr webView);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern int webkit_web_view_can_go_forward(IntPtr webView);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_web_view_get_uri(IntPtr webView); // borrowed

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_web_view_get_title(IntPtr webView); // borrowed

	// URI request (for HttpRequestMessage navigation with headers)
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_uri_request_new([MarshalAs(UnmanagedType.LPUTF8Str)] string uri);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_uri_request_get_http_headers(IntPtr request); // may be NULL

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_uri_request_get_uri(IntPtr request); // borrowed

	// popup ("create" signal) support
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_navigation_action_get_request(IntPtr navigationAction);

	// libsoup 3 (only needed to append headers on a WebKitURIRequest)
	[DllImport("libsoup-3.0.so.0", CallingConvention = CallingConvention.Cdecl)]
	public static extern void soup_message_headers_append(IntPtr headers, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

	// JavaScript evaluation
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern unsafe void webkit_web_view_evaluate_javascript(IntPtr webView, [MarshalAs(UnmanagedType.LPUTF8Str)] string script, nint length, [MarshalAs(UnmanagedType.LPUTF8Str)] string? worldName, [MarshalAs(UnmanagedType.LPUTF8Str)] string? sourceUri, IntPtr cancellable, delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> callback, IntPtr userData);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr webkit_web_view_evaluate_javascript_finish(IntPtr webView, IntPtr result, out IntPtr error);

	// JSCValue (a GObject; unref when done)
	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern int jsc_value_is_null(IntPtr value);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern int jsc_value_is_undefined(IntPtr value);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern int jsc_value_is_string(IntPtr value);

	[DllImport(NativeLibraries.WpeWebKit, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr jsc_value_to_json(IntPtr value, uint indent); // owned; g_free
}
