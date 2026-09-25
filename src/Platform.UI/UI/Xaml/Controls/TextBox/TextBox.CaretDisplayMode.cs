namespace Microsoft.UI.Xaml.Controls;

partial class TextBox
{
	/// <summary>
	/// How the caret of a text box is displayed.
	/// </summary>
	internal enum CaretDisplayMode
	{
		ThumblessCaretHidden,
		ThumblessCaretShowing,
		CaretWithThumbsOnlyEndShowing,
		CaretWithThumbsBothEndsShowing
	}
}
