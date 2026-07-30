#nullable enable

using System.Text;

using Windows.System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: no direct counterpart file - replaces System.Windows.Input.KeyGesture for the
//editor's own command system (see EditorCommand.cs). Matching keeps WPF KeyGesture semantics:
//the pressed key and the exact active modifier set must both equal the gesture's values.

/// <summary>
/// An immutable keyboard shortcut: one key plus the exact set of modifier keys that must be
/// active for the gesture to match.
/// </summary>
/// <param name="Key">The (non-modifier) key of the shortcut.</param>
/// <param name="Modifiers">The modifier keys of the shortcut.</param>
public readonly record struct KeyGesture(VirtualKey Key, VirtualKeyModifiers Modifiers)
{
	/// <summary>
	/// Creates a gesture with no modifier keys.
	/// </summary>
	/// <param name="key">The key of the shortcut.</param>
	public KeyGesture(VirtualKey key)
		: this(key, VirtualKeyModifiers.None)
	{
	}

	/// <summary>
	/// Gets whether this gesture matches the given key press. The pressed key must equal
	/// <see cref="Key"/> and the active modifiers must equal <see cref="Modifiers"/> exactly:
	/// Ctrl+X does not match the plain X gesture, and plain X does not match the Ctrl+X gesture.
	/// </summary>
	/// <param name="key">The key that was pressed.</param>
	/// <param name="activeModifiers">The modifier keys that were active for the key press.</param>
	public bool Matches(VirtualKey key, VirtualKeyModifiers activeModifiers)
	{
		return key == Key && activeModifiers == Modifiers;
	}

	/// <summary>
	/// Returns a display string for the gesture, e.g. "Ctrl+Shift+Z".
	/// </summary>
	public override string ToString()
	{
		StringBuilder b = new StringBuilder();
		if ((Modifiers & VirtualKeyModifiers.Control) != 0)
			b.Append("Ctrl+");
		if ((Modifiers & VirtualKeyModifiers.Menu) != 0)
			b.Append("Alt+");
		if ((Modifiers & VirtualKeyModifiers.Shift) != 0)
			b.Append("Shift+");
		if ((Modifiers & VirtualKeyModifiers.Windows) != 0)
			b.Append("Win+");
		b.Append(Key.ToString());
		return b.ToString();
	}
}
