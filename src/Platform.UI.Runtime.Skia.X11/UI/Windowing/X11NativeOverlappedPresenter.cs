using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Runtime.Skia;
namespace CodeBrix.Platform.WinUI.Runtime.Skia.X11; //Was previously: Uno.WinUI.Runtime.Skia.X11

internal class X11NativeOverlappedPresenter(X11Window x11Window, X11WindowWrapper wrapper) : INativeOverlappedPresenter
{
	// EWMH has _NET_WM_ALLOWED_ACTIONS: https://specifications.freedesktop.org/wm-spec/wm-spec-1.3.html#idm45912237317440
	// but it turns out that these shouldn't be set by the client, but only read to see what actions are available.
	// Setting them doesn't really do anything.
	// There is also this from ICCCM, but I don't think people use this anymore:
	// https://specifications.freedesktop.org/wm-spec/wm-spec-1.3.html#NORESIZE
	// What works is using the Motif WM hints, which aren't standardized or documented anywhere
	// https://stackoverflow.com/a/13788970

	// This doesn't prevent resizing using xlib calls (e.g. XResizeWindow), so settings the size ApplicationView for example would still work.
	public void SetIsResizable(bool isResizable) => X11Helper.SetMotifWMFunctions(x11Window, isResizable, (IntPtr)MotifFunctions.Resize);

	public void SetIsModal(bool isModal)
	{
		// TODO: modal windows
	}

	// Making the window unminimizable removes the `-` button in the title bar and greys out the `Minimize` option if
	// you open the Menu, but the window will still be minimizable if you click on the window icon in the dock/task bar.
	// This is at least what happens on XFCE. Since these are just "hints", each WM can choose what it means to be "minimizable" differently.
	public void SetIsMinimizable(bool isMinimizable) => X11Helper.SetMotifWMFunctions(x11Window, isMinimizable, (IntPtr)MotifFunctions.Minimize);

	public void SetIsMaximizable(bool isMaximizable) => X11Helper.SetMotifWMFunctions(x11Window, isMaximizable, (IntPtr)MotifFunctions.Maximize);

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop)
	{
		X11Helper.SetWMHints(
			x11Window,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE),
			isAlwaysOnTop ? 1 : 0,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_ABOVE));
	}

	public void Maximize()
	{
		X11Helper.SetWMHints(
			x11Window,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE),
			1,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_HORZ),
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_VERT));
	}

	public void Minimize(bool activateWindow)
	{
		using var lockDiposable = X11Helper.XLock(x11Window.Display);

		// Minimizing while in full screen could be buggy depending on the implementation
		// https://stackoverflow.com/questions/6381098/minimize-fullscreen-xlib-opengl-window
		wrapper.SetFullScreenMode(false);

		// XLib.XScreenNumberOfScreen(x11Window.Display, screen) is buggy. We use the default screen instead (which should be fine for 99% of cases)
		_ = XLib.XIconifyWindow(x11Window.Display, x11Window.Window, XLib.XDefaultScreen(x11Window.Display));
		_ = XLib.XFlush(x11Window.Display);
	}

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		// Border doesn't seem to do anything except show the title bar even if !hasTitleBar, which is fine for now,
		// since it doesn't do anything on WinUI either.
		// X11Helper.SetMotifWMDecorations(x11Window, hasBorder, (IntPtr)MotifDecorations.Border);
		X11Helper.SetMotifWMDecorations(x11Window, hasTitleBar, (IntPtr)MotifDecorations.Title);
	}

	public void Restore(bool activateWindow)
	{
		// WPE1-14: Restore used to only raise the window (https://stackoverflow.com/a/30256233), so a maximized window
		// stayed maximized and a minimized one stayed iconified on EWMH window managers (Cinnamon/Muffin, for one).
		// Like the Win32 head's SW_SHOWNORMAL it now leaves both states: the maximized atoms are removed through a
		// _NET_WM_STATE request and an iconified window is mapped again and activated (_NET_ACTIVE_WINDOW).
		using var lockDiposable = X11Helper.XLock(x11Window.Display);

		var display = x11Window.Display;
		var state = GetWMState();
		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(display, x11Window.Window, ref attributes);

		var plan = X11WindowStateRules.PlanRestore(
			wasShown: wrapper.WasShown,
			mapState: attributes.map_state,
			hidden: state.Contains(X11Helper.GetAtom(display, X11Helper._NET_WM_STATE_HIDDEN)),
			maximized: state.Contains(X11Helper.GetAtom(display, X11Helper._NET_WM_STATE_MAXIMIZED_HORZ))
				|| state.Contains(X11Helper.GetAtom(display, X11Helper._NET_WM_STATE_MAXIMIZED_VERT)),
			activateWindow: activateWindow);

		if ((plan & X11RestoreActions.Unmaximize) != 0)
		{
			X11Helper.SetWMHints(
				x11Window,
				X11Helper.GetAtom(display, X11Helper._NET_WM_STATE),
				0, // _NET_WM_STATE_REMOVE
				X11Helper.GetAtom(display, X11Helper._NET_WM_STATE_MAXIMIZED_HORZ),
				X11Helper.GetAtom(display, X11Helper._NET_WM_STATE_MAXIMIZED_VERT));
		}

		if ((plan & X11RestoreActions.Deiconify) != 0)
		{
			// ICCCM: mapping an iconic window asks the window manager to make it normal again; EWMH: a
			// _NET_ACTIVE_WINDOW request (source indication 1 = an application) also un-minimizes it.
			_ = XLib.XMapWindow(display, x11Window.Window);
			X11Helper.SetWMHints(
				x11Window,
				X11Helper.GetAtom(display, X11Helper._NET_ACTIVE_WINDOW),
				1,
				X11Helper.CurrentTime);
		}

		if ((plan & X11RestoreActions.Activate) != 0)
		{
			wrapper.Activate();
		}

		_ = XLib.XFlush(display);
	}

	public OverlappedPresenterState State
	{
		get
		{
			using var _1 = X11Helper.XLock(x11Window.Display);

			var minimized = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_HIDDEN);
			var maximizedHorizontal = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_HORZ);
			var maximizedVertical = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_VERT);

			return X11WindowStateRules.ToPresenterState(GetWMState(), minimized, maximizedHorizontal, maximizedVertical);
		}
	}

	private unsafe IntPtr[] GetWMState()
	{
		using var _1 = X11Helper.XLock(x11Window.Display);

		var _2 = XLib.XGetWindowProperty(
			x11Window.Display,
			x11Window.Window,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE),
			0,
			X11Helper.LONG_LENGTH,
			false,
			X11Helper.AnyPropertyType,
			out IntPtr actualType,
			out int actual_format,
			out IntPtr nItems,
			out _,
			out IntPtr prop);

		using var _3 = new DisposableStruct<IntPtr>(static p => { _ = XLib.XFree(p); }, prop);

		if (actualType == X11Helper.None)
		{
			// A window the window manager has not mapped yet legitimately has no _NET_WM_STATE: the
			// property is set when the window is managed. SetNative -> Restore(false) -> here runs on
			// every launch, before the first map, for every application whether or not it ever touches
			// the presenter, so treating that as an EWMH fault put two Error lines in every launch log.
			// An absent property on a MAPPED window is still a real EWMH complaint.
			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(x11Window.Display, x11Window.Window, ref attributes);

			if (X11WindowStateRules.IsMissingWMStateExpected(attributes.map_state))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"{X11Helper._NET_WM_STATE} does not exist on the window yet; it has not been mapped, so {nameof(OverlappedPresenterState)} is {nameof(OverlappedPresenterState.Restored)}.");
				}
			}
			else if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Couldn't get {nameof(OverlappedPresenterState)}: {X11Helper._NET_WM_STATE} does not exist on the window. Make sure you use an EWMH-compliant WM.");
			}

			return Array.Empty<IntPtr>();
		}

		Debug.Assert(actual_format == 32);
		var span = new Span<IntPtr>(prop.ToPointer(), (int)nItems);

		return span.ToArray();
	}

	/// <summary>
	/// Applies the <see cref="OverlappedPresenter"/> size constraints, which are EFFECTIVE PIXELS of
	/// the CLIENT area, to WM_NORMAL_HINTS, which is raw device pixels of the client window.
	/// </summary>
	/// <param name="preferredMinimumWidth">The minimum width in effective pixels, or null for none.</param>
	/// <param name="preferredMinimumHeight">The minimum height in effective pixels, or null for none.</param>
	/// <param name="preferredMaximumWidth">The maximum width in effective pixels, or null for none.</param>
	/// <param name="preferredMaximumHeight">The maximum height in effective pixels, or null for none.</param>
	public unsafe void SetSizeConstraints(int? preferredMinimumWidth, int? preferredMinimumHeight, int? preferredMaximumWidth, int? preferredMaximumHeight)
	{
		// One conversion, one rule: the numbers arrive in effective pixels and XSetWMNormalHints wants
		// raw pixels, so everything below is multiplied by the window's scale - see item 8.
		var scale = wrapper.RasterizationScale;
		var minWidth = WindowSizeConversion.LogicalToNative(preferredMinimumWidth ?? 0, scale);
		var minHeight = WindowSizeConversion.LogicalToNative(preferredMinimumHeight ?? 0, scale);
		var maxWidth = WindowSizeConversion.LogicalToNative(preferredMaximumWidth ?? int.MaxValue, scale);
		var maxHeight = WindowSizeConversion.LogicalToNative(preferredMaximumHeight ?? int.MaxValue, scale);
		XSizeHints hints = new();
		hints.min_width = minWidth;
		hints.min_height = minHeight;
		hints.max_width = maxWidth;
		hints.max_height = maxHeight;
		hints.flags = (int)XSizeHintsFlags.PMinSize | (int)XSizeHintsFlags.PMaxSize;
		XLib.XSetWMNormalHints(x11Window.Display, x11Window.Window, ref hints);
	}

}

/// <summary>
/// The pure rules behind <see cref="X11NativeOverlappedPresenter"/>'s reading of the EWMH window
/// state. They live in their own class so that a host-free unit test can exercise them without
/// loading the presenter, which implements an interface only a running head can see.
/// </summary>
internal static class X11WindowStateRules
{
	/// <summary>
	/// Decides whether a window with no <c>_NET_WM_STATE</c> property is simply too young to have one.
	/// A window that has never been mapped is not managed by the window manager yet, so the absence is
	/// expected and says nothing about the window manager's EWMH compliance; on any other map state the
	/// property should exist and its absence is worth an error.
	/// </summary>
	/// <param name="mapState">The window's <c>map_state</c>, as read by <c>XGetWindowAttributes</c>.</param>
	/// <returns>True when the missing property is expected rather than a fault.</returns>
	internal static bool IsMissingWMStateExpected(MapState mapState)
		=> mapState == MapState.IsUnmapped;

	/// <summary>
	/// Maps the atoms of a window's <c>_NET_WM_STATE</c> to the presenter state (WPE1-14). Hidden wins over maximized
	/// wherever it appears in the list: a maximized window that is minimized keeps its maximized atoms, and the old
	/// first-atom-wins loop reported such a window as Maximized when the window manager listed those atoms first.
	/// </summary>
	/// <param name="atoms">The atoms of the window's <c>_NET_WM_STATE</c>.</param>
	/// <param name="hidden">The <c>_NET_WM_STATE_HIDDEN</c> atom.</param>
	/// <param name="maximizedHorizontal">The <c>_NET_WM_STATE_MAXIMIZED_HORZ</c> atom.</param>
	/// <param name="maximizedVertical">The <c>_NET_WM_STATE_MAXIMIZED_VERT</c> atom.</param>
	/// <returns>Minimized, Maximized or Restored.</returns>
	internal static OverlappedPresenterState ToPresenterState(IntPtr[] atoms, IntPtr hidden, IntPtr maximizedHorizontal, IntPtr maximizedVertical)
	{
		if (Array.IndexOf(atoms, hidden) >= 0)
		{
			return OverlappedPresenterState.Minimized;
		}

		// Either axis counts, as before (a window maximized in one direction only is still reported Maximized).
		return Array.IndexOf(atoms, maximizedHorizontal) >= 0 || Array.IndexOf(atoms, maximizedVertical) >= 0
			? OverlappedPresenterState.Maximized
			: OverlappedPresenterState.Restored;
	}

	/// <summary>
	/// Decides what <see cref="X11NativeOverlappedPresenter.Restore"/> must ask of the window manager (WPE1-14).
	/// </summary>
	/// <param name="wasShown">Whether the window has been shown; before that, only the old raise happens.</param>
	/// <param name="mapState">The window's <c>map_state</c>.</param>
	/// <param name="hidden">Whether <c>_NET_WM_STATE</c> holds <c>_NET_WM_STATE_HIDDEN</c>.</param>
	/// <param name="maximized">Whether <c>_NET_WM_STATE</c> holds either maximized atom.</param>
	/// <param name="activateWindow">The caller's activateWindow argument.</param>
	/// <returns>The actions to take.</returns>
	internal static X11RestoreActions PlanRestore(bool wasShown, MapState mapState, bool hidden, bool maximized, bool activateWindow)
	{
		var actions = X11RestoreActions.None;

		if (!wasShown)
		{
			// SetNative runs Restore(false) on every launch before the first map: nothing to leave, and mapping the
			// window here would show it before the application does. Kept exactly as before (a harmless raise).
			return activateWindow || hidden || mapState == MapState.IsUnmapped ? X11RestoreActions.Activate : actions;
		}

		if (maximized)
		{
			actions |= X11RestoreActions.Unmaximize;
		}

		if (hidden || mapState == MapState.IsUnmapped)
		{
			actions |= X11RestoreActions.Deiconify | X11RestoreActions.Activate;
		}
		else if (activateWindow)
		{
			actions |= X11RestoreActions.Activate;
		}

		return actions;
	}
}

/// <summary>
/// What <see cref="X11NativeOverlappedPresenter.Restore"/> asks of the window manager (WPE1-14).
/// </summary>
[Flags]
internal enum X11RestoreActions
{
	/// <summary>Nothing to do.</summary>
	None = 0,

	/// <summary>Remove the maximized atoms from <c>_NET_WM_STATE</c>.</summary>
	Unmaximize = 1,

	/// <summary>Map the iconified window again and ask for it with <c>_NET_ACTIVE_WINDOW</c>.</summary>
	Deiconify = 2,

	/// <summary>Raise the window (the wrapper's Activate).</summary>
	Activate = 4,
}
