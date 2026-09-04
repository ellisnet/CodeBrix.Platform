using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// A bitmap icon: PNG and every other format the platform's image decoder reads, themed, optionally
/// tinted, and picked per display scale.
/// </summary>
/// <remarks>
/// <para>
/// PNG is the format to reach for - it has the alpha channel a tinted icon needs - but nothing here
/// is PNG-specific: JPEG, BMP, GIF, WebP and ICO are decoded by the same platform decoder and work
/// without a line of format-specific code.
/// </para>
/// <para>
/// Where a file named with a scale qualifier sits beside the one named here -
/// <c>open.scale-125.png</c> beside <c>open.png</c> - the variant matching the display is used, the
/// smallest one that is big enough. The artwork may also come from an assembly's embedded resources
/// through this add-in's <see cref="IconResourceScheme">resource scheme</see>, in which case it is
/// written once to a temporary file so the decoder can read it.
/// </para>
/// <para>
/// <see cref="Tint"/> paints the image's ALPHA with one colour, exactly as
/// <c>BitmapIcon.ShowAsMonochrome</c> does - which is what makes a monochrome PNG follow a theme,
/// and what turns an opaque JPEG into a filled rectangle. Leave it unset to draw the image as it
/// is.
/// </para>
/// <para>
/// This element manages its own inherited <c>IconSource</c>; set the properties declared here
/// rather than that one.
/// </para>
/// </remarks>
public partial class RasterIcon : IconSourceElement
{
	private bool _updating;

	/// <summary>Initializes a new raster icon.</summary>
	public RasterIcon()
	{
		ActualThemeChanged += OnActualThemeChanged;
		Loaded += OnLoaded;

		//An INHERITED attached property arrives without anyone setting it here - when the icon is
		//put into a bar, and again whenever the bar changes its mind - so watching the property is
		//the only way to know that EffectiveIconSize has a different answer than it had a moment
		//ago. The scale-qualifier lookup depends on the size as well, so a bar-level change can
		//also mean a different file.
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
			typeof(RasterIcon),
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
			typeof(RasterIcon),
			new PropertyMetadata(null, OnIconPropertyChanged));

	/// <summary>
	/// The colour to paint the image's alpha with; leave it unset to draw the image as it is.
	/// </summary>
	/// <remarks>
	/// Only a <c>SolidColorBrush</c> tints. The image's own colours are discarded - only its
	/// transparency survives - so this is for monochrome artwork; a photograph tinted this way
	/// becomes a coloured silhouette.
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
			typeof(RasterIcon),
			new PropertyMetadata(null, OnIconPropertyChanged));

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
			typeof(RasterIcon),
			new PropertyMetadata(double.NaN, OnIconPropertyChanged));

	/// <summary>The size this icon is currently drawn at, in logical pixels.</summary>
	public double EffectiveIconSize
		=> double.IsNaN(Size) ? ToolBarProperties.GetIconSize(this) : Size;

	/// <summary>The URI the decoder is actually handed, after the theme and the scale have had
	/// their say.</summary>
	/// <remarks>Null when no artwork is set, or when an embedded resource could not be found.</remarks>
	public Uri? ResolvedUriSource { get; private set; }

	/// <summary>Re-reads every input and re-resolves the artwork if anything has changed.</summary>
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
			var artwork = ActualTheme == ElementTheme.Dark && DarkUriSource is not null
				? DarkUriSource
				: UriSource;

			ResolvedUriSource = IconAssetLocator.Resolve(artwork, IconScale.Of(this));

			if (ResolvedUriSource is null)
			{
				IconSource = null!;
				return;
			}

			var source = new BitmapIconSource
			{
				UriSource = ResolvedUriSource,
				ShowAsMonochrome = Tint is SolidColorBrush,
			};

			if (Tint is SolidColorBrush)
			{
				source.Foreground = Tint;
			}

			IconSource = source;

			Width = size;
			Height = size;
		}
		finally
		{
			_updating = false;
		}
	}

	private static void OnIconPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((RasterIcon)sender).UpdateIcon();

	private void OnInheritedIconSizeChanged(DependencyObject sender, DependencyProperty property)
	{
		//An icon that states its own Size ignores the bar, so there is nothing to redo for it.
		if (double.IsNaN(Size))
		{
			UpdateIcon();
		}
	}

	private void OnActualThemeChanged(FrameworkElement sender, object args) => UpdateIcon();

	private void OnLoaded(object sender, RoutedEventArgs e) => UpdateIcon();
}
