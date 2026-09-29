// Rendering and host registration adapted from FrameBuffer.Emulated/Hosting/TestTargetHost.cs
// and Rendering/EmulatedRenderer.cs. The existing head is deliberately independent.
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CodeBrix.Platform.ApplicationModel.Core;
using CodeBrix.Platform.ApplicationModel.DataTransfer;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia;
using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.Graphics.Display;
using Windows.UI.Core;

namespace CodeBrix.Platform.PlayTest.Hosting;

internal sealed class VirtualHost : SkiaHost, ISkiaApplicationHost, IXamlRootHost, ICoreApplicationExtension, IDisposable
{
    private readonly Func<Application> _factory;
    private readonly BlockingCollection<Action> _queue = new();
    private readonly ManualResetEventSlim _exit = new();
    private readonly AutoResetEvent _render = new(false);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _frameLock = new();
    private TaskCompletionSource<long> _frameChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Thread _uiThread;
    private Thread _renderThread;
    private volatile bool _stopping;
    private Exception _failure;
    private VirtualFrame _frame;
    private volatile VirtualScreen _screen;
    private long _sequence;
    private long _requested;
    private long _rendered;
    internal VirtualWindow Window { get; }
    internal VirtualInput Input { get; }
    internal VirtualClipboard Clipboard { get; } = new();
    internal int Width => _screen.Width;
    internal int Height => _screen.Height;
    internal ScreenOrientation Orientation => _screen.Orientation;
    internal event Action<VirtualFrame> FramePresented;
    internal UIElement Root => Window.Root;
    UIElement IXamlRootHost.RootElement => Root;
    public bool CanExit => true;

    internal VirtualHost(Func<Application> factory, bool portrait)
    {
        _factory = factory;
        _screen = new VirtualScreen(portrait ? ScreenOrientation.Portrait : ScreenOrientation.Landscape);
        Window = new VirtualWindow(this);
        Input = new VirtualInput(this);
    }

    protected override void Initialize()
    {
        _uiThread = new Thread(() =>
        {
            try
            {
                InitializeApplication();
                foreach (var action in _queue.GetConsumingEnumerable()) action();
            }
            catch (Exception e) { Fail(e); }
        }) { IsBackground = true, Name = "PlayTest UI" };
        if (OperatingSystem.IsWindows()) _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
    }

    private void InitializeApplication()
    {
        FeatureConfiguration.TextBox.UseOverlayOnSkia = false;
        FeatureConfiguration.Font.RestrictToEmbeddedFonts = true;
        CoreDispatcher.DispatchOverride = (action, priority) => Enqueue(action);
        CoreDispatcher.HasThreadAccessOverride = () => Thread.CurrentThread == _uiThread;
        ApiExtensibility.Register(typeof(INativeWindowFactoryExtension), _ => Window);
        ApiExtensibility.Register(typeof(ICoreApplicationExtension), _ => this);
        ApiExtensibility.Register<IXamlRootHost>(typeof(ICodeBrixCorePointerInputSource), _ => Input);
        ApiExtensibility.Register<IXamlRootHost>(typeof(ICodeBrixKeyboardInputSource), _ => Input);
        ApiExtensibility.Register(typeof(Windows.UI.ViewManagement.IApplicationViewExtension), _ => new VirtualApplicationView());
        ApiExtensibility.Register(typeof(IDisplayInformationExtension), _ => new VirtualDisplay(this));
        ApiExtensibility.Register(typeof(IClipboardExtension), _ => Clipboard);
        Application.Start(_ =>
        {
            var app = _factory();
            app.Host = this;
            app.UnhandledException += (_, args) =>
            {
                // Preserve a failed test and diagnostics instead of terminating the runner.
                Fail(args.Exception);
                args.Handled = true;
            };
        });
        _renderThread = new Thread(RenderLoop) { IsBackground = true, Name = "PlayTest Skia" };
        _renderThread.Start();
        InvalidateRender();
    }

    protected override Task RunLoop() { _exit.Wait(); return Task.CompletedTask; }

    internal void Enqueue(Action action)
    {
        if (_stopping) return;
        try { _queue.Add(action); }
        catch (InvalidOperationException) when (_stopping) { }
    }

    internal async Task<T> OnUI<T>(Func<T> action)
    {
        ThrowIfFailed();
        if (Thread.CurrentThread == _uiThread) return action();
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Enqueue(() =>
        {
            try { completion.TrySetResult(action()); }
            catch (Exception e) { completion.TrySetException(e); }
        });
        var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        ThrowIfFailed();
        return result;
    }

    internal Task OnUI(Action action) => OnUI(() => { action(); return true; });
    internal Task ReadyAsync() => _ready.Task.WaitAsync(TimeSpan.FromSeconds(30));

    // Dispatcher-only; callers await CaptureAsync before running the next test.
    internal void SetOrientation(ScreenOrientation orientation)
    {
        if (!Enum.IsDefined(orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
        if (_screen.Orientation == orientation) return;
        lock (_frameLock) _screen = new VirtualScreen(orientation);
        Window.UpdateSize();
        DisplayInformation.GetForCurrentView().NotifyOrientationChanged();
        InvalidateRender();
    }

    public void InvalidateRender()
    {
        Interlocked.Increment(ref _requested);
        _render.Set();
    }

    private void RenderLoop()
    {
        SKSurface surface = null;
        SKImageInfo info = default;
        try
        {
            while (!_stopping)
            {
                _render.WaitOne();
                if (_stopping) break;
                if (Root?.Visual.CompositionTarget is not CompositionTarget target) continue;
                var generation = Interlocked.Read(ref _requested);
                var screen = _screen;
                if (surface == null || info.Width != screen.Width || info.Height != screen.Height)
                {
                    surface?.Dispose();
                    surface = null;
                    info = new SKImageInfo(screen.Width, screen.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                    surface = SKSurface.Create(info) ?? throw new PlayTestException("Could not create the virtual Skia surface.");
                }
                surface.Canvas.Clear(SKColors.Transparent);
                target.OnNativePlatformFrameRequested(surface.Canvas, _ => surface.Canvas);
                surface.Canvas.Flush();
                var pixels = new byte[info.BytesSize];
                unsafe
                {
                    fixed (byte* pointer = pixels)
                        if (!surface.ReadPixels(info, (IntPtr)pointer, info.RowBytes, 0, 0))
                            throw new PlayTestException("Could not read the rendered virtual screen.");
                }
                TaskCompletionSource<long> changed;
                var frame = new VirtualFrame(screen, pixels);
                lock (_frameLock)
                {
                    // A frame already being drawn when orientation changed belongs to the old screen.
                    if (!ReferenceEquals(screen, _screen)) continue;
                    _frame = frame;
                    _rendered = generation;
                    _sequence++;
                    changed = _frameChanged;
                    _frameChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                changed.TrySetResult(_sequence);
                _ready.TrySetResult();
                FramePresented?.Invoke(frame);
                // Limit live animation presentation; invalidations coalesce while waiting.
                if (!_stopping) Thread.Sleep(16);
            }
        }
        catch (Exception e) { Fail(e); }
        finally { surface?.Dispose(); }
    }

    internal async Task<VirtualFrame> CaptureAsync()
    {
        // The compositor records on the UI thread and draws its previous recording.
        // Two passes with a dispatcher barrier produce the current tree, as in UIReqs.
        for (var pass = 0; pass < 2; pass++)
        {
            await OnUI(() => Window.ManagedWindow?.Content?.UpdateLayout()).ConfigureAwait(false);
            var generation = Interlocked.Increment(ref _requested);
            _render.Set();
            while (true)
            {
                Task changed;
                lock (_frameLock)
                {
                    if (_rendered >= generation && ReferenceEquals(_frame?.Screen, _screen)) break;
                    changed = _frameChanged.Task;
                }
                await changed.WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                ThrowIfFailed();
            }
            await OnUI(() => { }).ConfigureAwait(false);
        }
        lock (_frameLock) return _frame;
    }

    internal void ThrowIfFailed()
    {
        if (_failure != null) throw new PlayTestException("The application or renderer failed.", _failure);
        if (_stopping) throw new ObjectDisposedException(nameof(PlayTestApplication));
    }

    private void Fail(Exception error)
    {
        Interlocked.CompareExchange(ref _failure, error, null);
        _ready.TrySetException(error);
        lock (_frameLock) _frameChanged.TrySetException(error);
        _exit.Set();
    }

    public void Exit() => _exit.Set();

    public void Dispose()
    {
        _stopping = true;
        _render.Set();
        _exit.Set();
        _queue.CompleteAdding();
        _renderThread?.Join(TimeSpan.FromSeconds(5));
        _uiThread?.Join(TimeSpan.FromSeconds(5));
    }
}
