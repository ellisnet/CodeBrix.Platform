#nullable enable

using System;

namespace CodeBrix.Platform.UI.PlotterView.Input;

/// <summary>
/// Turns a stream of pointer-press positions and timestamps into click counts, so that
/// double-click plot gestures (reset on double-middle-click, for example) work on heads whose
/// pointer events carry no click count of their own.
/// </summary>
/// <remarks>
/// Presses count as consecutive clicks while each lands within
/// <see cref="MaximumIntervalMilliseconds"/> of the previous one and within
/// <see cref="MaximumDistance"/> of the first press of the run; any other press starts a new
/// run at count 1. The caller supplies timestamps, which keeps the class deterministic under
/// test.
/// </remarks>
public sealed class ClickCounter
{
    private long _lastClickTick;
    private double _runX = double.NaN;
    private double _runY = double.NaN;
    private int _count;

    /// <summary>The maximum milliseconds between presses that still counts as a multi-click.
    /// Default 500, the common desktop double-click time.</summary>
    public int MaximumIntervalMilliseconds { get; set; } = 500;

    /// <summary>The maximum distance in device-independent pixels a press may drift from the
    /// first press of the run. Default 4.</summary>
    public double MaximumDistance { get; set; } = 4.0;

    /// <summary>
    /// Registers a pointer press and returns its click count (1 for a single click, 2 for a
    /// double click, and so on).
    /// </summary>
    /// <param name="timestampMilliseconds">The press time in milliseconds on any steady clock
    /// (<see cref="Environment.TickCount64"/> at the control level).</param>
    /// <param name="x">The press x-coordinate.</param>
    /// <param name="y">The press y-coordinate.</param>
    /// <returns>The click count of this press.</returns>
    public int Register(long timestampMilliseconds, double x, double y)
    {
        var withinTime = _count > 0
            && timestampMilliseconds - _lastClickTick <= MaximumIntervalMilliseconds;
        var withinDistance = Math.Abs(x - _runX) <= MaximumDistance
            && Math.Abs(y - _runY) <= MaximumDistance;

        if (withinTime && withinDistance)
        {
            _count++;
        }
        else
        {
            _count = 1;
            _runX = x;
            _runY = y;
        }

        _lastClickTick = timestampMilliseconds;
        return _count;
    }

    /// <summary>
    /// Forgets the current run, so the next press counts as a fresh single click.
    /// </summary>
    public void Reset()
    {
        _count = 0;
        _lastClickTick = 0;
        _runX = double.NaN;
        _runY = double.NaN;
    }
}
