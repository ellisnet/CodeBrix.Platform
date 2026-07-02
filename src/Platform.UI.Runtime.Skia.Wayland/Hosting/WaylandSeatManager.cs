using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using Windows.System;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.CursorShapeV1;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// Owns the seat-level input devices (wl_pointer, wl_keyboard) and routes their events to
/// the per-window input sources, following pointer/keyboard focus. Also owns the xkbcommon
/// keymap state (the compositor supplies the keymap via fd) and the client-side key-repeat
/// timer that Wayland requires.
/// </summary>
internal sealed class WaylandSeatManager
{
	private const uint EvdevKeycodeOffset = 8; // evdev scancode -> xkb keycode

	private readonly WaylandConnection _connection;
	private WlSeat? _seat;

	private WlPointer? _pointer;
	private WlKeyboard? _keyboard;
	private WpCursorShapeDeviceV1? _cursorShapeDevice;

	// Pointer state (event-pump thread).
	private WaylandXamlRootHost? _pointerFocusHost;
	private Point _pointerPosition;
	private uint _lastPointerEnterSerial;
	private uint _lastInputSerial;

	// Cursor-theme fallback state, used when the compositor does not advertise
	// cursor-shape-v1 (notably Muffin/Cinnamon; GNOME and KDE do advertise it).
	private readonly object _cursorGate = new();
	private IntPtr _cursorTheme;
	private bool _cursorThemeLoadAttempted;
	private WlSurface? _cursorSurface;
	private readonly Dictionary<string, (WlBuffer Buffer, int Width, int Height, int HotspotX, int HotspotY)> _cursorCache = new();

	// Keyboard state (event-pump thread).
	private WaylandXamlRootHost? _keyboardFocusHost;
	private IntPtr _xkbContext;
	private IntPtr _xkbKeymap;
	private IntPtr _xkbState;

	// Client-side key repeat: Wayland compositors do NOT repeat keys for us.
	private int _repeatRatePerSecond = 25;
	private int _repeatDelayMilliseconds = 400;
	private readonly Timer _repeatTimer;
	private uint _repeatKeycode;
	private uint _repeatCount;

	public WaylandSeatManager(WaylandConnection connection)
	{
		_connection = connection;
		_repeatTimer = new Timer(OnRepeatTimer);
	}

	/// <summary>
	/// Called by the connection right after binding wl_seat (the capabilities listener is
	/// attached at bind time and relays here; events arrive on the following roundtrip).
	/// </summary>
	internal void AttachSeat(WlSeat seat) => _seat = seat;

	internal void OnCapabilities(WlSeat.CapabilityEnum capabilities)
	{
		if (_seat is not { } seat)
		{
			return;
		}

		var hasPointer = (capabilities & WlSeat.CapabilityEnum.Pointer) != 0;
		var hasKeyboard = (capabilities & WlSeat.CapabilityEnum.Keyboard) != 0;

		if (hasPointer && _pointer == null)
		{
			_pointer = seat.GetPointer(new WlPointer.Listener.Relay
			{
				OnEnter = (_, serial, surface, x, y) => OnPointerEnter(serial, surface, (double)x, (double)y),
				OnLeave = (_, _, surface) => OnPointerLeave(surface),
				OnMotion = (_, time, x, y) => OnPointerMotion(time, (double)x, (double)y),
				OnButton = (_, serial, time, button, state) => OnPointerButton(serial, time, button, state == WlPointer.ButtonStateEnum.Pressed),
				OnAxis = (_, time, axis, value) => OnPointerAxis(time, axis, (double)value),
			});

			if (_connection.CursorShapeManager is { } cursorManager)
			{
				_cursorShapeDevice = cursorManager.GetPointer(_pointer);
			}
		}
		else if (!hasPointer && _pointer != null)
		{
			_cursorShapeDevice?.Destroy();
			_cursorShapeDevice = null;
			_pointer.Release();
			_pointer = null;
		}

		if (hasKeyboard && _keyboard == null)
		{
			_keyboard = seat.GetKeyboard(new WlKeyboard.Listener.Relay
			{
				OnKeymap = (_, format, fd, size) => OnKeymap(format, fd, size),
				OnEnter = (_, _, surface, _) => OnKeyboardEnter(surface),
				OnLeave = (_, _, _) => OnKeyboardLeave(),
				OnKey = (_, serial, time, key, state) => { _lastInputSerial = serial; OnKey(time, key, state == WlKeyboard.KeyStateEnum.Pressed, isRepeat: false); },
				OnModifiers = (_, _, depressed, latched, locked, group) => OnModifiers(depressed, latched, locked, group),
				OnRepeatInfo = (_, rate, delay) => OnRepeatInfo(rate, delay),
			});
		}
		else if (!hasKeyboard && _keyboard != null)
		{
			StopRepeat();
			_keyboard.Release();
			_keyboard = null;
		}
	}

	internal VirtualKeyModifiers CurrentModifiers
	{
		get
		{
			if (_xkbState == IntPtr.Zero)
			{
				return VirtualKeyModifiers.None;
			}

			var modifiers = VirtualKeyModifiers.None;
			const LibXKBCommon.xkb_state_component effective = LibXKBCommon.xkb_state_component.XKB_STATE_MODS_EFFECTIVE;
			if (LibXKBCommon.xkb_state_mod_name_is_active(_xkbState, "Shift", effective) == 1)
			{
				modifiers |= VirtualKeyModifiers.Shift;
			}
			if (LibXKBCommon.xkb_state_mod_name_is_active(_xkbState, "Control", effective) == 1)
			{
				modifiers |= VirtualKeyModifiers.Control;
			}
			if (LibXKBCommon.xkb_state_mod_name_is_active(_xkbState, "Mod1", effective) == 1)
			{
				modifiers |= VirtualKeyModifiers.Menu;
			}
			if (LibXKBCommon.xkb_state_mod_name_is_active(_xkbState, "Mod4", effective) == 1)
			{
				modifiers |= VirtualKeyModifiers.Windows;
			}
			return modifiers;
		}
	}

	/// <summary>
	/// Sets the pointer cursor: via cursor-shape-v1 when the compositor advertises it (the
	/// compositor themes the cursor for us), else by attaching the matching XCursor-theme
	/// image from libwayland-cursor to a cursor surface. Uses the last pointer-enter serial,
	/// as both paths require.
	/// </summary>
	internal bool SetCursorShape(WpCursorShapeDeviceV1.ShapeEnum shape)
	{
		if (_cursorShapeDevice is { } device)
		{
			device.SetShape(_lastPointerEnterSerial, shape);
			_connection.Flush();
			return true;
		}

		return SetThemedCursor(shape);
	}

	private bool SetThemedCursor(WpCursorShapeDeviceV1.ShapeEnum shape)
	{
		if (_pointer is not { } pointer)
		{
			return false;
		}

		lock (_cursorGate)
		{
			if (!EnsureCursorTheme() || ResolveThemedCursor(shape) is not { } cursor)
			{
				return false;
			}

			_cursorSurface ??= _connection.Compositor.CreateSurface();
			_cursorSurface.Attach(cursor.Buffer, 0, 0);
			_cursorSurface.Damage(0, 0, cursor.Width, cursor.Height);
			_cursorSurface.Commit();
			pointer.SetCursor(_lastPointerEnterSerial, _cursorSurface, cursor.HotspotX, cursor.HotspotY);
			_connection.Flush();
			return true;
		}
	}

	// Must hold _cursorGate.
	private bool EnsureCursorTheme()
	{
		if (_cursorTheme != IntPtr.Zero)
		{
			return true;
		}
		if (_cursorThemeLoadAttempted)
		{
			return false;
		}
		_cursorThemeLoadAttempted = true;

		var themeName = Environment.GetEnvironmentVariable("XCURSOR_THEME");
		var size = int.TryParse(Environment.GetEnvironmentVariable("XCURSOR_SIZE"), out var parsed) && parsed > 0
			? parsed
			: 24;
		_cursorTheme = LibWaylandCursor.wl_cursor_theme_load(
			string.IsNullOrEmpty(themeName) ? null : themeName, size, _connection.Shm.Handle);

		if (_cursorTheme == IntPtr.Zero && this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn("No cursor-shape-v1 protocol and no loadable XCursor theme; the pointer cursor cannot be set.");
		}
		return _cursorTheme != IntPtr.Zero;
	}

	// Must hold _cursorGate.
	private (WlBuffer Buffer, int Width, int Height, int HotspotX, int HotspotY)? ResolveThemedCursor(WpCursorShapeDeviceV1.ShapeEnum shape)
	{
		// The cursor-spec name first, then the legacy XCursor alias older themes still use.
		var (name, legacyName) = shape switch
		{
			WpCursorShapeDeviceV1.ShapeEnum.Crosshair => ("crosshair", "cross"),
			WpCursorShapeDeviceV1.ShapeEnum.Pointer => ("pointer", "hand2"),
			WpCursorShapeDeviceV1.ShapeEnum.Help => ("help", "question_arrow"),
			WpCursorShapeDeviceV1.ShapeEnum.Text => ("text", "xterm"),
			WpCursorShapeDeviceV1.ShapeEnum.Move => ("move", "fleur"),
			WpCursorShapeDeviceV1.ShapeEnum.NeswResize => ("nesw-resize", "fd_double_arrow"),
			WpCursorShapeDeviceV1.ShapeEnum.NsResize => ("ns-resize", "sb_v_double_arrow"),
			WpCursorShapeDeviceV1.ShapeEnum.NwseResize => ("nwse-resize", "bd_double_arrow"),
			WpCursorShapeDeviceV1.ShapeEnum.EwResize => ("ew-resize", "sb_h_double_arrow"),
			WpCursorShapeDeviceV1.ShapeEnum.NotAllowed => ("not-allowed", "crossed_circle"),
			WpCursorShapeDeviceV1.ShapeEnum.Wait => ("wait", "watch"),
			_ => ("default", "left_ptr"),
		};

		if (_cursorCache.TryGetValue(name, out var cached))
		{
			return cached;
		}

		if (!LibWaylandCursor.TryGetCursorImage(_cursorTheme, name, out var raw, out var width, out var height, out var hotspotX, out var hotspotY)
			&& !LibWaylandCursor.TryGetCursorImage(_cursorTheme, legacyName, out raw, out width, out height, out hotspotX, out hotspotY))
		{
			return null;
		}

		// The wl_buffer belongs to the theme; import it borrowed so we never destroy it.
		var buffer = WlBuffer.Import(_connection.Display, null, raw, ownsHandle: false, listener: null);
		var entry = (buffer, width, height, hotspotX, hotspotY);
		_cursorCache[name] = entry;
		return entry;
	}

	internal Point PointerPosition => _pointerPosition;
	internal uint LastPointerEnterSerial => _lastPointerEnterSerial;
	internal uint LastInputSerial => _lastInputSerial;
	internal WlSeat? Seat => _seat;
	internal WlPointer? Pointer => _pointer;

	private void OnPointerEnter(uint serial, WlSurface? surface, double x, double y)
	{
		_lastPointerEnterSerial = serial;
		_lastInputSerial = serial;
		_pointerPosition = new Point(x, y);
		_pointerFocusHost = WaylandXamlRootHost.GetHostFromSurface(surface);

		// The cursor shape is undefined on enter — without a set the compositor keeps the
		// previous one (e.g. libdecor's resize arrow from the window edge). Re-assert the
		// window's cursor against this enter's serial before delivering the event.
		_pointerFocusHost?.PointerSource?.ReapplyCursor();

		_pointerFocusHost?.PointerSource?.ProcessEnter(_pointerPosition, CurrentModifiers);
	}

	private void OnPointerLeave(WlSurface? surface)
	{
		var host = WaylandXamlRootHost.GetHostFromSurface(surface) ?? _pointerFocusHost;
		host?.PointerSource?.ProcessLeave(_pointerPosition, CurrentModifiers);
		_pointerFocusHost = null;
	}

	private void OnPointerMotion(uint time, double x, double y)
	{
		_pointerPosition = new Point(x, y);
		_pointerFocusHost?.PointerSource?.ProcessMotion(_pointerPosition, time, CurrentModifiers);
	}

	private void OnPointerButton(uint serial, uint time, uint button, bool pressed)
	{
		_lastInputSerial = serial;
		_pointerFocusHost?.PointerSource?.ProcessButton(_pointerPosition, time, button, pressed, CurrentModifiers);
	}

	private void OnPointerAxis(uint time, WlPointer.AxisEnum axis, double value)
	{
		// libinput's smooth-scroll convention: one wheel detent = 15 surface units;
		// positive values scroll down/right.
		var detents = value / 15.0;
		_pointerFocusHost?.PointerSource?.ProcessWheel(
			_pointerPosition, time,
			horizontal: axis == WlPointer.AxisEnum.HorizontalScroll,
			detents, CurrentModifiers);
	}

	private void OnKeymap(WlKeyboard.KeymapFormatEnum format, Interop.WaylandFd fd, uint size)
	{
		var fdValue = fd.Consume();
		try
		{
			if (format != WlKeyboard.KeymapFormatEnum.XkbV1)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unsupported keymap format {format}; keyboard input will be unavailable.");
				}
				return;
			}

			var mapped = Libc.mmap(IntPtr.Zero, (UIntPtr)size, Libc.PROT_READ, MAP_PRIVATE, fdValue, 0);
			if (mapped == Libc.MAP_FAILED)
			{
				return;
			}

			try
			{
				if (_xkbContext == IntPtr.Zero)
				{
					_xkbContext = LibXKBCommon.xkb_context_new(LibXKBCommon.xkb_context_flags.XKB_CONTEXT_NO_FLAGS);
				}

				var keymap = LibXKBCommon.xkb_keymap_new_from_string(
					_xkbContext, mapped,
					LibXKBCommon.xkb_keymap_format.XKB_KEYMAP_FORMAT_TEXT_V1,
					LibXKBCommon.xkb_keymap_compile_flags.XKB_KEYMAP_COMPILE_NO_FLAGS);
				if (keymap == IntPtr.Zero)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("xkb_keymap_new_from_string failed; keyboard input will be unavailable.");
					}
					return;
				}

				var state = LibXKBCommon.xkb_state_new(keymap);
				if (state == IntPtr.Zero)
				{
					LibXKBCommon.xkb_keymap_unref(keymap);
					return;
				}

				if (_xkbState != IntPtr.Zero)
				{
					LibXKBCommon.xkb_state_unref(_xkbState);
				}
				if (_xkbKeymap != IntPtr.Zero)
				{
					LibXKBCommon.xkb_keymap_unref(_xkbKeymap);
				}

				_xkbKeymap = keymap;
				_xkbState = state;
			}
			finally
			{
				_ = Libc.munmap(mapped, (UIntPtr)size);
			}
		}
		finally
		{
			_ = Libc.close(fdValue);
		}
	}

	private const int MAP_PRIVATE = 0x02;

	private void OnKeyboardEnter(WlSurface? surface)
		=> _keyboardFocusHost = WaylandXamlRootHost.GetHostFromSurface(surface);

	private void OnKeyboardLeave()
	{
		StopRepeat();
		_keyboardFocusHost = null;
	}

	private void OnRepeatInfo(int rate, int delay)
	{
		// rate 0 disables repeat entirely.
		_repeatRatePerSecond = rate;
		_repeatDelayMilliseconds = delay;
	}

	private void OnModifiers(uint depressed, uint latched, uint locked, uint group)
	{
		if (_xkbState != IntPtr.Zero)
		{
			_ = LibXKBCommon.xkb_state_update_mask(_xkbState, depressed, latched, locked, 0, 0, group);
		}
	}

	private void OnKey(uint time, uint key, bool pressed, bool isRepeat)
	{
		if (_xkbState == IntPtr.Zero)
		{
			return;
		}

		var keycode = key + EvdevKeycodeOffset;
		var keysym = LibXKBCommon.xkb_state_key_get_one_sym(_xkbState, keycode);
		var unicode = GetUtf8Key(keycode);
		var repeatCount = isRepeat ? ++_repeatCount : 1u;

		_keyboardFocusHost?.KeyboardSource?.ProcessKeyEvent(
			pressed,
			XkbKeyTransform.VirtualKeyFromKeySym((IntPtr)keysym),
			CurrentModifiers,
			scanCode: keycode,
			repeatCount,
			unicode);

		if (!isRepeat)
		{
			if (pressed && _repeatRatePerSecond > 0 && LibXKBCommon.xkb_keymap_key_repeats(_xkbKeymap, keycode) == 1)
			{
				_repeatKeycode = key;
				_repeatCount = 1;
				_ = _repeatTimer.Change(_repeatDelayMilliseconds, Math.Max(1, 1000 / _repeatRatePerSecond));
			}
			else if (!pressed && key == _repeatKeycode)
			{
				StopRepeat();
			}
		}
	}

	private void OnRepeatTimer(object? state)
	{
		// Timer thread; xkb state reads are safe enough here since the pump only mutates
		// it on modifier changes, and a stale modifier read is inconsequential for repeat.
		OnKey(0, _repeatKeycode, pressed: true, isRepeat: true);
	}

	private void StopRepeat()
	{
		_ = _repeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
		_repeatCount = 0;
	}

	private char? GetUtf8Key(uint keycode)
	{
		unsafe
		{
			var buffer = stackalloc byte[8];
			var length = LibXKBCommon.xkb_state_key_get_utf8(_xkbState, keycode, buffer, 8);
			if (length <= 0)
			{
				return null;
			}

			var text = System.Text.Encoding.UTF8.GetString(buffer, Math.Min(length, 7));
			if (string.IsNullOrEmpty(text) || (text != "\r" && char.IsControl(text[0])))
			{
				return null;
			}

			return text[0];
		}
	}
}
