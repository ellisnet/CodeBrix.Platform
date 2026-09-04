#nullable enable

using System;

namespace CodeBrix.Platform.UI.Toolkit;

/// <summary>
/// Provides data for the <see cref="TriPaneView.DividerDragCompleted"/> event, which is raised when
/// the user finishes interacting with one of the two dividers of a <see cref="TriPaneView"/>.
/// </summary>
/// <remarks>
/// The new proportions are read from the control itself - <see cref="TriPaneView.SidePanePercent"/>,
/// <see cref="TriPaneView.StackPercent"/>, <see cref="TriPaneView.UpperPanePercent"/> and
/// <see cref="TriPaneView.LowerPanePercent"/> - which have already been written back, normalized to
/// sum to 100 on the affected axis, by the time this event is raised. That makes the event the
/// natural hook for persisting a layout the user has arranged.
/// </remarks>
public sealed class TriPaneViewDividerDragCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TriPaneViewDividerDragCompletedEventArgs"/>
	/// class.
	/// </summary>
	/// <param name="divider">The divider the user finished interacting with.</param>
	public TriPaneViewDividerDragCompletedEventArgs(TriPaneViewDividerKind divider) => Divider = divider;

	/// <summary>
	/// Gets the divider the user finished interacting with.
	/// </summary>
	public TriPaneViewDividerKind Divider { get; }
}
