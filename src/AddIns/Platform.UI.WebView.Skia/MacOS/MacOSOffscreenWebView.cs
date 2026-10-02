using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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

/// <summary>Supplies a Skia-composited WKWebView for the macOS PlayTest host.</summary>
/// <remarks>AppKit runs in a separate helper on its main thread. The normal macOS
/// desktop head continues to use its native window provider.</remarks>
/// <param name="owner">The framework WebView2 facade.</param>
public sealed class MacOSOffscreenWebViewProvider(CoreWebView2 owner) : INativeWebViewProvider
{
    INativeWebView INativeWebViewProvider.CreateNativeWebView(ContentPresenter presenter)
        => new MacOSOffscreenWebView(owner, presenter);
}

internal sealed class MacOSOffscreenWebView : ICleanableNativeWebView
{
    private readonly CoreWebView2 _owner;
    private readonly OffscreenWebViewElement _element;
    private readonly Control? _focusTarget;
    private readonly Process _process;
    private readonly Task<string> _errors;
    private readonly Task _reader;
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _write = new(1);
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _request;
    private int _generation;
    private int _width = 800;
    private int _height = 600;
    private string _title = "";
    private string? _pendingHtml;
    private volatile bool _hasDocument;
    private bool _scrollEnabled = true;
    private Exception? _failure;

    internal MacOSOffscreenWebView(CoreWebView2 owner, ContentPresenter presenter)
    {
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException("The offscreen WKWebView requires macOS.");
        _owner = owner;
        _element = new OffscreenWebViewElement(presenter.Visual.Compositor);
        _focusTarget = owner.Owner as Control;
        presenter.Content = _element;
        var helper = Path.Combine(AppContext.BaseDirectory, "CodeBrix.PlayTest.WebView");
        if (!File.Exists(helper)) throw new FileNotFoundException("The macOS WebView helper is missing. Rebuild the matching WebView package on macOS.", helper);
        // NuGet content extraction does not preserve executable permissions. Only
        // the copy in the consuming application's output directory is changed.
        File.SetUnixFileMode(helper, File.GetUnixFileMode(helper) | UnixFileMode.UserExecute);
        _process = Process.Start(new ProcessStartInfo(helper)
        {
            UseShellExecute = false, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Could not start the WKWebView helper.");
        _errors = _process.StandardError.ReadToEndAsync();
        _reader = ReadAsync();
        _element.SizeChanged += (_, _) => Resize();
        WireInput();
        Observe(CaptureAsync());
    }

    private async Task<JsonElement> RequestAsync(string operation, object? arguments = null)
    {
        _stop.Token.ThrowIfCancellationRequested();
        if (_failure is { } error) throw new InvalidOperationException("The WKWebView helper failed.", error);
        await _ready.Task.WaitAsync(TimeSpan.FromSeconds(20), _stop.Token).ConfigureAwait(false);
        var id = Interlocked.Increment(ref _request);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;
        try
        {
            var message = arguments is null ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(arguments))!;
            message["op"] = operation;
            message["id"] = id;
            await _write.WaitAsync(_stop.Token).ConfigureAwait(false);
            try { await _process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message)).ConfigureAwait(false);
                await _process.StandardInput.FlushAsync(_stop.Token).ConfigureAwait(false); }
            finally { _write.Release(); }
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(15), _stop.Token).ConfigureAwait(false);
        }
        finally { _pending.TryRemove(id, out _); }
    }

    private async Task ReadAsync()
    {
        try
        {
            while (await _process.StandardOutput.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                using var document = JsonDocument.Parse(line);
                var message = document.RootElement;
                if (message.TryGetProperty("id", out var identifier))
                {
                    if (_pending.TryRemove(identifier.GetInt64(), out var completion))
                    {
                        if (message.TryGetProperty("error", out var error)) completion.TrySetException(new InvalidOperationException(error.GetString()));
                        else completion.TrySetResult(message.GetProperty("result").Clone());
                    }
                }
                else if (message.GetProperty("event").GetString() == "ready") _ready.TrySetResult();
                else
                {
                    var copy = message.Clone();
                    _element.DispatcherQueue.TryEnqueue(() => { if (!_stop.IsCancellationRequested) HandleEvent(copy); });
                }
            }
            if (!_stop.IsCancellationRequested)
                throw new InvalidOperationException("WKWebView helper exited: " + await _errors.ConfigureAwait(false));
        }
        catch (Exception error)
        {
            _failure = error;
            _ready.TrySetException(error);
            foreach (var pending in _pending.Values) pending.TrySetException(error);
            if (!_stop.IsCancellationRequested)
                _element.DispatcherQueue.TryEnqueue(() => { if (!_stop.IsCancellationRequested) throw new InvalidOperationException("WKWebView transport failed.", error); });
        }
    }

    private void HandleEvent(JsonElement message)
    {
        switch (message.GetProperty("event").GetString())
        {
            case "navigation":
                var url = message.GetProperty("url").GetString()!;
                _owner.RaiseNavigationStarting(url == "about:blank" && _pendingHtml is { } html ? html : new Uri(url), out var cancel);
                _pendingHtml = null;
                Observe(RequestAsync("policy", new { token = message.GetProperty("token").GetInt64(), allow = !cancel }));
                break;
            case "state":
                _owner.Source = message.GetProperty("url").GetString()!;
                _owner.SetHistoryProperties(message.GetProperty("back").GetBoolean(), message.GetProperty("forward").GetBoolean());
                _owner.RaiseHistoryChanged();
                var title = message.GetProperty("title").GetString()!;
                if (_title != title) { _title = title; _owner.OnDocumentTitleChanged(); }
                break;
            case "completed":
                var success = message.GetProperty("success").GetBoolean();
                _hasDocument = success;
                _owner.RaiseNavigationCompleted(new Uri(message.GetProperty("url").GetString()!), success,
                    message.GetProperty("status").GetInt32(), success ? CoreWebView2WebErrorStatus.Unknown : CoreWebView2WebErrorStatus.UnexpectedError);
                break;
            case "message": _owner.RaiseWebMessageReceived(message.GetProperty("json").GetString()!); break;
            case "popup":
                var target = message.GetProperty("url").GetString()!;
                _owner.RaiseNewWindowRequested(target, new Uri(_owner.Source), out var handled);
                if (!handled) ProcessNavigation(new Uri(target));
                break;
            case "fatal": throw new InvalidOperationException(message.GetProperty("error").GetString());
        }
    }

    private async void Observe(Task operation)
    {
        try { await operation.ConfigureAwait(false); }
        catch (Exception) when (_stop.IsCancellationRequested) { }
        catch (Exception error)
        {
            _element.DispatcherQueue.TryEnqueue(() => { if (!_stop.IsCancellationRequested) throw new InvalidOperationException("The offscreen WKWebView failed.", error); });
        }
    }

    private async Task CaptureAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            if (_hasDocument)
            {
                var generation = Volatile.Read(ref _generation);
                var result = await RequestAsync("snapshot").ConfigureAwait(false);
                var image = SKImage.FromEncodedData(Convert.FromBase64String(result.GetProperty("png").GetString()!))
                    ?? throw new InvalidOperationException("WKWebView returned an invalid PNG.");
                if (!_element.DispatcherQueue.TryEnqueue(() =>
                {
                    if (_stop.IsCancellationRequested || generation != _generation
                        || result.GetProperty("width").GetInt32() != _width || result.GetProperty("height").GetInt32() != _height) image.Dispose();
                    else _element.PresentFrame(image);
                })) image.Dispose();
            }
            await Task.Delay(50, _stop.Token).ConfigureAwait(false);
        }
    }

    private void Resize()
    {
        var width = Math.Max(1, (int)Math.Ceiling(_element.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(_element.ActualHeight));
        if (width == _width && height == _height) return;
        _width = width; _height = height;
        Interlocked.Increment(ref _generation);
        Observe(RequestAsync("resize", new { width, height }));
    }

    private static ulong Modifiers(VirtualKeyModifiers modifiers) =>
        ((modifiers & VirtualKeyModifiers.Shift) != 0 ? 1UL << 17 : 0)
        | ((modifiers & VirtualKeyModifiers.Menu) != 0 ? 1UL << 19 : 0)
        // PlayTest uses Control-based shortcuts on every OS; AppKit calls that Command.
        | ((modifiers & (VirtualKeyModifiers.Control | VirtualKeyModifiers.Windows)) != 0 ? 1UL << 20 : 0);

    private void WireInput()
    {
        _element.PointerMoved += (_, args) => Mouse(args, 0);
        _element.PointerPressed += (_, args) => { _focusTarget?.Focus(FocusState.Pointer); _element.CapturePointer(args.Pointer); Mouse(args, 1); };
        _element.PointerReleased += (_, args) => { Mouse(args, 2); _element.ReleasePointerCapture(args.Pointer); };
        _element.PointerWheelChanged += (_, args) =>
        {
            if (!_scrollEnabled) return;
            var point = args.GetCurrentPoint(_element);
            var pixels = point.Properties.MouseWheelDelta * 53 / 120;
            Observe(RequestAsync("scroll", new { dx = point.Properties.IsHorizontalMouseWheel ? pixels : 0,
                dy = point.Properties.IsHorizontalMouseWheel ? 0 : pixels }));
            args.Handled = true;
        };
        var keys = (UIElement?)_focusTarget ?? _element;
        keys.KeyDown += (_, args) => Key(args, true);
        keys.KeyUp += (_, args) => Key(args, false);
    }

    private void Mouse(PointerRoutedEventArgs args, int kind)
    {
        var point = args.GetCurrentPoint(_element);
        var button = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => 1,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => 2,
            _ => 0,
        };
        Observe(RequestAsync("mouse", new { kind, button, x = point.Position.X, y = point.Position.Y, modifiers = Modifiers(args.KeyModifiers) }));
        args.Handled = true;
    }

    private void Key(KeyRoutedEventArgs args, bool down)
    {
        var (text, code) = args.OriginalKey switch
        {
            VirtualKey.Tab => ("\t", 48), VirtualKey.Enter => ("\r", 36),
            VirtualKey.Back => ("\b", 51), VirtualKey.Escape => ("\u001b", 53),
            VirtualKey.Left => ("\uf702", 123), VirtualKey.Right => ("\uf703", 124),
            VirtualKey.Up => ("\uf700", 126), VirtualKey.Down => ("\uf701", 125),
            VirtualKey.Delete => ("\uf728", 117), VirtualKey.Home => ("\uf729", 115),
            VirtualKey.End => ("\uf72b", 119), VirtualKey.PageUp => ("\uf72c", 116),
            VirtualKey.PageDown => ("\uf72d", 121), VirtualKey.Space => (" ", 49),
            _ => (args.UnicodeKey?.ToString() ?? "", 0),
        };
        Observe(RequestAsync("key", new { down, text, code, modifiers = Modifiers(args.KeyboardModifiers) }));
        args.Handled = true;
    }

    public string DocumentTitle => _title;
    public void GoBack() => Observe(RequestAsync("back"));
    public void GoForward() => Observe(RequestAsync("forward"));
    public void Reload() => Observe(RequestAsync("reload"));
    public void Stop() => Observe(RequestAsync("stop"));
    public void ProcessNavigation(Uri uri) => Observe(RequestAsync("navigate", new { url = uri.AbsoluteUri }));
    public void ProcessNavigation(string html) { _pendingHtml = html; Observe(RequestAsync("html", new { html })); }
    public void ProcessNavigation(HttpRequestMessage request) => Observe(NavigateRequestAsync(request));
    private async Task NavigateRequestAsync(HttpRequestMessage request)
    {
        var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(", ", header.Value));
        if (request.Content is { } content)
            foreach (var header in content.Headers) headers[header.Key] = string.Join(", ", header.Value);
        await RequestAsync("navigate", new { url = request.RequestUri!.AbsoluteUri, method = request.Method.Method, headers,
            body = request.Content is null ? null : Convert.ToBase64String(await request.Content.ReadAsByteArrayAsync(_stop.Token)) });
    }
    public async Task<string?> ExecuteScriptAsync(string script, CancellationToken token)
        => (await RequestAsync("script", new { script }).WaitAsync(token)).GetString();
    public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token)
        => ExecuteScriptAsync($"{script}({JsonSerializer.Serialize(arguments ?? Array.Empty<string>())[1..^1]})", token);
    public void SetScrollingEnabled(bool enabled) => _scrollEnabled = enabled;
    public void SetUserAgent(string userAgent) => Observe(RequestAsync("userAgent", new { value = userAgent }));
    public void OnLoaded() => Resize();
    public void OnUnloaded()
    {
        if (_stop.IsCancellationRequested) return;
        _stop.Cancel();
        _element.ClearFrame();
        Observe(CloseAsync());
    }
    private async Task CloseAsync()
    {
        try
        {
            await _write.WaitAsync().ConfigureAwait(false);
            try { _process.StandardInput.Close(); }
            finally { _write.Release(); }
            try { await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false); }
            catch (TimeoutException) { _process.Kill(entireProcessTree: true); await _process.WaitForExitAsync().ConfigureAwait(false); }
            await _reader.ConfigureAwait(false);
        }
        finally { _process.Dispose(); }
    }
}
