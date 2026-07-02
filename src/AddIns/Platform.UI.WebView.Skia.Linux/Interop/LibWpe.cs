using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux.Interop;

/// <summary>
/// Bindings for libwpe-1.0 (1.16.x). Struct layouts and semantics follow the C headers at tag
/// 1.16.2; see the WPE API notes in this AddIn's PORTING-NOTES for provenance. All structs are
/// x86-64/SysV layouts, which also hold on arm64 and riscv64 (4-byte int/enum, 1-byte bool).
/// </summary>
internal static class LibWpe
{
	// enum wpe_input_modifier
	public const uint ModifierControl = 1u << 0;
	public const uint ModifierShift = 1u << 1;
	public const uint ModifierAlt = 1u << 2;
	public const uint ModifierMeta = 1u << 3;
	public const uint ModifierPointerButton1 = 1u << 20;
	public const uint ModifierPointerButton2 = 1u << 21;
	public const uint ModifierPointerButton3 = 1u << 22;
	public const uint ModifierPointerButton4 = 1u << 23;
	public const uint ModifierPointerButton5 = 1u << 24;

	// enum wpe_input_pointer_event_type
	public const int PointerEventTypeNull = 0;
	public const int PointerEventTypeMotion = 1;
	public const int PointerEventTypeButton = 2;

	// WebKit's WPE button numbering (NOT the X11 convention): 1 = Left, 2 = Right, 3 = Middle.
	public const uint PointerButtonLeft = 1;
	public const uint PointerButtonRight = 2;
	public const uint PointerButtonMiddle = 3;

	// enum wpe_input_axis_event_type
	public const int AxisEventTypeMotion = 1;
	public const int AxisEventTypeMotionSmooth = 2;
	public const int AxisEventTypeMask2D = 1 << 16;

	// enum wpe_view_activity_state
	public const uint ActivityStateVisible = 1u << 0;
	public const uint ActivityStateFocused = 1u << 1;
	public const uint ActivityStateInWindow = 1u << 2;

	[StructLayout(LayoutKind.Sequential)]
	public struct WpeInputKeyboardEvent
	{
		public uint Time;
		public uint KeyCode; // an X11/xkb keysym ("keyval")
		public uint HardwareKeyCode; // X11-style keycode = evdev scancode + 8
		public byte Pressed; // C99 bool: 1 byte
		private byte _pad0;
		private byte _pad1;
		private byte _pad2;
		public uint Modifiers;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WpeInputPointerEvent
	{
		public int Type;
		public uint Time;
		public int X; // physical (backend) pixels - pre-scaled by device scale factor
		public int Y;
		public uint Button;
		public uint State; // 1 = pressed, 0 = released
		public uint Modifiers;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct WpeInputAxisEvent
	{
		public int Type;
		public uint Time;
		public int X;
		public int Y;
		public uint Axis; // 0 = vertical, 1 = horizontal (unused by the 2D variant)
		public int Value;
		public uint Modifiers;
	}

	// 48 bytes: 28-byte base + 4 padding + two doubles. WebKit downcasts based on the
	// mask_2d bit in base.Type, so the full struct must be passed to dispatch_axis_event.
	[StructLayout(LayoutKind.Explicit, Size = 48)]
	public struct WpeInputAxis2DEvent
	{
		[FieldOffset(0)] public WpeInputAxisEvent Base;
		[FieldOffset(32)] public double XAxis; // physical pixels; positive y scrolls content up
		[FieldOffset(40)] public double YAxis;
	}

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool wpe_loader_init([MarshalAs(UnmanagedType.LPUTF8Str)] string implLibraryName);

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_dispatch_set_size(IntPtr backend, uint width, uint height);

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_dispatch_set_device_scale_factor(IntPtr backend, float scale);

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_dispatch_keyboard_event(IntPtr backend, ref WpeInputKeyboardEvent @event);

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_dispatch_pointer_event(IntPtr backend, ref WpeInputPointerEvent @event);

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_dispatch_axis_event(IntPtr backend, ref WpeInputAxis2DEvent @event);

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_add_activity_state(IntPtr backend, uint state);

	[DllImport(NativeLibraries.LibWpe, CallingConvention = CallingConvention.Cdecl)]
	public static extern void wpe_view_backend_remove_activity_state(IntPtr backend, uint state);
}
