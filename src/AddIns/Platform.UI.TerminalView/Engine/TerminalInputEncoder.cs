#nullable enable

using CodeBrix.Terminal.Engine;

namespace CodeBrix.Platform.UI.TerminalView.Engine;

/// <summary>
/// The terminal's key encoder (WPE1 C6): tracks the modifier state, recognises the chords the terminal handles itself
/// (<see cref="TerminalKeyCommand"/>) and turns every other key into the VT byte sequence the host application expects,
/// through CodeBrix.Terminal's <see cref="TerminalKeyEncoder"/>. Its input is the engine's own key names
/// (<see cref="TerminalKey"/>, <see cref="TerminalModifierKey"/>) plus the character the platform's keyboard layout
/// composed, so it names no platform key type; a host maps its key codes onto those.
/// </summary>
internal sealed class TerminalInputEncoder
{
	private bool _shiftDown;
	private bool _controlDown;
	private bool _altDown;
	private bool _capsLock;
	private char? _pendingHighSurrogate;

	/// <summary>Whether a Shift key is down.</summary>
	internal bool ShiftDown => _shiftDown;

	/// <summary>Whether a Control key is down.</summary>
	internal bool ControlDown => _controlDown;

	/// <summary>Whether an Alt key is down.</summary>
	internal bool AltDown => _altDown;

	/// <summary>Whether Caps Lock is on.</summary>
	internal bool CapsLock => _capsLock;

	/// <summary>The current modifiers in CodeBrix.Terminal's terms.</summary>
	internal TerminalModifiers Modifiers
	{
		get
		{
			var modifiers = TerminalModifiers.None;
			if (_shiftDown) { modifiers |= TerminalModifiers.Shift; }
			if (_controlDown) { modifiers |= TerminalModifiers.Control; }
			if (_altDown) { modifiers |= TerminalModifiers.Alt; }
			if (_capsLock) { modifiers |= TerminalModifiers.CapsLock; }
			return modifiers;
		}
	}

	/// <summary>
	/// Records a modifier key going down or up (Caps Lock toggles on each press). Returns true when the key IS a
	/// modifier: the host then does nothing else with the key.
	/// </summary>
	/// <param name="key">The modifier (None for any other key).</param>
	/// <param name="isDown">True for key down, false for key up.</param>
	/// <returns>Whether the key was a modifier.</returns>
	internal bool UpdateModifier(TerminalModifierKey key, bool isDown)
	{
		switch (key)
		{
			case TerminalModifierKey.Shift:
				_shiftDown = isDown;
				return true;

			case TerminalModifierKey.Control:
				_controlDown = isDown;
				return true;

			case TerminalModifierKey.Alt:
				_altDown = isDown;
				return true;

			case TerminalModifierKey.CapsLock:
				if (isDown) { _capsLock = !_capsLock; }
				return true;

			default:
				return false;
		}
	}

	/// <summary>The chord a key press makes with the current modifiers, if the terminal handles it itself.</summary>
	/// <param name="key">The key pressed.</param>
	/// <returns>The command, or <see cref="TerminalKeyCommand.None"/>.</returns>
	internal TerminalKeyCommand GetCommand(TerminalKey key)
	{
		//Ctrl+Shift+C / Ctrl+Shift+V are the terminal-conventional clipboard
		//chords (never reach the shell as input)
		if (_controlDown && _shiftDown && key == TerminalKey.C) { return TerminalKeyCommand.Copy; }
		if (_controlDown && _shiftDown && key == TerminalKey.V) { return TerminalKeyCommand.Paste; }

		//Shift+PageUp/PageDown page through the scrollback
		if (_shiftDown && key == TerminalKey.PageUp) { return TerminalKeyCommand.ScrollPageUp; }
		if (_shiftDown && key == TerminalKey.PageDown) { return TerminalKeyCommand.ScrollPageDown; }

		return TerminalKeyCommand.None;
	}

	/// <summary>
	/// Encodes a key press as the VT sequence to send, or null when the key sends nothing.
	/// </summary>
	/// <param name="key">The key pressed (<see cref="TerminalKey.None"/> for a key with no terminal meaning).</param>
	/// <param name="composed">The character the platform's keyboard layout composed for the press, if any: preferred
	/// for printable keys, since the raw key cannot see shifted digit-row symbols like '(' on non-US layouts.</param>
	/// <param name="applicationCursor">Whether the terminal's cursor keys are in application mode (DECCKM).</param>
	/// <returns>The sequence, or null.</returns>
	/// <remarks>
	/// A character outside the Basic Multilingual Plane (an emoji, for one) arrives as TWO presses, each composing one
	/// UTF-16 half (KeyRoutedEventArgs.UnicodeKey is a single char). The high half is held back (null is returned for it)
	/// and sent together with the low half as ONE string, so a host never receives a lone surrogate. A half without its
	/// partner is dropped.
	/// </remarks>
	internal string? Encode(TerminalKey key, char? composed, bool applicationCursor)
	{
		if (composed is { } half && char.IsSurrogate(half))
		{
			return PairSurrogate(half);
		}

		//Any other key abandons a high half still waiting for its partner
		_pendingHighSurrogate = null;

		var modifiers = Modifiers;

		//Chords go through the full key mapping (Ctrl -> C0 codes, Alt -> ESC prefix)
		if (_controlDown || _altDown)
		{
			return TerminalKeyEncoder.Encode(key, modifiers, applicationCursor);
		}

		//Shift+Tab must reach Encode to become back-tab (EncodeSpecial is modifier-free)
		if (key == TerminalKey.Tab && _shiftDown)
		{
			return TerminalKeyEncoder.Encode(key, modifiers, applicationCursor);
		}

		var special = TerminalKeyEncoder.EncodeSpecial(key, applicationCursor);
		if (special != null) { return special; }

		//Printables: prefer the platform's layout-composed character
		if (composed is { } character)
		{
			var encoded = TerminalKeyEncoder.EncodeComposed(character, modifiers);
			if (encoded != null) { return encoded; }
		}

		return TerminalKeyEncoder.Encode(key, modifiers, applicationCursor);
	}

	/// <summary>
	/// One UTF-16 half of a non-BMP character: a high half is held until its low half arrives; the low half completes the
	/// pair and the whole character is returned. A low half with no high half before it, or a high half replacing one
	/// that never got its partner, is dropped.
	/// </summary>
	/// <param name="half">The surrogate the press composed.</param>
	/// <returns>The complete character as a string, or null while it is not complete.</returns>
	private string? PairSurrogate(char half)
	{
		if (char.IsHighSurrogate(half))
		{
			_pendingHighSurrogate = half;
			return null;
		}

		if (_pendingHighSurrogate is { } high)
		{
			_pendingHighSurrogate = null;
			return new string(new[] { high, half });
		}

		return null;
	}
}
