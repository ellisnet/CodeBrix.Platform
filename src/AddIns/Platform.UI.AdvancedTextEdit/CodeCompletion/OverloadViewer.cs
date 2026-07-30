#nullable enable

using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.CodeCompletion;

//was previously: ICSharpCode.AvalonEdit/CodeCompletion/OverloadViewer.cs (and the OverloadViewer
//style in InsightWindow.xaml) in the AvalonEdit repo (MIT). The ControlTemplate is rebuilt in
//code: a Grid with up/down buttons ('▲'/'▼' TextBlocks replace the WPF Path triangles, sized
//14x14 instead of 9x9 so the glyphs stay legible), the "i of n" TextBlock, and header/content
//presenters. The template's WPF bindings against Provider.* became direct PropertyChanged
//listening (raise PropertyChanged on the UI thread), the CollapseIfSingleOverloadConverter
//became inline visibility logic, and string content becomes a wrapping TextBlock in code
//(was: InsightWindowTemplateSelector).

/// <summary>
/// Represents a text between "Up" and "Down" buttons.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class OverloadViewer : Control
{
	static readonly global::Windows.UI.Color ButtonBackgroundColor = global::Windows.UI.Color.FromArgb(255, 211, 211, 211);
	static readonly global::Windows.UI.Color GlyphForegroundColor = global::Windows.UI.Color.FromArgb(255, 16, 16, 16);

	readonly StackPanel upDownPanel;
	readonly TextBlock indexTextBlock;
	readonly ContentPresenter headerHost;
	readonly ContentPresenter contentHost;
	readonly PropertyChangedEventHandler providerPropertyChangedHandler;
	Grid? rootGrid;
	IOverloadProvider? attachedProvider;

	/// <summary>
	/// Creates a new OverloadViewer.
	/// </summary>
	public OverloadViewer()
	{
		providerPropertyChangedHandler = new PropertyChangedEventHandler(ProviderPropertyChanged);

		Button upButton = CreateUpDownButton("▲");
		upButton.Click += (sender, e) =>
		{
			ChangeIndex(-1);
		};

		indexTextBlock = new TextBlock
		{
			Margin = new Thickness(2, 0, 2, 0),
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = new SolidColorBrush(GlyphForegroundColor)
		};

		Button downButton = CreateUpDownButton("▼");
		downButton.Click += (sender, e) =>
		{
			ChangeIndex(+1);
		};

		upDownPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(0, 0, 4, 0),
			VerticalAlignment = VerticalAlignment.Top,
			Visibility = Visibility.Collapsed
		};
		upDownPanel.Children.Add(upButton);
		upDownPanel.Children.Add(indexTextBlock);
		upDownPanel.Children.Add(downButton);

		headerHost = new ContentPresenter();
		contentHost = new ContentPresenter();

		//was previously: the tree came from the OverloadViewer ControlTemplate in
		//InsightWindow.xaml; this port builds the equivalent tree in code.
		Template = new ControlTemplate(CreateTemplateRoot);
	}

	static Button CreateUpDownButton(string glyph)
	{
		return new Button
		{
			Width = 14,
			Height = 14,
			MinWidth = 0,
			MinHeight = 0,
			Padding = new Thickness(0),
			BorderThickness = new Thickness(0),
			Background = new SolidColorBrush(ButtonBackgroundColor),
			CornerRadius = new CornerRadius(2),
			VerticalAlignment = VerticalAlignment.Center,
			Content = new TextBlock
			{
				Text = glyph,
				FontSize = 8,
				Foreground = new SolidColorBrush(GlyphForegroundColor),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
	}

	UIElement CreateTemplateRoot()
	{
		rootGrid?.Children.Clear();
		rootGrid = new Grid();
		rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		Grid.SetRow(upDownPanel, 0);
		Grid.SetColumn(upDownPanel, 0);
		rootGrid.Children.Add(upDownPanel);

		Grid.SetRow(headerHost, 0);
		Grid.SetColumn(headerHost, 1);
		rootGrid.Children.Add(headerHost);

		Grid.SetRow(contentHost, 1);
		Grid.SetColumn(contentHost, 0);
		Grid.SetColumnSpan(contentHost, 2);
		rootGrid.Children.Add(contentHost);

		return rootGrid;
	}

	/// <summary>
	/// The text property.
	/// </summary>
	public static readonly DependencyProperty TextProperty =
		DependencyProperty.Register(nameof(Text), typeof(string), typeof(OverloadViewer),
									new PropertyMetadata(null));

	/// <summary>
	/// Gets/Sets the text between the Up and Down buttons.
	/// </summary>
	/// <remarks>
	/// The built-in visual tree displays <see cref="IOverloadProvider.CurrentIndexText"/> in
	/// that place (matching the original template); this property is retained for
	/// compatibility with the original API.
	/// </remarks>
	public string? Text
	{
		get { return (string?)GetValue(TextProperty); }
		set { SetValue(TextProperty, value); }
	}

	/// <summary>
	/// The ItemProvider property.
	/// </summary>
	public static readonly DependencyProperty ProviderProperty =
		DependencyProperty.Register(nameof(Provider), typeof(IOverloadProvider), typeof(OverloadViewer),
									new PropertyMetadata(null, OnProviderChanged));

	/// <summary>
	/// Gets/Sets the item provider.
	/// </summary>
	public IOverloadProvider? Provider
	{
		get { return (IOverloadProvider?)GetValue(ProviderProperty); }
		set { SetValue(ProviderProperty, value); }
	}

	static void OnProviderChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	{
		((OverloadViewer)dp).OnProviderChanged((IOverloadProvider?)e.NewValue);
	}

	void OnProviderChanged(IOverloadProvider? newProvider)
	{
		//was previously: the template bindings tracked Provider.* automatically; this port
		//subscribes PropertyChanged directly.
		if (attachedProvider != null)
			attachedProvider.PropertyChanged -= providerPropertyChangedHandler;
		attachedProvider = newProvider;
		if (attachedProvider != null)
			attachedProvider.PropertyChanged += providerPropertyChangedHandler;
		RefreshFromProvider();
	}

	void ProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		RefreshFromProvider();
	}

	void RefreshFromProvider()
	{
		IOverloadProvider? provider = attachedProvider;
		//was previously: CollapseIfSingleOverloadConverter on Provider.Count.
		upDownPanel.Visibility = (provider == null || provider.Count < 2)
			? Visibility.Collapsed : Visibility.Visible;
		indexTextBlock.Text = provider?.CurrentIndexText ?? string.Empty;
		headerHost.Content = CreateContentElement(provider?.CurrentHeader);
		contentHost.Content = CreateContentElement(provider?.CurrentContent);
	}

	static UIElement? CreateContentElement(object? content)
	{
		//was previously: the InsightWindowTemplateSelector replaced string content by a
		//TextBlock with TextWrapping.
		if (content == null)
			return null;
		if (content is UIElement element)
			return element;
		return new TextBlock
		{
			Text = content as string ?? content.ToString(),
			TextWrapping = TextWrapping.Wrap,
			Foreground = new SolidColorBrush(GlyphForegroundColor)
		};
	}

	/// <summary>
	/// Changes the selected index.
	/// </summary>
	/// <param name="relativeIndexChange">The relative index change - usual values are +1 or -1.</param>
	public void ChangeIndex(int relativeIndexChange)
	{
		IOverloadProvider? p = this.Provider;
		if (p != null)
		{
			int newIndex = p.SelectedIndex + relativeIndexChange;
			if (newIndex < 0)
				newIndex = p.Count - 1;
			if (newIndex >= p.Count)
				newIndex = 0;
			p.SelectedIndex = newIndex;
		}
	}
}
