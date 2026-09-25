#nullable enable

using System;
using System.IO;
using CodeBrix.Platform.UI.CommandBar;
using SilverAssertions;
using Xunit;
using EngineIconResourceScheme = CodeBrix.Platform.UI.CommandBar.Engine.IconResourceScheme;
using EngineIconUri = CodeBrix.Platform.UI.CommandBar.Engine.IconUri;
using EngineSvgTintCss = CodeBrix.Platform.UI.CommandBar.Engine.SvgTintCss;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The CommandBar icon helpers (WPE1 C14): the cb-res:// embedded-resource scheme, the icon URI reading and the SVG tint
/// stylesheet run from the add-in's Engine namespace with no XAML - so a CodeBrix.Mobile icon resolves and tints artwork
/// exactly as the tool bar does - and no WinUI assembly loads.
/// </summary>
public class CommandBarEngineTests
{
	[Fact]
	public void When_Icon_Resources_Are_Named_Opened_And_Tinted_Then_The_Helpers_Agree_With_The_Tool_Bar_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var assembly = typeof(CommandBarEngineTests).Assembly;

		//Act
		var uri = EngineIconResourceScheme.Create(assembly, "themed.json"); // a suffix of the manifest name
		var opened = EngineIconResourceScheme.TryOpen(uri, out var stream);
		using var _ = stream;
		var publicOpened = IconResourceScheme.TryOpen(uri, out var publicStream); // the public facade forwards
		using var __ = publicStream;
		var missing = EngineIconResourceScheme.TryOpen(new Uri($"{IconResourceScheme.Scheme}://NoSuchAssembly/x.svg"), out var none);
		var relative = EngineIconUri.Parse("Assets/open.svg");
		var absolute = EngineIconUri.Parse("https://example.com/open.svg");
		var blank = EngineIconUri.Parse("  ");
		var currentColourOnly = EngineSvgTintCss.Compose(0x22, 0x66, 0xDD, IconTintMode.CurrentColorOnly);
		var replace = EngineSvgTintCss.Compose(0x22, 0x66, 0xDD, IconTintMode.ReplaceBlackAndWhite);
		var noTint = EngineSvgTintCss.Compose(0x22, 0x66, 0xDD, IconTintMode.None);

		//Assert
		uri.Scheme.Should().Be("cb-res");
		uri.Host.Should().Be(assembly.GetName().Name!.ToLowerInvariant());
		opened.Should().BeTrue();
		stream!.Length.Should().BeGreaterThan(100);
		publicOpened.Should().BeTrue();
		missing.Should().BeFalse();
		none.Should().BeNull();
		EngineIconResourceScheme.IsResourceUri(new Uri("ms-appx:///Assets/open.svg")).Should().BeFalse();
		relative.Should().Be(new Uri("ms-appx:///Assets/open.svg"));
		absolute.Should().Be(new Uri("https://example.com/open.svg"));
		blank.Should().BeNull();
		currentColourOnly.Should().Be("* { color: #2266DD; }");
		replace.Should().StartWith("* { color: #2266DD; } [fill=\"#000000\"],[fill=\"#000\"]");
		replace.Should().EndWith("{ stroke: #2266DD; }");
		noTint.Should().BeNull();

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.CommandBar.Core");
		EngineIsolation.AssertNoWinUILoaded("CommandBar (icon helpers)");
	}
}
