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
		UpdatePositionAndSize(); // set initial values

		RasterizationScale = (float)XamlRoot.GetDisplayInformation(_xamlRoot).RawPixelsPerViewPixel;
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
		// Wayland has no client-initiated raise/activate (that would need the
		// xdg-activation protocol plus a valid activation token). No-op by design.
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Window self-activation is not available on Wayland.");
		}
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
		presenter.SetNative(new WaylandNativeOverlappedPresenter(_host));
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
		// No client-side window positioning on Wayland; the compositor owns placement.
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Window positioning is not available on Wayland.");
		}
	}

	public override void Resize(SizeInt32 size)
	{
		// A Wayland client cannot force its outer size; the compositor has the last word.
		// The next commits simply use the requested buffer size, which floating-window
		// compositors generally accept.
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Resize requested to {size.Width}x{size.Height}");
		}
	}

	private void UpdatePositionAndSize()
	{
		// Wayland never exposes global window coordinates; (0,0) is the WinUI convention
		// for "unknown" here.
		Position = new PointInt32 { X = 0, Y = 0 };

		var size = _host.CurrentSize;
		SetSizes(size, size);

		var scale = _xamlRoot.RasterizationScale;
		var newWindowSize = new Size(size.Width / scale, size.Height / scale);
		var bounds = new Rect(default, newWindowSize);
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
