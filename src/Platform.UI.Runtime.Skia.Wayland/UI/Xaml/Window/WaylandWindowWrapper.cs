using System;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Xaml.Controls;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

internal class WaylandWindowWrapper : NativeWindowWrapperBase
{
	private readonly WaylandXamlRootHost _host;
	private readonly XamlRoot _xamlRoot;
	private string _title = string.Empty;

	internal WaylandWindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_xamlRoot = xamlRoot;

		_host = new WaylandXamlRootHost(this, window, xamlRoot, UpdatePositionAndSize, OnWindowClosing, OnNativeActivated, OnNativeVisibilityChanged);

		// The scale must be known before the first size derivation: physical size is
		// computed FROM the logical configure size, unlike X11 where physical is native.
		RasterizationScale = (float)XamlRoot.GetDisplayInformation(_xamlRoot).RawPixelsPerViewPixel;
		UpdatePositionAndSize(); // set initial values
	}

	internal WaylandXamlRootHost Host => _host;

	public override string Title
	{
		get => _title;
		set
		{
			_title = value ?? string.Empty;
			_host.SetTitle(_title);
		}
	}

	public override object NativeWindow => new WaylandNativeWindow(_host);

	internal protected override void Activate()
	{
		// Client-initiated activation rides xdg-activation-v1: request a token (tied to our
		// last input serial and surface), then activate the surface with it once the
		// compositor delivers it. Compositor policy still has the last word — without a
		// recent interaction it may only flag "demands attention" instead of focusing.
		if (_host.Connection is not { Activation: { } activation } connection
			|| _host.ShellSurface?.Surface is not { } surface
			|| _host.IsClosed)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug("Window self-activation is unavailable (no xdg-activation-v1 on this compositor).");
			}
			return;
		}

		var token = activation.GetActivationToken(new Protocols.XdgActivationV1.XdgActivationTokenV1.Listener.Relay
		{
			OnDone = (tokenObject, tokenValue) =>
			{
				// Event-pump thread: hand the token back to activate our surface.
				if (!_host.IsClosed)
				{
					activation.Activate(tokenValue, surface);
					connection.Flush();
				}
				tokenObject.Destroy();
			},
		});

		if (connection.SeatManager.Seat is { } seat)
		{
			token.SetSerial(connection.SeatManager.LastInputSerial, seat);
		}
		token.SetSurface(surface);
		token.Commit();
		connection.Flush();
	}

	public override void ExtendContentIntoTitleBar(bool extend)
	{
		base.ExtendContentIntoTitleBar(extend);

		// Same shape as the X11 head (motif hints toggle): hide the native decorations so
		// the XAML content (and any custom title bar) owns the full window surface.
		_host.ShellSurface?.SetDecorationsVisible(!extend);
	}

	protected override void CloseCore()
	{
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info("Closing Wayland window");
		}
		WaylandXamlRootHost.Close(_host);
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

	protected override void ShowCore() => _host.Show();

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		var native = new WaylandNativeOverlappedPresenter(_host);
		presenter.SetNative(native);
		_host.WindowStateChanged += native.OnNativeWindowStateChanged;
		return Disposable.Create(() =>
		{
			_host.WindowStateChanged -= native.OnNativeWindowStateChanged;
			presenter.SetNative(null);
		});
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
		// No client-side window positioning on Wayland; the compositor owns placement.
		WaylandNotSupported.WarnOnce(typeof(WaylandWindowWrapper),
			"AppWindow.Move / window positioning",
			"the compositor owns window placement; clients cannot set (or read back — AppWindow.Position always reports 0,0) global window coordinates.");
	}

	public override void Resize(SizeInt32 size)
	{
		// A Wayland client cannot force its outer size; the compositor has the last word.
		WaylandNotSupported.WarnOnce(typeof(WaylandWindowWrapper),
			"AppWindow.Resize",
			"a client cannot force its outer window size; the compositor has the last word.");
	}

	private void UpdatePositionAndSize()
	{
		// Wayland never exposes global window coordinates; (0,0) is the WinUI convention
		// for "unknown" here.
		Position = new PointInt32 { X = 0, Y = 0 };

		// The configure size is LOGICAL (unlike X11, whose native window size is physical
		// pixels): Bounds take it as-is, and the physical size reported through
		// AppWindow.Size is derived by multiplying the scale back on.
		var logicalSize = _host.CurrentSize;
		var scale = RasterizationScale;
		var physicalSize = new SizeInt32
		{
			Width = (int)Math.Round(logicalSize.Width * scale),
			Height = (int)Math.Round(logicalSize.Height * scale),
		};
		SetSizes(physicalSize, physicalSize);

		var bounds = new Rect(default, new Size(logicalSize.Width, logicalSize.Height));
		SetBoundsAndVisibleBounds(bounds, bounds);
	}

	internal void SetFullScreenMode(bool on)
	{
		if (_host.ShellSurface is { } shellSurface && !_host.IsClosed)
		{
			shellSurface.SetFullscreen(on);
		}
	}
}

/// <summary>
/// The object returned from <see cref="Microsoft.UI.Xaml.Window.GetNativeWindow"/>-style
/// APIs on this head. Exposes no raw handles yet; native element hosting on Wayland is a
/// later effort (subsurfaces).
/// </summary>
public sealed class WaylandNativeWindow
{
	private readonly object _host;

	internal WaylandNativeWindow(object host)
	{
		_host = host;
	}
}
