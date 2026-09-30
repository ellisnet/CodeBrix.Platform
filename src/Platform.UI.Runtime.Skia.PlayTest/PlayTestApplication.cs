using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.PlayTest.Hosting;
using CodeBrix.Platform.PlayTest.Preview;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.PlayTest;

public enum ScreenOrientation { Landscape, Portrait }

public sealed class PlayTestOptions
{
    private ScreenOrientation? _orientation;
    private float? _slowMo;
    /// <summary>An explicit orientation overrides environment and project preferences.</summary>
    public ScreenOrientation Orientation { get => _orientation ?? ScreenOrientation.Landscape; set => _orientation = value; }
    /// <summary>The test assembly containing project preferences; defaults to the entry assembly.</summary>
    public Assembly ConfigurationAssembly { get; set; }
    public bool Headless { get; set; } = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED") != "1";
    public float Timeout { get; set; } = 10_000;
    /// <summary>Action delay in milliseconds. An explicit value (including zero) wins over
    /// CODEBRIX_PLAYTEST_SLOWMO; otherwise defaults to 250 in headed mode and zero headless.</summary>
    public float SlowMo { get => _slowMo ?? DefaultSlowMo(); set => _slowMo = value; }
    public string ArtifactsDirectory { get; set; } = Path.Combine("TestResults", "PlayTest");

    private float DefaultSlowMo()
    {
        var environment = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_SLOWMO");
        if (string.IsNullOrEmpty(environment)) return Headless ? 0 : 250;
        if (float.TryParse(environment, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay)
            && float.IsFinite(delay) && delay >= 0)
            return delay;
        throw new ArgumentException($"CODEBRIX_PLAYTEST_SLOWMO must be a finite, non-negative number of milliseconds; received '{environment}'.");
    }

    /// <summary>Resolves code, environment, project metadata, then the landscape fallback.</summary>
    public ScreenOrientation ResolveOrientation()
    {
        if (_orientation is { } explicitOrientation)
        {
            if (!Enum.IsDefined(explicitOrientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
            return explicitOrientation;
        }
        var environment = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_ORIENTATION");
        if (!string.IsNullOrEmpty(environment))
            return OrientationPreference.Parse(environment, "CODEBRIX_PLAYTEST_ORIENTATION");
        var assembly = ConfigurationAssembly ?? Assembly.GetEntryAssembly();
        var preference = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(a => a.Key == "CodeBrixPlayTestPreferredOrientation")?.Value;
        return string.IsNullOrEmpty(preference) ? ScreenOrientation.Landscape
            : OrientationPreference.Parse(preference, "CodeBrixPlayTestPreferredOrientation");
    }

    /// <summary>Resolves the simulated OS theme: environment, project metadata, then Light.</summary>
    public ApplicationTheme ResolveTheme()
    {
        var environment = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_THEME");
        if (!string.IsNullOrEmpty(environment))
            return ParseTheme(environment, "CODEBRIX_PLAYTEST_THEME");
        var assembly = ConfigurationAssembly ?? Assembly.GetEntryAssembly();
        var preference = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(a => a.Key == "CodeBrixPlayTestPreferredTheme")?.Value;
        return string.IsNullOrEmpty(preference) ? ApplicationTheme.Light
            : ParseTheme(preference, "CodeBrixPlayTestPreferredTheme");
    }

    private static ApplicationTheme ParseTheme(string value, string source)
    {
        if (string.Equals(value, "light", StringComparison.OrdinalIgnoreCase)) return ApplicationTheme.Light;
        if (string.Equals(value, "dark", StringComparison.OrdinalIgnoreCase)) return ApplicationTheme.Dark;
        throw new ArgumentException($"{source} must be Light or Dark; received '{value}'.");
    }
}

public sealed class PlayTestException : Exception
{
    public PlayTestException(string message) : base(message) { }
    public PlayTestException(string message, Exception inner) : base(message, inner) { }
}

public sealed class PlayTestApplication : IAsyncDisposable
{
    private static int _launched;
    private readonly VirtualHost _host;
    private PreviewConnection _preview;
    private Task _run;
    private int _disposed;
    internal PlayTestOptions Options { get; }
    public Page Page { get; }
    public PlayTestFilePickers FilePickers { get; } = new();
    public int Width => _host.Width;
    public int Height => _host.Height;
    public bool Headless => Options.Headless;
    /// <summary>The simulated OS theme, resolved before app construction and fixed for this run.
    /// An application may explicitly choose its own requested theme.</summary>
    public ApplicationTheme SystemTheme { get; }
    /// <summary>The launch preference used by tests with no explicit orientation.</summary>
    public ScreenOrientation PreferredOrientation => Options.Orientation;
    /// <summary>The orientation of the current virtual screen.</summary>
    public ScreenOrientation Orientation => _host.Orientation;
    internal VirtualHost Host => _host;

    private PlayTestApplication(Func<Application> factory, PlayTestOptions options, ApplicationTheme theme)
    {
        Options = options;
        SystemTheme = theme;
        _host = new VirtualHost(factory, options.Orientation == ScreenOrientation.Portrait, theme, FilePickers);
        Page = new Page(this);
    }

    public static async Task<PlayTestApplication> LaunchAsync(Func<Application> application, PlayTestOptions options = null)
    {
        ArgumentNullException.ThrowIfNull(application);
        options ??= new PlayTestOptions();
        var orientation = options.ResolveOrientation();
        var theme = options.ResolveTheme();
        var slowMo = options.SlowMo;
        if (options.Timeout <= 0 || !float.IsFinite(options.Timeout) || slowMo < 0 || !float.IsFinite(slowMo))
            throw new ArgumentOutOfRangeException(nameof(options));
        if (Interlocked.Exchange(ref _launched, 1) != 0)
            throw new InvalidOperationException("PlayTest hosts one application per process. Share an application fixture and reset the page between tests; use separate processes for independent applications.");
        options = new PlayTestOptions
        {
            Orientation = orientation, Headless = options.Headless,
            Timeout = options.Timeout, SlowMo = slowMo, ArtifactsDirectory = options.ArtifactsDirectory,
        };
        var result = new PlayTestApplication(application, options, theme);
        try
        {
            // Stable default culture; an app or a test can explicitly exercise other cultures.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            if (!options.Headless)
            {
                result._preview = await PreviewConnection.StartAsync(result.Width, result.Height).ConfigureAwait(false);
                result._host.FramePresented += result._preview.Present;
            }
            result._run = Task.Run(result._host.Run);
            await result._host.ReadyAsync().ConfigureAwait(false);
            await result._host.CaptureAsync().ConfigureAwait(false);
            return result;
        }
        catch { await result.DisposeAsync().ConfigureAwait(false); throw; }
    }

    // A C# counterpart to evaluating application state: evaluation is marshalled onto
    // the actual UI thread. Tests should use locators for user actions.
    public Task<T> EvaluateAsync<T>(Func<T> expression) => _host.OnUI(expression);
    public Task EvaluateAsync(Action action) => _host.OnUI(action);

    /// <summary>Changes the virtual screen and waits for layout/rendering. Null restores the launch preference.
    /// Call between serialized tests, never concurrently with actions or screenshots.</summary>
    public async Task SetOrientationAsync(ScreenOrientation? orientation = null)
    {
        var selected = orientation ?? PreferredOrientation;
        if (!Enum.IsDefined(selected)) throw new ArgumentOutOfRangeException(nameof(orientation));
        await EvaluateAsync(() => _host.SetOrientation(selected)).ConfigureAwait(false);
        await _host.CaptureAsync().ConfigureAwait(false);
    }

    public async Task<T> WaitForAsync<T>(Func<T> probe, Func<T, bool> predicate, float? timeout = null, string description = "application outcome")
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(predicate);
        var limit = timeout ?? Options.Timeout;
        if (limit <= 0 || !float.IsFinite(limit)) throw new ArgumentOutOfRangeException(nameof(timeout));
        var elapsed = Stopwatch.StartNew();
        T actual = default;
        do
        {
            actual = await EvaluateAsync(probe).ConfigureAwait(false);
            if (predicate(actual)) return actual;
            await Task.Delay(25).ConfigureAwait(false);
        } while (elapsed.Elapsed.TotalMilliseconds < limit);
        throw await FailureAsync($"Timed out waiting for {description}. Last observed value: {actual}").ConfigureAwait(false);
    }

    internal async Task SlowAsync()
    {
        if (Options.SlowMo > 0) await Task.Delay(TimeSpan.FromMilliseconds(Options.SlowMo)).ConfigureAwait(false);
    }

    internal async Task<PlayTestException> FailureAsync(string message)
    {
        try
        {
            var path = Path.GetFullPath(Path.Combine(Options.ArtifactsDirectory, $"failure-{Guid.NewGuid():N}.png"));
            await Page.ScreenshotAsync(new() { Path = path }).ConfigureAwait(false);
            message += "\nScreenshot: " + path;
            message += "\nUI: " + await Page.DescribeAsync().ConfigureAwait(false);
        }
        catch (Exception e) { message += "\nDiagnostics unavailable: " + e.Message; }
        return new PlayTestException(message);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _host.Dispose();
        if (_preview != null) await _preview.DisposeAsync().ConfigureAwait(false);
        if (_run != null) await _run.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
    }
}

// Runner-neutral: xUnit, NUnit and MSTest fixtures may all use the same API.
public abstract class PageTest
{
    protected PageTest(PlayTestApplication application) => Page = application.Page;
    public Page Page { get; }
    protected static LocatorAssertions Expect(Locator locator) => Assertions.Expect(locator);
}
