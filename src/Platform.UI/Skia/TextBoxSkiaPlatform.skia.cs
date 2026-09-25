//CodeBrix warning-cleanup 2026-07-10: explicit static constructor retained deliberately (native/platform init, ordered initialization, or precise before-first-use timing); CA1810 suppressed rather than converting to field initializers.
#pragma warning disable CA1810
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Xaml;
using CodeBrix.Platform.UI.Xaml.Controls.Extensions;
using CodeBrix.Platform.UI.Xaml.Media;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using CodeBrix.Platform.UI.Xaml.Controls;
using CodeBrix.Platform.UI.Xaml.Core;
using CodeBrix.Platform.Foundation;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
using Microsoft.UI.Xaml.Media.Media3D;
using System.Numerics;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.UI.Contracts;
using CodeBrix.Platform.UI.Helpers.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if HAS_CODEBRIX_WINUI
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using Windows.UI.Input;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace CodeBrix.Platform.UI.Skia;

using SelectionDetails = (int start, int length, bool selectionEndsAtTheStart);
using CaretDisplayMode = Microsoft.UI.Xaml.Controls.TextBox.CaretDisplayMode;
using ScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;

/// <summary>
/// The Skia implementation of <see cref="ITextBoxPlatform"/>: the managed text box. The text is displayed by a
/// <see cref="TextBlock"/> (the display block of <see cref="TextBoxView"/>) and edited in managed code: caret and
/// selection, keyboard and pointer editing, touch carets with thumbs, the context menu, undo and redo, and scrolling
/// the caret into view. When <c>FeatureConfiguration.TextBox.UseOverlayOnSkia</c> is set, a native input view that a
/// head registers for <see cref="TextBoxView"/> does the editing instead.
/// </summary>
/// <remarks>
/// This is the code that lived in <c>TextBox.skia.cs</c> and <c>TextBox.pointers.skia.cs</c>, moved verbatim; members of
/// the text box are reached through <c>_owner</c>. One instance per text box, created through
/// <see cref="TextSkiaPlatform.CreateTextBoxPlatform"/>.
/// </remarks>
internal sealed class TextBoxSkiaPlatform : ITextBoxPlatform
{
	private readonly TextBox _owner;
	private readonly bool _isSkiaTextBox = !FeatureConfiguration.TextBox.UseOverlayOnSkia;

	private CaretWithStemAndThumb _selectionStartThumbfulCaret;
	private CaretWithStemAndThumb _selectionEndThumbfulCaret;
	private TextBoxView _textBoxView;
	private static ITextBoxNotificationsProviderSingleton _textBoxNotificationsSingleton;

	private SelectionDetails _selection;
	private float _caretXOffset; // this is not necessarily the visual offset of the caret, but where the caret is logically supposed to be when moving up and down with the keyboard, even if the caret is temporarily elsewhere
	private CaretDisplayMode _caretMode = CaretDisplayMode.ThumblessCaretHidden;

	private bool _inSelectInternal;

	private (int start, int length)? _pendingSelection;

	private bool _clearHistoryOnTextChanged = true;

	private static readonly VirtualKeyModifiers _platformCtrlKey;

	// We track what constitutes one typing "action" that can be undone/redone. The general gist is that
	// any sequence of characters (with backspace allowed) without any navigation moves (pointer click, arrow keys, etc.)
	// will be one "run"/"action". However, there are some arbitrary exceptions, so that is only a rule of thumb.
	private bool _currentlyTyping;
	private bool _suppressCurrentlyTyping;
	private SelectionDetails _selectionWhenTypingStarted;
	private string _textWhenTypingStarted;

	private int _historyIndex;
	private readonly List<HistoryRecord> _history = new(); // the selection of an action is what was selected right before it happened. Might turn out to be unnecessary.

	private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(0.5) };

	private MenuFlyout _contextMenu;
	private readonly Dictionary<ContextMenuItem, MenuFlyoutItem> _flyoutItems = new();
	// Keeps the software keyboard unmoved across the focus round-trip into the
	// built-in context menu (which is marked DoesNotAffectSoftwareKeyboard).
	private SoftwareKeyboardFlyoutGuard _contextMenuKeyboardGuard;

	/// <inheritdoc />
	public bool IsBackwardSelection => _selection.selectionEndsAtTheStart;

	/// <summary>
	/// Gets the text view that displays the text of the text box (and hosts a native input view when one is used), or
	/// <see langword="null"/> before a template is applied.
	/// </summary>
	internal TextBoxView TextBoxView => _textBoxView;

	/// <summary>
	/// Creates the platform side of <paramref name="owner"/>.
	/// </summary>
	/// <param name="owner">The text box.</param>
	internal TextBoxSkiaPlatform(TextBox owner)
	{
		_owner = owner;
	}

	/// <summary>
	/// Returns the Skia platform side of <paramref name="textBox"/>.
	/// </summary>
	/// <param name="textBox">The text box.</param>
	/// <returns>The text box's platform side.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static TextBoxSkiaPlatform Of(TextBox textBox)
	{
		Debug.Assert(textBox.TextBoxPlatform is TextBoxSkiaPlatform);
		return Unsafe.As<TextBoxSkiaPlatform>(textBox.TextBoxPlatform);
	}

	static TextBoxSkiaPlatform()
	{
		_platformCtrlKey =
			OperatingSystem.IsMacOS() || (OperatingSystem.IsBrowser() && WebAssemblyImports.EvalBool("navigator?.platform.toUpperCase().includes('MAC') ?? false"))
			? VirtualKeyModifiers.Windows
			: VirtualKeyModifiers.Control;
	}

	/// <inheritdoc />
	public CaretDisplayMode CaretMode
	{
		get => _caretMode;
		private set
		{
			if (_caretMode != value)
			{
				_caretMode = value;
				UpdateDisplaySelection();
				TextBoxView?.DisplayBlock.InvalidateInlines(false);
				if (value is CaretDisplayMode.ThumblessCaretShowing)
				{
					_timer.Start(); // restart
				}
				else if (value is CaretDisplayMode.CaretWithThumbsBothEndsShowing
						 or CaretDisplayMode.CaretWithThumbsOnlyEndShowing)
				{
					_timer.Stop();
				}
			}
		}
	}

	private void UpdateCanUndoRedo()
	{
		_owner.CanUndo = _historyIndex > 0;
		_owner.CanRedo = _historyIndex < _history.Count - 1;
	}

	private void TrySetCurrentlyTyping(bool newValue)
	{
		if (newValue == _currentlyTyping || _suppressCurrentlyTyping)
		{
			return;
		}

		if (newValue)
		{
			_textWhenTypingStarted = _owner.Text;
			_selectionWhenTypingStarted = (
				_selection.start,
				_selection.length,
				_selection.selectionEndsAtTheStart);
		}
		else
		{
			_historyIndex++;
			_history.RemoveAllAt(_historyIndex);
			_history.Add(new HistoryRecord(
				new ReplaceAction(_textWhenTypingStarted, _owner.Text, _selection.start),
				_selectionWhenTypingStarted.start,
				_selectionWhenTypingStarted.length,
				_selectionWhenTypingStarted.selectionEndsAtTheStart));
			UpdateCanUndoRedo();
		}

		_currentlyTyping = newValue;
	}

	/// <inheritdoc />
	public void OnUnloaded()
	{
		_timer.Stop();
		_selectionStartThumbfulCaret?.Hide();
		_selectionEndThumbfulCaret?.Hide();
		CaretMode = CaretDisplayMode.ThumblessCaretHidden;
	}

	/// <inheritdoc />
	public void OnForegroundColorChanged(Brush newValue) => TextBoxView?.OnForegroundChanged(newValue);

	/// <inheritdoc />
	public void OnSelectionHighlightColorChanged(SolidColorBrush brush) => TextBoxView?.OnSelectionHighlightColorChanged(brush);

	/// <inheritdoc />
	public void UpdateFont()
	{
		TextBoxView?.UpdateFont();
	}

	/// <inheritdoc />
	public void UpdateTextViewProperties() => TextBoxView?.UpdateProperties();

	/// <inheritdoc />
	public void OnMaxLengthChanged() => TextBoxView?.UpdateMaxLength();

	/// <inheritdoc />
	public void OnFlowDirectionChanged()
	{
		TextBoxView?.SetFlowDirection();
	}

	/// <inheritdoc />
	public void OnTextWrappingChanged()
	{
		TextBoxView?.SetWrapping();
		if (_owner.ContentElement is ScrollViewer sv)
		{
			// This is to work around sv giving infinite width. This has the unfortunate problem of resetting
			// locally-set values and/or changes in the template.
			sv.HorizontalScrollBarVisibility = _owner.TextWrapping == TextWrapping.NoWrap ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled;
		}
	}

	/// <inheritdoc />
	public void OnTextAlignmentChanged()
	{
		TextBoxView?.SetTextAlignment();
	}

	/// <inheritdoc />
	public void UpdateTextView()
	{
		_textBoxView ??= new TextBoxView(_owner);
		if (_owner.ContentElement != null)
		{
			var displayBlock = TextBoxView.DisplayBlock;
			if (_owner.ContentElement.Content != displayBlock)
			{
				_owner.ContentElement.Content = displayBlock;

				if (_isSkiaTextBox)
				{
					_selectionStartThumbfulCaret = new();
					_selectionEndThumbfulCaret = new();

					foreach (var caret in (ReadOnlySpan<CaretWithStemAndThumb>)[_selectionStartThumbfulCaret, _selectionEndThumbfulCaret])
					{
						caret.PointerPressed += CaretOnPointerPressed;
						caret.PointerReleased += CaretOnPointerReleased;
						caret.PointerMoved += CaretOnPointerMoved;
						caret.PointerCanceled += ClearCaretPointerState;
						caret.PointerCaptureLost += ClearCaretPointerState;
					}

					displayBlock.DrawingFinished += () =>
					{
						// Only invalidate the carets after drawing is complete
						// to avoid modifying the children visuals while they are being enumerated.
						NativeDispatcher.Main.Enqueue(() =>
						{
							UpdateFlyoutPosition();
						}, NativeDispatcherPriority.Normal);
					};
				}
			}

			TextBoxView.SetTextNative(_owner.Text);
		}
	}

	internal void UpdateFlyoutPosition()
	{
		if (CaretMode is CaretDisplayMode.CaretWithThumbsOnlyEndShowing or CaretDisplayMode.CaretWithThumbsBothEndsShowing)
		{
			var (selectionStart, selectionEnd) = IsBackwardSelection ? (SelectionStart + SelectionLength, SelectionLength) : (SelectionStart, SelectionStart + SelectionLength);
			foreach (var (index, caret) in (ReadOnlySpan<(int, CaretWithStemAndThumb)>)[(selectionStart, _selectionStartThumbfulCaret), (selectionEnd, _selectionEndThumbfulCaret)])
			{
				var rect = _textBoxView.DisplayBlock.ParsedText.GetRectForIndex(index);
				rect.Width = TextBlock.CaretThickness;
				caret.Height = rect.Height + 16;
				var transform = TextBoxView.DisplayBlock.TransformToVisual(null);
				if (transform.TransformBounds(rect).IntersectWith(_owner.GetAbsoluteBoundsRect()) is not null)
				{
					var matrixTransform = (MatrixTransform)transform;

					var textBoxMatrix = matrixTransform.Matrix.ToMatrix3x2();

					// Calculate the center point of the caret in local coordinates
					var localCenterX = rect.GetMidX() - caret.Width / 2;
					var localPoint = new Point(localCenterX, rect.Top);

					// Create translation matrix based on the target point
					var translationMatrix = Matrix3x2.CreateTranslation((float)localPoint.X, (float)localPoint.Y);
					var totalMatrix = Matrix3x2.Multiply(translationMatrix, textBoxMatrix);
					caret.ShowAt(_owner.XamlRoot, totalMatrix);
				}
			}
			if (CaretMode is CaretDisplayMode.CaretWithThumbsOnlyEndShowing)
			{
				_selectionStartThumbfulCaret.Hide();
				_selectionEndThumbfulCaret.SetStemVisible(SelectionLength == 0);
			}
		}
		else
		{
			_selectionStartThumbfulCaret.Hide();
			_selectionEndThumbfulCaret.Hide();
		}
	}

	/// <inheritdoc />
	public void OnFocusStateChanged(FocusState focusState, bool initial)
	{
		TextBoxView?.OnFocusStateChanged(focusState);

		if (_isSkiaTextBox)
		{
			// The guard suppresses the keyboard notifications (never the caret or
			// selection handling) across the focus round-trip into the built-in
			// context menu, so opening it and clicking Paste cannot hide and
			// re-show the software keyboard. If the menu instead closes with
			// focus elsewhere, the guard delivers the withheld unfocus itself.
			if (focusState != FocusState.Unfocused)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
				if (_contextMenuKeyboardGuard?.ShouldSuppressFocus() != true)
				{
					_textBoxNotificationsSingleton?.OnFocused(_owner);
				}
			}
			else
			{
				TrySetCurrentlyTyping(false);
				CaretMode = CaretDisplayMode.ThumblessCaretHidden;
				if (!initial && _contextMenuKeyboardGuard?.ShouldSuppressUnfocus() != true)
				{
					_textBoxNotificationsSingleton?.OnUnfocused(_owner);
				}
				_timer.Stop();
			}
			UpdateDisplaySelection();
		}
	}

#if false // Removing temporarily. We'll need to add it back.
	// TODO: Discuss this public API.
	public static void FinishAutofillContext(bool shouldSave)
	{
		_textBoxNotificationsSingleton?.FinishAutofillContext(shouldSave);
	}
#endif

	/// <inheritdoc />
	public void Select(int start, int length)
	{
		TrySetCurrentlyTyping(false);

		if (!_inSelectInternal)
		{
			// SelectInternal sets _selectionEndsAtTheStart and _caretXOffset on its own
			_selection.selectionEndsAtTheStart = false;
			_caretXOffset = (float)(TextBoxView?.DisplayBlock.ParsedText.GetRectForIndex(start + length).Left ?? 0);
		}

		var selection = (start, length, _selection.selectionEndsAtTheStart);
		var selectionChanged = selection != _selection;
		_selection = selection;

		// Even when using Skia TextBox, we may need to call Select,
		// which will update the native input in case of Wasm Skia for example.
		TextBoxView?.Select(start, length);

		if (_isSkiaTextBox)
		{
			if (length == 0 && CaretMode == CaretDisplayMode.CaretWithThumbsBothEndsShowing)
			{
				// It doesn't make sense to have 2 caret ends when there's no selection.
				CaretMode = CaretDisplayMode.CaretWithThumbsOnlyEndShowing;
			}
			else if (CaretMode is CaretDisplayMode.ThumblessCaretHidden)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
			}
			else if (CaretMode is CaretDisplayMode.ThumblessCaretShowing)
			{
				_timer.Start(); // restart
			}

			if (selectionChanged)
			{
				UpdateScrolling();
			}
			UpdateDisplaySelection();
		}
	}

	/// <inheritdoc />
	public int SelectionStart => _isSkiaTextBox ? _selection.start : TextBoxView?.GetSelectionStart() ?? 0;

	/// <inheritdoc />
	public int SelectionLength => _isSkiaTextBox ? _selection.length : TextBoxView?.GetSelectionLength() ?? 0;

	private void UpdateDisplaySelection()
	{
		if (_isSkiaTextBox && TextBoxView?.DisplayBlock is { } displayBlock)
		{
			displayBlock.Selection = new TextBlock.Range(SelectionStart, SelectionStart + SelectionLength);
			var isFocused = _owner.FocusState != FocusState.Unfocused || (_contextMenu?.IsOpen ?? false);
			displayBlock.RenderSelection = isFocused;
			if (CaretMode is CaretDisplayMode.ThumblessCaretShowing &&
				SelectionLength == 0 &&
				isFocused &&
				!_owner.IsReadOnly &&
				!FeatureConfiguration.TextBox.HideCaret)
			{
				var brush = DefaultBrushes.TextForegroundBrush.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor());
				displayBlock.RenderCaret = (IsBackwardSelection ? SelectionStart : SelectionStart + SelectionLength, brush);
			}
			else
			{
				displayBlock.RenderCaret = null;
			}
			((IBlock)TextBoxView.DisplayBlock).Invalidate(false);
		}
	}

	private void UpdateScrolling() => UpdateScrolling(true);

	/// <summary>
	/// Scrolls the text box's content element so that the caret is inside the visible viewport
	/// </summary>
	/// <remarks>
	/// By default, only the selection end moves, while the selection start stays fixed. This is not the
	/// case when dragging the caret thumb, in which case both ends can move. This case requires an
	/// explicit call to this method with <see cref="putSelectionEndInVisibleViewport"/> = false.
	/// </remarks>>
	private void UpdateScrolling(bool putSelectionEndInVisibleViewport)
	{
		if (_isSkiaTextBox && _owner.ContentElement is ScrollViewer sv)
		{
			var horizontalOffset = sv.HorizontalOffset;
			var verticalOffset = sv.VerticalOffset;

			var (selectionStart, selectionEnd) = _selection.selectionEndsAtTheStart ? (_selection.start + _selection.length, _selection.start) : (_selection.start, _selection.start + _selection.length);
			var index = putSelectionEndInVisibleViewport ? selectionEnd : selectionStart;

			var caretRect = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(index) with { Width = TextBlock.CaretThickness };

			// Because the caret is only a single-pixel wide, and because screens can't draw in fractions of a pixel,
			// we need to add Math.Ceiling to ensure that the caret is (fully) included in the visible viewport. This
			// Math.Ceiling sometimes horizontal overscrolling, but it's more acceptable than sometimes not showing the caret.
			var newHorizontalOffset = horizontalOffset.AtMost(caretRect.Left).AtLeast(Math.Ceiling(caretRect.Right - sv.ViewportWidth + TextBlock.CaretThickness));

			var newVerticalOffset = verticalOffset.AtMost(caretRect.Top).AtLeast(caretRect.Bottom - sv.ViewportHeight);

			sv.ChangeView(newHorizontalOffset, newVerticalOffset, null);
		}
	}

	/// <inheritdoc />
	public void OnKeyDown(KeyRoutedEventArgs args)
	{
		// This is a minimal copy of OnkeyDownSkia that just sets args.Handled without doing any work.
		// This is to match WinUI behavior where Handled is set for certain keys before public
		// subscribers get the event, but before any actual text processing is done.
		if (!_isSkiaTextBox)
		{
			return;
		}

		var (selectionStart, selectionLength) = _selection.selectionEndsAtTheStart ? (_selection.start + _selection.length, -_selection.length) : (_selection.start, _selection.length);
		var text = _owner.Text;
		var shift = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);
		var ctrl = args.KeyboardModifiers.HasFlag(_platformCtrlKey);
		switch (args.Key)
		{
			case VirtualKey.Escape:
				if (_owner.HasPointerCapture)
				{
					args.Handled = true;
				}
				return;
			case VirtualKey.Z when ctrl:
			case VirtualKey.Y when ctrl:
			case VirtualKey.Delete when !_owner.IsReadOnly:
			case VirtualKey.A when ctrl:
				if (!_owner.HasPointerCapture)
				{
					args.Handled = true;
				}
				return;
			case VirtualKey.Up:
				// on macOS start of document is `Command` and `Up`
				if (ctrl && OperatingSystem.IsMacOS())
				{
					KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				else
				{
					KeyDownUpArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				break;
			case VirtualKey.Down:
				// on macOS end of document is `Command` and `Down`
				if (ctrl && OperatingSystem.IsMacOS())
				{
					KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				else
				{
					KeyDownDownArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				break;
			case VirtualKey.Left when !TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
			case VirtualKey.Right when TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
				KeyDownLeftArrow(args, text, shift, ctrl, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Left when TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
			case VirtualKey.Right when !TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
				KeyDownRightArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Home:
				KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.End:
				KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			// TODO: PageUp/Down
			case VirtualKey.Back when !_owner.IsReadOnly:
				KeyDownBack(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
		}
	}

	/// <inheritdoc />
	public void OnPostKeyDown(KeyRoutedEventArgs args)
	{
		if (_selection.length != 0 &&
			args.Key is not (VirtualKey.Up or VirtualKey.Down or VirtualKey.Left or VirtualKey.Right))
		{
			// On WinUI, pressing anything except arrow keys will immediately make the caret thumbless.
			// Even shift + arrow keys will make the caret thumbless (because it's a shift _then_ an arrow key).
			CaretMode = CaretDisplayMode.ThumblessCaretShowing;
		}

		// Note: On windows ** only KeyDown ** is handled (not KeyUp)

		// move to possibly-negative selection length format
		var (selectionStart, selectionLength) = _selection.selectionEndsAtTheStart ? (_selection.start + _selection.length, -_selection.length) : (_selection.start, _selection.length);

		var text = _owner.Text;
		var shift = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift);
		var ctrl = args.KeyboardModifiers.HasFlag(_platformCtrlKey);
		// Text commands: always return from this switch, never break
		switch (args.Key)
		{
			case VirtualKey.Z when ctrl:
				if (!_owner.HasPointerCapture)
				{
					args.Handled = true;
					Undo();
				}
				return;
			case VirtualKey.Y when ctrl:
				if (!_owner.HasPointerCapture)
				{
					args.Handled = true;
					Redo();
				}
				return;
			case VirtualKey.X when ctrl:
				_owner.CutSelectionToClipboard();
				return;
			case VirtualKey.V when ctrl:
			case VirtualKey.Insert when shift:
				_owner.PasteFromClipboard(); // async so doesn't actually do anything right now
				return;
			case VirtualKey.C when ctrl:
			case VirtualKey.Insert when ctrl:
				_owner.CopySelectionToClipboard();
				return;
			case VirtualKey.Escape:
				if (_owner.HasPointerCapture)
				{
					args.Handled = true;
					_owner.ReleasePointerCaptures();
				}
				return;
			case VirtualKey.LeftShift:
			case VirtualKey.RightShift:
			case VirtualKey.Shift:
			case VirtualKey.Control:
			case VirtualKey.LeftControl:
			case VirtualKey.RightControl:
				// No-op when pressing these key specifically.
				return;
		}

		// text input/movement
		switch (args.Key)
		{
			case VirtualKey.Up:
				// on macOS start of document is `Command` and `Up`
				if (ctrl && OperatingSystem.IsMacOS())
				{
					KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				else
				{
					KeyDownUpArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				break;
			case VirtualKey.Down:
				// on macOS end of document is `Command` and `Down`
				if (ctrl && OperatingSystem.IsMacOS())
				{
					KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				else
				{
					KeyDownDownArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				}
				break;
			case VirtualKey.Left when !TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
			case VirtualKey.Right when TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
				KeyDownLeftArrow(args, text, shift, ctrl, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Left when TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
			case VirtualKey.Right when !TextBoxView.DisplayBlock.ParsedText.IsBaseDirectionRightToLeft:
				KeyDownRightArrow(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Home:
				KeyDownHome(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.End:
				KeyDownEnd(args, text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			// TODO: PageUp/Down
			case VirtualKey.Back when !_owner.IsReadOnly:
				KeyDownBack(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.Delete when !_owner.IsReadOnly:
				KeyDownDelete(args, ref text, ctrl, shift, ref selectionStart, ref selectionLength);
				break;
			case VirtualKey.A when ctrl:
				if (!_owner.HasPointerCapture)
				{
					args.Handled = true;
					TrySetCurrentlyTyping(false);
					selectionStart = 0;
					selectionLength = text.Length;
				}
				break;
			default:
				var isEnterKey = args.UnicodeKey is '\r' or '\n' || args.Key == VirtualKey.Enter;
				if (!_owner.IsReadOnly && !_owner.HasPointerCapture && args.UnicodeKey is { } key && (!isEnterKey || _owner.AcceptsReturn))
				{
					TrySetCurrentlyTyping(true);
					var start = Math.Min(selectionStart, selectionStart + selectionLength);
					var end = Math.Max(selectionStart, selectionStart + selectionLength);

					if (key is '\n')
					{
						// TextBox autoconverts to \r, like WinUI
						key = '\r';
					}

					text = text[..start] + key + text[end..];
					selectionStart = start + 1;
					selectionLength = 0;
					break;
				}
				else
				{
					return;
				}
		}

		selectionStart = Math.Max(0, Math.Min(text.Length, selectionStart));
		selectionLength = Math.Max(-selectionStart, Math.Min(text.Length - selectionStart, selectionLength));

		var caretXOffset = _caretXOffset;

		_suppressCurrentlyTyping = true;
		_clearHistoryOnTextChanged = false;
		if (!_owner.HasPointerCapture)
		{
			_pendingSelection = (selectionStart, selectionLength);
		}

		_owner.ProcessTextInput(text);
		_clearHistoryOnTextChanged = true;
		_suppressCurrentlyTyping = false;

		// don't change the caret offset when moving up and down
		if (args.Key is VirtualKey.Up or VirtualKey.Down)
		{
			// this condition is accurate in the case of hitting Down on the last line
			// or up on the first line. On WinUI, the caret offset won't change.
			_caretXOffset = caretXOffset;
		}
	}

	internal void SetPendingSelection(int selectionStart, int selectionLength)
		=> _pendingSelection = (selectionStart, selectionLength);

	private void KeyDownBack(KeyRoutedEventArgs args, ref string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// on macOS it is `option` + `delete` (same location as backspace on PC keyboards) that removes the previous word
		if (OperatingSystem.IsMacOS())
		{
			ctrl = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
		}

		if (_owner.HasPointerCapture)
		{
			return;
		}
		if (selectionLength != 0)
		{
			TrySetCurrentlyTyping(false);
			TrySetCurrentlyTyping(true);

			var start = Math.Min(selectionStart, selectionStart + selectionLength);
			var end = Math.Max(selectionStart, selectionStart + selectionLength);
			text = text[..start] + text[end..];
			selectionLength = 0;
			selectionStart = start;
		}
		else if (selectionStart != 0)
		{
			if (ctrl)
			{
				// ctrl always ends the previous typing run
				TrySetCurrentlyTyping(false);
			}
			else
			{
				// idempotent call to make sure we're starting a new typing run if we're not in one already
				TrySetCurrentlyTyping(true);
			}

			var oldText = text;
			var index = ctrl ? TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, false).start : selectionStart - 1;
			text = text[..index] + text[selectionStart..];
			selectionStart = index;

			if (ctrl)
			{
				// typing after ctrl starts a new run, and not a part of the ctrl-backspace run
				CommitAction(new ReplaceAction(oldText, text, selectionStart));
			}
		}
	}

	private void KeyDownUpArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// TODO ctrl+up
		if (_owner.HasPointerCapture)
		{
			return;
		}
		if (_owner.Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		var newEnd = GetUpDownResult(text, selectionStart, selectionLength, shift, up: true);
		if (shift)
		{
			selectionLength = newEnd - selectionStart;
		}
		else
		{
			selectionStart = newEnd;
			selectionLength = 0;
		}

		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownDownArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// TODO ctrl+down
		if (_owner.HasPointerCapture)
		{
			return;
		}
		if (_owner.Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		var newEnd = GetUpDownResult(text, selectionStart, selectionLength, shift, up: false);
		if (shift)
		{
			selectionLength = newEnd - selectionStart;
		}
		else
		{
			selectionStart = newEnd;
			selectionLength = 0;
		}

		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownLeftArrow(KeyRoutedEventArgs args, string text, bool shift, bool ctrl, ref int selectionStart, ref int selectionLength)
	{
		if (_owner.HasPointerCapture)
		{
			return;
		}
		if (_owner.Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		if (!shift && selectionStart == 0 && selectionLength == 0 || shift && selectionStart + selectionLength == 0)
		{
			return;
		}

		args.Handled = true;

		if (shift)
		{
			var end = selectionStart + selectionLength;
			if (ctrl)
			{
				end = TextBoxView.DisplayBlock.ParsedText.GetWordAt(end, false).start;
			}
			else
			{
				end--;
			}

			selectionLength = end - selectionStart;
		}
		else
		{
			if (selectionLength == 0)
			{
				selectionStart = ctrl ? TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, false).start : selectionStart - 1;
			}
			else
			{
				selectionStart = Math.Min(selectionStart, selectionStart + selectionLength);
			}
			selectionLength = 0;
		}
	}

	private void KeyDownRightArrow(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// on macOS it is:
		// * `option` + `right` that moves to the next word
		// * `shift` + `option` + `right` that select the next word
		if (OperatingSystem.IsMacOS())
		{
			ctrl = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
		}

		if (_owner.HasPointerCapture)
		{
			return;
		}
		if (_owner.Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var moveOutRight = !shift && selectionStart == text.Length && selectionLength == 0 || shift && selectionStart + selectionLength == _owner.Text.Length;
		if (!moveOutRight)
		{
			args.Handled = true;

			if (shift)
			{
				var end = selectionStart + selectionLength;
				if (ctrl)
				{
					var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(end, true);
					end = chunk.start + chunk.length;
				}
				else
				{
					end++;
				}

				selectionLength = end - selectionStart;
			}
			else
			{
				if (selectionLength == 0)
				{
					if (ctrl)
					{
						var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, true);
						selectionStart = chunk.start + chunk.length;
					}
					else
					{
						selectionStart += 1;
					}
				}
				else
				{
					selectionStart = Math.Max(selectionStart, selectionStart + selectionLength);
				}
				selectionLength = 0;
			}
		}
	}

	private void KeyDownHome(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		if (_owner.HasPointerCapture)
		{
			return;
		}
		if (_owner.Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		if (shift)
		{
			selectionLength = ctrl ? -selectionStart : TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength).start - selectionStart;
		}
		else
		{
			selectionStart = ctrl ? 0 : TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength).start;
			selectionLength = 0;
		}
		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownEnd(KeyRoutedEventArgs args, string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		if (_owner.HasPointerCapture)
		{
			return;
		}
		if (_owner.Text.Length != 0)
		{
			TrySetCurrentlyTyping(false);
		}

		var start = selectionStart;
		var end = selectionStart + selectionLength;
		if (shift)
		{
			if (ctrl)
			{
				selectionLength = text.Length - selectionStart;
			}
			else
			{
				var line = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength);
				selectionLength = line.start + line.length - selectionStart;
			}
		}
		else
		{
			if (ctrl)
			{
				selectionStart = text.Length;
			}
			else
			{
				var line = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength);
				selectionStart = line.start + line.length;
				if (line.length > 0 && selectionStart < text.Length && text[selectionStart - 1] == '\r')
				{
					// a newline is part of the line just before it, but End shouldn't go past the newline
					selectionStart--;
				}
			}
			selectionLength = 0;
		}
		args.Handled = selectionStart != start || selectionLength != end - start;
	}

	private void KeyDownDelete(KeyRoutedEventArgs args, ref string text, bool ctrl, bool shift, ref int selectionStart, ref int selectionLength)
	{
		// on macOS it is `option` + `delete>` that removes the next word
		if (OperatingSystem.IsMacOS())
		{
			ctrl = args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Menu);
		}

		if (_owner.HasPointerCapture)
		{
			return;
		}
		TrySetCurrentlyTyping(false);
		args.Handled = true;
		var oldText = text;
		if (selectionLength != 0)
		{
			var start = Math.Min(selectionStart, selectionStart + selectionLength);
			var end = Math.Max(selectionStart, selectionStart + selectionLength);
			text = text[..start] + text[end..];
			CommitAction(new DeleteAction(oldText, text, selectionStart, selectionLength));
			selectionLength = 0;
			selectionStart = start;
		}
		else if (selectionStart != text.Length)
		{
			if (shift)
			{
				// On WinUI, shift-delete doesn't do anything if nothing is selected for some reason
				// We still end the previous typing run
				return;
			}
			int index;
			if (ctrl)
			{
				var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(selectionStart, true);
				index = chunk.start + chunk.length;
			}
			else
			{
				index = selectionStart + 1;
			}
			text = text[..selectionStart] + text[index..];
			// On WinUI, when ctrl-delete is Undone, the deleted text actually gets selected even though initially, nothing was selected
			CommitAction(new DeleteAction(oldText, text, selectionStart, ctrl ? index - selectionStart : 0));
		}
	}

	/// <summary>
	/// Takes a possibly-negative selection length, indicating a selection that goes backwards.
	/// This makes the calculations a lot more natural.
	/// </summary>
	internal void SelectInternal(int selectionStart, int selectionLength)
	{
		_inSelectInternal = true;
		_selection.selectionEndsAtTheStart = selectionLength < 0;
		if (DisplayBlockInlines is { }) // this check is important because on start up, the Inlines haven't been created yet.
		{
			_caretXOffset = selectionLength >= 0 ?
				(float)TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(selectionStart + selectionLength).Left :
				(float)TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(selectionStart + selectionLength).Right;
		}
		_owner.Select(Math.Min(selectionStart, selectionStart + selectionLength), Math.Abs(selectionLength));
		_inSelectInternal = false;
	}

	private void TimerOnTick(object sender, object e)
	{
		if (_owner.IsLoaded && _owner.IsFocused)
		{
			if (CaretMode == CaretDisplayMode.ThumblessCaretHidden)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
			}
			else if (CaretMode == CaretDisplayMode.ThumblessCaretShowing)
			{
				CaretMode = CaretDisplayMode.ThumblessCaretHidden;
			}
			UpdateDisplaySelection();
		}
	}

	/// <summary>
	/// There are 2 concepts of a "line", there's a line that ends at end-of-text, \r, \n, etc.
	/// and then there's an actual rendered line that may end due to wrapping and not a line break.
	/// This method cares about the second kind of lines.
	/// </summary>
	private int GetUpDownResult(string text, int selectionStart, int selectionLength, bool shift, bool up)
	{
		if (text.Length == 0)
		{
			return 0;
		}

		var (startLineStart, startLineLength, startLineFirst, startLineLast, startLineIndex) = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart);
		var (endLineStart, endLineLength, endLineFirst, endLineLast, endLineIndex) = TextBoxView.DisplayBlock.ParsedText.GetLineAt(selectionStart + selectionLength);

		if (up && shift && endLineFirst)
		{
			return 0; // first line, goes to the beginning
		}
		else if (!up && shift && endLineLast)
		{
			return text.Length; // last line, goes to the end
		}
		else if (!up && !shift && (startLineLast || endLineLast))
		{
			return text.Length; // last line, goes to the end
		}

		int newLineIndex;
		if (up)
		{
			if (selectionLength < 0 || shift)
			{
				newLineIndex = !endLineFirst ? endLineIndex - 1 : endLineIndex;
			}
			else
			{
				newLineIndex = !startLineFirst ? startLineIndex - 1 : startLineIndex;
			}
		}
		else
		{
			if (selectionLength > 0 || shift)
			{
				newLineIndex = !endLineLast ? endLineIndex + 1 : endLineIndex;
			}
			else
			{
				newLineIndex = !startLineLast ? startLineIndex + 1 : startLineIndex;
			}
		}

		var rect = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(selectionStart + selectionLength);
		var x = _caretXOffset;
		var y = (newLineIndex + 0.5) * rect.Height; // 0.5 is to get the center of the line, rect.Height is line height
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(new Point(x, y), true, true));
		var (newLineStart, newLineLength, newLineFirst, newLineLast, _) = TextBoxView.DisplayBlock.ParsedText.GetLineAt(index);
		if (text.Length > index - 1
			&& newLineLength > 1 // this check is for cases where the line has nothing but \r (i.e. is empty)
			&& index - 1 >= 0
			&& index == newLineStart + newLineLength
			&& (text[index - 1] == '\r' || text[index - 1] == ' '))
		{
			// if we're past \r or space, we will actually be at the beginning of the next line, so we take a step back
			index--;
		}

		return index;
	}

	private InlineCollection DisplayBlockInlines => TextBoxView?.DisplayBlock.Inlines;

	/// <summary>
	/// There are 2 concepts of a "line", there's a line that ends at end-of-text, \r, \n, etc.
	/// and then there's an actual rendered line that may end due to wrapping and not a line break.
	/// StartOfLine and EndOfLine care about the first kind of lines.
	/// </summary>
	private int StartOfLine(int i)
	{
		var text = _owner.Text;

		i--;
		for (; i >= 0; i--)
		{
			var c = text[i];
			if (c == '\r')
			{
				break;
			}
		}

		return i + 1;
	}

	private int EndOfLine(int i)
	{
		var index = _owner.Text.IndexOf('\r', i);
		return index == -1 ? _owner.Text.Length - 1 : index;
	}

	/// <inheritdoc />
	public void Initialize()
	{
		_owner.ActualThemeChanged += (_, _) =>
		{
			TextBoxView?.DisplayBlock.InvalidateInlines(false);
			TextBoxView?.UpdateTheme();
		};
		_timer.Tick += TimerOnTick;
		EnsureHistory();

		_ = ApiExtensibility.CreateInstance(null, out _textBoxNotificationsSingleton);
	}

	/// <inheritdoc />
	public void OnTextChanged()
	{
		if (_isSkiaTextBox)
		{
			if (_pendingSelection is { } selection)
			{
				SelectInternal(selection.start, selection.length);
			}
			else
			{
				SelectInternal(0, 0);
			}

			if (_clearHistoryOnTextChanged)
			{
				ClearUndoRedoHistory();
			}

			_textBoxNotificationsSingleton?.NotifyValueChanged(_owner);
		}
	}

	private string RemoveLF(string baseString)
	{

		var builder = new StringBuilder();
		for (int i = 0; i < baseString.Length; i++)
		{
			var c = baseString[i];
			if (c == '\n')
			{
				builder.Append('\r');
			}
			else if (c == '\r' && i + 1 < baseString.Length && baseString[i + 1] == '\n')
			{
				if (_pendingSelection is { } selection)
				{
					var (start, end) = (selection.start, selection.start + selection.length);
					if (start > i)
					{
						start--;
					}
					if (end > i)
					{
						end--;
					}
					_pendingSelection = (start, end - start);
				}

				builder.Append('\r');
				i++;
			}
			else
			{
				builder.Append(c);
			}
		}

		baseString = builder.ToString();
		return baseString;
	}

	/// <inheritdoc />
	public void OnPasteFromClipboard(string adjustedClipboardText, int selectionStart, string newText)
	{
		if (_isSkiaTextBox)
		{
			if (_currentlyTyping)
			{
				TrySetCurrentlyTyping(false);
			}
			else
			{
				// we only commit an action if we were not typing, because if we were typing and we now set CurrentlyTyping = false,
				// we will already get a new action from the setter, so we don't need to commit another one here.
				CommitAction(new ReplaceAction(_owner.Text, newText, selectionStart));
			}

			_pendingSelection = (selectionStart + adjustedClipboardText.Length, 0);
		}
	}

	/// <inheritdoc />
	public void OnCutSelectionToClipboard()
	{
		if (_isSkiaTextBox)
		{
			if (_currentlyTyping)
			{
				TrySetCurrentlyTyping(false);
			}
			else
			{
				// we only commit an action if we were not typing, because if we were typing and we now set CurrentlyTyping = false,
				// we will already get a new action from the setter, so we don't need to commit another one here.
				CommitAction(new ReplaceAction(_owner.Text, _owner.Text.Remove(SelectionStart, SelectionLength), SelectionStart + SelectionLength));
			}
			_pendingSelection = (_selection.start, 0);
		}
	}

	private void EnsureHistory()
	{
		if (_history.Count == 0)
		{
			_history.Add(new HistoryRecord(SentinelAction.Instance, _selection.start, _selection.length, _selection.selectionEndsAtTheStart));
		}
		_historyIndex = Math.Max(0, Math.Min(_history.Count - 1, _historyIndex));
		UpdateCanUndoRedo();
	}

	/// <inheritdoc />
	public void ClearUndoRedoHistory()
	{
		TrySetCurrentlyTyping(false);
		_history.Clear();
		EnsureHistory();
	}

	/// <summary>
	/// Adds a new Action at the present point in history and deletes the old "future"
	/// </summary>
	private void CommitAction(TextBoxAction action)
	{
		_historyIndex++;
		_history.RemoveAllAt(_historyIndex);
		_history.Add(new HistoryRecord(action, _selection.start, _selection.length, _selection.selectionEndsAtTheStart));
		UpdateCanUndoRedo();
	}

	/// <inheritdoc />
	public void Undo()
	{
		if (!_isSkiaTextBox)
		{
			return;
		}

		TrySetCurrentlyTyping(false);
		if (_historyIndex == 0 || _owner.HasPointerCapture)
		{
			return;
		}

		var currentAction = _history[_historyIndex];
		_historyIndex--;

		_clearHistoryOnTextChanged = false;
		switch (currentAction.Action)
		{
			case ReplaceAction r:
				// remember that we use the possibly-negative format in _pendingSelection
				_pendingSelection = currentAction.SelectionEndsAtTheStart ?
					(currentAction.SelectionStart + currentAction.SelectionLength, -currentAction.SelectionLength) :
					(currentAction.SelectionStart, currentAction.SelectionLength);
				_owner.ProcessTextInput(r.OldText);
				break;
			case DeleteAction d:
				_pendingSelection = (d.UndoSelectionStart, d.UndoSelectionLength);
				_owner.ProcessTextInput(d.OldText);
				break;
			case SentinelAction:
				break;
			default:
				global::System.Diagnostics.CI.Assert(false, "TextBoxActions are not exhaustively switch-matched.");
				break;
		}
		_clearHistoryOnTextChanged = true;
		UpdateCanUndoRedo();
	}

	/// <inheritdoc />
	public void Redo()
	{
		if (!_isSkiaTextBox)
		{
			return;
		}

		if (_historyIndex == _history.Count - 1 || _owner.HasPointerCapture)
		{
			return;
		}

		TrySetCurrentlyTyping(false);

		_historyIndex++;
		var currentAction = _history[_historyIndex];

		_clearHistoryOnTextChanged = false;
		switch (currentAction.Action)
		{
			case ReplaceAction r:
				_pendingSelection = (r.caretIndexAfterReplacement, 0); // we always have an empty selection here.
				_owner.ProcessTextInput(r.NewText);
				break;
			case DeleteAction d:
				_pendingSelection = (Math.Min(d.UndoSelectionStart, d.UndoSelectionStart + d.UndoSelectionLength), 0);
				_owner.ProcessTextInput(d.NewText);
				break;
			case SentinelAction:
				break;
			default:
				global::System.Diagnostics.CI.Assert(false, "TextBoxActions are not exhaustively switch-matched.");
				break;
		}
		_clearHistoryOnTextChanged = true;
		UpdateCanUndoRedo();
	}

	/// <inheritdoc />
	public bool IsManagedEditing => _isSkiaTextBox;

	/// <inheritdoc />
	public int SelectionStartBeforeKeyDown => TextBoxView.SelectionBeforeKeyDown.start;

	/// <inheritdoc />
	public void ResetTextView() => _textBoxView = null;

	/// <inheritdoc />
	public void SetTextNative(string text) => _textBoxView?.SetTextNative(text);

	/// <inheritdoc />
	public void OnTextInputProcessed(string oldText)
	{
		if (_pendingSelection is { } selection && _owner.Text == oldText)
		{
			// OnTextChanged won't fire, so we immediately change the selection.
			// Note how we check that Text (after assignment) == oldText and
			// not oldText == newText. This is because CoerceText can make it so that
			// newText != oldText but Text (after assignment) == oldText
			SelectInternal(selection.start, selection.length);
		}
	}

	/// <inheritdoc />
	public string CoerceMultilineText(string text)
	{
		if (_isSkiaTextBox)
		{
			// WinUI replaces all \n's and and \r\n's by \r. This is annoying because
			// the _pendingSelection uses indices before this removal.
			// On UIKit targets we use invisible overlay and replacing newlines would break the sync between
			// the native input and the managed representation.
			text = RemoveLF(text);
		}

		return text;
	}

	/// <inheritdoc />
	public void ClampPendingSelection(int textLength)
	{
		// make sure this coercion doesn't cause the pending selection to be out of range
		if (_pendingSelection is { } selection2)
		{
			var start = Math.Min(selection2.start, textLength);
			var end = Math.Min(selection2.start + selection2.length, textLength);
			_pendingSelection = (start, end - start);
		}
	}

	/// <inheritdoc />
	public void ClearPendingSelection() => _pendingSelection = null;

	/// <inheritdoc />
	public void OnBeforeTextChangingCanceled()
	{
		if (_isSkiaTextBox)
		{
			// On WinUI, when a selection is canceled, the TextBox invokes a bunch of weird
			// SelectionChanging events followed by a bunch of matching SelectionChanged.
			// Probing for the value of SelectionStart and SelectionLength during these SelectionChanging
			// events will give incorrect transient values and the SelectionChanged events will end up
			// with the selection where it started (before the text change). Also, the direction of
			// of the selection will be reset, i.e. if the selection end was "at the start", then it won't be
			// so anymore.
			// In Uno, we choose a simpler sequence. We just reset the selection direction (like WinUI) and
			// we don't invoke any selection change events (since selection was in fact not changed).
			_pendingSelection = (SelectionStart, SelectionLength);
		}
	}

	/// <inheritdoc />
	public void OnPasteStarting()
	{
		_clearHistoryOnTextChanged = false;
		_suppressCurrentlyTyping = true;
	}

	/// <inheritdoc />
	public void OnPasteFinished()
	{
		_suppressCurrentlyTyping = false;
		_clearHistoryOnTextChanged = true;
		if (_owner.Text.IsNullOrEmpty())
		{
			// On WinUI, the caret never has thumbs if there is no text
			CaretMode = CaretDisplayMode.ThumblessCaretShowing;
		}
	}

	/// <inheritdoc />
	public void OnCutStarting() => _suppressCurrentlyTyping = true;

	/// <inheritdoc />
	public void OnCutFinished() => _suppressCurrentlyTyping = false;

	/// <inheritdoc />
	public void OnFocusedByPointer() => _textBoxNotificationsSingleton?.OnFocused(_owner);

	private bool _pendingUpdateScrolling;

	/// <inheritdoc />
	public void DispatchUpdateScrolling()
	{
		if (!_pendingUpdateScrolling)
		{
			_pendingUpdateScrolling = true;

			// We may be pushing scrolling updates too often
			// when pushing keystrokes programmatically.
			_owner.DispatcherQueue.TryEnqueue(() =>
			{
				_pendingUpdateScrolling = false;

				UpdateScrolling();
			});
		}
	}

	/// <inheritdoc />
	public void InvalidateOverlayLayout() => TextBoxView?.Extension?.InvalidateLayout();

	/// <inheritdoc />
	public void OnPasswordCharChanged()
	{
		// Force display update to refresh the password character
		TextBoxView?.UpdateDisplayBlockText(_owner.Text);
	}

	/// <inheritdoc />
	public void SetPasswordRevealState(PasswordRevealState state) => TextBoxView?.SetPasswordRevealState(state);

	/// <summary>
	/// point is null before first press. repeatedPresses is only valid if point.Pointer.PointerDeviceType
	/// is Mouse.
	/// </summary>
	private (PointerPoint point, int repeatedPresses) _lastPointerDown;
	private (int start, int length, bool tripleTap)? _mouseMultiTapChunk;
	// this is necessary because we can receive a PointerReleased without a PointerPressed (e.g. clicking on the
	// TextBox while the context menu is open to dismiss it). We want to ignore such PointerPressed's.
	private bool _isPressed;

	/// <inheritdoc />
	public void OnPointerMoved(PointerRoutedEventArgs e)
	{
		if (!_isSkiaTextBox || !_owner.HasPointerCapture)
		{
			return;
		}

		if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			// do nothing whether the touch pointer is pressed on not.
			// Moving while pressing the caret thumb or stem will move it. Anything else won't do anything.
		}
		else
		{
			var displayBlock = TextBoxView.DisplayBlock;
			var point = e.GetCurrentPoint(displayBlock);
			var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point.Position, false, true));
			if (_mouseMultiTapChunk is { } mtc)
			{
				(int start, int length) chunk;
				if (mtc.tripleTap)
				{
					chunk = (StartOfLine(index), EndOfLine(index) + 1 - StartOfLine(index));
				}
				else
				{
					chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(index, true);
				}

				if (chunk.start < mtc.start)
				{
					var start = mtc.start + mtc.length;
					var end = chunk.start;
					SelectInternal(start, end - start);
				}
				else if (chunk.start + chunk.length >= mtc.start + mtc.length)
				{
					var start = mtc.start;
					var end = chunk.start + chunk.length;
					SelectInternal(start, end - start);
				}
			}
			else
			{
				var selectionInternalStart = _selection.selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
				SelectInternal(selectionInternalStart, index - selectionInternalStart);
			}
		}
	}

	// TODO: remove this context menu when TextCommandBarFlyout is implemented
	/// <inheritdoc />
	public void OnRightTapped(RightTappedRoutedEventArgs e)
	{
		var displayBlock = TextBoxView.DisplayBlock;
		var position = e.GetPosition(displayBlock);

		var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(position, true, true));
		if (index < SelectionStart || index >= SelectionStart + SelectionLength)
		{
			// Right tapping should move the caret to the current pointer location if outside the selection
			_owner.Select(index, 0);
		}

		OpenContextMenu(position);
	}

	private static bool IsMultiTapGesture((ulong id, ulong ts, Point position) previousTap, PointerPoint down)
	{
		var currentId = down.PointerId;
		var currentTs = down.Timestamp;
		var currentPosition = down.Position;

		return previousTap.id == currentId
			&& currentTs - previousTap.ts <= GestureRecognizer.MultiTapMaxDelayMicroseconds
			&& !GestureRecognizer.IsOutOfTapRange(previousTap.position, currentPosition);
	}

	/// <inheritdoc />
	public void OnPointerPressed(PointerRoutedEventArgs args)
	{
		_isPressed = true;
		TrySetCurrentlyTyping(false);
		_contextMenu?.Close();

		if (!_isSkiaTextBox)
		{
			return;
		}

		var currentPoint = args.GetCurrentPoint(null);
		if (args.Pointer.PointerDeviceType == PointerDeviceType.Touch)
		{
			// we handle touch on the PointerReleased end
			_lastPointerDown = (currentPoint, 0);
		}
		else if (!currentPoint.Properties.IsRightButtonPressed) // Mouse (a pen is considered a mouse for now)
		{
			var displayBlock = TextBoxView.DisplayBlock;
			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(args.GetCurrentPoint(displayBlock).Position, true, true));

			if (currentPoint.Properties.IsLeftButtonPressed
				&& _lastPointerDown.point is { } p
				&& IsMultiTapGesture((p.PointerId, p.Timestamp, p.Position), currentPoint))
			{
				// multiple left presses

				if (_lastPointerDown.repeatedPresses == 1)
				{
					// triple tap

					var startOfLine = StartOfLine(index);
					_owner.Select(startOfLine, EndOfLine(index) + 1 - startOfLine);
					_mouseMultiTapChunk = (SelectionStart, SelectionLength, true);
					_lastPointerDown = (currentPoint, 2);
				}
				else // _lastPointerDown.repeatedPresses == 0 or 2
				{
					// double tap
					var chunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(index, true);
					_owner.Select(chunk.start, chunk.length);
					_mouseMultiTapChunk = (chunk.start, chunk.length, false);
					_lastPointerDown = (currentPoint, 1);
				}
			}
			else
			{
				// single click
				CaretMode = CaretDisplayMode.ThumblessCaretShowing;
				if ((args.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
				{
					var selectionInternalStart = _selection.selectionEndsAtTheStart ? _selection.start + _selection.length : _selection.start;
					SelectInternal(selectionInternalStart, index - selectionInternalStart);
				}
				else
				{
					_owner.Select(index, 0);
				}
				_lastPointerDown = (currentPoint, 0);
			}
		}
	}

	/// <inheritdoc />
	public void OnPointerReleased(PointerRoutedEventArgs args, bool wasFocused)
	{
		_mouseMultiTapChunk = null;

		if (!_isPressed || args.Pointer.PointerDeviceType is not PointerDeviceType.Touch)
		{
			// Mouse is handled on the PointerPressed side
			return;
		}

		_isPressed = false;

		if ((args.GetCurrentPoint(null).Timestamp - _lastPointerDown.point.Timestamp) >= GestureRecognizer.HoldMinDelayMicroseconds)
		{
			// Touch holding
			OpenContextMenu(args.GetCurrentPoint(_owner).Position);
		}
		else if (!_owner.Text.IsNullOrEmpty()) // Touch tap
		{
			TouchTap(args.GetCurrentPoint(TextBoxView.DisplayBlock).Position, wasFocused);
		}
	}

	private void TouchTap(Point point, bool wasFocused)
	{
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, true, true));

		var tappedChunk = TextBoxView.DisplayBlock.ParsedText.GetWordAt(index, true);

		var tappedInsideSelection = _selection.start <= index && index < _selection.start + _selection.length;
		if (tappedInsideSelection && CaretMode != CaretDisplayMode.CaretWithThumbsBothEndsShowing)
		{
			CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
		}
		else if (_selection.length == 0 && TextBoxView.DisplayBlock.ParsedText.GetWordAt(_selection.start, true) is var currentChunk && currentChunk.start <= index && index < currentChunk.start + currentChunk.length)
		{
			_owner.Select(tappedChunk.start, tappedChunk.length); // touch selection doesn't go backwards (no "negative length")
			CaretMode = CaretDisplayMode.CaretWithThumbsBothEndsShowing;
		}
		else
		{
			var lastNonSpanCharIndex = _owner.Text[tappedChunk.start..(tappedChunk.start + tappedChunk.length)].IndexOf(' ');
			var rightEndIndex = lastNonSpanCharIndex == -1 ? tappedChunk.start + tappedChunk.length - 1 : tappedChunk.start + lastNonSpanCharIndex;
			var leftEndIndex = tappedChunk.start;
			var leftEnd = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(leftEndIndex);
			var rightEnd = TextBoxView.DisplayBlock.ParsedText.GetRectForIndex(rightEndIndex);

			var closerEnd = Math.Abs(point.X - leftEnd.Left) < Math.Abs(point.X - rightEnd.Right) ? leftEndIndex : rightEndIndex + 1;

			if (wasFocused) // If we were not focused before, caret should be initially thumbless.
			{
				CaretMode = CaretDisplayMode.CaretWithThumbsOnlyEndShowing;
			}

			_owner.Select(closerEnd, 0);
		}
	}

	/// <inheritdoc />
	public void OnPointerCaptureLost(PointerRoutedEventArgs e)
	{
		_isPressed = false;
		_mouseMultiTapChunk = null;
	}

	private void CaretOnPointerPressed(object sender, PointerRoutedEventArgs args)
	{
		args.Handled = true;

		var caret = (CaretWithStemAndThumb)sender;
		if (caret.CapturePointer(args.Pointer))
		{
			caret.SetStemVisible(true);
		}

		caret.LastPointerDown = args.GetCurrentPoint(null);
	}

	private void CaretOnPointerMoved(object sender, PointerRoutedEventArgs args)
	{
		var caret = (CaretWithStemAndThumb)sender;
		if (!caret.HasPointerCapture)
		{
			return;
		}
		args.Handled = true;

		var displayBlock = TextBoxView.DisplayBlock;
		var point = args.GetCurrentPoint(displayBlock).Position - new Point(0, (caret.Height - 16) / 2);
		var index = Math.Max(0, TextBoxView.DisplayBlock.ParsedText.GetIndexAt(point, false, true));

		if (_selection.length == 0)
		{
			Debug.Assert(caret == _selectionEndThumbfulCaret);
			_owner.Select(index, 0);
		}
		else
		{
			Debug.Assert(CaretMode == CaretDisplayMode.CaretWithThumbsBothEndsShowing);
			var (start, end) = (_selection.start, _selection.start + _selection.length);
			if (sender == _selectionStartThumbfulCaret)
			{
				start = index;
			}
			else
			{
				end = index;
			}

			if (start != end) // if start == end, we do nothing like WinUI. This means that the 2 carets won't be on top of one another
			{
				SelectInternal(start, end - start);

				if (end < start)
				{
					// If we're here this means that the "selection end caret" was dragging "behind" the "selection start caret".
					// We swap which caret we consider the "selection start caret" now that the "end caret" is actually before the
					// "start caret".
					(_selectionStartThumbfulCaret, _selectionEndThumbfulCaret) = (_selectionEndThumbfulCaret, _selectionStartThumbfulCaret);
				}
			}
		}

		UpdateScrolling(caret == _selectionEndThumbfulCaret);
	}

	private void CaretOnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		ClearCaretPointerState(sender, e);

		var caret = (CaretWithStemAndThumb)sender;

		var previous = caret.LastPointerDown;
		if (IsMultiTapGesture((previous.PointerId, previous.Timestamp, previous.Position), e.GetCurrentPoint(null)))
		{
			e.Handled = true;
			TouchTap(e.GetCurrentPoint(TextBoxView.DisplayBlock).Position, true);
		}
	}

	private void ClearCaretPointerState(object sender, PointerRoutedEventArgs args)
	{
		args.Handled = true;
		var caret = (CaretWithStemAndThumb)sender;
		caret.SetStemVisible(false);
		caret.ReleasePointerCaptures();
	}

	private void OpenContextMenu(Point p)
	{
		if (_isSkiaTextBox)
		{
			if (_contextMenu is null)
			{
				_contextMenu = new MenuFlyout();
				_contextMenu.Opened += (_, _) => UpdateDisplaySelection();
				// The menu borrowing focus is not the user leaving the control:
				// the software keyboard must not hide on open / re-show on Paste.
				_contextMenu.DoesNotAffectSoftwareKeyboard = true;
				_contextMenuKeyboardGuard = new SoftwareKeyboardFlyoutGuard(_owner, _contextMenu,
					() => _textBoxNotificationsSingleton?.OnUnfocused(_owner));

				_flyoutItems.Add(ContextMenuItem.Cut, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_CUT"), Command = new StandardUICommand(StandardUICommandKind.Cut) { Command = new TextBoxCommand(_owner.CutSelectionToClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Copy, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_COPY"), Command = new StandardUICommand(StandardUICommandKind.Copy) { Command = new TextBoxCommand(_owner.CopySelectionToClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Paste, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_PASTE"), Command = new StandardUICommand(StandardUICommandKind.Paste) { Command = new TextBoxCommand(_owner.PasteFromClipboard) } });
				_flyoutItems.Add(ContextMenuItem.Undo, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_UNDO"), Command = new StandardUICommand(StandardUICommandKind.Undo) { Command = new TextBoxCommand(Undo) } });
				_flyoutItems.Add(ContextMenuItem.Redo, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_REDO"), Command = new StandardUICommand(StandardUICommandKind.Redo) { Command = new TextBoxCommand(Redo) } });
				_flyoutItems.Add(ContextMenuItem.SelectAll, new MenuFlyoutItem { Text = ResourceAccessor.GetLocalizedStringResource("TEXT_CONTEXT_MENU_SELECT_ALL"), Command = new StandardUICommand(StandardUICommandKind.SelectAll) { Command = new TextBoxCommand(_owner.SelectAll) } });
			}

			_contextMenu.Items.Clear();

			var hasSelection = _selection.length > 0;

			if (!_owner.IsReadOnly && hasSelection)
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Cut]);
			}

			if (hasSelection)
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Copy]);
			}

			if (!_owner.IsReadOnly)
			{
				_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Paste]);
				if (_owner.CanUndo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Undo]);
				}
				if (_owner.CanRedo)
				{
					_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.Redo]);
				}
			}

			_contextMenu.Items.Add(_flyoutItems[ContextMenuItem.SelectAll]);

			_contextMenu.ShowAt(_owner, p);
		}
	}

	private record struct HistoryRecord(TextBoxAction Action, int SelectionStart, int SelectionLength, bool SelectionEndsAtTheStart);

	private abstract record TextBoxAction;

	/// <summary>
	/// Instead of remembered what was removed and what was added in place, we just remember the initial and final states
	/// as well as where the caret will be if we Redo. This is used by typing, paste, etc.
	/// </summary>
	private record ReplaceAction(string OldText, string NewText, int caretIndexAfterReplacement) : TextBoxAction;

	/// <summary>
	/// Unlike other forms of text modification, Delete doesn't follow the simple undo sequence of *unapply modification* -> *select what was selected when the action happened*
	/// So we need to specifically need to remember what selection to go to when we Undo depending on how we got here (e.g. ctrl vs no ctrl)
	/// Selection uses the possibly-negative format
	/// </summary>
	private record DeleteAction(string OldText, string NewText, int UndoSelectionStart, int UndoSelectionLength) : TextBoxAction;

	/// <summary>
	/// Probably unnecessary, but we pad the bottom of the history as it makes index manipulation easier (the invariant we
	/// get is that history is never empty)
	/// </summary>
	private record SentinelAction : TextBoxAction
	{
		private SentinelAction() { }
		public static SentinelAction Instance { get; } = new SentinelAction();
	}

	private sealed class TextBoxCommand(Action action) : ICommand
	{
		public bool CanExecute(object parameter) => true;

		public void Execute(object parameter) => action();

#pragma warning disable 67 // An event was declared but never used in the class in which it was declared.
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67 // An event was declared but never used in the class in which it was declared.
	}

	private class CaretWithStemAndThumb : Grid
	{
		// This is equal to the default system accent color on Windows.
		// This is, however, a constant color that doesn't depend on the
		// current system accent color. Changing the accent color does NOT
		// change the thumb color on WinUI, only the selection color.
		private static readonly Color ThumbFillColor = Colors.FromARGB("FF0078D7");

		private readonly Ellipse _thumb;
		private readonly Ellipse _thumbRing;
		private readonly Rectangle _stem;
		private Popup _popup;

		public PointerPoint LastPointerDown { get; set; }

		public CaretWithStemAndThumb()
		{
			// Numbers and colors below are partially measured by hand from WinUI and partially made up to be reasonable.

			Background = new SolidColorBrush(Colors.Transparent); // to hit-test positively everywhere in the grid

			Width = 16;

			RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			RowDefinitions.Add(new RowDefinition { Height = new GridLength(16, GridUnitType.Pixel) });

			_thumb = new Ellipse
			{
				Fill = new SolidColorBrush(Colors.White),
				Width = 16,
				Height = 16
			};

			_thumbRing = new Ellipse
			{
				Stroke = new SolidColorBrush(ThumbFillColor),
				StrokeThickness = 2,
				Width = 14,
				Height = 14,
				Margin = new Thickness(1)
			};

			_stem = new Rectangle
			{
				Visibility = Visibility.Collapsed,
				IsHitTestVisible = false,
				HorizontalAlignment = HorizontalAlignment.Center,
				Stroke = new SolidColorBrush(ThumbFillColor),
				Width = 2
			};

			Grid.SetRow(_stem, 0);
			Grid.SetRow(_thumb, 1);
			Grid.SetRow(_thumbRing, 1);

			Children.Add(_stem);
			Children.Add(_thumb);
			Children.Add(_thumbRing);
		}

		public void SetStemVisible(bool visible) => _stem.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

		public void ShowAt(XamlRoot xamlRoot, Matrix3x2 transform)
		{
			_popup ??= new Popup
			{
				Child = this,
				IsLightDismissEnabled = false,
				XamlRoot = xamlRoot
			};
			_popup.PopupPanel.Visual.ZIndex = VisualTree.TextBoxTouchKnobPopupZIndex;

			if (this.RenderTransform is not MatrixTransform matrixTransform)
			{
				matrixTransform = new MatrixTransform();
				this.RenderTransform = matrixTransform;
			}
			matrixTransform.Matrix = new Matrix(transform);
			if (!_popup.IsOpen)
			{
				_popup.IsOpen = true;
				((CompositionTarget)Visual.CompositionTarget)!.FrameRendered += OnFrameRendered;
			}
		}

		private void OnFrameRendered()
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				if (FocusManager.GetFocusedElement(XamlRoot!) is TextBox textBox)
				{
					TextBoxSkiaPlatform.Of(textBox).UpdateFlyoutPosition();
				}
			}, NativeDispatcherPriority.Idle);
		}

		public void Hide()
		{
			if (Visual.CompositionTarget is CompositionTarget target)
			{
				target.FrameRendered -= OnFrameRendered;
			}
			if (_popup is not null)
			{
				_popup.IsOpen = false;
			}
		}
	}
}
