#nullable enable

using System;
using System.Collections.Generic;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: no direct counterpart file - replaces System.Windows.Input.RoutedCommand for the
//editor. Like a RoutedCommand, an EditorCommand is a pure identity token: it carries a name and
//its default key gestures but no execute logic. Execution routes through EditorCommandBinding
//instances registered on a TextAreaInputHandler (see TextAreaInputHandler.cs), which also plays
//the WPF class-input-binding role by matching key presses against DefaultGestures of commands
//that have a command binding in the handler.

/// <summary>
/// Identifies an editor command. The command itself carries no execute logic; a command runs when
/// a <see cref="TextAreaInputHandler"/> holding an <see cref="EditorCommandBinding"/> for it
/// dispatches it (either from a matching key press or from
/// <see cref="TextAreaInputHandler.ExecuteCommand"/>).
/// </summary>
public sealed class EditorCommand
{
	/// <summary>
	/// Creates a new command.
	/// </summary>
	/// <param name="name">The name identifying the command (used for display/diagnostics).</param>
	/// <param name="defaultGestures">
	/// The key gestures that trigger the command wherever a command binding for it is registered
	/// and no explicit <see cref="KeyBinding"/> claimed the key press first. May be empty for
	/// commands that are only invoked programmatically or through explicit key bindings.
	/// </param>
	public EditorCommand(string name, params KeyGesture[] defaultGestures)
	{
		if (name == null)
			throw new ArgumentNullException(nameof(name));
		this.Name = name;
		if (defaultGestures == null || defaultGestures.Length == 0)
		{
			this.DefaultGestures = Array.Empty<KeyGesture>();
		}
		else
		{
			// Copy so no caller-held array can mutate the command after construction.
			this.DefaultGestures = Array.AsReadOnly((KeyGesture[])defaultGestures.Clone());
		}
	}

	/// <summary>
	/// Gets the name of the command.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the default key gestures of the command. Immutable; set at construction.
	/// </summary>
	public IReadOnlyList<KeyGesture> DefaultGestures { get; }

	/// <inheritdoc/>
	public override string ToString()
	{
		return Name;
	}
}
