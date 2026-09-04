using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The icon cache: one rendering per distinct look, shared by everything that shows it.
/// </summary>
/// <remarks>
/// Parsing and rasterising an SVG is the expensive part of showing an icon, and a tool bar shows
/// the same icon in several places and re-renders the lot on a theme change. These tests fence what
/// counts as "the same look" - the five things in the key - because getting that wrong either
/// wastes the cache or, worse, shows a light icon in a dark theme.
/// </remarks>
public class IconRasterCacheTests
{
	private static readonly Uri Artwork = new("ms-appx:///Assets/cache-probe.svg");

	private static IconCacheKey Key(
		string source = "a",
		ElementTheme theme = ElementTheme.Light,
		double size = 24d,
		double scale = 1d,
		string tint = "")
		=> new(source, theme, size, scale, tint);

	[Fact]
	public void the_same_key_renders_once_and_is_answered_from_the_cache_after_that()
	{
		//Arrange
		IconRasterCache.Clear();
		var renders = 0;
		SvgImageSource Factory()
		{
			renders++;
			return new SvgImageSource();
		}

		//Act
		var first = IconRasterCache.GetOrCreate(Key(), Factory);
		var second = IconRasterCache.GetOrCreate(Key(), Factory);

		//Assert
		renders.Should().Be(1);
		second.Should().BeSameAs(first);
		IconRasterCache.Hits.Should().Be(1);
		IconRasterCache.Misses.Should().Be(1);
	}

	[Theory]
	[InlineData("source")]
	[InlineData("theme")]
	[InlineData("size")]
	[InlineData("scale")]
	[InlineData("tint")]
	public void each_part_of_the_key_on_its_own_is_a_different_rendering(string part)
	{
		//Arrange
		IconRasterCache.Clear();
		var baseline = Key();
		var changed = part switch
		{
			"source" => Key(source: "b"),
			"theme" => Key(theme: ElementTheme.Dark),
			"size" => Key(size: 32d),
			"scale" => Key(scale: 2d),
			_ => Key(tint: "* { color: #FF0000; }"),
		};

		//Act
		var first = IconRasterCache.GetOrCreate(baseline, () => new SvgImageSource());
		var second = IconRasterCache.GetOrCreate(changed, () => new SvgImageSource());

		//Assert
		second.Should().NotBeSameAs(first);
		IconRasterCache.Misses.Should().Be(2);
	}

	[Fact]
	public void Clear_empties_the_cache()
	{
		//Arrange
		IconRasterCache.Clear();
		var before = IconRasterCache.GetOrCreate(Key(), () => new SvgImageSource());

		//Act
		IconRasterCache.Clear();
		var after = IconRasterCache.GetOrCreate(Key(), () => new SvgImageSource());

		//Assert
		after.Should().NotBeSameAs(before);
		IconRasterCache.Count.Should().Be(1);
	}

	[Fact]
	public void two_icons_showing_the_same_artwork_share_one_rendering()
	{
		//Arrange
		IconRasterCache.Clear();

		//Act
		var first = new SvgIcon { UriSource = Artwork };
		var second = new SvgIcon { UriSource = Artwork };

		//Assert
		//The case the cache exists for: a bar with the same icon on several buttons parses and
		//rasterises it once.
		second.Source.Should().BeSameAs(first.Source);
		IconRasterCache.Hits.Should().Be(1);
	}

	[Fact]
	public void a_key_writes_itself_out_for_a_failure_message()
	{
		//Arrange
		var key = Key(source: "ms-appx:///a.svg", theme: ElementTheme.Dark, size: 24, scale: 1.25, tint: "x");

		//Act
		var text = key.ToString();

		//Assert
		text.Should().Be("ms-appx:///a.svg|Dark|24|1.25|x");
	}
}
