using System;
using System.Threading.Tasks;
using System.Linq;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.PlayTest.TestingPlatform;
using Microsoft.Testing.Platform.CommandLine;
using SilverAssertions;
using Xunit;

namespace PlayTestDemo.PlayTests;

public sealed class PreviewCommandLineTests
{
    [Theory]
    [InlineData(null, null, true, 0f)]
    [InlineData(null, "0", true, 0f)]
    [InlineData(null, "1", false, 250f)]
    [InlineData("headed", "0", false, 250f)]
    [InlineData("headed", "1", false, 250f)]
    [InlineData("nonheadless", "0", false, 250f)]
    [InlineData("headless", "1", true, 0f)]
    [InlineData("headless", "0", true, 0f)]
    public Task Command_line_overrides_environment_and_sets_the_default_delay(string option, string environment, bool headless, float delay)
        => WithPreferencesAsync(environment, async () =>
        {
            var provider = new PreviewCommandLineOptions();
            var result = await provider.ValidateCommandLineOptionsAsync(new Arguments(option));
            result.IsValid.Should().BeTrue();
            var options = new PlayTestOptions();
            options.Headless.Should().Be(headless);
            options.SlowMo.Should().Be(delay);
            Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED").Should().Be(environment);
        });

    [Theory]
    [InlineData("headed", true)]
    [InlineData("headless", false)]
    public Task Explicit_fixture_option_retains_priority(string option, bool headless)
        => WithPreferencesAsync(null, async () =>
        {
            await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(new Arguments(option));
            var options = new PlayTestOptions { Headless = headless };
            options.Headless.Should().Be(headless);
            options.SlowMo.Should().Be(headless ? 0f : 250f);
        });

    [Theory]
    [InlineData("headed")]
    [InlineData("nonheadless")]
    public Task Conflicting_options_are_rejected(string headed)
        => WithPreferencesAsync(null, async () =>
        {
            var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(new Arguments(headed, "headless"));
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("cannot combine");
            AppContext.GetData(PreviewCommandLineOptions.HeadlessSetting).Should().BeNull();
        });

    private static async Task WithPreferencesAsync(string headed, Func<Task> action)
    {
        var settings = new[] { PreviewCommandLineOptions.HeadlessSetting, PreviewCommandLineOptions.ThemeSetting, PreviewCommandLineOptions.OrientationSetting, PreviewCommandLineOptions.ScreenshotFolderSetting };
        var previousSettings = settings.Select(AppContext.GetData).ToArray();
        var previousHeaded = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED");
        var previousDelay = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_SLOWMO");
        try
        {
            AppContext.SetData(PreviewCommandLineOptions.HeadlessSetting, null);
            Environment.SetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED", headed);
            Environment.SetEnvironmentVariable("CODEBRIX_PLAYTEST_SLOWMO", null);
            await action();
        }
        finally
        {
            for (var i = 0; i < settings.Length; i++) AppContext.SetData(settings[i], previousSettings[i]);
            Environment.SetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED", previousHeaded);
            Environment.SetEnvironmentVariable("CODEBRIX_PLAYTEST_SLOWMO", previousDelay);
        }
    }

    private sealed class Arguments(params string[] options) : ICommandLineOptions
    {
        public bool IsOptionSet(string name) => Array.IndexOf(options, name) >= 0;
        public bool TryGetOptionArgumentList(string name, out string[] arguments)
        {
            arguments = IsOptionSet(name) ? Array.Empty<string>() : null;
            return IsOptionSet(name);
        }
    }
}
