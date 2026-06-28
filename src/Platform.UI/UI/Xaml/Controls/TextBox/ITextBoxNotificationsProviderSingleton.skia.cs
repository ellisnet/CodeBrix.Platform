using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CodeBrix.Platform.UI.Xaml.Controls.Extensions; //Was previously: Uno.UI.Xaml.Controls.Extensions

internal interface ITextBoxNotificationsProviderSingleton
{
	void OnFocused(TextBox textBox);

	void OnUnfocused(TextBox textBox);

	void OnEnteredVisualTree(TextBox textBox);

	void OnLeaveVisualTree(TextBox textBox);

	void FinishAutofillContext(bool shouldSave);

	void NotifyValueChanged(TextBox textBox);
}
