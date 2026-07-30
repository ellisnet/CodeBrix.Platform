#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using CodeBrix.Platform.UI.AdvancedTextEdit.Highlighting;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;
using CodeBrix.Platform.UI.AdvancedTextEdit.Utils;

namespace CodeBrix.Platform.UI.AdvancedTextEdit;

//was previously: ICSharpCode.AvalonEdit/TextEditor.cs in the AvalonEdit repo (MIT), where the
//control was named TextEditor; renamed per the port naming rules. The editor logic is
//transliterated. Structural re-expressions:
//- The theme ControlTemplate (a Border holding a ScrollViewer whose content was the text area)
//  is replaced by a visual tree built in code: a 2x2 Grid with the TextArea, an explicit
//  vertical and horizontal ScrollBar and a corner spacer. There is no ScrollViewer in this port
//  (the render design owns scrolling): the scroll bars are two-way-synced to the TextView's
//  scroll surface (SetVertical/SetHorizontalOffset vs. ScrollOffsetChanged/VisualLinesChanged),
//  and the Line*/Page*/ScrollTo* methods run the same math on the TextView offsets directly.
//- Every dependency property is registered fresh (this framework has no AddOwner), and
//  SetCurrentValue calls became plain SetValue.
//- Copy/Cut/Paste/Delete/SelectAll/Undo/Redo dispatch through the text area's active input
//  handler (TextAreaInputHandler.ExecuteCommand/CanExecuteCommand) instead of WPF command routing.
//- The undo-stack IsOriginalFile listener shares this port's PropertyChangedWeakEventManager with
//  the options listener (upstream used WPF's PropertyChangedEventManager with a property-name
//  filter); ReceiveWeakEvent dispatches on the sender and filters the property name itself.
//- The hover events are plain .NET events forwarded from the TextView (no RoutedEvent AddOwner),
//  and focus forwarding overrides OnGotFocus / hides UIElement.Focus instead of OnGotKeyboardFocus.
//- The TextArea appearance defaults the theme styles carried (selection brushes, monospace font)
//  are applied in the constructor here; only the background fill of the template Border survives
//  (no BorderBrush/BorderThickness rendering on the root grid).
//- Dropped, with no counterpart in this framework: the UI-automation peer, the Localizability/
//  ContentProperty attributes, and the internal ScrollViewer property.

/// <summary>
/// The text editor control.
/// Contains a scrollable <see cref="Editing.TextArea"/>.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class AdvancedTextEdit : Control, ITextEditorComponent, IWeakEventListener
{
	#region Constructors
	/// <summary>
	/// Creates a new AdvancedTextEdit instance.
	/// </summary>
	public AdvancedTextEdit() : this(new TextArea())
	{
	}

	/// <summary>
	/// Creates a new AdvancedTextEdit instance.
	/// </summary>
	protected AdvancedTextEdit(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		this.textArea = textArea;

		textArea.TextView.Services.AddService(typeof(AdvancedTextEdit), this);

		//was previously: static metadata overrides made the editor a focusable tab stop.
		IsTabStop = true;

		//was previously: the theme styles (TextEditor.xaml) supplied these defaults - the editor
		//font, the system-highlight selection brush (opacity 0.7 over the highlight color), the
		//highlight-brush selection border pen (thickness 1) and the highlight-text selection
		//foreground; re-expressed as constants because this port has no system-color resources.
		FontFamily = new FontFamily("monospace");
		FontSize = 13;
		//was previously: Foreground/Background came from SystemColors.WindowTextBrush/WindowBrush
		//DynamicResources in the theme style. Fixed white-surface/black-text defaults keep the
		//editor readable regardless of the app theme (the built-in highlighting definitions
		//assume a light surface, as they did upstream); consumers override both in XAML.
		Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
		Foreground = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
		textArea.SelectionBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0x66, 0x33, 0x99, 0xFF));
		textArea.SelectionForeground = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
		textArea.SelectionBorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0x33, 0x99, 0xFF));
		textArea.SelectionBorderThickness = 1.0;
		// textArea.Caret.CaretBrush stays null: the caret falls back to the text foreground.

		PushFontPropertiesToTextArea();
		RegisterFontPropertyCallbacks();
		RegisterPropertyChangedCallback(BackgroundProperty, (s, dp) => PushBackgroundToLayoutRoot());

		verticalScrollBar = new ScrollBar
		{
			Orientation = Orientation.Vertical,
			IndicatorMode = ScrollingIndicatorMode.MouseIndicator,
			IsTabStop = false,
		};
		verticalScrollBar.ValueChanged += VerticalScrollBar_ValueChanged;
		horizontalScrollBar = new ScrollBar
		{
			Orientation = Orientation.Horizontal,
			IndicatorMode = ScrollingIndicatorMode.MouseIndicator,
			IsTabStop = false,
		};
		horizontalScrollBar.ValueChanged += HorizontalScrollBar_ValueChanged;
		cornerSpacer = new Border();

		// The theme's ScrollBar template renders through indicator visual states that a hosting
		// ScrollViewer normally drives; standing alone on these heads the bar occupied space but
		// painted nothing. Both bars therefore get a minimal code-built template that provides
		// exactly the named parts the control's own track layout drives (Vertical/HorizontalRoot,
		// LargeDecrease/LargeIncrease repeat buttons whose size the control sets, and the Thumb) -
		// theme-independent, like the rest of this control's code-built tree.
		verticalScrollBar.Template = new ControlTemplate(() => BuildScrollBarTemplateRoot(Orientation.Vertical));
		horizontalScrollBar.Template = new ControlTemplate(() => BuildScrollBarTemplateRoot(Orientation.Horizontal));
		verticalScrollBar.Width = ScrollBarThickness;
		horizontalScrollBar.Height = ScrollBarThickness;

		TextView textView = textArea.TextView;
		textView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
		textView.VisualLinesChanged += TextView_VisualLinesChanged;

		//was previously: the four hover RoutedEvents were re-raised through AddOwner; the port
		//forwards the TextView's plain events with the editor as sender.
		textView.PreviewMouseHover += (sender, e) => PreviewMouseHover?.Invoke(this, e);
		textView.MouseHover += (sender, e) => MouseHover?.Invoke(this, e);
		textView.PreviewMouseHoverStopped += (sender, e) => PreviewMouseHoverStopped?.Invoke(this, e);
		textView.MouseHoverStopped += (sender, e) => MouseHoverStopped?.Invoke(this, e);

		//was previously: the visual tree came from a theme ControlTemplate (Border + ScrollViewer);
		//this port builds the equivalent tree in code, see CreateTemplateRoot.
		Template = new ControlTemplate(CreateTemplateRoot);

		SetValue(OptionsProperty, textArea.Options);
		SetValue(DocumentProperty, new TextDocument());
	}
	#endregion

	#region Visual tree / scroll bar synchronization
	readonly ScrollBar verticalScrollBar;
	readonly ScrollBar horizontalScrollBar;
	readonly Border cornerSpacer;
	Grid? layoutRoot;
	bool updatingScrollBars;

	/// <summary>
	/// The extent must exceed the viewport by more than this amount before an Auto scroll bar
	/// appears.
	/// </summary>
	const double ScrollBarAutoEpsilon = 0.01;

	/// <summary>The width of the vertical / height of the horizontal scroll bar, in DIPs.</summary>
	const double ScrollBarThickness = 12.0;

	/// <summary>
	/// Builds the minimal scroll bar template: a track-colored root grid holding the two
	/// transparent large-change repeat buttons and the thumb, all with the part names the
	/// control's track-layout logic looks up. The control sizes the thumb and the decrease
	/// button itself; the increase button fills the remainder.
	/// </summary>
	static UIElement BuildScrollBarTemplateRoot(Orientation orientation)
	{
		var vertical = orientation == Orientation.Vertical;
		var prefix = vertical ? "Vertical" : "Horizontal";

		static RepeatButton CreateTrackButton(string name)
		{
			var button = new RepeatButton
			{
				Name = name,
				IsTabStop = false,
				// Transparent but hit-testable: clicking the track pages toward the click.
				Template = new ControlTemplate(() => new Border
				{
					Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
				}),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				MinWidth = 0,
				MinHeight = 0,
			};
			return button;
		}

		var thumb = new Thumb
		{
			Name = prefix + "Thumb",
			IsTabStop = false,
			MinWidth = 0,
			MinHeight = 0,
			Template = new ControlTemplate(() => new Border
			{
				Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xA0, 0x80, 0x80, 0x80)),
				CornerRadius = new CornerRadius(3),
				Margin = new Thickness(2),
			}),
		};

		var root = new Grid
		{
			Name = prefix + "Root",
			Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0x14, 0x00, 0x00, 0x00)),
		};

		var decrease = CreateTrackButton(prefix + "LargeDecrease");
		var increase = CreateTrackButton(prefix + "LargeIncrease");
		if (vertical)
		{
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			Grid.SetRow(decrease, 0);
			Grid.SetRow(thumb, 1);
			Grid.SetRow(increase, 2);
		}
		else
		{
			root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
			root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0, GridUnitType.Auto) });
			root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			Grid.SetColumn(decrease, 0);
			Grid.SetColumn(thumb, 1);
			Grid.SetColumn(increase, 2);
		}

		root.Children.Add(decrease);
		root.Children.Add(thumb);
		root.Children.Add(increase);
		return root;
	}

	UIElement CreateTemplateRoot()
	{
		Grid grid = new Grid();
		layoutRoot = grid;
		PushBackgroundToLayoutRoot();

		grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		textArea.HorizontalAlignment = HorizontalAlignment.Stretch;
		textArea.VerticalAlignment = VerticalAlignment.Stretch;
		Grid.SetRow(textArea, 0);
		Grid.SetColumn(textArea, 0);
		grid.Children.Add(textArea);

		Grid.SetRow(verticalScrollBar, 0);
		Grid.SetColumn(verticalScrollBar, 1);
		grid.Children.Add(verticalScrollBar);

		Grid.SetRow(horizontalScrollBar, 1);
		Grid.SetColumn(horizontalScrollBar, 0);
		grid.Children.Add(horizontalScrollBar);

		Grid.SetRow(cornerSpacer, 1);
		Grid.SetColumn(cornerSpacer, 1);
		grid.Children.Add(cornerSpacer);

		UpdateScrollBars();
		return grid;
	}

	void PushBackgroundToLayoutRoot()
	{
		if (layoutRoot == null)
			return;
		//was previously: the template Border bound Background (and BorderBrush/BorderThickness,
		//which this port does not render); a transparent fill keeps empty areas hit-testable.
		layoutRoot.Background = Background
			?? new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 255, 255, 255));
	}

	void VerticalScrollBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (updatingScrollBars)
			return;
		textArea.TextView.SetVerticalOffset(e.NewValue);
	}

	void HorizontalScrollBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (updatingScrollBars)
			return;
		textArea.TextView.SetHorizontalOffset(e.NewValue);
	}

	void TextView_ScrollOffsetChanged(object? sender, EventArgs e)
	{
		TextView textView = textArea.TextView;
		if (VerticalScrollBarVisibility == ScrollBarVisibility.Disabled && textView.VerticalOffset != 0)
		{
			// Disabled semantics: the vertical axis does not scroll. The view has no vertical
			// gate, so pin the offset back to 0; the re-entrant ScrollOffsetChanged updates the bars.
			textView.SetVerticalOffset(0);
			return;
		}
		UpdateScrollBars();
	}

	void TextView_VisualLinesChanged(object? sender, EventArgs e)
	{
		UpdateScrollBars();
	}

	/// <summary>
	/// Copies the text view's scroll surface (offset, extent, viewport) onto the scroll bars.
	/// Maximum is extent minus viewport, clamped to 0; a line is the default line height
	/// (vertically) or the wide-space width (horizontally); a page is the viewport.
	/// </summary>
	void UpdateScrollBars()
	{
		TextView textView = textArea.TextView;
		updatingScrollBars = true;
		try
		{
			double extentHeight = textView.ExtentHeight;
			double viewportHeight = textView.ViewportHeight;
			if (double.IsInfinity(viewportHeight))
				viewportHeight = extentHeight;
			verticalScrollBar.Minimum = 0;
			verticalScrollBar.Maximum = Math.Max(0, extentHeight - viewportHeight);
			verticalScrollBar.ViewportSize = viewportHeight;
			verticalScrollBar.SmallChange = textView.DefaultLineHeight;
			verticalScrollBar.LargeChange = viewportHeight;
			verticalScrollBar.Value = textView.VerticalOffset;

			double extentWidth = textView.ExtentWidth;
			double viewportWidth = textView.ViewportWidth;
			if (double.IsInfinity(viewportWidth))
				viewportWidth = extentWidth;
			horizontalScrollBar.Minimum = 0;
			horizontalScrollBar.Maximum = Math.Max(0, extentWidth - viewportWidth);
			horizontalScrollBar.ViewportSize = viewportWidth;
			horizontalScrollBar.SmallChange = textView.WideSpaceWidth;
			horizontalScrollBar.LargeChange = viewportWidth;
			horizontalScrollBar.Value = textView.HorizontalOffset;

			UpdateScrollBarVisibility();
		}
		finally
		{
			updatingScrollBars = false;
		}
	}

	void UpdateScrollBarVisibility()
	{
		TextView textView = textArea.TextView;
		bool verticalVisible = VerticalScrollBarVisibility switch
		{
			ScrollBarVisibility.Visible => true,
			ScrollBarVisibility.Auto => textView.ExtentHeight - textView.ViewportHeight > ScrollBarAutoEpsilon,
			_ => false, // Hidden and Disabled show no bar
		};
		bool horizontalVisible = !WordWrap && (HorizontalScrollBarVisibility switch
		{
			ScrollBarVisibility.Visible => true,
			ScrollBarVisibility.Auto => textView.ExtentWidth - textView.ViewportWidth > ScrollBarAutoEpsilon,
			_ => false,
		});
		verticalScrollBar.Visibility = verticalVisible ? Visibility.Visible : Visibility.Collapsed;
		horizontalScrollBar.Visibility = horizontalVisible ? Visibility.Visible : Visibility.Collapsed;
		cornerSpacer.Visibility = (verticalVisible && horizontalVisible) ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <summary>
	/// Pushes the word-wrap state into the text view. Disabling the horizontal scroll bar has the
	/// same effect as enabling word wrap (as in the previous version of this control, where the
	/// scrolling surface treated a non-scrollable horizontal axis as word wrap).
	/// </summary>
	void SyncWordWrapToTextView()
	{
		textArea.TextView.WordWrap = WordWrap
			|| HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled;
	}
	#endregion

	#region Font property forwarding
	//was previously: the WPF text area/view read the inherited font/foreground dependency
	//properties; this control pushes its values into the text area, which propagates them to the
	//text view.

	void PushFontPropertiesToTextArea()
	{
		textArea.FontFamily = FontFamily;
		textArea.FontSize = FontSize;
		textArea.FontWeight = FontWeight;
		textArea.FontStyle = FontStyle;
		textArea.FontStretch = FontStretch;
		textArea.Foreground = Foreground;
	}

	void RegisterFontPropertyCallbacks()
	{
		RegisterPropertyChangedCallback(FontFamilyProperty, (s, dp) => textArea.FontFamily = FontFamily);
		RegisterPropertyChangedCallback(FontSizeProperty, (s, dp) => textArea.FontSize = FontSize);
		RegisterPropertyChangedCallback(FontWeightProperty, (s, dp) => textArea.FontWeight = FontWeight);
		RegisterPropertyChangedCallback(FontStyleProperty, (s, dp) => textArea.FontStyle = FontStyle);
		RegisterPropertyChangedCallback(FontStretchProperty, (s, dp) => textArea.FontStretch = FontStretch);
		RegisterPropertyChangedCallback(ForegroundProperty, (s, dp) => textArea.Foreground = Foreground);
	}
	#endregion

	#region Focus forwarding
	/// <summary>
	/// Sets focus on the text area (the editable part of the editor).
	/// </summary>
	/// <param name="value">How focus was obtained (used for the focus visuals).</param>
	/// <returns>True when focus was set successfully; false otherwise.</returns>
	//was previously: WPF moved focus in OnGotKeyboardFocus only; this method additionally hides
	//UIElement.Focus so that a direct editor.Focus(...) call lands on the text area.
	public new bool Focus(FocusState value)
	{
		return textArea.Focus(value);
	}

	/// <summary>
	/// Forwards focus to the text area when the editor control itself receives focus.
	/// </summary>
	//was previously: OnGotKeyboardFocus checked e.NewFocus == this; the port checks that the
	//editor itself (not a child) is the element that received focus.
	protected override void OnGotFocus(RoutedEventArgs e)
	{
		base.OnGotFocus(e);
		if (ReferenceEquals(e.OriginalSource, this))
		{
			textArea.Focus(FocusState.Programmatic);
		}
	}
	#endregion

	#region Document property
	/// <summary>
	/// Document property.
	/// </summary>
	//was previously: TextView.DocumentProperty.AddOwner(typeof(TextEditor), ...); this framework
	//has no AddOwner, so the editor registers its own property and forwards to the text area.
	public static readonly DependencyProperty DocumentProperty =
		DependencyProperty.Register(nameof(Document), typeof(TextDocument), typeof(AdvancedTextEdit),
									new PropertyMetadata(null, OnDocumentChanged));

	/// <summary>
	/// Gets/Sets the document displayed by the text editor.
	/// This is a dependency property.
	/// </summary>
	public TextDocument Document {
		get { return (TextDocument)GetValue(DocumentProperty); }
		set { SetValue(DocumentProperty, value); }
	}

	/// <summary>
	/// Occurs when the document property has changed.
	/// </summary>
	public event EventHandler? DocumentChanged;

	/// <summary>
	/// Raises the <see cref="DocumentChanged"/> event.
	/// </summary>
	protected virtual void OnDocumentChanged(EventArgs e)
	{
		DocumentChanged?.Invoke(this, e);
	}

	static void OnDocumentChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((AdvancedTextEdit)dp).OnDocumentChanged((TextDocument?)e.OldValue, (TextDocument?)e.NewValue);
	}

	void OnDocumentChanged(TextDocument? oldValue, TextDocument? newValue)
	{
		if (oldValue != null)
		{
			TextDocumentWeakEventManager.TextChanged.RemoveListener(oldValue, this);
			//was previously: PropertyChangedEventManager.RemoveListener(oldValue.UndoStack, this,
			//"IsOriginalFile"); the port's manager is unfiltered, see ReceiveWeakEvent.
			PropertyChangedWeakEventManager.RemoveListener(oldValue.UndoStack, this);
		}
		textArea.SetValue(TextArea.DocumentProperty, newValue);
		if (newValue != null)
		{
			TextDocumentWeakEventManager.TextChanged.AddListener(newValue, this);
			PropertyChangedWeakEventManager.AddListener(newValue.UndoStack, this);
		}
		OnDocumentChanged(EventArgs.Empty);
		OnTextChanged(EventArgs.Empty);
	}
	#endregion

	#region Options property
	/// <summary>
	/// Options property.
	/// </summary>
	//was previously: TextView.OptionsProperty.AddOwner(typeof(TextEditor), ...); see DocumentProperty.
	public static readonly DependencyProperty OptionsProperty =
		DependencyProperty.Register(nameof(Options), typeof(AdvancedTextEditOptions), typeof(AdvancedTextEdit),
									new PropertyMetadata(null, OnOptionsChanged));

	/// <summary>
	/// Gets/Sets the options currently used by the text editor.
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
		((AdvancedTextEdit)dp).OnOptionsChanged((AdvancedTextEditOptions?)e.OldValue, (AdvancedTextEditOptions?)e.NewValue);
	}

	void OnOptionsChanged(AdvancedTextEditOptions? oldValue, AdvancedTextEditOptions? newValue)
	{
		if (oldValue != null)
		{
			PropertyChangedWeakEventManager.RemoveListener(oldValue, this);
		}
		textArea.SetValue(TextArea.OptionsProperty, newValue);
		if (newValue != null)
		{
			PropertyChangedWeakEventManager.AddListener(newValue, this);
		}
		OnOptionChanged(new PropertyChangedEventArgs(null));
	}

	/// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
	protected virtual bool ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		if (managerType == typeof(PropertyChangedWeakEventManager))
		{
			//was previously: two managers - PropertyChangedWeakEventManager for the options and
			//WPF's PropertyChangedEventManager (filtered on "IsOriginalFile") for the undo stack;
			//the port shares one manager and dispatches on the sender, filtering the property
			//name in HandleIsOriginalChanged.
			if (sender is UndoStack)
			{
				HandleIsOriginalChanged((PropertyChangedEventArgs)e);
				return true;
			}
			OnOptionChanged((PropertyChangedEventArgs)e);
			return true;
		}
		else if (managerType == typeof(TextDocumentWeakEventManager.TextChanged))
		{
			OnTextChanged(e);
			return true;
		}
		return false;
	}

	bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object? sender, EventArgs e)
	{
		return ReceiveWeakEvent(managerType, sender, e);
	}
	#endregion

	#region Text property
	/// <summary>
	/// Gets/Sets the text of the current document.
	/// </summary>
	public string Text {
		get {
			TextDocument document = this.Document;
			return document != null ? document.Text : string.Empty;
		}
		set {
			TextDocument document = GetDocument();
			document.Text = value ?? string.Empty;
			// after replacing the full text, the caret is positioned at the end of the document
			// - reset it to the beginning.
			this.CaretOffset = 0;
			document.UndoStack.ClearAll();
		}
	}

	TextDocument GetDocument()
	{
		TextDocument document = this.Document;
		if (document == null)
			throw ThrowUtil.NoDocumentAssigned();
		return document;
	}

	/// <summary>
	/// Occurs when the Text property changes.
	/// </summary>
	public event EventHandler? TextChanged;

	/// <summary>
	/// Raises the <see cref="TextChanged"/> event.
	/// </summary>
	protected virtual void OnTextChanged(EventArgs e)
	{
		TextChanged?.Invoke(this, e);
	}
	#endregion

	#region TextArea property / command dispatch
	readonly TextArea textArea;

	/// <summary>
	/// Gets the text area.
	/// </summary>
	public TextArea TextArea {
		get {
			return textArea;
		}
	}

	//was previously: RoutedUICommand.CanExecute/Execute(null, textArea) through WPF command
	//routing; the port dispatches through the text area's active input handler.
	bool CanExecute(EditorCommand command)
	{
		return textArea.ActiveInputHandler is TextAreaInputHandler inputHandler
			&& inputHandler.CanExecuteCommand(command, null, out bool canExecute)
			&& canExecute;
	}

	void Execute(EditorCommand command)
	{
		(textArea.ActiveInputHandler as TextAreaInputHandler)?.ExecuteCommand(command, null);
	}
	#endregion

	#region Syntax highlighting
	/// <summary>
	/// The <see cref="SyntaxHighlighting"/> property.
	/// </summary>
	public static readonly DependencyProperty SyntaxHighlightingProperty =
		DependencyProperty.Register(nameof(SyntaxHighlighting), typeof(IHighlightingDefinition), typeof(AdvancedTextEdit),
									new PropertyMetadata(null, OnSyntaxHighlightingChanged));

	/// <summary>
	/// Gets/sets the syntax highlighting definition used to colorize the text.
	/// </summary>
	public IHighlightingDefinition? SyntaxHighlighting {
		get { return (IHighlightingDefinition?)GetValue(SyntaxHighlightingProperty); }
		set { SetValue(SyntaxHighlightingProperty, value); }
	}

	IVisualLineTransformer? colorizer;

	static void OnSyntaxHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((AdvancedTextEdit)d).OnSyntaxHighlightingChanged(e.NewValue as IHighlightingDefinition);
	}

	void OnSyntaxHighlightingChanged(IHighlightingDefinition? newValue)
	{
		if (colorizer != null)
		{
			textArea.TextView.LineTransformers.Remove(colorizer);
			colorizer = null;
		}
		if (newValue != null)
		{
			colorizer = CreateColorizer(newValue);
			if (colorizer != null)
				textArea.TextView.LineTransformers.Insert(0, colorizer);
		}
	}

	/// <summary>
	/// Creates the highlighting colorizer for the specified highlighting definition.
	/// Allows derived classes to provide custom colorizer implementations for special highlighting definitions.
	/// </summary>
	/// <returns>The colorizer to insert into the text view's line transformers.</returns>
	protected virtual IVisualLineTransformer CreateColorizer(IHighlightingDefinition highlightingDefinition)
	{
		if (highlightingDefinition == null)
			throw new ArgumentNullException(nameof(highlightingDefinition));
		return new HighlightingColorizer(highlightingDefinition);
	}
	#endregion

	#region WordWrap
	/// <summary>
	/// Word wrap dependency property.
	/// </summary>
	public static readonly DependencyProperty WordWrapProperty =
		DependencyProperty.Register(nameof(WordWrap), typeof(bool), typeof(AdvancedTextEdit),
									new PropertyMetadata(Boxes.False, OnWordWrapChanged));

	/// <summary>
	/// Specifies whether the text editor uses word wrapping.
	/// </summary>
	/// <remarks>
	/// Setting WordWrap=true has the same effect as setting HorizontalScrollBarVisibility=Disabled and will override the
	/// HorizontalScrollBarVisibility setting. While word wrap is active, the horizontal scroll bar is hidden.
	/// </remarks>
	public bool WordWrap {
		get { return (bool)GetValue(WordWrapProperty); }
		set { SetValue(WordWrapProperty, Boxes.Box(value)); }
	}

	//was previously: a template trigger set the ScrollViewer's HorizontalScrollBarVisibility to
	//Disabled, which made IScrollInfo report CanHorizontallyScroll=false to the text view; the
	//port pushes the text view's WordWrap property and hides the horizontal bar directly.
	static void OnWordWrapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		AdvancedTextEdit editor = (AdvancedTextEdit)d;
		editor.SyncWordWrapToTextView();
		editor.UpdateScrollBars();
	}
	#endregion

	#region IsReadOnly
	/// <summary>
	/// IsReadOnly dependency property.
	/// </summary>
	public static readonly DependencyProperty IsReadOnlyProperty =
		DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(AdvancedTextEdit),
									new PropertyMetadata(Boxes.False, OnIsReadOnlyChanged));

	/// <summary>
	/// Specifies whether the user can change the text editor content.
	/// Setting this property will replace the
	/// <see cref="Editing.TextArea.ReadOnlySectionProvider">TextArea.ReadOnlySectionProvider</see>.
	/// </summary>
	public bool IsReadOnly {
		get { return (bool)GetValue(IsReadOnlyProperty); }
		set { SetValue(IsReadOnlyProperty, Boxes.Box(value)); }
	}

	static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AdvancedTextEdit editor)
		{
			if ((bool)e.NewValue)
				editor.TextArea.ReadOnlySectionProvider = ReadOnlySectionDocument.Instance;
			else
				editor.TextArea.ReadOnlySectionProvider = NoReadOnlySections.Instance;
			//was previously: the UI-automation peer was notified of the change; automation is not
			//part of this port.
		}
	}
	#endregion

	#region IsModified
	/// <summary>
	/// Dependency property for <see cref="IsModified"/>.
	/// </summary>
	public static readonly DependencyProperty IsModifiedProperty =
		DependencyProperty.Register(nameof(IsModified), typeof(bool), typeof(AdvancedTextEdit),
									new PropertyMetadata(Boxes.False, OnIsModifiedChanged));

	/// <summary>
	/// Gets/Sets the 'modified' flag.
	/// </summary>
	/// <remarks>
	/// The flag follows the document's undo stack: it is cleared when the undo stack reaches the
	/// state that was marked as the original file (e.g. after undoing all changes or after
	/// <see cref="Save(Stream)"/>), and set when the undo stack leaves that state.
	/// </remarks>
	public bool IsModified {
		get { return (bool)GetValue(IsModifiedProperty); }
		set { SetValue(IsModifiedProperty, Boxes.Box(value)); }
	}

	static void OnIsModifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AdvancedTextEdit editor)
		{
			TextDocument document = editor.Document;
			if (document != null)
			{
				UndoStack undoStack = document.UndoStack;
				if ((bool)e.NewValue)
				{
					if (undoStack.IsOriginalFile)
						undoStack.DiscardOriginalFileMarker();
				}
				else
				{
					undoStack.MarkAsOriginalFile();
				}
			}
		}
	}

	bool HandleIsOriginalChanged(PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(UndoStack.IsOriginalFile))
		{
			TextDocument document = this.Document;
			if (document != null)
			{
				SetValue(IsModifiedProperty, Boxes.Box(!document.UndoStack.IsOriginalFile));
			}
			return true;
		}
		else
		{
			return false;
		}
	}
	#endregion

	#region ShowLineNumbers
	/// <summary>
	/// ShowLineNumbers dependency property.
	/// </summary>
	public static readonly DependencyProperty ShowLineNumbersProperty =
		DependencyProperty.Register(nameof(ShowLineNumbers), typeof(bool), typeof(AdvancedTextEdit),
									new PropertyMetadata(Boxes.False, OnShowLineNumbersChanged));

	/// <summary>
	/// Specifies whether line numbers are shown on the left to the text view.
	/// </summary>
	public bool ShowLineNumbers {
		get { return (bool)GetValue(ShowLineNumbersProperty); }
		set { SetValue(ShowLineNumbersProperty, Boxes.Box(value)); }
	}

	static void OnShowLineNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		AdvancedTextEdit editor = (AdvancedTextEdit)d;
		var leftMargins = editor.TextArea.LeftMargins;
		if ((bool)e.NewValue)
		{
			LineNumberMargin lineNumbers = new LineNumberMargin();
			UIElement line = DottedLineMargin.Create();
			leftMargins.Insert(0, lineNumbers);
			leftMargins.Insert(1, line);
			//was previously: the margin's Foreground and the line's Stroke were data-bound to
			//LineNumbersForeground; the dotted-line element's Stroke is not a dependency property
			//in this port, so the editor pushes the brush manually (see ApplyLineNumbersForeground).
			editor.ApplyLineNumbersForeground();
		}
		else
		{
			for (int i = 0; i < leftMargins.Count; i++)
			{
				if (leftMargins[i] is LineNumberMargin)
				{
					leftMargins.RemoveAt(i);
					if (i < leftMargins.Count && DottedLineMargin.IsDottedLineMargin(leftMargins[i]))
					{
						leftMargins.RemoveAt(i);
					}
					break;
				}
			}
		}
	}
	#endregion

	#region LineNumbersForeground
	static Brush CreateSolidBrush(byte a, byte r, byte g, byte b)
	{
		return new SolidColorBrush(global::Windows.UI.Color.FromArgb(a, r, g, b));
	}

	/// <summary>
	/// LineNumbersForeground dependency property.
	/// </summary>
	public static readonly DependencyProperty LineNumbersForegroundProperty =
		DependencyProperty.Register(nameof(LineNumbersForeground), typeof(Brush), typeof(AdvancedTextEdit),
									new PropertyMetadata(CreateSolidBrush(255, 128, 128, 128), OnLineNumbersForegroundChanged));

	/// <summary>
	/// Gets/sets the Brush used for displaying the foreground color of line numbers.
	/// The dotted separator line between the line numbers and the text uses the same brush.
	/// </summary>
	public Brush? LineNumbersForeground {
		get { return (Brush?)GetValue(LineNumbersForegroundProperty); }
		set { SetValue(LineNumbersForegroundProperty, value); }
	}

	static void OnLineNumbersForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((AdvancedTextEdit)d).ApplyLineNumbersForeground();
	}

	//was previously: the changed callback updated only the LineNumberMargin (a binding kept the
	//dotted line's Stroke in sync); the port pushes the brush into both margin elements.
	void ApplyLineNumbersForeground()
	{
		Brush? brush = LineNumbersForeground;
		var lineNumberMargin = textArea.LeftMargins.FirstOrDefault(margin => margin is LineNumberMargin) as LineNumberMargin;
		if (lineNumberMargin != null)
		{
			lineNumberMargin.Foreground = brush;
		}
		var dottedLine = textArea.LeftMargins.FirstOrDefault(margin => margin is DottedLineMarginElement) as DottedLineMarginElement;
		if (dottedLine != null)
		{
			dottedLine.Stroke = brush;
		}
	}
	#endregion

	#region TextBoxBase-like methods
	/// <summary>
	/// Appends text to the end of the document.
	/// </summary>
	public void AppendText(string textData)
	{
		var document = GetDocument();
		document.Insert(document.TextLength, textData);
	}

	/// <summary>
	/// Begins a group of document changes.
	/// </summary>
	public void BeginChange()
	{
		GetDocument().BeginUpdate();
	}

	/// <summary>
	/// Copies the current selection to the clipboard.
	/// </summary>
	public void Copy()
	{
		Execute(EditorCommands.Copy);
	}

	/// <summary>
	/// Removes the current selection and copies it to the clipboard.
	/// </summary>
	public void Cut()
	{
		Execute(EditorCommands.Cut);
	}

	/// <summary>
	/// Begins a group of document changes and returns an object that ends the group of document
	/// changes when it is disposed.
	/// </summary>
	public IDisposable DeclareChangeBlock()
	{
		return GetDocument().RunUpdate();
	}

	/// <summary>
	/// Removes the current selection without copying it to the clipboard.
	/// </summary>
	public void Delete()
	{
		Execute(EditorCommands.Delete);
	}

	/// <summary>
	/// Ends the current group of document changes.
	/// </summary>
	public void EndChange()
	{
		GetDocument().EndUpdate();
	}

	//was previously: the Line*/Page* methods forwarded to the template ScrollViewer; the port
	//runs the equivalent offset math on the text view directly (a line is DefaultLineHeight
	//vertically / WideSpaceWidth horizontally, a page is the viewport).

	/// <summary>
	/// Scrolls one line down.
	/// </summary>
	public void LineDown()
	{
		TextView textView = textArea.TextView;
		textView.SetVerticalOffset(textView.VerticalOffset + textView.DefaultLineHeight);
	}

	/// <summary>
	/// Scrolls to the left.
	/// </summary>
	public void LineLeft()
	{
		TextView textView = textArea.TextView;
		textView.SetHorizontalOffset(textView.HorizontalOffset - textView.WideSpaceWidth);
	}

	/// <summary>
	/// Scrolls to the right.
	/// </summary>
	public void LineRight()
	{
		TextView textView = textArea.TextView;
		textView.SetHorizontalOffset(textView.HorizontalOffset + textView.WideSpaceWidth);
	}

	/// <summary>
	/// Scrolls one line up.
	/// </summary>
	public void LineUp()
	{
		TextView textView = textArea.TextView;
		textView.SetVerticalOffset(textView.VerticalOffset - textView.DefaultLineHeight);
	}

	/// <summary>
	/// Scrolls one page down.
	/// </summary>
	public void PageDown()
	{
		TextView textView = textArea.TextView;
		textView.SetVerticalOffset(textView.VerticalOffset + textView.ViewportHeight);
	}

	/// <summary>
	/// Scrolls one page up.
	/// </summary>
	public void PageUp()
	{
		TextView textView = textArea.TextView;
		textView.SetVerticalOffset(textView.VerticalOffset - textView.ViewportHeight);
	}

	/// <summary>
	/// Scrolls one page left.
	/// </summary>
	public void PageLeft()
	{
		TextView textView = textArea.TextView;
		textView.SetHorizontalOffset(textView.HorizontalOffset - textView.ViewportWidth);
	}

	/// <summary>
	/// Scrolls one page right.
	/// </summary>
	public void PageRight()
	{
		TextView textView = textArea.TextView;
		textView.SetHorizontalOffset(textView.HorizontalOffset + textView.ViewportWidth);
	}

	/// <summary>
	/// Pastes the clipboard content.
	/// </summary>
	public void Paste()
	{
		Execute(EditorCommands.Paste);
	}

	/// <summary>
	/// Redoes the most recent undone command.
	/// </summary>
	/// <returns>True is the redo operation was successful, false is the redo stack is empty.</returns>
	public bool Redo()
	{
		if (CanExecute(EditorCommands.Redo))
		{
			Execute(EditorCommands.Redo);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Scrolls to the end of the document.
	/// </summary>
	public void ScrollToEnd()
	{
		ApplyTemplate(); // ensure scroll bars are created
		TextView textView = textArea.TextView;
		textView.SetVerticalOffset(textView.ExtentHeight);
	}

	/// <summary>
	/// Scrolls to the start of the document.
	/// </summary>
	public void ScrollToHome()
	{
		ApplyTemplate(); // ensure scroll bars are created
		textArea.TextView.SetScrollOffset(0, 0);
	}

	/// <summary>
	/// Scrolls to the specified position in the document.
	/// </summary>
	public void ScrollToHorizontalOffset(double offset)
	{
		ApplyTemplate(); // ensure scroll bars are created
		textArea.TextView.SetHorizontalOffset(offset);
	}

	/// <summary>
	/// Scrolls to the specified position in the document.
	/// </summary>
	public void ScrollToVerticalOffset(double offset)
	{
		ApplyTemplate(); // ensure scroll bars are created
		textArea.TextView.SetVerticalOffset(offset);
	}

	/// <summary>
	/// Selects the entire text.
	/// </summary>
	public void SelectAll()
	{
		Execute(EditorCommands.SelectAll);
	}

	/// <summary>
	/// Undoes the most recent command.
	/// </summary>
	/// <returns>True is the undo operation was successful, false is the undo stack is empty.</returns>
	public bool Undo()
	{
		if (CanExecute(EditorCommands.Undo))
		{
			Execute(EditorCommands.Undo);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Gets if the most recent undone command can be redone.
	/// </summary>
	public bool CanRedo {
		get { return CanExecute(EditorCommands.Redo); }
	}

	/// <summary>
	/// Gets if the most recent command can be undone.
	/// </summary>
	public bool CanUndo {
		get { return CanExecute(EditorCommands.Undo); }
	}

	/// <summary>
	/// Gets the vertical size of the scrollable document area.
	/// </summary>
	public double ExtentHeight {
		get {
			return textArea.TextView.ExtentHeight;
		}
	}

	/// <summary>
	/// Gets the horizontal size of the scrollable document area.
	/// </summary>
	public double ExtentWidth {
		get {
			return textArea.TextView.ExtentWidth;
		}
	}

	/// <summary>
	/// Gets the vertical size of the viewport.
	/// </summary>
	public double ViewportHeight {
		get {
			return textArea.TextView.ViewportHeight;
		}
	}

	/// <summary>
	/// Gets the horizontal size of the viewport.
	/// </summary>
	public double ViewportWidth {
		get {
			return textArea.TextView.ViewportWidth;
		}
	}

	/// <summary>
	/// Gets the vertical scroll position.
	/// </summary>
	public double VerticalOffset {
		get {
			return textArea.TextView.VerticalOffset;
		}
	}

	/// <summary>
	/// Gets the horizontal scroll position.
	/// </summary>
	public double HorizontalOffset {
		get {
			return textArea.TextView.HorizontalOffset;
		}
	}
	#endregion

	#region TextBox methods
	/// <summary>
	/// Gets/Sets the selected text.
	/// </summary>
	public string SelectedText {
		get {
			// We'll get the text from the whole surrounding segment.
			// This is done to ensure that SelectedText.Length == SelectionLength.
			if (textArea.Document is TextDocument document && !textArea.Selection.IsEmpty
				&& textArea.Selection.SurroundingSegment is ISegment segment)
				return document.GetText(segment);
			else
				return string.Empty;
		}
		set {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			TextDocument? document = textArea.Document;
			if (document != null)
			{
				int offset = this.SelectionStart;
				int length = this.SelectionLength;
				document.Replace(offset, length, value);
				// keep inserted text selected
				textArea.Selection = Selection.Create(textArea, offset, offset + value.Length);
			}
		}
	}

	/// <summary>
	/// Gets/sets the caret position.
	/// </summary>
	public int CaretOffset {
		get {
			return textArea.Caret.Offset;
		}
		set {
			textArea.Caret.Offset = value;
		}
	}

	/// <summary>
	/// Gets/sets the start position of the selection.
	/// </summary>
	public int SelectionStart {
		get {
			//was previously: tested Selection.IsEmpty; a null surrounding segment identifies the
			//empty selection, and pattern-matching it keeps the non-empty path warning-free.
			if (textArea.Selection.SurroundingSegment is ISegment segment)
				return segment.Offset;
			else
				return textArea.Caret.Offset;
		}
		set {
			Select(value, SelectionLength);
		}
	}

	/// <summary>
	/// Gets/sets the length of the selection.
	/// </summary>
	public int SelectionLength {
		get {
			if (textArea.Selection.SurroundingSegment is ISegment segment)
				return segment.Length;
			else
				return 0;
		}
		set {
			Select(SelectionStart, value);
		}
	}

	/// <summary>
	/// Selects the specified text section.
	/// </summary>
	public void Select(int start, int length)
	{
		int documentLength = Document != null ? Document.TextLength : 0;
		if (start < 0 || start > documentLength)
			throw new ArgumentOutOfRangeException(nameof(start), start, "Value must be between 0 and " + documentLength);
		if (length < 0 || start + length > documentLength)
			throw new ArgumentOutOfRangeException(nameof(length), length, "Value must be between 0 and " + (documentLength - start));
		textArea.Selection = Selection.Create(textArea, start, start + length);
		textArea.Caret.Offset = start + length;
	}

	/// <summary>
	/// Gets the number of lines in the document.
	/// </summary>
	public int LineCount {
		get {
			TextDocument document = this.Document;
			if (document != null)
				return document.LineCount;
			else
				return 1;
		}
	}

	/// <summary>
	/// Clears the text.
	/// </summary>
	public void Clear()
	{
		this.Text = string.Empty;
	}
	#endregion

	#region Loading from stream
	/// <summary>
	/// Loads the text from the stream, auto-detecting the encoding.
	/// </summary>
	/// <remarks>
	/// This method sets <see cref="IsModified"/> to false.
	/// </remarks>
	public void Load(Stream stream)
	{
		using (StreamReader reader = FileReader.OpenStream(stream, this.Encoding ?? System.Text.Encoding.UTF8))
		{
			this.Text = reader.ReadToEnd();
			// assign encoding after ReadToEnd() so that the StreamReader can autodetect the encoding
			SetValue(EncodingProperty, reader.CurrentEncoding);
		}
		SetValue(IsModifiedProperty, Boxes.False);
	}

	/// <summary>
	/// Loads the text from the file, auto-detecting the encoding.
	/// </summary>
	public void Load(string fileName)
	{
		if (fileName == null)
			throw new ArgumentNullException(nameof(fileName));
		using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			Load(fs);
		}
	}

	/// <summary>
	/// Encoding dependency property.
	/// </summary>
	public static readonly DependencyProperty EncodingProperty =
		DependencyProperty.Register(nameof(Encoding), typeof(Encoding), typeof(AdvancedTextEdit),
									new PropertyMetadata(null));

	/// <summary>
	/// Gets/sets the encoding used when the file is saved.
	/// </summary>
	/// <remarks>
	/// The <see cref="Load(Stream)"/> method autodetects the encoding of the file and sets this property accordingly.
	/// The <see cref="Save(Stream)"/> method uses the encoding specified in this property.
	/// </remarks>
	public Encoding? Encoding {
		get { return (Encoding?)GetValue(EncodingProperty); }
		set { SetValue(EncodingProperty, value); }
	}

	/// <summary>
	/// Saves the text to the stream.
	/// </summary>
	/// <remarks>
	/// This method sets <see cref="IsModified"/> to false.
	/// </remarks>
	public void Save(Stream stream)
	{
		if (stream == null)
			throw new ArgumentNullException(nameof(stream));
		Encoding? encoding = this.Encoding;
		TextDocument document = this.Document;
		StreamWriter writer = encoding != null ? new StreamWriter(stream, encoding) : new StreamWriter(stream);
		if (document != null)
			document.WriteTextTo(writer);
		writer.Flush();
		// do not close the stream
		SetValue(IsModifiedProperty, Boxes.False);
	}

	/// <summary>
	/// Saves the text to the file.
	/// </summary>
	public void Save(string fileName)
	{
		if (fileName == null)
			throw new ArgumentNullException(nameof(fileName));
		using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			Save(fs);
		}
	}
	#endregion

	#region MouseHover events
	//was previously: four RoutedEvents re-exposed through AddOwner; the port re-exposes the text
	//view's plain events (subscribed in the constructor) with the editor as sender.

	/// <summary>
	/// Occurs when the mouse has hovered over a fixed location for some time.
	/// Raised immediately before <see cref="MouseHover"/>.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? PreviewMouseHover;

	/// <summary>
	/// Occurs when the mouse has hovered over a fixed location for some time.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? MouseHover;

	/// <summary>
	/// Occurs when the mouse had previously hovered but now started moving again.
	/// Raised immediately before <see cref="MouseHoverStopped"/>.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? PreviewMouseHoverStopped;

	/// <summary>
	/// Occurs when the mouse had previously hovered but now started moving again.
	/// </summary>
	public event EventHandler<PointerRoutedEventArgs>? MouseHoverStopped;
	#endregion

	#region ScrollBarVisibility
	/// <summary>
	/// Dependency property for <see cref="HorizontalScrollBarVisibility"/>.
	/// </summary>
	//was previously: ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner(...); the port
	//registers its own property and applies the semantics to its explicit scroll bars.
	public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
		DependencyProperty.Register(nameof(HorizontalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(AdvancedTextEdit),
									new PropertyMetadata(ScrollBarVisibility.Visible, OnScrollBarVisibilityChanged));

	/// <summary>
	/// Gets/Sets the horizontal scroll bar visibility.
	/// </summary>
	/// <remarks>
	/// Visible always shows the bar; Auto shows it only while the document is wider than the
	/// viewport; Hidden shows no bar but still allows scrolling; Disabled additionally disables
	/// horizontal scrolling, which (as in the previous version of this control) is the same as
	/// enabling word wrap.
	/// </remarks>
	public ScrollBarVisibility HorizontalScrollBarVisibility {
		get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
		set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
	}

	/// <summary>
	/// Dependency property for <see cref="VerticalScrollBarVisibility"/>.
	/// </summary>
	public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
		DependencyProperty.Register(nameof(VerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(AdvancedTextEdit),
									new PropertyMetadata(ScrollBarVisibility.Visible, OnScrollBarVisibilityChanged));

	/// <summary>
	/// Gets/Sets the vertical scroll bar visibility.
	/// </summary>
	/// <remarks>
	/// Visible always shows the bar; Auto shows it only while the document is taller than the
	/// viewport; Hidden shows no bar but still allows scrolling; Disabled additionally pins the
	/// vertical scroll offset to 0.
	/// </remarks>
	public ScrollBarVisibility VerticalScrollBarVisibility {
		get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
		set { SetValue(VerticalScrollBarVisibilityProperty, value); }
	}

	static void OnScrollBarVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		AdvancedTextEdit editor = (AdvancedTextEdit)d;
		editor.SyncWordWrapToTextView();
		if (editor.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
			editor.textArea.TextView.SetVerticalOffset(0);
		editor.UpdateScrollBars();
	}
	#endregion

	object? IServiceProvider.GetService(Type serviceType)
	{
		return textArea.GetService(serviceType);
	}

	/// <summary>
	/// Gets the text view position from a point inside the editor.
	/// </summary>
	/// <param name="point">The position, relative to top left
	/// corner of the editor control.</param>
	/// <returns>The text view position, or null if the point is outside the document.</returns>
	public TextViewPosition? GetPositionFromPoint(Point point)
	{
		if (this.Document == null)
			return null;
		TextView textView = textArea.TextView;
		Point p = TransformToVisual(textView).TransformPoint(point);
		return textView.GetPosition(new Point(p.X + textView.HorizontalOffset, p.Y + textView.VerticalOffset));
	}

	/// <summary>
	/// Scrolls to the specified line.
	/// This method requires that the editor was already assigned a size (layout must have run prior).
	/// </summary>
	public void ScrollToLine(int line)
	{
		ScrollTo(line, -1);
	}

	/// <summary>
	/// Scrolls to the specified line/column.
	/// This method requires that the editor was already assigned a size (layout must have run prior).
	/// </summary>
	public void ScrollTo(int line, int column)
	{
		const double MinimumScrollFraction = 0.3;
		ScrollTo(line, column, VisualYPosition.LineMiddle, textArea.TextView.ViewportHeight / 2, MinimumScrollFraction);
	}

	/// <summary>
	/// Scrolls to the specified line/column.
	/// This method requires that the editor was already assigned a size (layout must have run prior).
	/// </summary>
	/// <param name="line">Line to scroll to.</param>
	/// <param name="column">Column to scroll to (important if wrapping is 'on', and for the horizontal scroll position).</param>
	/// <param name="yPositionMode">The mode how to reference the Y position of the line.</param>
	/// <param name="referencedVerticalViewPortOffset">Offset from the top of the viewport to where the referenced line/column should be positioned.</param>
	/// <param name="minimumScrollFraction">The minimum vertical and/or horizontal scroll offset, expressed as fraction of the height or width of the viewport window, respectively.</param>
	public void ScrollTo(int line, int column, VisualYPosition yPositionMode, double referencedVerticalViewPortOffset, double minimumScrollFraction)
	{
		TextView textView = textArea.TextView;
		TextDocument document = textView.Document;
		if (document != null)
		{
			if (line < 1)
				line = 1;
			if (line > document.LineCount)
				line = document.LineCount;

			//was previously: gated on !IScrollInfo.CanHorizontallyScroll; word wrap is the port's
			//equivalent (see TextView's provenance notes).
			if (textView.WordWrap)
			{
				// Word wrap is enabled. Ensure that we have up-to-date info about line height so that we scroll
				// to the correct position.
				// This avoids that the user has to repeat the ScrollTo() call several times when there are very long lines.
				VisualLine vl = textView.GetOrConstructVisualLine(document.GetLineByNumber(line));
				double remainingHeight = referencedVerticalViewPortOffset;

				while (remainingHeight > 0)
				{
					DocumentLine? prevLine = vl.FirstDocumentLine.PreviousLine;
					if (prevLine == null)
						break;
					vl = textView.GetOrConstructVisualLine(prevLine);
					remainingHeight -= vl.Height;
				}
			}

			Point p = textView.GetVisualPosition(new TextViewPosition(line, Math.Max(1, column)), yPositionMode);
			double verticalPos = p.Y - referencedVerticalViewPortOffset;
			if (Math.Abs(verticalPos - textView.VerticalOffset) > minimumScrollFraction * textView.ViewportHeight)
			{
				textView.SetVerticalOffset(Math.Max(0, verticalPos));
			}
			if (column > 0)
			{
				//was previously: Caret.MinimumDistanceToViewBorder; the constant moved to the
				//text view in this port so the scrolling surface and the caret stay in sync.
				if (p.X > textView.ViewportWidth - TextView.MinimumDistanceToViewBorder * 2)
				{
					double horizontalPos = Math.Max(0, p.X - textView.ViewportWidth / 2);
					if (Math.Abs(horizontalPos - textView.HorizontalOffset) > minimumScrollFraction * textView.ViewportWidth)
					{
						textView.SetHorizontalOffset(horizontalPos);
					}
				}
				else
				{
					textView.SetHorizontalOffset(0);
				}
			}
		}
	}
}
