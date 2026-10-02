using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Recording;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace CodeBrix.Platform.PlayTest.TestingPlatform;

// Compiled into Microsoft.Testing.Platform consumers by the package's targets.
// Keeping the adapter here leaves the PlayTest runtime independent of test runners.
// Consumers compile this file with their own nullable setting (enabled or disabled), so it is
// written to be warning-free either way: no reference-type '?' annotations, and nullable values
// flow through 'var' locals only.

internal static class TestingPlatformBuilderHook
{
    public static void AddExtensions(ITestApplicationBuilder builder, string[] arguments)
        => builder.CommandLine.AddProvider(() => new PreviewCommandLineOptions());
}

internal sealed class PreviewCommandLineOptions : ICommandLineOptionsProvider
{
    // Private adapter/runtime contract; do not change the user's environment.
    internal const string HeadlessSetting = "CodeBrix.Platform.PlayTest.CommandLineHeadless";
    internal const string ThemeSetting = "CodeBrix.Platform.PlayTest.CommandLineTheme";
    internal const string OrientationSetting = "CodeBrix.Platform.PlayTest.CommandLineOrientation";
    internal const string ScreenshotFolderSetting = "CodeBrix.Platform.PlayTest.CommandLineScreenshotFolder";

    public string Uid => "CodeBrix.Platform.PlayTest.PreviewCommandLineOptions";
    public string Version => "1.0.0";
    public string DisplayName => "CodeBrix PlayTest preview";
    public string Description => "Controls the PlayTest preview, display preferences and screenshot recording.";
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions() => new[]
    {
        new CommandLineOption("headed", "Show the PlayTest preview; overrides CODEBRIX_PLAYTEST_HEADED.", ArgumentArity.Zero, false),
        new CommandLineOption("nonheadless", "Alias for --headed.", ArgumentArity.Zero, false),
        new CommandLineOption("headless", "Run PlayTest without a preview; overrides CODEBRIX_PLAYTEST_HEADED.", ArgumentArity.Zero, false),
        new CommandLineOption("theme", "Simulated system theme: light or dark (case insensitive); overrides environment/project preferences.", ArgumentArity.ExactlyOne, false),
        new CommandLineOption("orientation", "Preferred orientation: landscape or portrait (case insensitive); overrides environment/project preferences.", ArgumentArity.ExactlyOne, false),
        new CommandLineOption("screenshotfolder", "Record test screenshots and screenshot-index.json in an existing, empty folder (xUnit v3 4+).", ArgumentArity.ExactlyOne, false),
    };

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
        try
        {
            if (commandOption.Name is "theme" or "orientation" or "screenshotfolder")
                ValidateValue(commandOption.Name, arguments);
            return ValidationResult.ValidTask;
        }
        catch (Exception e) when (e is ArgumentException || e is System.IO.IOException || e is UnauthorizedAccessException)
        { return Task.FromResult(ValidationResult.Invalid(e.Message)); }
    }

    private static string ValidateValue(string name, string[] arguments)
    {
        if (arguments == null || arguments.Length != 1 || string.IsNullOrWhiteSpace(arguments[0]))
            throw new ArgumentException($"--{name} requires exactly one value.");
        var value = arguments[0];
        if (name == "screenshotfolder")
        {
#if !CODEBRIX_PLAYTEST_XUNIT
            throw new ArgumentException("--screenshotfolder requires an xUnit v3 4+ PlayTest project; automatic test lifecycle recording is unavailable for this runner.");
#else
            return PlayTestRecording.ValidateFolder(value);
#endif
        }
        var allowed = name == "theme" ? new[] { "light", "dark" } : new[] { "landscape", "portrait" };
        foreach (var choice in allowed)
            if (string.Equals(value, choice, StringComparison.OrdinalIgnoreCase)) return choice;
        throw new ArgumentException($"--{name} must be {string.Join(" or ", allowed)}; received '{value}'.");
    }

    public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
    {
        var headed = commandLineOptions.IsOptionSet("headed") || commandLineOptions.IsOptionSet("nonheadless");
        var headless = commandLineOptions.IsOptionSet("headless");
        if (headed && headless)
            return Task.FromResult(ValidationResult.Invalid("PlayTest cannot combine --headless with --headed or --nonheadless."));

        try
        {
            var theme = commandLineOptions.TryGetOptionArgumentList("theme", out var themeValues)
                ? ValidateValue("theme", themeValues) : null;
            var orientation = commandLineOptions.TryGetOptionArgumentList("orientation", out var orientationValues)
                ? ValidateValue("orientation", orientationValues) : null;
            var folder = commandLineOptions.TryGetOptionArgumentList("screenshotfolder", out var folderValues)
                ? ValidateValue("screenshotfolder", folderValues) : null;
            AppContext.SetData(HeadlessSetting, headed ? (object)false : headless ? true : null);
            AppContext.SetData(ThemeSetting, theme);
            AppContext.SetData(OrientationSetting, orientation);
            AppContext.SetData(ScreenshotFolderSetting, folder);
            return ValidationResult.ValidTask;
        }
        catch (Exception e) when (e is ArgumentException || e is System.IO.IOException || e is UnauthorizedAccessException)
        { return Task.FromResult(ValidationResult.Invalid(e.Message)); }
    }
}
