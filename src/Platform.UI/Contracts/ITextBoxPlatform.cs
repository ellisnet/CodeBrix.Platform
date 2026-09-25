#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Contracts;

/// <summary>
/// The platform side of one <see cref="TextBox"/> (and <see cref="PasswordBox"/>): the text editing implementation
/// (caret, selection, keyboard and pointer editing, undo history, scrolling the caret into view, the text view that
/// displays the text, and any native input view the platform overlays). The text box's properties, events, template
/// handling and the WinUI text-change and selection-change protocol stay platform-neutral and call these members.
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// One instance per text box, created by <see cref="ITextPlatform.CreateTextBoxPlatform"/> on first use and kept in a
/// field. All members are called on the UI thread. The Skia implementation is
/// <c>CodeBrix.Platform.UI.Skia.TextBoxSkiaPlatform</c>: the managed text box (a <c>TextBlock</c> display block edited
/// in managed code), or, when <c>FeatureConfiguration.TextBox.UseOverlayOnSkia</c> is set, a native input view that a
/// head overlays on the text box. A platform-neutral editing engine is out of scope (the managed editing stays
/// platform code).
/// </para>
/// </remarks>
internal interface ITextBoxPlatform
{
	/// <summary>
	/// Gets a value indicating whether the platform edits the text in managed code. When <see langword="false"/>, a
	/// native input view does the editing and reports its text back through <c>TextBox.ProcessTextInput</c>.
	/// </summary>
	bool IsManagedEditing { get; }

	/// <summary>
	/// Gets the start of the selection.
	/// </summary>
	int SelectionStart { get; }

	/// <summary>
	/// Gets the length of the selection.
	/// </summary>
	int SelectionLength { get; }

	/// <summary>
	/// Gets a value indicating whether the selection was made backwards (its active end is at its start).
	/// </summary>
	bool IsBackwardSelection { get; }

	/// <summary>
	/// Gets how the caret is currently displayed.
	/// </summary>
	TextBox.CaretDisplayMode CaretMode { get; }

	/// <summary>
	/// Gets the selection start as it was right before the current key-down event (some native input views update the
	/// selection before the event is raised).
	/// </summary>
	int SelectionStartBeforeKeyDown { get; }

	/// <summary>
	/// Completes the construction of the text box (theme tracking, caret blink timer, undo history, keyboard
	/// notifications). Called once, at the end of the text box's constructor.
	/// </summary>
	void Initialize();

	/// <summary>
	/// Called when the text box is unloaded.
	/// </summary>
	void OnUnloaded();

	/// <summary>
	/// Forgets the text view of the previous template. Called when a new template is applied.
	/// </summary>
	void ResetTextView();

	/// <summary>
	/// Creates the text view if needed and puts it in the template's content element.
	/// </summary>
	void UpdateTextView();

	/// <summary>
	/// Pushes <paramref name="text"/> to the text view.
	/// </summary>
	/// <param name="text">The text.</param>
	void SetTextNative(string text);

	/// <summary>
	/// Called after the text changed, before the change events are queued: applies the pending selection and updates
	/// the undo history.
	/// </summary>
	void OnTextChanged();

	/// <summary>
	/// Called after <c>TextBox.ProcessTextInput</c> assigned the text. When the text did not actually change, the
	/// pending selection is applied now, because <see cref="OnTextChanged"/> will not run.
	/// </summary>
	/// <param name="oldText">The text before the input.</param>
	void OnTextInputProcessed(string oldText);

	/// <summary>
	/// Normalizes the line breaks of a multi-line text being coerced.
	/// </summary>
	/// <param name="text">The text, already known to accept returns.</param>
	/// <returns>The normalized text.</returns>
	string CoerceMultilineText(string text);

	/// <summary>
	/// Keeps the pending selection within a text of <paramref name="textLength"/> characters.
	/// </summary>
	/// <param name="textLength">The length of the coerced text.</param>
	void ClampPendingSelection(int textLength);

	/// <summary>
	/// Drops the pending selection.
	/// </summary>
	void ClearPendingSelection();

	/// <summary>
	/// Called when a <c>BeforeTextChanging</c> handler canceled the change.
	/// </summary>
	void OnBeforeTextChangingCanceled();

	/// <summary>
	/// Applies a selection that the text box has validated and raised <c>SelectionChanging</c> for.
	/// </summary>
	/// <param name="start">The selection start.</param>
	/// <param name="length">The selection length.</param>
	void Select(int start, int length);

	/// <summary>
	/// Called before a paste replaces the text.
	/// </summary>
	/// <param name="adjustedClipboardText">The pasted text, cut to the maximum length.</param>
	/// <param name="selectionStart">Where the text is pasted.</param>
	/// <param name="newText">The text after the paste.</param>
	void OnPasteFromClipboard(string adjustedClipboardText, int selectionStart, string newText);

	/// <summary>
	/// Called right before the pasted text is processed.
	/// </summary>
	void OnPasteStarting();

	/// <summary>
	/// Called right after the pasted text was processed (also when processing failed).
	/// </summary>
	void OnPasteFinished();

	/// <summary>
	/// Called before a cut removes the selected text.
	/// </summary>
	void OnCutSelectionToClipboard();

	/// <summary>
	/// Called right before the cut text is removed.
	/// </summary>
	void OnCutStarting();

	/// <summary>
	/// Called right after the cut text was removed (also when removing failed).
	/// </summary>
	void OnCutFinished();

	/// <summary>
	/// Undoes the last edit.
	/// </summary>
	void Undo();

	/// <summary>
	/// Redoes the last undone edit.
	/// </summary>
	void Redo();

	/// <summary>
	/// Clears the undo history.
	/// </summary>
	void ClearUndoRedoHistory();

	/// <summary>
	/// Called when a key goes down, before the public handlers run: marks the keys the editor will handle as handled,
	/// without editing.
	/// </summary>
	/// <param name="args">The event data.</param>
	void OnKeyDown(KeyRoutedEventArgs args);

	/// <summary>
	/// Edits the text for a key that went down and that no public handler handled. Called only when
	/// <see cref="IsManagedEditing"/> is <see langword="true"/>.
	/// </summary>
	/// <param name="args">The event data.</param>
	void OnPostKeyDown(KeyRoutedEventArgs args);

	/// <summary>
	/// Called after the base pointer-pressed handling.
	/// </summary>
	/// <param name="args">The event data.</param>
	void OnPointerPressed(PointerRoutedEventArgs args);

	/// <summary>
	/// Called after the base pointer-released handling.
	/// </summary>
	/// <param name="args">The event data.</param>
	/// <param name="wasFocused">Whether the text box had focus before the pointer was released.</param>
	void OnPointerReleased(PointerRoutedEventArgs args, bool wasFocused);

	/// <summary>
	/// Called when the text box lost the pointer capture.
	/// </summary>
	/// <param name="args">The event data.</param>
	void OnPointerCaptureLost(PointerRoutedEventArgs args);

	/// <summary>
	/// Called when a pointer moves over the text box (the event is already marked handled).
	/// </summary>
	/// <param name="args">The event data.</param>
	void OnPointerMoved(PointerRoutedEventArgs args);

	/// <summary>
	/// Called when the text box is right-tapped (the event is already marked handled): opens the context menu.
	/// </summary>
	/// <param name="args">The event data.</param>
	void OnRightTapped(RightTappedRoutedEventArgs args);

	/// <summary>
	/// Called when a pointer press or release lands on the text box while it already had focus, so the platform can
	/// show its keyboard again.
	/// </summary>
	void OnFocusedByPointer();

	/// <summary>
	/// Called when the focus state of the text box changed.
	/// </summary>
	/// <param name="focusState">The new focus state.</param>
	/// <param name="initial">Whether this is the initial state set when the template is applied.</param>
	void OnFocusStateChanged(FocusState focusState, bool initial);

	/// <summary>
	/// Scrolls the caret into view, once, after the pending layout (called when the text box's button states change).
	/// </summary>
	void DispatchUpdateScrolling();

	/// <summary>
	/// Called when the foreground brush changed.
	/// </summary>
	/// <param name="newValue">The new brush.</param>
	void OnForegroundColorChanged(Brush newValue);

	/// <summary>
	/// Called when the selection highlight brush changed.
	/// </summary>
	/// <param name="brush">The new brush.</param>
	void OnSelectionHighlightColorChanged(SolidColorBrush brush);

	/// <summary>
	/// Called when a font property of the text box changed, or when it got a new parent.
	/// </summary>
	void UpdateFont();

	/// <summary>
	/// Called when a property that a native input view mirrors changed (input scope, spell checking, text
	/// prediction, return key type).
	/// </summary>
	void UpdateTextViewProperties();

	/// <summary>
	/// Called when the maximum length changed.
	/// </summary>
	void OnMaxLengthChanged();

	/// <summary>
	/// Called when the flow direction changed.
	/// </summary>
	void OnFlowDirectionChanged();

	/// <summary>
	/// Called when the text wrapping changed.
	/// </summary>
	void OnTextWrappingChanged();

	/// <summary>
	/// Called when the text alignment changed.
	/// </summary>
	void OnTextAlignmentChanged();

	/// <summary>
	/// Called when the layout of the XAML root changed while the text box has focus: repositions any native input view.
	/// </summary>
	void InvalidateOverlayLayout();

	/// <summary>
	/// Called when the password character of a <see cref="PasswordBox"/> changed.
	/// </summary>
	void OnPasswordCharChanged();

	/// <summary>
	/// Shows or hides the characters of a <see cref="PasswordBox"/>.
	/// </summary>
	/// <param name="state">The reveal state.</param>
	void SetPasswordRevealState(PasswordRevealState state);
}
