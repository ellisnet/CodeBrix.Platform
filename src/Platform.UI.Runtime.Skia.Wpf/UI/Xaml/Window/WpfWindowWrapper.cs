#nullable enable

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.UI.Core;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf.UI.Controls; //Was previously: Uno.UI.Runtime.Skia.Wpf.UI.Controls

internal class WpfWindowWrapper : NativeWindowWrapperBase
{
	private readonly CodeBrixWpfWindow _wpfWindow;
	private bool _isFullScreen;
	private bool _wasShown;

	// Set by ResizeClient() when the window's chrome could not be measured yet, and consumed once by the host's
	// next size change - see ApplyPendingClientSize.
	private SizeInt32? _pendingClientSize;

	public WpfWindowWrapper(CodeBrixWpfWindow wpfWindow, WinUIWindow window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_wpfWindow = wpfWindow ?? throw new ArgumentNullException(nameof(wpfWindow));
		_wpfWindow.Activated += OnNativeActivated;
		_wpfWindow.Deactivated += OnNativeDeactivated;
		_wpfWindow.IsVisibleChanged += OnNativeIsVisibleChanged;
		_wpfWindow.Closing += OnNativeClosing;
		_wpfWindow.DpiChanged += OnNativeDpiChanged;
		_wpfWindow.StateChanged += OnNativeStateChanged;
		_wpfWindow.Host.SizeChanged += (_, e) => OnHostSizeChanged(e.NewSize);
		_wpfWindow.LocationChanged += OnNativeLocationChanged;
		_wpfWindow.SizeChanged += OnNativeSizeChanged;

		RasterizationScale = (float)VisualTreeHelper.GetDpi(_wpfWindow.Host).DpiScaleX;

		OnHostSizeChanged(new Size(_wpfWindow.Width, _wpfWindow.Height));
		UpdateSizeFromNative();
		UpdatePositionFromNative();
	}

	private void OnNativeSizeChanged(object sender, System.Windows.SizeChangedEventArgs e) => UpdateSizeFromNative();

	private void UpdateSizeFromNative()
	{
		if (!_wasShown)
		{
			var size = new SizeInt32() { Width = (int)(_wpfWindow.Width * RasterizationScale), Height = (int)(_wpfWindow.Height * RasterizationScale) };
			SetSizes(size, size);
		}
		else
		{
			var size = new SizeInt32() { Width = (int)(_wpfWindow.ActualWidth * RasterizationScale), Height = (int)(_wpfWindow.ActualHeight * RasterizationScale) };
			SetSizes(size, size);
		}
	}

	private void OnNativeLocationChanged(object? sender, EventArgs e) => UpdatePositionFromNative();

	private void UpdatePositionFromNative() =>
		Position = new() { X = (int)(_wpfWindow.Left * RasterizationScale), Y = (int)(_wpfWindow.Top * RasterizationScale) };

	private void OnNativeStateChanged(object? sender, EventArgs e) => UpdateIsVisible();

	private void OnNativeDpiChanged(object sender, DpiChangedEventArgs e) => RasterizationScale = (float)e.NewDpi.DpiScaleX;

	public override string Title
	{
		get => _wpfWindow.Title;
		set => _wpfWindow.Title = value;
	}

	public override object NativeWindow => _wpfWindow;

	protected override void ShowCore()
	{
		_wpfWindow.Show();
		_wasShown = true;
		UpdatePositionFromNative();
	}

	internal protected override void Activate() => _wpfWindow.Activate();

	protected override void CloseCore() => _wpfWindow.Close();

	public override void ExtendContentIntoTitleBar(bool extend)
	{
		base.ExtendContentIntoTitleBar(extend);
		_wpfWindow.ExtendContentIntoTitleBar(extend);
	}

	private void OnHostSizeChanged(Size size)
	{
		var bounds = new Windows.Foundation.Rect(default, new Windows.Foundation.Size(size.Width, size.Height));
		SetBoundsAndVisibleBounds(bounds, bounds);

		ApplyPendingClientSize(size);
	}

	private void OnNativeClosing(object? sender, CancelEventArgs e)
	{
		var closingArgs = RaiseClosing();
		if (closingArgs.Cancel)
		{
			e.Cancel = true;
			return;
		}

		// Closing should continue, perform suspension.
		WinUIApplication.Current.RaiseSuspending();
	}

	private void OnNativeDeactivated(object? sender, EventArgs e) =>
		ActivationState = CoreWindowActivationState.Deactivated;

	private void OnNativeActivated(object? sender, EventArgs e) =>
		ActivationState = CoreWindowActivationState.PointerActivated;

	private void OnNativeIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) => UpdateIsVisible();

	private void UpdateIsVisible()
	{
		var isVisible = _wpfWindow.IsVisible && _wpfWindow.WindowState != WindowState.Minimized;
		if (isVisible == IsVisible)
		{
			return;
		}

		if (isVisible)
		{
			WinUIApplication.Current?.RaiseLeavingBackground(() => IsVisible = isVisible);
		}
		else
		{
			IsVisible = isVisible;
			WinUIApplication.Current?.RaiseEnteredBackground(null);
		}
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		_isFullScreen = true;
		_wpfWindow.WindowStyle = WindowStyle.None;
		_wpfWindow.WindowState = WindowState.Maximized;

		return Disposable.Create(() =>
		{
			if (!_isFullScreen)
			{
				return;
			}

			_isFullScreen = false;
			_wpfWindow.WindowStyle = WindowStyle.SingleBorderWindow;
			_wpfWindow.WindowState = WindowState.Normal;
		});
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new NativeOverlappedPresenter(_wpfWindow));
		return Disposable.Create(() => presenter.SetNative(null));
	}

	public override void Move(PointInt32 position)
	{
		_wpfWindow.Left = position.X / RasterizationScale;
		_wpfWindow.Top = position.Y / RasterizationScale;

		if (!_wasShown)
		{
			// Set Position and trigger AppWindow.Changed
			UpdatePositionFromNative();
		}
	}

	public override void Resize(SizeInt32 size)
	{
		_wpfWindow.Width = size.Width / RasterizationScale;
		_wpfWindow.Height = size.Height / RasterizationScale;

		if (!_wasShown)
		{
			// Set size and trigger AppWindow.Changed
			UpdateSizeFromNative();
		}
	}

	/// <summary>
	/// Sets the client area - what <c>Window.Bounds</c> reports, the size of the window's content host - to
	/// <paramref name="size"/>, which is in EFFECTIVE pixels. A WPF window's Width and Height are device-independent
	/// units, the same unit, but they include the window chrome; the chrome is the difference between the WPF
	/// window's size and its content host's size, and is added before the window is sized.
	/// </summary>
	/// <remarks>
	/// Before the window has been laid out the chrome cannot be measured, so the client size is applied as the
	/// window size and remembered; the first host size change after that, when the chrome is known, corrects it
	/// once (a window that refuses the size cannot start a resize loop).
	/// </remarks>
	/// <param name="size">The client size in effective pixels.</param>
	public override void ResizeClient(SizeInt32 size)
	{
		var clientSize = new SizeInt32(Math.Max(1, size.Width), Math.Max(1, size.Height));

		if (TryGetChromeSize(out var chromeWidth, out var chromeHeight))
		{
			_pendingClientSize = null;
		}
		else
		{
			_pendingClientSize = clientSize;
		}

		_wpfWindow.Width = clientSize.Width + chromeWidth;
		_wpfWindow.Height = clientSize.Height + chromeHeight;

		if (!_wasShown)
		{
			// Set size and trigger AppWindow.Changed
			UpdateSizeFromNative();
		}
	}

	private bool TryGetChromeSize(out double chromeWidth, out double chromeHeight)
	{
		var host = _wpfWindow.Host;
		if (_wpfWindow.ActualWidth > 0 && _wpfWindow.ActualHeight > 0 && host.ActualWidth > 0 && host.ActualHeight > 0)
		{
			chromeWidth = Math.Max(0, _wpfWindow.ActualWidth - host.ActualWidth);
			chromeHeight = Math.Max(0, _wpfWindow.ActualHeight - host.ActualHeight);
			return true;
		}

		chromeWidth = 0;
		chromeHeight = 0;
		return false;
	}

	private void ApplyPendingClientSize(Size hostSize)
	{
		if (_pendingClientSize is not { } pending || !TryGetChromeSize(out var chromeWidth, out var chromeHeight))
		{
			return;
		}

		_pendingClientSize = null;

		if (hostSize.Width == pending.Width && hostSize.Height == pending.Height)
		{
			return;
		}

		_wpfWindow.Width = pending.Width + chromeWidth;
		_wpfWindow.Height = pending.Height + chromeHeight;
	}
}
