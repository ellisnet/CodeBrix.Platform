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

/// <summary>The shape of the virtual screen.</summary>
public enum ScreenOrientation
{
    /// <summary>1920 x 1080 logical pixels.</summary>
    Landscape,
    /// <summary>1080 x 1920 logical pixels.</summary>
    Portrait,
}

/// <summary>Launch options for <see cref="PlayTestApplication.LaunchAsync"/>. Leave a preference unset to
/// let the command line, environment and project metadata decide; an explicit value always wins.</summary>
public sealed class PlayTestOptions
{
    private ScreenOrientation? _orientation;
    private ApplicationTheme? _theme;
    private float? _slowMo;
    /// <summary>An explicit orientation overrides command-line, environment and project preferences.</summary>
    public ScreenOrientation Orientation { get => _orientation ?? ScreenOrientation.Landscape; set => _orientation = value; }
    /// <summary>An explicit simulated system theme overrides command-line, environment and project preferences.</summary>
    public ApplicationTheme Theme { get => _theme ?? ApplicationTheme.Light; set => _theme = value; }
    /// <summary>The test assembly containing project preferences; defaults to the entry assembly.</summary>
    public Assembly? ConfigurationAssembly { get; set; }
    /// <summary>An explicit value overrides the runner's --headed/--headless option,
    /// then CODEBRIX_PLAYTEST_HEADED. The fallback is headless.</summary>
    public bool Headless { get; set; } =
        AppContext.GetData("CodeBrix.Platform.PlayTest.CommandLineHeadless") is bool headless
            ? headless : Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_HEADED") != "1";

    /// <summary>Default retry limit for actions and assertions, in milliseconds (10 seconds unless set).</summary>
    public float Timeout { get; set; } = 10_000;

    /// <summary>Action delay in milliseconds. An explicit value (including zero) wins over
    /// CODEBRIX_PLAYTEST_SLOWMO; otherwise defaults to 250 in headed mode and zero headless.</summary>
    public float SlowMo { get => _slowMo ?? DefaultSlowMo(); set => _slowMo = value; }

    /// <summary>Folder for failure screenshots, relative to the working directory unless rooted;
    /// <c>TestResults/PlayTest</c> by default.</summary>
    public string ArtifactsDirectory { get; set; } = Path.Combine("TestResults", "PlayTest");

    /// <summary>Whether OpenGL is offered to the application: <see cref="PlayTestOpenGL.Available"/> (the
    /// default) gives OpenGL elements real contexts from the registered provider; <see cref="PlayTestOpenGL.Unavailable"/>
    /// launches without OpenGL so the application's no-OpenGL path runs. A launch option: it applies to the
    /// whole run, because OpenGL elements keep their context for the life of the window.</summary>
    public PlayTestOpenGL OpenGL { get; set; }

    private float DefaultSlowMo()
    {
        var environment = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_SLOWMO");
        if (string.IsNullOrEmpty(environment)) return Headless ? 0 : 250;
        if (float.TryParse(environment, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay)
            && float.IsFinite(delay) && delay >= 0)
            return delay;
        throw new ArgumentException($"CODEBRIX_PLAYTEST_SLOWMO must be a finite, non-negative number of milliseconds; received '{environment}'.");
    }

    /// <summary>Resolves code, command line, environment, project metadata, then the landscape fallback.</summary>
    /// <returns>The orientation the application will launch in.</returns>
    /// <exception cref="ArgumentException">The selected command-line, environment or project value is not Landscape or Portrait.</exception>
    public ScreenOrientation ResolveOrientation()
    {
        if (_orientation is { } explicitOrientation)
        {
            if (!Enum.IsDefined(explicitOrientation)) throw new ArgumentOutOfRangeException(nameof(Orientation));
            return explicitOrientation;
        }
        if (AppContext.GetData("CodeBrix.Platform.PlayTest.CommandLineOrientation") is string commandLine)
            return OrientationPreference.Parse(commandLine, "--orientation");
        var environment = Environment.GetEnvironmentVariable("CODEBRIX_PLAYTEST_ORIENTATION");
        if (!string.IsNullOrEmpty(environment))
            return OrientationPreference.Parse(environment, "CODEBRIX_PLAYTEST_ORIENTATION");
        var assembly = ConfigurationAssembly ?? Assembly.GetEntryAssembly();
        var preference = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(a => a.Key == "CodeBrixPlayTestPreferredOrientation")?.Value;
        return string.IsNullOrEmpty(preference) ? ScreenOrientation.Landscape
            : OrientationPreference.Parse(preference, "CodeBrixPlayTestPreferredOrientation");
    }

    /// <summary>Resolves the simulated OS theme: code, command line, environment, project metadata, then Light.</summary>
    /// <returns>The simulated system theme the application will launch with.</returns>
    /// <exception cref="ArgumentException">The selected command-line, environment or project value is not Light or Dark.</exception>
    public ApplicationTheme ResolveTheme()
    {
        if (_theme is { } explicitTheme)
        {
            if (!Enum.IsDefined(explicitTheme)) throw new ArgumentOutOfRangeException(nameof(Theme));
            return explicitTheme;
        }
        if (AppContext.GetData("CodeBrix.Platform.PlayTest.CommandLineTheme") is string commandLine)
            return ParseTheme(commandLine, "--theme");
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

/// <summary>A PlayTest failure: a timeout (whose message names a failure screenshot and describes the
/// visible UI), strict-mode violation, unsupported element, or application/renderer crash.</summary>
public sealed class PlayTestException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What failed.</param>
    public PlayTestException(string message) : base(message) { }

    /// <summary>Creates the exception with a message and its cause.</summary>
    /// <param name="message">What failed.</param>
    /// <param name="inner">The underlying exception.</param>
    public PlayTestException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>The application under test, running offscreen on a virtual Skia screen with its own UI
/// thread. One application per process: share it through a fixture and dispose it when the suite ends.</summary>
public sealed class PlayTestApplication : IAsyncDisposable
{
    private static int _launched;
    private readonly VirtualHost _host;
    private PreviewConnection? _preview;
    private Task? _run;
    private int _disposed;
    internal static PlayTestApplication? Current { get; private set; }
    internal Type? ApplicationType { get; private set; }
    internal PlayTestOptions Options { get; }

    /// <summary>The application's page: locators, content, input and screenshots.</summary>
    public Page Page { get; }

    /// <summary>Scripted responses for the application's folder, open-file and save-file pickers.</summary>
    public PlayTestFilePickers FilePickers { get; } = new();

    /// <summary>Records the application's <c>Windows.System.Launcher</c> requests (links, files, folders)
    /// without starting any process.</summary>
    public PlayTestLauncher Launcher { get; } = new();

    /// <summary>The window the application itself created (its <c>Window</c>, with the content it set),
    /// for suites that launch once and read the application's own page. Read or change it inside
    /// <see cref="EvaluateAsync{T}"/>.</summary>
    /// <exception cref="PlayTestException">The application has not created its window.</exception>
    public Window Window => _host.Window.ManagedWindow ?? throw new PlayTestException("The application has not created its window.");

    /// <summary>Current virtual-screen width in logical pixels (1920 landscape, 1080 portrait).</summary>
    public int Width => _host.Width;

    /// <summary>Current virtual-screen height in logical pixels (1080 landscape, 1920 portrait).</summary>
    public int Height => _host.Height;

    /// <summary>True when no preview window is shown; resolved once at launch.</summary>
    public bool Headless => Options.Headless;
    /// <summary>The simulated OS theme, resolved before app construction and fixed for this run.
    /// An application may explicitly choose its own requested theme.</summary>
    public ApplicationTheme SystemTheme { get; }
    /// <summary>The launch preference used by tests with no explicit orientation.</summary>
    public ScreenOrientation PreferredOrientation => Options.Orientation;
    /// <summary>The orientation of the current virtual screen.</summary>
    public ScreenOrientation Orientation => _host.Orientation;
    /// <summary>What the launch found out about OpenGL: the provider, renderer and version of a probe context,
    /// or why OpenGL elements get no context in this run.</summary>
    public PlayTestOpenGLInfo OpenGL => _host.OpenGL ?? throw new PlayTestException("The application has not started.");
    internal VirtualHost Host => _host;

    private PlayTestApplication(Func<Application> factory, PlayTestOptions options, ApplicationTheme theme, PlayTestOpenGLPlan openGL)
    {
        Options = options;
        SystemTheme = theme;
        _host = new VirtualHost(factory, options.Orientation == ScreenOrientation.Portrait, theme, FilePickers, Launcher, openGL);
        Page = new Page(this);
    }

    /// <summary>Starts the application offscreen and waits for its first rendered frame. Preferences are
    /// resolved and validated once; a headed run also starts the view-only preview process.</summary>
    /// <param name="application">Factory for the application's <c>Application</c> subclass, invoked on the UI thread.</param>
    /// <param name="options">Optional launch options.</param>
    /// <returns>The running application.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The timeout or action delay is not positive/non-negative and finite.</exception>
    /// <exception cref="ArgumentException">A selected preference value is invalid.</exception>
    /// <exception cref="InvalidOperationException">An application was already launched in this process.</exception>
    /// <exception cref="PlayTestException">The registered OpenGL provider could not create a context; the message
    /// gives its reason.</exception>
    public static async Task<PlayTestApplication> LaunchAsync(Func<Application> application, PlayTestOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(application);
        options ??= new PlayTestOptions();
        var orientation = options.ResolveOrientation();
        var theme = options.ResolveTheme();
        var slowMo = options.SlowMo;
        if (options.Timeout <= 0 || !float.IsFinite(options.Timeout) || slowMo < 0 || !float.IsFinite(slowMo))
            throw new ArgumentOutOfRangeException(nameof(options));
        var openGL = PlayTestOpenGLSetup.Plan(options.OpenGL, PlayTestOpenGLProviders.Registry);
        if (Interlocked.Exchange(ref _launched, 1) != 0)
            throw new InvalidOperationException("PlayTest hosts one application per process. Share an application fixture and reset the page between tests; use separate processes for independent applications.");
        options = new PlayTestOptions
        {
            Orientation = orientation, Headless = options.Headless,
            Timeout = options.Timeout, SlowMo = slowMo, ArtifactsDirectory = options.ArtifactsDirectory,
            OpenGL = options.OpenGL,
        };
        var result = new PlayTestApplication(application, options, theme, openGL);
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
            result.ApplicationType = await result._host.OnUI(() => Application.Current.GetType()).ConfigureAwait(false);
            Current = result;
            return result;
        }
        catch { await result.DisposeAsync().ConfigureAwait(false); throw; }
    }

    // A C# counterpart to evaluating application state: evaluation is marshalled onto
    // the actual UI thread. Tests should use locators for user actions.

    /// <summary>Runs <paramref name="expression"/> on the application's UI thread and returns its result.
    /// Use it to read state; use locators for user actions.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="expression">Code that reads application state.</param>
    /// <returns>The expression's result.</returns>
    public async Task<T> EvaluateAsync<T>(Func<T> expression)
    {
        await using var step = Recording.PlayTestRecording.Step(this, "Evaluate");
        return await _host.OnUI(expression).ConfigureAwait(false);
    }
    /// <summary>Runs <paramref name="action"/> on the application's UI thread.</summary>
    /// <param name="action">Code that reads or prepares application state.</param>
    /// <returns>A task that completes when the code has run.</returns>
    public async Task EvaluateAsync(Action action)
    {
        await using var step = Recording.PlayTestRecording.Step(this, "Evaluate");
        await _host.OnUI(action).ConfigureAwait(false);
    }

    /// <summary>Changes the virtual screen and waits for layout/rendering. Null restores the launch preference.
    /// Call between serialized tests, never concurrently with actions or screenshots.</summary>
    /// <param name="orientation">The orientation to apply, or null for the launch preference.</param>
    /// <returns>A task that completes after the re-laid-out page has rendered.</returns>
    public async Task SetOrientationAsync(ScreenOrientation? orientation = null)
    {
        await using var step = Recording.PlayTestRecording.Step(this, "SetOrientation");
        var selected = orientation ?? PreferredOrientation;
        if (!Enum.IsDefined(selected)) throw new ArgumentOutOfRangeException(nameof(orientation));
        await EvaluateAsync(() => _host.SetOrientation(selected)).ConfigureAwait(false);
        await _host.CaptureAsync().ConfigureAwait(false);
    }

    /// <summary>Asks the window to close, as the user clicking its close button does: the application's
    /// <c>AppWindow.Closing</c> handlers run and may cancel, then <c>Window.Closed</c> handlers run and may
    /// cancel by marking the event handled. When nothing cancels, the window raises its closing events and
    /// becomes hidden (<c>Window.VisibilityChanged</c>) but the process and the application keep running:
    /// the next <see cref="Page.SetContentAsync(Func{UIElement}, ScreenOrientation?)"/> shows the window again.</summary>
    /// <returns>True when the window closed; false when the application cancelled the close.</returns>
    public async Task<bool> RequestCloseAsync()
    {
        await using var step = Recording.PlayTestRecording.Step(this, "RequestClose");
        var closed = await _host.OnUI(_host.Window.RequestClose).ConfigureAwait(false);
        await _host.CaptureAsync().ConfigureAwait(false);
        return closed;
    }

    /// <summary>Minimizes the window, as the desktop heads report it: the window is deactivated
    /// (<c>Window.Activated</c> with Deactivated) and becomes hidden (<c>Window.VisibilityChanged</c> with
    /// Visible false), so pause-on-hidden logic runs. Rendering continues, so screenshots still show the page.</summary>
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task MinimizeAsync()
    {
        await using var step = Recording.PlayTestRecording.Step(this, "Minimize");
        await _host.OnUI(_host.Window.Minimize).ConfigureAwait(false);
        await _host.CaptureAsync().ConfigureAwait(false);
    }

    /// <summary>Restores a minimized window: it becomes visible (<c>Window.VisibilityChanged</c> with Visible
    /// true) and is activated again. Does nothing when the window is not minimized.</summary>
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task RestoreAsync()
    {
        await using var step = Recording.PlayTestRecording.Step(this, "Restore");
        await _host.OnUI(_host.Window.Restore).ConfigureAwait(false);
        await _host.CaptureAsync().ConfigureAwait(false);
    }

    /// <summary>Polls <paramref name="probe"/> on the UI thread until <paramref name="predicate"/> accepts its
    /// value; use it for non-visual outcomes such as a service call completing.</summary>
    /// <typeparam name="T">The probed value's type.</typeparam>
    /// <param name="probe">Reads the value, on the UI thread.</param>
    /// <param name="predicate">Decides whether the value is the expected outcome.</param>
    /// <param name="timeout">Milliseconds; null uses the application timeout.</param>
    /// <param name="description">Names the outcome in the failure message.</param>
    /// <returns>The first accepted value.</returns>
    /// <exception cref="PlayTestException">No accepted value before the timeout; the message includes the last value.</exception>
    public async Task<T> WaitForAsync<T>(Func<T> probe, Func<T, bool> predicate, float? timeout = null, string description = "application outcome")
    {
        await using var step = Recording.PlayTestRecording.Step(this, "WaitFor", description);
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(predicate);
        var limit = timeout ?? Options.Timeout;
        if (limit <= 0 || !float.IsFinite(limit)) throw new ArgumentOutOfRangeException(nameof(timeout));
        var elapsed = Stopwatch.StartNew();
        T? actual = default;
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

    /// <summary>Captures a final recording screenshot if one is due, releases keys still held with
    /// <see cref="Keyboard.DownAsync"/>, then stops the application, its renderer and any preview process.
    /// Safe to call more than once.</summary>
    /// <returns>A task that completes when everything has stopped.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try
        {
            await Recording.PlayTestRecording.BeforeApplicationDisposedAsync(this).ConfigureAwait(false);
            // Held keys get their key-up events while the application can still handle them.
            try { await _host.OnUI(Page.Keyboard.ReleaseHeldKeys).ConfigureAwait(false); }
            catch (Exception) { /* A failed or stopped application cannot receive them. */ }
        }
        finally
        {
            if (ReferenceEquals(Current, this)) Current = null;
            _host.Dispose();
            if (_preview != null) await _preview.DisposeAsync().ConfigureAwait(false);
            if (_run != null) await _run.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
    }
}

// Runner-neutral: xUnit, NUnit and MSTest fixtures may all use the same API.

/// <summary>Optional base class for test classes: supplies <see cref="Page"/> and <see cref="Expect"/>
/// without depending on a particular test runner.</summary>
public abstract class PageTest
{
    /// <summary>Binds the test class to the shared application.</summary>
    /// <param name="application">The fixture's running application.</param>
    protected PageTest(PlayTestApplication application) => Page = application.Page;

    /// <summary>The shared application's page.</summary>
    public Page Page { get; }

    /// <summary>Starts a retrying assertion; same as <see cref="Assertions.Expect"/>.</summary>
    /// <param name="locator">The element(s) to assert on.</param>
    /// <returns>The assertion builder.</returns>
    protected static LocatorAssertions Expect(Locator locator) => Assertions.Expect(locator);
}
