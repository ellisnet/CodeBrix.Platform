using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using CodeBrix.Platform.Extensions;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

/// <summary>
/// The emulated head's pointer source. Only touch is ever raised — a single
/// emulated finger, injected by the transport (see the Emulated partial). The
/// emulated device has no mouse, so there is no relative-pointer support, no
/// wheel, and no cursor.
/// </summary>
internal partial class FrameBufferPointerInputSource : ICodeBrixCorePointerInputSource
{
#pragma warning disable CS0067 // Only pressed/moved/released are raised by the single-touch model.
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

	private FrameBufferPointerInputSource()
	{
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

	private void RaisePointerMoved(PointerEventArgs args)
		=> PointerMoved?.Invoke(this, args);

	private void RaisePointerPressed(PointerEventArgs args)
		=> PointerPressed?.Invoke(this, args);

	private void RaisePointerReleased(PointerEventArgs args)
		=> PointerReleased?.Invoke(this, args);

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

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Skia for FrameBuffer.Emulated.");
		}
	}
}
