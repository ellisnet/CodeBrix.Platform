using System.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;

namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS; //Was previously: Uno.UI.Runtime.Skia.MacOS

internal class MacOSWindowWrapper : NativeWindowWrapperBase
{
	private readonly MacOSWindowNative _nativeWindow;

	public MacOSWindowWrapper(MacOSWindowNative nativeWindow, Window window, XamlRoot xamlRoot, Size initialSize) : base(window, xamlRoot)
	{
		_nativeWindow = nativeWindow;

		nativeWindow.Host.Closing += OnWindowClosing;
		nativeWindow.Host.RasterizationScaleChanged += Host_RasterizationScaleChanged;
		nativeWindow.Host.SizeChanged += (_, s) => OnHostSizeChanged(s);
		OnHostSizeChanged(initialSize);
		nativeWindow.Host.PositionChanged += (_, s) => OnHostPositionChanged(s.X, s.Y);
		// the initial event occurred before the managed side was ready to handle it
		NativeCodeBrix.codebrix_window_get_position(nativeWindow.Handle, out var x, out var y);
		OnHostPositionChanged(x, y);

		RasterizationScale = (float)_nativeWindow.Host.RasterizationScale;
	}

	private void Host_RasterizationScaleChanged(object? sender, EventArgs args)
	{
		RasterizationScale = (float)_nativeWindow.Host.RasterizationScale;
	}

	public override object NativeWindow => _nativeWindow;

	public override string Title
	{
		get => NativeCodeBrix.codebrix_window_get_title(_nativeWindow.Handle);
		set => NativeCodeBrix.codebrix_window_set_title(_nativeWindow.Handle, value);
	}

	internal protected override void Activate()
	{
		NativeCodeBrix.codebrix_window_activate(_nativeWindow.Handle);
	}

	protected override void CloseCore()
	{
		NativeCodeBrix.codebrix_window_close(_nativeWindow.Handle);
	}

	public override void Move(PointInt32 position)
	{
		// user input in physical pixels transformed into logical pixels
		var x = position.X / RasterizationScale;
		var y = position.Y / RasterizationScale;
		NativeCodeBrix.codebrix_window_move(_nativeWindow.Handle, x, y);
	}

	public override void Resize(SizeInt32 size)
	{
		// user input in physical pixels transformed into logical pixels
		var w = size.Width / RasterizationScale;
		var h = size.Height / RasterizationScale;
		NativeCodeBrix.codebrix_window_resize(_nativeWindow.Handle, w, h);
	}

	private void OnHostPositionChanged(double x, double y)
	{
		// in physical pixels
		var sx = (int)(x * RasterizationScale);
		var sy = (int)(y * RasterizationScale);
		Position = new PointInt32(sx, sy);
	}

	private void OnHostSizeChanged(Size size)
	{
		var bounds = new Rect(default, size);
		// in logical pixels
		SetBoundsAndVisibleBounds(bounds, bounds);
		// in physical pixels
		int w = (int)(size.Width * RasterizationScale);
		int h = (int)(size.Height * RasterizationScale);
		var fullSize = new SizeInt32(w, h);
		SetSizes(fullSize, fullSize);
	}

	private void OnWindowClosing(object? sender, CancelEventArgs e)
	{
		var closingArgs = RaiseClosing();
		e.Cancel = closingArgs.Cancel;
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		NativeCodeBrix.codebrix_window_enter_full_screen(_nativeWindow.Handle);
		return Disposable.Create(() => NativeCodeBrix.codebrix_window_exit_full_screen(_nativeWindow.Handle));
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new MacOSNativeOverlappedPresenter(_nativeWindow));
		return Disposable.Create(() => presenter.SetNative(null));
	}
}
