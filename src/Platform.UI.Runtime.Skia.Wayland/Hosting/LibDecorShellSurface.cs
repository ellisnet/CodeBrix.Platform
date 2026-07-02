using System;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Shell surface driven by libdecor. libdecor owns the xdg_surface / xdg_toplevel lifecycle
/// and negotiates server-side vs client-side decorations with the compositor internally, so
/// this one path yields a properly decorated window on GNOME (CSD) as well as on KDE / wlroots
/// / muffin (SSD) — matching how SDL and GLFW integrate libdecor.
/// </summary>
/// <remarks>
/// The libdecor context is shared per-application (owned by <see cref="WaylandConnection"/>);
/// its events are pumped alongside the wl_display in the connection's event pump.
/// </remarks>
internal sealed class LibDecorShellSurface : IWaylandShellSurface
{
	private readonly WaylandConnection _connection;
	private readonly WlSurface _surface;
	private readonly int _defaultWidth;
	private readonly int _defaultHeight;

	private IntPtr _frame;
	private GCHandle _selfHandle;

	// Kept alive for the frame's lifetime so the native side can call back.
	private readonly LibDecor.FrameConfigureDelegate _configureDelegate;
	private readonly LibDecor.FrameCloseDelegate _closeDelegate;
	private readonly LibDecor.FrameCommitDelegate _commitDelegate;
	// Unmanaged copy of the libdecor_frame_interface: libdecor retains the pointer and calls
	// through it for the frame's lifetime, so it cannot point at (movable) managed memory.
	private IntPtr _frameInterfacePtr;

	private int _currentWidth;
	private int _currentHeight;

	// libdecor's GTK plugin is single-threaded; every libdecor call must hold this gate
	// (shared with the connection's event pump — see WaylandConnection.LibDecorGate).
	private object Gate => _connection.LibDecorGate;

	public event Action<int, int, bool>? Configured;
	public event Action? CloseRequested;

	event Action<int, int, bool> IWaylandShellSurface.Configured
	{
		add => Configured += value;
		remove => Configured -= value;
	}

	event Action IWaylandShellSurface.CloseRequested
	{
		add => CloseRequested += value;
		remove => CloseRequested -= value;
	}

	public WlSurface Surface => _surface;

	public LibDecorShellSurface(WaylandConnection connection, IntPtr libdecorContext, int defaultWidth, int defaultHeight)
	{
		_connection = connection;
		_defaultWidth = _currentWidth = defaultWidth;
		_defaultHeight = _currentHeight = defaultHeight;

		_surface = connection.Compositor.CreateSurface();

		_configureDelegate = OnConfigure;
		_closeDelegate = OnClose;
		_commitDelegate = OnCommit;
		var frameInterface = new LibDecor.libdecor_frame_interface
		{
			configure = Marshal.GetFunctionPointerForDelegate(_configureDelegate),
			close = Marshal.GetFunctionPointerForDelegate(_closeDelegate),
			commit = Marshal.GetFunctionPointerForDelegate(_commitDelegate),
		};
		_frameInterfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf<LibDecor.libdecor_frame_interface>());
		Marshal.StructureToPtr(frameInterface, _frameInterfacePtr, false);

		_selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
		lock (Gate)
		{
			_frame = LibDecor.libdecor_decorate(libdecorContext, _surface.Handle, _frameInterfacePtr, GCHandle.ToIntPtr(_selfHandle));
		}
		if (_frame == IntPtr.Zero)
		{
			_selfHandle.Free();
			Marshal.FreeHGlobal(_frameInterfacePtr);
			_frameInterfacePtr = IntPtr.Zero;
			throw new InvalidOperationException("libdecor_decorate returned NULL.");
		}
	}

	private static LibDecorShellSurface? FromUserData(IntPtr userData)
		=> userData != IntPtr.Zero && GCHandle.FromIntPtr(userData).Target is LibDecorShellSurface self ? self : null;

	private void OnConfigure(IntPtr frame, IntPtr configuration, IntPtr userData)
	{
		if (!LibDecor.libdecor_configuration_get_content_size(configuration, frame, out var width, out var height)
			|| width <= 0 || height <= 0)
		{
			width = _defaultWidth;
			height = _defaultHeight;
		}

		_currentWidth = width;
		_currentHeight = height;

		// Acknowledge the configure by committing a state of the chosen content size.
		// (Invoked from a dispatch that already holds the gate; the lock is reentrant.)
		lock (Gate)
		{
			var state = LibDecor.libdecor_state_new(width, height);
			LibDecor.libdecor_frame_commit(frame, state, configuration);
			LibDecor.libdecor_state_free(state);
		}

		var activated = LibDecor.libdecor_configuration_get_window_state(configuration, out var windowState)
			&& (windowState & LibDecor.libdecor_window_state.LIBDECOR_WINDOW_STATE_ACTIVE) != 0;

		Configured?.Invoke(width, height, activated);
	}

	private void OnClose(IntPtr frame, IntPtr userData) => CloseRequested?.Invoke();

	private void OnCommit(IntPtr frame, IntPtr userData)
	{
		// libdecor asks us to (re)commit the content surface, e.g. after a decoration redraw.
		// Re-raise the last configured size so the host repaints and commits.
		Configured?.Invoke(_currentWidth, _currentHeight, true);
	}

	public void SetTitle(string title)
	{
		if (_frame != IntPtr.Zero)
		{
			lock (Gate)
			{
				LibDecor.libdecor_frame_set_title(_frame, title);
			}
		}
	}

	public void SetAppId(string appId)
	{
		if (_frame != IntPtr.Zero)
		{
			lock (Gate)
			{
				LibDecor.libdecor_frame_set_app_id(_frame, appId);
			}
		}
	}

	public void SetMaximized(bool maximized)
	{
		if (_frame == IntPtr.Zero)
		{
			return;
		}
		lock (Gate)
		{
			if (maximized)
			{
				LibDecor.libdecor_frame_set_maximized(_frame);
			}
			else
			{
				LibDecor.libdecor_frame_unset_maximized(_frame);
			}
		}
	}

	public void SetMinimized()
	{
		if (_frame != IntPtr.Zero)
		{
			lock (Gate)
			{
				LibDecor.libdecor_frame_set_minimized(_frame);
			}
		}
	}

	public void SetFullscreen(bool fullscreen)
	{
		if (_frame == IntPtr.Zero)
		{
			return;
		}
		lock (Gate)
		{
			if (fullscreen)
			{
				LibDecor.libdecor_frame_set_fullscreen(_frame, IntPtr.Zero);
			}
			else
			{
				LibDecor.libdecor_frame_unset_fullscreen(_frame);
			}
		}
	}

	public void SetMinMaxSize(int minWidth, int minHeight, int maxWidth, int maxHeight)
	{
		// libdecor exposes min/max via libdecor_frame_set_min_content_size /
		// set_max_content_size; not bound yet (rarely used). No-op for now.
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("SetMinMaxSize is not yet wired for the libdecor path.");
		}
	}

	public void MapInitial()
	{
		lock (Gate)
		{
			LibDecor.libdecor_frame_map(_frame);
		}
		_connection.Flush();
	}

	public void Dispose()
	{
		if (_frame != IntPtr.Zero)
		{
			lock (Gate)
			{
				LibDecor.libdecor_frame_unref(_frame);
			}
			_frame = IntPtr.Zero;
		}

		_surface.Destroy();
		_connection.Flush();

		// Only after the frame is gone: libdecor calls through this memory until unref.
		if (_frameInterfacePtr != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_frameInterfacePtr);
			_frameInterfacePtr = IntPtr.Zero;
		}

		if (_selfHandle.IsAllocated)
		{
			_selfHandle.Free();
		}
	}
}
