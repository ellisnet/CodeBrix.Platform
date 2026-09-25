#nullable enable

namespace CodeBrix.Platform.UI.TerminalView.Engine;

/// <summary>
/// The modifier keys the terminal's key encoder tracks (WPE1 C6), the engine's own names: a host maps its platform's
/// key codes (WinUI VirtualKey, Android KeyEvent, ...) onto these and onto CodeBrix.Terminal's TerminalKey.
/// </summary>
internal enum TerminalModifierKey
{
	/// <summary>Not a modifier key.</summary>
	None = 0,

	/// <summary>Either Shift key.</summary>
	Shift,

	/// <summary>Either Control key.</summary>
	Control,

	/// <summary>Either Alt (Menu) key.</summary>
	Alt,

	/// <summary>Caps Lock (toggles on each press).</summary>
	CapsLock,
}
