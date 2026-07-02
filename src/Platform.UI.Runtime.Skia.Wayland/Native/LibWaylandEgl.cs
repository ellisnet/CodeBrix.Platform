using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// P/Invoke for libwayland-egl — the small shim that wraps a wl_surface in a native window
/// EGL can create a rendering surface against, for the GPU (EGL/GLES) render path.
/// </summary>
internal static partial class LibWaylandEgl
{
	private const string LibName = "libwayland-egl.so.1";

	[LibraryImport(LibName)]
	internal static partial IntPtr wl_egl_window_create(IntPtr surface, int width, int height);

	[LibraryImport(LibName)]
	internal static partial void wl_egl_window_destroy(IntPtr egl_window);

	[LibraryImport(LibName)]
	internal static partial void wl_egl_window_resize(IntPtr egl_window, int width, int height, int dx, int dy);
}
