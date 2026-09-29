#nullable enable

using System;

namespace CodeBrix.Platform.UI.TerminalView.Engine;

/// <summary>What a touch contact turned out to be, as far as it has gone (<see cref="TerminalTouchGesture"/>).</summary>
internal enum TerminalTouchState
{
	/// <summary>No finger is down.</summary>
	None = 0,

	/// <summary>A finger is down and has not yet moved far enough to say what it is doing.</summary>
	Pending,

	/// <summary>The finger is scrolling the history (a mainly vertical drag that began at once).</summary>
	Scrolling,

	/// <summary>The finger is extending a selection (a mainly horizontal drag, or any drag after a long press).</summary>
	Selecting,
}

/// <summary>What the host does when a finger lifts (<see cref="TerminalTouchGesture.Release"/>).</summary>
internal enum TerminalTouchOutcome
{
	/// <summary>Nothing more to do.</summary>
	None = 0,

	/// <summary>A short tap: handled like a click (it clears the selection; a quick second tap selects the word).</summary>
	Tapped,

	/// <summary>A long press that did not move: the host opens its Copy/Paste menu at the lift point.</summary>
	ContextMenu,

	/// <summary>A selection drag ended (the host releases its pointer capture and stops its edge auto-scroll).</summary>
	SelectionEnded,

	/// <summary>A scroll drag ended.</summary>
	ScrollEnded,
}

/// <summary>
/// The terminal's one-finger touch gestures (WPE1-21), on the engine so they run without XAML:
/// <list type="bullet">
/// <item>a drag that sets off mainly VERTICALLY at once scrolls the history, one line per cell height the finger
/// travels (down = back into history, the way content follows a finger);</item>
/// <item>a drag that sets off mainly HORIZONTALLY at once selects, as a finger drag always did;</item>
/// <item>a long press (<see cref="LongPressMilliseconds"/> before the finger moves past <see cref="SlopDips"/>) then a
/// drag in any direction selects from where the finger went down;</item>
/// <item>a long press without a drag opens the Copy/Paste menu where the finger lifts (the existing selection stays,
/// so it can be copied);</item>
/// <item>a short tap is a click.</item>
/// </list>
/// The long press is judged from the timestamps of the contact's own events, so no timer is needed and a test
/// can drive it with explicit times. Mouse and pen input keep the renderer's press/drag path.
/// </summary>
internal sealed class TerminalTouchGesture
{
	/// <summary>How long a finger rests before a drag selects instead of scrolling (and a lift opens the menu).</summary>
	internal const long LongPressMilliseconds = 500;

	/// <summary>How far (DIPs, either axis) a finger may wander before it counts as a drag.</summary>
	internal const double SlopDips = 8;

	private readonly TerminalRenderer _renderer;
	private TerminalTouchState _state;
	private double _originX;
	private double _originY;
	private long _pressTick;
	private double _scrollAnchorY;

	/// <summary>Creates the gesture recognizer for a renderer.</summary>
	/// <param name="renderer">The renderer it scrolls and selects on.</param>
	internal TerminalTouchGesture(TerminalRenderer renderer) => _renderer = renderer;

	/// <summary>What the current contact is doing.</summary>
	internal TerminalTouchState State => _state;

	/// <summary>A finger went down.</summary>
	/// <param name="x">The X coordinate in DIPs.</param>
	/// <param name="y">The Y coordinate in DIPs.</param>
	/// <param name="tickMilliseconds">The time (a monotonic millisecond clock).</param>
	internal void Press(double x, double y, long tickMilliseconds)
	{
		if (_state == TerminalTouchState.Selecting) { _renderer.EndDrag(); }

		_state = TerminalTouchState.Pending;
		_originX = x;
		_originY = y;
		_pressTick = tickMilliseconds;
	}

	/// <summary>The finger moved. Returns the state afterwards.</summary>
	/// <param name="x">The X coordinate in DIPs (may lie outside the surface).</param>
	/// <param name="y">The Y coordinate in DIPs (may lie outside the surface).</param>
	/// <param name="tickMilliseconds">The time (a monotonic millisecond clock).</param>
	/// <returns>The state after the move.</returns>
	internal TerminalTouchState Move(double x, double y, long tickMilliseconds)
	{
		switch (_state)
		{
			case TerminalTouchState.Pending:
				var dx = x - _originX;
				var dy = y - _originY;
				if (Math.Abs(dx) < SlopDips && Math.Abs(dy) < SlopDips) { break; }

				if (tickMilliseconds - _pressTick >= LongPressMilliseconds || Math.Abs(dx) > Math.Abs(dy))
				{
					_state = TerminalTouchState.Selecting;
					_renderer.StartSelectionAt(_originX, _originY);
					_renderer.DragTo(x, y);
				}
				else
				{
					_state = TerminalTouchState.Scrolling;
					_scrollAnchorY = _originY;
					ScrollTo(y);
				}
				break;

			case TerminalTouchState.Scrolling:
				ScrollTo(y);
				break;

			case TerminalTouchState.Selecting:
				_renderer.DragTo(x, y);
				break;
		}

		return _state;
	}

	/// <summary>The finger lifted. Returns what the host does next.</summary>
	/// <param name="x">The X coordinate in DIPs.</param>
	/// <param name="y">The Y coordinate in DIPs.</param>
	/// <param name="tickMilliseconds">The time (a monotonic millisecond clock).</param>
	/// <returns>The outcome.</returns>
	internal TerminalTouchOutcome Release(double x, double y, long tickMilliseconds)
	{
		var state = _state;
		_state = TerminalTouchState.None;

		switch (state)
		{
			case TerminalTouchState.Pending:
				if (tickMilliseconds - _pressTick >= LongPressMilliseconds) { return TerminalTouchOutcome.ContextMenu; }

				//A tap is a click: the press, the release point, the end of the (empty) drag
				if (_renderer.PressAt(_originX, _originY, _pressTick))
				{
					_renderer.DragTo(x, y);
					_renderer.EndDrag();
				}
				return TerminalTouchOutcome.Tapped;

			case TerminalTouchState.Scrolling:
				ScrollTo(y);
				return TerminalTouchOutcome.ScrollEnded;

			case TerminalTouchState.Selecting:
				//A touch can lift beyond its last move, so the selection runs to the lift point
				_renderer.DragTo(x, y);
				_renderer.EndDrag();
				return TerminalTouchOutcome.SelectionEnded;

			default:
				return TerminalTouchOutcome.None;
		}
	}

	/// <summary>The contact was lost (capture lost, pointer cancelled): a selection in progress ends where it is.</summary>
	internal void Cancel()
	{
		if (_state == TerminalTouchState.Selecting) { _renderer.EndDrag(); }
		_state = TerminalTouchState.None;
	}

	/// <summary>Scrolls by the whole lines the finger has travelled since the last scroll step.</summary>
	/// <param name="y">The finger's Y coordinate in DIPs.</param>
	private void ScrollTo(double y)
	{
		var lineHeight = _renderer.Metrics.Height;
		if (lineHeight <= 0) { return; }

		var lines = (int)((y - _scrollAnchorY) / lineHeight);
		if (lines == 0) { return; }

		//The content follows the finger: dragging down brings older lines into view (back into history)
		_renderer.ScrollLines(-lines);
		_scrollAnchorY += lines * lineHeight;
	}
}
