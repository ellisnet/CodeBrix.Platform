using System;
using System.Collections.Generic;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.AppSettings.Tests;

/// <summary>
/// The logging service is a process-wide static holding the sink lists and the
/// replay history, so these tests join the same non-parallel collection as the
/// rest of the static-state suites.
/// </summary>
[Collection(AppSettingsCollection.Name)]
public class AppSettingLoggingServiceTests : IDisposable
{
    readonly bool consoleOutputBefore = AppSettingLoggingService.ConsoleOutput;
    readonly List<Action> undo = new();

    public AppSettingLoggingServiceTests()
    {
        // Keep the suite's own output out of the test log.
        AppSettingLoggingService.ConsoleOutput = false;
    }

    public void Dispose()
    {
        foreach (var action in undo)
            action();
        AppSettingLoggingService.ConsoleOutput = consoleOutputBefore;
        GC.SuppressFinalize(this);
    }

    Action<string> TrackText(Action<string> sink)
    {
        AppSettingLoggingService.AddSink(sink);
        undo.Add(() => AppSettingLoggingService.RemoveSink(sink));
        return sink;
    }

    Action<AppSettingLogLevel, string> TrackLevel(Action<AppSettingLogLevel, string> sink)
    {
        AppSettingLoggingService.AddSink(sink);
        undo.Add(() => AppSettingLoggingService.RemoveSink(sink));
        return sink;
    }

    [Fact]
    public void A_text_sink_receives_lines_logged_after_it_registers()
    {
        //Arrange
        var lines = new List<string>();
        TrackText(lines.Add);

        //Act
        AppSettingLoggingService.LogInfo("after registration");

        //Assert
        lines.Should().ContainSingle(line => line.Contains("after registration", StringComparison.Ordinal));
    }

    [Fact]
    public void A_text_sink_is_replayed_the_lines_logged_before_it_registered()
    {
        //Arrange
        AppSettingLoggingService.LogInfo("logged before the sink existed");

        //Act
        var lines = new List<string>();
        TrackText(lines.Add);

        //Assert
        lines.Should().Contain(line => line.Contains("logged before the sink existed", StringComparison.Ordinal));
    }

    [Fact]
    public void A_text_line_carries_a_timestamp_and_a_severity_label()
    {
        //Arrange
        var lines = new List<string>();
        TrackText(lines.Add);

        //Act
        AppSettingLoggingService.LogWarning("careful");

        //Assert
        var line = lines.Find(text => text.Contains("careful", StringComparison.Ordinal));
        line.Should().NotBeNull();
        line!.Should().StartWith("[");
        line.Should().Contain("WARN");
    }

    [Fact]
    public void A_level_sink_receives_the_severity_and_the_bare_message()
    {
        //Arrange
        var received = new List<(AppSettingLogLevel Level, string Message)>();
        TrackLevel((level, message) => received.Add((level, message)));

        //Act
        AppSettingLoggingService.LogInfo("informational");
        AppSettingLoggingService.LogWarning("warned");
        AppSettingLoggingService.LogError("failed");

        //Assert — the message reaches a level sink unformatted, so the sink can
        // apply its own layout as well as filter.
        received.Should().Equal(new[]
        {
            (AppSettingLogLevel.Info, "informational"),
            (AppSettingLogLevel.Warning, "warned"),
            (AppSettingLogLevel.Error, "failed"),
        });
    }

    [Fact]
    public void A_level_sink_is_not_replayed()
    {
        //Arrange
        AppSettingLoggingService.LogInfo("before the level sink");

        //Act
        var received = new List<string>();
        TrackLevel((_, message) => received.Add(message));

        //Assert
        received.Should().BeEmpty();
    }

    [Fact]
    public void An_error_with_an_exception_includes_the_exception_text()
    {
        //Arrange
        var received = new List<string>();
        TrackLevel((_, message) => received.Add(message));

        //Act
        AppSettingLoggingService.LogError("could not open", new InvalidOperationException("the reason"));

        //Assert
        received.Should().ContainSingle(message => message.Contains("could not open", StringComparison.Ordinal)
            && message.Contains("the reason", StringComparison.Ordinal));
    }

    [Fact]
    public void A_removed_sink_stops_receiving_lines()
    {
        //Arrange
        var lines = new List<string>();
        Action<string> sink = lines.Add;
        AppSettingLoggingService.AddSink(sink);

        //Act
        AppSettingLoggingService.LogInfo("delivered");
        var removed = AppSettingLoggingService.RemoveSink(sink);
        AppSettingLoggingService.LogInfo("not delivered");

        //Assert
        removed.Should().BeTrue();
        lines.Should().Contain(line => line.Contains("delivered", StringComparison.Ordinal));
        lines.Should().NotContain(line => line.Contains("not delivered", StringComparison.Ordinal));
    }

    [Fact]
    public void Removing_a_sink_that_was_never_added_reports_false()
    {
        //Assert
        AppSettingLoggingService.RemoveSink((string _) => { }).Should().BeFalse();
        AppSettingLoggingService.RemoveSink((AppSettingLogLevel _, string _) => { }).Should().BeFalse();
    }

    [Fact]
    public void A_null_sink_is_rejected()
    {
        //Act
        Action text = () => AppSettingLoggingService.AddSink((Action<string>) null!);
        Action level = () => AppSettingLoggingService.AddSink((Action<AppSettingLogLevel, string>) null!);

        //Assert
        text.Should().Throw<ArgumentNullException>();
        level.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Console_output_is_on_by_default()
    {
        //Assert — the default preserves what an application saw before this
        // package existed, since the ambient logger discards everything until
        // the application configures it.
        consoleOutputBefore.Should().BeTrue();
    }
}
