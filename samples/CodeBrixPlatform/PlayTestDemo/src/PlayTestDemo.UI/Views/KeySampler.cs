using System;
using System.Threading;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace PlayTestDemo.Views;

/// <summary>A focusable surface that reads key state the way a game loop does: key events only
/// update a state table, a release becomes visible one dispatcher pass later, and a timer samples
/// the table once per tick. A key pressed and released between two ticks is never seen.</summary>
public sealed class KeySampler : Grid
{
    private readonly int[] _state = new int[256];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(15) };
    private int _samplesDown;

    public int SamplesDown => Volatile.Read(ref _samplesDown);
    public int KeyUps { get; private set; }
    public event EventHandler Sampled;

    public KeySampler()
    {
        IsTabStop = true;
        Background = new SolidColorBrush(Colors.SlateGray);
        Children.Add(new TextBlock { Text = "Key sampler", Margin = new Thickness(12), Foreground = new SolidColorBrush(Colors.White) });
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        // Marked handled so the press keeps the focus it just gave this surface.
        PointerPressed += (_, e) =>
        {
            Focus(FocusState.Programmatic);
            e.Handled = true;
        };
        _timer.Tick += (_, _) =>
        {
            if (IsDown(VirtualKey.Right)) Interlocked.Increment(ref _samplesDown);
            Sampled?.Invoke(this, EventArgs.Empty);
        };
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
    }

    public bool IsDown(VirtualKey key) => (int)key < _state.Length && Volatile.Read(ref _state[(int)key]) != 0;

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if ((int)e.Key < _state.Length) Volatile.Write(ref _state[(int)e.Key], 1);
        e.Handled = true;
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        KeyUps++;
        var key = (int)e.Key;
        if (key >= _state.Length) return;
        Volatile.Write(ref _state[key], 2);
        DispatcherQueue.TryEnqueue(() =>
        {
            if (Volatile.Read(ref _state[key]) == 2) Volatile.Write(ref _state[key], 0);
        });
        e.Handled = true;
    }
}
