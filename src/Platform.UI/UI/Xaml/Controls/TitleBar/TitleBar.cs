// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TitleBar.idl, TitleBar.properties.cpp, TitleBar.properties.h, commit fc2f82117

using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a title bar: the window's title, subtitle and icon, a back button, a pane toggle button, and headers and
/// content of the application's own, laid out as WinUI's TitleBar control lays them out.
/// </summary>
/// <remarks>
/// The control is an ordinary element: it is drawn where the application places it. An application that also sets
/// Window.ExtendsContentIntoTitleBar (and Window.SetTitleBar) gets it in the window's title-bar area on the heads that
/// can extend content there; see AGENT-README (TITLEBAR).
/// </remarks>
[ContentProperty(Name = nameof(Content))]
public partial class TitleBar : Control
{
	/// <summary>Gets or sets the title text to display in the title bar.</summary>
	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>Gets or sets the subtitle text to display in the title bar.</summary>
	public string Subtitle
	{
		get => (string)GetValue(SubtitleProperty);
		set => SetValue(SubtitleProperty, value);
	}

	/// <summary>Gets or sets the icon of the title bar.</summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>Gets or sets the content shown at the left of the title bar (after the buttons).</summary>
	public UIElement LeftHeader
	{
		get => (UIElement)GetValue(LeftHeaderProperty);
		set => SetValue(LeftHeaderProperty, value);
	}

	/// <summary>Gets or sets the content shown in the middle of the title bar.</summary>
	public UIElement Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	/// <summary>Gets or sets the content shown at the right of the title bar.</summary>
	public UIElement RightHeader
	{
		get => (UIElement)GetValue(RightHeaderProperty);
		set => SetValue(RightHeaderProperty, value);
	}

	/// <summary>Gets or sets a value that indicates whether the back button is visible. The default is false.</summary>
	public bool IsBackButtonVisible
	{
		get => (bool)GetValue(IsBackButtonVisibleProperty);
		set => SetValue(IsBackButtonVisibleProperty, value);
	}

	/// <summary>Gets or sets a value that indicates whether the back button is enabled. The default is true.</summary>
	public bool IsBackButtonEnabled
	{
		get => (bool)GetValue(IsBackButtonEnabledProperty);
		set => SetValue(IsBackButtonEnabledProperty, value);
	}

	/// <summary>Gets or sets a value that indicates whether the pane toggle button is visible. The default is false.</summary>
	public bool IsPaneToggleButtonVisible
	{
		get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
		set => SetValue(IsPaneToggleButtonVisibleProperty, value);
	}

	/// <summary>Gets the values a template can bind to (the icon element built from <see cref="IconSource"/>).</summary>
	public TitleBarTemplateSettings TemplateSettings => (TitleBarTemplateSettings)GetValue(TemplateSettingsProperty);

	/// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
	public static DependencyProperty TitleProperty { get; } =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar),
			new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	/// <summary>Identifies the <see cref="Subtitle"/> dependency property.</summary>
	public static DependencyProperty SubtitleProperty { get; } =
		DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TitleBar),
			new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

	/// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(nameof(IconSource), typeof(IconSource), typeof(TitleBar),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>Identifies the <see cref="LeftHeader"/> dependency property.</summary>
	public static DependencyProperty LeftHeaderProperty { get; } =
		DependencyProperty.Register(nameof(LeftHeader), typeof(UIElement), typeof(TitleBar),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(nameof(Content), typeof(UIElement), typeof(TitleBar),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>Identifies the <see cref="RightHeader"/> dependency property.</summary>
	public static DependencyProperty RightHeaderProperty { get; } =
		DependencyProperty.Register(nameof(RightHeader), typeof(UIElement), typeof(TitleBar),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

	/// <summary>Identifies the <see cref="IsBackButtonVisible"/> dependency property.</summary>
	public static DependencyProperty IsBackButtonVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(bool), typeof(TitleBar),
			new FrameworkPropertyMetadata(false, OnPropertyChanged));

	/// <summary>Identifies the <see cref="IsBackButtonEnabled"/> dependency property.</summary>
	public static DependencyProperty IsBackButtonEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsBackButtonEnabled), typeof(bool), typeof(TitleBar),
			new FrameworkPropertyMetadata(true, OnPropertyChanged));

	/// <summary>Identifies the <see cref="IsPaneToggleButtonVisible"/> dependency property.</summary>
	public static DependencyProperty IsPaneToggleButtonVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsPaneToggleButtonVisible), typeof(bool), typeof(TitleBar),
			new FrameworkPropertyMetadata(false, OnPropertyChanged));

	/// <summary>Identifies the <see cref="TemplateSettings"/> dependency property.</summary>
	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(TitleBarTemplateSettings), typeof(TitleBar),
			new FrameworkPropertyMetadata(null));

	/// <summary>Occurs when the back button is invoked.</summary>
	public event TypedEventHandler<TitleBar, object> BackRequested;

	/// <summary>Occurs when the pane toggle button is invoked.</summary>
	public event TypedEventHandler<TitleBar, object> PaneToggleRequested;

	private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TitleBar)sender).OnPropertyChanged(args);
}
