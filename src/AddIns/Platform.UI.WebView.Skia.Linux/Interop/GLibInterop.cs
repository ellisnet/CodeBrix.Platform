using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;

/// <summary>
/// The minimal GLib/GObject surface this AddIn needs: a private main context + loop for the
/// dedicated WPE thread, cross-thread invokes, GObject refcounting/signals, and GValue-based
/// object construction (g_object_new is varargs, so g_object_new_with_properties is bound instead).
/// </summary>
internal static class GLibInterop
{
	public const int GSourceRemove = 0;
	public const int GPriorityDefault = 0;

	// GValue is { GType g_type; guint64 data[2]; } = 24 bytes on 64-bit.
	[StructLayout(LayoutKind.Sequential)]
	public struct GValue
	{
		public nuint GType;
		public ulong Data0;
		public ulong Data1;
	}

	// glib
	[DllImport(NativeLibraries.GLib, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr g_main_context_new();

	[DllImport(NativeLibraries.GLib, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_main_context_push_thread_default(IntPtr context);

	[DllImport(NativeLibraries.GLib, CallingConvention = CallingConvention.Cdecl)]
	public static extern unsafe void g_main_context_invoke_full(IntPtr context, int priority, delegate* unmanaged[Cdecl]<IntPtr, int> function, IntPtr data, IntPtr notify);

	[DllImport(NativeLibraries.GLib, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr g_main_loop_new(IntPtr context, int isRunning);

	[DllImport(NativeLibraries.GLib, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_main_loop_run(IntPtr loop);

	[DllImport(NativeLibraries.GLib, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_free(IntPtr mem);

	[DllImport(NativeLibraries.GLib, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_error_free(IntPtr error);

	// gobject
	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr g_object_ref(IntPtr @object);

	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_object_unref(IntPtr @object);

	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern ulong g_signal_connect_data(IntPtr instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string detailedSignal, IntPtr cHandler, IntPtr data, IntPtr destroyData, int connectFlags);

	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr g_object_new_with_properties(nuint objectType, uint nProperties, IntPtr[] names, GValue[] values);

	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_value_init(ref GValue value, nuint gType);

	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_value_set_boxed(ref GValue value, IntPtr boxed);

	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_value_set_object(ref GValue value, IntPtr @object);

	[DllImport(NativeLibraries.GObject, CallingConvention = CallingConvention.Cdecl)]
	public static extern void g_value_unset(ref GValue value);

	/// <summary>Reads the message field of a GError* (domain@0, code@4, message@8 on 64-bit).</summary>
	public static string? GetErrorMessage(IntPtr error)
	{
		if (error == IntPtr.Zero)
		{
			return null;
		}

		var messagePtr = Marshal.ReadIntPtr(error, 8);
		return Marshal.PtrToStringUTF8(messagePtr);
	}
}
