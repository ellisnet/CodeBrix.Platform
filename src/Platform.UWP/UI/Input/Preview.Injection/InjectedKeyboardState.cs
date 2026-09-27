#nullable enable

using System.Collections.Generic;
using CodeBrix.Platform.UI.Core;
using Windows.System;
using Windows.UI.Core;

namespace Windows.UI.Input.Preview.Injection;

/// <summary>
/// The keyboard of one <see cref="InputInjector"/>: turns each <see cref="InjectedInputKeyboardInfo"/> into the
/// <see cref="KeyEventArgs"/> a real keyboard source raises for the same key, so that an injected key takes the same
/// path as a real one (key events of the focused element, access keys, keyboard accelerators and text input).
/// </summary>
/// <remarks>
/// <para>The keys this injector holds down are tracked here, the way a keyboard source tracks its own keys, so the
/// modifier mask of every injected key (and of injected mouse and touch input) says which injected modifiers are
/// down. It is OR-ed with the modifiers the application's keyboard state already reports as held (a key held on the
/// real keyboard), as the system keyboard state is shared by real and injected input on Windows.</para>
/// <para>Text: a character key produces the character a US keyboard layout gives it (Shift and Caps Lock applied), and
/// none while Control, Alt or a Windows key is down; <see cref="InjectedInputKeyOptions.Unicode"/> types any UTF-16
/// code unit as the <c>VK_PACKET</c> key. This matches the character a real head delivers with the key.</para>
/// </remarks>
internal sealed class InjectedKeyboardState
{
	/// <summary>VK_PACKET: the virtual key of a key that carries a Unicode character (<see cref="InjectedInputKeyOptions.Unicode"/>).</summary>
	internal const VirtualKey PacketKey = (VirtualKey)0xE7;

	private const string ShiftedDigits = ")!@#$%^&*(";

	private readonly HashSet<VirtualKey> _pressed = new();

	/// <summary>The modifiers of the keys this injector holds down.</summary>
	internal VirtualKeyModifiers HeldModifiers { get; private set; }

	/// <summary>
	/// Applies one injected key transition and returns the event a keyboard source would raise for it, or
	/// <see langword="null"/> when the entry names no key (no virtual key, no scan code, no character).
	/// </summary>
	/// <param name="info">The injected key.</param>
	/// <param name="isDown"><see langword="true"/> for a key press, <see langword="false"/> for a release.</param>
	/// <returns>The event arguments, or <see langword="null"/>.</returns>
	internal KeyEventArgs? Apply(InjectedInputKeyboardInfo info, out bool isDown)
	{
		var options = info.KeyOptions;
		isDown = (options & InjectedInputKeyOptions.KeyUp) == 0;
		var isExtended = (options & InjectedInputKeyOptions.ExtendedKey) != 0;

		VirtualKey key;
		char? unicode = null;
		if ((options & InjectedInputKeyOptions.Unicode) != 0)
		{
			key = PacketKey;
			unicode = (char)info.ScanCode;
		}
		else if ((options & InjectedInputKeyOptions.ScanCode) != 0)
		{
			key = VirtualKeyFromScanCode(info.ScanCode, isExtended, IsLocked(VirtualKey.NumberKeyLock));
		}
		else
		{
			key = (VirtualKey)info.VirtualKey;
		}

		if (key == VirtualKey.None)
		{
			return null;
		}

		var wasDown = _pressed.Contains(key);
		if (isDown)
		{
			_pressed.Add(key);
		}
		else
		{
			_pressed.Remove(key);
		}

		HeldModifiers = ModifiersOf(_pressed);
		var modifiers = HeldModifiers | TrackedModifiers();

		if (isDown && unicode is null)
		{
			unicode = CharacterFor(key, modifiers, IsLocked(VirtualKey.CapitalLock));
		}

		var status = new CorePhysicalKeyStatus
		{
			RepeatCount = 1,
			ScanCode = info.ScanCode,
			IsExtendedKey = isExtended,
			IsMenuKeyDown = (modifiers & VirtualKeyModifiers.Menu) != 0,
			WasKeyDown = isDown ? wasDown : true,
			IsKeyReleased = !isDown,
		};

		return isDown
			? new KeyEventArgs("keyboard", key, modifiers, status, unicode)
			: new KeyEventArgs("keyboard", key, modifiers, status);
	}

	/// <summary>
	/// The character a key types on a US keyboard layout, or <see langword="null"/> for a key that types none, and for
	/// any key while Control, Alt (Menu) or a Windows key is held.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="modifiers">The modifiers held with it.</param>
	/// <param name="capsLock">Whether Caps Lock is on.</param>
	/// <returns>The character, or <see langword="null"/>.</returns>
	internal static char? CharacterFor(VirtualKey key, VirtualKeyModifiers modifiers, bool capsLock)
	{
		if ((modifiers & (VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu | VirtualKeyModifiers.Windows)) != 0)
		{
			return null;
		}

		var shift = (modifiers & VirtualKeyModifiers.Shift) != 0;
		var code = (int)key;
		return key switch
		{
			>= VirtualKey.A and <= VirtualKey.Z => (char)((shift ^ capsLock ? 'A' : 'a') + (code - (int)VirtualKey.A)),
			>= VirtualKey.Number0 and <= VirtualKey.Number9 => shift ? ShiftedDigits[code - (int)VirtualKey.Number0] : (char)('0' + (code - (int)VirtualKey.Number0)),
			>= VirtualKey.NumberPad0 and <= VirtualKey.NumberPad9 => (char)('0' + (code - (int)VirtualKey.NumberPad0)),
			VirtualKey.Space => ' ',
			VirtualKey.Enter => '\r',
			VirtualKey.Multiply => '*',
			VirtualKey.Add => '+',
			VirtualKey.Subtract => '-',
			VirtualKey.Decimal => '.',
			VirtualKey.Divide => '/',
			_ => code switch
			{
				0xBA => shift ? ':' : ';',
				0xBB => shift ? '+' : '=',
				0xBC => shift ? '<' : ',',
				0xBD => shift ? '_' : '-',
				0xBE => shift ? '>' : '.',
				0xBF => shift ? '?' : '/',
				0xC0 => shift ? '~' : '`',
				0xDB => shift ? '{' : '[',
				0xDC => shift ? '|' : '\\',
				0xDD => shift ? '}' : ']',
				0xDE => shift ? '"' : '\'',
				_ => null,
			},
		};
	}

	/// <summary>
	/// The virtual key of a scan code (PC/AT scan code set 1, US layout), as the system maps
	/// <see cref="InjectedInputKeyOptions.ScanCode"/> input; <see cref="VirtualKey.None"/> for an unknown code.
	/// </summary>
	/// <param name="scanCode">The scan code.</param>
	/// <param name="isExtended">Whether the key is an extended key (the E0 prefix).</param>
	/// <param name="numLock">Whether Num Lock is on (the keypad types digits rather than moving).</param>
	/// <returns>The virtual key.</returns>
	internal static VirtualKey VirtualKeyFromScanCode(ushort scanCode, bool isExtended, bool numLock)
	{
		if (isExtended)
		{
			return scanCode switch
			{
				0x1C => VirtualKey.Enter,
				0x1D => VirtualKey.Control,
				0x35 => VirtualKey.Divide,
				0x37 => VirtualKey.Snapshot,
				0x38 => VirtualKey.Menu,
				0x47 => VirtualKey.Home,
				0x48 => VirtualKey.Up,
				0x49 => VirtualKey.PageUp,
				0x4B => VirtualKey.Left,
				0x4D => VirtualKey.Right,
				0x4F => VirtualKey.End,
				0x50 => VirtualKey.Down,
				0x51 => VirtualKey.PageDown,
				0x52 => VirtualKey.Insert,
				0x53 => VirtualKey.Delete,
				0x5B => VirtualKey.LeftWindows,
				0x5C => VirtualKey.RightWindows,
				0x5D => VirtualKey.Application,
				_ => VirtualKey.None,
			};
		}

		return scanCode switch
		{
			0x01 => VirtualKey.Escape,
			>= 0x02 and <= 0x0A => VirtualKey.Number1 + (scanCode - 0x02),
			0x0B => VirtualKey.Number0,
			0x0C => (VirtualKey)0xBD,
			0x0D => (VirtualKey)0xBB,
			0x0E => VirtualKey.Back,
			0x0F => VirtualKey.Tab,
			0x10 => VirtualKey.Q,
			0x11 => VirtualKey.W,
			0x12 => VirtualKey.E,
			0x13 => VirtualKey.R,
			0x14 => VirtualKey.T,
			0x15 => VirtualKey.Y,
			0x16 => VirtualKey.U,
			0x17 => VirtualKey.I,
			0x18 => VirtualKey.O,
			0x19 => VirtualKey.P,
			0x1A => (VirtualKey)0xDB,
			0x1B => (VirtualKey)0xDD,
			0x1C => VirtualKey.Enter,
			0x1D => VirtualKey.Control,
			0x1E => VirtualKey.A,
			0x1F => VirtualKey.S,
			0x20 => VirtualKey.D,
			0x21 => VirtualKey.F,
			0x22 => VirtualKey.G,
			0x23 => VirtualKey.H,
			0x24 => VirtualKey.J,
			0x25 => VirtualKey.K,
			0x26 => VirtualKey.L,
			0x27 => (VirtualKey)0xBA,
			0x28 => (VirtualKey)0xDE,
			0x29 => (VirtualKey)0xC0,
			0x2A => VirtualKey.Shift,
			0x2B => (VirtualKey)0xDC,
			0x2C => VirtualKey.Z,
			0x2D => VirtualKey.X,
			0x2E => VirtualKey.C,
			0x2F => VirtualKey.V,
			0x30 => VirtualKey.B,
			0x31 => VirtualKey.N,
			0x32 => VirtualKey.M,
			0x33 => (VirtualKey)0xBC,
			0x34 => (VirtualKey)0xBE,
			0x35 => (VirtualKey)0xBF,
			0x36 => VirtualKey.Shift,
			0x37 => VirtualKey.Multiply,
			0x38 => VirtualKey.Menu,
			0x39 => VirtualKey.Space,
			0x3A => VirtualKey.CapitalLock,
			>= 0x3B and <= 0x44 => VirtualKey.F1 + (scanCode - 0x3B),
			0x45 => VirtualKey.NumberKeyLock,
			0x46 => VirtualKey.Scroll,
			0x47 => numLock ? VirtualKey.NumberPad7 : VirtualKey.Home,
			0x48 => numLock ? VirtualKey.NumberPad8 : VirtualKey.Up,
			0x49 => numLock ? VirtualKey.NumberPad9 : VirtualKey.PageUp,
			0x4A => VirtualKey.Subtract,
			0x4B => numLock ? VirtualKey.NumberPad4 : VirtualKey.Left,
			0x4C => numLock ? VirtualKey.NumberPad5 : VirtualKey.Clear,
			0x4D => numLock ? VirtualKey.NumberPad6 : VirtualKey.Right,
			0x4E => VirtualKey.Add,
			0x4F => numLock ? VirtualKey.NumberPad1 : VirtualKey.End,
			0x50 => numLock ? VirtualKey.NumberPad2 : VirtualKey.Down,
			0x51 => numLock ? VirtualKey.NumberPad3 : VirtualKey.PageDown,
			0x52 => numLock ? VirtualKey.NumberPad0 : VirtualKey.Insert,
			0x53 => numLock ? VirtualKey.Decimal : VirtualKey.Delete,
			0x57 => VirtualKey.F11,
			0x58 => VirtualKey.F12,
			_ => VirtualKey.None,
		};
	}

	private static VirtualKeyModifiers ModifiersOf(HashSet<VirtualKey> pressed)
	{
		var modifiers = VirtualKeyModifiers.None;
		foreach (var key in pressed)
		{
			modifiers |= ModifierOf(key);
		}

		return modifiers;
	}

	private static VirtualKeyModifiers ModifierOf(VirtualKey key) => key switch
	{
		VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift => VirtualKeyModifiers.Shift,
		VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl => VirtualKeyModifiers.Control,
		VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu => VirtualKeyModifiers.Menu,
		VirtualKey.LeftWindows or VirtualKey.RightWindows => VirtualKeyModifiers.Windows,
		_ => VirtualKeyModifiers.None,
	};

	/// <summary>The modifiers the application's keyboard state reports as held (by any keyboard).</summary>
	private static VirtualKeyModifiers TrackedModifiers()
	{
		var modifiers = VirtualKeyModifiers.None;
		if (IsDown(VirtualKey.Shift) || IsDown(VirtualKey.LeftShift) || IsDown(VirtualKey.RightShift))
		{
			modifiers |= VirtualKeyModifiers.Shift;
		}

		if (IsDown(VirtualKey.Control) || IsDown(VirtualKey.LeftControl) || IsDown(VirtualKey.RightControl))
		{
			modifiers |= VirtualKeyModifiers.Control;
		}

		if (IsDown(VirtualKey.Menu) || IsDown(VirtualKey.LeftMenu) || IsDown(VirtualKey.RightMenu))
		{
			modifiers |= VirtualKeyModifiers.Menu;
		}

		if (IsDown(VirtualKey.LeftWindows) || IsDown(VirtualKey.RightWindows))
		{
			modifiers |= VirtualKeyModifiers.Windows;
		}

		return modifiers;
	}

	private static bool IsDown(VirtualKey key)
		=> (KeyboardStateTracker.GetKeyState(key) & CoreVirtualKeyStates.Down) != 0;

	private static bool IsLocked(VirtualKey key)
		=> (KeyboardStateTracker.GetKeyState(key) & CoreVirtualKeyStates.Locked) != 0;
}
