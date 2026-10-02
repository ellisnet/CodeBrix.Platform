using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.UI.Xaml;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// Every preference resolves in the same order: explicit code value > command line (the AppContext
// key the MTP adapter writes) > environment variable > project metadata on ConfigurationAssembly >
// built-in default. SlowMo has no command-line or project form: code > environment > the
// headed/headless default.
public sealed class PlayTestOptionsTests
{
    private const string OrientationMetadata = "CodeBrixPlayTestPreferredOrientation";
    private const string ThemeMetadata = "CodeBrixPlayTestPreferredTheme";

    [Fact]
    public void Headless_defaults_to_true()
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();

        //Act
        var options = new PlayTestOptions();

        //Assert
        options.Headless.Should().BeTrue();
    }

    [Theory]
    [InlineData("1", false)]
    [InlineData("0", true)]
    [InlineData("", true)]
    [InlineData("true", true)]
    public void Headless_follows_only_the_value_1_of_the_environment_variable(string headed, bool expected)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, headed);

        //Act
        var options = new PlayTestOptions();

        //Assert
        options.Headless.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "1", true)]
    [InlineData(false, "0", false)]
    [InlineData(false, null, false)]
    public void Headless_command_line_overrides_the_environment(bool commandLineHeadless, string? headed, bool expected)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.HeadlessKey, commandLineHeadless);
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, headed);

        //Act
        var options = new PlayTestOptions();

        //Assert
        options.Headless.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Headless_explicit_code_value_wins(bool headless)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.HeadlessKey, !headless);
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, headless ? "1" : "0");

        //Act
        var options = new PlayTestOptions { Headless = headless };

        //Assert
        options.Headless.Should().Be(headless);
    }

    [Theory]
    [InlineData(null, null, 0f)]
    [InlineData("0", null, 0f)]
    [InlineData("1", null, 250f)]
    [InlineData("1", "", 250f)]
    [InlineData("1", "0", 0f)]
    [InlineData("1", "100", 100f)]
    [InlineData("1", "750", 750f)]
    [InlineData("0", "125", 125f)]
    public void SlowMo_environment_overrides_the_headed_or_headless_default(string? headed, string? slowMo, float expected)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, headed);
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.SlowMoVariable, slowMo);

        //Act
        var delay = new PlayTestOptions().SlowMo;

        //Assert
        delay.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, 0f)]
    [InlineData(false, 250f)]
    public void SlowMo_default_uses_the_final_headless_option(bool headless, float expected)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, headless ? "1" : "0");

        //Act
        var options = new PlayTestOptions { Headless = headless };

        //Assert
        options.SlowMo.Should().Be(expected);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(50f)]
    [InlineData(500f)]
    public void SlowMo_explicit_code_value_wins_including_zero(float delay)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, "1");
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.SlowMoVariable, "750");

        //Act
        var options = new PlayTestOptions { SlowMo = delay, Headless = false };
        var headed = options.SlowMo;
        options.Headless = true;
        var headless = options.SlowMo;

        //Assert
        headed.Should().Be(delay);
        headless.Should().Be(delay);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("fast")]
    [InlineData(" ")]
    public void SlowMo_invalid_environment_delay_identifies_the_setting(string delay)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, "1");
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.SlowMoVariable, delay);

        //Act
        Action resolve = () => _ = new PlayTestOptions().SlowMo;

        //Assert
        resolve.Should().Throw<ArgumentException>().WithMessage("*CODEBRIX_PLAYTEST_SLOWMO*");
    }

    [Fact]
    public void SlowMo_fractional_milliseconds_use_invariant_culture()
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.HeadedVariable, "1");
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.SlowMoVariable, "12.5");
        var previous = CultureInfo.CurrentCulture;
        float delay;

        //Act
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            delay = new PlayTestOptions().SlowMo;
        }
        finally { CultureInfo.CurrentCulture = previous; }

        //Assert
        delay.Should().Be(12.5f);
    }

    [Fact]
    public void ResolveOrientation_defaults_to_landscape()
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        var options = new PlayTestOptions { ConfigurationAssembly = WithMetadata() };

        //Act
        var orientation = options.ResolveOrientation();

        //Assert
        orientation.Should().Be(ScreenOrientation.Landscape);
        options.Orientation.Should().Be(ScreenOrientation.Landscape);
    }

    [Theory]
    [InlineData(null, null, "Portrait", ScreenOrientation.Portrait)]
    [InlineData(null, "LANDSCAPE", "Portrait", ScreenOrientation.Landscape)]
    [InlineData(null, "", "Portrait", ScreenOrientation.Portrait)]
    [InlineData("portrait", "landscape", "Landscape", ScreenOrientation.Portrait)]
    [InlineData("landscape", "portrait", "Portrait", ScreenOrientation.Landscape)]
    public void ResolveOrientation_prefers_command_line_then_environment_then_project(
        string? commandLine, string? environment, string project, ScreenOrientation expected)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.OrientationKey, commandLine);
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.OrientationVariable, environment);
        var options = new PlayTestOptions { ConfigurationAssembly = WithMetadata((OrientationMetadata, project)) };

        //Act
        var orientation = options.ResolveOrientation();

        //Assert
        orientation.Should().Be(expected);
    }

    [Theory]
    [InlineData(ScreenOrientation.Landscape)]
    [InlineData(ScreenOrientation.Portrait)]
    public void ResolveOrientation_explicit_code_value_wins(ScreenOrientation explicitOrientation)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.OrientationKey, "invalid command line");
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.OrientationVariable, "invalid environment");
        var options = new PlayTestOptions
        {
            ConfigurationAssembly = WithMetadata((OrientationMetadata, "invalid project")),
            Orientation = explicitOrientation,
        };

        //Act
        var orientation = options.ResolveOrientation();

        //Assert
        orientation.Should().Be(explicitOrientation);
    }

    [Theory]
    [InlineData("sideways", null, null, "--orientation")]
    [InlineData(null, "sideways", null, "CODEBRIX_PLAYTEST_ORIENTATION")]
    [InlineData(null, null, "sideways", OrientationMetadata)]
    public void ResolveOrientation_invalid_selected_value_names_its_source(
        string? commandLine, string? environment, string? project, string source)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.OrientationKey, commandLine);
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.OrientationVariable, environment);
        var options = new PlayTestOptions
        {
            ConfigurationAssembly = project == null ? WithMetadata() : WithMetadata((OrientationMetadata, project)),
        };

        //Act
        Action resolve = () => options.ResolveOrientation();

        //Assert
        resolve.Should().Throw<ArgumentException>().WithMessage($"*{source}*sideways*");
    }

    [Fact]
    public void ResolveOrientation_rejects_an_undefined_explicit_value()
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        var options = new PlayTestOptions { Orientation = (ScreenOrientation)7 };

        //Act
        Action resolve = () => options.ResolveOrientation();

        //Assert
        resolve.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ResolveTheme_defaults_to_light()
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        var options = new PlayTestOptions { ConfigurationAssembly = WithMetadata() };

        //Act
        var theme = options.ResolveTheme();

        //Assert
        theme.Should().Be(ApplicationTheme.Light);
        options.Theme.Should().Be(ApplicationTheme.Light);
    }

    [Theory]
    [InlineData(null, null, "Dark", ApplicationTheme.Dark)]
    [InlineData(null, "light", "Dark", ApplicationTheme.Light)]
    [InlineData(null, "", "Dark", ApplicationTheme.Dark)]
    [InlineData("DaRk", "light", "Light", ApplicationTheme.Dark)]
    [InlineData("LiGhT", "dark", "Dark", ApplicationTheme.Light)]
    public void ResolveTheme_prefers_command_line_then_environment_then_project(
        string? commandLine, string? environment, string project, ApplicationTheme expected)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.ThemeKey, commandLine);
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.ThemeVariable, environment);
        var options = new PlayTestOptions { ConfigurationAssembly = WithMetadata((ThemeMetadata, project)) };

        //Act
        var theme = options.ResolveTheme();

        //Assert
        theme.Should().Be(expected);
    }

    [Theory]
    [InlineData(ApplicationTheme.Light)]
    [InlineData(ApplicationTheme.Dark)]
    public void ResolveTheme_explicit_code_value_wins(ApplicationTheme explicitTheme)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.ThemeKey, "invalid command line");
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.ThemeVariable, "invalid environment");
        var options = new PlayTestOptions
        {
            ConfigurationAssembly = WithMetadata((ThemeMetadata, "invalid project")),
            Theme = explicitTheme,
        };

        //Act
        var theme = options.ResolveTheme();

        //Assert
        theme.Should().Be(explicitTheme);
    }

    [Theory]
    [InlineData("system", null, null, "--theme")]
    [InlineData(null, "system", null, "CODEBRIX_PLAYTEST_THEME")]
    [InlineData(null, null, "system", ThemeMetadata)]
    public void ResolveTheme_invalid_selected_value_names_its_source(
        string? commandLine, string? environment, string? project, string source)
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();
        AppContext.SetData(PlayTestSettingsScope.ThemeKey, commandLine);
        Environment.SetEnvironmentVariable(PlayTestSettingsScope.ThemeVariable, environment);
        var options = new PlayTestOptions
        {
            ConfigurationAssembly = project == null ? WithMetadata() : WithMetadata((ThemeMetadata, project)),
        };

        //Act
        Action resolve = () => options.ResolveTheme();

        //Assert
        resolve.Should().Throw<ArgumentException>().WithMessage($"*{source}*system*");
    }

    [Fact]
    public void Timeout_and_artifacts_directory_have_documented_defaults()
    {
        //Arrange
        using var scope = new PlayTestSettingsScope();

        //Act
        var options = new PlayTestOptions();

        //Assert
        options.Timeout.Should().Be(10_000f);
        options.ArtifactsDirectory.Should().Be(System.IO.Path.Combine("TestResults", "PlayTest"));
        options.ConfigurationAssembly.Should().BeNull();
    }

    // A collectible in-memory assembly carrying exactly the given AssemblyMetadata attributes -
    // the shape the package's targets give a test project for its project preferences.
    private static Assembly WithMetadata(params (string Key, string Value)[] metadata)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("PlayTestPreferences_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var constructor = typeof(AssemblyMetadataAttribute).GetConstructor(new[] { typeof(string), typeof(string) })
            ?? throw new InvalidOperationException("AssemblyMetadataAttribute(string, string) is missing.");
        foreach (var (key, value) in metadata)
            assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, new object[] { key, value }));
        return assembly;
    }
}
