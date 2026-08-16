#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Xaml.Controls.Extensions;

/// <summary>
/// The control-agnostic sibling of <see cref="ITextBoxNotificationsProviderSingleton"/>:
/// the seam a head's software-keyboard controller implements so that CUSTOM
/// text-entry controls (reporting through <see cref="SoftwareKeyboardFocus"/>)
/// drive the same keyboard show/hide machinery TextBox does. The caller gates
/// on its own editable/enabled state before reporting focus, so no read-only
/// check happens behind this seam.
/// </summary>
internal interface ITextInputFocusNotificationsSingleton
{
	/// <summary>An editable custom text-entry control gained focus.</summary>
	void OnTextControlFocused(Control control);

	/// <summary>A custom text-entry control lost focus.</summary>
	void OnTextControlUnfocused(Control control);
}
