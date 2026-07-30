#nullable enable

using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit;

//was previously: ICSharpCode.AvalonEdit/AvalonEditCommands.cs in the AvalonEdit repo (MIT),
//where the class was named AvalonEditCommands; renamed per the port naming rules. The WPF
//RoutedCommands (with their InputGestureCollections) became this port's EditorCommand instances
//with the same default gestures; the upstream CA1702 suppressions on the *Whitespace commands
//are not needed here.

/// <summary>
/// Custom commands for the editor. Each command is an identity token; the editing input handler
/// (part of <see cref="Editing.TextAreaDefaultInputHandler"/>) provides the implementations.
/// </summary>
public static class AdvancedTextEditCommands
{
	/// <summary>
	/// Toggles Overstrike mode
	/// The default shortcut is Ins.
	/// </summary>
	public static readonly EditorCommand ToggleOverstrike = new EditorCommand(
		"ToggleOverstrike",
		new KeyGesture(VirtualKey.Insert));

	/// <summary>
	/// Deletes the current line.
	/// The default shortcut is Ctrl+D.
	/// </summary>
	public static readonly EditorCommand DeleteLine = new EditorCommand(
		"DeleteLine",
		new KeyGesture(VirtualKey.D, VirtualKeyModifiers.Control));

	/// <summary>
	/// Removes leading whitespace from the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static readonly EditorCommand RemoveLeadingWhitespace = new EditorCommand("RemoveLeadingWhitespace");

	/// <summary>
	/// Removes trailing whitespace from the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static readonly EditorCommand RemoveTrailingWhitespace = new EditorCommand("RemoveTrailingWhitespace");

	/// <summary>
	/// Converts the selected text to upper case.
	/// </summary>
	public static readonly EditorCommand ConvertToUppercase = new EditorCommand("ConvertToUppercase");

	/// <summary>
	/// Converts the selected text to lower case.
	/// </summary>
	public static readonly EditorCommand ConvertToLowercase = new EditorCommand("ConvertToLowercase");

	/// <summary>
	/// Converts the selected text to title case.
	/// </summary>
	public static readonly EditorCommand ConvertToTitleCase = new EditorCommand("ConvertToTitleCase");

	/// <summary>
	/// Inverts the case of the selected text.
	/// </summary>
	public static readonly EditorCommand InvertCase = new EditorCommand("InvertCase");

	/// <summary>
	/// Converts tabs to spaces in the selected text.
	/// </summary>
	public static readonly EditorCommand ConvertTabsToSpaces = new EditorCommand("ConvertTabsToSpaces");

	/// <summary>
	/// Converts spaces to tabs in the selected text.
	/// </summary>
	public static readonly EditorCommand ConvertSpacesToTabs = new EditorCommand("ConvertSpacesToTabs");

	/// <summary>
	/// Converts leading tabs to spaces in the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static readonly EditorCommand ConvertLeadingTabsToSpaces = new EditorCommand("ConvertLeadingTabsToSpaces");

	/// <summary>
	/// Converts leading spaces to tabs in the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static readonly EditorCommand ConvertLeadingSpacesToTabs = new EditorCommand("ConvertLeadingSpacesToTabs");

	/// <summary>
	/// Runs the IIndentationStrategy on the selected lines (or the whole document if the selection is empty).
	/// </summary>
	public static readonly EditorCommand IndentSelection = new EditorCommand(
		"IndentSelection",
		new KeyGesture(VirtualKey.I, VirtualKeyModifiers.Control));
}
