using System;
using System.Globalization;
using CodeBrix.Platform.PlayTest;
using SilverAssertions;
using Xunit;

namespace PlayTestDemo.PlayTests;

// The assembly is serialized; restore environment/culture before the next test.
public sealed class SlowMoPreferenceTests
{
    [Theory]
    [InlineData(null, null, 0f)]
    [InlineData("0", null, 0f)]
    [InlineData("1", null, 250f)]
    [InlineData("1", "", 250f)]
    [InlineData("1", "0", 0f)]
    [InlineData("1", "100", 100f)]
    [InlineData("1", "750", 750f)]
    [InlineData("0", "125", 125f)]
    public void Environment_overrides_the_headed_or_headless_default(string headed, string slowMo, float expected)
    {
        WithEnvironment(headed, slowMo, () => new PlayTestOptions().SlowMo.Should().Be(expected));
    }

    [Theory]
    [InlineData(true, 0f)]
    [InlineData(false, 250f)]
    public void Default_uses_the_final_headless_option(bool headless, float expected)
    {
        WithEnvironment(headless ? "1" : "0", null, () =>
        {
            var options = new PlayTestOptions { Headless = headless };
            options.SlowMo.Should().Be(expected);
        });
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(50f)]
    [InlineData(500f)]
    public void Explicit_code_value_wins_including_zero(float delay)
    {
        WithEnvironment("1", "750", () =>
        {
            var options = new PlayTestOptions { SlowMo = delay, Headless = false };
            options.SlowMo.Should().Be(delay);
            options.Headless = true;
            options.SlowMo.Should().Be(delay);
        });
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("fast")]
    [InlineData(" ")]
    public void Invalid_environment_delay_identifies_the_setting(string delay)
    {
        WithEnvironment("1", delay, () =>
        {
            Action resolve = () => _ = new PlayTestOptions().SlowMo;
            resolve.Should().Throw<ArgumentException>().WithMessage("*CODEBRIX_PLAYTEST_SLOWMO*");
        });
    }

    [Fact]
    public void Fractional_milliseconds_use_invariant_culture()
    {
        WithEnvironment("1", "12.5", () =>
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                new PlayTestOptions().SlowMo.Should().Be(12.5f);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        });
    }

    private static void WithEnvironment(string headed, string delay, Action action)
    {
        const string headedName = "CODEBRIX_PLAYTEST_HEADED";
        const string delayName = "CODEBRIX_PLAYTEST_SLOWMO";
        var previousHeaded = Environment.GetEnvironmentVariable(headedName);
        var previousDelay = Environment.GetEnvironmentVariable(delayName);
        const string commandLineName = "CodeBrix.Platform.PlayTest.CommandLineHeadless";
        var previousCommandLine = AppContext.GetData(commandLineName);
        try
        {
            AppContext.SetData(commandLineName, null);
            Environment.SetEnvironmentVariable(headedName, headed);
            Environment.SetEnvironmentVariable(delayName, delay);
            action();
        }
        finally
        {
            AppContext.SetData(commandLineName, previousCommandLine);
            Environment.SetEnvironmentVariable(headedName, previousHeaded);
            Environment.SetEnvironmentVariable(delayName, previousDelay);
        }
    }
}
