//
// AppSettingLoggingService.cs
//
// Copyright (c) 2026 Jeremy Ellis and contributors
//     (extracted for CodeBrix.Platform from the sample settings stores;
//      inspired by MonoDevelop.Core.LoggingService, simplified)
// SPDX-License-Identifier: Apache-2.0
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeBrix.Platform.Extensions;
using Microsoft.Extensions.Logging;

namespace CodeBrix.Platform.AppSettings; //was previously: Doom.Brix.Settings.LoggingService

/// <summary>The severity of a line written by <see cref="AppSettingLoggingService"/>.</summary>
public enum AppSettingLogLevel
{
    /// <summary>An informational message.</summary>
    Info,

    /// <summary>A warning.</summary>
    Warning,

    /// <summary>An error.</summary>
    Error,
}

/// <summary>
/// The logging service for the settings backend. Every line is forwarded to
/// the framework's ambient logger, written to the console while
/// <see cref="ConsoleOutput"/> is enabled, and delivered to any registered
/// sinks — with earlier lines replayed to a sink that registers late.
/// </summary>
/// <remarks>
/// Console output is on by default because an application that has not
/// configured framework logging would otherwise see nothing at all: the
/// ambient logger factory discards everything until something wires it up,
/// and a typical application only does so in DEBUG builds. An application
/// that has configured logging and does not want the duplicate should set
/// <see cref="ConsoleOutput"/> to false during startup.
/// </remarks>
public static class AppSettingLoggingService
{
    /// <summary>
    /// The category the framework's ambient logger records these lines under.
    /// Note that it begins with "CodeBrix.Platform", so an application that
    /// filters that prefix down to Warning — as the usual logging setup does —
    /// will not see the informational lines unless it adds a more specific
    /// filter for this category.
    /// </summary>
    public const string LogCategory = "CodeBrix.Platform.AppSettings";

    static readonly object sync = new();
    static readonly List<string> history = new();
    static readonly List<Action<string>> textSinks = new();
    static readonly List<Action<AppSettingLogLevel, string>> levelSinks = new();
    static ILogger? logger;
    static bool forwardingFailed;

    /// <summary>
    /// Whether every logged line is also written to the console. Defaults to
    /// true; see the remarks on <see cref="AppSettingLoggingService"/>.
    /// </summary>
    public static bool ConsoleOutput { get; set; } = true;

    /// <summary>
    /// Registers a sink that receives every logged line from now on; lines
    /// logged before registration are replayed to it first, so no message is
    /// missed. Sinks may be called from any thread — marshal to the UI
    /// thread inside the sink if needed.
    /// </summary>
    public static void AddSink(Action<string> sink)
    {
        if (sink == null)
            throw new ArgumentNullException(nameof(sink));

        string[] backlog;
        lock (sync)
        {
            backlog = history.ToArray();
            textSinks.Add(sink);
        }
        foreach (var line in backlog)
            sink(line);
    }

    /// <summary>
    /// Registers a sink that receives every logged line along with its
    /// severity, so the sink can filter. Unlike the plain-text overload this
    /// one is not replayed: the severity of an already-formatted historical
    /// line is not recoverable.
    /// </summary>
    public static void AddSink(Action<AppSettingLogLevel, string> sink)
    {
        if (sink == null)
            throw new ArgumentNullException(nameof(sink));

        lock (sync)
            levelSinks.Add(sink);
    }

    /// <summary>
    /// Unregisters a sink previously passed to <see cref="AddSink(Action{string})"/>.
    /// Returns true when a sink was removed.
    /// </summary>
    public static bool RemoveSink(Action<string> sink)
    {
        lock (sync)
            return textSinks.Remove(sink);
    }

    /// <summary>
    /// Unregisters a sink previously passed to
    /// <see cref="AddSink(Action{AppSettingLogLevel, string})"/>. Returns true
    /// when a sink was removed.
    /// </summary>
    public static bool RemoveSink(Action<AppSettingLogLevel, string> sink)
    {
        lock (sync)
            return levelSinks.Remove(sink);
    }

    /// <summary>Logs an informational message.</summary>
    public static void LogInfo(string message) => Log(AppSettingLogLevel.Info, message);

    /// <summary>Logs a warning message.</summary>
    public static void LogWarning(string message) => Log(AppSettingLogLevel.Warning, message);

    /// <summary>Logs an error message.</summary>
    public static void LogError(string message) => Log(AppSettingLogLevel.Error, message);

    /// <summary>Logs an error message with exception details.</summary>
    public static void LogError(string message, Exception ex) => Log(AppSettingLogLevel.Error, $"{message}: {ex}");

    static void Log(AppSettingLogLevel level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {Label(level)}: {message}";

        if (ConsoleOutput)
            Console.WriteLine(line);

        Forward(level, message);

        Action<string>[] textTargets;
        Action<AppSettingLogLevel, string>[] levelTargets;
        lock (sync)
        {
            history.Add(line);
            textTargets = textSinks.ToArray();
            levelTargets = levelSinks.ToArray();
        }
        foreach (var sink in textTargets)
            sink(line);
        foreach (var sink in levelTargets)
            sink(level, message);
    }

    static void Forward(AppSettingLogLevel level, string message)
    {
        if (forwardingFailed)
            return;
        try
        {
            // The call is isolated in its own non-inlined method so that this
            // try/catch actually covers it. A missing Microsoft.Extensions.Logging
            // assembly surfaces when the method touching those types is JIT-compiled,
            // which happens on ENTRY to the method - a handler inside that same
            // method would never run.
            ForwardCore(level, message);
        }
        catch (Exception ex)
        {
            // Logging must never be the reason an application fails to start, and
            // one failure means every later attempt fails the same way.
            forwardingFailed = true;
            if (ConsoleOutput)
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] WARN : settings log forwarding disabled: {ex.Message}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ForwardCore(AppSettingLogLevel level, string message)
    {
        // Resolved lazily: touching the ambient factory materializes it, and
        // there is no reason to do that before the first line is logged.
        logger ??= LogExtensionPoint.AmbientLoggerFactory.CreateLogger(LogCategory);
        // CA2254/CA1848: the message is already fully formatted, and these lines
        // are rare startup/recovery events - a LoggerMessage delegate would buy
        // nothing and would fix the severity at compile time, which this cannot.
#pragma warning disable CA2254, CA1848
        logger.Log(ToLogLevel(level), message);
#pragma warning restore CA2254, CA1848
    }

    static LogLevel ToLogLevel(AppSettingLogLevel level) => level switch
    {
        AppSettingLogLevel.Warning => LogLevel.Warning,
        AppSettingLogLevel.Error => LogLevel.Error,
        _ => LogLevel.Information,
    };

    static string Label(AppSettingLogLevel level) => level switch
    {
        AppSettingLogLevel.Warning => "WARN ",
        AppSettingLogLevel.Error => "ERROR",
        _ => "INFO ",
    };
}
