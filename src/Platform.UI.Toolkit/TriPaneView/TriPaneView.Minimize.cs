#nullable enable

using CodeBrix.Platform.UI.Toolkit.Engine;
using CodeBrix.Platform.UI.Toolkit.Internal;

namespace CodeBrix.Platform.UI.Toolkit;

public sealed partial class TriPaneView
{
	//WPE1 C12: the minimize/restore state machine (snapshots, causes, the batch guard, the state-pass numbers) moved
	//into the WinUI-free engine (Engine/TriPaneLayoutState); these public members forward to it, and this control keeps
	//the dependency-property half (the minimized flags it publishes, LayoutHost below).
	private bool _isSyncingMinimizedFlags;

	/// <summary>
	/// Minimizes the side pane: its width weight is snapshotted and set to zero, so the pane
	/// collapses to nothing while its content element stays in the visual tree. The very same
	/// instance, with all of its state, is shown again by <see cref="RestoreSidePane"/>.
	/// </summary>
	/// <remarks>
	/// A request that would leave no pane open at all is ignored and the control's state is left
	/// exactly as it was. Because this is a request from code, no restore grip is offered while
	/// <see cref="RestoreGripMode"/> is <see cref="TriPaneViewRestoreGripMode.Auto"/>; calling this
	/// on a pane the user had already dragged shut turns its grip off for the same reason.
	/// </remarks>
	public void MinimizeSidePane() => State.MinimizeSidePane();

	/// <summary>
	/// Restores the side pane to the width weight it had when it was minimized, or to the default
	/// weight when there is no snapshot to go back to. Does nothing when the pane is already open.
	/// </summary>
	/// <remarks>
	/// The pane's content element never left the visual tree, so this shows the very same instance
	/// - with its scroll position, its text and its selection - that was there before.
	/// </remarks>
	public void RestoreSidePane() => State.RestoreSidePane();

	/// <summary>
	/// Minimizes the upper pane: its height weight is snapshotted and set to zero, so the pane
	/// collapses to nothing while its content element stays in the visual tree. The very same
	/// instance, with all of its state, is shown again by <see cref="RestoreUpperPane"/>.
	/// </summary>
	/// <remarks>
	/// Minimizing whichever of the upper and lower panes is the second to go also snapshots and
	/// zeroes <see cref="StackPercent"/>, so the whole stack collapses and the side pane takes the
	/// control. A request that would leave no pane open at all is ignored and the control's state is
	/// left exactly as it was. Because this is a request from code, no restore grip is offered while
	/// <see cref="RestoreGripMode"/> is <see cref="TriPaneViewRestoreGripMode.Auto"/>.
	/// </remarks>
	public void MinimizeUpperPane() => State.MinimizeUpperPane();

	/// <summary>
	/// Restores the upper pane to the height weight it had when it was minimized, or to the default
	/// weight when there is no snapshot to go back to. Does nothing when the pane is already open.
	/// </summary>
	/// <remarks>
	/// Restoring the upper pane while the whole stack is minimized brings the stack back as well,
	/// and leaves the lower pane minimized if that is where it was - its own snapshot is kept for a
	/// later <see cref="RestoreLowerPane"/>. The pane's content element never left the visual tree,
	/// so this shows the very same instance that was there before.
	/// </remarks>
	public void RestoreUpperPane() => State.RestoreUpperPane();

	/// <summary>
	/// Minimizes the lower pane: its height weight is snapshotted and set to zero, so the pane
	/// collapses to nothing while its content element stays in the visual tree. The very same
	/// instance, with all of its state, is shown again by <see cref="RestoreLowerPane"/>.
	/// </summary>
	/// <remarks>
	/// Minimizing whichever of the upper and lower panes is the second to go also snapshots and
	/// zeroes <see cref="StackPercent"/>, so the whole stack collapses and the side pane takes the
	/// control. A request that would leave no pane open at all is ignored and the control's state is
	/// left exactly as it was. Because this is a request from code, no restore grip is offered while
	/// <see cref="RestoreGripMode"/> is <see cref="TriPaneViewRestoreGripMode.Auto"/>.
	/// </remarks>
	public void MinimizeLowerPane() => State.MinimizeLowerPane();

	/// <summary>
	/// Restores the lower pane to the height weight it had when it was minimized, or to the default
	/// weight when there is no snapshot to go back to. Does nothing when the pane is already open.
	/// </summary>
	/// <remarks>
	/// Restoring the lower pane while the whole stack is minimized brings the stack back as well,
	/// and leaves the upper pane minimized if that is where it was - its own snapshot is kept for a
	/// later <see cref="RestoreUpperPane"/>. The pane's content element never left the visual tree,
	/// so this shows the very same instance that was there before.
	/// </remarks>
	public void RestoreLowerPane() => State.RestoreLowerPane();

	/// <summary>
	/// Restores every minimized region at once, each to the weight it had when it was minimized or
	/// to its default weight when there is no snapshot to go back to.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only regions that are actually minimized are touched, and "minimized" means exactly what
	/// <see cref="IsSidePaneMinimized"/>, <see cref="IsUpperPaneMinimized"/> and
	/// <see cref="IsLowerPaneMinimized"/> report. A pair of weights that are BOTH zero is laid out
	/// evenly and minimizes neither member, so a deliberate <c>0</c> / <c>0</c> pair is left exactly
	/// as it was set rather than being re-proportioned to the default weights. A pane whose weight is
	/// still positive but which is minimized only because the whole stack is collapsed keeps that
	/// weight too, so restoring the stack brings the panes back in the proportion they had.
	/// </para>
	/// <para>
	/// No pane content is ever detached while minimized, so this shows the very same element
	/// instances - with all of their state - that were there before.
	/// </para>
	/// </remarks>
	public void RestoreAll() => State.RestoreAll();

	/// <summary>
	/// Minimizes the stack - the whole region holding the upper and lower panes: its width weight is
	/// snapshotted and set to zero, so both panes collapse together and the side pane takes the
	/// control. Neither pane's content element leaves the visual tree, and
	/// <see cref="RestoreStack"/> brings the very same instances back.
	/// </summary>
	/// <remarks>
	/// A request that would leave no pane open at all - the side pane is minimized too - is ignored
	/// and the control's state is left exactly as it was. Because this is a request from code, no
	/// restore grip is offered while <see cref="RestoreGripMode"/> is
	/// <see cref="TriPaneViewRestoreGripMode.Auto"/>; <see cref="RestoreStack"/> is then the only way
	/// back, and it works whatever collapsed the stack.
	/// </remarks>
	public void MinimizeStack() => State.MinimizeStack();

	/// <summary>
	/// Restores the stack - the region holding the upper and lower panes - to the width weight it
	/// had when it was minimized, or to the default weight when there is no snapshot to go back to.
	/// When both stack panes are also at zero they are restored with it, so the stack comes back
	/// usable. Does nothing when the stack is already open.
	/// </summary>
	/// <remarks>
	/// This is the counterpart of <see cref="MinimizeStack"/> and it does not care what collapsed
	/// the stack: a stack the user dragged shut and a stack code shut both come back through here.
	/// No pane content is ever detached while the stack is minimized, so this shows the very same
	/// element instances that were there before.
	/// </remarks>
	public void RestoreStack() => State.RestoreStack();

	private void OnWeightChanged() => State.OnWeightChanged();

	/// <summary>
	/// Publishes one state pass's minimized flags.
	/// </summary>
	/// <param name="version">The number of the state pass these values were computed by.</param>
	/// <param name="isSideMinimized">Whether the side pane is minimized.</param>
	/// <param name="isStackMinimized">Whether the whole stack is minimized.</param>
	/// <param name="isUpperMinimized">Whether the upper pane is minimized.</param>
	/// <param name="isLowerMinimized">Whether the lower pane is minimized.</param>
	/// <remarks>
	/// The three flags are meant to be bound two way, so a source setter reached through a binding
	/// can run arbitrary code - including code that comes straight back through here - between the
	/// writes below. Two things follow. The guard is saved and restored rather than assigned, so a
	/// nested pass cannot clear it on its way out and leave the remaining writes looking like
	/// external commands; and the pass number is re-checked between the writes, so once a newer pass
	/// has published a newer state this one abandons the values it computed before.
	/// </remarks>
	private void SyncMinimizedFlags(
		int version,
		bool isSideMinimized,
		bool isStackMinimized,
		bool isUpperMinimized,
		bool isLowerMinimized)
	{
		var wasSyncing = _isSyncingMinimizedFlags;
		_isSyncingMinimizedFlags = true;

		try
		{
			//The stack flag is read-only, so nothing it runs can come back through a setter of ours;
			//it is published first so a handler of one of the three settable flags below already
			//sees it.
			SetValue(IsStackMinimizedProperty, isStackMinimized);
			SetValue(IsSidePaneMinimizedProperty, isSideMinimized);

			if (State.StateVersion != version)
			{
				return;
			}

			SetValue(IsUpperPaneMinimizedProperty, isUpperMinimized);

			if (State.StateVersion != version)
			{
				return;
			}

			SetValue(IsLowerPaneMinimizedProperty, isLowerMinimized);
		}
		finally
		{
			_isSyncingMinimizedFlags = wasSyncing;
		}
	}

	private void OnMinimizedFlagChanged(TriPaneViewRegion region, bool isMinimized)
	{
		if (_isSyncingMinimizedFlags)
		{
			return;
		}

		State.SetMinimized(region, isMinimized);
	}

	/// <summary>
	/// The engine's host (WPE1 C12): the four weights and the settings the pane logic reads are this control's
	/// dependency properties, and a state pass's results go to <see cref="SyncMinimizedFlags"/> and the template.
	/// </summary>
	private sealed class LayoutHost : ITriPaneLayoutHost
	{
		private readonly TriPaneView _owner;

		internal LayoutHost(TriPaneView owner) => _owner = owner;

		public double SidePanePercent { get => _owner.SidePanePercent; set => _owner.SidePanePercent = value; }

		public double StackPercent { get => _owner.StackPercent; set => _owner.StackPercent = value; }

		public double UpperPanePercent { get => _owner.UpperPanePercent; set => _owner.UpperPanePercent = value; }

		public double LowerPanePercent { get => _owner.LowerPanePercent; set => _owner.LowerPanePercent = value; }

		public TriPaneViewSidePanePlacement SidePanePlacement => _owner.SidePanePlacement;

		public TriPaneViewRestoreGripMode RestoreGripMode => _owner.RestoreGripMode;

		public bool IsDragToMinimizeEnabled => _owner.IsDragToMinimizeEnabled;

		public double SidePaneMinLength => _owner.SidePaneMinLength;

		public double StackMinLength => _owner.StackMinLength;

		public double UpperPaneMinLength => _owner.UpperPaneMinLength;

		public double LowerPaneMinLength => _owner.LowerPaneMinLength;

		public void SyncMinimizedFlags(int version, bool isSideMinimized, bool isStackMinimized, bool isUpperMinimized, bool isLowerMinimized)
			=> _owner.SyncMinimizedFlags(version, isSideMinimized, isStackMinimized, isUpperMinimized, isLowerMinimized);

		public void ApplyLayout(TriPaneLayoutResult layout) => _owner.ApplyLayout(layout);
	}
}
