#nullable enable

using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/CompletionWindow.cs in the AvalonEdit
//repo (MIT). Following the Window->Popup re-expression in CompletionWindowBase:
//- The window chrome (border, background, corner radius) is built in code; the WPF sizing
//  (SizeToContent=Height, fixed Width=175, MaxHeight=300) became size-to-content in both axes
//  with MaxWidth=400/MaxHeight=300 and the upstream 30x15 minimum, scrolling inside the list.
//- The selected item's description ToolTip became a second small popup placed to the right of
//  the completion popup (no close animation, so its content is cleared directly on close).
//- The MouseWheel redirection into the list is dropped (the pointer wheel works over the popup
//  itself), and the PreviewTextInput forwarding became a TextEntered-driven filter refresh (the
//  caret-position tracking already drives the filter, exactly as upstream).

/// <summary>
/// The code completion window.
/// </summary>
public class CompletionWindow : CompletionWindowBase
{
	readonly CompletionList completionList = new CompletionList();
	readonly Border chrome;
	readonly ContentPresenter toolTipContentHost;
	readonly Border toolTipChrome;
	readonly Brush chromeForeground;
	Microsoft.UI.Xaml.Controls.Primitives.Popup? toolTip;

	/// <summary>
	/// Gets the completion list used in this completion window.
	/// </summary>
	public CompletionList CompletionList
	{
		get { return completionList; }
	}

	/// <summary>
	/// Creates a new code completion window.
	/// </summary>
	public CompletionWindow(TextArea textArea) : base(textArea)
	{
		this.CloseAutomatically = true;

		//was previously: SizeToContent = Height, MaxHeight = 300, Width = 175, MinHeight = 15,
		//MinWidth = 30 on the WPF window; the popup chrome below carries the equivalent limits,
		//with the width growing to fit the content up to 400.
		Brush background = textArea.Background ?? new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 255, 255, 255));
		chromeForeground = new SolidColorBrush(GetContrastingForeground(background));
		chrome = new Border
		{
			Background = background,
			BorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 128, 128, 128)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(3),
			MinWidth = 30,
			MinHeight = 15,
			MaxWidth = 400,
			MaxHeight = 300,
			Child = completionList
		};
		completionList.Foreground = chromeForeground;
		this.Content = chrome;
		chrome.SizeChanged += ChromeSizeChanged;

		//was previously: a WPF ToolTip with PlacementTarget = this and Placement = Right.
		toolTipContentHost = new ContentPresenter();
		toolTipChrome = new Border
		{
			Background = background,
			BorderBrush = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 128, 128, 128)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(3),
			Padding = new Thickness(4, 2, 4, 2),
			MaxWidth = 400,
			Child = toolTipContentHost
		};
		toolTip = new Microsoft.UI.Xaml.Controls.Primitives.Popup
		{
			IsLightDismissEnabled = false,
			Child = toolTipChrome
		};
		toolTip.Closed += ToolTipClosed;

		AttachEvents();
	}

	static global::Windows.UI.Color GetContrastingForeground(Brush background)
	{
		//was previously: item foregrounds came from WPF system colors; with the in-code chrome
		//the text color is derived from the background's luminance (dark editor -> light text).
		if (background is SolidColorBrush solidColorBrush)
		{
			global::Windows.UI.Color c = solidColorBrush.Color;
			double luminance = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
			if (luminance < 128)
				return global::Windows.UI.Color.FromArgb(255, 240, 240, 240);
		}
		return global::Windows.UI.Color.FromArgb(255, 16, 16, 16);
	}

	#region ToolTip handling
	void ToolTipClosed(object? sender, object e)
	{
		// Clear content after tooltip is closed.
		//was previously: the WPF tooltip used a close animation, forcing the deferred clear;
		//the popup closes immediately, so the content is cleared right here.
		toolTipContentHost.Content = null;
	}

	void CompletionListSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (toolTip == null)
			return;
		ICompletionData? item = completionList.SelectedItem;
		if (item == null)
			return;
		object? description = item.Description;
		if (description != null)
		{
			if (description is string descriptionText)
			{
				toolTipContentHost.Content = new TextBlock
				{
					Text = descriptionText,
					TextWrapping = TextWrapping.Wrap,
					Foreground = chromeForeground
				};
			}
			else
			{
				toolTipContentHost.Content = CreateContentElement(description, chromeForeground);
			}
			if (this.IsOpen)
			{
				UpdateToolTipPosition();
				toolTip.IsOpen = true;
			}
		}
		else
		{
			toolTip.IsOpen = false;
		}
	}

	void UpdateToolTipPosition()
	{
		if (toolTip == null)
			return;
		XamlRoot? xamlRoot = Popup.XamlRoot ?? this.TextArea.XamlRoot;
		if (xamlRoot == null)
			return;
		if (toolTip.XamlRoot != xamlRoot)
			toolTip.XamlRoot = xamlRoot;
		double width = chrome.ActualWidth;
		if (width <= 0)
			width = chrome.DesiredSize.Width;
		toolTip.HorizontalOffset = Popup.HorizontalOffset + width + 4;
		toolTip.VerticalOffset = Popup.VerticalOffset;
	}

	void ChromeSizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (toolTip != null && toolTip.IsOpen)
			UpdateToolTipPosition();
	}

	/// <inheritdoc/>
	protected override void UpdatePosition()
	{
		base.UpdatePosition();
		// keep the description popup glued to the right edge of the completion popup
		if (toolTip != null && toolTip.IsOpen)
			UpdateToolTipPosition();
	}
	#endregion

	void CompletionListInsertionRequested(object? sender, EventArgs e)
	{
		Close();
		// The window must close before Complete() is called.
		// If the Complete callback pushes stacked input handlers, we don't want to pop those when the CC window closes.
		ICompletionData? item = completionList.SelectedItem;
		if (item != null)
			item.Complete(this.TextArea, new AnchorSegment(this.TextArea.Document, this.StartOffset, this.EndOffset - this.StartOffset), e);
	}

	void AttachEvents()
	{
		this.completionList.InsertionRequested += CompletionListInsertionRequested;
		this.completionList.SelectionChanged += CompletionListSelectionChanged;
		this.TextArea.Caret.PositionChanged += CaretPositionChanged;
		//was previously: also this.TextArea.MouseWheel (redirected into the list's scroll
		//viewer) and this.TextArea.PreviewTextInput (re-raised on the window); the wheel works
		//directly over the in-app popup, and TextEntered below refreshes the filter (the caret
		//tracking already performs the actual filtering).
		this.TextArea.TextEntered += TextAreaTextEntered;
	}

	/// <inheritdoc/>
	protected override void DetachEvents()
	{
		this.completionList.InsertionRequested -= CompletionListInsertionRequested;
		this.completionList.SelectionChanged -= CompletionListSelectionChanged;
		this.TextArea.Caret.PositionChanged -= CaretPositionChanged;
		this.TextArea.TextEntered -= TextAreaTextEntered;
		base.DetachEvents();
	}

	/// <inheritdoc/>
	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
		if (toolTip != null)
		{
			toolTip.IsOpen = false;
			toolTip = null;
		}
	}

	/// <inheritdoc/>
	protected override bool OnKeyDown(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		bool handled = base.OnKeyDown(key, modifiers);
		if (!handled)
		{
			handled = completionList.HandleKey(key);
		}
		return handled;
	}

	void TextAreaTextEntered(object? sender, TextInputEventArgs e)
	{
		// refresh the filter; SelectItem short-circuits when the query text is unchanged
		CaretPositionChanged(sender, EventArgs.Empty);
	}

	/// <summary>
	/// Gets/Sets whether the completion window should close automatically.
	/// The default value is true.
	/// </summary>
	public bool CloseAutomatically { get; set; }

	/// <inheritdoc/>
	protected override bool CloseOnFocusLost
	{
		get { return this.CloseAutomatically; }
	}

	/// <summary>
	/// When this flag is set, code completion closes if the caret moves to the
	/// beginning of the allowed range. This is useful in Ctrl+Space and "complete when typing",
	/// but not in dot-completion.
	/// Has no effect if CloseAutomatically is false.
	/// </summary>
	public bool CloseWhenCaretAtBeginning { get; set; }

	void CaretPositionChanged(object? sender, EventArgs e)
	{
		int offset = this.TextArea.Caret.Offset;
		if (offset == this.StartOffset)
		{
			if (CloseAutomatically && CloseWhenCaretAtBeginning)
			{
				Close();
			}
			else
			{
				completionList.SelectItem(string.Empty);
			}
			return;
		}
		if (offset < this.StartOffset || offset > this.EndOffset)
		{
			if (CloseAutomatically)
			{
				Close();
			}
		}
		else
		{
			TextDocument document = this.TextArea.Document;
			if (document != null)
			{
				completionList.SelectItem(document.GetText(this.StartOffset, offset - this.StartOffset));
			}
		}
	}
}
