using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// An SVG icon, as a value: the icon to put on a <c>ToolButton.Icon</c>, an
/// <c>AppBarButton.Icon</c>, or anywhere else the framework takes an icon source.
/// </summary>
/// <remarks>
/// <para>
/// Everything <see cref="SvgIcon"/> does, this describes: artwork from a URI or written inline, an
/// alternate for the dark theme, a tint, and a size. Asked for an element it creates an
/// <see cref="SvgIcon"/> BOUND to itself, so one source can drive several buttons and a later change
/// - a new tint, a new size - reaches all of them.
/// </para>
/// <para>
/// In XAML the terse form is the <see cref="SvgIconExtension">{cb:SvgIcon}</see> markup extension;
/// this full form is the one to use when a property has to be bound.
/// </para>
/// </remarks>
public partial class SvgIconSource : ToolIconSource
{
	/// <summary>Initializes a new SVG icon source.</summary>
	public SvgIconSource()
	{
	}

	/// <summary>The artwork, and the artwork used in the light theme where a dark one is also
	/// given.</summary>
	/// <remarks>
	/// Any scheme the platform reads - <c>ms-appx:///</c>, <c>ms-appdata:///</c>, <c>file:</c>,
	/// <c>http(s):</c> - plus this add-in's <see cref="IconResourceScheme">cb-res://</see> scheme for
	/// artwork embedded in an assembly.
	/// </remarks>
	public Uri? Source
	{
		get => (Uri?)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(
			nameof(Source),
			typeof(Uri),
			typeof(SvgIconSource),
			new FrameworkPropertyMetadata(null));

	/// <summary>The artwork to use when the theme is dark; optional.</summary>
	public Uri? Dark
	{
		get => (Uri?)GetValue(DarkProperty);
		set => SetValue(DarkProperty, value);
	}

	/// <summary>Identifies the <see cref="Dark"/> dependency property.</summary>
	public static DependencyProperty DarkProperty { get; } =
		DependencyProperty.Register(
			nameof(Dark),
			typeof(Uri),
			typeof(SvgIconSource),
			new FrameworkPropertyMetadata(null));

	/// <summary>An SVG document written inline, used instead of <see cref="Source"/> when set.</summary>
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
			typeof(SvgIconSource),
			new FrameworkPropertyMetadata(null));

	/// <summary>The colour to paint the artwork in; unset draws the file exactly as drawn.</summary>
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
			typeof(SvgIconSource),
			new FrameworkPropertyMetadata(null));

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
			typeof(SvgIconSource),
			new FrameworkPropertyMetadata(IconTintMode.CurrentColorOnly));

	/// <summary>
	/// The icon's edge length in logical pixels; NaN, the default, takes the size from the bar
	/// through <see cref="ToolBarProperties.IconSizeProperty"/>.
	/// </summary>
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
			typeof(SvgIconSource),
			new FrameworkPropertyMetadata(double.NaN));

	/// <summary>Creates the element that draws this icon.</summary>
	/// <returns>An <see cref="SvgIcon"/> bound to this source.</returns>
#if !HAS_CODEBRIX_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		var icon = new SvgIcon();

		IconBinding.Bind(icon, SvgIcon.UriSourceProperty, this, nameof(Source));
		IconBinding.Bind(icon, SvgIcon.DarkUriSourceProperty, this, nameof(Dark));
		IconBinding.Bind(icon, SvgIcon.MarkupProperty, this, nameof(Markup));
		IconBinding.Bind(icon, SvgIcon.TintProperty, this, nameof(Tint));
		IconBinding.Bind(icon, SvgIcon.TintModeProperty, this, nameof(TintMode));
		IconBinding.Bind(icon, SvgIcon.SizeProperty, this, nameof(Size));

		return icon;
	}
}
