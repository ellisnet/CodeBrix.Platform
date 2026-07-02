using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Hand-bound P/Invoke surface for libwayland-cursor (X11/MIT-family license, runtime-linked
/// only — addendum Part D). Loads XCursor themes and exposes their images as wl_buffers, used
/// as the pointer-cursor fallback on compositors that do not advertise cursor-shape-v1
/// (notably Muffin/Cinnamon; GNOME and KDE do advertise it).
/// </summary>
internal static partial class LibWaylandCursor
{
	private const string LibName = "libwayland-cursor.so.0";

	// struct wl_cursor_image { uint32_t width, height, hotspot_x, hotspot_y, delay; }
	[StructLayout(LayoutKind.Sequential)]
	internal struct wl_cursor_image
	{
		public uint width;
		public uint height;
		public uint hotspot_x;
		public uint hotspot_y;
		public uint delay;
	}

	// struct wl_cursor { unsigned int image_count; struct wl_cursor_image **images; char *name; }
	[StructLayout(LayoutKind.Sequential)]
	internal struct wl_cursor
	{
		public uint image_count;
		public IntPtr images;
		public IntPtr name;
	}

	[LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
	internal static partial IntPtr wl_cursor_theme_load(string? name, int size, IntPtr shm);

	[LibraryImport(LibName)]
	internal static partial void wl_cursor_theme_destroy(IntPtr theme);

	[LibraryImport(LibName, StringMarshalling = StringMarshalling.Utf8)]
	internal static partial IntPtr wl_cursor_theme_get_cursor(IntPtr theme, string name);

	// The returned wl_buffer belongs to the theme; it must NOT be destroyed by the caller.
	[LibraryImport(LibName)]
	internal static partial IntPtr wl_cursor_image_get_buffer(IntPtr image);

	/// <summary>
	/// Resolves the first image of the named cursor: its theme-owned wl_buffer, size, and
	/// hotspot. Returns false when the theme has no such cursor.
	/// </summary>
	internal static bool TryGetCursorImage(IntPtr theme, string name,
		out IntPtr buffer, out int width, out int height, out int hotspotX, out int hotspotY)
	{
		buffer = IntPtr.Zero;
		width = height = hotspotX = hotspotY = 0;

		var cursorPtr = wl_cursor_theme_get_cursor(theme, name);
		if (cursorPtr == IntPtr.Zero)
		{
			return false;
		}

		var cursor = Marshal.PtrToStructure<wl_cursor>(cursorPtr);
		if (cursor.image_count == 0 || cursor.images == IntPtr.Zero)
		{
			return false;
		}

		// Animated cursors have several images; the first frame suffices for a static shape
		// (the same simplification SDL and GLFW make for non-animated use).
		var imagePtr = Marshal.ReadIntPtr(cursor.images);
		if (imagePtr == IntPtr.Zero)
		{
			return false;
		}

		var image = Marshal.PtrToStructure<wl_cursor_image>(imagePtr);
		buffer = wl_cursor_image_get_buffer(imagePtr);
		if (buffer == IntPtr.Zero)
		{
			return false;
		}

		width = (int)image.width;
		height = (int)image.height;
		hotspotX = (int)image.hotspot_x;
		hotspotY = (int)image.hotspot_y;
		return true;
	}
}
