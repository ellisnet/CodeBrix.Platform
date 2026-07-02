using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;

/// <summary>
/// Bindings for WPEBackend-fdo 1.16.x - the offscreen ("exportable") view backend. Initialized
/// in SHM mode, the web process renders on the CPU and every finished frame arrives through the
/// export_shm_buffer callback as a shared-memory buffer this process can read directly.
/// </summary>
internal static unsafe class WpeBackendFdo
{
	/// <summary>
	/// Native layout of struct wpe_view_backend_exportable_fdo_client (5 pointers). The instance
	/// passed to wpe_view_backend_exportable_fdo_create is retained by the backend, so it must
	/// live in unmanaged memory for the exportable's whole lifetime.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct ExportableClient
	{
		public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> ExportBufferResource;
		public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> ExportDmabufResource;
		public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> ExportShmBuffer;
		public IntPtr Reserved0;
		public IntPtr Reserved1;
	}

	[DllImport(NativeLibraries.WpeBackendFdo, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool wpe_fdo_initialize_shm();

	[DllImport(NativeLibraries.WpeBackendFdo, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr wpe_view_backend_exportable_fdo_create(ExportableClient* client, IntPtr data, uint width, uint height);

	[DllImport(NativeLibraries.WpeBackendFdo, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_exportable_fdo_destroy(IntPtr exportable);

	[DllImport(NativeLibraries.WpeBackendFdo, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr wpe_view_backend_exportable_fdo_get_view_backend(IntPtr exportable);

	[DllImport(NativeLibraries.WpeBackendFdo, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_exportable_fdo_dispatch_frame_complete(IntPtr exportable);

	[DllImport(NativeLibraries.WpeBackendFdo, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_exportable_fdo_dispatch_release_shm_exported_buffer(IntPtr exportable, IntPtr buffer);

	[DllImport(NativeLibraries.WpeBackendFdo, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr wpe_fdo_shm_exported_buffer_get_shm_buffer(IntPtr buffer);
}

/// <summary>
/// The wl_shm_buffer pixel accessors come from libwayland-server (WPEBackend-fdo links it but
/// does not re-export these). WL_SHM_FORMAT_ARGB8888 (0) / _XRGB8888 (1) are native-endian
/// packed pixels - byte order B,G,R,A on little-endian, i.e. Skia's Bgra8888.
/// </summary>
internal static class LibWaylandServer
{
	public const uint WlShmFormatArgb8888 = 0;
	public const uint WlShmFormatXrgb8888 = 1;

	[DllImport(NativeLibraries.WaylandServer, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wl_shm_buffer_begin_access(IntPtr buffer);

	[DllImport(NativeLibraries.WaylandServer, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wl_shm_buffer_end_access(IntPtr buffer);

	[DllImport(NativeLibraries.WaylandServer, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr wl_shm_buffer_get_data(IntPtr buffer);

	[DllImport(NativeLibraries.WaylandServer, CallingConvention = CallingConvention.Cdecl)]
	public static extern int wl_shm_buffer_get_stride(IntPtr buffer);

	[DllImport(NativeLibraries.WaylandServer, CallingConvention = CallingConvention.Cdecl)]
	public static extern uint wl_shm_buffer_get_format(IntPtr buffer);

	[DllImport(NativeLibraries.WaylandServer, CallingConvention = CallingConvention.Cdecl)]
	public static extern int wl_shm_buffer_get_width(IntPtr buffer);

	[DllImport(NativeLibraries.WaylandServer, CallingConvention = CallingConvention.Cdecl)]
	public static extern int wl_shm_buffer_get_height(IntPtr buffer);
}
