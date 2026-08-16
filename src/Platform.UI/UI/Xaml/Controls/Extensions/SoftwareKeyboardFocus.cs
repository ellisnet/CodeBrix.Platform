#nullable enable

using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.Foundation.Extensibility;

namespace CodeBrix.Platform.UI.Xaml.Controls.Extensions;

/// <summary>
/// The seam a CUSTOM text-entry control (a control users type into that is not
/// a TextBox/PasswordBox — the TerminalView and AdvancedTextEdit add-ins, say)
/// uses to drive the software (on-screen) keyboard the way TextBox does: call
/// <see cref="NotifyFocused"/> when the control gains focus AND is currently
/// editable/enabled, and <see cref="NotifyUnfocused"/> when it loses focus. The
/// keyboard then auto-shows and auto-hides with the full built-in behavior — a
/// focus move between two text controls does not flicker it, hiding waits for
/// the finger to lift, and the user's dismiss-key choice is honored until they
/// tap back into the control. On heads without a software keyboard, or when the
/// application did not enable one, both calls do nothing.
/// </summary>
public static class SoftwareKeyboardFocus
{
	private static ITextInputFocusNotificationsSingleton? _singleton;

	// Re-resolved until found: the head registers the controller during host
	// initialization, and a miss on a keyboard-less head is a cheap no-op.
	private static ITextInputFocusNotificationsSingleton? Singleton
	{
		get
		{
			if (_singleton == null)
			{
				_ = ApiExtensibility.CreateInstance(null!, out _singleton);
			}
			return _singleton;
		}
	}

	/// <summary>
	/// Reports that an editable custom text-entry control gained focus, which
	/// shows the software keyboard (unless the user dismissed it for this very
	/// control and has not tapped back into it).
	/// </summary>
	public static void NotifyFocused(Control control)
		=> Singleton?.OnTextControlFocused(control);

	/// <summary>
	/// Reports that a custom text-entry control lost focus, which hides the
	/// software keyboard unless focus moved on to another text control.
	/// </summary>
	public static void NotifyUnfocused(Control control)
		=> Singleton?.OnTextControlUnfocused(control);
}
