using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace CodeBrix.Platform.PlayTest;

/// <summary>Synthetic keyboard input for the virtual application. Physical desktop input never reaches it.</summary>
public sealed class Keyboard
{
    private readonly PlayTestApplication _app;
    // Keys pressed with DownAsync and not yet released. UI thread only.
    private readonly HeldKeys _held = new();
    internal Keyboard(PlayTestApplication app) => _app = app;

    /// <summary>Presses and releases a key or chord on the focused element, for example <c>Enter</c>,
    /// <c>Control+z</c>, <c>Shift+Tab</c> or <c>ArrowDown</c>. Modifiers are released in reverse
    /// order even when a handler throws. <c>ControlOrMeta</c> maps to Control on every OS.</summary>
    /// <param name="key">One key name, or several joined by <c>+</c>: a letter or digit, a
    /// <c>Windows.System.VirtualKey</c> name (case-insensitive), or Control, Alt, Meta, Backspace,
    /// ArrowLeft, ArrowRight, ArrowUp, ArrowDown, ControlOrMeta.</param>
    /// <param name="options">Optional hold time (<see cref="KeyboardPressOptions.Delay"/>). Without it, key
    /// down and key up are dispatched together, so code that samples key state once per frame or game
    /// cycle may never see the key; hold it with a delay or with <see cref="DownAsync"/>.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or names an unsupported key.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The delay is negative or not finite.</exception>
    public async Task PressAsync(string key, KeyboardPressOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "KeyPress");
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var delay = ValidDelay(options?.Delay);
        var parts = key.Split('+');
        var keys = parts.Select(Parse).ToArray();
        if (delay == 0)
        {
            await _app.EvaluateAsync(() =>
            {
                var pressed = new List<VirtualKey>();
                try { PressKeys(parts, keys, pressed); }
                finally { ReleaseKeys(pressed); }
            }).ConfigureAwait(false);
        }
        else
        {
            var pressed = new List<VirtualKey>();
            try
            {
                await _app.EvaluateAsync(() =>
                {
                    try { PressKeys(parts, keys, pressed); }
                    catch { ReleaseKeys(pressed); pressed.Clear(); throw; }
                }).ConfigureAwait(false);
                // The application renders and its timers run while the chord is held.
                await _app.Host.CaptureAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);
            }
            finally { if (pressed.Count != 0) await _app.EvaluateAsync(() => ReleaseKeys(pressed)).ConfigureAwait(false); }
        }
        await _app.Host.CaptureAsync().ConfigureAwait(false);
        await _app.SlowAsync().ConfigureAwait(false);
    }

    /// <summary>Presses one key and keeps it down until <see cref="UpAsync"/>, like a held physical key
    /// (no auto-repeat). A held modifier (Shift, Control, Alt, Meta) applies to later presses and typing.
    /// Keys still held are released (their key-up events are dispatched) by
    /// <see cref="Page.SetContentAsync(Func{UIElement}, ScreenOrientation?)"/> before the old page is removed,
    /// and when the application is disposed, so they do not leak into the next test.</summary>
    /// <param name="key">One key name (see <see cref="PressAsync"/>); chords are not accepted.</param>
    /// <returns>A task that completes after a rendered frame, with the key still down.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty, a chord, or an unsupported key.</exception>
    public async Task DownAsync(string key)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "KeyDown");
        var virtualKey = ParseSingle(key);
        await _app.EvaluateAsync(() =>
        {
            _app.Host.Input.Key(true, virtualKey, Character(key));
            _held.Add(virtualKey);
        }).ConfigureAwait(false);
        await _app.Host.CaptureAsync().ConfigureAwait(false);
        await _app.SlowAsync().ConfigureAwait(false);
    }

    /// <summary>Releases a key pressed with <see cref="DownAsync"/>. Releasing a key that is not held
    /// still dispatches its key-up event, as a physical keyboard would after focus moved.</summary>
    /// <param name="key">One key name (see <see cref="PressAsync"/>).</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty, a chord, or an unsupported key.</exception>
    public async Task UpAsync(string key)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "KeyUp");
        var virtualKey = ParseSingle(key);
        await _app.EvaluateAsync(() =>
        {
            _held.Remove(virtualKey);
            _app.Host.Input.Key(false, virtualKey);
        }).ConfigureAwait(false);
        await _app.Host.CaptureAsync().ConfigureAwait(false);
        await _app.SlowAsync().ConfigureAwait(false);
    }

    // UI thread: releases every key still held by DownAsync, most recent first.
    internal void ReleaseHeldKeys()
    {
        foreach (var key in _held.TakeAll()) _app.Host.Input.Key(false, key);
    }

    private void PressKeys(string[] parts, VirtualKey[] keys, List<VirtualKey> pressed)
    {
        for (var i = 0; i < keys.Length; i++)
        {
            _app.Host.Input.Key(true, keys[i], Character(parts[i]));
            pressed.Add(keys[i]);
        }
    }

    private void ReleaseKeys(List<VirtualKey> pressed)
    {
        for (var i = pressed.Count - 1; i >= 0; i--) _app.Host.Input.Key(false, pressed[i]);
    }

    // Internal (not private) so the host-free unit tests can fence these rules.
    internal static char? Character(string key) => key.Length == 1 ? key[0] : key == "Space" ? ' ' : null;

    internal static VirtualKey ParseSingle(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 1 && key.Contains('+'))
            throw new ArgumentException($"'{key}' is a chord. Hold one key per DownAsync call; release each with UpAsync.", nameof(key));
        return Parse(key);
    }

    internal static float ValidDelay(float? delay)
    {
        var value = delay ?? 0;
        if (value < 0 || !float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(delay), "The delay must be a finite, non-negative number of milliseconds.");
        return value;
    }

    /// <summary>Replaces the focused text box's selection with <paramref name="text"/> without key events,
    /// like pasting. Use <see cref="TypeAsync"/> to exercise key handlers.</summary>
    /// <param name="text">The text to insert.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    /// <exception cref="PlayTestException">No editable, enabled text box has focus.</exception>
    public async Task InsertTextAsync(string text)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "InsertText");
        ArgumentNullException.ThrowIfNull(text);
        await _app.EvaluateAsync(() =>
        {
            if (_app.Host.Root?.XamlRoot is not { } root
                || FocusManager.GetFocusedElement(root) is not TextBox { IsReadOnly: false, IsEnabled: true } box)
                throw new PlayTestException("InsertTextAsync requires a focused, editable text box.");
            var start = box.SelectionStart;
            var retainedLength = box.Text.Length - box.SelectionLength;
            box.SelectedText = text;
            box.Select(start + box.Text.Length - retainedLength, 0);
        }).ConfigureAwait(false);
        await _app.Host.CaptureAsync().ConfigureAwait(false);
    }

    /// <summary>Types characters through routed key events on the focused control. Newlines and tabs
    /// use Enter and Tab, preserving the application's indentation, completion and shortcut handling.</summary>
    /// <param name="text">The characters to type; CR and CRLF are typed as Enter.</param>
    /// <param name="options">Optional wait between characters.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The delay is negative or not finite.</exception>
    public async Task TypeAsync(string text, KeyboardTypeOptions? options = null)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "Type");
        ArgumentNullException.ThrowIfNull(text);
        var delay = ValidDelay(options?.Delay);
        var characters = VisualTree.InputText(text);
        var first = true;
        foreach (var character in characters)
        {
            if (!first && delay > 0)
            {
                await _app.Host.CaptureAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);
            }
            first = false;
            await _app.EvaluateAsync(() =>
            {
                var key = character switch
                {
                    '\n' => VirtualKey.Enter,
                    '\t' => VirtualKey.Tab,
                    _ when char.IsAsciiLetter(character) => Enum.Parse<VirtualKey>(char.ToUpperInvariant(character).ToString()),
                    _ when char.IsAsciiDigit(character) => VirtualKey.Number0 + (character - '0'),
                    _ => VirtualKey.None,
                };
                try { _app.Host.Input.Key(true, key, character); }
                finally { _app.Host.Input.Key(false, key); }
            }).ConfigureAwait(false);
        }
        await _app.Host.CaptureAsync().ConfigureAwait(false);
        await _app.SlowAsync().ConfigureAwait(false);
    }

    // Internal (not private) so the host-free unit tests can fence the key-name table.
    internal static VirtualKey Parse(string key) => key switch
    {
        "ControlOrMeta" => VirtualKey.Control, // Virtual head has a stable Windows-style key model on every OS.
        "Control" => VirtualKey.Control,
        "Alt" => VirtualKey.Menu,
        "Meta" => VirtualKey.LeftWindows,
        "Backspace" => VirtualKey.Back,
        "ArrowLeft" => VirtualKey.Left,
        "ArrowRight" => VirtualKey.Right,
        "ArrowUp" => VirtualKey.Up,
        "ArrowDown" => VirtualKey.Down,
        _ when key.Length == 1 && char.IsLetter(key[0]) => Enum.Parse<VirtualKey>(key.ToUpperInvariant()),
        _ when key.Length == 1 && char.IsDigit(key[0]) => VirtualKey.Number0 + (key[0] - '0'),
        _ when Enum.TryParse<VirtualKey>(key, true, out var result) => result,
        _ => throw new ArgumentException($"Unsupported key '{key}'. Use a named virtual key or InsertTextAsync for text."),
    };
}

/// <summary>Synthetic mouse input at virtual-screen coordinates. Prefer locator actions, which wait for
/// a stable, hit-testable target; use the mouse directly for free-form gestures.</summary>
public sealed class Mouse
{
    private readonly PlayTestApplication _app;
    internal Mouse(PlayTestApplication app) => _app = app;

    /// <summary>Moves the pointer to a virtual-screen position.</summary>
    /// <param name="x">Horizontal position in logical pixels.</param>
    /// <param name="y">Vertical position in logical pixels.</param>
    /// <returns>A task that completes when the move has been dispatched.</returns>
    public async Task MoveAsync(float x, float y)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseMove");
        await _app.EvaluateAsync(() => _app.Host.Input.Move(x, y)).ConfigureAwait(false);
    }
    /// <summary>Presses the left button at the current pointer position.</summary>
    /// <returns>A task that completes when the press has been dispatched.</returns>
    public async Task DownAsync()
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseDown");
        await _app.EvaluateAsync(_app.Host.Input.Down).ConfigureAwait(false);
    }
    /// <summary>Releases the left button at the current pointer position.</summary>
    /// <returns>A task that completes when the release has been dispatched.</returns>
    public async Task UpAsync()
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseUp");
        await _app.EvaluateAsync(_app.Host.Input.Up).ConfigureAwait(false);
    }
    /// <summary>Moves to a virtual-screen position and clicks the left button there, without the
    /// stability and hit-test checks of <see cref="Locator.ClickAsync"/>.</summary>
    /// <param name="x">Horizontal position in logical pixels.</param>
    /// <param name="y">Vertical position in logical pixels.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task ClickAsync(float x, float y)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseClick");
        await MoveAsync(x, y).ConfigureAwait(false);
        await DownAsync().ConfigureAwait(false);
        await UpAsync().ConfigureAwait(false);
        await _app.Host.CaptureAsync().ConfigureAwait(false);
        await _app.SlowAsync().ConfigureAwait(false);
    }
    /// <summary>Turns the mouse wheel at the current pointer position.</summary>
    /// <param name="deltaX">Must be zero: horizontal wheel input is not supported.</param>
    /// <param name="deltaY">Vertical delta; positive scrolls content down, as in a browser.</param>
    /// <returns>A task that completes when the wheel event has been dispatched.</returns>
    /// <exception cref="NotSupportedException"><paramref name="deltaX"/> is not zero.</exception>
    public async Task WheelAsync(float deltaX, float deltaY)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseWheel");
        if (deltaX != 0) throw new NotSupportedException("PlayTest supports vertical scrolling only.");
        await _app.EvaluateAsync(() => _app.Host.Input.Wheel(-(int)deltaY)).ConfigureAwait(false);
    }
}

// The keys a test holds with Keyboard.DownAsync, in press order. A key pressed twice is held once.
internal sealed class HeldKeys
{
    private readonly List<VirtualKey> _keys = new();
    internal IReadOnlyList<VirtualKey> Keys => _keys;
    internal void Add(VirtualKey key) { if (!_keys.Contains(key)) _keys.Add(key); }
    internal void Remove(VirtualKey key) => _keys.Remove(key);

    // Most recently pressed first, so modifiers pressed before a key are released after it.
    internal VirtualKey[] TakeAll()
    {
        var keys = Enumerable.Reverse(_keys).ToArray();
        _keys.Clear();
        return keys;
    }
}
