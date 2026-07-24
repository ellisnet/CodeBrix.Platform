using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

/// <summary>
/// The emulated device's keyboard. V1 of the CodeBrix.Develop emulator sends
/// no key messages at all — the stored "Hardware keyboard support" option is
/// not wired up yet — so in practice nothing is raised and the app behaves
/// like a kiosk with no keyboard attached. The INJECTION path below ships
/// dormant (and is advertised in the transport Hello's capability bits) so
/// that enabling keyboard forwarding later is purely an IDE-side change, with
/// no republish of this head package.
/// </summary>
internal class EmulatedKeyboardInputSource : ICodeBrixKeyboardInputSource
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	private IXamlRootHost? _host;

	// Modifier state is tracked HERE from the modifier keys' own down/up
	// messages (the wire deliberately carries no modifier field), mirroring
	// how the real head derives it from xkb state.
	private readonly HashSet<VirtualKey> _pressedModifiers = new();

	internal void SetHost(IXamlRootHost host) => _host = host;

	internal VirtualKeyModifiers GetCurrentModifiersState()
	{
		var modifiers = VirtualKeyModifiers.None;
		lock (_pressedModifiers)
		{
			foreach (var key in _pressedModifiers)
			{
				modifiers |= key switch
				{
					VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift => VirtualKeyModifiers.Shift,
					VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl => VirtualKeyModifiers.Control,
					VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu => VirtualKeyModifiers.Menu,
					VirtualKey.LeftWindows or VirtualKey.RightWindows => VirtualKeyModifiers.Windows,
					_ => VirtualKeyModifiers.None,
				};
			}
		}
		return modifiers;
	}

	/// <summary>
	/// Injects one emulated key transition, called on the transport's input
	/// thread. The shapes mirror the real head's xkb-driven raises: ScanCode
	/// carries the X11-style hardware keycode (which the WebView add-in relies
	/// on), and the Unicode codepoint rides the KeyDown for text input.
	/// </summary>
	public void ProcessEmulatedKey(bool pressed, uint virtualKey, uint hardwareKeyCode, uint unicodeCodepoint)
	{
		var key = (VirtualKey) virtualKey;
		lock (_pressedModifiers)
		{
			if (pressed)
			{
				_pressedModifiers.Add(key);
			}
			else
			{
				_pressedModifiers.Remove(key);
			}
		}

		// A codepoint above the BMP cannot ride the char-typed argument; text
		// that exotic goes through an on-screen input path instead.
		char? unicodeKey = pressed && unicodeCodepoint is > 0 and <= char.MaxValue
			? (char) unicodeCodepoint
			: null;

		var args = pressed
			? new KeyEventArgs(
				"keyboard",
				key,
				GetCurrentModifiersState(),
				new CorePhysicalKeyStatus
				{
					ScanCode = hardwareKeyCode,
					RepeatCount = 1,
				},
				unicodeKey)
			: new KeyEventArgs(
				"keyboard",
				key,
				GetCurrentModifiersState(),
				new CorePhysicalKeyStatus
				{
					ScanCode = hardwareKeyCode,
					RepeatCount = 1,
				});

		if (_host?.RootElement is { } rootElement)
		{
			_ = rootElement.Dispatcher.RunAsync(
				CoreDispatcherPriority.High,
				() => (pressed ? KeyDown : KeyUp)?.Invoke(this, args));
		}
	}
}
