using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MuxTextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace CodeBrix.Platform.UI.Xaml.Controls.Extensions; //Was previously: Uno.UI.Xaml.Controls.Extensions

internal interface IOverlayTextBoxViewExtension
{
	bool IsOverlayLayerInitialized(XamlRoot xamlRoot);

	void StartEntry();

	void EndEntry();

	void UpdateNativeView();

	void InvalidateLayout();

	void UpdateSize();

	void UpdatePosition();

	void SetText(string text);

	void SetPasswordRevealState(PasswordRevealState passwordRevealState);

	void Select(int start, int length);

	int GetSelectionStart();

	int GetSelectionLength();

	void UpdateProperties();

	int GetSelectionStartBeforeKeyDown();

	int GetSelectionLengthBeforeKeyDown();
}
