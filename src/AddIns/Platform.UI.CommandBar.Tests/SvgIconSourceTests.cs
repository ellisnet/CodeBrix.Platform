using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The SVG icon: which artwork the theme chooses, what the tint composes to, what size the display
/// scale asks for, and that the source and its element stay in step.
/// </summary>
/// <remarks>
/// The rendering itself - that the stylesheet reaches the parser and comes out as coloured pixels -
/// is measured in <see cref="SvgProviderCssTests"/>, against the platform's own SVG route.
/// </remarks>
public class SvgIconSourceTests
{
	private static readonly Color Red = Color.FromArgb(0xFF, 0xFF, 0x00, 0x00);

	private static readonly Uri LightArtwork = new("ms-appx:///Assets/open.svg");
	private static readonly Uri DarkArtwork = new("ms-appx:///Assets/open-dark.svg");

	[Fact]
	public void CreateIconElement_makes_an_SvgIcon_carrying_the_sources_values()
	{
		//Arrange
		var source = new SvgIconSource
		{
			Source = LightArtwork,
			Dark = DarkArtwork,
			Tint = new SolidColorBrush(Red),
			TintMode = IconTintMode.ReplaceBlackAndWhite,
			Size = 20d,
		};

		//Act
		var element = source.CreateIconElement();

		//Assert
		var icon = element.Should().BeOfType<SvgIcon>().Subject;
		icon.UriSource.Should().Be(LightArtwork);
		icon.DarkUriSource.Should().Be(DarkArtwork);
		icon.TintMode.Should().Be(IconTintMode.ReplaceBlackAndWhite);
		icon.Size.Should().Be(20d);
	}

	[Fact]
	public void CreateIconElement_binds_so_a_later_change_on_the_source_reaches_the_element()
	{
		//Arrange
		var source = new SvgIconSource { Source = LightArtwork };
		var icon = (SvgIcon)source.CreateIconElement();

		//Act
		//One source can drive several buttons, so a change must reach every element it made -
		//that is why the element is bound to the source rather than copied from it.
		source.Size = 48d;

		//Assert
		icon.Size.Should().Be(48d);
	}

	[Fact]
	public void UriSource_is_used_in_the_light_theme()
	{
		//Arrange
		var icon = new SvgIcon { UriSource = LightArtwork, DarkUriSource = DarkArtwork };

		//Act
		icon.RequestedTheme = ElementTheme.Light;

		//Assert
		icon.ActualTheme.Should().Be(ElementTheme.Light);
		icon.ResolvedUriSource.Should().Be(LightArtwork);
		icon.LastKey.Theme.Should().Be(ElementTheme.Light);
	}

	[Fact]
	public void DarkUriSource_replaces_the_artwork_in_the_dark_theme()
	{
		//Arrange
		var icon = new SvgIcon { UriSource = LightArtwork, DarkUriSource = DarkArtwork };
		icon.RequestedTheme = ElementTheme.Light;
		var beforeTheme = icon.Source;

		//Act
		icon.RequestedTheme = ElementTheme.Dark;

		//Assert
		icon.ResolvedUriSource.Should().Be(DarkArtwork);
		icon.LastKey.Theme.Should().Be(ElementTheme.Dark);
		//A different rendering, not the same object recoloured: light and dark are separate cache
		//entries, which is what lets an icon swap back instantly.
		icon.Source.Should().NotBeSameAs(beforeTheme);
	}

	[Fact]
	public void UriSource_is_kept_in_the_dark_theme_when_no_dark_artwork_is_given()
	{
		//Arrange
		var icon = new SvgIcon { UriSource = LightArtwork };

		//Act
		icon.RequestedTheme = ElementTheme.Dark;

		//Assert
		icon.ResolvedUriSource.Should().Be(LightArtwork);
	}

	[Fact]
	public void EffectiveIconSize_follows_the_bar_until_the_icon_sets_its_own()
	{
		//Arrange
		var group = new ToolBarGroup();
		ToolBarProperties.SetIconSize(group, 40d);
		var icon = new SvgIcon { UriSource = LightArtwork };
		group.Children.Add(icon);

		//Act
		var inherited = icon.EffectiveIconSize;
		icon.Size = 18d;
		var own = icon.EffectiveIconSize;

		//Assert
		//Also the fence for a framework trap found while building this stream: a dependency property
		//whose NAME matches an inherited attached property shadows it on the declaring type. An
		//earlier draft called this element's own property IconSize, and the element stopped seeing
		//the bar's ToolBarProperties.IconSize entirely - 24 instead of 40, with nothing to say why.
		//Hence Size. Any type in this add-in that wants a per-item override of an inherited
		//attached property has to avoid its name in the same way.
		inherited.Should().Be(40d);
		own.Should().Be(18d);
	}

	[Fact]
	public void TintCss_is_null_when_no_tint_is_set()
	{
		//Arrange
		var icon = new SvgIcon { UriSource = LightArtwork };

		//Act
		var css = icon.TintCss;

		//Assert
		//Unset means "draw the file as it was drawn"; a stylesheet is only composed when asked for.
		css.Should().BeNull();
	}

	[Fact]
	public void TintCss_recolours_currentColor_only_by_default()
	{
		//Arrange
		var icon = new SvgIcon
		{
			UriSource = LightArtwork,
			Tint = new SolidColorBrush(Color.FromArgb(0xFF, 0x22, 0x66, 0xDD)),
		};

		//Act
		var css = icon.TintCss;

		//Assert
		css.Should().Be("* { color: #2266DD; }");
	}

	[Fact]
	public void TintCss_also_replaces_black_and_white_in_the_replace_mode()
	{
		//Arrange
		var icon = new SvgIcon
		{
			UriSource = LightArtwork,
			Tint = new SolidColorBrush(Color.FromArgb(0xFF, 0x22, 0x66, 0xDD)),
			TintMode = IconTintMode.ReplaceBlackAndWhite,
		};

		//Act
		var css = icon.TintCss!;

		//Assert
		css.Should().StartWith("* { color: #2266DD; }");
		css.Should().Contain("[fill=\"#000000\"]");
		css.Should().Contain("[fill=\"black\"]");
		css.Should().Contain("[stroke=\"#ffffff\"]");
		css.Should().Contain("{ fill: #2266DD; }");
		css.Should().Contain("{ stroke: #2266DD; }");
	}

	[Fact]
	public void TintCss_is_null_when_the_mode_is_None_even_with_a_tint()
	{
		//Arrange
		var icon = new SvgIcon
		{
			UriSource = LightArtwork,
			Tint = new SolidColorBrush(Red),
			TintMode = IconTintMode.None,
		};

		//Act
		var css = icon.TintCss;

		//Assert
		css.Should().BeNull();
	}

	[Fact]
	public void a_tint_change_re_renders_rather_than_recolouring_in_place()
	{
		//Arrange
		var icon = new SvgIcon { UriSource = LightArtwork };
		var untinted = icon.Source;

		//Act
		icon.Tint = new SolidColorBrush(Red);

		//Assert
		//The tint is applied when the document is PARSED, so a new tint is a new parse - and the
		//key says so, which is what keeps the two renderings apart in the cache.
		icon.LastKey.Tint.Should().Be("* { color: #FF0000; }");
		icon.Source.Should().NotBeSameAs(untinted);
	}

	[Fact]
	public void Markup_is_drawn_instead_of_the_artwork_when_it_is_set()
	{
		//Arrange
		var icon = new SvgIcon { UriSource = LightArtwork };

		//Act
		icon.Markup = IconFixtures.Text(IconFixtures.CurrentColorSvg);

		//Assert
		icon.ResolvedUriSource.Should().BeNull();
		icon.LastKey.Source.Should().StartWith("markup:");
		icon.Source.Should().NotBeNull();
	}

	[Collection(DisplayScaleCollection.Name)]
	public class ScaleAware
	{
		[Fact]
		public void the_render_is_keyed_on_the_display_scale()
		{
			//Arrange
			IconRasterCache.Clear();
			double atOne = 0;
			double atTwo = 0;

			//Act
			DisplayScale.At(1.0, () => atOne = new SvgIcon { UriSource = LightArtwork }.LastKey.Scale);
			DisplayScale.At(2.0, () => atTwo = new SvgIcon { UriSource = LightArtwork }.LastKey.Scale);

			//Assert
			//Host-free there is no XamlRoot, so the scale comes from the display; on a head it is
			//the XamlRoot's rasterization scale, which is the same number for a single window.
			atOne.Should().Be(1.0);
			atTwo.Should().Be(2.0);
		}

		[Fact]
		public void two_display_scales_are_two_cache_entries()
		{
			//Arrange
			IconRasterCache.Clear();
			object? atOne = null;
			object? atTwo = null;

			//Act
			DisplayScale.At(1.0, () => atOne = new SvgIcon { UriSource = LightArtwork }.Source);
			DisplayScale.At(2.0, () => atTwo = new SvgIcon { UriSource = LightArtwork }.Source);

			//Assert
			//Which is what makes a window dragged to a 200% display re-rasterise rather than
			//stretch the bitmap it already had.
			atOne.Should().NotBeNull();
			atTwo.Should().NotBeNull();
			atOne.Should().NotBeSameAs(atTwo);
		}
	}
}
