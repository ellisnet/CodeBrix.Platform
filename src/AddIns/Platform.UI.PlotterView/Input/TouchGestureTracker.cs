#nullable enable

using System.Collections.Generic;
using CodeBrix.Plotter;

namespace CodeBrix.Platform.UI.PlotterView.Input;

/// <summary>
/// Tracks the active touch contacts on the plot and snapshots their positions, in a stable
/// order, before and after each move - the two arrays
/// <see cref="PlotterTouchEventArgs(ScreenPoint[], ScreenPoint[])"/> needs to derive pan
/// translation (one finger) and pinch scale (two fingers) itself.
/// </summary>
/// <remarks>
/// Contacts are keyed by the framework's pointer id and kept in press order, so "the first
/// two fingers down" are the pinch pair regardless of event interleaving. When a contact
/// appears or disappears the before/after arrays differ in length, which the event-args
/// constructor treats as "no delta" - so a joining or lifting finger never causes a jump.
/// </remarks>
public sealed class TouchGestureTracker
{
    private readonly List<uint> _order = new();
    private readonly Dictionary<uint, ScreenPoint> _positions = new();

    /// <summary>The number of active touch contacts.</summary>
    public int Count => _order.Count;

    /// <summary>
    /// Registers a new touch contact.
    /// </summary>
    /// <param name="pointerId">The framework pointer id.</param>
    /// <param name="position">The contact position.</param>
    /// <returns><c>true</c> when this is the first contact of a gesture; <c>false</c> for
    /// fingers joining an existing gesture (or a duplicate press for an id already down,
    /// which just updates its position).</returns>
    public bool Down(uint pointerId, ScreenPoint position)
    {
        if (!_positions.ContainsKey(pointerId))
        {
            _order.Add(pointerId);
        }

        _positions[pointerId] = position;
        return _order.Count == 1;
    }

    /// <summary>
    /// Moves a touch contact, snapshotting the contact positions before and after.
    /// </summary>
    /// <param name="pointerId">The framework pointer id.</param>
    /// <param name="position">The new contact position.</param>
    /// <param name="currentTouches">The contact positions after the move, in press order.</param>
    /// <param name="previousTouches">The contact positions before the move, in press order.</param>
    /// <returns><c>false</c> when the id is not a tracked contact (a hover, or a contact
    /// already lifted), in which case both arrays are empty.</returns>
    public bool Move(uint pointerId, ScreenPoint position,
        out ScreenPoint[] currentTouches, out ScreenPoint[] previousTouches)
    {
        if (!_positions.ContainsKey(pointerId))
        {
            currentTouches = [];
            previousTouches = [];
            return false;
        }

        previousTouches = Snapshot();
        _positions[pointerId] = position;
        currentTouches = Snapshot();
        return true;
    }

    /// <summary>
    /// Removes a touch contact.
    /// </summary>
    /// <param name="pointerId">The framework pointer id.</param>
    /// <returns><c>true</c> when this was the last contact, ending the gesture.</returns>
    public bool Up(uint pointerId)
    {
        if (_positions.Remove(pointerId))
        {
            _order.Remove(pointerId);
        }

        return _order.Count == 0;
    }

    /// <summary>
    /// Forgets every contact (a canceled gesture).
    /// </summary>
    public void Clear()
    {
        _order.Clear();
        _positions.Clear();
    }

    /// <summary>
    /// The current contact positions in press order.
    /// </summary>
    /// <returns>The positions array.</returns>
    public ScreenPoint[] Snapshot()
    {
        var points = new ScreenPoint[_order.Count];
        for (var i = 0; i < _order.Count; i++)
        {
            points[i] = _positions[_order[i]];
        }

        return points;
    }
}
