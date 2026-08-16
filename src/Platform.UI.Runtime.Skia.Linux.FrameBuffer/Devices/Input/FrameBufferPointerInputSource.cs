using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

internal partial class FrameBufferPointerInputSource : ICodeBrixCorePointerInputSource, ICodeBrixRelativePointerSource
{
#pragma warning disable CS0067 // PointerCaptureLost is not raised by this head (the managed input manager owns capture state).
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	private Func<VirtualKeyModifiers>? _keyboardInputSource;
	private IXamlRootHost? _host;

	// Written on the UI thread (MouseDevice subscription changes), read on the libinput thread.
	private volatile MouseDevice? _relativeMouseDevice;

	// Fractional motion remainders carried between events (libinput thread only).
	private double _relativeDxRemainder;
	private double _relativeDyRemainder;

	// libinput deltas are already relative; while a session is active, motion feeds
	// MouseDevice.MouseMoved and the on-screen position stays frozen (which also serves
	// as the pointer confinement for this head).
	public void StartRelativeMouse(MouseDevice device) => _relativeMouseDevice = device;

	public void StopRelativeMouse() => _relativeMouseDevice = null;

	private void RaiseRelativeMouseMoved(MouseDevice device, int deltaX, int deltaY)
	{
		if (_host?.RootElement is { } rootElement)
		{
			_ = rootElement.Dispatcher.RunAsync(
				CoreDispatcherPriority.High,
				() => device.RaiseMouseMoved(deltaX, deltaY));
		}
	}

	// A launcher can declare the device's touch digitizer as mounted rotated
	// 180 degrees relative to the display (the WinBook TW700 ships this way)
	// via CODEBRIX_FRAMEBUFFER_TOUCH_ROTATION=180: touch positions are then
	// flipped to land where the finger actually is. Touch only — an attached
	// mouse is not part of the rotated digitizer. 180 is the only supported
	// rotation, since it is the only one that composes with every display
	// orientation without an axis swap.
	private const string EnvironmentCodeBrixTouchRotation = "CODEBRIX_FRAMEBUFFER_TOUCH_ROTATION";
	private readonly bool _touchRotated180;

	private FrameBufferPointerInputSource()
	{
		var rotation = Environment.GetEnvironmentVariable(EnvironmentCodeBrixTouchRotation);
		if (rotation == "180")
		{
			_touchRotated180 = true;
		}
		else if (!string.IsNullOrEmpty(rotation) && rotation != "0")
		{
			this.Log().LogWarning(
				$"Ignoring {EnvironmentCodeBrixTouchRotation}={rotation}: only 180 (or 0/unset) is supported.");
		}
	}

	internal static FrameBufferPointerInputSource Instance { get; } = new FrameBufferPointerInputSource();

	internal void SetHost(IXamlRootHost host) => _host = host;

	public void Configure(Func<VirtualKeyModifiers> keyboardInputSource)
	{
		_keyboardInputSource = keyboardInputSource;
	}

	[NotImplemented] public bool HasCapture => false;

	[NotImplemented] public CoreCursor PointerCursor { get; set; } = new(CoreCursorType.Arrow, 0);

	[NotImplemented] public Point PointerPosition => default!;

	[NotImplemented] public void SetPointerCapture(PointerIdentifier pointer) => LogNotSupported();
	[NotImplemented] public void SetPointerCapture() => LogNotSupported();
	[NotImplemented] public void ReleasePointerCapture(PointerIdentifier pointer) => LogNotSupported();
	[NotImplemented] public void ReleasePointerCapture() => LogNotSupported();

	private void RaisePointerEntered(PointerEventArgs args)
		=> PointerEntered?.Invoke(this, args);

	private void RaisePointerExited(PointerEventArgs args)
		=> PointerExited?.Invoke(this, args);

	private void RaisePointerMoved(PointerEventArgs args)
		=> PointerMoved?.Invoke(this, args);

	private void RaisePointerPressed(PointerEventArgs args)
		=> PointerPressed?.Invoke(this, args);

	private void RaisePointerReleased(PointerEventArgs args)
		=> PointerReleased?.Invoke(this, args);

	private void RaisePointerCancelled(PointerEventArgs args)
		=> PointerCancelled?.Invoke(this, args);

	private void RaisePointerWheelChanged(PointerEventArgs args)
		=> PointerWheelChanged?.Invoke(this, args);

	private void RaisePointerEvent(Action<PointerEventArgs> raisePointerEvent, PointerEventArgs args)
	{
		if (_host?.RootElement is { } rootElement)
		{
			_ = rootElement.Dispatcher.RunAsync(
				CoreDispatcherPriority.High,
				() => raisePointerEvent(args));
		}
	}

	private VirtualKeyModifiers GetCurrentModifiersState()
		=> _keyboardInputSource?.Invoke() ?? VirtualKeyModifiers.None;

	private (double x, double y) GetOrientationAdjustedAbsolutionPosition(IntPtr rawEvent, Func<IntPtr, int, double> getX, Func<IntPtr, int, double> getY)
	{
		double x, y;
		switch (FrameBufferWindowWrapper.Instance.Orientation)
		{
			case DisplayOrientations.None:
			case DisplayOrientations.Landscape:
				x = getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				y = getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				break;
			case DisplayOrientations.Portrait:
				y = FrameBufferWindowWrapper.Instance.Bounds.Height - getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				x = getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				break;
			case DisplayOrientations.LandscapeFlipped:
				x = FrameBufferWindowWrapper.Instance.Bounds.Width - getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				y = FrameBufferWindowWrapper.Instance.Bounds.Height - getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				break;
			case DisplayOrientations.PortraitFlipped:
				y = getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				x = FrameBufferWindowWrapper.Instance.Bounds.Width - getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return (x, y);
	}

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Skia for FrameBuffer.");
		}
	}
}
