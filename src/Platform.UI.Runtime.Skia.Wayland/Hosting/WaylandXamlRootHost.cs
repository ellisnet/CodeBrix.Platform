using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.FractionalScaleV1;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Viewporter;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;
using Timer = System.Timers.Timer;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Per-window host: owns the window's content wl_surface and renderer, and delegates the
/// shell/decoration lifecycle to an <see cref="IWaylandShellSurface"/> (xdg-shell or libdecor).
/// </summary>
internal partial class WaylandXamlRootHost : IXamlRootHost
{
	private static bool _firstWindowCreated;
	private static readonly object _hostsMutex = new();
	private static readonly List<WaylandXamlRootHost> _hosts = new();
	private static readonly ConcurrentDictionary<Window, WaylandXamlRootHost> _windowToHost = new();
	private static readonly ConcurrentDictionary<WlSurface, WaylandXamlRootHost> _surfaceToHost = new();

	private readonly TaskCompletionSource _closed;
	private readonly ApplicationView _applicationView;
	private readonly WaylandWindowWrapper _wrapper;
	private readonly Window _window;
	private readonly Action _configureCallback;
	private readonly Action _closingCallback;
	private readonly Action<bool> _focusCallback;
	private readonly Action<bool> _visibilityCallback;
	private readonly Timer _renderTimer;

	private WaylandConnection? _connection;
	private IWaylandShellSurface? _shellSurface;
	private WlSurface? _wlSurface;
	private IWaylandRenderer? _renderer;
	private WpViewport? _viewport;
	private WpFractionalScaleV1? _fractionalScale;
	private WaylandDisplayInformationExtension? _displayInformationExtension;

	// Written on the event-pump thread, read on the UI thread after a queued callback.
	private volatile bool _activated;
	private volatile bool _shown;

	private int _width;
	private int _height;

	// The compositor's preferred fractional scale for this surface (from
	// wp_fractional_scale_v1.preferred_scale, delivered in 1/120ths); 0 until the first event.
	// long-boxed double so the event-pump write / any-thread read stays tear-free.
	private long _preferredScaleBits;

	public WaylandXamlRootHost(WaylandWindowWrapper wrapper, Window winUIWindow, XamlRoot xamlRoot,
		Action configureCallback, Action closingCallback, Action<bool> focusCallback, Action<bool> visibilityCallback)
	{
		_wrapper = wrapper;
		_window = winUIWindow;

		_closingCallback = closingCallback;
		_focusCallback = focusCallback;
		_visibilityCallback = visibilityCallback;
		_configureCallback = configureCallback;

		_closed = new TaskCompletionSource();
		Closed = _closed.Task;

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);

		Initialize();

		_windowToHost[winUIWindow] = this;
		XamlRootMap.Register(xamlRoot, this);

		if (!string.IsNullOrEmpty(Windows.ApplicationModel.Package.Current.DisplayName))
		{
			_applicationView.Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}

		_renderTimer = CreateRenderTimer();

		var windowBackgroundDisposable = _window.RegisterBackgroundChangedEvent((_, _) => UpdateRendererBackground());
		UpdateRendererBackground();

		Closed.ContinueWith(closedTask =>
		{
			XamlRootMap.Unregister(xamlRoot);
			_ = _windowToHost.TryRemove(winUIWindow, out _);
			if (_wlSurface is { } wlSurface)
			{
				_ = _surfaceToHost.TryRemove(wlSurface, out _);
			}
			windowBackgroundDisposable.Dispose();
			_renderTimer.Dispose();
			_renderer?.Dispose();
			DestroyProtocolObjects();
		});
	}

	public static WaylandXamlRootHost? GetHostFromWindow(Window window)
		=> _windowToHost.TryGetValue(window, out var host) ? host : null;

	// Same indirection as the X11 head's X11Helper.XamlRootHostFromApplicationView.
	public static WaylandXamlRootHost? GetHostFromApplicationView(ApplicationView view)
		=> Microsoft.UI.Windowing.AppWindow.GetFromWindowId(view.WindowId) is { } appWindow &&
			Window.GetFromAppWindow(appWindow) is { } window
				? GetHostFromWindow(window)
				: null;

	public Task Closed { get; }

	public bool IsClosed => _closed.Task.IsCompleted;

	internal IWaylandShellSurface? ShellSurface => _shellSurface;

	internal WaylandConnection? Connection => _connection;

	internal WaylandPointerInputSource? PointerSource { get; private set; }
	internal WaylandKeyboardInputSource? KeyboardSource { get; private set; }
	internal WaylandDragDropExtension? DragDropExtension { get; private set; }

	internal void SetDragDropExtension(WaylandDragDropExtension dragDrop)
	{
		if (DragDropExtension is not null)
		{
			throw new InvalidOperationException($"{nameof(WaylandDragDropExtension)} is set twice on the same {nameof(WaylandXamlRootHost)}");
		}
		DragDropExtension = dragDrop;
	}

	public void SetPointerSource(WaylandPointerInputSource pointerSource)
	{
		if (PointerSource is not null)
		{
			throw new InvalidOperationException($"{nameof(WaylandPointerInputSource)} is set twice on the same {nameof(WaylandXamlRootHost)}");
		}
		PointerSource = pointerSource;
	}

	public void SetKeyboardSource(WaylandKeyboardInputSource keyboardSource)
	{
		if (KeyboardSource is not null)
		{
			throw new InvalidOperationException($"{nameof(WaylandKeyboardInputSource)} is set twice on the same {nameof(WaylandXamlRootHost)}");
		}
		KeyboardSource = keyboardSource;
	}

	internal static WaylandXamlRootHost? GetHostFromSurface(WlSurface? surface)
		=> surface != null && _surfaceToHost.TryGetValue(surface, out var host) ? host : null;

	internal SizeInt32 CurrentSize => new() { Width = _width, Height = _height };

	/// <summary>
	/// The viewport for the content surface when the compositor supports wp_viewporter;
	/// the renderers use it (with buffer scale 1) for true fractional scaling.
	/// </summary>
	internal WpViewport? Viewport => _viewport;

	/// <summary>
	/// The scale this window should render at: the compositor's preferred fractional scale
	/// for the surface when wp_fractional_scale_v1 delivered one, else the primary output's
	/// integer scale. This is the value DisplayInformation reports as RawPixelsPerViewPixel
	/// (absent an override) and the value the renderers must reconcile buffers against.
	/// </summary>
	internal double EffectiveScale
	{
		get
		{
			var preferred = BitConverter.Int64BitsToDouble(Interlocked.Read(ref _preferredScaleBits));
			if (preferred > 0)
			{
				return preferred;
			}

			return Math.Max(1, _connection?.PrimaryOutput.Scale ?? 1);
		}
	}

	internal void SetDisplayInformationExtension(WaylandDisplayInformationExtension extension)
		=> _displayInformationExtension = extension;

	// Event-pump thread (or any thread): some scale source changed (fractional preferred
	// scale, output scale). Refresh DisplayInformation, the window's RasterizationScale and
	// the size/bounds derived from it, then repaint — mirroring the X11 head's
	// RESOURCE_MANAGER PropertyNotify handling.
	internal void OnScaleSourceChanged()
	{
		if (IsClosed)
		{
			return;
		}

		QueueAction(this, () =>
		{
			_displayInformationExtension?.UpdateDetails();
			var scale = _displayInformationExtension?.RawPixelsPerViewPixel ?? EffectiveScale;
			_wrapper.RasterizationScale = (float)scale;
			_configureCallback();
			((IXamlRootHost)this).InvalidateRender();
		});
	}

	private void OnPreferredFractionalScale(uint scale120)
	{
		var newScale = scale120 / 120.0;
		var previous = BitConverter.Int64BitsToDouble(
			Interlocked.Exchange(ref _preferredScaleBits, BitConverter.DoubleToInt64Bits(newScale)));
		if (Math.Abs(previous - newScale) > 0.001)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Compositor preferred scale for the surface is now {newScale}.");
			}
			OnScaleSourceChanged();
		}
	}

	private void Initialize()
	{
		try
		{
			// The authoritative connect; throws WaylandCompositorMissingException when no
			// compositor is reachable (the application host fail-fasts before this point,
			// so in practice the connection already exists here).
			var connection = WaylandConnection.ConnectOrThrow();
			_connection = connection;

			var size = ApplicationView.PreferredLaunchViewSize;
			if (size == Size.Empty)
			{
				size = new Size(NativeWindowWrapperBase.InitialWidth, NativeWindowWrapperBase.InitialHeight);
			}

			_width = (int)size.Width;
			_height = (int)size.Height;

			// Decoration strategy (resolves plan open-question V3): prefer libdecor when it is
			// installed — it owns the xdg lifecycle and negotiates SSD vs CSD internally, so a
			// single path yields a decorated window on GNOME (CSD) and KDE/wlroots/muffin (SSD),
			// exactly as SDL and GLFW do. Without libdecor, fall back to raw xdg-shell +
			// xdg-decoration (SSD where the compositor offers it; undecorated on GNOME).
			var libdecorContext = connection.GetLibDecorContext();
			_shellSurface = libdecorContext != IntPtr.Zero
				? new LibDecorShellSurface(connection, libdecorContext, _width, _height)
				: new XdgShellSurface(connection, _width, _height);

			_wlSurface = _shellSurface.Surface;
			_surfaceToHost[_wlSurface] = this;

			// Fractional-scaling plumbing (P2): the viewport maps the (physical-pixel) buffer
			// onto the window's logical size, and the fractional-scale object tells us the
			// scale the compositor prefers for this surface. Both are optional protocols.
			if (connection.Viewporter is { } viewporter)
			{
				_viewport = viewporter.GetViewport(_wlSurface);
			}
			if (connection.FractionalScaleManager is { } fractionalScaleManager)
			{
				_fractionalScale = fractionalScaleManager.GetFractionalScale(_wlSurface,
					new WpFractionalScaleV1.Listener.Relay
					{
						OnPreferredScale = (_, scale120) => OnPreferredFractionalScale(scale120),
					});
			}

			_shellSurface.Configured += OnShellConfigured;
			_shellSurface.CloseRequested += OnShellCloseRequested;
			_shellSurface.WindowStateChanged += OnShellWindowStateChanged;

			// app_id is the hook the desktop uses to associate the window with an installed
			// .desktop file (taskbar icon, grouping). Full icon/.desktop integration is a
			// separate, later effort — but the id is exposed now.
			_shellSurface.SetAppId(GetAppId());

			_shellSurface.MapInitial();

			lock (_hostsMutex)
			{
				_hosts.Add(this);
			}
		}
		finally
		{
			// Set even if window creation crashed, so the keep-alive Main thread can exit.
			lock (_hostsMutex)
			{
				_firstWindowCreated = true;
			}
		}
	}

	private static string GetAppId()
		=> Windows.ApplicationModel.Package.Current.Id.Name is { Length: > 0 } packageName
			? packageName
			: Assembly.GetEntryAssembly()?.GetName().Name ?? "codebrix.platform.app";

	// Event-pump thread: the shell surface has acked the configure / committed decoration
	// state; we apply the content size and repaint.
	private void OnShellConfigured(int width, int height, bool activated)
	{
		if (IsClosed)
		{
			return;
		}

		var sizeChanged = width > 0 && height > 0 && (width != _width || height != _height);
		if (sizeChanged)
		{
			_width = width;
			_height = height;
			QueueAction(this, _configureCallback);
		}

		if (activated != _activated)
		{
			_activated = activated;
			QueueAction(this, () => _focusCallback(activated));
		}

		if (_shown)
		{
			((IXamlRootHost)this).InvalidateRender();
		}
	}

	// Event-pump thread.
	private void OnShellCloseRequested()
		=> QueueAction(this, _closingCallback);

	/// <summary>
	/// Raised on the UI thread when the compositor-communicated window state changed:
	/// (isMaximized, isFullscreen). Feeds the presenter so external maximize/restore
	/// (titlebar button, keyboard shortcut) reflects in the WinUI API.
	/// </summary>
	internal event Action<bool, bool>? WindowStateChanged;

	// Event-pump thread.
	private void OnShellWindowStateChanged(bool maximized, bool fullscreen)
		=> QueueAction(this, () => WindowStateChanged?.Invoke(maximized, fullscreen));

	internal void Show()
	{
		_shown = true;
		QueueAction(this, () =>
		{
			_visibilityCallback(true);
			_configureCallback();
		});
		((IXamlRootHost)this).InvalidateRender();
	}

	internal void SetTitle(string title)
	{
		if (_shellSurface is { } shellSurface && !IsClosed)
		{
			shellSurface.SetTitle(title);
			_connection?.Flush();
		}
	}

	private void DestroyProtocolObjects()
	{
		// Per-surface add-on objects go first (the protocols require destroying them before
		// the wl_surface); the shell surface then owns and tears down the xdg_surface/
		// xdg_toplevel (or libdecor frame) plus the content wl_surface.
		_fractionalScale?.Destroy();
		_fractionalScale = null;
		_viewport?.Destroy();
		_viewport = null;
		_shellSurface?.Dispose();
		_connection?.Flush();
	}

	public static void CloseAllWindows()
	{
		List<WaylandXamlRootHost> hosts;
		lock (_hostsMutex)
		{
			hosts = _hosts.ToList();
		}

		foreach (var host in hosts)
		{
			Close(host);
		}
	}

	public static bool AllWindowsDone()
	{
		lock (_hostsMutex)
		{
			return _firstWindowCreated && _hosts.Count == 0;
		}
	}

	public static void Close(WaylandXamlRootHost host)
	{
		lock (_hostsMutex)
		{
			if (_hosts.Remove(host))
			{
				host._closed.SetResult();
			}
			else if (typeof(WaylandXamlRootHost).Log().IsEnabled(LogLevel.Error))
			{
				typeof(WaylandXamlRootHost).Log().Error($"{nameof(Close)} could not find the window host");
			}
		}
	}

	public static void QueueAction(IXamlRootHost host, Action action)
		=> host.RootElement?.Dispatcher.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(action));

	UIElement? IXamlRootHost.RootElement => _window.RootElement;

	void IXamlRootHost.InvalidateRender()
	{
		if (!_closed.Task.IsCompleted && _shown)
		{
			_renderTimer.Enabled = true;
		}
	}

	private Timer CreateRenderTimer()
	{
		var timer = new Timer
		{
			AutoReset = false,
			Interval = TimeSpan.FromSeconds(1.0 / FeatureConfiguration.CompositionTarget.FrameRate).TotalMilliseconds,
		};
		timer.Elapsed += (_, _) => GetOrCreateRenderer()?.Render();
		return timer;
	}

	private IWaylandRenderer? GetOrCreateRenderer()
	{
		if (IsClosed)
		{
			return null;
		}

		if (_renderer == null && _connection is { } connection && _wlSurface is { } surface)
		{
			// wl_shm software rendering is the default (universal, proven). The EGL/GPU path
			// (P7) is opt-in via CODEBRIX_WAYLAND_USE_GPU=1; it falls back to software if the
			// GL context cannot be created.
			var useGpu = string.Equals(
				Environment.GetEnvironmentVariable("CODEBRIX_WAYLAND_USE_GPU"), "1", StringComparison.Ordinal);
			if (useGpu)
			{
				try
				{
					_renderer = new WaylandEglRenderer(this, connection, surface);
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn("Wayland GPU (EGL) renderer unavailable; falling back to wl_shm software rendering.", e);
					}
					_renderer = new WaylandShmRenderer(this, connection, surface);
				}
			}
			else
			{
				_renderer = new WaylandShmRenderer(this, connection, surface);
			}

			UpdateRendererBackground();
		}

		return _renderer;
	}

	private void UpdateRendererBackground()
	{
		if (_window.Background is Microsoft.UI.Xaml.Media.SolidColorBrush brush)
		{
			_renderer?.SetBackgroundColor(new SkiaSharp.SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A));
		}
		else if (_window.Background is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("This platform only supports SolidColorBrush for the Window background");
			}
		}
	}
}
