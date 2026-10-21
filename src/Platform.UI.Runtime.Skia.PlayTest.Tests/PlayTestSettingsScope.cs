using System;
using System.Linq;

namespace CodeBrix.Platform.PlayTest.Tests;

// Every PlayTest preference is process-wide: five AppContext keys written by the command-line
// adapter and five environment variables. A scope clears all of them on entry and restores the
// previous values on Dispose, so a test sees only what it sets and leaves nothing behind. The
// suite is serialized (AssemblyInfo.cs) because these values are shared by every test.
internal sealed class PlayTestSettingsScope : IDisposable
{
    internal const string HeadlessKey = "CodeBrix.Platform.PlayTest.CommandLineHeadless";
    internal const string ThemeKey = "CodeBrix.Platform.PlayTest.CommandLineTheme";
    internal const string OrientationKey = "CodeBrix.Platform.PlayTest.CommandLineOrientation";
    internal const string ScreenshotFolderKey = "CodeBrix.Platform.PlayTest.CommandLineScreenshotFolder";
    internal const string AudibleKey = "CodeBrix.Platform.PlayTest.CommandLineAudible";
    internal const string HeadedVariable = "CODEBRIX_PLAYTEST_HEADED";
    internal const string SlowMoVariable = "CODEBRIX_PLAYTEST_SLOWMO";
    internal const string ThemeVariable = "CODEBRIX_PLAYTEST_THEME";
    internal const string OrientationVariable = "CODEBRIX_PLAYTEST_ORIENTATION";
    internal const string AudibleVariable = "CODEBRIX_PLAYTEST_AUDIBLE";

    private static readonly string[] Keys = { HeadlessKey, ThemeKey, OrientationKey, ScreenshotFolderKey, AudibleKey };
    private static readonly string[] Variables = { HeadedVariable, SlowMoVariable, ThemeVariable, OrientationVariable, AudibleVariable };

    private readonly object?[] _keyValues;
    private readonly string?[] _variableValues;

    internal PlayTestSettingsScope()
    {
        _keyValues = Keys.Select(AppContext.GetData).ToArray();
        _variableValues = Variables.Select(Environment.GetEnvironmentVariable).ToArray();
        foreach (var key in Keys) AppContext.SetData(key, null);
        foreach (var variable in Variables) Environment.SetEnvironmentVariable(variable, null);
    }

    public void Dispose()
    {
        for (var i = 0; i < Keys.Length; i++) AppContext.SetData(Keys[i], _keyValues[i]);
        for (var i = 0; i < Variables.Length; i++) Environment.SetEnvironmentVariable(Variables[i], _variableValues[i]);
    }
}
