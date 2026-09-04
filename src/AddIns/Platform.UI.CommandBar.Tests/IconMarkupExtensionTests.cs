using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.UI.CommandBar.Tests;

/// <summary>
/// The terse XAML forms - <c>{cb:SvgIconSource}</c> and <c>{cb:RasterIconSource}</c> - the names the
/// XAML generator resolves them under, and the embedded-resource scheme they can name.
/// </summary>
public class IconMarkupExtensionTests
{
	private const string IconNamespace = "CodeBrix.Platform.UI.CommandBar";

	[Fact]
	public void SvgIconSource_extension_produces_a_source_carrying_everything_it_was_given()
	{
		//Arrange
		var extension = new SvgIconSourceExtension
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
		var extension = new RasterIconSourceExtension { Source = "Assets/open.png" };

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
		var extension = new RasterIconSourceExtension { Source = "file:///tmp/open.png" };

		//Act
		var source = extension.CreateSource();

		//Assert
		source.Source!.IsFile.Should().BeTrue();
	}

	[Fact]
	public void an_unset_source_stays_unset()
	{
		//Arrange
		var extension = new SvgIconSourceExtension();

		//Act
		var source = extension.CreateSource();

		//Assert
		source.Source.Should().BeNull();
		source.Dark.Should().BeNull();
	}

	[Fact]
	public void the_element_names_are_free_for_the_icon_ELEMENTS()
	{
		//Arrange
		//Act
		//Assert
		//The XAML generator asks for "<name>Extension" BEFORE it asks for "<name>", in either
		//syntax, so a markup extension named after an ELEMENT would answer for the element form
		//as well as the curly-brace form - and hand an AppBarButton.Icon an icon SOURCE, which is
		//not an IconElement. Nothing may answer to these two names but the elements themselves.
		MarkupExtensionFor("SvgIcon").Should().BeNull();
		MarkupExtensionFor("RasterIcon").Should().BeNull();
		ElementFor("SvgIcon").Should().Be(typeof(SvgIcon));
		ElementFor("RasterIcon").Should().Be(typeof(RasterIcon));
		typeof(IconElement).IsAssignableFrom(typeof(SvgIcon)).Should().BeTrue();
		typeof(IconElement).IsAssignableFrom(typeof(RasterIcon)).Should().BeTrue();
	}

	[Fact]
	public void the_markup_names_are_the_SOURCE_names()
	{
		//Arrange
		//Act
		var svg = MarkupExtensionFor("SvgIconSource");
		var raster = MarkupExtensionFor("RasterIconSource");

		//Assert
		//{cb:SvgIconSource ...} and {cb:RasterIconSource ...} are what a page writes, and each
		//says in its own name what it hands back.
		svg.Should().Be(typeof(SvgIconSourceExtension));
		raster.Should().Be(typeof(RasterIconSourceExtension));
		ReturnTypeOf(svg!).Should().Be(typeof(SvgIconSource));
		ReturnTypeOf(raster!).Should().Be(typeof(RasterIconSource));
	}

	[Fact]
	public void an_extension_given_nothing_produces_a_source_with_the_documented_defaults()
	{
		//Arrange
		var svg = new SvgIconSourceExtension();
		var raster = new RasterIconSourceExtension();

		//Act
		var svgSource = svg.CreateSource();
		var rasterSource = raster.CreateSource();

		//Assert
		//The rename moved the NAME and nothing else: an unwritten property still means what it
		//meant before - no artwork, no tint, currentColor tinting, and a size the bar decides.
		svgSource.Markup.Should().BeNull();
		svgSource.Tint.Should().BeNull();
		svgSource.TintMode.Should().Be(IconTintMode.CurrentColorOnly);
		double.IsNaN(svgSource.Size).Should().BeTrue();
		rasterSource.Tint.Should().BeNull();
		double.IsNaN(rasterSource.Size).Should().BeTrue();
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

	/// <summary>
	/// The markup-extension type the XAML generator would resolve a prefixed XAML name to, by the
	/// generator's own rule: the "Extension"-suffixed name first, then the bare name, and only a
	/// type that derives from <see cref="MarkupExtension"/> counts. Modelling the rule here is
	/// what makes this suite able to hold the line host-free - running the generator itself needs
	/// a Roslyn compilation of a XAML file, which is not something the add-in's suite does.
	/// </summary>
	/// <param name="xamlName">The name as it is written in XAML after the prefix.</param>
	/// <returns>The extension type, or null when no markup extension answers to that name.</returns>
	private static Type? MarkupExtensionFor(string xamlName)
		=> ExtensionNamed(xamlName + "Extension") ?? ExtensionNamed(xamlName);

	private static Type? ExtensionNamed(string typeName)
		=> typeof(SvgIcon).Assembly.GetType($"{IconNamespace}.{typeName}") is { } type
			&& typeof(MarkupExtension).IsAssignableFrom(type)
				? type
				: null;

	private static Type? ElementFor(string xamlName)
		=> typeof(SvgIcon).Assembly.GetType($"{IconNamespace}.{xamlName}");

	private static Type? ReturnTypeOf(Type extension)
		=> extension.GetCustomAttribute<MarkupExtensionReturnTypeAttribute>()?.ReturnType;
}
