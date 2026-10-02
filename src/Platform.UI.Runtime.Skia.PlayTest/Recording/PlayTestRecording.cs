using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.Platform.PlayTest.Recording;

/// <summary>Infrastructure used by the package's automatically registered runner adapter.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PlayTestRecording
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly AsyncLocal<int> Depth = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly Dictionary<string, int> Cases = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> Paths = new(StringComparer.OrdinalIgnoreCase);
    private static RecordingRun? _run;
    private static RecordedTest? _current;
    // Meaningful only while _run is set; StartRunAsync assigns both together.
    private static string _folder = "";
    private static bool _body;

    /// <summary>Normalizes a directory without creating or changing it.</summary>
    /// <param name="folder">An absolute or relative path; a leading <c>~/</c> or <c>~\</c> means the user profile.</param>
    /// <returns>The full path of the existing, empty folder.</returns>
    /// <exception cref="ArgumentException">The folder is blank, does not exist, or is not empty (hidden entries count).</exception>
    public static string ValidateFolder(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        if (folder == "~" || folder.StartsWith("~/", StringComparison.Ordinal) || folder.StartsWith("~\\", StringComparison.Ordinal))
            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), folder.Length == 1 ? "" : folder[2..]);
        folder = Path.GetFullPath(folder);
        if (!Directory.Exists(folder)) throw new ArgumentException($"--screenshotfolder must name an existing folder: {folder}");
        if (Directory.EnumerateFileSystemEntries(folder).Any())
            throw new ArgumentException($"--screenshotfolder must be empty (including hidden files and subfolders): {folder}");
        return folder;
    }

    /// <summary>Claims the empty folder for this test process and creates the initial index.</summary>
    /// <param name="tests">The test assembly being run.</param>
    /// <returns>A task that completes when the index exists; does nothing when recording was not requested.</returns>
    public static async Task StartRunAsync(Assembly tests)
    {
        if (AppContext.GetData("CodeBrix.Platform.PlayTest.CommandLineScreenshotFolder") is not string folder) return;
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_run != null) throw new InvalidOperationException("A screenshot recording session is already active.");
            folder = ValidateFolder(folder);
            var run = new RecordingRun
            {
                RunId = Guid.NewGuid().ToString("N"), StartedAt = DateTimeOffset.Now,
                TestAssembly = AssemblyInfo(tests),
                RootNamespace = tests.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "CodeBrixPlayTestRootNamespace")?.Value ?? tests.GetName().Name ?? "",
                System = new { OperatingSystem = RuntimeInformation.OSDescription, OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), Runtime = RuntimeInformation.FrameworkDescription,
                    Environment.MachineName, ProcessId = Environment.ProcessId, TimeZone = TimeZoneInfo.Local.Id },
                Preferences = new { Headless = AppContext.GetData("CodeBrix.Platform.PlayTest.CommandLineHeadless"),
                    Theme = AppContext.GetData("CodeBrix.Platform.PlayTest.CommandLineTheme"),
                    Orientation = AppContext.GetData("CodeBrix.Platform.PlayTest.CommandLineOrientation"),
                    EnvironmentTheme = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_THEME"),
                    EnvironmentOrientation = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_ORIENTATION"),
                    EnvironmentHeaded = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED") },
                PlayTestAssembly = AssemblyInfo(typeof(PlayTestApplication).Assembly),
            };
            // Exclusive creation also prevents two test modules from claiming the same destination.
            await using (var stream = new FileStream(Path.Combine(folder, "screenshot-index.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await JsonSerializer.SerializeAsync(stream, run, Json).ConfigureAwait(false);
            _folder = folder;
            _run = run;
            Cases.Clear();
            Paths.Clear();
        }
        finally { Gate.Release(); }
    }

    /// <summary>Records test identity before setup. Screenshots begin at the test body.</summary>
    /// <param name="id">The runner's unique test ID.</param>
    /// <param name="displayName">The runner's display name.</param>
    /// <param name="testClass">The test class.</param>
    /// <param name="method">The test method.</param>
    /// <param name="theory">True for a data-driven test; each row gets its own case folder.</param>
    /// <param name="arguments">The row's arguments, if any.</param>
    /// <param name="sourceFile">The test's source file, when the runner knows it.</param>
    /// <param name="sourceLine">The test's source line, when the runner knows it.</param>
    /// <returns>A task that completes when the index has been updated.</returns>
    public static async Task TestStartingAsync(string id, string displayName, Type testClass, MethodInfo method,
        bool theory, object?[]? arguments, string? sourceFile, int? sourceLine)
    {
        if (_run == null) return;
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current != null) throw new InvalidOperationException("Screenshot recording requires serialized PlayTests. Disable parallel test execution for the shared application.");
            var ns = testClass.Namespace ?? "";
            var relativeNamespace = ns == _run.RootNamespace ? "" : ns.StartsWith(_run.RootNamespace + ".", StringComparison.Ordinal) ? ns[(_run.RootNamespace.Length + 1)..] : ns;
            var fullClassName = testClass.FullName ?? testClass.Name;
            var identity = fullClassName + "." + method;
            var parts = relativeNamespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(SafeName).ToList();
            parts.Add(SafeName(fullClassName[(ns.Length == 0 || !fullClassName.StartsWith(ns + ".", StringComparison.Ordinal) ? 0 : ns.Length + 1)..]));
            parts.Add(SafeName(method.Name));
            var path = string.Join("/", parts);
            if (Paths.TryGetValue(path, out var owner) && owner != identity) path += "-" + Hash(identity);
            Paths[path] = identity;
            Cases.TryGetValue(identity, out var ordinal);
            Cases[identity] = ++ordinal;
            if (theory) path += "/test-case-" + ordinal.ToString(CultureInfo.InvariantCulture);
            else if (ordinal > 1) path += "/attempt-" + ordinal.ToString(CultureInfo.InvariantCulture);
            Directory.CreateDirectory(Path.Combine(_folder, path.Replace('/', Path.DirectorySeparatorChar)));
            _current = new RecordedTest
            {
                Id = id, DisplayName = displayName, Namespace = ns, ClassName = testClass.Name,
                FullClassName = fullClassName, Method = method.Name, MethodSignature = method.ToString(),
                CaseNumber = theory ? ordinal : null, Arguments = arguments?.Select(FormatArgument).ToArray() ?? Array.Empty<object>(),
                Directory = path, StartedAt = DateTimeOffset.Now, Source = Source(sourceFile, sourceLine),
            };
            _run.Tests.Add(_current);
            _body = false;
            await SaveIndexAsync().ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }

    /// <summary>Captures the initial UI after test setup.</summary>
    /// <returns>A task that completes when the start screenshot has been written.</returns>
    public static async Task TestBodyStartingAsync()
    {
        if (_current == null) return;
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current is not { } current) return;
            _body = true;
            current.BodyStartedAt = DateTimeOffset.Now;
            await CaptureAsync(PlayTestApplication.Current, "start", "Test start", null, current.Source).ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }

    /// <summary>Captures the final UI before test cleanup, also when the test failed.</summary>
    /// <returns>A task that completes when the final screenshot has been written.</returns>
    public static async Task TestBodyFinishedAsync()
    {
        if (_current == null) return;
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await CaptureFinalAsync(PlayTestApplication.Current).ConfigureAwait(false);
            _body = false;
        }
        finally { Gate.Release(); }
    }

    /// <summary>Stores the runner's outcome after cleanup.</summary>
    /// <param name="id">The runner's unique test ID, as passed to <see cref="TestStartingAsync"/>.</param>
    /// <param name="outcome">The runner's result, for example <c>passed</c>, <c>failed</c> or <c>skipped</c>.</param>
    /// <param name="errors">Failure messages, if any.</param>
    /// <returns>A task that completes when the index has been updated.</returns>
    public static async Task TestFinishedAsync(string id, string outcome, string[]? errors)
    {
        if (_run == null) return;
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current is not { } current || current.Id != id) return;
            _body = false;
            var finishedAt = DateTimeOffset.Now;
            current.Outcome = outcome;
            current.Errors = errors;
            current.FinishedAt = finishedAt;
            current.DurationMilliseconds = (finishedAt - current.StartedAt).TotalMilliseconds;
            if (current.Screenshots.Count == 0)
                current.ScreenshotNote = current.BodyStartedAt == null ? "Test body did not run (for example, skipped or setup failed)." : "No running PlayTest application was available during this test.";
            _current = null;
            await SaveIndexAsync().ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }

    /// <summary>Marks a normally terminated test session complete.</summary>
    /// <returns>A task that completes when the final index has been written.</returns>
    public static async Task FinishRunAsync()
    {
        if (_run == null) return;
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_run is { } run)
            {
                run.Status = _current == null ? "completed" : "incomplete";
                run.FinishedAt = DateTimeOffset.Now;
                await SaveIndexAsync().ConfigureAwait(false);
            }
        }
        finally { _run = null; _current = null; _body = false; Gate.Release(); }
    }

    internal static StepScope? Step(PlayTestApplication app, string operation, string? target = null)
    {
        if (!_body || _current == null) return null;
        var outermost = Depth.Value == 0;
        Depth.Value++;
        return new StepScope(app, _current, outermost ? operation : null, target, outermost ? CallerSource() : null);
    }

    internal sealed class StepScope : IAsyncDisposable
    {
        private readonly PlayTestApplication _app;
        private readonly RecordedTest _test;
        private readonly string? _operation, _target;
        private readonly SourceLocation? _source;
        internal StepScope(PlayTestApplication app, RecordedTest test, string? operation, string? target, SourceLocation? source)
        { _app = app; _test = test; _operation = operation; _target = target; _source = source; }
        public ValueTask DisposeAsync()
        {
            Depth.Value--;
            return _operation == null ? ValueTask.CompletedTask : new ValueTask(RecordAsync());
        }
        private async Task RecordAsync()
        {
            await Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                // Timed-out/background work must never be filed under the following test.
                if (!_body || !ReferenceEquals(_current, _test)) return;
                if (!_test.Screenshots.Any(s => s.Kind == "start"))
                    await CaptureAsync(_app, "start", "Application became available", null, _source).ConfigureAwait(false);
                await CaptureAsync(_app, "step", _operation, _target, _source).ConfigureAwait(false);
            }
            finally { Gate.Release(); }
        }
    }

    internal static async Task BeforeApplicationDisposedAsync(PlayTestApplication app)
    {
        if (!_body || _current == null) return;
        await Gate.WaitAsync().ConfigureAwait(false);
        try { await CaptureFinalAsync(app).ConfigureAwait(false); }
        finally { Gate.Release(); }
    }

    private static Task CaptureFinalAsync(PlayTestApplication? app) => _current is not { } current || current.Screenshots.Any(s => s.Kind == "final")
        ? Task.CompletedTask : CaptureAsync(app, "final", "Test final state", null, null);

    // Callers hold Gate with a test in progress; the guards cover a run torn down underneath them.
    private static async Task CaptureAsync(PlayTestApplication? app, string kind, string? operation, string? target, SourceLocation? source)
    {
        if (app == null || _current is not { } current || _run is not { } run) return;
        var number = kind == "step" ? current.Screenshots.Count(s => s.Kind == "step") + 1 : (int?)null;
        var name = "screenshot-" + (number?.ToString(CultureInfo.InvariantCulture) ?? kind) + ".png";
        var path = current.Directory + "/" + name;
        try
        {
            var bytes = await app.Page.ScreenshotAsync().ConfigureAwait(false);
            var themes = await app.Host.OnUI(() => (
                Microsoft.UI.Xaml.Application.Current.RequestedTheme.ToString(),
                (app.Host.Window.ManagedWindow?.Content as Microsoft.UI.Xaml.FrameworkElement)?.RequestedTheme.ToString())).ConfigureAwait(false);
            var absolute = Path.Combine(_folder, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? _folder);
            await using (var stream = new FileStream(absolute, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await stream.WriteAsync(bytes).ConfigureAwait(false);
            run.Application ??= new { Type = app.ApplicationType?.FullName, Assembly = AssemblyInfo(app.ApplicationType?.Assembly),
                app.Headless, SystemTheme = app.SystemTheme.ToString(), PreferredOrientation = app.PreferredOrientation.ToString(),
                app.Options.SlowMo, app.Options.Timeout };
            current.Screenshots.Add(new RecordedScreenshot
            {
                Kind = kind, Step = number, Path = path, CapturedAt = DateTimeOffset.Now, Operation = operation, Target = target,
                Width = app.Width, Height = app.Height, Orientation = app.Orientation.ToString(), SystemTheme = app.SystemTheme.ToString(), Source = source,
                ApplicationTheme = themes.Item1, RootRequestedTheme = themes.Item2,
            });
            await SaveIndexAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            current.RecordingErrors.Add($"{operation}: {e.Message}");
            await SaveIndexAsync().ConfigureAwait(false);
            throw new PlayTestException($"Unable to record PlayTest screenshot '{path}': {e.Message}", e);
        }
    }

    private static async Task SaveIndexAsync()
    {
        var temporary = Path.Combine(_folder, ".screenshot-index-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(_run, Json)).ConfigureAwait(false);
            File.Move(temporary, Path.Combine(_folder, "screenshot-index.json"), overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static object? AssemblyInfo(Assembly? assembly) => assembly == null ? null : new
    {
        Name = assembly.GetName().Name, Version = assembly.GetName().Version?.ToString(),
        InformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        assembly.Location, ModuleVersionId = assembly.ManifestModule.ModuleVersionId.ToString(),
    };

    private static object FormatArgument(object? value)
    {
        string? text;
        try { text = value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value?.ToString(); }
        catch { text = "<ToString unavailable>"; }
        return new { Type = value?.GetType().FullName, Value = text };
    }

    private static SourceLocation? CallerSource()
    {
        foreach (var frame in new StackTrace(true).GetFrames())
        {
            if (frame.GetFileLineNumber() <= 0 || frame.GetMethod()?.DeclaringType?.Assembly == typeof(PlayTestRecording).Assembly) continue;
            return new SourceLocation { File = frame.GetFileName(), Line = frame.GetFileLineNumber(), Member = frame.GetMethod()?.DeclaringType?.FullName + "." + frame.GetMethod()?.Name };
        }
        return null;
    }
    private static SourceLocation? Source(string? file, int? line) => file == null ? null : new() { File = file, Line = line };
    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..12].ToLowerInvariant();
    internal static string SafeName(string value)
    {
        var safe = string.Concat(value.Select(c => char.IsControl(c) || "<>:\"/\\|?*%".Contains(c) ? "%" + ((int)c).ToString("X4", CultureInfo.InvariantCulture) : c.ToString()));
        if (safe.Length == 0) safe = "_";
        if (safe is "." or ".." || safe.EndsWith('.') || safe.EndsWith(' ')) safe += "_";
        var stem = safe.Split('.')[0];
        if (new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" }.Contains(stem, StringComparer.OrdinalIgnoreCase)) safe = "_" + safe;
        return safe.Length <= 100 ? safe : safe[..80] + "-" + Hash(value);
    }

    private sealed class RecordingRun
    {
        public int SchemaVersion { get; set; } = 1;
        public string RunId { get; set; } = "";
        public string Status { get; set; } = "running";
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
        public string RootNamespace { get; set; } = "";
        public object? TestAssembly { get; set; }
        public object? PlayTestAssembly { get; set; }
        public object? System { get; set; }
        public object? Preferences { get; set; }
        public object? Application { get; set; }
        public List<RecordedTest> Tests { get; } = new();
    }
    internal sealed class RecordedTest
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Namespace { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string FullClassName { get; set; } = "";
        public string Method { get; set; } = "";
        public string? MethodSignature { get; set; }
        public int? CaseNumber { get; set; }
        public object[] Arguments { get; set; } = Array.Empty<object>();
        public string Directory { get; set; } = "";
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? BodyStartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
        public string Outcome { get; set; } = "running";
        public double? DurationMilliseconds { get; set; }
        public string[]? Errors { get; set; }
        public SourceLocation? Source { get; set; }
        public string? ScreenshotNote { get; set; }
        public List<string> RecordingErrors { get; } = new();
        public List<RecordedScreenshot> Screenshots { get; } = new();
    }
    internal sealed class RecordedScreenshot
    {
        public string Kind { get; set; } = "";
        public int? Step { get; set; }
        public string Path { get; set; } = "";
        public DateTimeOffset CapturedAt { get; set; }
        public string? Operation { get; set; }
        public string? Target { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Orientation { get; set; } = "";
        public string SystemTheme { get; set; } = "";
        public string? ApplicationTheme { get; set; }
        public string? RootRequestedTheme { get; set; }
        public SourceLocation? Source { get; set; }
    }
    internal sealed class SourceLocation
    {
        public string? File { get; set; }
        public int? Line { get; set; }
        public string? Member { get; set; }
    }
}
