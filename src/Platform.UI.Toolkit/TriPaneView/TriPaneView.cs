#nullable enable

using CodeBrix.Platform.UI.Toolkit.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Toolkit;

/// <summary>
/// A three-pane container: one full-height side pane along the left or right edge, and a stack on
/// the other side of a draggable side divider holding an upper pane and a lower pane separated by a
/// second draggable divider. Every pane scrolls vertically by default, proportions are kept as
/// percent-style weights, and any pane can be minimized - collapsed to nothing while staying alive -
/// and restored, by the user or from code.
/// </summary>
/// <remarks>
/// <para>
/// Proportions live in two pairs of weights: <see cref="SidePanePercent"/> against
/// <see cref="StackPercent"/> across the control, and <see cref="UpperPanePercent"/> against
/// <see cref="LowerPanePercent"/> down the stack. They are used as star weights, so only the ratio
/// within a pair matters. A weight of zero means the pane it belongs to is minimized; the user
/// reaches that state by dragging a divider all the way over, and code reaches it through
/// <see cref="MinimizeSidePane"/>, <see cref="MinimizeUpperPane"/>,
/// <see cref="MinimizeLowerPane"/> or the matching <c>IsMinimized</c> properties.
/// </para>
/// <para>
/// Minimizing never detaches anything. The pane's content element stays exactly where it is in the
/// visual tree and its column or row is simply given zero length, so the very same instance - with
/// its scroll position, its text, its selection - is what comes back when the pane is restored.
/// </para>
/// <para>
/// Each pane is hosted in a <see cref="ScrollViewer"/>, so pane content is measured with unbounded
/// height and will not stretch vertically to fill the pane unless the pane's vertical scroll bar
/// visibility is set to <see cref="ScrollBarVisibility.Disabled"/>. The same applies horizontally
/// once a pane's horizontal scroll mode is turned on.
/// </para>
/// <para>
/// In XAML, map the namespace with <c>xmlns:toolkit="using:CodeBrix.Platform.UI.Toolkit"</c> and
/// use <c>&lt;toolkit:TriPaneView&gt;</c> with its <c>SidePane</c>, <c>UpperPane</c> and
/// <c>LowerPane</c> properties.
/// </para>
/// </remarks>
[TemplatePart(Name = SideColumnPartName, Type = typeof(ColumnDefinition))]
[TemplatePart(Name = SideDividerColumnPartName, Type = typeof(ColumnDefinition))]
[TemplatePart(Name = StackColumnPartName, Type = typeof(ColumnDefinition))]
[TemplatePart(Name = StackGridPartName, Type = typeof(Grid))]
[TemplatePart(Name = UpperRowPartName, Type = typeof(RowDefinition))]
[TemplatePart(Name = StackDividerRowPartName, Type = typeof(RowDefinition))]
[TemplatePart(Name = LowerRowPartName, Type = typeof(RowDefinition))]
[TemplatePart(Name = SidePaneScrollViewerPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = UpperPaneScrollViewerPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = LowerPaneScrollViewerPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = SideDividerPartName, Type = typeof(TriPaneViewDivider))]
[TemplatePart(Name = StackDividerPartName, Type = typeof(TriPaneViewDivider))]
public sealed partial class TriPaneView : Control
{
	/// <summary>The default value of <see cref="DividerThickness"/>, in pixels.</summary>
	internal const double DefaultDividerThickness = 6d;

	private const string SideColumnPartName = "PART_SideColumn";
	private const string SideDividerColumnPartName = "PART_SideDividerColumn";
	private const string StackColumnPartName = "PART_StackColumn";
	private const string StackGridPartName = "PART_StackGrid";
	private const string UpperRowPartName = "PART_UpperRow";
	private const string StackDividerRowPartName = "PART_StackDividerRow";
	private const string LowerRowPartName = "PART_LowerRow";
	private const string SidePaneScrollViewerPartName = "PART_SidePaneScrollViewer";
	private const string UpperPaneScrollViewerPartName = "PART_UpperPaneScrollViewer";
	private const string LowerPaneScrollViewerPartName = "PART_LowerPaneScrollViewer";
	private const string SideDividerPartName = "PART_SideDivider";
	private const string StackDividerPartName = "PART_StackDivider";

	private const int LeadingColumnIndex = 0;
	private const int TrailingColumnIndex = 2;

	private ColumnDefinition? _sideColumn;
	private ColumnDefinition? _sideDividerColumn;
	private ColumnDefinition? _stackColumn;
	private RowDefinition? _upperRow;
	private RowDefinition? _stackDividerRow;
	private RowDefinition? _lowerRow;
	private ScrollViewer? _sidePaneScrollViewer;
	private ScrollViewer? _upperPaneScrollViewer;
	private ScrollViewer? _lowerPaneScrollViewer;
	private FrameworkElement? _stackGrid;
	private TriPaneViewDivider? _sideDivider;
	private TriPaneViewDivider? _stackDivider;

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

	/// <summary>
	/// Initializes a new instance of the <see cref="TriPaneView"/> class.
	/// </summary>
	public TriPaneView()
	{
		DefaultStyleKey = typeof(TriPaneView);
		IsTabStop = false;
		SizeChanged += OnSizeChanged;
		UpdateState();
	}

	/// <summary>
	/// Gets the effective star weight of the side pane: the normalized half of the side-pane/stack
	/// pair. Zero means the pane is minimized.
	/// </summary>
	internal double SidePaneEffectiveWeight { get; private set; }

	/// <summary>
	/// Gets the effective star weight of the stack: the normalized half of the side-pane/stack pair.
	/// Zero means the whole stack is minimized.
	/// </summary>
	internal double StackEffectiveWeight { get; private set; }

	/// <summary>
	/// Gets the effective star weight of the upper pane: the normalized half of the upper/lower
	/// pair. Zero means the pane is minimized.
	/// </summary>
	internal double UpperPaneEffectiveWeight { get; private set; }

	/// <summary>
	/// Gets the effective star weight of the lower pane: the normalized half of the upper/lower
	/// pair. Zero means the pane is minimized.
	/// </summary>
	internal double LowerPaneEffectiveWeight { get; private set; }

	/// <summary>Gets a value indicating whether the side divider is shown at all.</summary>
	internal bool IsSideDividerVisible { get; private set; }

	/// <summary>Gets a value indicating whether the side divider is acting as a restore grip.</summary>
	internal bool IsSideRestoreGripVisible { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the side divider's restore chevron points left rather than
	/// right.
	/// </summary>
	internal bool IsSideRestoreGripTowardStart { get; private set; }

	/// <summary>Gets a value indicating whether the stack divider is shown at all.</summary>
	internal bool IsStackDividerVisible { get; private set; }

	/// <summary>Gets a value indicating whether the stack divider is acting as a restore grip.</summary>
	internal bool IsStackRestoreGripVisible { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the stack divider's restore chevron points up rather than
	/// down.
	/// </summary>
	internal bool IsStackRestoreGripTowardStart { get; private set; }

	/// <summary>Gets the root-grid column index the side pane currently occupies.</summary>
	internal int SidePaneColumnIndex { get; private set; }

	/// <summary>Gets the root-grid column index the stack currently occupies.</summary>
	internal int StackColumnIndex { get; private set; }

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		DetachDivider(_sideDivider, OnSideDividerDragStarted, OnSideDividerDragDelta, OnSideDividerDragCompleted);
		DetachDivider(_stackDivider, OnStackDividerDragStarted, OnStackDividerDragDelta, OnStackDividerDragCompleted);

		_sideColumn = GetTemplateChild(SideColumnPartName) as ColumnDefinition;
		_sideDividerColumn = GetTemplateChild(SideDividerColumnPartName) as ColumnDefinition;
		_stackColumn = GetTemplateChild(StackColumnPartName) as ColumnDefinition;
		_upperRow = GetTemplateChild(UpperRowPartName) as RowDefinition;
		_stackDividerRow = GetTemplateChild(StackDividerRowPartName) as RowDefinition;
		_lowerRow = GetTemplateChild(LowerRowPartName) as RowDefinition;
		_sidePaneScrollViewer = GetTemplateChild(SidePaneScrollViewerPartName) as ScrollViewer;
		_upperPaneScrollViewer = GetTemplateChild(UpperPaneScrollViewerPartName) as ScrollViewer;
		_lowerPaneScrollViewer = GetTemplateChild(LowerPaneScrollViewerPartName) as ScrollViewer;
		_stackGrid = GetTemplateChild(StackGridPartName) as FrameworkElement;
		_sideDivider = GetTemplateChild(SideDividerPartName) as TriPaneViewDivider;
		_stackDivider = GetTemplateChild(StackDividerPartName) as TriPaneViewDivider;

		AttachDivider(_sideDivider, OnSideDividerDragStarted, OnSideDividerDragDelta, OnSideDividerDragCompleted);
		AttachDivider(_stackDivider, OnStackDividerDragStarted, OnStackDividerDragDelta, OnStackDividerDragCompleted);

		ApplyPlacement();
		UpdateState();
		ApplyScrollSettings();
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
		if (kind == TriPaneViewDividerKind.Side)
		{
			_isSideDragActive = true;
			_sideDragHasMoved = false;
			_sideDragFirstLength = firstLength;
			_sideDragSecondLength = secondLength;
			_sideDragTotalDelta = 0d;
			_sideDragStartSidePercent = SidePanePercent;
			_sideDragStartStackPercent = StackPercent;
			_sideDragStartSideSnapshot = _sideSnapshot;
			_sideDragStartStackSnapshot = _stackSnapshot;
			_sideSnapshot = SnapshotWeight(SidePanePercent, _sideSnapshot);
			_stackSnapshot = SnapshotWeight(StackPercent, _stackSnapshot);
		}
		else
		{
			_isStackDragActive = true;
			_stackDragHasMoved = false;
			_stackDragFirstLength = firstLength;
			_stackDragSecondLength = secondLength;
			_stackDragTotalDelta = 0d;
			_stackDragStartUpperPercent = UpperPanePercent;
			_stackDragStartLowerPercent = LowerPanePercent;
			_stackDragStartUpperSnapshot = _upperSnapshot;
			_stackDragStartLowerSnapshot = _lowerSnapshot;
			_upperSnapshot = SnapshotWeight(UpperPanePercent, _upperSnapshot);
			_lowerSnapshot = SnapshotWeight(LowerPanePercent, _lowerSnapshot);
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
	/// where it was when the drag started. <see cref="DividerDragCompleted"/> is raised afterwards,
	/// with the weights already written back, and only when the interaction actually changed the
	/// layout - a bare click on an ordinary divider, and a cancelled drag, raise nothing.
	/// </summary>
	/// <param name="kind">The divider that was being dragged.</param>
	/// <param name="totalTravel">The total distance, in pixels, the pointer travelled.</param>
	/// <param name="canceled">Whether the drag was cancelled rather than completed.</param>
	internal void CompleteDividerDrag(TriPaneViewDividerKind kind, double totalTravel, bool canceled)
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
			return;
		}

		var hasChanged = hasMoved;

		if (canceled)
		{
			RollBackDrag(kind);
			hasChanged = false;
		}
		else if (TriPaneViewLayoutMath.IsTap(totalTravel))
		{
			hasChanged = RestoreFromGrip(kind) || hasChanged;
		}

		//The drag is over, so the divider goes back to the visibility and enabled state the state
		//model asks for; both were left alone while the gesture was running.
		UpdateState();

		if (hasChanged)
		{
			DividerDragCompleted?.Invoke(this, new TriPaneViewDividerDragCompletedEventArgs(kind));
		}
	}

	/// <summary>
	/// Recomputes the whole state model - effective weights, minimized flags, minimize causes - and
	/// then pushes the result at the template. Every state change funnels through here, and none of
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

		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);

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
		_sideCause = ResolveCause(_sideCause, TriPaneViewLayoutMath.SanitizeWeight(SidePanePercent) <= 0d);
		_stackCause = ResolveCause(_stackCause, TriPaneViewLayoutMath.SanitizeWeight(StackPercent) <= 0d);
		_upperCause = ResolveCause(_upperCause, TriPaneViewLayoutMath.SanitizeWeight(UpperPanePercent) <= 0d);
		_lowerCause = ResolveCause(_lowerCause, TriPaneViewLayoutMath.SanitizeWeight(LowerPanePercent) <= 0d);

		SyncMinimizedFlags(
			version,
			isSideMinimized,
			isStackMinimized || isUpperWeightZero,
			isStackMinimized || isLowerWeightZero);

		if (_stateVersion != version)
		{
			return;
		}

		ApplyLayout(sideWeight, stackWeight, upperWeight, lowerWeight);
	}

	private static void AttachDivider(
		TriPaneViewDivider? divider,
		DragStartedEventHandler started,
		DragDeltaEventHandler delta,
		DragCompletedEventHandler completed)
	{
		if (divider is not null)
		{
			divider.DragStarted += started;
			divider.DragDelta += delta;
			divider.DragCompleted += completed;
		}
	}

	private static void DetachDivider(
		TriPaneViewDivider? divider,
		DragStartedEventHandler started,
		DragDeltaEventHandler delta,
		DragCompletedEventHandler completed)
	{
		if (divider is not null)
		{
			divider.DragStarted -= started;
			divider.DragDelta -= delta;
			divider.DragCompleted -= completed;
		}
	}

	private static void ApplyColumn(ColumnDefinition? column, double weight, double minLength)
	{
		if (column is null)
		{
			return;
		}

		if (TriPaneViewLayoutMath.IsMinimized(weight))
		{
			column.MinWidth = 0d;
			column.Width = new GridLength(0d, GridUnitType.Pixel);
		}
		else
		{
			column.MinWidth = TriPaneViewLayoutMath.SanitizeLength(minLength);
			column.Width = new GridLength(weight, GridUnitType.Star);
		}
	}

	private static void ApplyRow(RowDefinition? row, double weight, double minLength)
	{
		if (row is null)
		{
			return;
		}

		if (TriPaneViewLayoutMath.IsMinimized(weight))
		{
			row.MinHeight = 0d;
			row.Height = new GridLength(0d, GridUnitType.Pixel);
		}
		else
		{
			row.MinHeight = TriPaneViewLayoutMath.SanitizeLength(minLength);
			row.Height = new GridLength(weight, GridUnitType.Star);
		}
	}

	private static void ApplyPaneScrollSettings(
		ScrollViewer? scrollViewer,
		ScrollBarVisibility verticalScrollBarVisibility,
		TriPaneViewHorizontalScrollMode horizontalScrollMode,
		bool isPortrait)
	{
		if (scrollViewer is null)
		{
			return;
		}

		var isHorizontalEnabled = TriPaneViewLayoutMath.ShouldEnableHorizontalScrolling(horizontalScrollMode, isPortrait);

		var verticalScrollMode = verticalScrollBarVisibility == ScrollBarVisibility.Disabled
			? ScrollMode.Disabled
			: ScrollMode.Enabled;
		var horizontalScrollBarVisibility = isHorizontalEnabled
			? ScrollBarVisibility.Auto
			: ScrollBarVisibility.Disabled;
		var horizontalScrollModeValue = isHorizontalEnabled ? ScrollMode.Enabled : ScrollMode.Disabled;

		var hasChanged = scrollViewer.VerticalScrollBarVisibility != verticalScrollBarVisibility
			|| scrollViewer.VerticalScrollMode != verticalScrollMode
			|| scrollViewer.HorizontalScrollBarVisibility != horizontalScrollBarVisibility
			|| scrollViewer.HorizontalScrollMode != horizontalScrollModeValue;

		if (!hasChanged)
		{
			return;
		}

		scrollViewer.VerticalScrollBarVisibility = verticalScrollBarVisibility;
		scrollViewer.VerticalScrollMode = verticalScrollMode;
		scrollViewer.HorizontalScrollMode = horizontalScrollModeValue;
		scrollViewer.HorizontalScrollBarVisibility = horizontalScrollBarVisibility;

		//Turning an axis on or off changes how the pane's content is measured - unbounded along an
		//axis that scrolls, and to the pane along one that does not - but none of the four scroll
		//properties invalidates a measure, so without this the pane would keep the extent it was
		//last measured with and would not start (or stop) scrolling until something else happened
		//to invalidate it. Only a real change asks for the new pass, so the size changes that leave
		//the settings alone cost nothing.
		InvalidatePaneMeasure(scrollViewer);
	}

	/// <summary>
	/// Asks a pane to measure itself again after its scroll settings changed.
	/// </summary>
	/// <param name="scrollViewer">The pane's scroll viewer.</param>
	/// <remarks>
	/// It is the scroll content presenter's own measure that has to be invalidated. Invalidating
	/// only the scroll viewer is not enough: it measures the presenter with the same available size
	/// as before, and a presenter that is not itself invalid answers from its cached result without
	/// ever looking at the new setting - so the content would go on being measured to the pane along
	/// an axis that has just been told to scroll.
	/// </remarks>
	private static void InvalidatePaneMeasure(ScrollViewer scrollViewer)
	{
		scrollViewer.InvalidateMeasure();

		if (FindScrollContentPresenter(scrollViewer) is { } presenter)
		{
			presenter.InvalidateMeasure();
		}
	}

	/// <summary>
	/// Finds the scroll content presenter a scroll viewer is using, which is the first one in its
	/// subtree: any other belongs to the pane's own content, below it.
	/// </summary>
	/// <param name="root">The element to search from.</param>
	/// <returns>The presenter, or <see langword="null"/> when the scroll viewer has no template yet.</returns>
	private static ScrollContentPresenter? FindScrollContentPresenter(DependencyObject root)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);

		for (var index = 0; index < count; index++)
		{
			var child = VisualTreeHelper.GetChild(root, index);

			if (child is ScrollContentPresenter presenter)
			{
				return presenter;
			}

			if (FindScrollContentPresenter(child) is { } deeper)
			{
				return deeper;
			}
		}

		return null;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e) => ApplyScrollSettings();

	private void OnSideDividerDragStarted(object sender, DragStartedEventArgs e)
	{
		var isPlacedLeft = SidePanePlacement == TriPaneViewSidePanePlacement.Left;
		var sideLength = _sidePaneScrollViewer?.ActualWidth ?? 0d;
		var stackLength = _stackGrid?.ActualWidth ?? 0d;

		StartDividerDrag(
			TriPaneViewDividerKind.Side,
			isPlacedLeft ? sideLength : stackLength,
			isPlacedLeft ? stackLength : sideLength);
	}

	private void OnSideDividerDragDelta(object sender, DragDeltaEventArgs e)
	{
		if (CanUserDragSideDivider)
		{
			UpdateDividerDrag(TriPaneViewDividerKind.Side, e.HorizontalChange);
		}
	}

	private void OnSideDividerDragCompleted(object sender, DragCompletedEventArgs e)
		=> CompleteDividerDrag(TriPaneViewDividerKind.Side, e.HorizontalChange, e.Canceled);

	private void OnStackDividerDragStarted(object sender, DragStartedEventArgs e)
		=> StartDividerDrag(
			TriPaneViewDividerKind.Stack,
			_upperPaneScrollViewer?.ActualHeight ?? 0d,
			_lowerPaneScrollViewer?.ActualHeight ?? 0d);

	private void OnStackDividerDragDelta(object sender, DragDeltaEventArgs e)
	{
		if (CanUserDragStackDivider)
		{
			UpdateDividerDrag(TriPaneViewDividerKind.Stack, e.VerticalChange);
		}
	}

	private void OnStackDividerDragCompleted(object sender, DragCompletedEventArgs e)
		=> CompleteDividerDrag(TriPaneViewDividerKind.Stack, e.VerticalChange, e.Canceled);

	private void ApplySideDrag()
	{
		var isPlacedLeft = SidePanePlacement == TriPaneViewSidePanePlacement.Left;
		var firstMinLength = isPlacedLeft ? SidePaneMinLength : StackMinLength;
		var secondMinLength = isPlacedLeft ? StackMinLength : SidePaneMinLength;

		var (firstLength, secondLength) = TriPaneViewLayoutMath.ResolveDragLengths(
			_sideDragFirstLength,
			_sideDragSecondLength,
			_sideDragTotalDelta,
			firstMinLength,
			secondMinLength,
			IsDragToMinimizeEnabled);

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
				SidePanePercent = percent.First;
				StackPercent = percent.Second;
			}
			else
			{
				StackPercent = percent.First;
				SidePanePercent = percent.Second;
			}

			//A region the drag has opened again is described by its live weight, not by a snapshot
			//taken before the gesture, so the stale slot is dropped.
			if (TriPaneViewLayoutMath.SanitizeWeight(SidePanePercent) > 0d)
			{
				_sideSnapshot = null;
			}

			if (TriPaneViewLayoutMath.SanitizeWeight(StackPercent) > 0d)
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
			UpperPaneMinLength,
			LowerPaneMinLength,
			IsDragToMinimizeEnabled);

		if (TriPaneViewLayoutMath.LengthsToPercent(firstLength, secondLength) is not { } percent)
		{
			return;
		}

		var wasBatchUpdating = _isBatchUpdating;
		_isBatchUpdating = true;

		try
		{
			UpperPanePercent = percent.First;
			LowerPanePercent = percent.Second;

			//A region the drag has opened again is described by its live weight, not by a snapshot
			//taken before the gesture, so the stale slot is dropped.
			if (TriPaneViewLayoutMath.SanitizeWeight(UpperPanePercent) > 0d)
			{
				_upperSnapshot = null;
			}

			if (TriPaneViewLayoutMath.SanitizeWeight(LowerPanePercent) > 0d)
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
				SidePanePercent = _sideDragStartSidePercent;
				StackPercent = _sideDragStartStackPercent;
				_sideSnapshot = _sideDragStartSideSnapshot;
				_stackSnapshot = _sideDragStartStackSnapshot;
			}
			else
			{
				UpperPanePercent = _stackDragStartUpperPercent;
				LowerPanePercent = _stackDragStartLowerPercent;
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
		var mode = RestoreGripMode;
		var (sideWeight, stackWeight) = TriPaneViewLayoutMath.NormalizePair(SidePanePercent, StackPercent);
		var (upperWeight, lowerWeight) = TriPaneViewLayoutMath.NormalizePair(UpperPanePercent, LowerPanePercent);

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

	private void ApplyPlacement()
	{
		var isPlacedLeft = SidePanePlacement == TriPaneViewSidePanePlacement.Left;

		if (_sidePaneScrollViewer is not null)
		{
			Grid.SetColumn(_sidePaneScrollViewer, isPlacedLeft ? LeadingColumnIndex : TrailingColumnIndex);
		}

		if (_stackGrid is not null)
		{
			Grid.SetColumn(_stackGrid, isPlacedLeft ? TrailingColumnIndex : LeadingColumnIndex);
		}
	}

	private void ApplyLayout(double sideWeight, double stackWeight, double upperWeight, double lowerWeight)
	{
		var isPlacedLeft = SidePanePlacement == TriPaneViewSidePanePlacement.Left;
		var mode = RestoreGripMode;

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

		SidePaneEffectiveWeight = sideWeight;
		StackEffectiveWeight = stackWeight;
		UpperPaneEffectiveWeight = upperWeight;
		LowerPaneEffectiveWeight = lowerWeight;
		IsSideDividerVisible = isSideDividerVisible;
		IsSideRestoreGripVisible = isSideGripVisible;
		IsSideRestoreGripTowardStart = isSideGripTowardStart;
		IsStackDividerVisible = isStackDividerVisible;
		IsStackRestoreGripVisible = isStackGripVisible;
		IsStackRestoreGripTowardStart = isStackGripTowardStart;
		SidePaneColumnIndex = isPlacedLeft ? LeadingColumnIndex : TrailingColumnIndex;
		StackColumnIndex = isPlacedLeft ? TrailingColumnIndex : LeadingColumnIndex;

		//A divider that is being dragged stays on screen and stays enabled for the whole gesture,
		//whatever the state model says it should be once the gesture is over. The pane can reach zero
		//mid-drag with the shipped defaults, and hiding, shrinking or disabling the very element the
		//pointer is holding would take the handle away in the middle of the drag - and disabling it
		//would cancel the drag from inside this method.
		var isSideDividerShown = isSideDividerVisible || _isSideDragActive;
		var isStackDividerShown = isStackDividerVisible || _isStackDragActive;

		var thickness = DividerThickness;

		ApplyColumn(
			_sideColumn,
			isPlacedLeft ? sideWeight : stackWeight,
			isPlacedLeft ? SidePaneMinLength : StackMinLength);

		ApplyColumn(
			_stackColumn,
			isPlacedLeft ? stackWeight : sideWeight,
			isPlacedLeft ? StackMinLength : SidePaneMinLength);

		if (_sideDividerColumn is not null)
		{
			_sideDividerColumn.Width = new GridLength(
				TriPaneViewLayoutMath.ResolveDividerTrackLength(isSideDividerShown, thickness),
				GridUnitType.Pixel);
		}

		ApplyRow(_upperRow, upperWeight, UpperPaneMinLength);
		ApplyRow(_lowerRow, lowerWeight, LowerPaneMinLength);

		if (_stackDividerRow is not null)
		{
			_stackDividerRow.Height = new GridLength(
				TriPaneViewLayoutMath.ResolveDividerTrackLength(isStackDividerShown, thickness),
				GridUnitType.Pixel);
		}

		ApplyDividerState(
			_sideDivider,
			isSideDividerShown,
			CanUserDragSideDivider || _isSideDragActive,
			isSideGripVisible,
			isSideGripTowardStart);

		ApplyDividerState(
			_stackDivider,
			isStackDividerShown,
			CanUserDragStackDivider || _isStackDragActive,
			isStackGripVisible,
			isStackGripTowardStart);
	}

	private static void ApplyDividerState(
		TriPaneViewDivider? divider,
		bool isVisible,
		bool canUserDrag,
		bool isRestoreGrip,
		bool isGripTowardStart)
	{
		if (divider is null)
		{
			return;
		}

		divider.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
		divider.IsEnabled = canUserDrag || isRestoreGrip;
		divider.SetRestoreGripState(isRestoreGrip, isGripTowardStart);
	}

	private void ApplyScrollSettings()
	{
		var isPortrait = TriPaneViewLayoutMath.IsPortrait(ActualWidth, ActualHeight);

		ApplyPaneScrollSettings(
			_sidePaneScrollViewer,
			SidePaneVerticalScrollBarVisibility,
			SidePaneHorizontalScrollMode,
			isPortrait);

		ApplyPaneScrollSettings(
			_upperPaneScrollViewer,
			UpperPaneVerticalScrollBarVisibility,
			UpperPaneHorizontalScrollMode,
			isPortrait);

		ApplyPaneScrollSettings(
			_lowerPaneScrollViewer,
			LowerPaneVerticalScrollBarVisibility,
			LowerPaneHorizontalScrollMode,
			isPortrait);
	}
}
