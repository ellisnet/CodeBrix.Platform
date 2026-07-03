using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;
using CodeBrix.Platform.Foundation.Logging;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

internal class WaylandNativeOverlappedPresenter(WaylandXamlRootHost host) : INativeOverlappedPresenter
{
	private OverlappedPresenterState _state = OverlappedPresenterState.Restored;

	public void SetIsResizable(bool isResizable)
	{
		// Communicated on Wayland via xdg_toplevel min/max size: equal min and max
		// means fixed-size. TODO(P4): wire to the decoration layer's size constraints.
	}

	public void SetIsModal(bool isModal)
	{
		// TODO: modal windows (xdg_dialog_v1 once added to the pinned protocols).
	}

	public void SetIsMinimizable(bool isMinimizable)
	{
		// Not expressible in xdg-shell; compositor/decoration policy decides.
		WaylandNotSupported.WarnOnce(typeof(WaylandNativeOverlappedPresenter),
			"OverlappedPresenter.IsMinimizable",
			"xdg-shell cannot remove the minimize capability; compositor/decoration policy decides.");
	}

	public void SetIsMaximizable(bool isMaximizable)
	{
		// Not expressible in xdg-shell; compositor/decoration policy decides.
		WaylandNotSupported.WarnOnce(typeof(WaylandNativeOverlappedPresenter),
			"OverlappedPresenter.IsMaximizable",
			"xdg-shell cannot remove the maximize capability; compositor/decoration policy decides.");
	}

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop)
	{
		// Not expressible in core Wayland/xdg-shell for regular apps (needs
		// wlr-layer-shell, which is compositor-specific). No-op by design.
		WaylandNotSupported.WarnOnce(typeof(WaylandNativeOverlappedPresenter),
			"OverlappedPresenter.IsAlwaysOnTop",
			"core Wayland/xdg-shell has no always-on-top for regular application windows (wlr-layer-shell is compositor-specific).");
	}

	public void Maximize()
	{
		host.ShellSurface?.SetMaximized(true);
		_state = OverlappedPresenterState.Maximized;
	}

	public void Minimize(bool activateWindow)
	{
		host.ShellSurface?.SetMinimized();
		_state = OverlappedPresenterState.Minimized;
	}

	public void Restore(bool activateWindow)
	{
		host.ShellSurface?.SetMaximized(false);
		_state = OverlappedPresenterState.Restored;
	}

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		// Wayland decorations are all-or-nothing (no border-without-titlebar): the frame is
		// shown unless the caller asked for a fully undecorated window.
		host.ShellSurface?.SetDecorationsVisible(hasBorder || hasTitleBar);
	}

	/// <summary>
	/// UI thread, from <see cref="WaylandXamlRootHost.WindowStateChanged"/>: reflects
	/// compositor-side maximize/restore (titlebar button, keyboard shortcut) into the
	/// presenter state instead of only tracking our own requests.
	/// </summary>
	internal void OnNativeWindowStateChanged(bool maximized, bool fullscreen)
	{
		if (_state != OverlappedPresenterState.Minimized || maximized)
		{
			_state = maximized ? OverlappedPresenterState.Maximized : OverlappedPresenterState.Restored;
		}
	}

	public void SetSizeConstraints(int? preferredMinimumWidth, int? preferredMinimumHeight, int? preferredMaximumWidth, int? preferredMaximumHeight)
	{
		if (host.ShellSurface is { } shellSurface && !host.IsClosed)
		{
			// Sizes are logical (surface-local) coordinates; 0 means unconstrained.
			shellSurface.SetMinMaxSize(preferredMinimumWidth ?? 0, preferredMinimumHeight ?? 0, preferredMaximumWidth ?? 0, preferredMaximumHeight ?? 0);
		}
	}

	public OverlappedPresenterState State => _state;
}
