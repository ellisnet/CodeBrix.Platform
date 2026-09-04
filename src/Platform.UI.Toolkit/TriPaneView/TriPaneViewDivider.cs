#nullable enable

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace CodeBrix.Platform.UI.Toolkit;

/// <summary>
/// The draggable divider a <see cref="TriPaneView"/> puts between the side pane and the stack, and
/// between the upper and lower panes. It is a pointer-driven drag handle: it raises
/// <see cref="DragStarted"/>, <see cref="DragDelta"/> and <see cref="DragCompleted"/> exactly as the
/// framework's <see cref="Thumb"/> does, and it sets the resize cursor that matches its
/// <see cref="Orientation"/>.
/// </summary>
/// <remarks>
/// <para>
/// The type is public because it is a template part: an application restyling
/// <see cref="TriPaneView"/> needs to name it. It is not otherwise meant to be used on its own,
/// and it takes all of its state from the <see cref="TriPaneView"/> that owns it.
/// </para>
/// <para>
/// When the pane next to the divider is minimized and the owning control's
/// <see cref="TriPaneView.RestoreGripMode"/> calls for it, the divider becomes a restore grip:
/// <see cref="IsRestoreGrip"/> turns <see langword="true"/> and the default template shows a chevron
/// pointing toward the minimized pane. Clicking the grip restores the pane; dragging it reopens the
/// pane live.
/// </para>
/// </remarks>
[TemplateVisualState(GroupName = CommonStatesGroupName, Name = NormalStateName)]
[TemplateVisualState(GroupName = CommonStatesGroupName, Name = PointerOverStateName)]
[TemplateVisualState(GroupName = CommonStatesGroupName, Name = PressedStateName)]
[TemplateVisualState(GroupName = CommonStatesGroupName, Name = DisabledStateName)]
[TemplateVisualState(GroupName = GripStatesGroupName, Name = NoGripStateName)]
[TemplateVisualState(GroupName = GripStatesGroupName, Name = GripLeftStateName)]
[TemplateVisualState(GroupName = GripStatesGroupName, Name = GripRightStateName)]
[TemplateVisualState(GroupName = GripStatesGroupName, Name = GripUpStateName)]
[TemplateVisualState(GroupName = GripStatesGroupName, Name = GripDownStateName)]
public sealed partial class TriPaneViewDivider : Control
{
	private const string CommonStatesGroupName = "CommonStates";
	private const string NormalStateName = "Normal";
	private const string PointerOverStateName = "PointerOver";
	private const string PressedStateName = "Pressed";
	private const string DisabledStateName = "Disabled";

	private const string GripStatesGroupName = "GripStates";
	private const string NoGripStateName = "NoGrip";
	private const string GripLeftStateName = "GripLeft";
	private const string GripRightStateName = "GripRight";
	private const string GripUpStateName = "GripUp";
	private const string GripDownStateName = "GripDown";

	private bool _isPointerOver;
	private Point _origin;
	private Point _previousPosition;
	private GeneralTransform? _transformToOrigin;
	private Pointer? _capturedPointer;

	/// <summary>
	/// Initializes a new instance of the <see cref="TriPaneViewDivider"/> class.
	/// </summary>
	public TriPaneViewDivider()
	{
		DefaultStyleKey = typeof(TriPaneViewDivider);
		IsTabStop = false;

		//The divider sits between scrolling panes, and a drag on it is a resize, never a
		//manipulation. Taking the divider out of the manipulation system stops direct manipulation
		//taking the pointer over mid-drag - the framework's own recommendation in place of the
		//deprecated capture option that used to say the same thing.
		ManipulationMode = ManipulationModes.None;

		IsEnabledChanged += OnIsEnabledChanged;
		UpdateCursor();
	}

	/// <summary>
	/// Occurs when the divider takes pointer capture and a drag begins.
	/// </summary>
	public event DragStartedEventHandler? DragStarted;

	/// <summary>
	/// Occurs each time the pointer moves while the divider is being dragged. The event carries the
	/// change since the previous move, not the total travel.
	/// </summary>
	public event DragDeltaEventHandler? DragDelta;

	/// <summary>
	/// Occurs when the divider loses pointer capture and the drag ends, whether it was completed or
	/// cancelled.
	/// </summary>
	public event DragCompletedEventHandler? DragCompleted;

	/// <summary>
	/// Gets or sets the orientation of the line the divider draws:
	/// <see cref="Microsoft.UI.Xaml.Controls.Orientation.Vertical"/> for the side divider, which is
	/// dragged left and right, and
	/// <see cref="Microsoft.UI.Xaml.Controls.Orientation.Horizontal"/> for the stack divider, which
	/// is dragged up and down. The default is
	/// <see cref="Microsoft.UI.Xaml.Controls.Orientation.Vertical"/>.
	/// </summary>
	/// <remarks>
	/// The orientation also picks the resize cursor the divider shows while the pointer is over it.
	/// </remarks>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="Orientation"/> dependency property.
	/// </summary>
	public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(
			nameof(Orientation),
			typeof(Orientation),
			typeof(TriPaneViewDivider),
			new FrameworkPropertyMetadata(Orientation.Vertical, OnOrientationChanged));

	/// <summary>
	/// Gets a value indicating whether the divider is currently acting as the restore grip of a
	/// minimized pane. It is set by the owning <see cref="TriPaneView"/> and should be treated as
	/// read-only by everything else. The default template shows a chevron while it is
	/// <see langword="true"/>.
	/// </summary>
	public bool IsRestoreGrip => (bool)GetValue(IsRestoreGripProperty);

	/// <summary>
	/// Identifies the <see cref="IsRestoreGrip"/> dependency property.
	/// </summary>
	public static DependencyProperty IsRestoreGripProperty { get; } =
		DependencyProperty.Register(
			nameof(IsRestoreGrip),
			typeof(bool),
			typeof(TriPaneViewDivider),
			new FrameworkPropertyMetadata(false, OnVisualStatePropertyChanged));

	/// <summary>
	/// Gets a value indicating which way the restore-grip chevron points: <see langword="true"/>
	/// toward the start of the divider's axis - left for a vertical divider, up for a horizontal one
	/// - and <see langword="false"/> toward the end. The chevron points into the minimized pane, so
	/// this follows where that pane sits. It is set by the owning <see cref="TriPaneView"/> and
	/// should be treated as read-only by everything else, and it is meaningless while
	/// <see cref="IsRestoreGrip"/> is <see langword="false"/>.
	/// </summary>
	public bool IsGripTowardStart => (bool)GetValue(IsGripTowardStartProperty);

	/// <summary>
	/// Identifies the <see cref="IsGripTowardStart"/> dependency property.
	/// </summary>
	public static DependencyProperty IsGripTowardStartProperty { get; } =
		DependencyProperty.Register(
			nameof(IsGripTowardStart),
			typeof(bool),
			typeof(TriPaneViewDivider),
			new FrameworkPropertyMetadata(false, OnVisualStatePropertyChanged));

	/// <summary>
	/// Gets or sets the brush the divider paints itself with while the pointer is over it. The
	/// owning <see cref="TriPaneView"/> forwards its
	/// <see cref="TriPaneView.DividerPointerOverBrush"/> here.
	/// </summary>
	public Brush? PointerOverBrush
	{
		get => (Brush?)GetValue(PointerOverBrushProperty);
		set => SetValue(PointerOverBrushProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="PointerOverBrush"/> dependency property.
	/// </summary>
	public static DependencyProperty PointerOverBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverBrush),
			typeof(Brush),
			typeof(TriPaneViewDivider),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the brush the divider paints itself with while it is being dragged. The owning
	/// <see cref="TriPaneView"/> forwards its <see cref="TriPaneView.DividerPressedBrush"/> here.
	/// </summary>
	public Brush? PressedBrush
	{
		get => (Brush?)GetValue(PressedBrushProperty);
		set => SetValue(PressedBrushProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="PressedBrush"/> dependency property.
	/// </summary>
	public static DependencyProperty PressedBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(PressedBrush),
			typeof(Brush),
			typeof(TriPaneViewDivider),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets a value indicating whether the divider is currently being dragged. It is set by the
	/// divider's own pointer handling and should be treated as read-only by everything else.
	/// </summary>
	public bool IsDragging => (bool)GetValue(IsDraggingProperty);

	/// <summary>
	/// Identifies the <see cref="IsDragging"/> dependency property.
	/// </summary>
	public static DependencyProperty IsDraggingProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDragging),
			typeof(bool),
			typeof(TriPaneViewDivider),
			new FrameworkPropertyMetadata(false, OnVisualStatePropertyChanged));

	/// <summary>
	/// Cancels a drag in progress, raising <see cref="DragCompleted"/> with its
	/// <see cref="DragCompletedEventArgs.Canceled"/> flag set. Does nothing when no drag is running.
	/// </summary>
	public void CancelDrag()
	{
		if (IsDragging)
		{
			SetValue(IsDraggingProperty, false);
			ReleaseCapturedPointer();
			RaiseDragCompleted(true);
		}
	}

	/// <inheritdoc />
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();
		UpdateVisualStates(false);
	}

	/// <inheritdoc />
	protected override void OnPointerEntered(PointerRoutedEventArgs e)
	{
		base.OnPointerEntered(e);

		if (IsEnabled)
		{
			_isPointerOver = true;
			UpdateVisualStates(true);
		}
	}

	/// <inheritdoc />
	protected override void OnPointerExited(PointerRoutedEventArgs e)
	{
		base.OnPointerExited(e);

		_isPointerOver = false;
		UpdateVisualStates(true);
	}

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerRoutedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (e.Handled || IsDragging || !IsEnabled)
		{
			return;
		}

		var pointerPoint = e.GetCurrentPoint(null);

		if (!pointerPoint.Properties.IsLeftButtonPressed)
		{
			return;
		}

		if (Parent is not UIElement parent)
		{
			return;
		}

		if (parent.TransformToVisual(null).Inverse is not { } transformToOrigin)
		{
			return;
		}

		e.Handled = true;
		CapturePointer(e.Pointer);
		_capturedPointer = e.Pointer;

		_transformToOrigin = transformToOrigin;
		_origin = _previousPosition = transformToOrigin.TransformPoint(pointerPoint.RawPosition);
		SetValue(IsDraggingProperty, true);

		try
		{
			RaiseDragStarted();
		}
		catch
		{
			CancelDrag();
			throw;
		}
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);

		if (!IsDragging || _transformToOrigin is null)
		{
			return;
		}

		var position = _transformToOrigin.TransformPoint(e.GetCurrentPoint(null).RawPosition);

		if (position.X != _previousPosition.X || position.Y != _previousPosition.Y)
		{
			var horizontalChange = position.X - _previousPosition.X;
			var verticalChange = position.Y - _previousPosition.Y;
			_previousPosition = position;
			RaiseDragDelta(horizontalChange, verticalChange);
		}
	}

	/// <inheritdoc />
	protected override void OnPointerReleased(PointerRoutedEventArgs e)
	{
		base.OnPointerReleased(e);
		EndDrag(e, canceled: false);
	}

	/// <inheritdoc />
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
	{
		base.OnPointerCaptureLost(e);
		EndDrag(e, canceled: false);
	}

	/// <inheritdoc />
	protected override void OnPointerCanceled(PointerRoutedEventArgs e)
	{
		base.OnPointerCanceled(e);
		EndDrag(e, canceled: true);
	}

	/// <summary>
	/// Sets the restore-grip state of the divider. Only the owning <see cref="TriPaneView"/> calls
	/// this; the two properties it writes are meant to be treated as read-only by everything else.
	/// </summary>
	/// <param name="isRestoreGrip">Whether the divider is acting as a restore grip.</param>
	/// <param name="isTowardStart">
	/// Whether the chevron points toward the start of the divider's axis.
	/// </param>
	internal void SetRestoreGripState(bool isRestoreGrip, bool isTowardStart)
	{
		SetValue(IsRestoreGripProperty, isRestoreGrip);
		SetValue(IsGripTowardStartProperty, isTowardStart);
	}

	/// <summary>
	/// Raises <see cref="DragStarted"/> from the divider's current drag origin.
	/// </summary>
	internal void RaiseDragStarted() => DragStarted?.Invoke(this, new DragStartedEventArgs(_origin.X, _origin.Y));

	/// <summary>
	/// Raises <see cref="DragDelta"/> with the supplied change since the previous pointer move.
	/// </summary>
	/// <param name="horizontalChange">The horizontal change, in pixels.</param>
	/// <param name="verticalChange">The vertical change, in pixels.</param>
	internal void RaiseDragDelta(double horizontalChange, double verticalChange)
		=> DragDelta?.Invoke(this, new DragDeltaEventArgs(horizontalChange, verticalChange));

	/// <summary>
	/// Raises <see cref="DragCompleted"/> with the total travel since the drag started.
	/// </summary>
	/// <param name="canceled">Whether the drag was cancelled rather than completed.</param>
	internal void RaiseDragCompleted(bool canceled)
		=> DragCompleted?.Invoke(
			this,
			new DragCompletedEventArgs(_previousPosition.X - _origin.X, _previousPosition.Y - _origin.Y, canceled));

	private static void OnOrientationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var divider = (TriPaneViewDivider)sender;

		divider.UpdateCursor();
		divider.UpdateVisualStates(false);
	}

	private static void OnVisualStatePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TriPaneViewDivider)sender).UpdateVisualStates(true);

	private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
	{
		if (!IsEnabled)
		{
			_isPointerOver = false;
			CancelDrag();
		}

		UpdateCursor();
		UpdateVisualStates(true);
	}

	private void EndDrag(PointerRoutedEventArgs e, bool canceled)
	{
		if (!IsDragging)
		{
			return;
		}

		SetValue(IsDraggingProperty, false);
		_capturedPointer = null;
		ReleasePointerCapture(e.Pointer);
		RaiseDragCompleted(canceled);
	}

	/// <summary>
	/// Gives up the pointer capture the divider took when the drag began, for the paths that end a
	/// drag without a pointer event of their own to release it with.
	/// </summary>
	private void ReleaseCapturedPointer()
	{
		if (_capturedPointer is { } pointer)
		{
			_capturedPointer = null;
			ReleasePointerCapture(pointer);
		}
	}

	private void UpdateCursor()
		=> ProtectedCursor = IsEnabled
			? InputSystemCursor.Create(
				Orientation == Orientation.Vertical
					? InputSystemCursorShape.SizeWestEast
					: InputSystemCursorShape.SizeNorthSouth)
			: null;

	private void UpdateVisualStates(bool useTransitions)
	{
		var commonState = !IsEnabled
			? DisabledStateName
			: IsDragging
				? PressedStateName
				: _isPointerOver
					? PointerOverStateName
					: NormalStateName;

		VisualStateManager.GoToState(this, commonState, useTransitions);

		var isVertical = Orientation == Orientation.Vertical;
		var gripState = !IsRestoreGrip
			? NoGripStateName
			: isVertical
				? IsGripTowardStart ? GripLeftStateName : GripRightStateName
				: IsGripTowardStart ? GripUpStateName : GripDownStateName;

		VisualStateManager.GoToState(this, gripState, useTransitions);
	}
}
