#if !__NETSTD_REFERENCE__
using System;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Xaml;
using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

// The members of TextBox whose behavior belongs to the platform's text editing implementation: they forward to the
// text box's platform side (ITextBoxPlatform), created on first use.
public partial class TextBox
{
	private ITextBoxPlatform _textBoxPlatform;

	// TextBox had an explicit static constructor (now TextBoxSkiaPlatform's): this empty one keeps the type's precise
	// initialization semantics (no beforefieldinit), so its static dependency properties are registered exactly when
	// they were before.
	static TextBox()
	{
	}

	/// <summary>
	/// Gets the platform side of this text box (the text editing implementation), creating it on first use.
	/// </summary>
	internal ITextBoxPlatform TextBoxPlatform => _textBoxPlatform ??= PlatformServices.Text.CreateTextBoxPlatform(this);

	internal ContentControl ContentElement => _contentElement;

	internal bool IsBackwardSelection => TextBoxPlatform.IsBackwardSelection;

	internal CaretDisplayMode CaretMode => TextBoxPlatform.CaretMode;

	[GeneratedDependencyProperty(DefaultValue = false)]
	public static DependencyProperty CanUndoProperty { get; } = CreateCanUndoProperty();

	public bool CanUndo
	{
		get => GetCanUndoValue();
		internal set => SetCanUndoValue(value);
	}

	[GeneratedDependencyProperty(DefaultValue = false)]
	public static DependencyProperty CanRedoProperty { get; } = CreateCanRedoProperty();

	public bool CanRedo
	{
		get => GetCanRedoValue();
		internal set => SetCanRedoValue(value);
	}

	public int SelectionStart
	{
		get => TextBoxPlatform.SelectionStart;
		set => Select(start: value, length: SelectionLength);
	}

	public int SelectionLength
	{
		get => TextBoxPlatform.SelectionLength;
		set => Select(SelectionStart, value);
	}

	public void ClearUndoRedoHistory() => TextBoxPlatform.ClearUndoRedoHistory();

	public void Undo() => TextBoxPlatform.Undo();

	public void Redo() => TextBoxPlatform.Redo();

	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);
		e.Handled = true;

		TextBoxPlatform.OnPointerMoved(e);
	}

	// TODO: remove this context menu when TextCommandBarFlyout is implemented
	protected override void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		base.OnRightTapped(e);
		e.Handled = true;

		TextBoxPlatform.OnRightTapped(e);
	}

	protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs args)
	{
		base.OnDoubleTapped(args);
		args.Handled = true;
	}

	internal override bool IsDelegatingFocusToTemplateChild()
		=> OperatingSystem.IsBrowser();

	partial void InitializePartial() => TextBoxPlatform.Initialize();

	partial void OnUnloadedPartial() => TextBoxPlatform.OnUnloaded();

	partial void ResetTextBoxView() => TextBoxPlatform.ResetTextView();

	partial void UpdateTextBoxView() => TextBoxPlatform.UpdateTextView();

	partial void SetTextNative() => TextBoxPlatform.SetTextNative(Text);

	partial void OnTextChangedPartial() => TextBoxPlatform.OnTextChanged();

	partial void OnForegroundColorChangedPartial(Brush newValue) => TextBoxPlatform.OnForegroundColorChanged(newValue);

	partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush brush) => TextBoxPlatform.OnSelectionHighlightColorChanged(brush);

	partial void UpdateFontPartial() => TextBoxPlatform.UpdateFont();

	partial void OnInputScopeChangedPartial(InputScope newValue) => TextBoxPlatform.UpdateTextViewProperties();

	partial void OnIsSpellCheckEnabledChangedPartial(bool newValue) => TextBoxPlatform.UpdateTextViewProperties();

	partial void OnIsTextPredictionEnabledChangedPartial(bool newValue) => TextBoxPlatform.UpdateTextViewProperties();

	partial void SetInputReturnTypePlatform(InputReturnType inputReturnType) => TextBoxPlatform.UpdateTextViewProperties();

	partial void OnMaxLengthChangedPartial(int newValue) => TextBoxPlatform.OnMaxLengthChanged();

	partial void OnFlowDirectionChangedPartial() => TextBoxPlatform.OnFlowDirectionChanged();

	partial void OnTextWrappingChangedPartial() => TextBoxPlatform.OnTextWrappingChanged();

	partial void OnTextAlignmentChangedPartial(TextAlignment newValue) => TextBoxPlatform.OnTextAlignmentChanged();

	partial void OnFocusStateChangedPartial(FocusState focusState, bool initial) => TextBoxPlatform.OnFocusStateChanged(focusState, initial);

	partial void SelectPartial(int start, int length) => TextBoxPlatform.Select(start, length);

	partial void SelectAllPartial() => Select(0, Text.Length);

	partial void OnKeyDownPartial(KeyRoutedEventArgs args) => TextBoxPlatform.OnKeyDown(args);

	partial void PasteFromClipboardPartial(string adjustedClipboardText, int selectionStart, string newText)
		=> TextBoxPlatform.OnPasteFromClipboard(adjustedClipboardText, selectionStart, newText);

	partial void CutSelectionToClipboardPartial() => TextBoxPlatform.OnCutSelectionToClipboard();

	partial void OnPointerPressedPartial(PointerRoutedEventArgs args) => TextBoxPlatform.OnPointerPressed(args);

	partial void OnPointerReleasedPartial(PointerRoutedEventArgs args, bool wasFocused) => TextBoxPlatform.OnPointerReleased(args, wasFocused);

	partial void OnPointerCaptureLostPartial(PointerRoutedEventArgs e) => TextBoxPlatform.OnPointerCaptureLost(e);
}
#endif
