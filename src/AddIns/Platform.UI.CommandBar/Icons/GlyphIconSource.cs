using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// An icon drawn from a SYMBOL FONT: one glyph, in whatever font the application ships.
/// </summary>
/// <remarks>
/// <para>
/// The cheapest icon there is - no file to load, no bitmap to rasterise, and it scales and themes
/// like text because it IS text. It is here so a tool bar can mix a font icon set with SVG and
/// raster icons without the application having to reach for a second vocabulary; the drawing is the
/// framework's <c>FontIcon</c>, which needs nothing from this add-in.
/// </para>
/// <para>
/// The glyph is normally written as an escaped code point, for example <c>&amp;#xE8A5;</c> in XAML.
/// Leave <see cref="FontFamily"/> unset to use the application's symbol font.
/// </para>
/// </remarks>
public partial class GlyphIconSource : ToolIconSource
{
	/// <summary>Initializes a new glyph icon source.</summary>
	public GlyphIconSource()
	{
	}

	/// <summary>The character to draw.</summary>
	public string? Glyph
	{
		get => (string?)GetValue(GlyphProperty);
		set => SetValue(GlyphProperty, value);
	}

	/// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
	public static DependencyProperty GlyphProperty { get; } =
		DependencyProperty.Register(
			nameof(Glyph),
			typeof(string),
			typeof(GlyphIconSource),
			new FrameworkPropertyMetadata(null));

	/// <summary>The font the glyph is drawn from; unset uses the application's symbol font.</summary>
	public FontFamily? FontFamily
	{
		get => (FontFamily?)GetValue(FontFamilyProperty);
		set => SetValue(FontFamilyProperty, value);
	}

	/// <summary>Identifies the <see cref="FontFamily"/> dependency property.</summary>
	public static DependencyProperty FontFamilyProperty { get; } =
		DependencyProperty.Register(
			nameof(FontFamily),
			typeof(FontFamily),
			typeof(GlyphIconSource),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// The glyph's font size in logical pixels; NaN, the default, uses the icon size the bar sets
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
			typeof(GlyphIconSource),
			new FrameworkPropertyMetadata(double.NaN));

	/// <summary>Creates the element that draws this icon.</summary>
	/// <returns>A <c>FontIcon</c> bound to this source.</returns>
#if !HAS_CODEBRIX_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		var icon = new FontIcon();

		IconBinding.Bind(icon, FontIcon.GlyphProperty, this, nameof(Glyph));

		if (FontFamily is not null)
		{
			IconBinding.Bind(icon, FontIcon.FontFamilyProperty, this, nameof(FontFamily));
		}

		if (!double.IsNaN(Size))
		{
			IconBinding.Bind(icon, FontIcon.FontSizeProperty, this, nameof(Size));
		}
		else
		{
			//No size of its own: follow the bar, the same inherited value the SVG and raster icons
			//read, so a bar of mixed icon kinds is one size. Read when the glyph enters the tree,
			//because that is when an inherited attached value is there to be read.
			icon.Loaded += (_, _) => icon.FontSize = ToolBarProperties.GetIconSize(icon);
		}

		return icon;
	}
}
