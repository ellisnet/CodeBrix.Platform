using System;
using System.IO;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The terse XAML forms - <c>{cb:SvgIcon}</c> and <c>{cb:RasterIcon}</c> - and the embedded-resource
/// scheme they can name.
/// </summary>
public class IconMarkupExtensionTests
{
	[Fact]
	public void SvgIcon_extension_produces_a_source_carrying_everything_it_was_given()
	{
		//Arrange
		var extension = new SvgIconExtension
		{
			Source = "ms-appx:///Assets/open.svg",
			Dark = "ms-appx:///Assets/open-dark.svg",
			Tint = new SolidColorBrush(Color.FromArgb(0xFF, 0x11, 0x22, 0x33)),
			TintMode = IconTintMode.ReplaceBlackAndWhite,
			Size = 18d,
		};

		//Act
		var source = extension.CreateSource();

		//Assert
		source.Source.Should().Be(new Uri("ms-appx:///Assets/open.svg"));
		source.Dark.Should().Be(new Uri("ms-appx:///Assets/open-dark.svg"));
		source.TintMode.Should().Be(IconTintMode.ReplaceBlackAndWhite);
		source.Size.Should().Be(18d);
	}

	[Fact]
	public void a_relative_path_is_read_as_an_application_asset()
	{
		//Arrange
		var extension = new RasterIconExtension { Source = "Assets/open.png" };

		//Act
		var source = extension.CreateSource();

		//Assert
		//The shortest thing worth writing in XAML is a path; ms-appx:/// is where an application's
		//own assets are, so that is what a path means.
		source.Source.Should().Be(new Uri("ms-appx:///Assets/open.png"));
	}

	[Fact]
	public void an_absolute_uri_is_taken_exactly_as_written()
	{
		//Arrange
		var extension = new RasterIconExtension { Source = "file:///tmp/open.png" };

		//Act
		var source = extension.CreateSource();

		//Assert
		source.Source!.IsFile.Should().BeTrue();
	}

	[Fact]
	public void an_unset_source_stays_unset()
	{
		//Arrange
		var extension = new SvgIconExtension();

		//Act
		var source = extension.CreateSource();

		//Assert
		source.Source.Should().BeNull();
		source.Dark.Should().BeNull();
	}

	[Fact]
	public void the_resource_scheme_opens_a_resource_embedded_in_an_assembly()
	{
		//Arrange
		var uri = IconFixtures.ResourceUri(IconFixtures.CurrentColorSvg);

		//Act
		var found = IconResourceScheme.TryOpen(uri, out var stream);

		//Assert
		found.Should().BeTrue();
		using var reader = new StreamReader(stream!);
		reader.ReadToEnd().Should().Contain("currentColor");
	}

	[Fact]
	public void Create_builds_a_uri_that_resolves_back_to_the_same_resource()
	{
		//Arrange
		var assembly = typeof(IconFixtures).Assembly;

		//Act
		var uri = IconResourceScheme.Create(assembly, IconFixtures.AlphaPng);
		var found = IconResourceScheme.TryOpen(uri, out var stream);

		//Assert
		//A library ships its icons this way: one call, no build step in the consuming application.
		IconResourceScheme.IsResourceUri(uri).Should().BeTrue();
		found.Should().BeTrue();
		stream!.Dispose();
	}

	[Fact]
	public void a_resource_uri_naming_an_assembly_that_is_not_here_is_simply_not_found()
	{
		//Arrange
		var uri = new Uri($"{IconResourceScheme.Scheme}://NoSuchAssembly/nothing.svg");

		//Act
		var found = IconResourceScheme.TryOpen(uri, out _);

		//Assert
		//A missing icon must not take the application down with it.
		found.Should().BeFalse();
	}

	[Fact]
	public void an_ordinary_uri_is_not_a_resource_uri()
	{
		//Arrange
		//Act
		//Assert
		IconResourceScheme.IsResourceUri(new Uri("ms-appx:///Assets/open.svg")).Should().BeFalse();
		IconResourceScheme.IsResourceUri(null).Should().BeFalse();
	}
}
