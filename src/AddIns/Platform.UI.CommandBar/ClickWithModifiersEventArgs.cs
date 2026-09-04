using System;
using Windows.System;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Carries the modifier keys that were held down at the moment a tool bar button was clicked.
/// </summary>
/// <remarks>
/// Desktop tool bars have used modifier-clicks for decades - Shift-click a build button for "build
/// with options", Ctrl-click an open button for "open in a new window". The ordinary click event
/// cannot say which keys were down, so this one does: the state is read at the click, from the
/// input source, rather than remembered from the last key event.
/// </remarks>
public sealed class ClickWithModifiersEventArgs : EventArgs
{
	/// <summary>Initializes the arguments for a click made with the given modifiers.</summary>
	/// <param name="modifiers">The modifier keys held down when the click happened.</param>
	public ClickWithModifiersEventArgs(VirtualKeyModifiers modifiers)
	{
		Modifiers = modifiers;
	}

	/// <summary>
	/// Gets the modifier keys that were held down when the click happened.
	/// </summary>
	/// <remarks>
	/// <see cref="VirtualKeyModifiers.None"/> for a plain click. Test a single modifier with a
	/// bitwise AND - a click made with Shift AND Control reports both.
	/// </remarks>
	public VirtualKeyModifiers Modifiers { get; }

	/// <summary>Gets a value indicating whether the Shift key was held down.</summary>
	public bool IsShiftPressed => (Modifiers & VirtualKeyModifiers.Shift) != 0;

	/// <summary>Gets a value indicating whether the Control key was held down.</summary>
	public bool IsControlPressed => (Modifiers & VirtualKeyModifiers.Control) != 0;

	/// <summary>Gets a value indicating whether the Alt key was held down.</summary>
	public bool IsAltPressed => (Modifiers & VirtualKeyModifiers.Menu) != 0;
}
