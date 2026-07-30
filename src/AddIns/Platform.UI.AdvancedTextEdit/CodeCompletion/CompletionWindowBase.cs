#nullable enable

using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/CompletionWindowBase.cs in the AvalonEdit
//repo (MIT). The completion "window" is re-expressed as an in-app XAML popup instead of a WPF
//top-level Window (locked design decision):
//- The class is a plain object owning a Microsoft.UI.Xaml.Controls.Primitives.Popup (never
//  light-dismiss, never focused) so keyboard input keeps flowing to the text area; the WPF
//  Window machinery (Owner, WindowStyle, ShowActivated, OnSourceInitialized, OnDeactivated,
//  RaiseEventPair with simulated tunnel/bubble key events) is gone. Keys reach the window
//  through the stacked input handler's OnPreviewKeyDown/Up calling the OnKeyDown/OnKeyUp
//  virtual methods directly.
//- Positioning uses XamlRoot coordinates instead of screen coordinates: the caret location is
//  transformed from the text view to the XamlRoot content and clamped/flipped against the
//  XamlRoot bounds (the in-app replacement for the screen working area).
//- CloseOnFocusLost closes when the text area loses focus, unless the popup content itself
//  took the focus; parentWindow.LocationChanged repositioning became XamlRoot.Changed, and
//  clicking into the popup refocuses the text area (was: ActivateParentWindow).

/// <summary>
/// Base class for completion windows. Handles positioning the completion popup at the caret.
/// </summary>
/// <remarks>
/// Despite the name (kept from the original code base), this class is not a top-level window:
/// it manages an in-app popup anchored to the text area's XamlRoot.
/// </remarks>
public class CompletionWindowBase
{
	readonly Microsoft.UI.Xaml.Controls.Primitives.Popup popup;
	readonly PointerEventHandler popupChildPointerReleasedHandler;
	readonly SizeChangedEventHandler popupChildSizeChangedHandler;
	TextDocument? document;
	InputHandler? myInputHandler;
	XamlRoot? attachedXamlRoot;
	UIElement? popupChild;
	object? content;
	bool positionInitialized;
	bool isClosed;

	/// <summary>
	/// Gets the parent TextArea.
	/// </summary>
	public TextArea TextArea { get; private set; }

	/// <summary>
	/// Gets the popup that presents the completion window content inside the application.
	/// </summary>
	//was previously: the class itself was the WPF Window; derived classes reach the popup for
	//placement-related work (e.g. positioning a secondary description popup next to it).
	protected Microsoft.UI.Xaml.Controls.Primitives.Popup Popup
	{
		get { return popup; }
	}

	/// <summary>
	/// Gets/Sets the start of the text range in which the completion window stays open.
	/// This text portion is used to determine the text used to select an entry in the completion list by typing.
	/// </summary>
	public int StartOffset { get; set; }

	/// <summary>
	/// Gets/Sets the end of the text range in which the completion window stays open.
	/// This text portion is used to determine the text used to select an entry in the completion list by typing.
	/// </summary>
	public int EndOffset { get; set; }

	/// <summary>
	/// Gets whether the window was opened above the current line.
	/// </summary>
	protected bool IsUp { get; private set; }

	/// <summary>
	/// Gets whether the completion popup is currently open.
	/// </summary>
	//was previously: Window.IsVisible.
	public bool IsOpen
	{
		get { return popup.IsOpen; }
	}

	/// <summary>
	/// Gets/Sets the content of the completion window. A UIElement is shown as-is; a string is
	/// shown as a wrapping TextBlock.
	/// </summary>
	//was previously: Window.Content.
	public object? Content
	{
		get { return content; }
		set
		{
			content = value;
			OnContentChanged();
		}
	}

	/// <summary>
	/// Occurs after the completion window was closed.
	/// </summary>
	//was previously: Window.Closed.
	public event EventHandler? Closed;

	/// <summary>
	/// Creates a new CompletionWindowBase.
	/// </summary>
	public CompletionWindowBase(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		this.TextArea = textArea;

		//was previously: parentWindow = Window.GetWindow(textArea); this.Owner = parentWindow;
		//window-style/activation metadata overrides. The popup below replaces all of it.
		popup = new Microsoft.UI.Xaml.Controls.Primitives.Popup
		{
			// Light dismiss must stay off: the popup never takes focus, and typing in the text
			// area must not close it.
			IsLightDismissEnabled = false
		};
		popup.Closed += Popup_Closed;
		popupChildPointerReleasedHandler = new PointerEventHandler(PopupChild_PointerReleased);
		popupChildSizeChangedHandler = new SizeChangedEventHandler(PopupChild_SizeChanged);

		StartOffset = EndOffset = this.TextArea.Caret.Offset;

		AttachEvents();
	}

	#region Content element handling
	/// <summary>
	/// Called when <see cref="Content"/> changes. The default implementation places the content
	/// element directly into the popup.
	/// </summary>
	protected virtual void OnContentChanged()
	{
		SetPopupChild(CreateContentElement(content));
	}

	/// <summary>
	/// Turns window content into an element: UIElements pass through, strings become wrapping
	/// TextBlocks, other objects are shown via their ToString() text, null stays null.
	/// </summary>
	/// <param name="contentValue">The content value to convert.</param>
	/// <param name="foreground">Optional explicit foreground for generated TextBlocks.</param>
	//was previously: WPF templates used ContentPresenters plus the InsightWindowTemplateSelector
	//(string -> wrapping TextBlock); this port builds the equivalent element in code.
	protected static UIElement? CreateContentElement(object? contentValue, Brush? foreground = null)
	{
		if (contentValue == null)
			return null;
		if (contentValue is UIElement element)
			return element;
		TextBlock textBlock = new TextBlock
		{
			Text = contentValue as string ?? contentValue.ToString(),
			TextWrapping = TextWrapping.Wrap
		};
		if (foreground != null)
			textBlock.Foreground = foreground;
		return textBlock;
	}

	/// <summary>
	/// Sets the popup child and keeps the pointer-released and size-changed hooks attached to it.
	/// </summary>
	private protected void SetPopupChild(UIElement? element)
	{
		if (popupChild == element)
			return;
		if (popupChild != null)
		{
			popupChild.RemoveHandler(UIElement.PointerReleasedEvent, popupChildPointerReleasedHandler);
			if (popupChild is FrameworkElement oldElement)
				oldElement.SizeChanged -= popupChildSizeChangedHandler;
		}
		popupChild = element;
		if (popupChild != null)
		{
			//was previously: this.AddHandler(MouseUpEvent, OnMouseUp, handledEventsToo: true).
			popupChild.AddHandler(UIElement.PointerReleasedEvent, popupChildPointerReleasedHandler, true);
			if (popupChild is FrameworkElement newElement)
				newElement.SizeChanged += popupChildSizeChangedHandler;
		}
		popup.Child = element;
	}
	#endregion

	#region Event Handlers
	void AttachEvents()
	{
		document = this.TextArea.Document;
		if (document != null)
		{
			document.Changing += TextAreaDocumentChanging;
		}
		//was previously: this.TextArea.LostKeyboardFocus += TextAreaLostFocus ("more reliable
		//than PreviewLostKeyboardFocus"); LostFocus is the seam in this framework.
		this.TextArea.LostFocus += TextAreaLostFocus;
		this.TextArea.TextView.ScrollOffsetChanged += TextViewScrollOffsetChanged;
		this.TextArea.TextView.VisualLinesChanged += TextViewVisualLinesChanged;
		this.TextArea.DocumentChanged += TextAreaDocumentChanged;
		//was previously: parentWindow.LocationChanged += parentWindow_LocationChanged; the
		//XamlRoot.Changed subscription in Show() is the counterpart.

		// close previous completion windows of same type
		foreach (InputHandler x in this.TextArea.StackedInputHandlers.OfType<InputHandler>())
		{
			if (x.window.GetType() == this.GetType())
				this.TextArea.PopStackedInputHandler(x);
		}

		myInputHandler = new InputHandler(this);
		this.TextArea.PushStackedInputHandler(myInputHandler);
	}

	/// <summary>
	/// Detaches events from the text area.
	/// </summary>
	protected virtual void DetachEvents()
	{
		if (document != null)
		{
			document.Changing -= TextAreaDocumentChanging;
		}
		this.TextArea.LostFocus -= TextAreaLostFocus;
		this.TextArea.TextView.ScrollOffsetChanged -= TextViewScrollOffsetChanged;
		this.TextArea.TextView.VisualLinesChanged -= TextViewVisualLinesChanged;
		this.TextArea.DocumentChanged -= TextAreaDocumentChanged;
		if (attachedXamlRoot != null)
		{
			attachedXamlRoot.Changed -= XamlRoot_Changed;
			attachedXamlRoot = null;
		}
		if (myInputHandler != null)
			this.TextArea.PopStackedInputHandler(myInputHandler);
	}

	#region InputHandler
	/// <summary>
	/// A dummy input handler (that justs invokes the default input handler).
	/// This is used to ensure the completion window closes when any other input handler
	/// becomes active.
	/// </summary>
	sealed class InputHandler : TextAreaStackedInputHandler
	{
		internal readonly CompletionWindowBase window;

		public InputHandler(CompletionWindowBase window)
			: base(window.TextArea)
		{
			Debug.Assert(window != null);
			this.window = window;
		}

		public override void Detach()
		{
			base.Detach();
			window.Close();
		}

		//was previously: the handlers re-raised simulated tunnel/bubble key event pairs on the
		//WPF window (RaiseEventPair) and skipped Key.DeadCharProcessed; this port calls the
		//window's key virtuals directly, and there is no dead-char key in this framework.
		public override bool OnPreviewKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			return window.OnKeyDown(key, modifiers);
		}

		public override bool OnPreviewKeyUp(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			return window.OnKeyUp(key, modifiers);
		}
	}
	#endregion

	void TextViewScrollOffsetChanged(object? sender, EventArgs e)
	{
		if (!positionInitialized)
			return;

		TextView textView = this.TextArea.TextView;
		Rect visibleRect = new Rect(textView.HorizontalOffset, textView.VerticalOffset,
									textView.ViewportWidth, textView.ViewportHeight);
		// close completion window when the user scrolls so far that the anchor position is leaving the visible area
		if (visibleRect.Contains(visualLocation) || visibleRect.Contains(visualLocationTop))
			UpdatePosition();
		else
			Close();
	}

	void TextViewVisualLinesChanged(object? sender, EventArgs e)
	{
		//was previously: not handled (the WPF window tracked parentWindow.LocationChanged
		//instead); with an in-app popup, line-layout changes move the anchor line, so the popup
		//repositions. Deferred because the visual lines are still being rebuilt while this
		//event is raised.
		if (!positionInitialized)
			return;
		var dispatcherQueue = this.TextArea.DispatcherQueue;
		if (dispatcherQueue != null)
		{
			dispatcherQueue.TryEnqueue(() =>
			{
				if (!isClosed)
					RefreshPosition();
			});
		}
		else
		{
			RefreshPosition();
		}
	}

	void TextAreaDocumentChanged(object? sender, EventArgs e)
	{
		Close();
	}

	void TextAreaLostFocus(object? sender, RoutedEventArgs e)
	{
		//was previously: Dispatcher.BeginInvoke(CloseIfFocusLost, DispatcherPriority.Background).
		var dispatcherQueue = this.TextArea.DispatcherQueue;
		if (dispatcherQueue != null)
			dispatcherQueue.TryEnqueue(CloseIfFocusLost);
		else
			CloseIfFocusLost();
	}

	void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		//was previously: parentWindow_LocationChanged -> UpdatePosition().
		if (positionInitialized && !isClosed)
			UpdatePosition();
	}

	void Popup_Closed(object? sender, object e)
	{
		// Defensive: if anything else closes the popup, run the full close path so the
		// stacked input handler is popped and the Closed event is raised.
		if (!isClosed)
			Close();
	}
	#endregion

	// Special handler: handledEventsToo
	void PopupChild_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		ActivateParentWindow();
	}

	void PopupChild_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		//was previously: OnRenderSizeChanged - when the window is above the line and its height
		//changes, keep the bottom edge anchored.
		if (!positionInitialized)
			return;
		if (IsUp && e.PreviousSize.Height != e.NewSize.Height)
		{
			popup.VerticalOffset += e.PreviousSize.Height - e.NewSize.Height;
		}
	}

	/// <summary>
	/// Returns the keyboard focus to the text area, so that typing continues to flow into the
	/// editor after the user clicked into the completion popup.
	/// </summary>
	//was previously: ActivateParentWindow() -> parentWindow.Activate(); there is no separate
	//top-level window to activate for an in-app popup.
	protected virtual void ActivateParentWindow()
	{
		this.TextArea.Focus(FocusState.Programmatic);
	}

	void CloseIfFocusLost()
	{
		if (isClosed)
			return;
		if (CloseOnFocusLost)
		{
			if (!this.TextArea.IsKeyboardFocused && !IsPopupContentFocused)
			{
				Close();
			}
		}
	}

	/// <summary>
	/// Gets whether the completion window should automatically close when the text editor looses focus.
	/// </summary>
	protected virtual bool CloseOnFocusLost
	{
		get { return true; }
	}

	bool IsPopupContentFocused
	{
		get
		{
			UIElement? child = popup.Child;
			if (child == null)
				return false;
			XamlRoot? xamlRoot = popup.XamlRoot ?? this.TextArea.XamlRoot;
			if (xamlRoot == null)
				return false;
			DependencyObject? focused = FocusManager.GetFocusedElement(xamlRoot) as DependencyObject;
			while (focused != null)
			{
				if (focused == child)
					return true;
				focused = VisualTreeHelper.GetParent(focused);
			}
			return false;
		}
	}

	/// <summary>
	/// Called for key-down events while the completion window is open (the text area forwards
	/// them through the stacked input handler before processing them itself).
	/// Escape closes the window.
	/// </summary>
	/// <param name="key">The key that was pressed.</param>
	/// <param name="modifiers">The modifier keys active for the key press.</param>
	/// <returns>True to mark the key press handled; false to let the text area process it.</returns>
	//was previously: protected override void OnKeyDown(KeyEventArgs e) on the WPF window.
	protected virtual bool OnKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		if (key == VirtualKey.Escape)
		{
			Close();
			return true;
		}
		return false;
	}

	/// <summary>
	/// Called for key-up events while the completion window is open. The default implementation
	/// does nothing.
	/// </summary>
	/// <param name="key">The key that was released.</param>
	/// <param name="modifiers">The modifier keys active for the key release.</param>
	/// <returns>True to mark the key release handled; false to let the text area process it.</returns>
	protected virtual bool OnKeyUp(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		return false;
	}

	#region Show / Close
	/// <summary>
	/// Opens the completion popup, positioned at <see cref="StartOffset"/> (or the caret when
	/// the start offset is at the caret).
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// The window was already closed, or the text area is not loaded in a visual tree yet.
	/// </exception>
	//was previously: Window.Show(); the position setup lived in OnSourceInitialized.
	public virtual void Show()
	{
		if (isClosed)
			throw new InvalidOperationException("Cannot show a completion window that was already closed.");
		if (popup.IsOpen)
			return;
		XamlRoot? xamlRoot = this.TextArea.XamlRoot;
		if (xamlRoot == null)
			throw new InvalidOperationException("The text area must be loaded in a visual tree before the completion window can be shown.");
		if (popup.XamlRoot != xamlRoot)
			popup.XamlRoot = xamlRoot;
		if (attachedXamlRoot != xamlRoot)
		{
			if (attachedXamlRoot != null)
				attachedXamlRoot.Changed -= XamlRoot_Changed;
			attachedXamlRoot = xamlRoot;
			xamlRoot.Changed += XamlRoot_Changed;
		}

		if (document != null && this.StartOffset != this.TextArea.Caret.Offset)
		{
			SetPosition(new TextViewPosition(document.GetLocation(this.StartOffset)));
		}
		else
		{
			SetPosition(this.TextArea.Caret.Position);
		}
		positionInitialized = true;

		popup.IsOpen = true;
	}

	/// <summary>
	/// Closes the completion popup. A closed completion window cannot be shown again.
	/// </summary>
	//was previously: Window.Close().
	public void Close()
	{
		if (isClosed)
			return;
		isClosed = true;
		popup.IsOpen = false;
		OnClosed(EventArgs.Empty);
	}

	/// <summary>
	/// Raises the <see cref="Closed"/> event and detaches the text area events.
	/// </summary>
	protected virtual void OnClosed(EventArgs e)
	{
		Closed?.Invoke(this, e);
		DetachEvents();
	}
	#endregion

	#region Positioning
	Point visualLocation, visualLocationTop;
	TextViewPosition anchorPosition;

	/// <summary>
	/// Positions the completion window at the specified position.
	/// </summary>
	protected void SetPosition(TextViewPosition position)
	{
		TextView textView = this.TextArea.TextView;

		anchorPosition = position;
		visualLocation = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		visualLocationTop = textView.GetVisualPosition(position, VisualYPosition.LineTop);
		UpdatePosition();
	}

	void RefreshPosition()
	{
		RefreshVisualLocation();
		UpdatePosition();
	}

	void RefreshVisualLocation()
	{
		// Re-derive the anchor pixels after the visual lines changed. The stored position can
		// have gone stale (the document may have changed since SetPosition); when it no longer
		// exists, keep the previous pixels.
		TextDocument? currentDocument = this.TextArea.Document;
		if (currentDocument == null)
			return;
		if (anchorPosition.Line < 1 || anchorPosition.Line > currentDocument.LineCount)
			return;
		DocumentLine line = currentDocument.GetLineByNumber(anchorPosition.Line);
		if (anchorPosition.Column < 1 || anchorPosition.Column > line.Length + 1)
			return;
		TextView textView = this.TextArea.TextView;
		TextViewPosition position = new TextViewPosition(anchorPosition.Location);
		visualLocation = textView.GetVisualPosition(position, VisualYPosition.LineBottom);
		visualLocationTop = textView.GetVisualPosition(position, VisualYPosition.LineTop);
	}

	/// <summary>
	/// Updates the position of the completion popup based on the text view position and the
	/// XamlRoot bounds. It ensures that the popup stays completely visible inside the XamlRoot.
	/// </summary>
	//was previously: non-virtual, positioning the WPF window in screen coordinates against the
	//screen working area; virtual here so derived classes can move companion popups along.
	protected virtual void UpdatePosition()
	{
		TextView textView = this.TextArea.TextView;
		XamlRoot? xamlRoot = popup.XamlRoot ?? this.TextArea.XamlRoot;
		UIElement? rootContent = xamlRoot?.Content;
		if (xamlRoot == null || rootContent == null)
			return;

		GeneralTransform transform;
		try
		{
			transform = textView.TransformToVisual(rootContent);
		}
		catch (ArgumentException)
		{
			// the text view is not connected to the XamlRoot content (yet)
			return;
		}
		Point location = transform.TransformPoint(
			new Point(visualLocation.X - textView.HorizontalOffset,
					  visualLocation.Y - textView.VerticalOffset));
		Point locationTop = transform.TransformPoint(
			new Point(visualLocationTop.X - textView.HorizontalOffset,
					  visualLocationTop.Y - textView.VerticalOffset));

		Size completionWindowSize = GetPopupContentSize(xamlRoot);
		Rect bounds = new Rect(location, completionWindowSize);
		Rect workingArea = new Rect(0, 0, xamlRoot.Size.Width, xamlRoot.Size.Height);
		if (!Contains(workingArea, bounds))
		{
			if (bounds.Left < workingArea.Left)
			{
				bounds.X = workingArea.Left;
			}
			else if (bounds.Right > workingArea.Right)
			{
				bounds.X = workingArea.Right - bounds.Width;
			}
			if (bounds.Bottom > workingArea.Bottom)
			{
				bounds.Y = locationTop.Y - bounds.Height;
				IsUp = true;
			}
			else
			{
				IsUp = false;
			}
			if (bounds.Y < workingArea.Top)
			{
				bounds.Y = workingArea.Top;
			}
		}
		popup.HorizontalOffset = bounds.X;
		popup.VerticalOffset = bounds.Y;
	}

	Size GetPopupContentSize(XamlRoot xamlRoot)
	{
		if (popup.Child is FrameworkElement child)
		{
			if (child.ActualWidth > 0 || child.ActualHeight > 0)
				return new Size(child.ActualWidth, child.ActualHeight);
			// not laid out yet - measure to get the initial size
			child.Measure(new Size(xamlRoot.Size.Width, xamlRoot.Size.Height));
			return child.DesiredSize;
		}
		return new Size(0, 0);
	}

	static bool Contains(Rect outer, Rect inner)
	{
		//was previously: Rect.Contains(Rect); Windows.Foundation.Rect has no rect overload.
		return inner.Left >= outer.Left && inner.Right <= outer.Right
			&& inner.Top >= outer.Top && inner.Bottom <= outer.Bottom;
	}
	#endregion

	/// <summary>
	/// Gets/sets whether the completion window should expect text insertion at the start offset,
	/// which not go into the completion region, but before it.
	/// </summary>
	/// <remarks>This property allows only a single insertion, it is reset to false
	/// when that insertion has occurred.</remarks>
	public bool ExpectInsertionBeforeStart { get; set; }

	void TextAreaDocumentChanging(object? sender, DocumentChangeEventArgs e)
	{
		if (e.Offset + e.RemovalLength == this.StartOffset && e.RemovalLength > 0)
		{
			Close(); // removal immediately in front of completion segment: close the window
					 // this is necessary when pressing backspace after dot-completion
		}
		if (e.Offset == StartOffset && e.RemovalLength == 0 && ExpectInsertionBeforeStart)
		{
			StartOffset = e.GetNewOffset(StartOffset, AnchorMovementType.AfterInsertion);
			this.ExpectInsertionBeforeStart = false;
		}
		else
		{
			StartOffset = e.GetNewOffset(StartOffset, AnchorMovementType.BeforeInsertion);
		}
		EndOffset = e.GetNewOffset(EndOffset, AnchorMovementType.AfterInsertion);
	}
}
