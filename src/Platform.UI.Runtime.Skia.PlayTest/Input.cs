using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace CodeBrix.Platform.PlayTest;

public sealed class Keyboard
{
    private readonly PlayTestApplication _app;
    internal Keyboard(PlayTestApplication app) => _app = app;

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

    public async Task InsertTextAsync(string text)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "InsertText");
        ArgumentNullException.ThrowIfNull(text);
        await _app.EvaluateAsync(() =>
        {
            if (FocusManager.GetFocusedElement(_app.Host.Root.XamlRoot) is not TextBox { IsReadOnly: false, IsEnabled: true } box)
                throw new PlayTestException("InsertTextAsync requires a focused, editable text box.");
            var start = box.SelectionStart;
            var retainedLength = box.Text.Length - box.SelectionLength;
            box.SelectedText = text;
            box.Select(start + box.Text.Length - retainedLength, 0);
        }).ConfigureAwait(false);
        await _app.Host.CaptureAsync().ConfigureAwait(false);
    }

    private static VirtualKey Parse(string key) => key switch
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

public sealed class Mouse
{
    private readonly PlayTestApplication _app;
    internal Mouse(PlayTestApplication app) => _app = app;
    public async Task MoveAsync(float x, float y)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseMove");
        await _app.EvaluateAsync(() => _app.Host.Input.Move(x, y)).ConfigureAwait(false);
    }
    public async Task DownAsync()
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseDown");
        await _app.EvaluateAsync(_app.Host.Input.Down).ConfigureAwait(false);
    }
    public async Task UpAsync()
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseUp");
        await _app.EvaluateAsync(_app.Host.Input.Up).ConfigureAwait(false);
    }
    public async Task ClickAsync(float x, float y)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseClick");
        await MoveAsync(x, y).ConfigureAwait(false);
        await DownAsync().ConfigureAwait(false);
        await UpAsync().ConfigureAwait(false);
        await _app.Host.CaptureAsync().ConfigureAwait(false);
        await _app.SlowAsync().ConfigureAwait(false);
    }
    public async Task WheelAsync(float deltaX, float deltaY)
    {
        await using var step = Recording.PlayTestRecording.Step(_app, "MouseWheel");
        if (deltaX != 0) throw new NotSupportedException("PlayTest 0.1 supports vertical scrolling only.");
        await _app.EvaluateAsync(() => _app.Host.Input.Wheel(-(int)deltaY)).ConfigureAwait(false);
    }
}
