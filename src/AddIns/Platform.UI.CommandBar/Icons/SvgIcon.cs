using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// An SVG icon: vector artwork drawn through the platform's SVG route, themed, tintable, and
/// rasterised for the display it is on.
/// </summary>
/// <remarks>
/// <para>
/// Use this element where an icon is wanted on its own - in a template, a menu item, or anywhere
/// the framework takes an <c>IconElement</c>. Where an icon is wanted as a VALUE, on
/// <c>ToolButton.Icon</c> or <c>AppBarButton.Icon</c>, use <see cref="SvgIconSource"/> instead; the
/// two carry the same properties and the source creates one of these when it is asked for an
/// element.
/// </para>
/// <para>
/// The artwork comes from <see cref="UriSource"/> (any scheme the platform reads, plus this
/// add-in's <see cref="IconResourceScheme">embedded-resource scheme</see>) or from
/// <see cref="Markup"/>, an SVG document written inline. Where <see cref="DarkUriSource"/> is set,
/// it replaces <see cref="UriSource"/> whenever the element's actual theme is dark, and the icon
/// swaps back the moment the theme changes.
/// </para>
/// <para>
/// The icon is rasterised at <see cref="IconSize"/> multiplied by the display's rasterization
/// scale, so it is pixel-exact rather than drawn at one size and stretched, and it is rasterised
/// again when that scale changes. Renderings are shared through <see cref="IconRasterCache"/>: the
/// same artwork, theme, size, scale and tint is parsed and rasterised once however many buttons
/// show it.
/// </para>
/// </remarks>
public partial class SvgIcon : ImageIcon
{
	private XamlRoot? _hookedRoot;
	private bool _updating;

	/// <summary>Initializes a new SVG icon.</summary>
	public SvgIcon()
	{
		ActualThemeChanged += OnActualThemeChanged;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;

		//An INHERITED attached property arrives without anyone setting it here - when the icon is
		//put into a bar, and again whenever the bar changes its mind - so watching the property is
		//the only way to know that EffectiveIconSize has a different answer than it had a moment
		//ago. Without this the icon renders once at the default 24 and is then stretched to
		//whatever size the bar asked for, which is exactly the blur this add-in exists to avoid.
		RegisterPropertyChangedCallback(ToolBarProperties.IconSizeProperty, OnInheritedIconSizeChanged);
	}

	/// <summary>The artwork, and the artwork used in the light theme where a dark one is also
	/// given.</summary>
	public Uri? UriSource
	{
		get => (Uri?)GetValue(UriSourceProperty);
		set => SetValue(UriSourceProperty, value);
	}

	/// <summary>Identifies the <see cref="UriSource"/> dependency property.</summary>
	public static DependencyProperty UriSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(UriSource),
			typeof(Uri),
			typeof(SvgIcon),
			new PropertyMetadata(null, OnIconPropertyChanged));

	/// <summary>The artwork to use when the element's actual theme is dark; optional.</summary>
	public Uri? DarkUriSource
	{
		get => (Uri?)GetValue(DarkUriSourceProperty);
		set => SetValue(DarkUriSourceProperty, value);
	}

	/// <summary>Identifies the <see cref="DarkUriSource"/> dependency property.</summary>
	public static DependencyProperty DarkUriSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(DarkUriSource),
			typeof(Uri),
			typeof(SvgIcon),
			new PropertyMetadata(null, OnIconPropertyChanged));

	/// <summary>An SVG document written inline, used instead of <see cref="UriSource"/> when
	/// set.</summary>
	public string? Markup
	{
		get => (string?)GetValue(MarkupProperty);
		set => SetValue(MarkupProperty, value);
	}

	/// <summary>Identifies the <see cref="Markup"/> dependency property.</summary>
	public static DependencyProperty MarkupProperty { get; } =
		DependencyProperty.Register(
			nameof(Markup),
			typeof(string),
			typeof(SvgIcon),
			new PropertyMetadata(null, OnIconPropertyChanged));

	/// <summary>
	/// The colour to paint the artwork in; leave it unset to draw the file exactly as it was drawn.
	/// </summary>
	/// <remarks>
	/// Only a <c>SolidColorBrush</c> can tint artwork, and only its colour is used - a translucent
	/// icon comes from the element's <c>Opacity</c>, not from the brush's alpha. Bind it to a theme
	/// resource and the icon follows the theme, because the icon re-renders when the theme changes.
	/// </remarks>
	public Brush? Tint
	{
		get => (Brush?)GetValue(TintProperty);
		set => SetValue(TintProperty, value);
	}

	/// <summary>Identifies the <see cref="Tint"/> dependency property.</summary>
	public static DependencyProperty TintProperty { get; } =
		DependencyProperty.Register(
			nameof(Tint),
			typeof(Brush),
			typeof(SvgIcon),
			new PropertyMetadata(null, OnIconPropertyChanged));

	/// <summary>How far <see cref="Tint"/> reaches into the artwork.</summary>
	public IconTintMode TintMode
	{
		get => (IconTintMode)GetValue(TintModeProperty);
		set => SetValue(TintModeProperty, value);
	}

	/// <summary>Identifies the <see cref="TintMode"/> dependency property.</summary>
	public static DependencyProperty TintModeProperty { get; } =
		DependencyProperty.Register(
			nameof(TintMode),
			typeof(IconTintMode),
			typeof(SvgIcon),
			new PropertyMetadata(IconTintMode.CurrentColorOnly, OnIconPropertyChanged));

	/// <summary>
	/// The icon's edge length in logical pixels; NaN, the default, reads
	/// <see cref="ToolBarProperties.IconSizeProperty"/> from the tree instead.
	/// </summary>
	/// <remarks>
	/// It is called Size rather than IconSize deliberately: a dependency property whose NAME matches
	/// an inherited attached property SHADOWS that attached property on the declaring type, so an
	/// element with its own "IconSize" would stop seeing the bar's
	/// <see cref="ToolBarProperties.IconSizeProperty"/> entirely. Measured, not assumed - see the
	/// suite.
	/// </remarks>
	public double Size
	{
		get => (double)GetValue(SizeProperty);
		set => SetValue(SizeProperty, value);
	}

	/// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
	public static DependencyProperty SizeProperty { get; } =
		DependencyProperty.Register(
			nameof(Size),
			typeof(double),
			typeof(SvgIcon),
			new PropertyMetadata(double.NaN, OnIconPropertyChanged));

	/// <summary>The size this icon is currently drawn at, in logical pixels.</summary>
	/// <remarks>
	/// <see cref="Size"/> when it is set, and the inherited
	/// <see cref="ToolBarProperties.IconSizeProperty"/> otherwise - which is how a bar sets the
	/// size of every icon in it at once.
	/// </remarks>
	public double EffectiveIconSize
		=> double.IsNaN(Size) ? ToolBarProperties.GetIconSize(this) : Size;

	/// <summary>The stylesheet this icon hands the SVG parser, or null when it tints nothing.</summary>
	internal string? TintCss => SvgTintCss.Compose(Tint, TintMode);

	/// <summary>The artwork actually chosen, after the theme has had its say.</summary>
	/// <remarks>Null when the icon draws inline <see cref="Markup"/>, or has no artwork at all.</remarks>
	public Uri? ResolvedUriSource { get; private set; }

	/// <summary>What the last render was keyed on, for a test or a diagnostic.</summary>
	internal IconCacheKey LastKey { get; private set; }

	/// <summary>Re-reads every input and re-renders if anything about the look has changed.</summary>
	/// <remarks>
	/// Called for you when a property, the theme or the display scale changes. It is public because
	/// an application that changes something the element cannot observe - a brush's colour in
	/// place, say - has no other way to say so.
	/// </remarks>
	public void UpdateIcon()
	{
		if (_updating)
		{
			return;
		}

		_updating = true;
		try
		{
			var size = EffectiveIconSize;
			var scale = IconScale.Of(this);
			var theme = ActualTheme;
			var css = TintCss;

			var artwork = theme == ElementTheme.Dark && DarkUriSource is not null
				? DarkUriSource
				: UriSource;
			var markup = Markup;

			ResolvedUriSource = string.IsNullOrEmpty(markup) ? artwork : null;

			if (string.IsNullOrEmpty(markup) && artwork is null)
			{
				Source = null!;
				return;
			}

			var identity = string.IsNullOrEmpty(markup) ? artwork!.ToString() : "markup:" + markup;
			var key = new IconCacheKey(identity, theme, size, scale, css ?? string.Empty);
			LastKey = key;

			Source = IconRasterCache.GetOrCreate(key, () => SvgImageSourceFactory.Create(artwork, markup, size, css));

			Width = size;
			Height = size;
		}
		finally
		{
			_updating = false;
		}
	}

	private static void OnIconPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((SvgIcon)sender).UpdateIcon();

	private void OnInheritedIconSizeChanged(DependencyObject sender, DependencyProperty property)
	{
		//An icon that states its own Size ignores the bar, so there is nothing to redo for it.
		if (double.IsNaN(Size))
		{
			UpdateIcon();
		}
	}

	private void OnActualThemeChanged(FrameworkElement sender, object args) => UpdateIcon();

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		HookXamlRoot();
		UpdateIcon();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e) => UnhookXamlRoot();

	private void HookXamlRoot()
	{
		if (ReferenceEquals(_hookedRoot, XamlRoot))
		{
			return;
		}

		UnhookXamlRoot();

		if (XamlRoot is { } root)
		{
			root.Changed += OnXamlRootChanged;
			_hookedRoot = root;
		}
	}

	private void UnhookXamlRoot()
	{
		if (_hookedRoot is { } root)
		{
			root.Changed -= OnXamlRootChanged;
			_hookedRoot = null;
		}
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateIcon();
}
