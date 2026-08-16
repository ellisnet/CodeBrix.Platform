// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// See the LICENSE file in the project root for more information.
//
// Base interactions with libinput derived from https://github.com/AvaloniaUI/Avalonia

#nullable enable

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using CodeBrix.Platform.UI.Runtime.Skia.Native;
using static CodeBrix.Platform.UI.Runtime.Skia.Native.LibInput;
using static Windows.UI.Input.PointerUpdateKind;
using static CodeBrix.Platform.UI.Runtime.Skia.Native.libinput_event_type;
using CodeBrix.Platform.Foundation.Logging;
using System.Collections.Generic;
using Windows.Graphics.Display;
using CodeBrix.Platform.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia; //Was previously: Uno.UI.Runtime.Skia

unsafe internal partial class FrameBufferPointerInputSource
{
	private readonly Dictionary<uint, Point> _activePointers = new();
	private readonly HashSet<libinput_event_code> _pointerPressed = new();

	public void ProcessTouchEvent(IntPtr rawEvent, libinput_event_type rawEventType)
	{
		var rawTouchEvent = libinput_event_get_touch_event(rawEvent);

		if (rawTouchEvent != IntPtr.Zero
			&& rawEventType < LIBINPUT_EVENT_TOUCH_FRAME)
		{
			var properties = new PointerPointProperties();
			var timestamp = libinput_event_touch_get_time_usec(rawTouchEvent);
			var pointerId = (uint)libinput_event_touch_get_slot(rawTouchEvent);
			Action<PointerEventArgs>? raisePointerEvent = null;
			Point currentPosition;

			if (rawEventType == LIBINPUT_EVENT_TOUCH_DOWN
				|| rawEventType == LIBINPUT_EVENT_TOUCH_MOTION)
			{
				var (x, y) = GetOrientationAdjustedAbsolutionPosition(rawTouchEvent, libinput_event_touch_get_x_transformed, libinput_event_touch_get_y_transformed);
				if (_touchRotated180)
				{
					// The digitizer is mounted upside-down relative to the panel:
					// mirror both axes in the window's coordinate space. A 180
					// flip commutes with every orientation mapping above, so it
					// composes correctly however the app is rotated.
					x = FrameBufferWindowWrapper.Instance.Bounds.Width - x;
					y = FrameBufferWindowWrapper.Instance.Bounds.Height - y;
				}
				// libinput's transformed coordinates span the CLOSED range
				// [0, dimension], and every "dimension - value" arm above (the
				// flipped orientation mappings and the 180 digitizer flip) turns
				// an exact-edge touch into exactly the dimension — ONE PAST the
				// last pixel, where a press hits nothing and pulls focus off the
				// control being typed into. Most reachable at the bottom edge of
				// a half-height keyboard's space bar. A finger at the bezel
				// belongs ON the edge pixel, so the final position is clamped
				// just inside the bounds.
				x = Math.Clamp(x, 0, Math.Max(0, FrameBufferWindowWrapper.Instance.Bounds.Width - 0.5));
				y = Math.Clamp(y, 0, Math.Max(0, FrameBufferWindowWrapper.Instance.Bounds.Height - 0.5));
				currentPosition = new Point(x, y);
				_activePointers[pointerId] = currentPosition;
			}
			else
			{
				_activePointers.TryGetValue(pointerId, out currentPosition);
				_activePointers.Remove(pointerId);
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"ProcessTouchEvent: {rawEventType}, pointerId:{pointerId}, currentPosition:{currentPosition}, timestamp:{timestamp}");
			}

			// A finger's full sequence is Entered / Pressed ... Released / Exited,
			// matching what the X11 head raises for XI_TouchBegin / XI_TouchEnd.
			// The trailing Exited is NOT cosmetic: for touch pointers the managed
			// input manager deliberately leaves captures alone on pointer-up and
			// relies on the source-level Exited to release them (there is no
			// auto-release safety net either - UIElement passes autoRelease: false
			// under managed pointers). Without it every explicit CapturePointer on
			// this head leaks, and each later release is re-routed to the stale
			// capture target instead of the element actually touched. The leading
			// Entered mirrors X11 for parity; the input manager also raises its own
			// enter ahead of the press, so it is the Exited that does the work.
			// Each pair is dispatched as ONE action so nothing can interleave
			// between the two raises. TOUCH_CANCEL needs no Exited - the cancel
			// path releases captures on its own.
			switch (rawEventType)
			{
				case LIBINPUT_EVENT_TOUCH_MOTION:
					raisePointerEvent = RaisePointerMoved;
					break;

				case LIBINPUT_EVENT_TOUCH_DOWN:
					properties.PointerUpdateKind = LeftButtonPressed;
					raisePointerEvent = touchArgs =>
					{
						SoftwareKeyboard.ActivePointerTracker.OnPointerDown(pointerId);
						RaisePointerEntered(touchArgs);
						RaisePointerPressed(touchArgs);
					};
					break;

				case LIBINPUT_EVENT_TOUCH_UP:
					properties.PointerUpdateKind = LeftButtonReleased;
					raisePointerEvent = touchArgs =>
					{
						RaisePointerReleased(touchArgs);
						RaisePointerExited(touchArgs);
						// After the release, so anything deferred until the finger
						// lifts (the software keyboard's auto-hide) runs once the
						// gesture it would have disturbed is complete.
						SoftwareKeyboard.ActivePointerTracker.OnPointerUp(pointerId);
					};
					break;

				case LIBINPUT_EVENT_TOUCH_CANCEL:
					properties.PointerUpdateKind = LeftButtonReleased;
					raisePointerEvent = touchArgs =>
					{
						RaisePointerCancelled(touchArgs);
						// A cancelled finger never sends TOUCH_UP; without this the
						// tracker would believe it is still down for ever.
						SoftwareKeyboard.ActivePointerTracker.OnPointerUp(pointerId);
					};
					break;
			}

			properties.IsLeftButtonPressed = rawEventType != LIBINPUT_EVENT_TOUCH_UP && rawEventType != LIBINPUT_EVENT_TOUCH_CANCEL;

			var timestampInMicroseconds = timestamp;
			var pointerPoint = new Windows.UI.Input.PointerPoint(
				frameId: (uint)timestamp, // UNO TODO: How should set the frame, timestamp may overflow.
				timestamp: timestampInMicroseconds,
				device: PointerDevice.For(PointerDeviceType.Touch),
				pointerId: pointerId,
				rawPosition: currentPosition,
				position: currentPosition,
				isInContact: properties.HasPressedButton,
				properties: properties
			);

			if (raisePointerEvent != null)
			{
				var args = new PointerEventArgs(pointerPoint, GetCurrentModifiersState());

				RaisePointerEvent(raisePointerEvent, args);
			}
			else
			{
				this.Log().LogWarning($"Touch event type {rawEventType} was not handled");
			}
		}
	}
}
