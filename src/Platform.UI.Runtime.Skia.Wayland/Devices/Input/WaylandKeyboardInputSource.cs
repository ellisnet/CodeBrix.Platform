using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Per-window keyboard input source, fed by the seat manager following wl_keyboard focus.
/// Keymap parsing and key repeat live in <see cref="WaylandSeatManager"/> (they are
/// seat-level state); this class only shapes and raises the WinUI events.
/// </summary>
internal class WaylandKeyboardInputSource : ICodeBrixKeyboardInputSource
{
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;

	private readonly WaylandXamlRootHost _host;

	public WaylandKeyboardInputSource(IXamlRootHost host)
	{
		if (host is not WaylandXamlRootHost waylandHost)
		{
			throw new ArgumentException($"{nameof(host)} must be a Wayland host instance");
		}

		_host = waylandHost;
		_host.SetKeyboardSource(this);
	}

	// Runs on the Wayland event-pump thread (or the repeat-timer thread) and marshals to UI.
	internal void ProcessKeyEvent(bool pressed, VirtualKey virtualKey, VirtualKeyModifiers modifiers, uint scanCode, uint repeatCount, char? unicodeKey)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ProcessKeyEvent pressed={pressed}: {scanCode} -> {virtualKey} utf8:{unicodeKey}");
		}

		var args = new KeyEventArgs(
			"keyboard",
			virtualKey,
			modifiers,
			new CorePhysicalKeyStatus
			{
				ScanCode = scanCode,
				RepeatCount = repeatCount,
			},
			unicodeKey: unicodeKey);

		WaylandXamlRootHost.QueueAction(_host, () =>
		{
			if (pressed)
			{
				KeyDown?.Invoke(this, args);
			}
			else
			{
				KeyUp?.Invoke(this, args);
			}
		});
	}
}
