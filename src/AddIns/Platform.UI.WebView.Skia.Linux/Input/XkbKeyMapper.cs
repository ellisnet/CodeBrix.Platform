using Windows.System;

namespace CodeBrix.Platform.UI.WebView.Skia.Linux.Input;

/// <summary>
/// Maps WinUI key event data to the XKB keysyms that WPE WebKit expects.
/// </summary>
/// <remarks>
/// The values are the standard X11 keysym constants (keysymdef.h); they are a stable ABI.
/// For printable characters the XKB rule is: code points U+0020..U+007E and U+00A0..U+00FF
/// are their own keysym; anything else is the code point OR'ed with 0x01000000.
/// </remarks>
internal static class XkbKeyMapper
{
	public static uint KeysymFromChar(char c)
	{
		uint cp = c;
		if (cp is >= 0x20 and <= 0x7E or >= 0xA0 and <= 0xFF)
		{
			return cp;
		}

		// Control characters that arrive as chars still need their function keysym.
		return cp switch
		{
			0x08 => 0xff08, // BackSpace
			0x09 => 0xff09, // Tab
			0x0A or 0x0D => 0xff0d, // Return
			0x1B => 0xff1b, // Escape
			0x7F => 0xffff, // Delete
			_ => cp | 0x01000000,
		};
	}

	/// <summary>Maps non-printable/navigation keys; returns 0 when the key has no function keysym.</summary>
	public static uint KeysymFromVirtualKey(VirtualKey key) => key switch
	{
		VirtualKey.Back => 0xff08, // BackSpace
		VirtualKey.Tab => 0xff09,
		VirtualKey.Enter => 0xff0d,
		VirtualKey.Escape => 0xff1b,
		VirtualKey.Space => 0x0020,
		VirtualKey.PageUp => 0xff55, // Prior
		VirtualKey.PageDown => 0xff56, // Next
		VirtualKey.End => 0xff57,
		VirtualKey.Home => 0xff50,
		VirtualKey.Left => 0xff51,
		VirtualKey.Up => 0xff52,
		VirtualKey.Right => 0xff53,
		VirtualKey.Down => 0xff54,
		VirtualKey.Insert => 0xff63,
		VirtualKey.Delete => 0xffff,
		VirtualKey.Pause => 0xff13,
		VirtualKey.CapitalLock => 0xffe5, // Caps_Lock
		VirtualKey.NumberKeyLock => 0xff7f, // Num_Lock
		VirtualKey.Scroll => 0xff14, // Scroll_Lock
		VirtualKey.Snapshot => 0xff61, // Print
		VirtualKey.Application => 0xff67, // Menu
		VirtualKey.Shift or VirtualKey.LeftShift => 0xffe1, // Shift_L
		VirtualKey.RightShift => 0xffe2,
		VirtualKey.Control or VirtualKey.LeftControl => 0xffe3, // Control_L
		VirtualKey.RightControl => 0xffe4,
		VirtualKey.Menu or VirtualKey.LeftMenu => 0xffe9, // Alt_L
		VirtualKey.RightMenu => 0xffea, // Alt_R
		VirtualKey.LeftWindows => 0xffeb, // Super_L
		VirtualKey.RightWindows => 0xffec, // Super_R
		VirtualKey.F1 => 0xffbe,
		VirtualKey.F2 => 0xffbf,
		VirtualKey.F3 => 0xffc0,
		VirtualKey.F4 => 0xffc1,
		VirtualKey.F5 => 0xffc2,
		VirtualKey.F6 => 0xffc3,
		VirtualKey.F7 => 0xffc4,
		VirtualKey.F8 => 0xffc5,
		VirtualKey.F9 => 0xffc6,
		VirtualKey.F10 => 0xffc7,
		VirtualKey.F11 => 0xffc8,
		VirtualKey.F12 => 0xffc9,
		VirtualKey.NumberPad0 => 0xffb0, // KP_0
		VirtualKey.NumberPad1 => 0xffb1,
		VirtualKey.NumberPad2 => 0xffb2,
		VirtualKey.NumberPad3 => 0xffb3,
		VirtualKey.NumberPad4 => 0xffb4,
		VirtualKey.NumberPad5 => 0xffb5,
		VirtualKey.NumberPad6 => 0xffb6,
		VirtualKey.NumberPad7 => 0xffb7,
		VirtualKey.NumberPad8 => 0xffb8,
		VirtualKey.NumberPad9 => 0xffb9,
		VirtualKey.Multiply => 0xffaa, // KP_Multiply
		VirtualKey.Add => 0xffab, // KP_Add
		VirtualKey.Separator => 0xffac, // KP_Separator
		VirtualKey.Subtract => 0xffad, // KP_Subtract
		VirtualKey.Decimal => 0xffae, // KP_Decimal
		VirtualKey.Divide => 0xffaf, // KP_Divide
		_ => 0,
	};
}
