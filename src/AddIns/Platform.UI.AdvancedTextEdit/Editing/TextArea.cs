#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Indentation;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;
using CodeBrix.Platform.UI.Xaml.Controls.Extensions;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

//was previously: ICSharpCode.AvalonEdit/Editing/TextArea.cs in the AvalonEdit repo (MIT).
//The input/selection/caret orchestration is transliterated. Structural re-expressions:
//- IScrollInfo forwarding is gone: the TextView owns the scroll surface directly
//  (HorizontalOffset/VerticalOffset/SetScrollOffset/MakeVisible); the pointer wheel scrolls
//  through it here (3 * TextView.DefaultLineHeight per notch, Shift = horizontal).
//- Key input: OnKeyDown offers the key press to the stacked input handlers (reverse push order),
//  then to the active input handler's HandleKeyDown (the port's replacement for WPF command
//  routing), and finally treats KeyRoutedEventArgs.UnicodeKey as typed text (WPF OnTextInput);
//  Enter and Tab are claimed by the command system and never doubly inserted here.
//- The clipboard veto seam raises plain .NET events (DataObjectCopying/DataObjectSettingData/
//  DataObjectPasting) instead of WPF's DataObject.* attached events, and TextEntering/TextEntered
//  carry the port's TextInputEventArgs instead of WPF TextCompositionEventArgs.
//- The control builds its visual tree in code (margins in auto grid columns + the text view in a
//  star column) instead of a theme ControlTemplate; SelectionBorder (a WPF Pen) became the
//  SelectionBorderBrush + SelectionBorderThickness pair.
//- Dropped, with no counterpart in this framework: IME support (ImeSupport), the UI-automation
//  peer, CommandManager.InvalidateRequerySuggested (can-execute is queried on each key press),
//  hide-mouse-cursor-while-typing (Options.HideCursorWhileTyping has no effect), and the WPF
//  HitTestCore override (a transparent background achieves the same).

/// <summary>
/// Control that wraps a TextView and adds support for user input and the caret.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class TextArea : Control, ITextEditorComponent, IWeakEventListener
{
	#region Constructor
	/// <summary>
	/// Creates a new TextArea instance.
	/// </summary>
	public TextArea() : this(new TextView())
	{
	}

	/// <summary>
	/// Creates a new TextArea instance.
	/// </summary>
	protected TextArea(TextView textView)
	{
		if (textView == null)
			throw new ArgumentNullException(nameof(textView));
		this.textView = textView;
		this.Options = textView.Options;

		selection = emptySelection = new EmptySelection(this);

		textView.Services.AddService(typeof(TextArea), this);

		textView.LineTransformers.Add(new SelectionColorizer(this));
		//was previously: textView.InsertLayer(new SelectionLayer(this), KnownLayer.Selection,
		//LayerInsertionPosition.Replace); layers are background-renderer draw phases in this port.
		textView.BackgroundRenderers.Add(new SelectionLayer(this));

		caret = new Caret(this);
		caret.PositionChanged += (sender, e) => RequestSelectionValidation();
		caret.PositionChanged += CaretPositionChanged;

		leftMargins.CollectionChanged += LeftMargins_CollectionChanged;

		this.DefaultInputHandler = new TextAreaDefaultInputHandler(this);
		this.ActiveInputHandler = this.DefaultInputHandler;

		//was previously: static metadata overrides made the text area a focusable tab stop.
		IsTabStop = true;

		//was previously: the visual tree came from a theme ControlTemplate (a DockPanel with the
		//left margins and the text view); this port builds the equivalent tree in code.
		Template = new ControlTemplate(CreateTemplateRoot);

		PushFontPropertiesToTextView();
		RegisterFontPropertyCallbacks();
	}
	#endregion

	#region Visual tree
	Grid? layoutRoot;

	UIElement CreateTemplateRoot()
	{
		layoutRoot = new Grid();
		//was previously: HitTestCore override; a transparent background keeps empty areas clickable.
		layoutRoot.Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 255, 255, 255));
		RebuildLayout();
		return layoutRoot;
	}

	void RebuildLayout()
	{
		Grid? grid = layoutRoot;
		if (grid == null)
			return;
		grid.Children.Clear();
		grid.ColumnDefinitions.Clear();
		int column = 0;
		foreach (UIElement margin in leftMargins)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			if (margin is FrameworkElement marginElement)
				Grid.SetColumn(marginElement, column);
			column++;
			grid.Children.Add(margin);
		}
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		Grid.SetColumn(textView, column);
		grid.Children.Add(textView);
		foreach (UIElement overlay in overlays)
		{
			if (overlay is FrameworkElement overlayElement)
				Grid.SetColumn(overlayElement, column);
			grid.Children.Add(overlay);
		}
	}

	//was previously: overlay elements (e.g. the search panel) were hosted on the WPF AdornerLayer
	//above the text area; this framework has no adorner layer, so the text area hosts overlays as
	//extra children of its root grid, in the text view's column, above the text view.
	readonly List<UIElement> overlays = new List<UIElement>();

	/// <summary>
	/// Attaches an overlay element above the text view. The element is placed in the text view's
	/// grid cell (use alignment properties to position it, e.g. top-right for a search panel) and
	/// stays attached across layout rebuilds until <see cref="RemoveOverlay"/> is called.
	/// </summary>
	internal void AttachOverlay(UIElement overlay)
	{
		if (overlay == null)
			throw new ArgumentNullException(nameof(overlay));
		if (overlays.Contains(overlay))
			return;
		overlays.Add(overlay);
		Grid? grid = layoutRoot;
		if (grid != null)
		{
			if (overlay is FrameworkElement overlayElement)
				Grid.SetColumn(overlayElement, leftMargins.Count);
			grid.Children.Add(overlay);
		}
	}

	/// <summary>
	/// Removes an overlay element attached with <see cref="AttachOverlay"/>.
	/// Does nothing when the element is not attached.
	/// </summary>
	internal void RemoveOverlay(UIElement overlay)
	{
		if (overlay == null)
			throw new ArgumentNullException(nameof(overlay));
		if (overlays.Remove(overlay))
		{
			layoutRoot?.Children.Remove(overlay);
		}
	}
	#endregion

	#region Font property forwarding
	//was previously: the WPF TextView read the inherited font/foreground dependency properties;
	//the port's TextView is a Panel (inherits none), so this control pushes its values down and
	//keeps them in sync.

	void PushFontPropertiesToTextView()
	{
		textView.FontFamily = FontFamily?.Source;
		textView.FontSize = FontSize;
		textView.FontWeight = FontWeight;
		textView.FontStyle = FontStyle;
		textView.FontStretch = FontStretch;
		textView.Foreground = Foreground;
	}

	void RegisterFontPropertyCallbacks()
	{
		RegisterPropertyChangedCallback(FontFamilyProperty, (s, dp) => textView.FontFamily = FontFamily?.Source);
		RegisterPropertyChangedCallback(FontSizeProperty, (s, dp) => textView.FontSize = FontSize);
		RegisterPropertyChangedCallback(FontWeightProperty, (s, dp) => textView.FontWeight = FontWeight);
		RegisterPropertyChangedCallback(FontStyleProperty, (s, dp) => textView.FontStyle = FontStyle);
		RegisterPropertyChangedCallback(FontStretchProperty, (s, dp) => textView.FontStretch = FontStretch);
		RegisterPropertyChangedCallback(ForegroundProperty, (s, dp) => textView.Foreground = Foreground);
	}
	#endregion

	#region Thread verification
	/// <summary>
	/// Throws when the text area is accessed from a thread other than its UI thread.
	/// No-op when the control has no dispatcher (host-free unit tests).
	/// </summary>
	void VerifyAccess()
	{
		var dispatcherQueue = DispatcherQueue;
		if (dispatcherQueue != null && !dispatcherQueue.HasThreadAccess)
			throw new InvalidOperationException("TextArea can be accessed only from the thread that owns it.");
	}
	#endregion

	#region InputHandler management
	/// <summary>
	/// Gets the default input handler.
	/// </summary>
	/// <remarks><inheritdoc cref="ITextAreaInputHandler"/></remarks>
	public TextAreaDefaultInputHandler DefaultInputHandler { get; private set; }

	ITextAreaInputHandler? activeInputHandler;
	bool isChangingInputHandler;

	/// <summary>
	/// Gets/Sets the active input handler.
	/// This property does not return currently active stacked input handlers. Setting this property detaches all stacked input handlers.
	/// </summary>
	/// <remarks><inheritdoc cref="ITextAreaInputHandler"/></remarks>
	public ITextAreaInputHandler? ActiveInputHandler {
		get { return activeInputHandler; }
		set {
			if (value != null && value.TextArea != this)
				throw new ArgumentException("The input handler was created for a different text area than this one.");
			if (isChangingInputHandler)
				throw new InvalidOperationException("Cannot set ActiveInputHandler recursively");
			if (activeInputHandler != value)
			{
				isChangingInputHandler = true;
				try
				{
					// pop the whole stack (the bottom-most handler pops everything above it too)
					PopStackedInputHandler(stackedInputHandlers.LastOrDefault());
					Debug.Assert(stackedInputHandlers.IsEmpty);

					if (activeInputHandler != null)
						activeInputHandler.Detach();
					activeInputHandler = value;
					if (value != null)
						value.Attach();
				}
				finally
				{
					isChangingInputHandler = false;
				}
				ActiveInputHandlerChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Occurs when the ActiveInputHandler property changes.
	/// </summary>
	public event EventHandler? ActiveInputHandlerChanged;

	/// <summary>
	/// Gets the aggregated command bindings of the text area. Attached input handlers mirror
	/// their command bindings into this collection (see <see cref="TextAreaInputHandler"/>).
	/// </summary>
	//was previously: the WPF UIElement.CommandBindings collection, which WPF command routing
	//consulted directly; key dispatch runs through ActiveInputHandler.HandleKeyDown here.
	public ICollection<EditorCommandBinding> CommandBindings { get; } = new List<EditorCommandBinding>();

	/// <summary>
	/// Gets the aggregated input bindings of the text area. Attached input handlers mirror
	/// their key bindings into this collection (see <see cref="TextAreaInputHandler"/>).
	/// </summary>
	//was previously: the WPF UIElement.InputBindings collection; see CommandBindings.
	public ICollection<KeyBinding> InputBindings { get; } = new List<KeyBinding>();

	ImmutableStack<TextAreaStackedInputHandler> stackedInputHandlers = ImmutableStack<TextAreaStackedInputHandler>.Empty;

	/// <summary>
	/// Gets the list of currently active stacked input handlers.
	/// </summary>
	/// <remarks><inheritdoc cref="ITextAreaInputHandler"/></remarks>
	public ImmutableStack<TextAreaStackedInputHandler> StackedInputHandlers {
		get { return stackedInputHandlers; }
	}

	/// <summary>
	/// Pushes an input handler onto the list of stacked input handlers.
	/// </summary>
	/// <remarks><inheritdoc cref="ITextAreaInputHandler"/></remarks>
	public void PushStackedInputHandler(TextAreaStackedInputHandler inputHandler)
	{
		if (inputHandler == null)
			throw new ArgumentNullException(nameof(inputHandler));
		stackedInputHandlers = stackedInputHandlers.Push(inputHandler);
		inputHandler.Attach();
	}

	/// <summary>
	/// Pops the stacked input handler (and all input handlers above it).
	/// If <paramref name="inputHandler"/> is not found in the currently stacked input handlers, or is null, this method
	/// does nothing.
	/// </summary>
	/// <remarks><inheritdoc cref="ITextAreaInputHandler"/></remarks>
	public void PopStackedInputHandler(TextAreaStackedInputHandler? inputHandler)
	{
		if (inputHandler != null && stackedInputHandlers.Any(i => i == inputHandler))
		{
			ITextAreaInputHandler oldHandler;
			do
			{
				oldHandler = stackedInputHandlers.Peek();
				stackedInputHandlers = stackedInputHandlers.Pop();
				oldHandler.Detach();
			} while (oldHandler != inputHandler);
		}
	}
	#endregion

	#region Document property
	/// <summary>
	/// Document property.
	/// </summary>
	//was previously: TextView.DocumentProperty.AddOwner(typeof(TextArea), ...); this framework
	//has no AddOwner, so the text area registers its own property and forwards to the text view.
	public static readonly DependencyProperty DocumentProperty =
		DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(TextArea),
									new PropertyMetadata(null, OnDocumentChanged));

	/// <summary>
	/// Gets/Sets the document displayed by the text editor.
	/// The value is null while no document is attached.
	/// </summary>
	public TextDocument Document {
		get { return (TextDocument)GetValue(DocumentProperty); }
		set { SetValue(DocumentProperty, value); }
	}

	/// <inheritdoc/>
	public event EventHandler? DocumentChanged;

	static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((TextArea)dp).OnDocumentChanged((TextDocument?)e.OldValue, (TextDocument?)e.NewValue);
	}

	void OnDocumentChanged(TextDocument? oldValue, TextDocument? newValue)
	{
		if (oldValue != null)
		{
			TextDocumentWeakEventManager.Changing.RemoveListener(oldValue, this);
			TextDocumentWeakEventManager.Changed.RemoveListener(oldValue, this);
			TextDocumentWeakEventManager.UpdateStarted.RemoveListener(oldValue, this);
			TextDocumentWeakEventManager.UpdateFinished.RemoveListener(oldValue, this);
		}
		textView.SetValue(TextView.DocumentProperty, newValue);
		if (newValue != null)
		{
			TextDocumentWeakEventManager.Changing.AddListener(newValue, this);
			TextDocumentWeakEventManager.Changed.AddListener(newValue, this);
			TextDocumentWeakEventManager.UpdateStarted.AddListener(newValue, this);
			TextDocumentWeakEventManager.UpdateFinished.AddListener(newValue, this);
		}
		// Reset caret location and selection: this is necessary because the caret/selection might be invalid
		// in the new document (e.g. if new document is shorter than the old document).
		caret.Location = new TextLocation(1, 1);
		this.ClearSelection();
		DocumentChanged?.Invoke(this, EventArgs.Empty);
		//was previously: CommandManager.InvalidateRequerySuggested(); the port queries
		//can-execute on each key press, so there is no requery cache to invalidate.
	}
	#endregion

	#region Options property
	/// <summary>
	/// Options property.
	/// </summary>
	//was previously: TextView.OptionsProperty.AddOwner(typeof(TextArea), ...); see DocumentProperty.
	public static readonly DependencyProperty OptionsProperty =
		DependencyProperty.Register(nameof(Options), typeof(AdvancedTextEditOptions), typeof(TextArea),
									new PropertyMetadata(null, OnOptionsChanged));

	/// <summary>
	/// Gets/Sets the options used by the text editor.
	/// </summary>
	public AdvancedTextEditOptions Options {
		get { return (AdvancedTextEditOptions)GetValue(OptionsProperty); }
		set { SetValue(OptionsProperty, value); }
	}

	/// <summary>
	/// Occurs when a text editor option has changed.
	/// </summary>
	public event PropertyChangedEventHandler? OptionChanged;

	/// <summary>
	/// Raises the <see cref="OptionChanged"/> event.
	/// </summary>
	protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
	{
		OptionChanged?.Invoke(this, e);
	}

	static void OnOptionsChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((TextArea)dp).OnOptionsChanged((AdvancedTextEditOptions?)e.OldValue, (AdvancedTextEditOptions?)e.NewValue);
	}

	void OnOptionsChanged(AdvancedTextEditOptions? oldValue, AdvancedTextEditOptions? newValue)
	{
		if (oldValue != null)
		{
			PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
		}
		textView.SetValue(TextView.OptionsProperty, newValue);
		if (newValue != null)
		{
			PropertyChangedWeakEventManager.AddListener(newValue, this);
		}
		OnOptionChanged(new PropertyChangedEventArgs(null));
	}
	#endregion

	#region ReceiveWeakEvent
	/// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
	protected virtual bool ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		if (managerType == typeof(TextDocumentWeakEventManager.Changing))
		{
			OnDocumentChanging();
			return true;
		}
		else if (managerType == typeof(TextDocumentWeakEventManager.Changed))
		{
			OnDocumentChanged((DocumentChangeEventArgs)e);
			return true;
		}
		else if (managerType == typeof(TextDocumentWeakEventManager.UpdateStarted))
		{
			OnUpdateStarted();
			return true;
		}
		else if (managerType == typeof(TextDocumentWeakEventManager.UpdateFinished))
		{
			OnUpdateFinished();
			return true;
		}
		else if (managerType == typeof(PropertyChangedWeakEventManager))
		{
			OnOptionChanged((PropertyChangedEventArgs)e);
			return true;
		}
		return false;
	}

	bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		return ReceiveWeakEvent(managerType, sender, e);
	}
	#endregion

	#region Caret handling on document changes
	void OnDocumentChanging()
	{
		caret.OnDocumentChanging();
	}

	void OnDocumentChanged(DocumentChangeEventArgs e)
	{
		caret.OnDocumentChanged(e);
		this.Selection = selection.UpdateOnDocumentChange(e);
	}

	void OnUpdateStarted()
	{
		Document.UndoStack.PushOptional(new RestoreCaretAndSelectionUndoAction(this));
	}

	void OnUpdateFinished()
	{
		caret.OnDocumentUpdateFinished();
	}

	sealed class RestoreCaretAndSelectionUndoAction : IUndoableOperation
	{
		// keep textarea in weak reference because the IUndoableOperation is stored with the document
		readonly WeakReference<TextArea> textAreaReference;
		readonly TextViewPosition caretPosition;
		readonly Selection selection;

		public RestoreCaretAndSelectionUndoAction(TextArea textArea)
		{
			this.textAreaReference = new WeakReference<TextArea>(textArea);
			// Just save the old caret position, no need to validate here.
			// If we restore it, we'll validate it anyways.
			this.caretPosition = textArea.Caret.NonValidatedPosition;
			this.selection = textArea.Selection;
		}

		public void Undo()
		{
			if (textAreaReference.TryGetTarget(out TextArea? textArea))
			{
				textArea.Caret.Position = caretPosition;
				textArea.Selection = selection;
			}
		}

		public void Redo()
		{
			// redo=undo: we just restore the caret/selection state
			Undo();
		}
	}
	#endregion

	#region TextView property
	readonly TextView textView;

	/// <summary>
	/// Gets the text view used to display text in this text area. The text view owns the scroll
	/// surface (offsets, extent, viewport, MakeVisible).
	/// </summary>
	//was previously: the text area also forwarded IScrollInfo to the text view; consumers now
	//scroll through the TextView members directly.
	public TextView TextView {
		get {
			return textView;
		}
	}
	#endregion

	#region Selection property
	internal readonly Selection emptySelection;
	Selection selection;

	/// <summary>
	/// Occurs when the selection has changed.
	/// </summary>
	public event EventHandler? SelectionChanged;

	/// <summary>
	/// Gets/Sets the selection in this text area.
	/// </summary>
	public Selection Selection {
		get { return selection; }
		set {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (value.textArea != this)
				throw new ArgumentException("Cannot use a Selection instance that belongs to another text area.");
			if (!object.Equals(selection, value))
			{
				if (textView != null)
				{
					ISegment? oldSegment = selection.SurroundingSegment;
					ISegment? newSegment = value.SurroundingSegment;
					if (!Selection.EnableVirtualSpace && (selection is SimpleSelection && value is SimpleSelection && oldSegment != null && newSegment != null))
					{
						// perf optimization:
						// When a simple selection changes, don't redraw the whole selection, but only the changed parts.
						int oldSegmentOffset = oldSegment.Offset;
						int newSegmentOffset = newSegment.Offset;
						if (oldSegmentOffset != newSegmentOffset)
						{
							textView.Redraw(Math.Min(oldSegmentOffset, newSegmentOffset),
											Math.Abs(oldSegmentOffset - newSegmentOffset));
						}
						int oldSegmentEndOffset = oldSegment.EndOffset;
						int newSegmentEndOffset = newSegment.EndOffset;
						if (oldSegmentEndOffset != newSegmentEndOffset)
						{
							textView.Redraw(Math.Min(oldSegmentEndOffset, newSegmentEndOffset),
											Math.Abs(oldSegmentEndOffset - newSegmentEndOffset));
						}
					}
					else
					{
						textView.Redraw(oldSegment);
						textView.Redraw(newSegment);
					}
				}
				selection = value;
				SelectionChanged?.Invoke(this, EventArgs.Empty);
				//was previously: CommandManager.InvalidateRequerySuggested(); see DocumentProperty.
			}
		}
	}

	/// <summary>
	/// Clears the current selection.
	/// </summary>
	public void ClearSelection()
	{
		this.Selection = emptySelection;
	}

	static void OnSelectionAppearanceChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((TextArea)dp).textView.Redraw();
	}

	/// <summary>
	/// The <see cref="SelectionBrush"/> property.
	/// </summary>
	public static readonly DependencyProperty SelectionBrushProperty =
		DependencyProperty.Register(nameof(SelectionBrush), typeof(Brush), typeof(TextArea),
									new PropertyMetadata(null, OnSelectionAppearanceChanged));

	/// <summary>
	/// Gets/Sets the background brush used for the selection.
	/// </summary>
	public Brush? SelectionBrush {
		get { return (Brush?)GetValue(SelectionBrushProperty); }
		set { SetValue(SelectionBrushProperty, value); }
	}

	/// <summary>
	/// The <see cref="SelectionForeground"/> property.
	/// </summary>
	public static readonly DependencyProperty SelectionForegroundProperty =
		DependencyProperty.Register(nameof(SelectionForeground), typeof(Brush), typeof(TextArea),
									new PropertyMetadata(null, OnSelectionAppearanceChanged));

	/// <summary>
	/// Gets/Sets the foreground brush used for selected text.
	/// </summary>
	public Brush? SelectionForeground {
		get { return (Brush?)GetValue(SelectionForegroundProperty); }
		set { SetValue(SelectionForegroundProperty, value); }
	}

	/// <summary>
	/// The <see cref="SelectionBorderBrush"/> property.
	/// </summary>
	//was previously: the SelectionBorder dependency property held a WPF Pen; re-expressed as a
	//brush + thickness pair per the port's drawing rules.
	public static readonly DependencyProperty SelectionBorderBrushProperty =
		DependencyProperty.Register(nameof(SelectionBorderBrush), typeof(Brush), typeof(TextArea),
									new PropertyMetadata(null, OnSelectionAppearanceChanged));

	/// <summary>
	/// Gets/Sets the brush used for the border of the selection.
	/// </summary>
	public Brush? SelectionBorderBrush {
		get { return (Brush?)GetValue(SelectionBorderBrushProperty); }
		set { SetValue(SelectionBorderBrushProperty, value); }
	}

	/// <summary>
	/// The <see cref="SelectionBorderThickness"/> property.
	/// </summary>
	public static readonly DependencyProperty SelectionBorderThicknessProperty =
		DependencyProperty.Register(nameof(SelectionBorderThickness), typeof(double), typeof(TextArea),
									new PropertyMetadata(1.0, OnSelectionAppearanceChanged));

	/// <summary>
	/// Gets/Sets the thickness of the border of the selection. The border is drawn only while
	/// <see cref="SelectionBorderBrush"/> is non-null.
	/// </summary>
	public double SelectionBorderThickness {
		get { return (double)GetValue(SelectionBorderThicknessProperty); }
		set { SetValue(SelectionBorderThicknessProperty, value); }
	}

	/// <summary>
	/// The <see cref="SelectionCornerRadius"/> property.
	/// </summary>
	public static readonly DependencyProperty SelectionCornerRadiusProperty =
		DependencyProperty.Register(nameof(SelectionCornerRadius), typeof(double), typeof(TextArea),
									new PropertyMetadata(3.0, OnSelectionAppearanceChanged));

	/// <summary>
	/// Gets/Sets the corner radius of the selection.
	/// </summary>
	public double SelectionCornerRadius {
		get { return (double)GetValue(SelectionCornerRadiusProperty); }
		set { SetValue(SelectionCornerRadiusProperty, value); }
	}

	/// <summary>
	/// Gets/Sets the active mouse selection mode.
	///
	/// Setting this property to MouseSelectionMode.None will cancel mouse selection
	/// and release pointer capture.
	///
	/// Currently, the setter only supports the values <c>None</c>, <c>Normal</c>
	/// and <c>Rectangular</c>; because pointer capture requires an active pointer in this
	/// framework, activating a mode takes effect only while a pointer is captured (e.g. from
	/// within a pointer event handler).
	/// </summary>
	public MouseSelectionMode MouseSelectionMode {
		get {
			if (DefaultInputHandler.MouseSelection is SelectionMouseHandler mouseHandler)
			{
				return mouseHandler.MouseSelectionMode;
			}
			else
			{
				return MouseSelectionMode.None;
			}
		}
		set {
			if (DefaultInputHandler.MouseSelection is SelectionMouseHandler mouseHandler)
			{
				mouseHandler.MouseSelectionMode = value;
			}
		}
	}
	#endregion

	#region Force caret to stay inside selection
	bool ensureSelectionValidRequested;
	int allowCaretOutsideSelection;

	void RequestSelectionValidation()
	{
		if (!ensureSelectionValidRequested && allowCaretOutsideSelection == 0)
		{
			//was previously: Dispatcher.BeginInvoke(DispatcherPriority.Normal, ...). Without a
			//dispatcher (host-free unit tests) the deferred validation simply does not run.
			var dispatcherQueue = DispatcherQueue;
			if (dispatcherQueue != null)
			{
				ensureSelectionValidRequested = true;
				dispatcherQueue.TryEnqueue(EnsureSelectionValid);
			}
		}
	}

	/// <summary>
	/// Code that updates only the caret but not the selection can cause confusion when
	/// keys like 'Delete' delete the (possibly invisible) selected text and not the
	/// text around the caret.
	///
	/// So we'll ensure that the caret is inside the selection.
	/// (when the caret is not in the selection, we'll clear the selection)
	///
	/// This method is invoked using the dispatcher so that code may temporarily violate this rule
	/// (e.g. most 'extend selection' methods work by first setting the caret, then the selection),
	/// it's sufficient to fix it after any event handlers have run.
	/// </summary>
	void EnsureSelectionValid()
	{
		ensureSelectionValidRequested = false;
		if (allowCaretOutsideSelection == 0)
		{
			if (!selection.IsEmpty && !selection.Contains(caret.Offset))
			{
				Debug.WriteLine("Resetting selection because caret is outside");
				this.ClearSelection();
			}
		}
	}

	/// <summary>
	/// Temporarily allows positioning the caret outside the selection.
	/// Dispose the returned IDisposable to revert the allowance.
	/// </summary>
	/// <remarks>
	/// The text area only forces the caret to be inside the selection when other events
	/// have finished running (using the dispatcher), so you don't have to use this method
	/// for temporarily positioning the caret in event handlers.
	/// </remarks>
	public IDisposable AllowCaretOutsideSelection()
	{
		VerifyAccess();
		allowCaretOutsideSelection++;
		return new CallbackOnDispose(
			delegate {
				VerifyAccess();
				allowCaretOutsideSelection--;
				RequestSelectionValidation();
			});
	}
	#endregion

	#region Properties
	readonly Caret caret;

	/// <summary>
	/// Gets the Caret used for this text area.
	/// </summary>
	public Caret Caret {
		get { return caret; }
	}

	void CaretPositionChanged(object? sender, EventArgs e)
	{
		if (textView == null)
			return;

		this.textView.HighlightedLine = this.Caret.Line;
	}

	readonly ObservableCollection<UIElement> leftMargins = new ObservableCollection<UIElement>();

	/// <summary>
	/// Gets the collection of margins displayed to the left of the text view.
	/// </summary>
	public ObservableCollection<UIElement> LeftMargins {
		get {
			return leftMargins;
		}
	}

	void LeftMargins_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems != null)
		{
			foreach (ITextViewConnect c in e.OldItems.OfType<ITextViewConnect>())
			{
				c.RemoveFromTextView(textView);
			}
		}
		if (e.NewItems != null)
		{
			foreach (ITextViewConnect c in e.NewItems.OfType<ITextViewConnect>())
			{
				c.AddToTextView(textView);
			}
		}
		RebuildLayout();
	}

	IReadOnlySectionProvider readOnlySectionProvider = NoReadOnlySections.Instance;

	/// <summary>
	/// Gets/Sets an object that provides read-only sections for the text area.
	/// </summary>
	public IReadOnlySectionProvider ReadOnlySectionProvider {
		get { return readOnlySectionProvider; }
		set {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			readOnlySectionProvider = value;
			//was previously: CommandManager.InvalidateRequerySuggested(); see DocumentProperty.
		}
	}
	#endregion

	#region Focus Handling (Show/Hide Caret)
	/// <summary>
	/// Gets whether the text area currently holds the keyboard focus.
	/// </summary>
	//was previously: the WPF IsKeyboardFocused dependency property; derived from FocusState here.
	//The hidden internal UIElement.IsKeyboardFocused is true only for FocusState.Keyboard, but
	//the text area counts pointer/programmatic focus as ready for keyboard input as well.
	public new bool IsKeyboardFocused {
		get { return FocusState != FocusState.Unfocused; }
	}

	/// <inheritdoc/>
	protected override void OnPointerPressed(PointerRoutedEventArgs e)
	{
		base.OnPointerPressed(e);
		Focus(FocusState.Pointer);
	}

	/// <inheritdoc/>
	protected override void OnGotFocus(RoutedEventArgs e)
	{
		base.OnGotFocus(e);
		caret.Show();
		//An editable text area summons the software keyboard on heads that have
		//one. A fully read-only editor (IsReadOnly sets the ReadOnlySectionDocument
		//provider) is not text ENTRY and never summons it; a partially read-only
		//document still does.
		if (ReadOnlySectionProvider is not ReadOnlySectionDocument && IsEnabled)
		{
			SoftwareKeyboardFocus.NotifyFocused(this);
		}
	}

	/// <inheritdoc/>
	protected override void OnLostFocus(RoutedEventArgs e)
	{
		base.OnLostFocus(e);
		caret.Hide();
		SoftwareKeyboardFocus.NotifyUnfocused(this);
	}
	#endregion

	#region OnTextInput / RemoveSelectedText / ReplaceSelectionWithText
	/// <summary>
	/// Occurs when the TextArea receives text input, immediately before the text area handles
	/// the input. Setting <see cref="TextInputEventArgs.Handled"/> vetoes the input.
	/// </summary>
	public event EventHandler<TextInputEventArgs>? TextEntering;

	/// <summary>
	/// Occurs when the TextArea receives text input, immediately after the text area handled
	/// the input.
	/// </summary>
	public event EventHandler<TextInputEventArgs>? TextEntered;

	/// <summary>
	/// Raises the TextEntering event.
	/// </summary>
	protected virtual void OnTextEntering(TextInputEventArgs e)
	{
		TextEntering?.Invoke(this, e);
	}

	/// <summary>
	/// Raises the TextEntered event.
	/// </summary>
	protected virtual void OnTextEntered(TextInputEventArgs e)
	{
		TextEntered?.Invoke(this, e);
	}

	/// <summary>
	/// Performs text input.
	/// This raises the <see cref="TextEntering"/> event, replaces the selection with the text,
	/// and then raises the <see cref="TextEntered"/> event.
	/// </summary>
	public void PerformTextInput(string text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		PerformTextInput(new TextInputEventArgs(text));
	}

	/// <summary>
	/// Performs text input.
	/// This raises the <see cref="TextEntering"/> event, replaces the selection with the text,
	/// and then raises the <see cref="TextEntered"/> event.
	/// </summary>
	public void PerformTextInput(TextInputEventArgs e)
	{
		if (e == null)
			throw new ArgumentNullException(nameof(e));
		if (this.Document == null)
			throw ThrowUtil.NoDocumentAssigned();
		OnTextEntering(e);
		if (!e.Handled)
		{
			if (e.Text == "\n" || e.Text == "\r" || e.Text == "\r\n")
			{
				ReplaceSelectionWithNewLine();
			}
			else
			{
				if (OverstrikeMode && Selection.IsEmpty && Document.GetLineByNumber(Caret.Line).EndOffset > Caret.Offset)
				{
					//was previously: EditingCommands.SelectRightByCharacter.Execute(null, this);
					//dispatched through the active input handler's command bindings here.
					(ActiveInputHandler as TextAreaInputHandler)?.ExecuteCommand(EditorCommands.SelectRightByCharacter, null);
				}
				ReplaceSelectionWithText(e.Text);
			}
			OnTextEntered(e);
			caret.BringCaretToView();
		}
	}

	void ReplaceSelectionWithNewLine()
	{
		string newLine = TextUtilities.GetNewLineFromDocument(this.Document, this.Caret.Line);
		using (this.Document.RunUpdate())
		{
			ReplaceSelectionWithText(newLine);
			if (this.IndentationStrategy != null)
			{
				DocumentLine line = this.Document.GetLineByNumber(this.Caret.Line);
				ISegment[] deletable = GetDeletableSegments(line);
				if (deletable.Length == 1 && deletable[0].Offset == line.Offset && deletable[0].Length == line.Length)
				{
					// use indentation strategy only if the line is not read-only
					this.IndentationStrategy.IndentLine(this.Document, line);
				}
			}
		}
	}

	internal void RemoveSelectedText()
	{
		if (this.Document == null)
			throw ThrowUtil.NoDocumentAssigned();
		selection.ReplaceSelectionWithText(string.Empty);
#if DEBUG
		if (!selection.IsEmpty)
		{
			foreach (ISegment s in selection.Segments)
			{
				Debug.Assert(!this.ReadOnlySectionProvider.GetDeletableSegments(s).Any());
			}
		}
#endif
	}

	internal void ReplaceSelectionWithText(string newText)
	{
		if (newText == null)
			throw new ArgumentNullException(nameof(newText));
		if (this.Document == null)
			throw ThrowUtil.NoDocumentAssigned();
		selection.ReplaceSelectionWithText(newText);
	}

	internal ISegment[] GetDeletableSegments(ISegment segment)
	{
		var deletableSegments = this.ReadOnlySectionProvider.GetDeletableSegments(segment);
		if (deletableSegments == null)
			throw new InvalidOperationException("ReadOnlySectionProvider.GetDeletableSegments returned null");
		var array = deletableSegments.ToArray();
		int lastIndex = segment.Offset;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Offset < lastIndex)
				throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
			lastIndex = array[i].EndOffset;
		}
		if (lastIndex > segment.EndOffset)
			throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
		return array;
	}
	#endregion

	#region IndentationStrategy property
	/// <summary>
	/// IndentationStrategy property.
	/// </summary>
	public static readonly DependencyProperty IndentationStrategyProperty =
		DependencyProperty.Register(nameof(IndentationStrategy), typeof(IIndentationStrategy), typeof(TextArea),
									new PropertyMetadata(new DefaultIndentationStrategy()));

	/// <summary>
	/// Gets/Sets the indentation strategy used when inserting new lines.
	/// </summary>
	public IIndentationStrategy? IndentationStrategy {
		get { return (IIndentationStrategy?)GetValue(IndentationStrategyProperty); }
		set { SetValue(IndentationStrategyProperty, value); }
	}
	#endregion

	#region OnKeyDown/OnKeyUp
	/// <summary>
	/// Processes a key press: the stacked input handlers get the first chance (in reverse push
	/// order), then the active input handler's command dispatch, and finally the key's typed
	/// character (if any) is inserted as text input.
	/// </summary>
	//was previously: WPF routed the key press through OnPreviewKeyDown (stacked handlers),
	//command routing (the attached input/command bindings) and OnTextInput; this override
	//performs those three phases explicitly, see the KEY DISPATCH SEAM in TextAreaInputHandler.
	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.Handled)
			return;

		VirtualKeyModifiers modifiers = e.KeyboardModifiers;

		// (1) stacked input handlers, in reverse order of being pushed
		foreach (TextAreaStackedInputHandler h in stackedInputHandlers)
		{
			if (h.OnPreviewKeyDown(e.Key, modifiers))
			{
				e.Handled = true;
				return;
			}
		}

		// (2) the active input handler's key bindings / command default gestures
		if (activeInputHandler is TextAreaInputHandler inputHandler && inputHandler.HandleKeyDown(e.Key, modifiers))
		{
			e.Handled = true;
			return;
		}

		// (3) typed character input (mirrors the framework TextBox's UnicodeKey handling)
		if (this.Document == null)
			return;
		if (e.UnicodeKey is not { } typedCharacter)
			return;
		bool isEnter = typedCharacter is '\r' or '\n' || e.Key == VirtualKey.Enter;
		if (isEnter || e.Key == VirtualKey.Tab)
		{
			// Enter and Tab belong to the command system (EnterParagraphBreak/TabForward);
			// when their commands decline (e.g. no focus), do not insert the character here.
			return;
		}
		if (char.IsControl(typedCharacter))
		{
			//was previously: OnTextInput ignored ESC ("\x1b"), backspace ("\b") and empty text;
			//filtering all control characters covers those cases in this framework.
			return;
		}
		PerformTextInput(typedCharacter.ToString());
		e.Handled = true;
	}

	/// <summary>
	/// Processes a key release: the stacked input handlers get the event in reverse push order.
	/// </summary>
	protected override void OnKeyUp(KeyRoutedEventArgs e)
	{
		base.OnKeyUp(e);
		if (e.Handled)
			return;
		VirtualKeyModifiers modifiers = e.KeyboardModifiers;
		foreach (TextAreaStackedInputHandler h in stackedInputHandlers)
		{
			if (h.OnPreviewKeyUp(e.Key, modifiers))
			{
				e.Handled = true;
				return;
			}
		}
	}
	#endregion

	#region Pointer wheel scrolling
	/// <summary>
	/// The number of lines scrolled per wheel notch.
	/// </summary>
	//was previously: SystemParameters.WheelScrollLines; constant per the port's rules.
	const double WheelScrollLines = 3;

	/// <inheritdoc/>
	protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
	{
		base.OnPointerWheelChanged(e);
		if (e.Handled)
			return;
		int wheelDelta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
		if (wheelDelta == 0)
			return;
		double notches = wheelDelta / 120.0;
		double distance = notches * WheelScrollLines * textView.DefaultLineHeight;
		if ((e.KeyModifiers & VirtualKeyModifiers.Shift) != 0)
		{
			textView.SetHorizontalOffset(textView.HorizontalOffset - distance);
		}
		else
		{
			textView.SetVerticalOffset(textView.VerticalOffset - distance);
		}
		e.Handled = true;
	}
	#endregion

	#region Overstrike mode
	/// <summary>
	/// The <see cref="OverstrikeMode"/> dependency property.
	/// </summary>
	public static readonly DependencyProperty OverstrikeModeProperty =
		DependencyProperty.Register(nameof(OverstrikeMode), typeof(bool), typeof(TextArea),
									new PropertyMetadata(Boxes.False, OnOverstrikeModeChanged));

	/// <summary>
	/// Gets/Sets whether overstrike mode is active.
	/// </summary>
	public bool OverstrikeMode {
		get { return (bool)GetValue(OverstrikeModeProperty); }
		set { SetValue(OverstrikeModeProperty, Boxes.Box(value)); }
	}

	static void OnOverstrikeModeChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		//was previously: handled in the WPF OnPropertyChanged override.
		((TextArea)dp).caret.UpdateIfVisible();
	}
	#endregion

	#region Clipboard veto events
	//was previously: the WPF DataObject.Copying/SettingData/Pasting attached events; plain .NET
	//events here, raised by the editing command handler through the On* methods.

	/// <summary>
	/// Occurs when a copy or cut command is about to place a data package on the clipboard.
	/// Handlers can modify the package or cancel the command.
	/// </summary>
	public event EventHandler<DataObjectCopyingEventArgs>? DataObjectCopying;

	/// <summary>
	/// Occurs once per data format before the editor adds that format to a copy data package.
	/// Handlers can veto individual formats.
	/// </summary>
	public event EventHandler<DataObjectSettingDataEventArgs>? DataObjectSettingData;

	/// <summary>
	/// Occurs when clipboard content is about to be pasted into the text area.
	/// Handlers can redirect the format to apply or cancel the paste.
	/// </summary>
	public event EventHandler<DataObjectPastingEventArgs>? DataObjectPasting;

	/// <summary>
	/// Raises the <see cref="DataObjectCopying"/> event.
	/// </summary>
	internal void OnDataObjectCopying(DataObjectCopyingEventArgs e)
	{
		DataObjectCopying?.Invoke(this, e);
	}

	/// <summary>
	/// Raises the <see cref="DataObjectSettingData"/> event.
	/// </summary>
	internal void OnDataObjectSettingData(DataObjectSettingDataEventArgs e)
	{
		DataObjectSettingData?.Invoke(this, e);
	}

	/// <summary>
	/// Raises the <see cref="DataObjectPasting"/> event.
	/// </summary>
	internal void OnDataObjectPasting(DataObjectPastingEventArgs e)
	{
		DataObjectPasting?.Invoke(this, e);
	}
	#endregion

	/// <summary>
	/// Gets the requested service.
	/// </summary>
	/// <returns>Returns the requested service instance, or null if the service cannot be found.</returns>
	public virtual object? GetService(Type serviceType)
	{
		return textView.GetService(serviceType);
	}

	/// <summary>
	/// Occurs when text inside the TextArea was copied.
	/// </summary>
	public event EventHandler<TextEventArgs>? TextCopied;

	internal void OnTextCopied(TextEventArgs e)
	{
		TextCopied?.Invoke(this, e);
	}
}

/// <summary>
/// EventArgs with text.
/// </summary>
public class TextEventArgs : EventArgs
{
	readonly string text;

	/// <summary>
	/// Gets the text.
	/// </summary>
	public string Text {
		get {
			return text;
		}
	}

	/// <summary>
	/// Creates a new TextEventArgs instance.
	/// </summary>
	public TextEventArgs(string text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		this.text = text;
	}
}

/// <summary>
/// Event data for the <see cref="TextArea.TextEntering"/> and <see cref="TextArea.TextEntered"/>
/// events.
/// </summary>
//was previously: WPF TextCompositionEventArgs; only the members the editor consumes are provided.
public class TextInputEventArgs : EventArgs
{
	/// <summary>
	/// Creates a new TextInputEventArgs instance.
	/// </summary>
	public TextInputEventArgs(string text)
	{
		if (text == null)
			throw new ArgumentNullException(nameof(text));
		this.Text = text;
	}

	/// <summary>
	/// Gets the text being entered.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// Gets/Sets whether the input has been handled. Setting this from a
	/// <see cref="TextArea.TextEntering"/> handler vetoes the input.
	/// </summary>
	public bool Handled { get; set; }
}
