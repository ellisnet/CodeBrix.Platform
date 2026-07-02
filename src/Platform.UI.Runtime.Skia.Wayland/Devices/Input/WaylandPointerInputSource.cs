using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Controls;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Per-window pointer input source, fed by the seat manager following wl_pointer focus.
/// </summary>
internal partial class WaylandPointerInputSource : ICodeBrixCorePointerInputSource
{
	// Linux evdev button codes carried by wl_pointer.button.
	private const uint BTN_LEFT = 0x110;
	private const uint BTN_RIGHT = 0x111;
	private const uint BTN_MIDDLE = 0x112;
	private const uint BTN_SIDE = 0x113;
	private const uint BTN_EXTRA = 0x114;

#pragma warning disable CS0067 // touch is not wired yet, so Cancelled is never raised
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	private readonly WaylandXamlRootHost _host;
	private CoreCursor _pointerCursor;
	private PointerPointProperties? _previousPointerPointProperties;

	private Point _pointerPosition;
	private bool _leftPressed;
	private bool _middlePressed;
	private bool _rightPressed;

	public WaylandPointerInputSource(IXamlRootHost host)
	{
		if (host is not WaylandXamlRootHost waylandHost)
		{
			throw new ArgumentException($"{nameof(host)} must be a Wayland host instance");
		}

		_host = waylandHost;
		_host.SetPointerSource(this);

		_pointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
	}

	[NotImplemented] public bool HasCapture => false;

	public CoreCursor PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;
			ApplyCursor(value);
		}
	}

	/// <summary>
	/// The cursor is undefined on every wl_pointer.enter: the compositor keeps whatever shape
	/// was last set (e.g. libdecor's resize arrows on the window edge) until the client sets
	/// one for the new enter serial. The seat manager calls this on each enter of the content
	/// surface to re-assert the XAML-selected cursor.
	/// </summary>
	internal void ReapplyCursor() => ApplyCursor(_pointerCursor);

	private void ApplyCursor(CoreCursor? value)
	{
		// Prefer cursor-shape-v1 (the compositor themes the cursor for us). Setting a null
		// CoreCursor to hide the pointer, and a libwayland-cursor theme fallback for older
		// compositors, are later refinements.
		var shape = value?.Type switch
		{
			CoreCursorType.Arrow => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Default,
			CoreCursorType.Cross => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Crosshair,
			CoreCursorType.Hand => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Pointer,
			CoreCursorType.Help => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Help,
			CoreCursorType.IBeam => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Text,
			CoreCursorType.SizeAll => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Move,
			CoreCursorType.SizeNortheastSouthwest => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.NeswResize,
			CoreCursorType.SizeNorthSouth => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.NsResize,
			CoreCursorType.SizeNorthwestSoutheast => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.NwseResize,
			CoreCursorType.SizeWestEast => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.EwResize,
			CoreCursorType.UniversalNo => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.NotAllowed,
			CoreCursorType.Wait => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Wait,
			_ => Protocols.CursorShapeV1.WpCursorShapeDeviceV1.ShapeEnum.Default,
		};

		_ = _host.Connection?.SeatManager.SetCursorShape(shape);
	}

	public Point PointerPosition => _pointerPosition;

	// Wayland has no client-side pointer grab for regular surfaces; within-window capture
	// is handled at the XAML level, matching the X11 head's behavior.
	public void SetPointerCapture(PointerIdentifier pointer) => LogNotSupported();
	public void ReleasePointerCapture(PointerIdentifier pointer) => LogNotSupported();
	public void ReleasePointerCapture() => LogNotSupported();
	public void SetPointerCapture() => LogNotSupported();

	// All Process* methods run on the Wayland event-pump thread and marshal to UI.

	internal void ProcessEnter(Point position, VirtualKeyModifiers modifiers)
	{
		_pointerPosition = position;
		var args = CreatePointerEventArgsFromCurrentState(0, modifiers);
		WaylandXamlRootHost.QueueAction(_host, () => PointerEntered?.Invoke(this, args));
	}

	internal void ProcessLeave(Point position, VirtualKeyModifiers modifiers)
	{
		_pointerPosition = position;
		var args = CreatePointerEventArgsFromCurrentState(0, modifiers);
		WaylandXamlRootHost.QueueAction(_host, () => PointerExited?.Invoke(this, args));
	}

	internal void ProcessMotion(Point position, uint time, VirtualKeyModifiers modifiers)
	{
		_pointerPosition = position;
		var args = CreatePointerEventArgsFromCurrentState(time, modifiers);
		WaylandXamlRootHost.QueueAction(_host, () => PointerMoved?.Invoke(this, args));
	}

	internal void ProcessButton(Point position, uint time, uint button, bool pressed, VirtualKeyModifiers modifiers)
	{
		_pointerPosition = position;
		switch (button)
		{
			case BTN_LEFT:
				_leftPressed = pressed;
				break;
			case BTN_MIDDLE:
				_middlePressed = pressed;
				break;
			case BTN_RIGHT:
				_rightPressed = pressed;
				break;
		}

		var args = CreatePointerEventArgsFromCurrentState(time, modifiers);
		if (pressed)
		{
			WaylandXamlRootHost.QueueAction(_host, () => PointerPressed?.Invoke(this, args));
		}
		else
		{
			WaylandXamlRootHost.QueueAction(_host, () => PointerReleased?.Invoke(this, args));
		}
	}

	internal void ProcessWheel(Point position, uint time, bool horizontal, double detents, VirtualKeyModifiers modifiers)
	{
		_pointerPosition = position;
		var args = CreatePointerEventArgsFromCurrentState(time, modifiers);
		var props = args.CurrentPoint.Properties;
		props.IsHorizontalMouseWheel = horizontal;
		// Wayland scroll is smooth (a detent is ~15 surface units, already divided out by
		// the seat manager); negative wl values scroll up/left which is a POSITIVE wheel delta.
		props.MouseWheelDelta = (int)Math.Round(-detents * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta);
		if (props.MouseWheelDelta == 0)
		{
			return; // sub-detent noise; nothing to scroll
		}

		WaylandXamlRootHost.QueueAction(_host, () => PointerWheelChanged?.Invoke(this, args));
	}

	private PointerEventArgs CreatePointerEventArgsFromCurrentState(uint time, VirtualKeyModifiers modifiers)
		=> new(CreatePointFromCurrentState(time), modifiers);

	private PointerPoint CreatePointFromCurrentState(uint time)
	{
		var properties = new PointerPointProperties
		{
			IsLeftButtonPressed = _leftPressed,
			IsMiddleButtonPressed = _middlePressed,
			IsRightButtonPressed = _rightPressed,
		};

		var scale = ((IXamlRootHost)_host).RootElement?.XamlRoot is { } root
			? root.RasterizationScale
			: 1;

		// wl time is milliseconds with an undefined base; WinUI wants microseconds.
		var timeInMicroseconds = (ulong)time * 1000;
		var position = new Point(_pointerPosition.X / scale, _pointerPosition.Y / scale);
		var point = new PointerPoint(
			frameId: time,
			timestamp: timeInMicroseconds,
			PointerDevice.For(PointerDeviceType.Mouse),
			0,
			position,
			position,
			properties.HasPressedButton,
			properties.SetUpdateKindFromPrevious(_previousPointerPointProperties)
		);

		_previousPointerPointProperties = properties;

		return point;
	}

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Skia for Wayland.");
		}
	}
}
