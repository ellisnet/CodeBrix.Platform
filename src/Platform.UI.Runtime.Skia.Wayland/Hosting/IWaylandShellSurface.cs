using System;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Abstracts the shell integration for one window so the host is agnostic to whether the
/// window is decorated by the compositor (raw xdg-shell + xdg-decoration) or by libdecor
/// (client-side decorations). This is the resolution of the plan's open question V3
/// (the libdecor↔xdg-shell ownership boundary): whichever strategy owns the xdg_surface /
/// xdg_toplevel lifecycle implements this interface; the host only ever talks to the
/// content <see cref="Surface"/> and these window-level operations.
/// </summary>
internal interface IWaylandShellSurface : IDisposable
{
	/// <summary>The content wl_surface the renderer attaches buffers to.</summary>
	WlSurface Surface { get; }

	/// <summary>
	/// Raised (on the event-pump thread) when the compositor has configured the window.
	/// Carries the content-region size to render at and whether the window is activated.
	/// The implementation has already acked the configure / committed decoration state by
	/// the time this fires; the host responds by resizing the renderer and rendering.
	/// </summary>
	event Action<int, int, bool> Configured;

	/// <summary>Raised (on the event-pump thread) when the user/compositor asks to close.</summary>
	event Action CloseRequested;

	/// <summary>
	/// Raised (on the event-pump thread) when the compositor-communicated window state
	/// changed: (isMaximized, isFullscreen). Lets external maximize/restore (titlebar
	/// button, keyboard shortcut) reflect back into the WinUI presenter state.
	/// </summary>
	event Action<bool, bool> WindowStateChanged;

	void SetTitle(string title);

	/// <summary>
	/// Shows or hides the window decorations (title bar + borders): libdecor visibility on
	/// the libdecor path, the xdg-decoration mode on the raw xdg-shell path.
	/// </summary>
	void SetDecorationsVisible(bool visible);

	void SetAppId(string appId);

	void SetMaximized(bool maximized);

	void SetMinimized();

	void SetFullscreen(bool fullscreen);

	void SetMinMaxSize(int minWidth, int minHeight, int maxWidth, int maxHeight);

	/// <summary>
	/// Performs the initial map/commit that starts the configure handshake. Called once,
	/// after the caller has wired the events.
	/// </summary>
	void MapInitial();
}
