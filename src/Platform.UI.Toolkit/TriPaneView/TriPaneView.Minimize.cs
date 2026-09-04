#nullable enable

using CodeBrix.Platform.UI.Toolkit.Internal;

namespace CodeBrix.Platform.UI.Toolkit;

public sealed partial class TriPaneView
{
	private double? _sideSnapshot;
	private double? _stackSnapshot;
	private double? _upperSnapshot;
	private double? _lowerSnapshot;

	private TriPaneViewMinimizeCause? _sideCause;
	private TriPaneViewMinimizeCause? _stackCause;
	private TriPaneViewMinimizeCause? _upperCause;
	private TriPaneViewMinimizeCause? _lowerCause;

	private bool _isBatchUpdating;
	private bool _isSyncingMinimizedFlags;
	private int _stateVersion;

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
	public void MinimizeSidePane()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);
		var isUpperOpen = !isStackMinimized && !TriPaneViewLayoutMath.IsMinimized(upperWeight);
		var isLowerOpen = !isStackMinimized && !TriPaneViewLayoutMath.IsMinimized(lowerWeight);

		if (!isUpperOpen && !isLowerOpen)
		{
			UpdateState();

			return;
		}

		if (!TriPaneViewLayoutMath.IsMinimized(sideWeight))
		{
			_sideSnapshot = SidePanePercent;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			SidePanePercent = 0d;
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;

			//The cause belongs to THIS region and nothing else: a code minimize here must not turn
			//off the restore grip of a pane the user dragged shut.
			_sideCause = TriPaneViewMinimizeCause.Code;
			UpdateState();
		}
	}

	/// <summary>
	/// Restores the side pane to the width weight it had when it was minimized, or to the default
	/// weight when there is no snapshot to go back to. Does nothing when the pane is already open.
	/// </summary>
	/// <remarks>
	/// The pane's content element never left the visual tree, so this shows the very same instance
	/// - with its scroll position, its text and its selection - that was there before.
	/// </remarks>
	public void RestoreSidePane()
	{
		var (sideWeight, _) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);

		if (!TriPaneViewLayoutMath.IsMinimized(sideWeight))
		{
			UpdateState();

			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			SidePanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
				_sideSnapshot,
				TriPaneViewLayoutMath.DefaultSidePanePercent);
			_sideSnapshot = null;
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
			UpdateState();
		}
	}

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
	public void MinimizeUpperPane()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);
		var isSideOpen = !TriPaneViewLayoutMath.IsMinimized(sideWeight);
		var isLowerOpen = !isStackMinimized && !TriPaneViewLayoutMath.IsMinimized(lowerWeight);

		if (!isSideOpen && !isLowerOpen)
		{
			UpdateState();

			return;
		}

		if (!TriPaneViewLayoutMath.IsMinimized(upperWeight))
		{
			_upperSnapshot = UpperPanePercent;
		}

		var wasBatchUpdating = _isBatchUpdating;
		var didMinimizeStack = false;
		_isBatchUpdating = true;

		try
		{
			UpperPanePercent = 0d;
			didMinimizeStack = MinimizeStackIfBothPanesAreZero(isStackMinimized);
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;

			//Only the regions this call actually minimized get the code cause; every other
			//minimized region keeps the cause it already had.
			_upperCause = TriPaneViewMinimizeCause.Code;

			if (didMinimizeStack)
			{
				_stackCause = TriPaneViewMinimizeCause.Code;
			}

			UpdateState();
		}
	}

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
	public void RestoreUpperPane()
	{
		var (_, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (upperWeight, _) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);

		if (!isStackMinimized && !TriPaneViewLayoutMath.IsMinimized(upperWeight))
		{
			UpdateState();

			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			RestoreStackWeight(isStackMinimized);

			if (TriPaneViewLayoutMath.SanitizeWeight(UpperPanePercent) <= 0d)
			{
				UpperPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_upperSnapshot,
					TriPaneViewLayoutMath.DefaultUpperPanePercent);
				_upperSnapshot = null;
			}
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
			UpdateState();
		}
	}

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
	public void MinimizeLowerPane()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);
		var isSideOpen = !TriPaneViewLayoutMath.IsMinimized(sideWeight);
		var isUpperOpen = !isStackMinimized && !TriPaneViewLayoutMath.IsMinimized(upperWeight);

		if (!isSideOpen && !isUpperOpen)
		{
			UpdateState();

			return;
		}

		if (!TriPaneViewLayoutMath.IsMinimized(lowerWeight))
		{
			_lowerSnapshot = LowerPanePercent;
		}

		var wasBatchUpdating = _isBatchUpdating;
		var didMinimizeStack = false;
		_isBatchUpdating = true;

		try
		{
			LowerPanePercent = 0d;
			didMinimizeStack = MinimizeStackIfBothPanesAreZero(isStackMinimized);
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;

			//Only the regions this call actually minimized get the code cause; every other
			//minimized region keeps the cause it already had.
			_lowerCause = TriPaneViewMinimizeCause.Code;

			if (didMinimizeStack)
			{
				_stackCause = TriPaneViewMinimizeCause.Code;
			}

			UpdateState();
		}
	}

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
	public void RestoreLowerPane()
	{
		var (_, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (_, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);

		if (!isStackMinimized && !TriPaneViewLayoutMath.IsMinimized(lowerWeight))
		{
			UpdateState();

			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			RestoreStackWeight(isStackMinimized);

			if (TriPaneViewLayoutMath.SanitizeWeight(LowerPanePercent) <= 0d)
			{
				LowerPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_lowerSnapshot,
					TriPaneViewLayoutMath.DefaultLowerPanePercent);
				_lowerSnapshot = null;
			}
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
			UpdateState();
		}
	}

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
	public void RestoreAll()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);
		var isSideMinimized = TriPaneViewLayoutMath.IsMinimized(sideWeight);
		var isUpperMinimized = isStackMinimized || TriPaneViewLayoutMath.IsMinimized(upperWeight);
		var isLowerMinimized = isStackMinimized || TriPaneViewLayoutMath.IsMinimized(lowerWeight);
		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			if (isSideMinimized)
			{
				SidePanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_sideSnapshot,
					TriPaneViewLayoutMath.DefaultSidePanePercent);
				_sideSnapshot = null;
			}

			if (isStackMinimized)
			{
				StackPercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_stackSnapshot,
					TriPaneViewLayoutMath.DefaultStackPercent);
				_stackSnapshot = null;
			}

			if (isUpperMinimized && TriPaneViewLayoutMath.SanitizeWeight(UpperPanePercent) <= 0d)
			{
				UpperPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_upperSnapshot,
					TriPaneViewLayoutMath.DefaultUpperPanePercent);
				_upperSnapshot = null;
			}

			if (isLowerMinimized && TriPaneViewLayoutMath.SanitizeWeight(LowerPanePercent) <= 0d)
			{
				LowerPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_lowerSnapshot,
					TriPaneViewLayoutMath.DefaultLowerPanePercent);
				_lowerSnapshot = null;
			}
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
			UpdateState();
		}
	}

	/// <summary>
	/// Restores the stack - the column holding the upper and lower panes - to the width weight it
	/// had when it was minimized. When both stack panes are also at zero they are restored with it,
	/// so the stack comes back usable. Does nothing when the stack is already open.
	/// </summary>
	internal void RestoreStack()
	{
		var (_, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);

		if (!TriPaneViewLayoutMath.IsMinimized(stackWeight))
		{
			UpdateState();

			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			RestoreStackWeight(true);

			if (TriPaneViewLayoutMath.SanitizeWeight(UpperPanePercent) <= 0d
				&& TriPaneViewLayoutMath.SanitizeWeight(LowerPanePercent) <= 0d)
			{
				UpperPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_upperSnapshot,
					TriPaneViewLayoutMath.DefaultUpperPanePercent);
				LowerPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_lowerSnapshot,
					TriPaneViewLayoutMath.DefaultLowerPanePercent);
				_upperSnapshot = null;
				_lowerSnapshot = null;
			}
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
			UpdateState();
		}
	}

	/// <summary>
	/// Reads a minimize cause that may not have been recorded yet - a weight that arrived at zero
	/// straight from XAML, for instance - and falls back to
	/// <see cref="TriPaneViewMinimizeCause.Drag"/>, the cause a developer-set zero is given.
	/// </summary>
	/// <param name="cause">The recorded cause, if there is one.</param>
	/// <returns>The cause to reason with.</returns>
	private static TriPaneViewMinimizeCause CauseOrDefault(TriPaneViewMinimizeCause? cause)
		=> cause ?? TriPaneViewMinimizeCause.Drag;

	/// <summary>
	/// Collapses the whole stack once both of its panes have reached zero.
	/// </summary>
	/// <param name="wasStackAlreadyMinimized">Whether the stack was already minimized when the call began.</param>
	/// <returns>
	/// <see langword="true"/> when this call is what minimized the stack, so the caller knows whether
	/// the stack's minimize cause is its to stamp.
	/// </returns>
	private bool MinimizeStackIfBothPanesAreZero(bool wasStackAlreadyMinimized)
	{
		if (wasStackAlreadyMinimized
			|| TriPaneViewLayoutMath.SanitizeWeight(UpperPanePercent) > 0d
			|| TriPaneViewLayoutMath.SanitizeWeight(LowerPanePercent) > 0d)
		{
			return false;
		}

		_stackSnapshot = StackPercent;
		StackPercent = 0d;

		return true;
	}

	private void RestoreStackWeight(bool isStackMinimized)
	{
		if (!isStackMinimized)
		{
			return;
		}

		StackPercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
			_stackSnapshot,
			TriPaneViewLayoutMath.DefaultStackPercent);
		_stackSnapshot = null;
	}

	/// <summary>
	/// Keeps one region's minimize cause up to date: a region that is not minimized has no cause, a
	/// region that has just become minimized without a recorded cause was zeroed by a drag or by a
	/// developer-set zero, and a region that already had a cause keeps it.
	/// </summary>
	/// <param name="current">The cause recorded for the region so far, if there is one.</param>
	/// <param name="isMinimized">Whether the region's own raw weight is zero.</param>
	/// <returns>The cause the region should carry now.</returns>
	private static TriPaneViewMinimizeCause? ResolveCause(TriPaneViewMinimizeCause? current, bool isMinimized)
		=> isMinimized ? current ?? TriPaneViewMinimizeCause.Drag : null;

	private void OnWeightChanged() => UpdateState();

	/// <summary>
	/// Publishes one state pass's minimized flags.
	/// </summary>
	/// <param name="version">The number of the state pass these values were computed by.</param>
	/// <param name="isSideMinimized">Whether the side pane is minimized.</param>
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
	private void SyncMinimizedFlags(int version, bool isSideMinimized, bool isUpperMinimized, bool isLowerMinimized)
	{
		var wasSyncing = _isSyncingMinimizedFlags;
		_isSyncingMinimizedFlags = true;

		try
		{
			SetValue(IsSidePaneMinimizedProperty, isSideMinimized);

			if (_stateVersion != version)
			{
				return;
			}

			SetValue(IsUpperPaneMinimizedProperty, isUpperMinimized);

			if (_stateVersion != version)
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

		switch (region)
		{
			case TriPaneViewRegion.Side when isMinimized:
				MinimizeSidePane();
				break;
			case TriPaneViewRegion.Side:
				RestoreSidePane();
				break;
			case TriPaneViewRegion.Upper when isMinimized:
				MinimizeUpperPane();
				break;
			case TriPaneViewRegion.Upper:
				RestoreUpperPane();
				break;
			case TriPaneViewRegion.Lower when isMinimized:
				MinimizeLowerPane();
				break;
			case TriPaneViewRegion.Lower:
				RestoreLowerPane();
				break;
			default:
				UpdateState();
				break;
		}
	}
}
