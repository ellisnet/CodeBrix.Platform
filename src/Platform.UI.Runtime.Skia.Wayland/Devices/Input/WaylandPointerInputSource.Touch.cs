using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using static Windows.UI.Input.PointerUpdateKind;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// The wl_touch half of this window's pointer source. Touch rides the SAME source (and
/// therefore the same event set) as the mouse, exactly as the X11 head routes XInput2
/// touch through its pointer source — the input manager subscribes to one core pointer
/// source per window, so a separate touch source would never be seen.
/// </summary>
internal partial class WaylandPointerInputSource
{
	// A finger's full sequence is Entered / Pressed ... Released / Exited. The trailing
	// Exited is NOT cosmetic: for touch pointers the managed input manager deliberately
	// leaves captures alone on pointer-up and relies on the source-level Exited to release
	// them, and there is no auto-release fallback under managed pointers. Without it every
	// explicit CapturePointer leaks and each later release is re-routed to the stale
	// capture target instead of the element actually touched. The X11 head raises the same
	// pairs from XI_TouchBegin / XI_TouchEnd; the frame-buffer heads were missing the
	// Exited half and wedged app-wide touch input after a single tap until it was added.
	// Each pair is queued as ONE action so nothing can interleave between the two raises.

	internal void ProcessTouchDown(int contactId, Point position, uint time, VirtualKeyModifiers modifiers)
	{
		var args = CreateTouchEventArgs(contactId, position, time, inContact: true, LeftButtonPressed, modifiers);
		WaylandXamlRootHost.QueueAction(_host, () =>
		{
			PointerEntered?.Invoke(this, args);
			PointerPressed?.Invoke(this, args);
		});
	}

	internal void ProcessTouchMotion(int contactId, Point position, uint time, VirtualKeyModifiers modifiers)
	{
		var args = CreateTouchEventArgs(contactId, position, time, inContact: true, Other, modifiers);
		WaylandXamlRootHost.QueueAction(_host, () => PointerMoved?.Invoke(this, args));
	}

	internal void ProcessTouchUp(int contactId, Point position, uint time, VirtualKeyModifiers modifiers)
	{
		var args = CreateTouchEventArgs(contactId, position, time, inContact: false, LeftButtonReleased, modifiers);
		WaylandXamlRootHost.QueueAction(_host, () =>
		{
			PointerReleased?.Invoke(this, args);
			PointerExited?.Invoke(this, args);
		});
	}

	/// <summary>
	/// The compositor took the sequence over for a system gesture (an edge swipe, a
	/// workspace switch): the contact is gone and nothing further arrives for it. Cancel
	/// needs no trailing Exited — the input manager's cancel path releases captures itself.
	/// </summary>
	internal void ProcessTouchCancel(int contactId, Point position, uint time, VirtualKeyModifiers modifiers)
	{
		var args = CreateTouchEventArgs(contactId, position, time, inContact: false, LeftButtonReleased, modifiers);
		WaylandXamlRootHost.QueueAction(_host, () => PointerCancelled?.Invoke(this, args));
	}

	// Deliberately does NOT touch _pointerPosition or _previousPointerPointProperties:
	// those belong to the mouse, back the PointerPosition property and the cursor, and a
	// finger must not move the cursor. The update kind is set outright rather than derived
	// from a previous state, because contacts are independent of each other and of the
	// mouse.
	private PointerEventArgs CreateTouchEventArgs(int contactId, Point position, uint time,
		bool inContact, PointerUpdateKind updateKind, VirtualKeyModifiers modifiers)
	{
		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = inContact,
			PointerUpdateKind = updateKind,
		};

		// wl_touch coordinates are surface-local LOGICAL units, the same space wl_pointer
		// uses and exactly what the view layer wants — no rasterization-scale divide here
		// (see CreatePointFromCurrentState). wl time is milliseconds with an undefined
		// base; the view layer wants microseconds.
		var point = new PointerPoint(
			frameId: time,
			timestamp: (ulong)time * 1000,
			PointerDevice.For(PointerDeviceType.Touch),
			// The wl_touch contact id IS the pointer id, so concurrent fingers stay
			// distinct all the way into the input manager (X11 uses the XI2 touch id the
			// same way).
			(uint)contactId,
			position,
			position,
			inContact,
			properties
		);

		return new PointerEventArgs(point, modifiers);
	}
}
