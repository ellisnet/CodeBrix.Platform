using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace CodeBrix.Platform.PlayTest;

/// <summary>The application's single virtual page: locators, page content, keyboard/mouse input,
/// screenshots and the isolated clipboard. Obtain it from <see cref="PlayTestApplication.Page"/>.</summary>
public sealed class Page
{
    internal PlayTestApplication Application { get; }

    /// <summary>Synthetic keyboard input to the focused element.</summary>
    public Keyboard Keyboard { get; }

    /// <summary>Synthetic mouse input at virtual-screen coordinates.</summary>
    public Mouse Mouse { get; }
    internal Page(PlayTestApplication application)
    {
        Application = application;
        Keyboard = new Keyboard(application);
        Mouse = new Mouse(application);
    }

    /// <summary>Finds elements by ARIA role (derived from the control type or its automation peer) and,
    /// optionally, accessible name. Includes open popups and flyouts.</summary>
    /// <param name="role">The role to match.</param>
    /// <param name="options">Optional name, exact-match and hidden-element filters.</param>
    /// <returns>A lazy locator.</returns>
    public Locator GetByRole(AriaRole role, PageGetByRoleOptions? options = null) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => VisualTree.Role(e) == role
            && (options?.IncludeHidden == true || VisualTree.Visible(e))
            && (options?.Name == null || VisualTree.Matches(VisualTree.Name(e), options.Name, options.Exact == true))
            && (options?.NameRegex == null || options.NameRegex.IsMatch(VisualTree.Name(e)))),
        $"GetByRole({role}, Name = {options?.Name ?? options?.NameRegex?.ToString() ?? "*"})");

    /// <summary>Finds visible elements whose <c>AutomationProperties.AutomationId</c> equals <paramref name="testId"/>.</summary>
    /// <param name="testId">The automation ID.</param>
    /// <param name="options">Optional hidden-element filter.</param>
    /// <returns>A lazy locator.</returns>
    public Locator GetByTestId(string testId, PageGetByTestIdOptions? options = null) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => AutomationProperties.GetAutomationId(e) == testId
            && (options?.IncludeHidden == true || VisualTree.Visible(e))),
        $"GetByTestId(\"{testId}\")");

    /// <summary>Finds controls by an application-owned type without requiring PlayTest
    /// to reference the library declaring that type. Resolution remains lazy.</summary>
    /// <typeparam name="T">The element type to match.</typeparam>
    /// <param name="includeHidden">True also matches attached elements that are not visible.</param>
    /// <returns>A lazy locator.</returns>
    public Locator GetByType<T>(bool includeHidden = false) where T : UIElement => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).OfType<T>()
            .Where(e => includeHidden || VisualTree.Visible(e)), $"GetByType<{typeof(T).Name}>()");

    /// <summary>Finds the innermost visible text blocks and string-content controls whose text matches
    /// <paramref name="text"/> (case-insensitive substring unless <see cref="PageGetByTextOptions.Exact"/>).</summary>
    /// <param name="text">The text to match.</param>
    /// <param name="options">Optional exact matching and hidden-element filter.</param>
    /// <returns>A lazy locator.</returns>
    public Locator GetByText(string text, PageGetByTextOptions? options = null) => TextLocator(
        value => VisualTree.Matches(value, text, options?.Exact == true), options?.IncludeHidden == true, $"GetByText(\"{text}\")");

    /// <summary>Finds the innermost visible text blocks and string-content controls whose text matches a pattern.</summary>
    /// <param name="text">The pattern to match.</param>
    /// <returns>A lazy locator.</returns>
    public Locator GetByText(Regex text) => TextLocator(text.IsMatch, false, $"GetByText(/{text}/)");
    private Locator TextLocator(Func<string, bool> matches, bool includeHidden, string description) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => e is TextBlock || e is ContentControl { Content: string })
            .Where(e => matches(VisualTree.Text(e)) && (includeHidden || VisualTree.Visible(e)))
            .Where(e => !VisualTree.Walk(e).Any(child => child != e && child is TextBlock && matches(VisualTree.Text(child))
                && (includeHidden || VisualTree.Visible(child)))), description);

    /// <summary>Finds visible controls whose accessible name (AutomationProperties.Name, LabeledBy, or the
    /// automation peer's name) matches <paramref name="text"/>.</summary>
    /// <param name="text">The label to match.</param>
    /// <param name="options">Optional exact matching and hidden-element filter.</param>
    /// <returns>A lazy locator.</returns>
    public Locator GetByLabel(string text, PageGetByTextOptions? options = null) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => e is Control
            && VisualTree.Matches(VisualTree.Name(e), text, options?.Exact == true)
            && (options?.IncludeHidden == true || VisualTree.Visible(e))), $"GetByLabel(\"{text}\")");

    /// <summary>Changes the retry timeout used by every later action and assertion that has no explicit timeout.</summary>
    /// <param name="timeout">Milliseconds; must be positive and finite.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is not positive and finite.</exception>
    public void SetDefaultTimeout(float timeout)
    {
        if (timeout <= 0 || !float.IsFinite(timeout)) throw new ArgumentOutOfRangeException(nameof(timeout));
        Application.Options.Timeout = timeout;
    }

    /// <summary>Runs <paramref name="expression"/> on the application's UI thread and returns its result.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="expression">Code that reads application state.</param>
    /// <returns>The expression's result.</returns>
    public Task<T> EvaluateAsync<T>(Func<T> expression) => Application.EvaluateAsync(expression);

    /// <summary>Runs <paramref name="expression"/> on the application's UI thread.</summary>
    /// <param name="expression">Code that reads or prepares application state.</param>
    /// <returns>A task that completes when the code has run.</returns>
    public Task EvaluateAsync(Action expression) => Application.EvaluateAsync(expression);

    /// <summary>Installs a fresh page using the fixture's launch preference.</summary>
    /// <param name="content">Factory for the new window content, invoked on the UI thread.</param>
    /// <returns>A task that completes after the new page has rendered.</returns>
    public Task SetContentAsync(Func<UIElement> content) => SetContentAsync(content, null);

    /// <summary>Installs a fresh page in the required orientation. Null restores the fixture's launch preference.
    /// Orientation is applied before the page factory runs. Shared fixtures must be serialized.
    /// Keys still held with <see cref="Keyboard.DownAsync"/> are released (key-up events go to the old page),
    /// a minimized or closed window is shown again, open popups are closed, the previous page's disposable
    /// DataContext (also inside a Frame) is disposed, and the isolated clipboard is cleared.</summary>
    /// <param name="content">Factory for the new window content, invoked on the UI thread.</param>
    /// <param name="orientation">The orientation to apply first, or null for the launch preference.</param>
    /// <returns>A task that completes after the new page has rendered.</returns>
    public async Task SetContentAsync(Func<UIElement> content, ScreenOrientation? orientation)
    {
        await using var step = Recording.PlayTestRecording.Step(Application, "SetContent");
        ArgumentNullException.ThrowIfNull(content);
        var selected = orientation ?? Application.PreferredOrientation;
        if (!Enum.IsDefined(selected)) throw new ArgumentOutOfRangeException(nameof(orientation));
        await Application.EvaluateAsync(() =>
        {
            var window = Application.Host.Window.ManagedWindow
                ?? throw new PlayTestException("The application has not created its window.");
            // Keys held with Keyboard.DownAsync are released while the old page still has focus,
            // and a minimized or closed window is shown again, so nothing leaks into the next test.
            Keyboard.ReleaseHeldKeys();
            Application.Host.Window.ShowForNextTest();
            if (window.Content?.XamlRoot is { } root)
                foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root).ToArray()) popup.IsOpen = false;
            // Application startup commonly hosts its page inside a navigation Frame.
            // Dispose that page's view model too, so its timers/streams do not outlive a reset.
            var previous = window.Content;
            while (previous is Frame { Content: UIElement child }) previous = child;
            if (previous is FrameworkElement { DataContext: IDisposable disposable }) disposable.Dispose();
            window.Content = null;
            Application.Host.SetOrientation(selected);
            window.Content = content();
            Clipboard.Clear();
        }).ConfigureAwait(false);
        await Application.Host.CaptureAsync().ConfigureAwait(false);
    }

    /// <summary>Captures the current virtual screen (the application's own pixels, never the preview
    /// window or the desktop) as a PNG.</summary>
    /// <param name="options">Optional file path to also write the PNG to, and a stable capture that waits
    /// for animations and transitions to settle.</param>
    /// <returns>The PNG bytes.</returns>
    /// <exception cref="PlayTestException">A stable capture kept changing until the timeout.</exception>
    public async Task<byte[]> ScreenshotAsync(PageScreenshotOptions? options = null)
    {
        var limit = options?.Timeout ?? Application.Options.Timeout;
        if (limit <= 0 || !float.IsFinite(limit)) throw new ArgumentOutOfRangeException(nameof(options), "The timeout must be positive and finite.");
        var bytes = await Screenshots.CaptureAsync(Application, null, options?.Stable == true, Stopwatch.StartNew(), limit, "page").ConfigureAwait(false);
        if (options?.Path != null) await Screenshots.WriteAsync(options.Path, bytes).ConfigureAwait(false);
        return bytes;
    }

    /// <summary>Reads the isolated application clipboard, never the desktop clipboard.</summary>
    /// <returns>The clipboard text, or an empty string when it holds no text.</returns>
    public async Task<string> ClipboardTextAsync()
    {
        var view = await Application.EvaluateAsync(Clipboard.GetContent).ConfigureAwait(false);
        return view is { } content && content.Contains(StandardDataFormats.Text) ? await content.GetTextAsync() : "";
    }

    internal Task<string> DescribeAsync() => Application.EvaluateAsync(() => string.Join("\n",
        VisualTree.Walk(Application.Host.Root, includePopups: true).Where(VisualTree.Visible)
            .Where(e => e is Control || e is TextBlock)
            .Take(100).Select(e =>
            {
                var name = VisualTree.Name(e);
                if (name.Length > 200) name = name[..200] + "…";
                return $"{e.GetType().Name} role={VisualTree.Role(e)} id={AutomationProperties.GetAutomationId(e)} name={name}";
            })));
}
