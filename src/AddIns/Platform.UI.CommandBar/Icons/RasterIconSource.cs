using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// A bitmap icon, as a value: the icon to put on a <c>ToolButton.Icon</c>, an
/// <c>AppBarButton.Icon</c>, or anywhere else the framework takes an icon source.
/// </summary>
/// <remarks>
/// <para>
/// PNG is the format to reach for; JPEG, BMP, GIF, WebP and ICO come free, because the decoding is
/// the platform's and this add-in adds no format-specific code. Asked for an element it creates a
/// <see cref="RasterIcon"/> BOUND to itself.
/// </para>
/// <para>
/// In XAML the terse form is the <see cref="RasterIconSourceExtension">{cb:RasterIconSource}</see>
/// markup extension.
/// </para>
/// </remarks>
public partial class RasterIconSource : ToolIconSource
{
	/// <summary>Initializes a new raster icon source.</summary>
	public RasterIconSource()
	{
	}

	/// <summary>The artwork, and the artwork used in the light theme where a dark one is also
	/// given.</summary>
	/// <remarks>
	/// Any scheme the platform reads, plus this add-in's
	/// <see cref="IconResourceScheme">cb-res://</see> scheme. A file named with a scale qualifier
	/// beside it - <c>open.scale-200.png</c> beside <c>open.png</c> - is preferred when it matches
	/// the display better.
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
			typeof(RasterIconSource),
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
			typeof(RasterIconSource),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// The colour to paint the image's alpha with; unset draws the image as it is.
	/// </summary>
	/// <remarks>
	/// The image's own colours are discarded and only its transparency survives, so this suits
	/// monochrome artwork - a PNG with an alpha channel. An opaque format such as JPEG becomes a
	/// filled rectangle, which is documented rather than prevented: it is the same rule the
	/// framework's own <c>BitmapIcon.ShowAsMonochrome</c> follows.
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
			typeof(RasterIconSource),
			new FrameworkPropertyMetadata(null));

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
			typeof(RasterIconSource),
			new FrameworkPropertyMetadata(double.NaN));

	/// <summary>Creates the element that draws this icon.</summary>
	/// <returns>A <see cref="RasterIcon"/> bound to this source.</returns>
#if !HAS_CODEBRIX_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		var icon = new RasterIcon();

		IconBinding.Bind(icon, RasterIcon.UriSourceProperty, this, nameof(Source));
		IconBinding.Bind(icon, RasterIcon.DarkUriSourceProperty, this, nameof(Dark));
		IconBinding.Bind(icon, RasterIcon.TintProperty, this, nameof(Tint));
		IconBinding.Bind(icon, RasterIcon.SizeProperty, this, nameof(Size));

		return icon;
	}
}
