#nullable enable

using System;

using Windows.System;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: no direct counterpart file - replaces System.Windows.Input.KeyBinding (and its
//InputBinding base) for the editor's own command system. Instances are immutable, so they are
//freely shareable between editor instances; that replaces both the upstream Freezable trick
//(TextAreaDefaultInputHandler.CreateFrozenKeyBinding) and the WPF memory-leak workaround
//(WorkaroundWPFMemoryLeak), which are not ported.

/// <summary>
/// Binds a <see cref="KeyGesture"/> to an <see cref="EditorCommand"/>: when the gesture
/// matches a key press, the command is dispatched through the owning
/// <see cref="TextAreaInputHandler"/>'s command bindings.
/// </summary>
public sealed class KeyBinding
{
	/// <summary>
	/// Creates a new key binding.
	/// </summary>
	/// <param name="command">The command to run when the gesture matches.</param>
	/// <param name="gesture">The key gesture that triggers the command.</param>
	public KeyBinding(EditorCommand command, KeyGesture gesture)
		: this(command, gesture, null)
	{
	}

	/// <summary>
	/// Creates a new key binding.
	/// </summary>
	/// <param name="command">The command to run when the gesture matches.</param>
	/// <param name="key">The key of the gesture.</param>
	/// <param name="modifiers">The modifier keys of the gesture.</param>
	public KeyBinding(EditorCommand command, VirtualKey key, VirtualKeyModifiers modifiers)
		: this(command, new KeyGesture(key, modifiers), null)
	{
	}

	/// <summary>
	/// Creates a new key binding with a command parameter.
	/// </summary>
	/// <param name="command">The command to run when the gesture matches.</param>
	/// <param name="gesture">The key gesture that triggers the command.</param>
	/// <param name="commandParameter">The parameter passed to the command when it runs.</param>
	public KeyBinding(EditorCommand command, KeyGesture gesture, object? commandParameter)
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));
		this.Command = command;
		this.Gesture = gesture;
		this.CommandParameter = commandParameter;
	}

	/// <summary>
	/// Gets the command this key binding triggers.
	/// </summary>
	public EditorCommand Command { get; }

	/// <summary>
	/// Gets the key gesture that triggers the command.
	/// </summary>
	public KeyGesture Gesture { get; }

	/// <summary>
	/// Gets the parameter passed to the command when it runs, or null.
	/// </summary>
	public object? CommandParameter { get; }
}
