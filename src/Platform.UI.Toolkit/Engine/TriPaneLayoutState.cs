#nullable enable

using CodeBrix.Platform.UI.Toolkit.Internal;

namespace CodeBrix.Platform.UI.Toolkit.Engine;

/// <summary>
/// The TriPaneView ENGINE (WPE1 C12): the pane logic of the three-pane layout - the minimize/restore state machine with
/// its restore snapshots and minimize causes, the divider-drag resolution (tap-versus-drag, drag-to-minimize, cancel
/// roll-back, restore-by-grip) and the state pass that turns the four weights into effective weights, minimized flags and
/// restore-grip visibility - over TriPaneViewLayoutMath. Names no XAML type: the weights and settings live with the host
/// (<see cref="ITriPaneLayoutHost"/>; TriPaneView keeps them in its dependency properties, a CodeBrix.Mobile view in plain
/// fields), which also publishes the minimized flags and applies the computed layout. Moved verbatim out of
/// TriPaneView(.Minimize).cs; TriPaneView wraps it.
/// </summary>
internal sealed class TriPaneLayoutState
{
	private readonly ITriPaneLayoutHost _host;

	private double? _sideSnapshot;
	private double? _stackSnapshot;
	private double? _upperSnapshot;
	private double? _lowerSnapshot;

	private TriPaneViewMinimizeCause? _sideCause;
	private TriPaneViewMinimizeCause? _stackCause;
	private TriPaneViewMinimizeCause? _upperCause;
	private TriPaneViewMinimizeCause? _lowerCause;

	private bool _isBatchUpdating;
	private int _stateVersion;

	private double _dragStartSidePercent;
	private double _dragStartStackPercent;
	private double _dragStartUpperPercent;
	private double _dragStartLowerPercent;

	private bool _isSideDragActive;
	private bool _sideDragHasMoved;
	private double _sideDragFirstLength;
	private double _sideDragSecondLength;
	private double _sideDragTotalDelta;
	private double _sideDragStartSidePercent;
	private double _sideDragStartStackPercent;
	private double? _sideDragStartSideSnapshot;
	private double? _sideDragStartStackSnapshot;

	private bool _isStackDragActive;
	private bool _stackDragHasMoved;
	private double _stackDragFirstLength;
	private double _stackDragSecondLength;
	private double _stackDragTotalDelta;
	private double _stackDragStartUpperPercent;
	private double _stackDragStartLowerPercent;
	private double? _stackDragStartUpperSnapshot;
	private double? _stackDragStartLowerSnapshot;

	/// <summary>Creates the engine over a host's weights and settings.</summary>
	/// <param name="host">The host.</param>
	internal TriPaneLayoutState(ITriPaneLayoutHost host)
	{
		_host = host;
	}

	/// <summary>The number of the latest state pass (a pass that sees a newer number stands down).</summary>
	internal int StateVersion => _stateVersion;

	/// <summary>Whether a side-divider drag is in progress.</summary>
	internal bool IsSideDragActive => _isSideDragActive;

	/// <summary>Whether a stack-divider drag is in progress.</summary>
	internal bool IsStackDragActive => _isStackDragActive;

	/// <summary>A weight change from outside (a property set): runs a state pass unless a batch update is running.</summary>
	internal void OnWeightChanged() => UpdateState();

	/// <summary>
	/// A minimized flag set from outside (a two-way binding): minimizes or restores that region.
	/// </summary>
	/// <param name="region">The region.</param>
	/// <param name="isMinimized">The flag's new value.</param>
	internal void SetMinimized(TriPaneViewRegion region, bool isMinimized)
	{
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

	/// <summary>
	/// Records the pixel lengths the two panes on a divider's axis had when a drag started. Every
	/// later move of that drag is resolved against these, so the panes cannot drift.
	/// </summary>
	/// <param name="kind">The divider being dragged.</param>
	/// <param name="firstLength">
	/// The length, in pixels, of the pane laid out before the divider - the left or upper one.
	/// </param>
	/// <param name="secondLength">
	/// The length, in pixels, of the pane laid out after the divider - the right or lower one.
	/// </param>
	/// <remarks>
	/// The weights of the two regions on that axis are snapshotted here as well, so a pane the drag
	/// closes reopens at the weight it had before the drag rather than at the default one. A region
	/// that is already at zero keeps the snapshot it already had, which is the weight it was open at.
	/// </remarks>
	internal void StartDividerDrag(TriPaneViewDividerKind kind, double firstLength, double secondLength)
	{
		_dragStartSidePercent = _host.SidePanePercent;
		_dragStartStackPercent = _host.StackPercent;
		_dragStartUpperPercent = _host.UpperPanePercent;
		_dragStartLowerPercent = _host.LowerPanePercent;

		if (kind == TriPaneViewDividerKind.Side)
		{
			_isSideDragActive = true;
			_sideDragHasMoved = false;
			_sideDragFirstLength = firstLength;
			_sideDragSecondLength = secondLength;
			_sideDragTotalDelta = 0d;
			_sideDragStartSidePercent = _host.SidePanePercent;
			_sideDragStartStackPercent = _host.StackPercent;
			_sideDragStartSideSnapshot = _sideSnapshot;
			_sideDragStartStackSnapshot = _stackSnapshot;
			_sideSnapshot = SnapshotWeight(_host.SidePanePercent, _sideSnapshot);
			_stackSnapshot = SnapshotWeight(_host.StackPercent, _stackSnapshot);
		}
		else
		{
			_isStackDragActive = true;
			_stackDragHasMoved = false;
			_stackDragFirstLength = firstLength;
			_stackDragSecondLength = secondLength;
			_stackDragTotalDelta = 0d;
			_stackDragStartUpperPercent = _host.UpperPanePercent;
			_stackDragStartLowerPercent = _host.LowerPanePercent;
			_stackDragStartUpperSnapshot = _upperSnapshot;
			_stackDragStartLowerSnapshot = _lowerSnapshot;
			_upperSnapshot = SnapshotWeight(_host.UpperPanePercent, _upperSnapshot);
			_lowerSnapshot = SnapshotWeight(_host.LowerPanePercent, _lowerSnapshot);
		}
	}

	/// <summary>
	/// Advances a divider drag by the distance the pointer has moved since the previous step, and
	/// writes the resulting weights - normalized to sum to 100 - back to the control.
	/// </summary>
	/// <param name="kind">The divider being dragged.</param>
	/// <param name="delta">
	/// The change, in pixels, since the previous step. Positive values move the divider away from
	/// the first pane.
	/// </param>
	/// <remarks>
	/// Movement that still adds up to less than the tap threshold is not applied at all, so a
	/// slightly shaky click on a restore grip stays a click and does not leave the pane a pixel or
	/// two wide. The suppression is latched to the START of the gesture: once the pointer has passed
	/// the threshold once, every later move is applied, including one that brings the divider back
	/// to where it began.
	/// </remarks>
	internal void UpdateDividerDrag(TriPaneViewDividerKind kind, double delta)
	{
		var moved = double.IsFinite(delta) ? delta : 0d;

		if (kind == TriPaneViewDividerKind.Side)
		{
			if (!_isSideDragActive)
			{
				return;
			}

			_sideDragTotalDelta += moved;

			if (_sideDragHasMoved || !TriPaneViewLayoutMath.IsTap(_sideDragTotalDelta))
			{
				_sideDragHasMoved = true;
				ApplySideDrag();
			}
		}
		else
		{
			if (!_isStackDragActive)
			{
				return;
			}

			_stackDragTotalDelta += moved;

			if (_stackDragHasMoved || !TriPaneViewLayoutMath.IsTap(_stackDragTotalDelta))
			{
				_stackDragHasMoved = true;
				ApplyStackDrag();
			}
		}
	}

	/// <summary>
	/// Ends a divider drag. A drag that barely moved while the divider was acting as a restore grip
	/// counts as a tap and restores the minimized pane; a cancelled drag puts the axis back exactly
	/// where it was when the drag started. Returns true when the host must report the completed drag
	/// (TriPaneView raises DividerDragCompleted), with the weights already written back - only when
	/// the interaction actually changed the layout: a bare click on an ordinary divider, a cancelled
	/// drag, and a drag that ends with all four weights back where they started all report nothing.
	/// </summary>
	/// <param name="kind">The divider that was being dragged.</param>
	/// <param name="totalTravel">The total distance, in pixels, the pointer travelled.</param>
	/// <param name="canceled">Whether the drag was cancelled rather than completed.</param>
	/// <returns>Whether the completed drag is to be reported.</returns>
	/// <remarks>
	/// The tap that restores a pane is a gesture that never passed the drag threshold at all, not
	/// one that happens to END where it began: <paramref name="totalTravel"/> is net displacement,
	/// so a drag taken out and brought back would otherwise read as a tap and reopen the very pane
	/// the user had just dragged shut. The latched "this gesture has moved" flag decides.
	/// </remarks>
	internal bool CompleteDividerDrag(TriPaneViewDividerKind kind, double totalTravel, bool canceled)
	{
		bool wasActive;
		bool hasMoved;

		if (kind == TriPaneViewDividerKind.Side)
		{
			wasActive = _isSideDragActive;
			hasMoved = _sideDragHasMoved;
			_isSideDragActive = false;
			_sideDragHasMoved = false;
		}
		else
		{
			wasActive = _isStackDragActive;
			hasMoved = _stackDragHasMoved;
			_isStackDragActive = false;
			_stackDragHasMoved = false;
		}

		if (!wasActive)
		{
			return false;
		}

		var hasChanged = hasMoved;

		if (canceled)
		{
			RollBackDrag(kind);
			hasChanged = false;
		}
		else if (!hasMoved && TriPaneViewLayoutMath.IsTap(totalTravel))
		{
			hasChanged = RestoreFromGrip(kind) || hasChanged;
		}

		//The drag is over, so the divider goes back to the visibility and enabled state the state
		//model asks for; both were left alone while the gesture was running.
		UpdateState();

		if (hasChanged && HasLeftTheStartingWeights())
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Tests whether the gesture that has just ended left any of the four weights somewhere other
	/// than where it found it. A press, a move and a release on a divider already sitting on its
	/// floor moves the pointer but not the layout, and an application that persists the weights has
	/// nothing to persist.
	/// </summary>
	/// <returns><see langword="true"/> when at least one weight is different.</returns>
	private bool HasLeftTheStartingWeights()
		=> _host.SidePanePercent != _dragStartSidePercent
			|| _host.StackPercent != _dragStartStackPercent
			|| _host.UpperPanePercent != _dragStartUpperPercent
			|| _host.LowerPanePercent != _dragStartLowerPercent;

	/// <summary>
	/// Recomputes the whole state model - effective weights, minimized flags, minimize causes - and
	/// then hands the result to the host (TriPaneView pushes it at its template). Every state change funnels through here, and none of
	/// it needs a template to be correct.
	/// </summary>
	internal void UpdateState()
	{
		if (_isBatchUpdating)
		{
			return;
		}

		//Every pass takes a number. Writing the minimized flags can run application code - they are
		//meant to be bound two way - which can change the state and start a NEWER pass; when that
		//happens this one has to stand down rather than write the values it computed before.
		var version = ++_stateVersion;

		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);

		var isSideMinimized = TriPaneViewLayoutMath.IsMinimized(sideWeight);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);
		var isUpperWeightZero = TriPaneViewLayoutMath.IsMinimized(upperWeight);
		var isLowerWeightZero = TriPaneViewLayoutMath.IsMinimized(lowerWeight);

		//The causes are tracked off the RAW weights, not the normalized ones: "was this region
		//deliberately zeroed" is a question about what was set, and a pair in which BOTH weights are
		//zero normalizes to an even split, which would otherwise wipe the causes of two regions that
		//are about to be minimized again the moment one of them is restored. A cause is only ever
		//read while the matching minimized test is already true, so a cause held for a pair that is
		//laid out evenly is inert.
		_sideCause = ResolveCause(_sideCause, TriPaneViewLayoutMath.SanitizeWeight(_host.SidePanePercent) <= 0d);
		_stackCause = ResolveCause(_stackCause, TriPaneViewLayoutMath.SanitizeWeight(_host.StackPercent) <= 0d);
		_upperCause = ResolveCause(_upperCause, TriPaneViewLayoutMath.SanitizeWeight(_host.UpperPanePercent) <= 0d);
		_lowerCause = ResolveCause(_lowerCause, TriPaneViewLayoutMath.SanitizeWeight(_host.LowerPanePercent) <= 0d);

		_host.SyncMinimizedFlags(
			version,
			isSideMinimized,
			isStackMinimized,
			isStackMinimized || isUpperWeightZero,
			isStackMinimized || isLowerWeightZero);

		if (_stateVersion != version)
		{
			return;
		}

		_host.ApplyLayout(ComputeLayout(sideWeight, stackWeight, upperWeight, lowerWeight));
	}

	private void ApplySideDrag()
	{
		var isPlacedLeft = _host.SidePanePlacement == TriPaneViewSidePanePlacement.Left;
		var firstMinLength = isPlacedLeft ? _host.SidePaneMinLength : _host.StackMinLength;
		var secondMinLength = isPlacedLeft ? _host.StackMinLength : _host.SidePaneMinLength;

		var (firstLength, secondLength) = TriPaneViewLayoutMath.ResolveDragLengths(
			_sideDragFirstLength,
			_sideDragSecondLength,
			_sideDragTotalDelta,
			firstMinLength,
			secondMinLength,
			_host.IsDragToMinimizeEnabled);

		if (TriPaneViewLayoutMath.LengthsToPercent(firstLength, secondLength) is not { } percent)
		{
			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			if (isPlacedLeft)
			{
				_host.SidePanePercent = percent.First;
				_host.StackPercent = percent.Second;
			}
			else
			{
				_host.StackPercent = percent.First;
				_host.SidePanePercent = percent.Second;
			}

			//A region the drag has opened again is described by its live weight, not by a snapshot
			//taken before the gesture, so the stale slot is dropped.
			if (TriPaneViewLayoutMath.SanitizeWeight(_host.SidePanePercent) > 0d)
			{
				_sideSnapshot = null;
			}

			if (TriPaneViewLayoutMath.SanitizeWeight(_host.StackPercent) > 0d)
			{
				_stackSnapshot = null;
			}
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
		}

		UpdateState();
	}

	private void ApplyStackDrag()
	{
		var (firstLength, secondLength) = TriPaneViewLayoutMath.ResolveDragLengths(
			_stackDragFirstLength,
			_stackDragSecondLength,
			_stackDragTotalDelta,
			_host.UpperPaneMinLength,
			_host.LowerPaneMinLength,
			_host.IsDragToMinimizeEnabled);

		if (TriPaneViewLayoutMath.LengthsToPercent(firstLength, secondLength) is not { } percent)
		{
			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			_host.UpperPanePercent = percent.First;
			_host.LowerPanePercent = percent.Second;

			//A region the drag has opened again is described by its live weight, not by a snapshot
			//taken before the gesture, so the stale slot is dropped.
			if (TriPaneViewLayoutMath.SanitizeWeight(_host.UpperPanePercent) > 0d)
			{
				_upperSnapshot = null;
			}

			if (TriPaneViewLayoutMath.SanitizeWeight(_host.LowerPanePercent) > 0d)
			{
				_lowerSnapshot = null;
			}
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
		}

		UpdateState();
	}

	/// <summary>
	/// Picks the weight to record as a region's restore snapshot when a drag starts: the weight it
	/// is open at, or - for a region that is already minimized - the snapshot it already carries,
	/// which is the weight it was open at before it closed.
	/// </summary>
	/// <param name="current">The region's current raw weight.</param>
	/// <param name="existing">The snapshot the region already carries, if there is one.</param>
	/// <returns>The snapshot the region should carry for the duration of the drag.</returns>
	private static double? SnapshotWeight(double current, double? existing)
		=> TriPaneViewLayoutMath.SanitizeWeight(current) > 0d ? current : existing;

	/// <summary>
	/// Puts one axis back exactly where it was when a cancelled drag started - both weights and both
	/// restore snapshots - so nothing the gesture did survives it.
	/// </summary>
	/// <param name="kind">The divider whose axis is being rolled back.</param>
	private void RollBackDrag(TriPaneViewDividerKind kind)
	{
		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			if (kind == TriPaneViewDividerKind.Side)
			{
				_host.SidePanePercent = _sideDragStartSidePercent;
				_host.StackPercent = _sideDragStartStackPercent;
				_sideSnapshot = _sideDragStartSideSnapshot;
				_stackSnapshot = _sideDragStartStackSnapshot;
			}
			else
			{
				_host.UpperPanePercent = _stackDragStartUpperPercent;
				_host.LowerPanePercent = _stackDragStartLowerPercent;
				_upperSnapshot = _stackDragStartUpperSnapshot;
				_lowerSnapshot = _stackDragStartLowerSnapshot;
			}
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;
		}
	}

	/// <summary>
	/// Answers a tap on a divider that is currently a restore grip by restoring the pane - or the
	/// whole stack - the grip belongs to.
	/// </summary>
	/// <param name="kind">The divider that was tapped.</param>
	/// <returns><see langword="true"/> when a region really was restored.</returns>
	private bool RestoreFromGrip(TriPaneViewDividerKind kind)
	{
		var mode = _host.RestoreGripMode;
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);

		if (kind == TriPaneViewDividerKind.Side)
		{
			if (TriPaneViewLayoutMath.IsMinimized(sideWeight))
			{
				if (TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_sideCause)))
				{
					RestoreSidePane();

					return true;
				}
			}
			else if (TriPaneViewLayoutMath.IsMinimized(stackWeight)
				&& TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_stackCause)))
			{
				RestoreStack();

				return true;
			}

			return false;
		}

		if (TriPaneViewLayoutMath.IsMinimized(stackWeight))
		{
			return false;
		}

		if (TriPaneViewLayoutMath.IsMinimized(upperWeight))
		{
			if (TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_upperCause)))
			{
				RestoreUpperPane();

				return true;
			}
		}
		else if (TriPaneViewLayoutMath.IsMinimized(lowerWeight)
			&& TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_lowerCause)))
		{
			RestoreLowerPane();

			return true;
		}

		return false;
	}

	/// <summary>
	/// Works out what the layout shows for a set of effective weights: which dividers are visible, which act as restore
	/// grips (by the minimize causes and the grip mode) and which way their chevrons point.
	/// </summary>
	internal TriPaneLayoutResult ComputeLayout(double sideWeight, double stackWeight, double upperWeight, double lowerWeight)
	{
		var isPlacedLeft = _host.SidePanePlacement == TriPaneViewSidePanePlacement.Left;
		var mode = _host.RestoreGripMode;

		var isSideMinimized = TriPaneViewLayoutMath.IsMinimized(sideWeight);
		var isStackMinimized = TriPaneViewLayoutMath.IsMinimized(stackWeight);
		var isUpperWeightZero = TriPaneViewLayoutMath.IsMinimized(upperWeight);
		var isLowerWeightZero = TriPaneViewLayoutMath.IsMinimized(lowerWeight);

		var isSideGripVisible = false;
		var isSideGripTowardStart = false;

		if (isSideMinimized)
		{
			isSideGripVisible = TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_sideCause));
			isSideGripTowardStart = isPlacedLeft;
		}
		else if (isStackMinimized)
		{
			isSideGripVisible = TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_stackCause));
			isSideGripTowardStart = !isPlacedLeft;
		}

		var isSideDividerVisible = (!isSideMinimized && !isStackMinimized) || isSideGripVisible;

		var isStackGripVisible = false;
		var isStackGripTowardStart = false;

		if (!isStackMinimized)
		{
			if (isUpperWeightZero)
			{
				isStackGripVisible = TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_upperCause));
				isStackGripTowardStart = true;
			}
			else if (isLowerWeightZero)
			{
				isStackGripVisible = TriPaneViewLayoutMath.IsRestoreGripVisible(mode, true, CauseOrDefault(_lowerCause));
			}
		}

		var isStackDividerVisible = !isStackMinimized
			&& ((!isUpperWeightZero && !isLowerWeightZero) || isStackGripVisible);

		return new TriPaneLayoutResult(
			sideWeight,
			stackWeight,
			upperWeight,
			lowerWeight,
			isPlacedLeft,
			isSideDividerVisible,
			isSideGripVisible,
			isSideGripTowardStart,
			isStackDividerVisible,
			isStackGripVisible,
			isStackGripTowardStart);
	}

	/// <summary>
	/// Minimizes the side pane: its width weight is snapshotted and set to zero, so the pane
	/// collapses to nothing while its content element stays in the visual tree. The very same
	/// instance, with all of its state, is shown again by <see cref="RestoreSidePane"/>.
	/// </summary>
	/// <remarks>
	/// A request that would leave no pane open at all is ignored and the control's state is left
	/// exactly as it was. Because this is a request from code, no restore grip is offered while
	/// <see cref="ITriPaneLayoutHost.RestoreGripMode"/> is <see cref="TriPaneViewRestoreGripMode.Auto"/>; calling this
	/// on a pane the user had already dragged shut turns its grip off for the same reason.
	/// </remarks>
	internal void MinimizeSidePane()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);
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
			_sideSnapshot = _host.SidePanePercent;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			_host.SidePanePercent = 0d;
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
	internal void RestoreSidePane()
	{
		var (sideWeight, _) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);

		if (!TriPaneViewLayoutMath.IsMinimized(sideWeight))
		{
			UpdateState();

			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			_host.SidePanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
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
	/// zeroes <see cref="ITriPaneLayoutHost.StackPercent"/>, so the whole stack collapses and the side pane takes the
	/// control. A request that would leave no pane open at all is ignored and the control's state is
	/// left exactly as it was. Because this is a request from code, no restore grip is offered while
	/// <see cref="ITriPaneLayoutHost.RestoreGripMode"/> is <see cref="TriPaneViewRestoreGripMode.Auto"/>.
	/// </remarks>
	internal void MinimizeUpperPane()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);
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
			_upperSnapshot = _host.UpperPanePercent;
		}

		var wasBatchUpdating = _isBatchUpdating;
		var didMinimizeStack = false;
		_isBatchUpdating = true;

		try
		{
			_host.UpperPanePercent = 0d;
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
	internal void RestoreUpperPane()
	{
		var (_, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (upperWeight, _) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);
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

			if (TriPaneViewLayoutMath.SanitizeWeight(_host.UpperPanePercent) <= 0d)
			{
				_host.UpperPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
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
	/// zeroes <see cref="ITriPaneLayoutHost.StackPercent"/>, so the whole stack collapses and the side pane takes the
	/// control. A request that would leave no pane open at all is ignored and the control's state is
	/// left exactly as it was. Because this is a request from code, no restore grip is offered while
	/// <see cref="ITriPaneLayoutHost.RestoreGripMode"/> is <see cref="TriPaneViewRestoreGripMode.Auto"/>.
	/// </remarks>
	internal void MinimizeLowerPane()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);
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
			_lowerSnapshot = _host.LowerPanePercent;
		}

		var wasBatchUpdating = _isBatchUpdating;
		var didMinimizeStack = false;
		_isBatchUpdating = true;

		try
		{
			_host.LowerPanePercent = 0d;
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
	internal void RestoreLowerPane()
	{
		var (_, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (_, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);
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

			if (TriPaneViewLayoutMath.SanitizeWeight(_host.LowerPanePercent) <= 0d)
			{
				_host.LowerPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
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
	/// <c>TriPaneView.IsSidePaneMinimized</c>, <c>TriPaneView.IsUpperPaneMinimized</c> and
	/// <c>TriPaneView.IsLowerPaneMinimized</c> report. A pair of weights that are BOTH zero is laid out
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
	internal void RestoreAll()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(_host.UpperPanePercent, _host.LowerPanePercent);
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
				_host.SidePanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_sideSnapshot,
					TriPaneViewLayoutMath.DefaultSidePanePercent);
				_sideSnapshot = null;
			}

			if (isStackMinimized)
			{
				_host.StackPercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_stackSnapshot,
					TriPaneViewLayoutMath.DefaultStackPercent);
				_stackSnapshot = null;
			}

			if (isUpperMinimized && TriPaneViewLayoutMath.SanitizeWeight(_host.UpperPanePercent) <= 0d)
			{
				_host.UpperPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_upperSnapshot,
					TriPaneViewLayoutMath.DefaultUpperPanePercent);
				_upperSnapshot = null;
			}

			if (isLowerMinimized && TriPaneViewLayoutMath.SanitizeWeight(_host.LowerPanePercent) <= 0d)
			{
				_host.LowerPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
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
	/// Minimizes the stack - the whole region holding the upper and lower panes: its width weight is
	/// snapshotted and set to zero, so both panes collapse together and the side pane takes the
	/// control. Neither pane's content element leaves the visual tree, and
	/// <see cref="RestoreStack"/> brings the very same instances back.
	/// </summary>
	/// <remarks>
	/// A request that would leave no pane open at all - the side pane is minimized too - is ignored
	/// and the control's state is left exactly as it was. Because this is a request from code, no
	/// restore grip is offered while <see cref="ITriPaneLayoutHost.RestoreGripMode"/> is
	/// <see cref="TriPaneViewRestoreGripMode.Auto"/>; <see cref="RestoreStack"/> is then the only way
	/// back, and it works whatever collapsed the stack.
	/// </remarks>
	internal void MinimizeStack()
	{
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);

		if (TriPaneViewLayoutMath.IsMinimized(sideWeight) || TriPaneViewLayoutMath.IsMinimized(stackWeight))
		{
			UpdateState();

			return;
		}

		_stackSnapshot = _host.StackPercent;

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			_host.StackPercent = 0d;
		}
		finally
		{
			_isBatchUpdating = wasBatchUpdating;

			//The cause belongs to THIS region and nothing else: a code minimize here must not turn
			//off the restore grip of a pane the user dragged shut.
			_stackCause = TriPaneViewMinimizeCause.Code;
			UpdateState();
		}
	}

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
	internal void RestoreStack()
	{
		var (_, stackWeight) = TriPaneViewLayoutMath.NormalizePair(_host.SidePanePercent, _host.StackPercent);

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

			if (TriPaneViewLayoutMath.SanitizeWeight(_host.UpperPanePercent) <= 0d
				&& TriPaneViewLayoutMath.SanitizeWeight(_host.LowerPanePercent) <= 0d)
			{
				_host.UpperPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
					_upperSnapshot,
					TriPaneViewLayoutMath.DefaultUpperPanePercent);
				_host.LowerPanePercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
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
			|| TriPaneViewLayoutMath.SanitizeWeight(_host.UpperPanePercent) > 0d
			|| TriPaneViewLayoutMath.SanitizeWeight(_host.LowerPanePercent) > 0d)
		{
			return false;
		}

		_stackSnapshot = _host.StackPercent;
		_host.StackPercent = 0d;

		return true;
	}

	private void RestoreStackWeight(bool isStackMinimized)
	{
		if (!isStackMinimized)
		{
			return;
		}

		_host.StackPercent = TriPaneViewLayoutMath.ResolveRestoreWeight(
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
}
