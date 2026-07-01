using System;
using System.Collections.Generic;
using System.Threading;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Interop;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.FractionalScaleV1;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Viewporter;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.CursorShapeV1;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.XdgDecorationUnstableV1;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.XdgShell;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// The application-wide Wayland connection: one wl_display shared by every window,
/// the registry-bound globals, and the event-pump thread that dispatches compositor
/// events into the NWayland-generated listeners.
/// </summary>
/// <remarks>
/// Unlike the X11 head (which opens two X connections per window), Wayland clients
/// conventionally share a single display connection; per-window state lives on
/// per-window wl_surface/xdg_surface objects created from the shared globals.
/// </remarks>
internal sealed class WaylandConnection : IDisposable
{
	private static readonly object _instanceLock = new();
	private static WaylandConnection? _instance;

	private readonly Dictionary<string, (uint Name, uint Version)> _globals = new();
	private readonly List<OutputInfo> _outputs = new();
	private readonly Thread _eventPumpThread;
	private volatile bool _disposed;

	public static WaylandConnection Instance
	{
		get
		{
			lock (_instanceLock)
			{
				return _instance ??= new WaylandConnection();
			}
		}
	}

	/// <summary>
	/// The authoritative fail-fast check (decision 2.(6) of the plan): attempts the actual
	/// wl_display_connect rather than trusting WAYLAND_DISPLAY, which can be unset with a
	/// live default socket, or set but stale.
	/// </summary>
	public static WaylandConnection ConnectOrThrow()
	{
		try
		{
			return Instance;
		}
		catch (WaylandCompositorMissingException)
		{
			throw;
		}
		catch (Exception e)
		{
			throw new WaylandCompositorMissingException(e);
		}
	}

	public WlDisplay Display { get; }
	public WlCompositor Compositor { get; }
	public WlShm Shm { get; }
	public XdgWmBase WmBase { get; }
	public WlSeat? Seat { get; }
	public WaylandSeatManager SeatManager { get; }

	// Shared libdecor context (lazily created on first window when libdecor is present).
	// libdecor negotiates SSD vs CSD internally, giving decorated windows on GNOME too.
	private IntPtr _libdecorContext;
	private LibDecor.LibDecorErrorDelegate? _libdecorErrorDelegate;
	private LibDecor.libdecor_interface _libdecorInterface;
	private readonly object _libdecorGate = new();

	/// <summary>
	/// Returns the shared libdecor context, creating it on first use, or IntPtr.Zero when
	/// libdecor is unavailable (callers then fall back to raw xdg-shell decorations).
	/// </summary>
	public IntPtr GetLibDecorContext()
	{
		lock (_libdecorGate)
		{
			if (_libdecorContext != IntPtr.Zero)
			{
				return _libdecorContext;
			}

			if (!LibDecor.IsAvailable())
			{
				return IntPtr.Zero;
			}

			_libdecorErrorDelegate = OnLibDecorError;
			_libdecorInterface = new LibDecor.libdecor_interface
			{
				error = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_libdecorErrorDelegate),
			};
			_libdecorContext = LibDecor.libdecor_new(Display.Handle, ref _libdecorInterface);
			return _libdecorContext;
		}
	}

	private void OnLibDecorError(IntPtr context, LibDecor.libdecor_error error, IntPtr message)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			var text = message != IntPtr.Zero ? System.Runtime.InteropServices.Marshal.PtrToStringUTF8(message) : null;
			this.Log().Error($"libdecor error {error}: {text}");
		}
	}
	public ZxdgDecorationManagerV1? DecorationManager { get; }
	public WpViewporter? Viewporter { get; }
	public WpFractionalScaleManagerV1? FractionalScaleManager { get; }
	public WlDataDeviceManager? DataDeviceManager { get; }
	public WpCursorShapeManagerV1? CursorShapeManager { get; }

	internal sealed class OutputInfo
	{
		public WlOutput? Output;
		public int Width;
		public int Height;
		public int Scale = 1;
	}

	/// <summary>Best-known information about the primary (first-advertised) output.</summary>
	public (int WidthPx, int HeightPx, int Scale) PrimaryOutput
	{
		get
		{
			lock (_outputs)
			{
				return _outputs.Count > 0
					? (_outputs[0].Width, _outputs[0].Height, _outputs[0].Scale)
					: (0, 0, 1);
			}
		}
	}

	internal event Action? OutputsChanged;

	private readonly WlRegistry _registry;

	private WaylandConnection()
	{
		try
		{
			Display = WlDisplay.Connect();
		}
		catch (Exception e)
		{
			throw new WaylandCompositorMissingException(e);
		}

		// Kept alive for the connection lifetime: late global binds (e.g. hot-plugged
		// wl_output) need a live registry proxy.
		_registry = Display.GetRegistry(new WlRegistry.Listener.Relay
		{
			OnGlobal = (_, name, @interface, version) =>
			{
				_globals[@interface] = (name, version);
				if (@interface == "wl_output")
				{
					BindOutput(name, version);
				}
			},
			OnGlobalRemove = (_, name) => OnGlobalRemoved(name),
		});

		// First roundtrip: collect the advertised globals (wl_output binds happen inline).
		_ = Display.Roundtrip();

		Compositor = BindRequired<WlCompositor>(_registry, "wl_compositor");
		Shm = BindRequired<WlShm>(_registry, "wl_shm");
		WmBase = Bind<XdgWmBase>(_registry, "xdg_wm_base", new XdgWmBase.Listener.Relay
		{
			// The compositor pings to check responsiveness; failing to pong gets the app
			// tagged as unresponsive (and possibly killed).
			OnPing = (wmBase, serial) => wmBase.Pong(serial),
		}) ?? throw new NWaylandException(
			"The Wayland compositor does not advertise xdg_wm_base (xdg-shell); desktop windows cannot be created.");

		SeatManager = new WaylandSeatManager(this);
		Seat = Bind<WlSeat>(_registry, "wl_seat", new WlSeat.Listener.Relay
		{
			OnCapabilities = (_, capabilities) => SeatManager.OnCapabilities(capabilities),
		});
		if (Seat is { } seat)
		{
			SeatManager.AttachSeat(seat);
		}
		DecorationManager = Bind<ZxdgDecorationManagerV1>(_registry, "zxdg_decoration_manager_v1");
		Viewporter = Bind<WpViewporter>(_registry, "wp_viewporter");
		FractionalScaleManager = Bind<WpFractionalScaleManagerV1>(_registry, "wp_fractional_scale_manager_v1");
		DataDeviceManager = Bind<WlDataDeviceManager>(_registry, "wl_data_device_manager");
		CursorShapeManager = Bind<WpCursorShapeManagerV1>(_registry, "wp_cursor_shape_manager_v1");

		// Second roundtrip: let the bound globals deliver their initial state
		// (wl_shm formats, wl_output geometry/mode/scale, wl_seat capabilities).
		_ = Display.Roundtrip();

		_eventPumpThread = new Thread(EventPump)
		{
			Name = "Wayland Event Pump",
			IsBackground = true,
		};
		_eventPumpThread.Start();
	}

	private T? Bind<T>(WlRegistry registry, string interfaceName, IWlEventsListener? listener = null)
		where T : WlProxy, IWlProxyTypeDescriptorProvider
	{
		if (!_globals.TryGetValue(interfaceName, out var global))
		{
			return null;
		}

		var version = Math.Min(global.Version, (uint)T.ProxyType.Interface.Version);
		return registry.Bind<T>(global.Name, version, listener);
	}

	private T BindRequired<T>(WlRegistry registry, string interfaceName)
		where T : WlProxy, IWlProxyTypeDescriptorProvider
		=> Bind<T>(registry, interfaceName)
			?? throw new NWaylandException($"The Wayland compositor does not advertise the required global '{interfaceName}'.");

	private void BindOutput(uint name, uint version)
	{
		var info = new OutputInfo();
		lock (_outputs)
		{
			_outputs.Add(info);
		}

		// Bound from the registry listener during a roundtrip/dispatch, which is safe:
		// NWayland allows requests while dispatching on the same thread.
		var boundVersion = Math.Min(version, (uint)WlOutput.ProxyType.Interface.Version);
		info.Output = WlOutput.Bind(_registry, name, boundVersion, new WlOutput.Listener.Relay
		{
			OnMode = (_, flags, width, height, _) =>
			{
				if ((flags & WlOutput.ModeEnum.Current) != 0)
				{
					info.Width = width;
					info.Height = height;
				}
			},
			OnScale = (_, factor) => info.Scale = factor,
			OnDone = _ => OutputsChanged?.Invoke(),
		});
	}

	private void OnGlobalRemoved(uint name)
	{
		// TODO(P5): handle output removal. Harmless to ignore for fixed-output setups.
	}

	private void EventPump()
	{
		int fd;
		try
		{
			fd = Display.GetFd();
		}
		catch (ObjectDisposedException)
		{
			return;
		}

		while (!_disposed)
		{
			try
			{
				// Standard libwayland multi-threaded read pattern: drain the queue, declare
				// read intent, flush outgoing requests, poll without holding any dispatch
				// lock (so other threads can marshal requests + flush freely), then read
				// and dispatch. Display.Dispose() shuts the socket down, which wakes the
				// poll with POLLHUP and lets the pump exit.
				_ = Display.DispatchPending();

				// libdecor shares our wl_display; give it a chance to process its own state
				// (it added globals/listeners on the same connection). Non-blocking.
				if (_libdecorContext != IntPtr.Zero)
				{
					_ = LibDecor.libdecor_dispatch(_libdecorContext, 0);
				}

				if (Display.PrepareRead() != 0)
				{
					continue; // events already queued; loop around and dispatch them
				}

				_ = Display.Flush();

				var pollFd = new Libc.PollFd { fd = fd, events = Libc.POLLIN };
				var polled = Libc.poll(ref pollFd, 1, -1);

				if (polled > 0 && (pollFd.revents & Libc.POLLIN) != 0)
				{
					_ = Display.ReadEvents();
				}
				else
				{
					Display.CancelRead();
					if (polled > 0 && (pollFd.revents & (Libc.POLLERR | Libc.POLLHUP)) != 0)
					{
						if (!_disposed && this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error("The Wayland display connection was closed by the compositor.");
						}
						break;
					}
				}
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (Exception e)
			{
				if (_disposed)
				{
					break;
				}

				// Listener exceptions (rethrown by DispatchPending) are application bugs;
				// log and keep the connection alive rather than tearing down the app.
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Unhandled exception on the Wayland event pump", e);
				}

				if (Display.GetProtocolError() is { } protocolError)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Fatal Wayland protocol error: {protocolError}");
					}
					break;
				}
			}
		}
	}

	/// <summary>Sends buffered requests to the compositor. Cheap; call after request batches.</summary>
	public void Flush()
	{
		if (!_disposed)
		{
			_ = Display.Flush();
		}
	}

	public void Dispose()
	{
		lock (_instanceLock)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_instance = null;
		}

		if (_libdecorContext != IntPtr.Zero)
		{
			LibDecor.libdecor_unref(_libdecorContext);
			_libdecorContext = IntPtr.Zero;
		}

		Display.Dispose(); // shuts down the socket, waking the pump
		if (Thread.CurrentThread != _eventPumpThread)
		{
			_ = _eventPumpThread.Join(TimeSpan.FromSeconds(5));
		}
	}
}
