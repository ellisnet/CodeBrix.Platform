using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.TestingPlatform;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.UI.Xaml;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// PreviewCommandLineOptions is the source adapter the package compiles into every
// Microsoft.Testing.Platform consumer (linked into this project by the csproj). It validates
// --headed/--nonheadless/--headless/--theme/--orientation/--screenshotfolder and hands the result
// to PlayTestOptions through AppContext, never through the environment.
public sealed class PreviewCommandLineOptionsTests
{
    [Fact]
    public void GetCommandLineOptions_lists_the_six_playtest_switches()
    {
        //Act
        var names = new PreviewCommandLineOptions().GetCommandLineOptions().Select(o => o.Name).ToArray();

        //Assert
        names.Should().BeEquivalentTo(new[] { "headed", "nonheadless", "headless", "theme", "orientation", "screenshotfolder" });
    }

    [Fact]
    public void AppContext_keys_match_the_names_the_runtime_reads()
    {
        //Assert
        PreviewCommandLineOptions.HeadlessSetting.Should().Be(PlayTestSettingsScope.HeadlessKey);
        PreviewCommandLineOptions.ThemeSetting.Should().Be(PlayTestSettingsScope.ThemeKey);
        PreviewCommandLineOptions.OrientationSetting.Should().Be(PlayTestSettingsScope.OrientationKey);
        PreviewCommandLineOptions.ScreenshotFolderSetting.Should().Be(PlayTestSettingsScope.ScreenshotFolderKey);
    }

    [Theory]
    [InlineData(null, null, true, 0f)]
    [InlineData(null, "0", true, 0f)]
    [InlineData(null, "1", false, 250f)]
    [InlineData("headed", "0", false, 250f)]
    [InlineData("headed", "1", false, 250f)]
    [InlineData("nonheadless", "0", false, 250f)]
    [InlineData("headless", "1", true, 0f)]
    [InlineData("headless", "0", true, 0f)]
    public async Task ValidateCommandLineOptionsAsync_overrides_environment_and_sets_the_default_delay(
        string? option, string? environment, bool headless, float delay)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, environment);

        //Act
        var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(Switches(option));
        var options = new PlayTestOptions();

        //Assert
        result.IsValid.Should().BeTrue();
        options.Headless.Should().Be(headless);
        options.SlowMo.Should().Be(delay);
        Environment.GetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable).Should().Be(environment);
    }

    [Theory]
    [InlineData("headed", true)]
    [InlineData("headless", false)]
    public async Task ValidateCommandLineOptionsAsync_leaves_an_explicit_fixture_option_in_charge(string option, bool headless)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();

        //Act
        await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(Switches(option));
        var options = new PlayTestOptions { Headless = headless };

        //Assert
        options.Headless.Should().Be(headless);
        options.SlowMo.Should().Be(headless ? 0f : 250f);
    }

    [Theory]
    [InlineData("headed")]
    [InlineData("nonheadless")]
    public async Task ValidateCommandLineOptionsAsync_rejects_headed_with_headless(string headed)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();

        //Act
        var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(Switches(headed, "headless"));

        //Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cannot combine");
        AppContext.GetData(PreviewCommandLineOptions.HeadlessSetting).Should().BeNull();
    }

    [Theory]
    [InlineData("DaRk", "PoRtRaIt", ApplicationTheme.Dark, ScreenOrientation.Portrait)]
    [InlineData("LiGhT", "LaNdScApE", ApplicationTheme.Light, ScreenOrientation.Landscape)]
    public async Task ValidateCommandLineOptionsAsync_overrides_environment_and_project_but_code_still_wins(
        string theme, string orientation, ApplicationTheme expectedTheme, ScreenOrientation expectedOrientation)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.ThemeVariable, "invalid environment");
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.OrientationVariable, "invalid environment");
        var project = InvalidProjectPreferences();

        //Act
        var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(
            Values(("theme", theme), ("orientation", orientation)));
        var options = new PlayTestOptions { ConfigurationAssembly = project };
        var resolvedTheme = options.ResolveTheme();
        var resolvedOrientation = options.ResolveOrientation();
        options.Theme = expectedTheme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
        options.Orientation = expectedOrientation == ScreenOrientation.Portrait ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;

        //Assert
        result.IsValid.Should().BeTrue();
        resolvedTheme.Should().Be(expectedTheme);
        resolvedOrientation.Should().Be(expectedOrientation);
        options.ResolveTheme().Should().Be(options.Theme);
        options.ResolveOrientation().Should().Be(options.Orientation);
        Environment.GetEnvironmentVariable(PlayTestSettingsScope.ThemeVariable).Should().Be("invalid environment");
        Environment.GetEnvironmentVariable(PlayTestSettingsScope.OrientationVariable).Should().Be("invalid environment");
    }

    [Fact]
    public async Task ValidateCommandLineOptionsAsync_stores_normalized_values()
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();

        //Act
        var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(
            Values(("theme", "DARK"), ("orientation", "Portrait")));

        //Assert
        result.IsValid.Should().BeTrue();
        AppContext.GetData(PreviewCommandLineOptions.ThemeSetting).Should().Be("dark");
        AppContext.GetData(PreviewCommandLineOptions.OrientationSetting).Should().Be("portrait");
        AppContext.GetData(PreviewCommandLineOptions.HeadlessSetting).Should().BeNull();
        AppContext.GetData(PreviewCommandLineOptions.ScreenshotFolderSetting).Should().BeNull();
    }

    [Theory]
    [InlineData("theme", "system")]
    [InlineData("theme", "1")]
    [InlineData("theme", " dark ")]
    [InlineData("theme", "")]
    [InlineData("orientation", "sideways")]
    [InlineData("orientation", "0")]
    [InlineData("orientation", "")]
    public async Task ValidateCommandLineOptionsAsync_rejects_invalid_values_before_app_startup(string option, string value)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();

        //Act
        var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(Values((option, value)));

        //Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("--" + option);
    }

    [Theory]
    [InlineData("theme", "Dark", true)]
    [InlineData("theme", "sepia", false)]
    [InlineData("orientation", "PORTRAIT", true)]
    [InlineData("orientation", "upside-down", false)]
    public async Task ValidateOptionArgumentsAsync_checks_each_value(string option, string value, bool valid)
    {
        //Arrange
        var provider = new PreviewCommandLineOptions();
        var definition = provider.GetCommandLineOptions().Single(o => o.Name == option);

        //Act
        var result = await provider.ValidateOptionArgumentsAsync(definition, new[] { value });

        //Assert
        result.IsValid.Should().Be(valid);
    }

    [Fact]
    public async Task ValidateOptionArgumentsAsync_rejects_a_screenshot_folder_that_is_not_empty()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "PlayTestFolder_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var provider = new PreviewCommandLineOptions();
        var definition = provider.GetCommandLineOptions().Single(o => o.Name == "screenshotfolder");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "keep.txt"), "keep this", TestContext.Current.CancellationToken);

            //Act
            var result = await provider.ValidateOptionArgumentsAsync(definition, new[] { directory });

            //Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("must be empty");
            File.Exists(Path.Combine(directory, "keep.txt")).Should().BeTrue();
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static Assembly InvalidProjectPreferences()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Preferences_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var constructor = typeof(AssemblyMetadataAttribute).GetConstructor(new[] { typeof(string), typeof(string) })
            ?? throw new InvalidOperationException("AssemblyMetadataAttribute(string, string) is missing.");
        foreach (var key in new[] { "CodeBrixPlayTestPreferredTheme", "CodeBrixPlayTestPreferredOrientation" })
            assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { key, "invalid project" }));
        return assembly;
    }

    private static FakeCommandLineOptions Switches(params string?[] switches) =>
        new(switches.OfType<string>().ToDictionary(s => s, _ => Array.Empty<string>()));

    private static FakeCommandLineOptions Values(params (string Name, string Value)[] values) =>
        new(values.ToDictionary(v => v.Name, v => new[] { v.Value }));

    private sealed class FakeCommandLineOptions(Dictionary<string, string[]> options) : ICommandLineOptions
    {
        public bool IsOptionSet(string optionName) => options.ContainsKey(optionName);

        public bool TryGetOptionArgumentList(string optionName, [NotNullWhen(true)] out string[]? arguments)
            => options.TryGetValue(optionName, out arguments);
    }
}
