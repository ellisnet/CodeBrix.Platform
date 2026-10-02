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
    internal Keyboard(PlayTestApplication app) => _app = app;

    /// <summary>Presses and releases a key or chord on the focused element, for example <c>Enter</c>,
    /// <c>Control+z</c>, <c>Shift+Tab</c> or <c>ArrowDown</c>. Modifiers are released in reverse
    /// order even when a handler throws. <c>ControlOrMeta</c> maps to Control on every OS.</summary>
    /// <param name="key">One key name, or several joined by <c>+</c>: a letter or digit, a
    /// <c>Windows.System.VirtualKey</c> name (case-insensitive), or Control, Alt, Meta, Backspace,
    /// ArrowLeft, ArrowRight, ArrowUp, ArrowDown, ControlOrMeta.</param>
    /// <returns>A task that completes after the next rendered frame.</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or names an unsupported key.</exception>
    public async Task PressAsync(string key)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "KeyPress");
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var parts = key.Split('+');
        var keys = parts.Select(Parse).ToArray();
        await _app.EvaluateAsync(() =>
        {
            var pressed = new List<VirtualKey>();
            try
            {
                for (var i = 0; i < keys.Length; i++)
                {
                    char? character = parts[i].Length == 1 ? parts[i][0] : parts[i] == "Space" ? ' ' : null;
                    _app.Host.Input.Key(true, keys[i], character);
                    pressed.Add(keys[i]);
                }
            }
            finally { for (var i = pressed.Count - 1; i >= 0; i--) _app.Host.Input.Key(false, pressed[i]); }
        }).ConfigureAwait(false);
        await _app.Host.CaptureAsync().ConfigureAwait(false);
        await _app.SlowAsync().ConfigureAwait(false);
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
    /// <returns>A task that completes after the next rendered frame.</returns>
    public async Task TypeAsync(string text)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "Type");
        ArgumentNullException.ThrowIfNull(text);
        var characters = VisualTree.InputText(text);
        foreach (var character in characters)
        {
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
