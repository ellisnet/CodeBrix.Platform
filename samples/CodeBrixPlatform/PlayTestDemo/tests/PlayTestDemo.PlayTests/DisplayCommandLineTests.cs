using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest;
using CodeBrix.Platform.PlayTest.Recording;
using CodeBrix.Platform.PlayTest.TestingPlatform;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.UI.Xaml;
using SilverAssertions;
using Xunit;

namespace PlayTestDemo.PlayTests;

public sealed class DisplayCommandLineTests
{
    [Theory]
    [InlineData("DaRk", "PoRtRaIt", ApplicationTheme.Dark, ScreenOrientation.Portrait)]
    [InlineData("LiGhT", "LaNdScApE", ApplicationTheme.Light, ScreenOrientation.Landscape)]
    public async Task Command_line_overrides_environment_and_project_but_code_still_wins(
        string theme, string orientation, ApplicationTheme expectedTheme, ScreenOrientation expectedOrientation)
    {
        await Isolated(async () =>
        {
            Environment.SetEnvironmentVariable("CODEBRIX_PLAYTEST_THEME", "invalid environment");
            Environment.SetEnvironmentVariable("CODEBRIX_PLAYTEST_ORIENTATION", "invalid environment");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Preferences_" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
            foreach (var key in new[] { "CodeBrixPlayTestPreferredTheme", "CodeBrixPlayTestPreferredOrientation" })
                assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyMetadataAttribute).GetConstructor(new[] { typeof(string), typeof(string) }), new object[] { key, "invalid project" }));
            var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(new Arguments(new() { ["theme"] = theme, ["orientation"] = orientation }));
            result.IsValid.Should().BeTrue();
            var options = new PlayTestOptions { ConfigurationAssembly = assembly };
            options.ResolveTheme().Should().Be(expectedTheme);
            options.ResolveOrientation().Should().Be(expectedOrientation);
            options.Theme = expectedTheme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
            options.Orientation = expectedOrientation == ScreenOrientation.Portrait ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
            options.ResolveTheme().Should().Be(options.Theme);
            options.ResolveOrientation().Should().Be(options.Orientation);
            Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_THEME").Should().Be("invalid environment");
            Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_ORIENTATION").Should().Be("invalid environment");
        });
    }

    [Theory]
    [InlineData("theme", "system")]
    [InlineData("theme", "1")]
    [InlineData("theme", " dark ")]
    [InlineData("theme", "")]
    [InlineData("orientation", "sideways")]
    [InlineData("orientation", "0")]
    [InlineData("orientation", "")]
    public async Task Invalid_values_are_rejected_before_app_startup(string option, string value)
    {
        await Isolated(async () =>
        {
            var result = await new PreviewCommandLineOptions().ValidateCommandLineOptionsAsync(new Arguments(new() { [option] = value }));
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("--" + option);
        });
    }

    [Fact]
    public void Screenshot_folder_validation_never_creates_or_overwrites_user_content()
    {
        var directory = Path.Combine(Path.GetTempPath(), "PlayTestFolder_" + Guid.NewGuid().ToString("N"));
        Action missing = () => PlayTestRecording.ValidateFolder(directory);
        missing.Should().Throw<ArgumentException>().WithMessage("*existing folder*");
        Directory.Exists(directory).Should().BeFalse();
        Directory.CreateDirectory(directory);
        try
        {
            PlayTestRecording.ValidateFolder(directory).Should().Be(Path.GetFullPath(directory));
            Directory.EnumerateFileSystemEntries(directory).Should().BeEmpty();
            var marker = Path.Combine(directory, ".hidden");
            File.WriteAllText(marker, "keep this");
            Action occupied = () => PlayTestRecording.ValidateFolder(directory);
            occupied.Should().Throw<ArgumentException>().WithMessage("*must be empty*");
            File.ReadAllText(marker).Should().Be("keep this");
            File.Delete(marker);
            Directory.CreateDirectory(Path.Combine(directory, "empty-child"));
            occupied.Should().Throw<ArgumentException>().WithMessage("*must be empty*");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static async Task Isolated(Func<Task> test)
    {
        var keys = new[] { PreviewCommandLineOptions.HeadlessSetting, PreviewCommandLineOptions.ThemeSetting, PreviewCommandLineOptions.OrientationSetting, PreviewCommandLineOptions.ScreenshotFolderSetting };
        var values = keys.Select(AppContext.GetData).ToArray();
        var names = new[] { "CODEBRIX_PLAYTEST_THEME", "CODEBRIX_PLAYTEST_ORIENTATION" };
        var environment = names.Select(Environment.GetEnvironmentVariable).ToArray();
        try { await test(); }
        finally
        {
            for (var i = 0; i < keys.Length; i++) AppContext.SetData(keys[i], values[i]);
            for (var i = 0; i < names.Length; i++) Environment.SetEnvironmentVariable(names[i], environment[i]);
        }
    }

    private sealed class Arguments(Dictionary<string, string> values) : ICommandLineOptions
    {
        public bool IsOptionSet(string name) => values.ContainsKey(name);
        public bool TryGetOptionArgumentList(string name, out string[] arguments)
        {
            var exists = values.TryGetValue(name, out var value);
            arguments = exists ? new[] { value } : null;
            return exists;
        }
    }
}
