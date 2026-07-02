using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Hand-bound P/Invoke surface for libdecor's stable MIT C API (matching the installed
/// libdecor-0.so.0, v0.2.x). libdecor draws client-side window decorations (title bar,
/// buttons, interactive move/resize) on compositors that do not provide server-side
/// decorations — notably GNOME/Mutter, the primary Debian/Ubuntu target.
/// </summary>
/// <remarks>
/// The library is only P/Invoked at run time; its source is never vendored (runtime-stack
/// exception in the addendum's border rule). libdecor may itself dlopen a GTK/cairo plugin
/// to theme the decorations — that is OS-provided and outside our distribution.
/// </remarks>
internal static partial class LibDecor
{
	private const string LibName = "libdecor-0.so.0";

	internal enum libdecor_error
	{
		LIBDECOR_ERROR_COMPOSITOR_INCOMPATIBLE,
		LIBDECOR_ERROR_INVALID_FRAME_CONFIGURATION,
	}

	[Flags]
	internal enum libdecor_window_state
	{
		LIBDECOR_WINDOW_STATE_NONE = 0,
		LIBDECOR_WINDOW_STATE_ACTIVE = 1 << 0,
		LIBDECOR_WINDOW_STATE_MAXIMIZED = 1 << 1,
		LIBDECOR_WINDOW_STATE_FULLSCREEN = 1 << 2,
		LIBDECOR_WINDOW_STATE_TILED_LEFT = 1 << 3,
		LIBDECOR_WINDOW_STATE_TILED_RIGHT = 1 << 4,
		LIBDECOR_WINDOW_STATE_TILED_TOP = 1 << 5,
		LIBDECOR_WINDOW_STATE_TILED_BOTTOM = 1 << 6,
		LIBDECOR_WINDOW_STATE_SUSPENDED = 1 << 7,
	}

	// Native callback: void error(struct libdecor *context, enum libdecor_error error, const char *message)
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void LibDecorErrorDelegate(IntPtr context, libdecor_error error, IntPtr message);

	// Native callbacks on a struct libdecor_frame.
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void FrameConfigureDelegate(IntPtr frame, IntPtr configuration, IntPtr userData);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void FrameCloseDelegate(IntPtr frame, IntPtr userData);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void FrameCommitDelegate(IntPtr frame, IntPtr userData);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate void FrameDismissPopupDelegate(IntPtr frame, IntPtr seatName, IntPtr userData);

	// struct libdecor_interface { void (*error)(...); void (*reserved0..8)(void); }
	// The reserved slots are ABI padding libdecor validates against; they stay IntPtr.Zero.
	[StructLayout(LayoutKind.Sequential)]
	internal struct libdecor_interface
	{
		public IntPtr error;
		public IntPtr reserved0;
		public IntPtr reserved1;
		public IntPtr reserved2;
		public IntPtr reserved3;
		public IntPtr reserved4;
		public IntPtr reserved5;
		public IntPtr reserved6;
		public IntPtr reserved7;
		public IntPtr reserved8;
	}

	// struct libdecor_frame_interface {
	//   void (*configure)(...); void (*close)(...); void (*commit)(...);
	//   void (*dismiss_popup)(...); void (*reserved0..8)(void); }
	[StructLayout(LayoutKind.Sequential)]
	internal struct libdecor_frame_interface
	{
		public IntPtr configure;
		public IntPtr close;
		public IntPtr commit;
		public IntPtr dismiss_popup;
		public IntPtr reserved0;
		public IntPtr reserved1;
		public IntPtr reserved2;
		public IntPtr reserved3;
		public IntPtr reserved4;
		public IntPtr reserved5;
		public IntPtr reserved6;
		public IntPtr reserved7;
		public IntPtr reserved8;
	}

	// The iface pointers are RETAINED by libdecor and called through for the object's whole
	// lifetime (like wl_listener), so they must point at unmanaged memory that outlives the
	// context/frame — never at a managed struct pinned only for the call.
	[LibraryImport(LibName)]
	internal static partial IntPtr libdecor_new(IntPtr display, IntPtr iface);

	[LibraryImport(LibName)]
	internal static partial void libdecor_unref(IntPtr context);

	[LibraryImport(LibName)]
	internal static partial int libdecor_get_fd(IntPtr context);

	[LibraryImport(LibName)]
	internal static partial int libdecor_dispatch(IntPtr context, int timeout);

	[LibraryImport(LibName)]
	internal static partial IntPtr libdecor_decorate(IntPtr context, IntPtr surface, IntPtr iface, IntPtr userData);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_unref(IntPtr frame);

	[LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void libdecor_frame_set_title(IntPtr frame, string title);

	[LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void libdecor_frame_set_app_id(IntPtr frame, string appId);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_map(IntPtr frame);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_commit(IntPtr frame, IntPtr state, IntPtr configuration);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_set_minimized(IntPtr frame);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_set_maximized(IntPtr frame);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_unset_maximized(IntPtr frame);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_set_fullscreen(IntPtr frame, IntPtr output);

	[LibraryImport(LibName)]
	internal static partial void libdecor_frame_unset_fullscreen(IntPtr frame);

	[LibraryImport(LibName)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool libdecor_frame_is_floating(IntPtr frame);

	[LibraryImport(LibName)]
	internal static partial IntPtr libdecor_frame_get_xdg_surface(IntPtr frame);

	[LibraryImport(LibName)]
	internal static partial IntPtr libdecor_frame_get_xdg_toplevel(IntPtr frame);

	[LibraryImport(LibName)]
	internal static partial IntPtr libdecor_state_new(int width, int height);

	[LibraryImport(LibName)]
	internal static partial void libdecor_state_free(IntPtr state);

	[LibraryImport(LibName)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool libdecor_configuration_get_content_size(IntPtr configuration, IntPtr frame, out int width, out int height);

	[LibraryImport(LibName)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static partial bool libdecor_configuration_get_window_state(IntPtr configuration, out libdecor_window_state windowState);

	/// <summary>Probes whether libdecor-0.so.0 is present without throwing.</summary>
	internal static bool IsAvailable()
	{
		try
		{
			return NativeLibrary.TryLoad(LibName, out var handle) && Close(handle);
		}
		catch
		{
			return false;
		}

		static bool Close(IntPtr handle)
		{
			NativeLibrary.Free(handle);
			return true;
		}
	}
}
