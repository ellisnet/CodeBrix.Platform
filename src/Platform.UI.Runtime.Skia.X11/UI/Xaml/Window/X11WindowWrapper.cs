using System;
using System.Globalization;
using System.Threading;
using CodeBrix.Platform.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Dispatching;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.NativeElementHosting;
using CodeBrix.Platform.UI.Runtime.Skia;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.X11; //Was previously: Uno.WinUI.Runtime.Skia.X11

internal class X11WindowWrapper : NativeWindowWrapperBase
{
	private readonly X11XamlRootHost _host;
	private readonly XamlRoot _xamlRoot;

	// Set by Resize() and consumed once by the configure callback - see ApplyPendingFramedSize.
	private SizeInt32? _pendingFramedSize;

	internal X11WindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_xamlRoot = xamlRoot;

		_host = new X11XamlRootHost(this, window, xamlRoot, UpdatePositionAndSize, OnWindowClosing, OnNativeActivated, OnNativeVisibilityChanged);
		UpdatePositionAndSize(); // set initial values

		RasterizationScale = (float)XamlRoot.GetDisplayInformation(_xamlRoot).RawPixelsPerViewPixel;
	}

	public override string Title
	{
		get
		{
			using var lockDiposable = X11Helper.XLock(_host.RootX11Window.Display);
			var @out = string.Empty;
			_ = XLib.XFetchName(_host.RootX11Window.Display, _host.RootX11Window.Window, ref @out);
			return @out;
		}
		set
		{
			using var lockDiposable = X11Helper.XLock(_host.RootX11Window.Display);
			_ = XLib.XStoreName(_host.RootX11Window.Display, _host.RootX11Window.Window, value);
			// Important! Same as Activate() below: without a flush the XStoreName request sits
			// in the output buffer until unrelated X traffic flushes it, so a background app's
			// title visibly updates only when the user next interacts with the window.
			_ = XLib.XFlush(_host.RootX11Window.Display);
		}
	}

	public override object NativeWindow => new X11NativeWindow(_host.RootX11Window.Window);

	internal protected override void Activate()
	{
		var x11Window = _host.RootX11Window;
		using var lockDiposable = X11Helper.XLock(x11Window.Display);
		_ = XLib.XRaiseWindow(x11Window.Display, x11Window.Window);
		_ = XLib.XFlush(x11Window.Display); // Important! Otherwise X commands will sit waiting to be flushed, and since the window is not activated, there are no new X commands being sent to force a flush.

		// We could send _NET_ACTIVE_WINDOW as well, although it doesn't seem to be needed (and only works with EWMH-compliant WMs)
		// XClientMessageEvent xclient = default;
		// xclient.send_event = 1;
		// xclient.type = XEventName.ClientMessage;
		// xclient.window = x11Window.Window;
		// xclient.message_type = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_ACTIVE_WINDOW);
		// xclient.format = 32;
		// xclient.ptr1 = 1;
		// xclient.ptr2 = X11Helper.CurrentTime;
		//
		// XEvent xev = default;
		// xev.ClientMessageEvent = xclient;
		// _ = XLib.XSendEvent(x11Window.Display, XLib.XDefaultRootWindow(x11Window.Display), false, (IntPtr)(XEventMask.SubstructureRedirectMask | XEventMask.SubstructureNotifyMask), ref xev);
		// _ = XLib.XFlush(x11Window.Display);
	}

	protected override void CloseCore()
	{
		var x11Window = _host.RootX11Window;
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Forcibly closing X11 window {x11Window.Display.ToString("X", CultureInfo.InvariantCulture)}, {x11Window.Window.ToString("X", CultureInfo.InvariantCulture)}");
		}
		using (X11Helper.XLock(x11Window.Display))
		{
			X11XamlRootHost.Close(x11Window);
		}
	}

	public override void ExtendContentIntoTitleBar(bool extend)
	{
		base.ExtendContentIntoTitleBar(extend);
		_host.ExtendContentIntoTitleBar(extend);
	}

	private void OnWindowClosing()
	{
		var closingArgs = RaiseClosing();
		if (closingArgs.Cancel)
		{
			return;
		}

		// All prerequisites passed, can safely close.
		Close();
	}

	private void OnNativeActivated(bool focused) => ActivationState = focused ? CoreWindowActivationState.PointerActivated : CoreWindowActivationState.Deactivated;

	private void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	protected override void ShowCore()
	{
		using var lockDiposable = X11Helper.XLock(_host.RootX11Window.Display);
		using var lockDiposable2 = X11Helper.XLock(_host.TopX11Window.Display);
		_ = XLib.XMapWindow(_host.RootX11Window.Display, _host.RootX11Window.Window);
		_ = XLib.XMapWindow(_host.TopX11Window.Display, _host.TopX11Window.Window);
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new X11NativeOverlappedPresenter(_host.RootX11Window, this));
		return Disposable.Create(() => presenter.SetNative(null));
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		if (WasShown)
		{
			SetFullScreenMode(true);
		}

		return Disposable.Create(() =>
		{
			if (WasShown)
			{
				SetFullScreenMode(false);
			}
		});
	}

	public override void Move(PointInt32 position)
	{
		var display = _host.RootX11Window.Display;
		var window = _host.RootX11Window.Window;
		using var lockDiposable = X11Helper.XLock(display);

		_ = X11Helper.XMoveWindow(display, window, position.X, position.Y);
		XLib.XSync(display, false);
	}

	/// <summary>
	/// Resizes the window to <paramref name="size"/>, which is the FRAMED size - the size
	/// <see cref="NativeWindowWrapperBase.Size"/> reports, including whatever decorations the window
	/// manager draws around the window. <c>Resize(Size)</c> is therefore a no-op round trip, matching
	/// the Win32 head (where <c>Size</c> is the window rect and <c>Resize</c> sets the window rect).
	/// <see cref="NativeWindowWrapperBase.ClientSize"/> and <c>Window.Bounds</c> keep answering the
	/// client area, which is what the page is laid out into.
	/// </summary>
	/// <remarks>
	/// The request always goes to the application's OWN window, never to the window manager's frame:
	/// a reparenting window manager owns the frame and ignores configure requests aimed at it (a
	/// direct XResizeWindow of the frame is silently dropped by Muffin/Mutter, for instance), while a
	/// configure request on the client window is redirected to the window manager, which resizes its
	/// frame to fit. The frame extents are the difference between the two sizes last read by
	/// <see cref="UpdatePositionAndSize"/>; before the window manager has framed the window they are
	/// not knowable, so the request is remembered and corrected once, from the configure callback,
	/// as soon as the frame exists.
	/// </remarks>
	public override void Resize(SizeInt32 size)
	{
		var display = _host.RootX11Window.Display;
		var window = _host.RootX11Window.Window;
		using var lockDiposable = X11Helper.XLock(display);

		_pendingFramedSize = size;

		var clientSize = ToClientSize(size);
		_ = XLib.XResizeWindow(display, window, clientSize.Width, clientSize.Height);
		XLib.XSync(display, false);
	}

	private void UpdatePositionAndSize()
	{
		var display = _host.RootX11Window.Display;
		var window = _host.RootX11Window.Window;
		using var xLock = X11Helper.XLock(display);

		// If the window manager adds decorations, usually that is implemented by wrapping
		// the window in another slightly bigger window that includes the decorations. In that case,
		// XGetWindowAttributes will give us x and y offsets relative to this slightly bigger window,
		// not relative to the root window.
		_ = XLib.XQueryTree(display, window, out var root, out var parent, out var children, out _);
		_ = XLib.XQueryTree(display, parent, out _, out var parentParent, out var children2, out _);
		_ = XLib.XFree(children);
		_ = XLib.XFree(children2);

		var windowToRead = parentParent == root ? parent : window;
		XWindowAttributes windowAttrs = default;
		_ = XLib.XGetWindowAttributes(display, windowToRead, ref windowAttrs);
		_ = XLib.XTranslateCoordinates(display, windowToRead, root, 0, 0, out var rootx, out var rooty, out _);

		XWindowAttributes windowAttrs2 = default;
		_ = XLib.XGetWindowAttributes(display, window, ref windowAttrs2);

		Position = new PointInt32 { X = rootx, Y = rooty };
		// Size is the framed size (windowToRead is the window manager's frame once it has reparented
		// us) and ClientSize is the application's own window - the two differ by the decorations.
		var fullSize = new SizeInt32 { Width = windowAttrs.width, Height = windowAttrs.height };
		var clientSize = new SizeInt32 { Width = windowAttrs2.width, Height = windowAttrs2.height };
		SetSizes(fullSize, clientSize);

		var scale = _xamlRoot.RasterizationScale;
		var newWindowSize = new Size(windowAttrs2.width / scale, windowAttrs2.height / scale);
		var bounds = new Rect(default, newWindowSize);
		SetBoundsAndVisibleBounds(bounds, bounds);

		// copy the root window dimensions to the top window
		_ = XLib.XResizeWindow(display, _host.TopX11Window.Window, windowAttrs2.width, windowAttrs2.height);

		ApplyPendingFramedSize(display, window, fullSize, clientSize);

		// Every configure - a size increase and a size decrease alike - has to end in a repaint at the
		// new size. The layout pass that SetBoundsAndVisibleBounds triggers normally asks for one, but
		// only when something in the tree actually re-arranges; asking here as well makes the renderer
		// resize its surface and redraw the last frame unconditionally, so a window that gets smaller
		// can never be left painted at the previous size until the next input event arrives.
		((IXamlRootHost)_host).InvalidateRender();
	}

	/// <summary>
	/// Re-applies the size given to <see cref="Resize"/> once, if that call happened before the window
	/// manager had framed the window and so had to assume no decorations. Exactly one correction is
	/// made per <see cref="Resize"/> call, so a window manager that refuses the size cannot start a
	/// resize loop.
	/// </summary>
	private void ApplyPendingFramedSize(IntPtr display, IntPtr window, SizeInt32 fullSize, SizeInt32 clientSize)
	{
		if (_pendingFramedSize is not { } pending)
		{
			return;
		}

		if (!HasNonClientFrame(fullSize, clientSize))
		{
			// Either the window manager has not framed the window yet - keep the request until it
			// has - or there are no decorations at all, in which case Resize already applied the
			// framed size and the correction below would be a no-op anyway.
			return;
		}

		_pendingFramedSize = null;

		if (fullSize.Width == pending.Width && fullSize.Height == pending.Height)
		{
			return;
		}

		var correctedClientSize = ToClientSize(pending, fullSize, clientSize);
		_ = XLib.XResizeWindow(display, window, correctedClientSize.Width, correctedClientSize.Height);
		XLib.XSync(display, false);
	}

	internal void SetFullScreenMode(bool on)
	{
		if (WasShown)
		{
			X11Helper.SetWMHints(
				_host.RootX11Window,
				X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper._NET_WM_STATE),
				on ? 1 : 0,
				X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper._NET_WM_STATE_FULLSCREEN));
			_ = XLib.XSync(_host.RootX11Window.Display, false);
		}
	}
}
