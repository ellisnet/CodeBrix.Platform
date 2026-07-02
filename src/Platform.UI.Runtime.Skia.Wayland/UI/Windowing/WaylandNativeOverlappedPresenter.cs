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
	}

	public void SetIsMaximizable(bool isMaximizable)
	{
		// Not expressible in xdg-shell; compositor/decoration policy decides.
	}

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop)
	{
		// Not expressible in core Wayland/xdg-shell for regular apps (needs
		// wlr-layer-shell, which is compositor-specific). No-op by design.
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Always-on-top is not available on Wayland.");
		}
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
		// TODO(P4): toggle decorations (xdg-decoration mode / libdecor visibility).
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
