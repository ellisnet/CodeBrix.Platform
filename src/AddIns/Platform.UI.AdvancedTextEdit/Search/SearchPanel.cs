#nullable enable

using System;
using System.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;
using CodeBrix.Platform.UI.AdvancedTextEdit.Rendering;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Search;

//was previously: ICSharpCode.AvalonEdit/Search/SearchPanel.cs (plus SearchPanel.xaml) in the
//AvalonEdit repo (MIT). The search orchestration is transliterated. Structural re-expressions:
//- The panel was hosted on the WPF AdornerLayer (SearchPanelAdorner); this framework has no
//  adorner layer, so Open/Close attach/detach the panel through the text area's internal overlay
//  seam (TextArea.AttachOverlay/RemoveOverlay), top-right above the text view.
//- The XAML ControlTemplate is built in code: a bordered horizontal StackPanel with the search
//  TextBox, three inline ToggleButtons ('Aa' match case, 'ab' whole words, '.*' regex - replacing
//  the DropDownButton+Popup checkbox dropdown, which is NOT ported), '<'/'>' find buttons (the
//  upstream prev.png/next.png arrow images are NOT copied) and an 'x' close button, plus a message
//  block used for the "no matches" and pattern-error texts (upstream: a ToolTip below the box).
//- Invalid patterns surfaced through WPF binding validation (ExceptionValidationRule); the port
//  catches SearchPatternException from the strategy factory and shows the error in the message
//  block, keeping the previous results highlighted.
//- The WPF MarkerPen property became the MarkerBorderBrush/MarkerBorderThickness pair.
//- The panel sits inside the text area's visual tree, so OnKeyDown fences every key event after
//  handling Enter/F3/Escape (on the adorner layer, panel keys never reached the text area).
//- The Install(TextEditor) convenience overload is not ported here; install on the TextArea.

/// <summary>
/// Provides search functionality for the editor. It is displayed in the top-right corner of the
/// text area.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class SearchPanel : Control
{
	readonly TextArea textArea;
	readonly SearchInputHandler handler;
	readonly SearchResultBackgroundRenderer renderer;
	TextDocument? currentDocument;
	ISearchStrategy? strategy;

	TextBox? searchTextBox;
	ToggleButton? matchCaseButton;
	ToggleButton? wholeWordsButton;
	ToggleButton? useRegexButton;
	Button? prevButton;
	Button? nextButton;
	Border? messageView;
	TextBlock? messageText;
	bool synchronizingControls;

	#region DependencyProperties
	/// <summary>
	/// Dependency property for <see cref="UseRegex"/>.
	/// </summary>
	public static readonly DependencyProperty UseRegexProperty =
		DependencyProperty.Register(nameof(UseRegex), typeof(bool), typeof(SearchPanel),
									new PropertyMetadata(false, SearchPatternChangedCallback));

	/// <summary>
	/// Gets/sets whether the search pattern should be interpreted as regular expression.
	/// </summary>
	public bool UseRegex {
		get { return (bool)GetValue(UseRegexProperty); }
		set { SetValue(UseRegexProperty, value); }
	}

	/// <summary>
	/// Dependency property for <see cref="MatchCase"/>.
	/// </summary>
	public static readonly DependencyProperty MatchCaseProperty =
		DependencyProperty.Register(nameof(MatchCase), typeof(bool), typeof(SearchPanel),
									new PropertyMetadata(false, SearchPatternChangedCallback));

	/// <summary>
	/// Gets/sets whether the search pattern should be interpreted case-sensitive.
	/// </summary>
	public bool MatchCase {
		get { return (bool)GetValue(MatchCaseProperty); }
		set { SetValue(MatchCaseProperty, value); }
	}

	/// <summary>
	/// Dependency property for <see cref="WholeWords"/>.
	/// </summary>
	public static readonly DependencyProperty WholeWordsProperty =
		DependencyProperty.Register(nameof(WholeWords), typeof(bool), typeof(SearchPanel),
									new PropertyMetadata(false, SearchPatternChangedCallback));

	/// <summary>
	/// Gets/sets whether the search pattern should only match whole words.
	/// </summary>
	public bool WholeWords {
		get { return (bool)GetValue(WholeWordsProperty); }
		set { SetValue(WholeWordsProperty, value); }
	}

	/// <summary>
	/// Dependency property for <see cref="SearchPattern"/>.
	/// </summary>
	public static readonly DependencyProperty SearchPatternProperty =
		DependencyProperty.Register(nameof(SearchPattern), typeof(string), typeof(SearchPanel),
									new PropertyMetadata("", SearchPatternChangedCallback));

	/// <summary>
	/// Gets/sets the search pattern.
	/// </summary>
	public string SearchPattern {
		get { return (string)GetValue(SearchPatternProperty); }
		set { SetValue(SearchPatternProperty, value); }
	}

	/// <summary>
	/// Dependency property for <see cref="MarkerBrush"/>.
	/// </summary>
	public static readonly DependencyProperty MarkerBrushProperty =
		DependencyProperty.Register(nameof(MarkerBrush), typeof(Brush), typeof(SearchPanel),
									new PropertyMetadata(CreateLightGreenBrush(), MarkerBrushChangedCallback));

	/// <summary>
	/// Gets/sets the Brush used for marking search results in the TextView.
	/// </summary>
	public Brush? MarkerBrush {
		get { return (Brush?)GetValue(MarkerBrushProperty); }
		set { SetValue(MarkerBrushProperty, value); }
	}

	static void MarkerBrushChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SearchPanel panel)
		{
			panel.renderer.MarkerBrush = (Brush?)e.NewValue;
		}
	}

	/// <summary>
	/// Dependency property for <see cref="MarkerBorderBrush"/>.
	/// </summary>
	//was previously: the MarkerPen dependency property held a WPF Pen; re-expressed as a
	//brush + thickness pair per the port's drawing rules.
	public static readonly DependencyProperty MarkerBorderBrushProperty =
		DependencyProperty.Register(nameof(MarkerBorderBrush), typeof(Brush), typeof(SearchPanel),
									new PropertyMetadata(null, MarkerBorderChangedCallback));

	/// <summary>
	/// Gets/sets the brush used for the border around search result markers in the TextView.
	/// Null (the default) draws no border.
	/// </summary>
	public Brush? MarkerBorderBrush {
		get { return (Brush?)GetValue(MarkerBorderBrushProperty); }
		set { SetValue(MarkerBorderBrushProperty, value); }
	}

	/// <summary>
	/// Dependency property for <see cref="MarkerBorderThickness"/>.
	/// </summary>
	public static readonly DependencyProperty MarkerBorderThicknessProperty =
		DependencyProperty.Register(nameof(MarkerBorderThickness), typeof(double), typeof(SearchPanel),
									new PropertyMetadata(1.0, MarkerBorderChangedCallback));

	/// <summary>
	/// Gets/sets the thickness of the border around search result markers. The border is drawn
	/// only while <see cref="MarkerBorderBrush"/> is non-null.
	/// </summary>
	public double MarkerBorderThickness {
		get { return (double)GetValue(MarkerBorderThicknessProperty); }
		set { SetValue(MarkerBorderThicknessProperty, value); }
	}

	static void MarkerBorderChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SearchPanel panel)
		{
			panel.renderer.MarkerBorderBrush = panel.MarkerBorderBrush;
			panel.renderer.MarkerBorderThickness = panel.MarkerBorderThickness;
		}
	}

	/// <summary>
	/// Dependency property for <see cref="MarkerCornerRadius"/>.
	/// </summary>
	public static readonly DependencyProperty MarkerCornerRadiusProperty =
		DependencyProperty.Register(nameof(MarkerCornerRadius), typeof(double), typeof(SearchPanel),
									new PropertyMetadata(3.0, MarkerCornerRadiusChangedCallback));

	/// <summary>
	/// Gets/sets the corner-radius used for marking search results in the TextView.
	/// </summary>
	public double MarkerCornerRadius {
		get { return (double)GetValue(MarkerCornerRadiusProperty); }
		set { SetValue(MarkerCornerRadiusProperty, value); }
	}

	static void MarkerCornerRadiusChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SearchPanel panel)
		{
			panel.renderer.MarkerCornerRadius = (double)e.NewValue;
		}
	}

	/// <summary>
	/// Dependency property for <see cref="Localization"/>.
	/// </summary>
	public static readonly DependencyProperty LocalizationProperty =
		DependencyProperty.Register(nameof(Localization), typeof(Localization), typeof(SearchPanel),
									new PropertyMetadata(new Localization(), LocalizationChangedCallback));

	/// <summary>
	/// Gets/sets the localization for the SearchPanel.
	/// </summary>
	public Localization Localization {
		get { return (Localization)GetValue(LocalizationProperty); }
		set { SetValue(LocalizationProperty, value); }
	}

	static void LocalizationChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SearchPanel panel)
		{
			panel.ApplyLocalization();
		}
	}

	static Brush CreateLightGreenBrush()
	{
		return new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 144, 238, 144));
	}
	#endregion

	static void SearchPatternChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is SearchPanel panel)
		{
			panel.SynchronizeControlsFromProperties();
			panel.UpdateSearch();
		}
	}

	void UpdateSearch()
	{
		// only reset as long as there are results
		// if no results are found, the "no matches found" message should not flicker.
		// if results are found by the next run, the message will be hidden inside DoSearch ...
		if (renderer.CurrentResults.Any())
			HideMessage();
		try
		{
			strategy = SearchStrategyFactory.Create(SearchPattern ?? "", !MatchCase, WholeWords, UseRegex ? SearchMode.RegEx : SearchMode.Normal);
			OnSearchOptionsChanged(new SearchOptionsChangedEventArgs(SearchPattern ?? "", MatchCase, UseRegex, WholeWords));
			DoSearch(true);
		}
		catch (SearchPatternException ex)
		{
			//was previously: surfaced through WPF binding validation on the search text box; see
			//the file header note.
			strategy = null;
			ShowMessage(Localization.ErrorText + ex.Message);
		}
	}

	/// <summary>
	/// Creates a new SearchPanel attached to the given text area.
	/// </summary>
	SearchPanel(TextArea textArea)
	{
		this.textArea = textArea;
		this.renderer = new SearchResultBackgroundRenderer();
		this.handler = new SearchInputHandler(textArea, this);
		this.IsClosed = true;

		HorizontalAlignment = HorizontalAlignment.Right;
		VerticalAlignment = VerticalAlignment.Top;

		currentDocument = textArea.Document;
		if (currentDocument != null)
			currentDocument.TextChanged += TextArea_Document_TextChanged;
		textArea.DocumentChanged += TextArea_DocumentChanged;

		//was previously: the visual came from the theme Style in SearchPanel.xaml.
		Template = new ControlTemplate(CreateTemplateRoot);
	}

	/// <summary>
	/// Creates a SearchPanel and installs it to the TextArea.
	/// </summary>
	public static SearchPanel Install(TextArea textArea)
	{
		if (textArea == null)
			throw new ArgumentNullException(nameof(textArea));
		SearchPanel panel = new SearchPanel(textArea);
		textArea.DefaultInputHandler.NestedInputHandlers.Add(panel.handler);
		return panel;
	}

	/// <summary>
	/// Adds the commands used by the SearchPanel to the given command binding collection
	/// (e.g. <see cref="TextAreaInputHandler.CommandBindings"/> of a custom input handler).
	/// </summary>
	//was previously: RegisterCommands(CommandBindingCollection).
	public void RegisterCommands(System.Collections.Generic.ICollection<EditorCommandBinding> commandBindings)
	{
		if (commandBindings == null)
			throw new ArgumentNullException(nameof(commandBindings));
		handler.RegisterGlobalCommands(commandBindings);
	}

	/// <summary>
	/// Removes the SearchPanel from the TextArea.
	/// </summary>
	public void Uninstall()
	{
		Close();
		textArea.DocumentChanged -= TextArea_DocumentChanged;
		if (currentDocument != null)
			currentDocument.TextChanged -= TextArea_Document_TextChanged;
		textArea.DefaultInputHandler.NestedInputHandlers.Remove(handler);
	}

	void TextArea_DocumentChanged(object? sender, EventArgs e)
	{
		if (currentDocument != null)
			currentDocument.TextChanged -= TextArea_Document_TextChanged;
		currentDocument = textArea.Document;
		if (currentDocument != null)
		{
			currentDocument.TextChanged += TextArea_Document_TextChanged;
			DoSearch(false);
		}
	}

	void TextArea_Document_TextChanged(object? sender, EventArgs e)
	{
		DoSearch(false);
	}

	#region Template (built in code)
	UIElement CreateTemplateRoot()
	{
		searchTextBox = new TextBox {
			Width = 150,
			Height = 24,
			Margin = new Thickness(3, 3, 3, 0),
			Text = SearchPattern ?? "",
		};
		searchTextBox.TextChanged += (sender, e) =>
		{
			if (!synchronizingControls)
				SearchPattern = searchTextBox.Text;
		};

		//was previously: a DropDownButton opening a Popup with three checkboxes; simplified to
		//three inline toggle buttons with plain text glyphs.
		matchCaseButton = CreateOptionToggle("Aa", MatchCase, value => MatchCase = value);
		wholeWordsButton = CreateOptionToggle("ab", WholeWords, value => WholeWords = value);
		useRegexButton = CreateOptionToggle(".*", UseRegex, value => UseRegex = value);

		//was previously: image buttons showing prev.png/next.png; the arrows are plain text here.
		prevButton = new Button { Content = "<", Margin = new Thickness(3), Height = 24, Width = 24, Padding = new Thickness(0) };
		prevButton.Click += (sender, e) => FindPrevious();
		nextButton = new Button { Content = ">", Margin = new Thickness(3), Height = 24, Width = 24, Padding = new Thickness(0) };
		nextButton.Click += (sender, e) => FindNext();

		//was previously: a 16x16 button stroking an 'X' path.
		var closeButton = new Button { Content = "x", Height = 16, Width = 16, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Top, IsTabStop = false };
		closeButton.Click += (sender, e) => Close();

		var row = new StackPanel { Orientation = Orientation.Horizontal };
		row.Children.Add(searchTextBox);
		row.Children.Add(matchCaseButton);
		row.Children.Add(wholeWordsButton);
		row.Children.Add(useRegexButton);
		row.Children.Add(prevButton);
		row.Children.Add(nextButton);
		row.Children.Add(closeButton);

		//was previously: a ToolTip placed below the search text box.
		messageText = new TextBlock { Margin = new Thickness(3) };
		messageView = new Border { Visibility = Visibility.Collapsed, Child = messageText };

		var column = new StackPanel { Orientation = Orientation.Vertical };
		column.Children.Add(row);
		column.Children.Add(messageView);

		//was previously: SystemColors.WindowBrush / WindowTextBrush; constants per the port rules.
		var root = new Border {
			Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 0, 0, 0)),
			BorderThickness = new Thickness(1),
			Child = column,
		};

		ApplyLocalization();
		SynchronizeControlsFromProperties();
		return root;
	}

	ToggleButton CreateOptionToggle(string glyph, bool isChecked, Action<bool> setOption)
	{
		var button = new ToggleButton {
			Content = glyph,
			Height = 24,
			Margin = new Thickness(0, 3, 0, 0),
			Padding = new Thickness(4, 0, 4, 0),
			IsChecked = isChecked,
		};
		button.Checked += (sender, e) =>
		{
			if (!synchronizingControls)
				setOption(true);
		};
		button.Unchecked += (sender, e) =>
		{
			if (!synchronizingControls)
				setOption(false);
		};
		return button;
	}

	void ApplyLocalization()
	{
		Localization localization = Localization;
		if (localization == null)
			return;
		if (matchCaseButton != null)
			ToolTipService.SetToolTip(matchCaseButton, localization.MatchCaseText);
		if (wholeWordsButton != null)
			ToolTipService.SetToolTip(wholeWordsButton, localization.MatchWholeWordsText);
		if (useRegexButton != null)
			ToolTipService.SetToolTip(useRegexButton, localization.UseRegexText);
		if (prevButton != null)
			ToolTipService.SetToolTip(prevButton, localization.FindPreviousText);
		if (nextButton != null)
			ToolTipService.SetToolTip(nextButton, localization.FindNextText);
	}

	void SynchronizeControlsFromProperties()
	{
		if (synchronizingControls)
			return;
		synchronizingControls = true;
		try
		{
			string pattern = SearchPattern ?? "";
			if (searchTextBox != null && searchTextBox.Text != pattern)
				searchTextBox.Text = pattern;
			if (matchCaseButton != null && matchCaseButton.IsChecked != MatchCase)
				matchCaseButton.IsChecked = MatchCase;
			if (wholeWordsButton != null && wholeWordsButton.IsChecked != WholeWords)
				wholeWordsButton.IsChecked = WholeWords;
			if (useRegexButton != null && useRegexButton.IsChecked != UseRegex)
				useRegexButton.IsChecked = UseRegex;
		}
		finally
		{
			synchronizingControls = false;
		}
	}

	void ShowMessage(string message)
	{
		if (messageText != null && messageView != null)
		{
			messageText.Text = message;
			messageView.Visibility = Visibility.Visible;
		}
	}

	void HideMessage()
	{
		if (messageView != null)
			messageView.Visibility = Visibility.Collapsed;
	}
	#endregion

	/// <summary>
	/// Reactivates the SearchPanel by setting the focus on the search box and selecting all text.
	/// </summary>
	public void Reactivate()
	{
		// Materialize the code-built template early so that the search box exists even when
		// reactivation runs before the first layout pass after Open().
		ApplyTemplate();
		if (searchTextBox == null)
			return;
		searchTextBox.Focus(FocusState.Programmatic);
		searchTextBox.SelectAll();
	}

	/// <summary>
	/// Moves to the next occurrence in the file.
	/// </summary>
	public void FindNext()
	{
		SearchResult? result = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset + 1);
		result ??= renderer.CurrentResults.FirstSegment;
		if (result != null)
		{
			SelectResult(result);
		}
	}

	/// <summary>
	/// Moves to the previous occurrence in the file.
	/// </summary>
	public void FindPrevious()
	{
		SearchResult? result = renderer.CurrentResults.FindFirstSegmentWithStartAfter(textArea.Caret.Offset);
		if (result != null)
			result = renderer.CurrentResults.GetPreviousSegment(result);
		result ??= renderer.CurrentResults.LastSegment;
		if (result != null)
		{
			SelectResult(result);
		}
	}

	void DoSearch(bool changeSelection)
	{
		if (IsClosed)
			return;
		renderer.CurrentResults.Clear();

		if (!string.IsNullOrEmpty(SearchPattern) && strategy != null && textArea.Document != null)
		{
			int offset = textArea.Caret.Offset;
			if (changeSelection)
			{
				textArea.ClearSelection();
			}
			// We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
			foreach (SearchResult result in strategy.FindAll(textArea.Document, 0, textArea.Document.TextLength))
			{
				if (changeSelection && result.StartOffset >= offset)
				{
					SelectResult(result);
					changeSelection = false;
				}
				renderer.CurrentResults.Add(result);
			}
			if (!renderer.CurrentResults.Any())
			{
				ShowMessage(Localization.NoMatchesFoundText);
			}
			else
			{
				HideMessage();
			}
		}
		textArea.TextView.InvalidateLayer(KnownLayer.Selection);
	}

	void SelectResult(SearchResult result)
	{
		textArea.Caret.Offset = result.StartOffset;
		textArea.Selection = Selection.Create(textArea, result.StartOffset, result.EndOffset);
		textArea.Caret.BringCaretToView();
		// show caret even if the editor does not have the Keyboard Focus
		textArea.Caret.Show();
	}

	/// <summary>
	/// Handles the panel's own keyboard shortcuts (Enter finds the next/previous match, F3 and
	/// Shift+F3 likewise, Escape closes the panel) and fences every key event off from the text
	/// area the panel is hosted in.
	/// </summary>
	protected override void OnKeyDown(KeyRoutedEventArgs e)
	{
		base.OnKeyDown(e);
		if (!e.Handled)
		{
			switch (e.Key)
			{
				case VirtualKey.Enter:
				case VirtualKey.F3:
					//was previously: Enter was handled here and F3/Shift+F3 arrived through the
					//panel's own WPF command bindings; both are handled directly in this port.
					if ((e.KeyboardModifiers & VirtualKeyModifiers.Shift) != 0)
						FindPrevious();
					else
						FindNext();
					break;
				case VirtualKey.Escape:
					Close();
					break;
			}
		}
		//was previously: the adorner layer sat outside the text area's tree, so panel keys never
		//reached the text area; here the panel is inside the text area's grid, so every key event
		//is marked handled to keep it from bubbling into the editor.
		e.Handled = true;
	}

	/// <summary>
	/// Gets whether the Panel is already closed.
	/// </summary>
	public bool IsClosed { get; private set; }

	/// <summary>
	/// Closes the SearchPanel.
	/// </summary>
	public void Close()
	{
		//was previously: IsKeyboardFocusWithin decided whether to give focus back to the text area.
		bool hasFocus = searchTextBox != null && searchTextBox.FocusState != FocusState.Unfocused;

		textArea.RemoveOverlay(this);
		HideMessage();
		textArea.TextView.BackgroundRenderers.Remove(renderer);
		if (hasFocus)
			textArea.Focus(FocusState.Programmatic);
		IsClosed = true;

		// Clear existing search results so that the segments don't have to be maintained
		renderer.CurrentResults.Clear();
		textArea.TextView.InvalidateLayer(KnownLayer.Selection);
	}

	/// <summary>
	/// Opens an existing search panel.
	/// </summary>
	public void Open()
	{
		if (!IsClosed)
			return;
		textArea.AttachOverlay(this);
		textArea.TextView.BackgroundRenderers.Add(renderer);
		IsClosed = false;
		DoSearch(false);
	}

	/// <summary>
	/// Fired when SearchOptions are changed inside the SearchPanel.
	/// </summary>
	public event EventHandler<SearchOptionsChangedEventArgs>? SearchOptionsChanged;

	/// <summary>
	/// Raises the <see cref="SearchPanel.SearchOptionsChanged" /> event.
	/// </summary>
	protected virtual void OnSearchOptionsChanged(SearchOptionsChangedEventArgs e)
	{
		SearchOptionsChanged?.Invoke(this, e);
	}
}

/// <summary>
/// EventArgs for <see cref="SearchPanel.SearchOptionsChanged"/> event.
/// </summary>
public class SearchOptionsChangedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the search pattern.
	/// </summary>
	public string SearchPattern { get; }

	/// <summary>
	/// Gets whether the search pattern should be interpreted case-sensitive.
	/// </summary>
	public bool MatchCase { get; }

	/// <summary>
	/// Gets whether the search pattern should be interpreted as regular expression.
	/// </summary>
	public bool UseRegex { get; }

	/// <summary>
	/// Gets whether the search pattern should only match whole words.
	/// </summary>
	public bool WholeWords { get; }

	/// <summary>
	/// Creates a new SearchOptionsChangedEventArgs instance.
	/// </summary>
	public SearchOptionsChangedEventArgs(string searchPattern, bool matchCase, bool useRegex, bool wholeWords)
	{
		this.SearchPattern = searchPattern;
		this.MatchCase = matchCase;
		this.UseRegex = useRegex;
		this.WholeWords = wholeWords;
	}
}
