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

	// Written on the event-pump thread, read on the UI thread after a queued callback.
	private volatile bool _activated;
	private volatile bool _shown;

	private int _width;
	private int _height;

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

	public Task Closed { get; }

	public bool IsClosed => _closed.Task.IsCompleted;

	internal IWaylandShellSurface? ShellSurface => _shellSurface;

	internal WaylandConnection? Connection => _connection;

	internal WaylandPointerInputSource? PointerSource { get; private set; }
	internal WaylandKeyboardInputSource? KeyboardSource { get; private set; }

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

			_shellSurface.Configured += OnShellConfigured;
			_shellSurface.CloseRequested += OnShellCloseRequested;

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
		// The shell surface owns and tears down the xdg_surface/xdg_toplevel (or libdecor
		// frame) plus the content wl_surface.
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
