using System;
using System.Diagnostics;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.Graphics.Display;
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
		// Frame-buffer pixels -> logical (view) coordinates, the space pointer
		// events are raised in, undoing the mounted orientation exactly as the
		// real head's GetOrientationAdjustedAbsolutionPosition does for
		// libinput. The incoming coordinates are always in the BUFFER's space,
		// which never changes shape; Size is the application's space and is
		// transposed for the portrait mounts, so transpose it back to
		// normalize against. Bounds is the application's space divided by the
		// rasterization scale, so this stays correct under
		// CODEBRIX_DISPLAY_SCALE_OVERRIDE.
		var orientation = FrameBufferWindowWrapper.Instance.Orientation;
		var bounds = FrameBufferWindowWrapper.Instance.Bounds;
		var size = FrameBufferWindowWrapper.Instance.Size;
		var isPortrait = orientation
			is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped;
		var normalizedX = deviceX / (double) Math.Max(1, isPortrait ? size.Height : size.Width);
		var normalizedY = deviceY / (double) Math.Max(1, isPortrait ? size.Width : size.Height);
		var position = orientation switch
		{
			DisplayOrientations.Portrait => new Point(
				normalizedY * bounds.Width,
				(1 - normalizedX) * bounds.Height),
			DisplayOrientations.LandscapeFlipped => new Point(
				(1 - normalizedX) * bounds.Width,
				(1 - normalizedY) * bounds.Height),
			DisplayOrientations.PortraitFlipped => new Point(
				(1 - normalizedY) * bounds.Width,
				normalizedX * bounds.Height),
			_ => new Point(
				normalizedX * bounds.Width,
				normalizedY * bounds.Height),
		};

		var properties = new PointerPointProperties();
		Action<PointerEventArgs> raisePointerEvent;

		// Entered / Pressed ... Released / Exited - the same full finger sequence
		// the real head raises from libinput (and the X11 head from XI_TouchBegin /
		// XI_TouchEnd), so what the emulator shows is what a touch panel does. The
		// trailing Exited is what releases touch pointer captures: the managed
		// input manager leaves them alone on pointer-up by design and has no
		// auto-release fallback under managed pointers, so omitting it leaks every
		// explicit CapturePointer and re-routes later releases to a stale target.
		// Each pair is dispatched as ONE action so nothing interleaves between the
		// two raises.
		switch (messageType)
		{
			case FrameBufferEmulatorProtocol.TouchPressMessage:
				properties.PointerUpdateKind = LeftButtonPressed;
				properties.IsLeftButtonPressed = true;
				raisePointerEvent = touchArgs =>
				{
					SoftwareKeyboard.ActivePointerTracker.OnPointerDown((uint) pointerId);
					RaisePointerEntered(touchArgs);
					RaisePointerPressed(touchArgs);
				};
				break;

			case FrameBufferEmulatorProtocol.TouchMoveMessage:
				properties.IsLeftButtonPressed = true;
				raisePointerEvent = RaisePointerMoved;
				break;

			case FrameBufferEmulatorProtocol.TouchReleaseMessage:
				properties.PointerUpdateKind = LeftButtonReleased;
				properties.IsLeftButtonPressed = false;
				raisePointerEvent = touchArgs =>
				{
					RaisePointerReleased(touchArgs);
					RaisePointerExited(touchArgs);
					SoftwareKeyboard.ActivePointerTracker.OnPointerUp((uint) pointerId);
				};
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
