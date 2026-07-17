#nullable enable

using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.UI.XamlHost.Skia.Wpf;

/// <summary>
/// user32 raw-input and cursor-clipping imports used only by the relative mouse session
/// (see <see cref="WpfCorePointerInputSource.StartRelativeMouse"/>). Nothing here runs
/// unless a session is active.
/// </summary>
internal static class RelativeMouseNativeMethods
{
	internal const int WM_MOVE = 0x0003;
	internal const int WM_SIZE = 0x0005;
	internal const int WM_ACTIVATE = 0x0006;
	internal const int WM_INPUT = 0x00FF;
	internal const int WA_INACTIVE = 0;
	internal const uint RID_INPUT = 0x10000003;
	internal const uint RIM_TYPEMOUSE = 0;
	internal const ushort MOUSE_MOVE_ABSOLUTE = 0x0001;
	internal const uint RIDEV_REMOVE = 0x00000001;
	internal const ushort HID_USAGE_PAGE_GENERIC = 0x01;
	internal const ushort HID_USAGE_GENERIC_MOUSE = 0x02;

	[StructLayout(LayoutKind.Sequential)]
	internal struct RAWINPUTDEVICE
	{
		public ushort usUsagePage;
		public ushort usUsage;
		public uint dwFlags;
		public IntPtr hwndTarget;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RAWINPUTHEADER
	{
		public uint dwType;
		public uint dwSize;
		public IntPtr hDevice;
		public IntPtr wParam;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RAWMOUSE
	{
		public ushort usFlags;
		public uint ulButtons; // union of ulButtons and (usButtonFlags, usButtonData)
		public uint ulRawButtons;
		public int lLastX;
		public int lLastY;
		public uint ulExtraInformation;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct RAWINPUT
	{
		public RAWINPUTHEADER header;
		public RAWMOUSE mouse; // the union's mouse arm; keyboard/hid devices are never registered
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct NativeRect
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct NativePoint
	{
		public int X;
		public int Y;
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, ref RAWINPUT pData, ref uint pcbSize, uint cbSizeHeader);

	[DllImport("user32.dll", SetLastError = true, EntryPoint = "ClipCursor")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool ClipCursor(ref NativeRect lpRect);

	[DllImport("user32.dll", SetLastError = true, EntryPoint = "ClipCursor")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool ClipCursorRelease(IntPtr lpRect); // pass IntPtr.Zero to release

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetClientRect(IntPtr hWnd, out NativeRect lpRect);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint lpPoint);
}
