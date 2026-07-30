#nullable enable

using System.ComponentModel;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/IOverloadProvider.cs in the AvalonEdit
//repo (MIT). Ported unchanged apart from nullability annotations; note that the OverloadViewer
//listens to PropertyChanged directly (no XAML bindings), so implementations must raise
//PropertyChanged on the UI thread.

/// <summary>
/// Provides the items for the OverloadViewer.
/// </summary>
public interface IOverloadProvider : INotifyPropertyChanged
{
	/// <summary>
	/// Gets/Sets the selected index.
	/// </summary>
	int SelectedIndex { get; set; }

	/// <summary>
	/// Gets the number of overloads.
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Gets the text 'SelectedIndex of Count'.
	/// </summary>
	string CurrentIndexText { get; }

	/// <summary>
	/// Gets the current header. This can be a string or a UIElement; null shows nothing.
	/// </summary>
	object? CurrentHeader { get; }

	/// <summary>
	/// Gets the current content. This can be a string or a UIElement; null shows nothing.
	/// </summary>
	object? CurrentContent { get; }
}
