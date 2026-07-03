using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;

// ReSharper disable CheckNamespace

namespace ParityDemo.Views;

/// <summary>
/// Regression page for the Wayland popup/flyout path (parity plan P1). Every control on the
/// left rides the shared Popup pipeline; the right side logs window-activation events,
/// XamlRoot changes and popup open/close so misbehavior is visible with timestamps.
/// Set PARITYDEMO_SELFTEST=1 to run the scripted popup checks and exit (results are written
/// to the PARITYDEMO_RESULTS file path, one PASS/FAIL line per check).
/// </summary>
public sealed partial class MainPage : Page
{
    private readonly StringBuilder _log = new();
    private bool _menuFlyoutOpen;
    private bool _hooked;

    public MainPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (!_hooked)
        {
            _hooked = true;

            if (App.MainWindowInstance is { } window)
            {
                window.Activated += (_, args) => Log($"Window.Activated: {args.WindowActivationState}");
            }

            if (XamlRoot is { } xamlRoot)
            {
                xamlRoot.Changed += (_, _) =>
                    Log($"XamlRoot.Changed: size={xamlRoot.Size.Width:0}x{xamlRoot.Size.Height:0} scale={xamlRoot.RasterizationScale:0.##}");
            }

            TestCombo.DropDownOpened += (_, _) => Log("ComboBox.DropDownOpened");
            TestCombo.DropDownClosed += (_, _) => Log("ComboBox.DropDownClosed");
            TestMenuFlyout.Opened += (_, _) => { _menuFlyoutOpen = true; Log("MenuFlyout.Opened"); };
            TestMenuFlyout.Closed += (_, _) => { _menuFlyoutOpen = false; Log("MenuFlyout.Closed"); };

            Log($"Loaded. scale={XamlRoot?.RasterizationScale:0.##} size={XamlRoot?.Size.Width:0}x{XamlRoot?.Size.Height:0}");

            if (Environment.GetEnvironmentVariable("PARITYDEMO_SELFTEST") == "1")
            {
                _ = RunSelfTestAsync();
            }
            else if (Environment.GetEnvironmentVariable("PARITYDEMO_CLIPTEST") == "1")
            {
                _ = RunClipboardTestAsync();
            }
            else if (Environment.GetEnvironmentVariable("PARITYDEMO_CHROMETEST") == "1")
            {
                _ = RunChromeTestAsync();
            }
        }
    }

    private void Log(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
        _log.AppendLine(line);
        Console.WriteLine($"PARITY|{line}");
        if (LogText != null)
        {
            LogText.Text = _log.ToString();
            LogScroller?.ChangeView(null, double.MaxValue, null, disableAnimation: true);
        }
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
        => Log($"MenuFlyoutItem clicked: '{(sender as MenuFlyoutItem)?.Text}'");

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        _log.Clear();
        LogText.Text = string.Empty;
    }

    private async void DialogButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Parity ContentDialog",
            Content = "If you can read this and dismiss it with the button, dialogs work.",
            CloseButtonText = "Close",
            XamlRoot = XamlRoot,
        };
        Log("ContentDialog.ShowAsync");
        _ = await dialog.ShowAsync();
        Log("ContentDialog closed");
    }

    private void SuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var text = sender.Text ?? string.Empty;
            sender.ItemsSource = new[] { "alpha", "bravo", "charlie", "delta", "echo" }
                .Where(s => s.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    private string DescribeOpenPopups()
    {
        var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot);
        if (popups.Count == 0)
        {
            return "none";
        }

        var parts = new List<string>();
        foreach (var popup in popups)
        {
            var child = popup.Child as FrameworkElement;
            parts.Add(child == null
                ? "(no child)"
                : $"{child.GetType().Name} {child.ActualWidth:0}x{child.ActualHeight:0}");
        }
        return string.Join("; ", parts);
    }

    // Tracks the injected mouse position; the injector's virtual mouse starts at (0,0) and
    // InjectedInputMouseInfo only carries deltas.
    private int _injectedX;
    private int _injectedY;

    private void InjectMoveTo(InputInjector injector, Point target)
    {
        var tx = (int)Math.Round(target.X);
        var ty = (int)Math.Round(target.Y);
        injector.InjectMouseInput(new[]
        {
            new InjectedInputMouseInfo
            {
                DeltaX = tx - _injectedX,
                DeltaY = ty - _injectedY,
                TimeOffsetInMilliseconds = 1,
                MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
            },
        });
        _injectedX = tx;
        _injectedY = ty;
    }

    private async Task InjectClickAsync(InputInjector injector, Point target)
    {
        InjectMoveTo(injector, target);
        await Task.Delay(100);
        injector.InjectMouseInput(new[]
        {
            new InjectedInputMouseInfo { TimeOffsetInMilliseconds = 1, MouseOptions = InjectedInputMouseOptions.LeftDown },
        });
        await Task.Delay(100);
        injector.InjectMouseInput(new[]
        {
            new InjectedInputMouseInfo { TimeOffsetInMilliseconds = 1, MouseOptions = InjectedInputMouseOptions.LeftUp },
        });
        await Task.Delay(100);
    }

    private static Point CenterInRoot(FrameworkElement element)
        => element.TransformToVisual(null)
            .TransformPoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2));

    private void DropZone_DragEnter(object sender, DragEventArgs e)
        => Log($"DropZone.DragEnter: formats [{string.Join(", ", e.DataView.AvailableFormats)}]");

    private void DropZone_DragLeave(object sender, DragEventArgs e)
        => Log("DropZone.DragLeave");

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        try
        {
            Log($"DropZone.Drop: formats [{string.Join(", ", e.DataView.AvailableFormats)}]");
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                Log($"  dropped items: [{string.Join(", ", items.Select(i => i.Path))}]");
            }
            if (e.DataView.Contains(StandardDataFormats.Text))
            {
                Log($"  dropped text: '{await e.DataView.GetTextAsync()}'");
            }
        }
        catch (Exception ex)
        {
            Log($"Drop failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Microsoft.UI.Windowing.OverlappedPresenter CurrentPresenter
        => App.MainWindowInstance.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;

    private void ToggleChrome_Click(object sender, RoutedEventArgs e)
    {
        var window = App.MainWindowInstance;
        window.ExtendsContentIntoTitleBar = !window.ExtendsContentIntoTitleBar;
        Log($"ExtendsContentIntoTitleBar = {window.ExtendsContentIntoTitleBar}");
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        CurrentPresenter?.Maximize();
        Log($"Maximize() requested; State={CurrentPresenter?.State}");
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        CurrentPresenter?.Restore();
        Log($"Restore() requested; State={CurrentPresenter?.State}");
    }

    private void PresenterState_Click(object sender, RoutedEventArgs e)
        => Log($"Presenter State = {CurrentPresenter?.State}");

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        Log("Window.Activate() requested");
        App.MainWindowInstance.Activate();
    }

    private async Task RunChromeTestAsync()
    {
        var results = new List<string>();
        void Check(string name, bool pass, string detail)
        {
            var line = $"{(pass ? "PASS" : "FAIL")} {name}{(string.IsNullOrEmpty(detail) ? "" : $" ({detail})")}";
            results.Add(line);
            Log($"CHROMETEST: {line}");
        }

        try
        {
            var window = App.MainWindowInstance;
            await Task.Delay(2000);

            // Hide the native decorations; an external screenshot verifies visually.
            window.ExtendsContentIntoTitleBar = true;
            Log("CHROMETEST: ExtendsContentIntoTitleBar=true (screenshot window now)");
            await Task.Delay(4000);
            Check("chrome-extend-no-crash", window.ExtendsContentIntoTitleBar, "");

            window.ExtendsContentIntoTitleBar = false;
            await Task.Delay(1500);

            CurrentPresenter?.Maximize();
            await Task.Delay(1500);
            Check("chrome-maximize-state", CurrentPresenter?.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized,
                $"State={CurrentPresenter?.State} size={XamlRoot?.Size.Width:0}x{XamlRoot?.Size.Height:0}");

            CurrentPresenter?.Restore();
            await Task.Delay(1500);
            Check("chrome-restore-state", CurrentPresenter?.State == Microsoft.UI.Windowing.OverlappedPresenterState.Restored,
                $"State={CurrentPresenter?.State} size={XamlRoot?.Size.Width:0}x{XamlRoot?.Size.Height:0}");

            window.Activate();
            await Task.Delay(750);
            Check("chrome-activate-no-crash", true, "");
        }
        catch (Exception e)
        {
            results.Add($"FAIL chrometest-exception ({e.GetType().Name}: {e.Message})");
            Log($"CHROMETEST: exception {e}");
        }

        var resultsPath = Environment.GetEnvironmentVariable("PARITYDEMO_RESULTS");
        if (!string.IsNullOrEmpty(resultsPath))
        {
            File.WriteAllLines(resultsPath, results);
        }

        var failures = results.Count(r => r.StartsWith("FAIL", StringComparison.Ordinal));
        Log($"CHROMETEST: done, {failures} failure(s); exiting");
        await Task.Delay(250);
        Environment.Exit(failures);
    }

    private const string CustomClipFormat = "application/x-paritydemo";

    private static async Task<DataPackage> BuildRichClipboardPackageAsync(string tempFilePath)
    {
        var package = new DataPackage();
        package.SetText("parity-clipboard-π");
        package.SetHtmlFormat("<html><body><b>parity</b> clipboard</body></html>");

        // A small generated PNG (red 8x8) exercises the image path without asset plumbing.
        using (var bitmap = new SKBitmap(8, 8))
        {
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Red);
            }
            using var image = SKImage.FromBitmap(bitmap);
            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            var stream = new MemoryStream(png.ToArray());
            package.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream.AsRandomAccessStream()));
        }

        if (!string.IsNullOrEmpty(tempFilePath))
        {
            package.SetStorageItems(new IStorageItem[] { await StorageFile.GetFileFromPathAsync(tempFilePath) });
        }

        package.SetData(CustomClipFormat, new byte[] { 1, 2, 3, 4 });
        return package;
    }

    private async void CopyRich_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var tempFile = Path.Combine(Path.GetTempPath(), "paritydemo_clip.txt");
            File.WriteAllText(tempFile, "parity demo clipboard file");
            Clipboard.SetContent(await BuildRichClipboardPackageAsync(tempFile));
            Clipboard.Flush();
            Log("Copied rich content (text, html, png, file, custom bytes)");
        }
        catch (Exception ex)
        {
            Log($"Copy failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async void PasteInspect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var view = Clipboard.GetContent();
            Log($"Clipboard formats: [{string.Join(", ", view.AvailableFormats)}]");
            if (view.Contains(StandardDataFormats.Text))
            {
                Log($"  Text: '{await view.GetTextAsync()}'");
            }
            if (view.AvailableFormats.Contains("text/html"))
            {
                var html = await view.GetDataAsync("text/html");
                Log($"  text/html: {(html as byte[])?.Length.ToString() ?? "?"} bytes");
            }
            if (view.AvailableFormats.Contains("image/png"))
            {
                var pngBytes = await view.GetDataAsync("image/png") as byte[];
                using var decoded = pngBytes != null ? SKBitmap.Decode(pngBytes) : null;
                Log($"  image/png: {pngBytes?.Length.ToString() ?? "?"} bytes, decodes to {decoded?.Width}x{decoded?.Height}");
            }
            if (view.Contains(StandardDataFormats.StorageItems))
            {
                var items = await view.GetStorageItemsAsync();
                Log($"  StorageItems: [{string.Join(", ", items.Select(i => i.Path))}]");
            }
        }
        catch (Exception ex)
        {
            Log($"Paste failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task RunClipboardTestAsync()
    {
        var results = new List<string>();
        void Check(string name, bool pass, string detail)
        {
            var line = $"{(pass ? "PASS" : "FAIL")} {name}{(string.IsNullOrEmpty(detail) ? "" : $" ({detail})")}";
            results.Add(line);
            Log($"CLIPTEST: {line}");
        }

        try
        {
            await Task.Delay(2000); // let the window take keyboard focus

            var tempFile = Path.Combine(Path.GetTempPath(), "paritydemo_clip.txt");
            File.WriteAllText(tempFile, "parity demo clipboard file");
            Clipboard.SetContent(await BuildRichClipboardPackageAsync(tempFile));
            Clipboard.Flush();
            Log("CLIPTEST: content set; waiting for the selection round-trip");
            await Task.Delay(1000);

            var view = Clipboard.GetContent();
            var formats = view.AvailableFormats.ToList();
            Log($"CLIPTEST: formats [{string.Join(", ", formats)}]");

            var text = view.Contains(StandardDataFormats.Text) ? await view.GetTextAsync() : null;
            Check("clip-text-roundtrip", text == "parity-clipboard-π", $"got '{text}'");

            var htmlBytes = formats.Contains("text/html") ? await view.GetDataAsync("text/html") as byte[] : null;
            var html = htmlBytes != null ? Encoding.UTF8.GetString(htmlBytes) : null;
            Check("clip-html-roundtrip", html != null && html.Contains("<b>parity</b>"), $"got {htmlBytes?.Length.ToString() ?? "null"} bytes");

            var pngBytes = formats.Contains("image/png") ? await view.GetDataAsync("image/png") as byte[] : null;
            using (var decoded = pngBytes != null ? SKBitmap.Decode(pngBytes) : null)
            {
                Check("clip-png-roundtrip", decoded is { Width: 8, Height: 8 },
                    $"{pngBytes?.Length.ToString() ?? "null"} bytes -> {decoded?.Width}x{decoded?.Height}");
            }

            var items = view.Contains(StandardDataFormats.StorageItems) ? await view.GetStorageItemsAsync() : null;
            Check("clip-urilist-roundtrip", items != null && items.Any(i => i.Path == tempFile),
                $"got [{string.Join(", ", items?.Select(i => i.Path) ?? Array.Empty<string>())}]");

            var customBytes = formats.Contains(CustomClipFormat) ? await view.GetDataAsync(CustomClipFormat) as byte[] : null;
            Check("clip-custom-roundtrip", customBytes is { Length: 4 } && customBytes[3] == 4,
                $"got {customBytes?.Length.ToString() ?? "null"} bytes");
        }
        catch (Exception e)
        {
            results.Add($"FAIL cliptest-exception ({e.GetType().Name}: {e.Message})");
            Log($"CLIPTEST: exception {e}");
        }

        var resultsPath = Environment.GetEnvironmentVariable("PARITYDEMO_RESULTS");
        if (!string.IsNullOrEmpty(resultsPath))
        {
            File.WriteAllLines(resultsPath, results);
        }

        var failures = results.Count(r => r.StartsWith("FAIL", StringComparison.Ordinal));
        Log($"CLIPTEST: done, {failures} failure(s); exiting");
        await Task.Delay(250);
        Environment.Exit(failures);
    }

    private async Task RunSelfTestAsync()
    {
        var results = new List<string>();
        void Check(string name, bool pass, string detail)
        {
            var line = $"{(pass ? "PASS" : "FAIL")} {name}{(string.IsNullOrEmpty(detail) ? "" : $" ({detail})")}";
            results.Add(line);
            Log($"SELFTEST: {line}");
        }

        try
        {
            Log("SELFTEST: starting in 1.5s");
            await Task.Delay(1500);

            var injector = InputInjector.TryCreate();
            Check("input-injector-available", injector != null, "");

            // 1. Programmatic open: dropdown opens, renders, and stays open.
            TestCombo.IsDropDownOpen = true;
            await Task.Delay(1500);
            Check("combo-stays-open", TestCombo.IsDropDownOpen, $"popups: {DescribeOpenPopups()}");
            TestCombo.IsDropDownOpen = false;
            await Task.Delay(500);

            if (injector != null)
            {
                // 2. Injected click on the ComboBox opens the dropdown.
                await InjectClickAsync(injector, CenterInRoot(TestCombo));
                await Task.Delay(750);
                Check("combo-click-opens", TestCombo.IsDropDownOpen, $"popups: {DescribeOpenPopups()}");

                // 3. Injected click on the 'Charlie' item selects it and closes the dropdown
                //    (the "items dead" regression check).
                if (TestCombo.IsDropDownOpen
                    && TestCombo.ContainerFromIndex(2) is ComboBoxItem charlie
                    && charlie.ActualHeight > 0)
                {
                    await InjectClickAsync(injector, CenterInRoot(charlie));
                    await Task.Delay(750);
                    Check("combo-item-click-selects",
                        TestCombo.SelectedIndex == 2 && !TestCombo.IsDropDownOpen,
                        $"SelectedIndex={TestCombo.SelectedIndex} open={TestCombo.IsDropDownOpen}");
                }
                else
                {
                    Check("combo-item-click-selects", false, "dropdown not open or item not realized");
                }

                // 4. Injected click on the flyout button opens the MenuFlyout.
                await InjectClickAsync(injector, CenterInRoot(FlyoutButton));
                await Task.Delay(750);
                Check("menuflyout-click-opens", _menuFlyoutOpen, $"popups: {DescribeOpenPopups()}");

                // 5. Light-dismiss: a click far away closes the flyout.
                if (_menuFlyoutOpen)
                {
                    await InjectClickAsync(injector, new Point(XamlRoot.Size.Width - 30, 30));
                    await Task.Delay(750);
                    Check("menuflyout-light-dismiss", !_menuFlyoutOpen, "");
                }
                else
                {
                    Check("menuflyout-light-dismiss", false, "flyout never opened");
                }
            }

            // 6. ContentDialog appears.
            var dialog = new ContentDialog
            {
                Title = "Self-test dialog",
                Content = "auto",
                CloseButtonText = "Close",
                XamlRoot = XamlRoot,
            };
            var dialogTask = dialog.ShowAsync();
            await Task.Delay(1000);
            Check("contentdialog-open", VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Count > 0,
                $"popups: {DescribeOpenPopups()}");
            dialog.Hide();
            _ = dialogTask;
            await Task.Delay(500);
        }
        catch (Exception e)
        {
            results.Add($"FAIL selftest-exception ({e.GetType().Name}: {e.Message})");
            Log($"SELFTEST: exception {e}");
        }

        var resultsPath = Environment.GetEnvironmentVariable("PARITYDEMO_RESULTS");
        if (!string.IsNullOrEmpty(resultsPath))
        {
            File.WriteAllLines(resultsPath, results);
        }

        var failures = results.Count(r => r.StartsWith("FAIL", StringComparison.Ordinal));
        Log($"SELFTEST: done, {failures} failure(s); exiting");
        await Task.Delay(250);
        Environment.Exit(failures);
    }
}
