// Pointer/key construction adapted from FrameBuffer.Emulated/Devices/Input.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;

namespace CodeBrix.Platform.PlayTest.Hosting;

internal sealed class VirtualInput : ICodeBrixCorePointerInputSource, ICodeBrixKeyboardInputSource
{
    private readonly VirtualHost _host;
    private readonly HashSet<PointerIdentifier> _captures = new();
    private VirtualKeyModifiers _modifiers;
    private bool _leftPressed;
    private bool _entered;
    internal VirtualInput(VirtualHost host) => _host = host;
    public event TypedEventHandler<object, PointerEventArgs> PointerEntered;
    public event TypedEventHandler<object, PointerEventArgs> PointerExited { add { } remove { } }
    public event TypedEventHandler<object, PointerEventArgs> PointerMoved;
    public event TypedEventHandler<object, PointerEventArgs> PointerPressed;
    public event TypedEventHandler<object, PointerEventArgs> PointerReleased;
    public event TypedEventHandler<object, PointerEventArgs> PointerWheelChanged;
    public event TypedEventHandler<object, PointerEventArgs> PointerCancelled { add { } remove { } }
    public event TypedEventHandler<object, PointerEventArgs> PointerCaptureLost { add { } remove { } }
    public event TypedEventHandler<object, KeyEventArgs> KeyDown;
    public event TypedEventHandler<object, KeyEventArgs> KeyUp;
    public bool HasCapture => _captures.Count > 0;
    public CoreCursor PointerCursor { get; set; } = new(CoreCursorType.Arrow, 0);
    public Point PointerPosition { get; private set; }
    public void SetPointerCapture(PointerIdentifier pointer) => _captures.Add(pointer);
    public void ReleasePointerCapture(PointerIdentifier pointer) => _captures.Remove(pointer);
    public void SetPointerCapture() { }
    public void ReleasePointerCapture() => _captures.Clear();

    // All calls execute on the virtual application's dispatcher. Physical desktop input
    // is never connected to this source, including while the preview has keyboard focus.
    internal void Move(double x, double y) => Pointer("move", x, y);
    internal void Down() => Pointer("down", PointerPosition.X, PointerPosition.Y);
    internal void Up() => Pointer("up", PointerPosition.X, PointerPosition.Y);
    internal void Wheel(int delta) => Pointer("wheel", PointerPosition.X, PointerPosition.Y, delta);

    private void Pointer(string kind, double x, double y, int wheel = 0)
    {
        PointerPosition = new Point(x, y);
        if (kind == "down") _leftPressed = true;
        if (kind == "up") _leftPressed = false;
        var properties = new PointerPointProperties
        {
            IsLeftButtonPressed = _leftPressed,
            PointerUpdateKind = kind == "down" ? PointerUpdateKind.LeftButtonPressed
                : kind == "up" ? PointerUpdateKind.LeftButtonReleased : PointerUpdateKind.Other,
            MouseWheelDelta = wheel,
        };
        var timestamp = (ulong)(Stopwatch.GetTimestamp() * 1_000_000.0 / Stopwatch.Frequency);
        var point = new PointerPoint((uint)timestamp, timestamp, PointerDevice.For(PointerDeviceType.Mouse),
            1, PointerPosition, PointerPosition, _leftPressed, properties);
        var args = new PointerEventArgs(point, _modifiers);
        if (!_entered) { _entered = true; PointerEntered?.Invoke(this, args); }
        switch (kind)
        {
            case "move": PointerMoved?.Invoke(this, args); break;
            case "down": PointerPressed?.Invoke(this, args); break;
            case "up": PointerReleased?.Invoke(this, args); _captures.Clear(); break;
            case "wheel": PointerWheelChanged?.Invoke(this, args); break;
        }
        _host.InvalidateRender();
    }

    internal void Key(bool down, VirtualKey key, char? character = null)
    {
        var modifier = key switch
        {
            VirtualKey.Control => VirtualKeyModifiers.Control,
            VirtualKey.Shift => VirtualKeyModifiers.Shift,
            VirtualKey.Menu => VirtualKeyModifiers.Menu,
            VirtualKey.LeftWindows => VirtualKeyModifiers.Windows,
            _ => VirtualKeyModifiers.None,
        };
        _modifiers = down ? _modifiers | modifier : _modifiers & ~modifier;
        var args = new KeyEventArgs("PlayTest", key, _modifiers,
            new CorePhysicalKeyStatus { RepeatCount = 1 }, down ? character : null);
        (down ? KeyDown : KeyUp)?.Invoke(this, args);
        _host.InvalidateRender();
    }
}
