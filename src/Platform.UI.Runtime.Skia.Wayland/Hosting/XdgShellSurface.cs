using System;
using System.Threading;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.XdgDecorationUnstableV1;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.XdgShell;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Shell surface driven directly over xdg-shell, requesting server-side decorations via
/// xdg-decoration-unstable-v1 where the compositor offers them. Used when libdecor is not
/// available; on a compositor that declines SSD (e.g. GNOME) this yields an undecorated
/// window, which is why libdecor is preferred when present (see <see cref="LibDecorShellSurface"/>).
/// </summary>
internal sealed class XdgShellSurface : IWaylandShellSurface
{
	private readonly WaylandConnection _connection;
	private readonly WlSurface _surface;
	private readonly XdgSurface _xdgSurface;
	private readonly XdgToplevel _toplevel;
	private readonly ZxdgToplevelDecorationV1? _decoration;

	private int _pendingWidth;
	private int _pendingHeight;
	private int _currentWidth;
	private int _currentHeight;
	private bool _activated;
	private readonly int _defaultWidth;
	private readonly int _defaultHeight;

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

	public XdgShellSurface(WaylandConnection connection, int defaultWidth, int defaultHeight)
	{
		_connection = connection;
		_defaultWidth = defaultWidth;
		_defaultHeight = defaultHeight;
		_currentWidth = defaultWidth;
		_currentHeight = defaultHeight;

		_surface = connection.Compositor.CreateSurface();
		_xdgSurface = connection.WmBase.GetXdgSurface(_surface, new XdgSurface.Listener.Relay
		{
			OnConfigure = OnSurfaceConfigure,
		});
		_toplevel = _xdgSurface.GetToplevel(new XdgToplevel.Listener.Relay
		{
			OnConfigure = OnToplevelConfigure,
			OnClose = _ => CloseRequested?.Invoke(),
		});

		if (connection.DecorationManager is { } decorationManager)
		{
			_decoration = decorationManager.GetToplevelDecoration(_toplevel);
			_decoration.SetMode(ZxdgToplevelDecorationV1.ModeEnum.ServerSide);
		}
	}

	private void OnToplevelConfigure(XdgToplevel _, int width, int height, ReadOnlySpan<XdgToplevel.StateEnum> states)
	{
		if (width > 0 && height > 0)
		{
			Interlocked.Exchange(ref _pendingWidth, width);
			Interlocked.Exchange(ref _pendingHeight, height);
		}

		_activated = false;
		foreach (var state in states)
		{
			if (state == XdgToplevel.StateEnum.Activated)
			{
				_activated = true;
				break;
			}
		}
	}

	private void OnSurfaceConfigure(XdgSurface xdgSurface, uint serial)
	{
		xdgSurface.AckConfigure(serial);

		var width = _pendingWidth > 0 ? _pendingWidth : _defaultWidth;
		var height = _pendingHeight > 0 ? _pendingHeight : _defaultHeight;
		_currentWidth = width;
		_currentHeight = height;

		Configured?.Invoke(width, height, _activated);
	}

	public void SetTitle(string title) => _toplevel.SetTitle(title);

	public void SetAppId(string appId) => _toplevel.SetAppId(appId);

	public void SetMaximized(bool maximized)
	{
		if (maximized)
		{
			_toplevel.SetMaximized();
		}
		else
		{
			_toplevel.UnsetMaximized();
		}
	}

	public void SetMinimized() => _toplevel.SetMinimized();

	public void SetFullscreen(bool fullscreen)
	{
		if (fullscreen)
		{
			_toplevel.SetFullscreen(null);
		}
		else
		{
			_toplevel.UnsetFullscreen();
		}
	}

	public void SetMinMaxSize(int minWidth, int minHeight, int maxWidth, int maxHeight)
	{
		_toplevel.SetMinSize(minWidth, minHeight);
		_toplevel.SetMaxSize(maxWidth, maxHeight);
	}

	public void MapInitial()
	{
		// Commit with no buffer: begins the configure handshake without mapping content.
		_surface.Commit();
		_connection.Flush();
	}

	public void Dispose()
	{
		_decoration?.Destroy();
		_toplevel.Destroy();
		_xdgSurface.Destroy();
		_surface.Destroy();
		_connection.Flush();
	}
}
