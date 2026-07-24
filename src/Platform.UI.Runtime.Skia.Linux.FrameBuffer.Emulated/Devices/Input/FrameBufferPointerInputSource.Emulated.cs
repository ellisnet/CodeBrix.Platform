using System;
using System.Diagnostics;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using static Windows.UI.Input.PointerUpdateKind;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

internal partial class FrameBufferPointerInputSource
{
	/// <summary>
	/// Injects one emulated touch, called on the transport's input thread with
	/// DEVICE pixel coordinates. The single-touch model maps press / move /
	/// release onto the same pointer shapes the real FrameBuffer head produces
	/// for libinput touch events; a finger that is not touching the screen
	/// produces nothing at all, so there is no hover and no move outside a
	/// press/release pair. The pointer id is honored as sent (the V1 IDE only
	/// ever sends 0), so a future multi-finger sender needs no head change.
	/// </summary>
	public void ProcessEmulatedTouch(uint messageType, int pointerId, int deviceX, int deviceY)
	{
		// Device pixels -> logical (view) coordinates, the space pointer
		// events are raised in. Bounds is the raw size divided by the
		// rasterization scale, so this stays correct under
		// CODEBRIX_DISPLAY_SCALE_OVERRIDE.
		var bounds = FrameBufferWindowWrapper.Instance.Bounds;
		var size = FrameBufferWindowWrapper.Instance.Size;
		var position = new Point(
			deviceX * bounds.Width / Math.Max(1, size.Width),
			deviceY * bounds.Height / Math.Max(1, size.Height));

		var properties = new PointerPointProperties();
		Action<PointerEventArgs> raisePointerEvent;
		switch (messageType)
		{
			case FrameBufferEmulatorProtocol.TouchPressMessage:
				properties.PointerUpdateKind = LeftButtonPressed;
				properties.IsLeftButtonPressed = true;
				raisePointerEvent = RaisePointerPressed;
				break;

			case FrameBufferEmulatorProtocol.TouchMoveMessage:
				properties.IsLeftButtonPressed = true;
				raisePointerEvent = RaisePointerMoved;
				break;

			case FrameBufferEmulatorProtocol.TouchReleaseMessage:
				properties.PointerUpdateKind = LeftButtonReleased;
				properties.IsLeftButtonPressed = false;
				raisePointerEvent = RaisePointerReleased;
				break;

			default:
				this.Log().LogWarning($"Emulated touch message type {messageType} was not handled");
				return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"ProcessEmulatedTouch: {messageType}, device:({deviceX},{deviceY}), position:{position}");
		}

		var timestampInMicroseconds =
			(ulong) (Stopwatch.GetTimestamp() * 1_000_000.0 / Stopwatch.Frequency);
		var pointerPoint = new Windows.UI.Input.PointerPoint(
			frameId: (uint) timestampInMicroseconds,
			timestamp: timestampInMicroseconds,
			device: PointerDevice.For(PointerDeviceType.Touch),
			pointerId: (uint) pointerId,
			rawPosition: position,
			position: position,
			isInContact: properties.HasPressedButton,
			properties: properties
		);

		RaisePointerEvent(raisePointerEvent, new PointerEventArgs(pointerPoint, GetCurrentModifiersState()));
	}
}
