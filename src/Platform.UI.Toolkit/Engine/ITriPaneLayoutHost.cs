#nullable enable

namespace CodeBrix.Platform.UI.Toolkit.Engine;

/// <summary>
/// What the TriPaneView engine (<see cref="TriPaneLayoutState"/>, WPE1 C12) needs from its host: the four pane weights
/// (read and written), the settings the pane logic reads, and the two outputs of a state pass. On CodeBrix.Platform the
/// host is TriPaneView (its dependency properties; writing a weight runs the engine's
/// <see cref="TriPaneLayoutState.OnWeightChanged"/> from the property callback); a CodeBrix.Mobile view keeps them in
/// plain fields.
/// </summary>
internal interface ITriPaneLayoutHost
{
	/// <summary>The side pane's width weight (percent of the side/stack pair).</summary>
	double SidePanePercent { get; set; }

	/// <summary>The stack's width weight (percent of the side/stack pair).</summary>
	double StackPercent { get; set; }

	/// <summary>The upper pane's height weight (percent of the upper/lower pair).</summary>
	double UpperPanePercent { get; set; }

	/// <summary>The lower pane's height weight (percent of the upper/lower pair).</summary>
	double LowerPanePercent { get; set; }

	/// <summary>Which side the side pane is on.</summary>
	TriPaneViewSidePanePlacement SidePanePlacement { get; }

	/// <summary>When a minimized region offers a restore grip.</summary>
	TriPaneViewRestoreGripMode RestoreGripMode { get; }

	/// <summary>Whether a divider drag may take a pane down to zero.</summary>
	bool IsDragToMinimizeEnabled { get; }

	/// <summary>The side pane's minimum width, in pixels.</summary>
	double SidePaneMinLength { get; }

	/// <summary>The stack's minimum width, in pixels.</summary>
	double StackMinLength { get; }

	/// <summary>The upper pane's minimum height, in pixels.</summary>
	double UpperPaneMinLength { get; }

	/// <summary>The lower pane's minimum height, in pixels.</summary>
	double LowerPaneMinLength { get; }

	/// <summary>
	/// Publishes one state pass's minimized flags. The flags may be bound two way, so publishing can run code that starts
	/// a newer pass: the host re-checks <see cref="TriPaneLayoutState.StateVersion"/> against <paramref name="version"/>
	/// between its writes and stops when a newer pass has taken over.
	/// </summary>
	/// <param name="version">The number of the pass the values belong to.</param>
	/// <param name="isSideMinimized">Whether the side pane is minimized.</param>
	/// <param name="isStackMinimized">Whether the whole stack is minimized.</param>
	/// <param name="isUpperMinimized">Whether the upper pane is minimized.</param>
	/// <param name="isLowerMinimized">Whether the lower pane is minimized.</param>
	void SyncMinimizedFlags(int version, bool isSideMinimized, bool isStackMinimized, bool isUpperMinimized, bool isLowerMinimized);

	/// <summary>Applies a state pass's layout (the host's surface: columns, rows, dividers).</summary>
	/// <param name="layout">The layout.</param>
	void ApplyLayout(TriPaneLayoutResult layout);
}
