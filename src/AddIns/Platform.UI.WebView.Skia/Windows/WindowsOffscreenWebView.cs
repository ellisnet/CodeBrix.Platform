extern alias mswebview2;
using Native = mswebview2::Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using SkiaSharp;
using Windows.System;

namespace CodeBrix.Platform.UI.WebView.Skia.Offscreen;

/// <summary>Supplies a Skia-composited Edge WebView2 for the Windows PlayTest host.</summary>
/// <remarks>The optional WebView add-in is discovered by PlayTest. Normal desktop heads
/// continue to register their own native-window providers.</remarks>
/// <param name="owner">The framework WebView2 facade.</param>
public sealed class WindowsOffscreenWebViewProvider(CoreWebView2 owner) : INativeWebViewProvider
{
    INativeWebView INativeWebViewProvider.CreateNativeWebView(ContentPresenter presenter)
        => new WindowsOffscreenWebView(owner, presenter);
}

internal sealed class WindowsOffscreenWebView : ICleanableNativeWebView, ISupportsVirtualHostMapping
{
    private readonly CoreWebView2 _owner;
    private readonly OffscreenWebViewElement _element;
    private readonly Control? _focusTarget;
    private readonly Dictionary<ulong, string> _navigations = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _ready;
    private Native.CoreWebView2Controller? _controller;
    private Native.CoreWebView2CompositionController? _composition;
    private Native.CoreWebView2? _browser;
    private IntPtr _window;
    private string _title = "";
    private string _defaultUserAgent = "";
    private bool _hasDocument;
    private bool _unloaded;
    private bool _scrollEnabled = true;

    internal WindowsOffscreenWebView(CoreWebView2 owner, ContentPresenter presenter)
    {
        if (!OperatingSystem.IsWindows() || Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new PlatformNotSupportedException("The offscreen Edge WebView requires a Windows STA with a native message pump.");
        _owner = owner;
        _element = new OffscreenWebViewElement(presenter.Visual.Compositor);
        presenter.Content = _element;
        _focusTarget = owner.Owner as Control;
        _element.SizeChanged += (_, _) => Resize();
        WireInput();
        _ready = InitializeAsync();
        Observe(_ready);
    }

    private async Task InitializeAsync()
    {
        _window = CreateWindowEx(0, "STATIC", "CodeBrix offscreen WebView", 0,
            0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_window == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            var profile = Path.Combine(AppContext.BaseDirectory, "TestResults", "PlayTest", "WebView2", Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            var environment = await Native.CoreWebView2Environment.CreateAsync(userDataFolder: profile);
            _stop.Token.ThrowIfCancellationRequested();
            var composition = await environment.CreateCoreWebView2CompositionControllerAsync(_window);
            if (_stop.IsCancellationRequested)
            {
                ((Native.CoreWebView2Controller)composition).Close();
                _stop.Token.ThrowIfCancellationRequested();
            }
            _composition = composition;
            _controller = (Native.CoreWebView2Controller)composition;
            _controller.ShouldDetectMonitorScaleChanges = false;
            _controller.RasterizationScale = 1;
            _controller.BoundsMode = Native.CoreWebView2BoundsMode.UseRawPixels;
            _controller.IsVisible = true;
            _browser = _controller.CoreWebView2;
            _defaultUserAgent = _browser.Settings.UserAgent;
            _browser.Settings.AreDefaultContextMenusEnabled = false;
            _browser.Settings.AreDefaultScriptDialogsEnabled = false;
            _browser.Settings.AreDevToolsEnabled = false;
            WireEvents();
            Resize();
            Observe(CaptureFramesAsync());
        }
        catch
        {
            CloseNative();
            throw;
        }
    }

    // Async failures are raised on the owning XAML dispatcher, so the test host reports
    // the actual initialization/capture failure instead of silently presenting a blank view.
    private async void Observe(Task task)
    {
        try { await task; }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception) when (_stop.IsCancellationRequested) { }
    }

    private async Task WithBrowserAsync(Action<Native.CoreWebView2> action)
    {
        await _ready;
        _stop.Token.ThrowIfCancellationRequested();
        action(_browser!);
    }

    private void WireEvents()
    {
        var browser = _browser!;
        browser.ContentLoading += (_, _) => _hasDocument = true;
        browser.NavigationStarting += (_, args) =>
        {
            _owner.RaiseNavigationStarting(new Uri(args.Uri), out var cancel);
            args.Cancel = cancel;
            if (!cancel) _navigations[args.NavigationId] = args.Uri;
        };
        browser.SourceChanged += (_, _) => _owner.Source = browser.Source;
        browser.NavigationCompleted += (_, args) =>
        {
            _navigations.Remove(args.NavigationId, out var source);
            _owner.SetHistoryProperties(browser.CanGoBack, browser.CanGoForward);
            _owner.RaiseNavigationCompleted(Uri.TryCreate(source, UriKind.Absolute, out var uri) ? uri : null,
                args.IsSuccess, args.HttpStatusCode, (CoreWebView2WebErrorStatus)args.WebErrorStatus, shouldSetSource: false);
        };
        browser.HistoryChanged += (_, _) =>
        {
            _owner.SetHistoryProperties(browser.CanGoBack, browser.CanGoForward);
            _owner.RaiseHistoryChanged();
        };
        browser.DocumentTitleChanged += (_, _) => { _title = browser.DocumentTitle; _owner.OnDocumentTitleChanged(); };
        browser.WebMessageReceived += (_, args) => _owner.RaiseWebMessageReceived(args.WebMessageAsJson);
        browser.NewWindowRequested += (_, args) =>
        {
            _owner.RaiseNewWindowRequested(args.Uri, CoreWebView2.BlankUri, out var handled);
            // Offscreen tests must never open a browser window on the user's desktop.
            args.Handled = true;
            if (!handled) browser.Navigate(args.Uri);
        };
        browser.DownloadStarting += (_, args) =>
        {
            var native = args.DownloadOperation;
            var operation = new CoreWebView2DownloadOperation(native.Uri, native.ContentDisposition,
                native.MimeType, (long)(native.TotalBytesToReceive ?? 0), args.ResultFilePath, native.Cancel);
            native.BytesReceivedChanged += (_, _) => operation.ReportProgress((long)native.BytesReceived, (long)(native.TotalBytesToReceive ?? 0));
            native.StateChanged += (_, _) => operation.ReportStateChanged((CoreWebView2DownloadState)native.State, (CoreWebView2DownloadInterruptReason)native.InterruptReason);
            var deferral = args.GetDeferral();
            _owner.RaiseDownloadStarting(operation, decision =>
            {
                args.Cancel = decision.Cancel;
                args.ResultFilePath = decision.ResultFilePath;
                args.Handled = true;
                operation.SetResultFilePath(decision.ResultFilePath);
                deferral.Complete();
            });
        };
    }

    private async Task CaptureFramesAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            if (_hasDocument && !_unloaded)
            {
                var bounds = _controller!.Bounds;
                using var stream = new MemoryStream();
                await _browser!.CapturePreviewAsync(Native.CoreWebView2CapturePreviewImageFormat.Png, stream);
                if (_stop.IsCancellationRequested) return;
                // Resizing can race a pending capture. Never publish a frame from the old size.
                if (bounds == _controller.Bounds)
                {
                    stream.Position = 0;
                    var frame = SKImage.FromEncodedData(stream) ?? throw new InvalidOperationException("Edge returned an invalid preview image.");
                    _element.PresentFrame(frame);
                }
            }
            await Task.Delay(50, _stop.Token);
        }
    }

    private void Resize()
    {
        if (_controller is null) return;
        var scale = _element.XamlRoot?.RasterizationScale ?? 1;
        _controller.RasterizationScale = scale;
        var bounds = new Rectangle(0, 0, Math.Max(1, (int)Math.Ceiling(_element.ActualWidth * scale)),
            Math.Max(1, (int)Math.Ceiling(_element.ActualHeight * scale)));
        if (_controller.Bounds != bounds) _controller.Bounds = bounds;
    }

    private void WireInput()
    {
        _element.PointerMoved += (_, e) => Mouse(e, Native.CoreWebView2MouseEventKind.Move);
        _element.PointerPressed += (_, e) =>
        {
            _focusTarget?.Focus(FocusState.Pointer);
            _element.CapturePointer(e.Pointer);
            Mouse(e, e.GetCurrentPoint(_element).Properties.PointerUpdateKind switch
            {
                PointerUpdateKind.RightButtonPressed => Native.CoreWebView2MouseEventKind.RightButtonDown,
                PointerUpdateKind.MiddleButtonPressed => Native.CoreWebView2MouseEventKind.MiddleButtonDown,
                _ => Native.CoreWebView2MouseEventKind.LeftButtonDown,
            });
        };
        _element.PointerReleased += (_, e) =>
        {
            Mouse(e, e.GetCurrentPoint(_element).Properties.PointerUpdateKind switch
            {
                PointerUpdateKind.RightButtonReleased => Native.CoreWebView2MouseEventKind.RightButtonUp,
                PointerUpdateKind.MiddleButtonReleased => Native.CoreWebView2MouseEventKind.MiddleButtonUp,
                _ => Native.CoreWebView2MouseEventKind.LeftButtonUp,
            });
            _element.ReleasePointerCapture(e.Pointer);
        };
        _element.PointerExited += (_, _) => _composition?.SendMouseInput(Native.CoreWebView2MouseEventKind.Leave, 0, 0, default);
        _element.PointerWheelChanged += (_, e) =>
        {
            if (_scrollEnabled) Mouse(e, e.GetCurrentPoint(_element).Properties.IsHorizontalMouseWheel
                ? Native.CoreWebView2MouseEventKind.HorizontalWheel : Native.CoreWebView2MouseEventKind.Wheel);
        };
        var keys = (UIElement?)_focusTarget ?? _element;
        keys.KeyDown += (_, e) => Key(e, true);
        keys.KeyUp += (_, e) => Key(e, false);
    }

    private void Mouse(PointerRoutedEventArgs args, Native.CoreWebView2MouseEventKind kind)
    {
        if (_composition is null) return;
        var point = args.GetCurrentPoint(_element);
        var scale = _element.XamlRoot?.RasterizationScale ?? 1;
        var modifiers = Native.CoreWebView2MouseEventVirtualKeys.None;
        if (point.Properties.IsLeftButtonPressed) modifiers |= Native.CoreWebView2MouseEventVirtualKeys.LeftButton;
        if (point.Properties.IsRightButtonPressed) modifiers |= Native.CoreWebView2MouseEventVirtualKeys.RightButton;
        if (point.Properties.IsMiddleButtonPressed) modifiers |= Native.CoreWebView2MouseEventVirtualKeys.MiddleButton;
        if ((args.KeyModifiers & VirtualKeyModifiers.Control) != 0) modifiers |= Native.CoreWebView2MouseEventVirtualKeys.Control;
        if ((args.KeyModifiers & VirtualKeyModifiers.Shift) != 0) modifiers |= Native.CoreWebView2MouseEventVirtualKeys.Shift;
        _composition.SendMouseInput(kind, modifiers,
            kind is Native.CoreWebView2MouseEventKind.Wheel or Native.CoreWebView2MouseEventKind.HorizontalWheel ? unchecked((uint)point.Properties.MouseWheelDelta) : 0,
            new Point((int)Math.Round(point.Position.X * scale), (int)Math.Round(point.Position.Y * scale)));
        args.Handled = true;
    }

    private void Key(KeyRoutedEventArgs args, bool down)
    {
        var modifiers = args.KeyboardModifiers;
        var mask = ((modifiers & VirtualKeyModifiers.Menu) != 0 ? 1 : 0)
            | ((modifiers & VirtualKeyModifiers.Control) != 0 ? 2 : 0)
            | ((modifiers & VirtualKeyModifiers.Windows) != 0 ? 4 : 0)
            | ((modifiers & VirtualKeyModifiers.Shift) != 0 ? 8 : 0);
        var character = down && (mask & 7) == 0 ? args.UnicodeKey?.ToString() ?? "" : "";
        var key = args.OriginalKey switch
        {
            VirtualKey.Back => "Backspace",
            VirtualKey.Left => "ArrowLeft",
            VirtualKey.Right => "ArrowRight",
            VirtualKey.Up => "ArrowUp",
            VirtualKey.Down => "ArrowDown",
            VirtualKey.Menu => "Alt",
            VirtualKey.LeftWindows or VirtualKey.RightWindows => "Meta",
            _ => args.UnicodeKey?.ToString() ?? args.OriginalKey.ToString(),
        };
        // Dispatch browser input, not script that changes a DOM value or invokes a click handler.
        // Chromium needs the DOM key identity for default actions such as Tab navigation.
        var payload = JsonSerializer.Serialize(new { type = down ? (character.Length == 0 ? "rawKeyDown" : "keyDown") : "keyUp", key, modifiers = mask,
            windowsVirtualKeyCode = (int)args.OriginalKey, text = character, unmodifiedText = character });
        Observe(DispatchKeyAsync(payload));
        args.Handled = true;
    }

    private async Task DispatchKeyAsync(string payload)
    {
        await _ready;
        _stop.Token.ThrowIfCancellationRequested();
        await _browser!.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", payload);
    }

    public string DocumentTitle => _title;
    public void GoBack() => Observe(WithBrowserAsync(browser => browser.GoBack()));
    public void GoForward() => Observe(WithBrowserAsync(browser => browser.GoForward()));
    public void Stop() => Observe(WithBrowserAsync(browser => browser.Stop()));
    public void Reload() => Observe(WithBrowserAsync(browser => browser.Reload()));
    public void ProcessNavigation(Uri uri) => Observe(WithBrowserAsync(browser => browser.Navigate(uri.AbsoluteUri)));
    public void ProcessNavigation(string html) => Observe(WithBrowserAsync(browser => browser.NavigateToString(html)));
    public void ProcessNavigation(HttpRequestMessage request) => Observe(WithBrowserAsync(browser =>
    {
        var native = browser.Environment.CreateWebResourceRequest(request.RequestUri!.AbsoluteUri, request.Method.Method,
            request.Content?.ReadAsStream(), request.Headers.ToString());
        browser.NavigateWithWebResourceRequest(native);
    }));
    public async Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
    {
        await _ready.WaitAsync(token);
        _stop.Token.ThrowIfCancellationRequested();
        return await _browser!.ExecuteScriptAsync(script).WaitAsync(token);
    }
    public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
        => ExecuteScriptAsync($"{script}({JsonSerializer.Serialize(arguments ?? Array.Empty<string>())[1..^1]})", token);
    public void SetScrollingEnabled(bool enabled) => _scrollEnabled = enabled;
    public void SetUserAgent(string userAgent) => Observe(WithBrowserAsync(browser => browser.Settings.UserAgent = string.IsNullOrEmpty(userAgent) ? _defaultUserAgent : userAgent));
    public void SetVirtualHostNameToFolderMapping(string host, string folder, CoreWebView2HostResourceAccessKind access)
        => Observe(WithBrowserAsync(browser => browser.SetVirtualHostNameToFolderMapping(host, folder, (Native.CoreWebView2HostResourceAccessKind)access)));
    public void ClearVirtualHostNameToFolderMapping(string host)
        => Observe(WithBrowserAsync(browser => browser.ClearVirtualHostNameToFolderMapping(host)));
    public void OnLoaded() { _unloaded = false; Resize(); }
    public void OnUnloaded()
    {
        _unloaded = true;
        _stop.Cancel();
        CloseNative();
        _element.ClearFrame();
    }
    private void CloseNative()
    {
        _controller?.Close();
        _controller = null;
        _composition = null;
        _browser = null;
        if (_window != IntPtr.Zero) { DestroyWindow(_window); _window = IntPtr.Zero; }
    }

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string title, uint style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr window);
}
