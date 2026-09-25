#nullable enable

namespace CodeBrix.Platform.UI.TerminalView.Engine;

/// <summary>
/// A key chord the terminal handles itself instead of sending it to the host application (WPE1 C6): the
/// terminal-conventional clipboard chords and the scrollback paging keys.
/// </summary>
internal enum TerminalKeyCommand
{
	/// <summary>Not a command: the key is encoded and sent.</summary>
	None = 0,

	/// <summary>Ctrl+Shift+C: copy the selection.</summary>
	Copy,

	/// <summary>Ctrl+Shift+V: paste the clipboard text.</summary>
	Paste,

	/// <summary>Shift+PageUp: one page back into the scrollback.</summary>
	ScrollPageUp,

	/// <summary>Shift+PageDown: one page forward.</summary>
	ScrollPageDown,
}
