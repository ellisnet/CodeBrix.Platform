#nullable enable

using System;

using Microsoft.UI.Xaml.Media;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/ICompletionData.cs in the AvalonEdit repo
//(MIT). The Image type changed from System.Windows.Media.ImageSource to
//Microsoft.UI.Xaml.Media.ImageSource; the completion list realizes its item visuals in code
//instead of WPF data binding, so implementations no longer need bindable public properties.

/// <summary>
/// Describes an entry in the <see cref="CompletionList"/>.
/// </summary>
public interface ICompletionData
{
	/// <summary>
	/// Gets the image shown to the left of the completion text, or null for no image.
	/// </summary>
	ImageSource? Image { get; }

	/// <summary>
	/// Gets the text. This property is used to filter the list of visible elements.
	/// </summary>
	string Text { get; }

	/// <summary>
	/// The displayed content. This can be the same as 'Text', or a UIElement if
	/// you want to display rich content. Null shows only the image.
	/// </summary>
	object? Content { get; }

	/// <summary>
	/// Gets the description shown next to the completion list for the selected item,
	/// or null for no description. This can be a string or a UIElement.
	/// </summary>
	object? Description { get; }

	/// <summary>
	/// Gets the priority. This property is used in the selection logic. You can use it to prefer selecting those items
	/// which the user is accessing most frequently.
	/// </summary>
	double Priority { get; }

	/// <summary>
	/// Perform the completion.
	/// </summary>
	/// <param name="textArea">The text area on which completion is performed.</param>
	/// <param name="completionSegment">The text segment that was used by the completion window if
	/// the user types (segment between CompletionWindow.StartOffset and CompletionWindow.EndOffset).</param>
	/// <param name="insertionRequestEventArgs">The EventArgs used for the insertion request.
	/// These can be DoubleTappedRoutedEventArgs or plain EventArgs (for key-triggered insertion),
	/// depending on how the insertion was triggered.</param>
	void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs);
}
