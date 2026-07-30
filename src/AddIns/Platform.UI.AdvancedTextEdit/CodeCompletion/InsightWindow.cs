#nullable enable

using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using CodeBrix.Platform.UI.AdvancedTextEdit.Editing;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/InsightWindow.cs (and the InsightWindow
//style in InsightWindow.xaml) in the AvalonEdit repo (MIT). Following the Window->Popup
//re-expression in CompletionWindowBase:
//- The tooltip-like chrome (1px border, corner radius 2, padding 1,1,3,1) is built in code;
//  the WPF SystemColors.Info/InfoText/WindowFrame theme brushes became fixed light-info colors.
//- OnSourceInitialized's screen working-area size limits are applied from the XamlRoot size
//  when the popup is shown.
//- The InsightWindowTemplateSelector (string content -> wrapping TextBlock) is performed by
//  the in-code content conversion.

/// <summary>
/// A popup-like tool window that is attached to a text segment.
/// </summary>
public class InsightWindow : CompletionWindowBase
{
	static readonly global::Windows.UI.Color InfoBackgroundColor = global::Windows.UI.Color.FromArgb(255, 255, 255, 225);
	static readonly global::Windows.UI.Color InfoForegroundColor = global::Windows.UI.Color.FromArgb(255, 16, 16, 16);
	static readonly global::Windows.UI.Color FrameBorderColor = global::Windows.UI.Color.FromArgb(255, 100, 100, 100);

	readonly ContentPresenter presenter = new ContentPresenter();
	readonly Border chrome;

	/// <summary>
	/// Creates a new InsightWindow.
	/// </summary>
	public InsightWindow(TextArea textArea) : base(textArea)
	{
		chrome = new Border
		{
			Background = new SolidColorBrush(InfoBackgroundColor),
			BorderBrush = new SolidColorBrush(FrameBorderColor),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(2),
			Padding = new Thickness(1, 1, 3, 1),
			Child = presenter
		};
		SetPopupChild(chrome);
		this.CloseAutomatically = true;
		AttachEvents();
	}

	/// <summary>
	/// Places the window content inside the tool-window chrome; strings become wrapping
	/// TextBlocks.
	/// </summary>
	protected override void OnContentChanged()
	{
		//was previously: the content flowed through the ControlTemplate's ContentPresenter with
		//the InsightWindowTemplateSelector.
		presenter.Content = CreateContentElement(this.Content, new SolidColorBrush(InfoForegroundColor));
	}

	/// <summary>
	/// Opens the insight popup. The maximum size is limited to the XamlRoot bounds
	/// (full height; width up to 60% of the root, but at least 1000 device-independent pixels
	/// when the root is that wide).
	/// </summary>
	//was previously: the limits were computed in OnSourceInitialized from the screen working
	//area of the caret's screen.
	public override void Show()
	{
		XamlRoot? xamlRoot = this.TextArea.XamlRoot;
		if (xamlRoot != null)
		{
			chrome.MaxHeight = xamlRoot.Size.Height;
			chrome.MaxWidth = Math.Min(xamlRoot.Size.Width, Math.Max(1000, xamlRoot.Size.Width * 0.6));
		}
		base.Show();
	}

	/// <summary>
	/// Gets/Sets whether the insight window should close automatically.
	/// The default value is true.
	/// </summary>
	public bool CloseAutomatically { get; set; }

	/// <inheritdoc/>
	protected override bool CloseOnFocusLost
	{
		get { return this.CloseAutomatically; }
	}

	void AttachEvents()
	{
		this.TextArea.Caret.PositionChanged += CaretPositionChanged;
	}

	/// <inheritdoc/>
	protected override void DetachEvents()
	{
		this.TextArea.Caret.PositionChanged -= CaretPositionChanged;
		base.DetachEvents();
	}

	void CaretPositionChanged(object? sender, EventArgs e)
	{
		if (this.CloseAutomatically)
		{
			int offset = this.TextArea.Caret.Offset;
			if (offset < this.StartOffset || offset > this.EndOffset)
			{
				Close();
			}
		}
	}
}
