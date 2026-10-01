using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.ApplicationModel.DataTransfer;

namespace CodeBrix.Platform.PlayTest;

public sealed class Page
{
    internal PlayTestApplication Application { get; }
    public Keyboard Keyboard { get; }
    public Mouse Mouse { get; }
    internal Page(PlayTestApplication application)
    {
        Application = application;
        Keyboard = new Keyboard(application);
        Mouse = new Mouse(application);
    }

    public Locator GetByRole(AriaRole role, PageGetByRoleOptions options = null) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => VisualTree.Role(e) == role
            && (options?.IncludeHidden == true || VisualTree.Visible(e))
            && (options?.Name == null || VisualTree.Matches(VisualTree.Name(e), options.Name, options.Exact == true))
            && (options?.NameRegex == null || options.NameRegex.IsMatch(VisualTree.Name(e)))),
        $"GetByRole({role}, Name = {options?.Name ?? options?.NameRegex?.ToString() ?? "*"})");

    public Locator GetByTestId(string testId) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => AutomationProperties.GetAutomationId(e) == testId),
        $"GetByTestId(\"{testId}\")");

    /// <summary>Finds controls by an application-owned type without requiring PlayTest
    /// to reference the library declaring that type. Resolution remains lazy.</summary>
    public Locator GetByType<T>(bool includeHidden = false) where T : UIElement => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).OfType<T>()
            .Where(e => includeHidden || VisualTree.Visible(e)), $"GetByType<{typeof(T).Name}>()");

    public Locator GetByText(string text, PageGetByTextOptions options = null) => TextLocator(
        value => VisualTree.Matches(value, text, options?.Exact == true), $"GetByText(\"{text}\")");
    public Locator GetByText(Regex text) => TextLocator(text.IsMatch, $"GetByText(/{text}/)");
    private Locator TextLocator(Func<string, bool> matches, string description) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => e is TextBlock || e is ContentControl { Content: string })
            .Where(e => matches(VisualTree.Text(e)))
            .Where(e => !VisualTree.Walk(e).Any(child => child != e && child is TextBlock && matches(VisualTree.Text(child)))), description);

    public Locator GetByLabel(string text, PageGetByTextOptions options = null) => new(this,
        () => VisualTree.Walk(Application.Host.Root, includePopups: true).Where(e => e is Control &&
            VisualTree.Matches(VisualTree.Name(e), text, options?.Exact == true)), $"GetByLabel(\"{text}\")");

    public void SetDefaultTimeout(float timeout)
    {
        if (timeout <= 0 || !float.IsFinite(timeout)) throw new ArgumentOutOfRangeException(nameof(timeout));
        Application.Options.Timeout = timeout;
    }

    public Task<T> EvaluateAsync<T>(Func<T> expression) => Application.EvaluateAsync(expression);
    public Task EvaluateAsync(Action expression) => Application.EvaluateAsync(expression);

    /// <summary>Installs a fresh page using the fixture's launch preference.</summary>
    public Task SetContentAsync(Func<UIElement> content) => SetContentAsync(content, null);

    /// <summary>Installs a fresh page in the required orientation. Null restores the fixture's launch preference.
    /// Orientation is applied before the page factory runs. Shared fixtures must be serialized.</summary>
    public async Task SetContentAsync(Func<UIElement> content, ScreenOrientation? orientation)
    {
        await using var step = Recording.PlayTestRecording.Step(Application, "SetContent");
        ArgumentNullException.ThrowIfNull(content);
        var selected = orientation ?? Application.PreferredOrientation;
        if (!Enum.IsDefined(selected)) throw new ArgumentOutOfRangeException(nameof(orientation));
        await Application.EvaluateAsync(() =>
        {
            var window = Application.Host.Window.ManagedWindow;
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(window.Content.XamlRoot).ToArray()) popup.IsOpen = false;
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

    public async Task<byte[]> ScreenshotAsync(PageScreenshotOptions options = null)
    {
        var frame = await Application.Host.CaptureAsync().ConfigureAwait(false);
        var pixels = frame.Pixels;
        var info = new SKImageInfo(frame.Width, frame.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();
        if (options?.Path != null)
        {
            var path = Path.GetFullPath(options.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
        }
        return bytes;
    }

    /// <summary>Reads the isolated application clipboard, never the desktop clipboard.</summary>
    public async Task<string> ClipboardTextAsync()
    {
        var view = await Application.EvaluateAsync(Clipboard.GetContent).ConfigureAwait(false);
        return view.Contains(StandardDataFormats.Text) ? await view.GetTextAsync() : "";
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
