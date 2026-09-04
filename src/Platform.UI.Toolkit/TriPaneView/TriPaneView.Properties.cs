#nullable enable

using System;
using CodeBrix.Platform.UI.Toolkit.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.UI.Toolkit;

public sealed partial class TriPaneView
{
	/// <summary>
	/// Gets or sets the content of the full-height side pane. The default is <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// The element stays in the visual tree while the pane is minimized, so the same instance - with
	/// all of its state - comes back when the pane is restored.
	/// </remarks>
	public UIElement? SidePane
	{
		get => (UIElement?)GetValue(SidePaneProperty);
		set => SetValue(SidePaneProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SidePane"/> dependency property.
	/// </summary>
	public static DependencyProperty SidePaneProperty { get; } =
		DependencyProperty.Register(
			nameof(SidePane),
			typeof(UIElement),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the content of the upper pane of the stack. The default is
	/// <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// The element stays in the visual tree while the pane is minimized, so the same instance - with
	/// all of its state - comes back when the pane is restored.
	/// </remarks>
	public UIElement? UpperPane
	{
		get => (UIElement?)GetValue(UpperPaneProperty);
		set => SetValue(UpperPaneProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="UpperPane"/> dependency property.
	/// </summary>
	public static DependencyProperty UpperPaneProperty { get; } =
		DependencyProperty.Register(
			nameof(UpperPane),
			typeof(UIElement),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the content of the lower pane of the stack. The default is
	/// <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// The element stays in the visual tree while the pane is minimized, so the same instance - with
	/// all of its state - comes back when the pane is restored.
	/// </remarks>
	public UIElement? LowerPane
	{
		get => (UIElement?)GetValue(LowerPaneProperty);
		set => SetValue(LowerPaneProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="LowerPane"/> dependency property.
	/// </summary>
	public static DependencyProperty LowerPaneProperty { get; } =
		DependencyProperty.Register(
			nameof(LowerPane),
			typeof(UIElement),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets which edge the side pane occupies. The default is
	/// <see cref="TriPaneViewSidePanePlacement.Left"/>.
	/// </summary>
	/// <remarks>
	/// The default template's two outer columns are POSITIONAL: <c>PART_SideColumn</c> is the leading
	/// column and <c>PART_StackColumn</c> the trailing one, and the side pane and the stack swap
	/// between them under <see cref="TriPaneViewSidePanePlacement.Right"/> - so under that placement
	/// the column named <c>PART_SideColumn</c> is the one holding the stack. Anyone re-templating the
	/// control has to keep that arrangement.
	/// </remarks>
	public TriPaneViewSidePanePlacement SidePanePlacement
	{
		get => (TriPaneViewSidePanePlacement)GetValue(SidePanePlacementProperty);
		set => SetValue(SidePanePlacementProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SidePanePlacement"/> dependency property.
	/// </summary>
	public static DependencyProperty SidePanePlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(SidePanePlacement),
			typeof(TriPaneViewSidePanePlacement),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewSidePanePlacement.Left, OnPlacementPropertyChanged));

	/// <summary>
	/// Gets or sets the width weight of the side pane, on a nominal 0-to-100 scale. It is paired
	/// with <see cref="StackPercent"/>: the two are used as star weights, so only their ratio
	/// matters - <c>60</c> and <c>60</c> lay out the same as <c>50</c> and <c>50</c>, and <c>40</c>
	/// and <c>120</c> lay out as a quarter and three quarters. The default is <c>33.3</c>.
	/// </summary>
	/// <remarks>
	/// Negative values and <see cref="double.NaN"/> are treated as zero, and a weight of zero means
	/// the pane is minimized. When both weights of the pair are zero the pair is laid out evenly and
	/// neither member is minimized. A drag of the side divider writes this property and
	/// <see cref="StackPercent"/> back normalized to sum to 100.
	/// </remarks>
	public double SidePanePercent
	{
		get => (double)GetValue(SidePanePercentProperty);
		set => SetValue(SidePanePercentProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SidePanePercent"/> dependency property.
	/// </summary>
	public static DependencyProperty SidePanePercentProperty { get; } =
		DependencyProperty.Register(
			nameof(SidePanePercent),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewLayoutMath.DefaultSidePanePercent, OnWeightPropertyChanged));

	/// <summary>
	/// Gets or sets the width weight of the stack - the column holding the upper and lower panes -
	/// on a nominal 0-to-100 scale. It is paired with <see cref="SidePanePercent"/>. The default is
	/// <c>66.7</c>.
	/// </summary>
	/// <remarks>
	/// A weight of zero minimizes the whole stack, which minimizes the upper and lower panes with
	/// it. See <see cref="SidePanePercent"/> for how a pair of weights is normalized.
	/// </remarks>
	public double StackPercent
	{
		get => (double)GetValue(StackPercentProperty);
		set => SetValue(StackPercentProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="StackPercent"/> dependency property.
	/// </summary>
	public static DependencyProperty StackPercentProperty { get; } =
		DependencyProperty.Register(
			nameof(StackPercent),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewLayoutMath.DefaultStackPercent, OnWeightPropertyChanged));

	/// <summary>
	/// Gets or sets the height weight of the upper pane, on a nominal 0-to-100 scale. It is paired
	/// with <see cref="LowerPanePercent"/>. The default is <c>50</c>.
	/// </summary>
	/// <remarks>
	/// See <see cref="SidePanePercent"/> for how a pair of weights is normalized.
	/// </remarks>
	public double UpperPanePercent
	{
		get => (double)GetValue(UpperPanePercentProperty);
		set => SetValue(UpperPanePercentProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="UpperPanePercent"/> dependency property.
	/// </summary>
	public static DependencyProperty UpperPanePercentProperty { get; } =
		DependencyProperty.Register(
			nameof(UpperPanePercent),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewLayoutMath.DefaultUpperPanePercent, OnWeightPropertyChanged));

	/// <summary>
	/// Gets or sets the height weight of the lower pane, on a nominal 0-to-100 scale. It is paired
	/// with <see cref="UpperPanePercent"/>. The default is <c>50</c>.
	/// </summary>
	/// <remarks>
	/// See <see cref="SidePanePercent"/> for how a pair of weights is normalized.
	/// </remarks>
	public double LowerPanePercent
	{
		get => (double)GetValue(LowerPanePercentProperty);
		set => SetValue(LowerPanePercentProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="LowerPanePercent"/> dependency property.
	/// </summary>
	public static DependencyProperty LowerPanePercentProperty { get; } =
		DependencyProperty.Register(
			nameof(LowerPanePercent),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewLayoutMath.DefaultLowerPanePercent, OnWeightPropertyChanged));

	/// <summary>
	/// Gets or sets the smallest width, in pixels, the side pane may be given while it is open. The
	/// default is <c>0</c>.
	/// </summary>
	/// <remarks>
	/// This is a hard floor: layout honors it and a divider drag stops at it. What happens when a
	/// drag asks for less depends on <see cref="IsDragToMinimizeEnabled"/>.
	/// </remarks>
	public double SidePaneMinLength
	{
		get => (double)GetValue(SidePaneMinLengthProperty);
		set => SetValue(SidePaneMinLengthProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SidePaneMinLength"/> dependency property.
	/// </summary>
	public static DependencyProperty SidePaneMinLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(SidePaneMinLength),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(0d, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets the smallest width, in pixels, the stack may be given while it is open. The
	/// default is <c>0</c>.
	/// </summary>
	/// <remarks>
	/// This is a hard floor: layout honors it and a divider drag stops at it. What happens when a
	/// drag asks for less depends on <see cref="IsDragToMinimizeEnabled"/>.
	/// </remarks>
	public double StackMinLength
	{
		get => (double)GetValue(StackMinLengthProperty);
		set => SetValue(StackMinLengthProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="StackMinLength"/> dependency property.
	/// </summary>
	public static DependencyProperty StackMinLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(StackMinLength),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(0d, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets the smallest height, in pixels, the upper pane may be given while it is open.
	/// The default is <c>0</c>.
	/// </summary>
	/// <remarks>
	/// This is a hard floor: layout honors it and a divider drag stops at it. What happens when a
	/// drag asks for less depends on <see cref="IsDragToMinimizeEnabled"/>.
	/// </remarks>
	public double UpperPaneMinLength
	{
		get => (double)GetValue(UpperPaneMinLengthProperty);
		set => SetValue(UpperPaneMinLengthProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="UpperPaneMinLength"/> dependency property.
	/// </summary>
	public static DependencyProperty UpperPaneMinLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(UpperPaneMinLength),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(0d, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets the smallest height, in pixels, the lower pane may be given while it is open.
	/// The default is <c>0</c>.
	/// </summary>
	/// <remarks>
	/// This is a hard floor: layout honors it and a divider drag stops at it. What happens when a
	/// drag asks for less depends on <see cref="IsDragToMinimizeEnabled"/>.
	/// </remarks>
	public double LowerPaneMinLength
	{
		get => (double)GetValue(LowerPaneMinLengthProperty);
		set => SetValue(LowerPaneMinLengthProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="LowerPaneMinLength"/> dependency property.
	/// </summary>
	public static DependencyProperty LowerPaneMinLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(LowerPaneMinLength),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(0d, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets a value indicating whether the user may drag the side divider. The default is
	/// <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// Setting this to <see langword="false"/> stops the divider being dragged, but a divider that
	/// is currently a restore grip can still be clicked to restore its pane. Use
	/// <see cref="TriPaneViewRestoreGripMode.Never"/> to take that away as well.
	/// </remarks>
	public bool CanUserDragSideDivider
	{
		get => (bool)GetValue(CanUserDragSideDividerProperty);
		set => SetValue(CanUserDragSideDividerProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="CanUserDragSideDivider"/> dependency property.
	/// </summary>
	public static DependencyProperty CanUserDragSideDividerProperty { get; } =
		DependencyProperty.Register(
			nameof(CanUserDragSideDivider),
			typeof(bool),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(true, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets a value indicating whether the user may drag the stack divider. The default is
	/// <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// Setting this to <see langword="false"/> stops the divider being dragged, but a divider that
	/// is currently a restore grip can still be clicked to restore its pane. Use
	/// <see cref="TriPaneViewRestoreGripMode.Never"/> to take that away as well.
	/// </remarks>
	public bool CanUserDragStackDivider
	{
		get => (bool)GetValue(CanUserDragStackDividerProperty);
		set => SetValue(CanUserDragStackDividerProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="CanUserDragStackDivider"/> dependency property.
	/// </summary>
	public static DependencyProperty CanUserDragStackDividerProperty { get; } =
		DependencyProperty.Register(
			nameof(CanUserDragStackDivider),
			typeof(bool),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(true, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets the thickness, in pixels, of each divider: the width of the side divider and the
	/// height of the stack divider. The default is <c>6</c>.
	/// </summary>
	/// <remarks>
	/// A hidden divider takes no space at all, whatever this is set to.
	/// </remarks>
	public double DividerThickness
	{
		get => (double)GetValue(DividerThicknessProperty);
		set => SetValue(DividerThicknessProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="DividerThickness"/> dependency property.
	/// </summary>
	public static DependencyProperty DividerThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(DividerThickness),
			typeof(double),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(DefaultDividerThickness, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets the brush the dividers paint themselves with at rest. The default style binds it
	/// to the <c>TriPaneViewDividerBrush</c> theme resource.
	/// </summary>
	public Brush? DividerBrush
	{
		get => (Brush?)GetValue(DividerBrushProperty);
		set => SetValue(DividerBrushProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="DividerBrush"/> dependency property.
	/// </summary>
	public static DependencyProperty DividerBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(DividerBrush),
			typeof(Brush),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the brush the dividers paint themselves with while the pointer is over them. The
	/// default style binds it to the <c>TriPaneViewDividerPointerOverBrush</c> theme resource.
	/// </summary>
	public Brush? DividerPointerOverBrush
	{
		get => (Brush?)GetValue(DividerPointerOverBrushProperty);
		set => SetValue(DividerPointerOverBrushProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="DividerPointerOverBrush"/> dependency property.
	/// </summary>
	public static DependencyProperty DividerPointerOverBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(DividerPointerOverBrush),
			typeof(Brush),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the brush the dividers paint themselves with while they are being dragged. The
	/// default style binds it to the <c>TriPaneViewDividerPressedBrush</c> theme resource.
	/// </summary>
	public Brush? DividerPressedBrush
	{
		get => (Brush?)GetValue(DividerPressedBrushProperty);
		set => SetValue(DividerPressedBrushProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="DividerPressedBrush"/> dependency property.
	/// </summary>
	public static DependencyProperty DividerPressedBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(DividerPressedBrush),
			typeof(Brush),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value indicating what a drag does when it asks a pane for less than its
	/// minimum length. The default is <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// While this is <see langword="false"/> the drag simply stops at the minimum length. A pane
	/// whose minimum length is <c>0</c> can still be dragged all the way to zero and is minimized
	/// when it gets there.
	/// </para>
	/// <para>
	/// While this is <see langword="true"/> a drag that asks for less than the minimum length snaps
	/// the pane to zero, minimizing it, and dragging back past the minimum reopens the pane at its
	/// minimum length. The pane is never left at an in-between size.
	/// </para>
	/// </remarks>
	public bool IsDragToMinimizeEnabled
	{
		get => (bool)GetValue(IsDragToMinimizeEnabledProperty);
		set => SetValue(IsDragToMinimizeEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="IsDragToMinimizeEnabled"/> dependency property.
	/// </summary>
	public static DependencyProperty IsDragToMinimizeEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsDragToMinimizeEnabled),
			typeof(bool),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets when a minimized pane keeps a visible restore grip. The default is
	/// <see cref="TriPaneViewRestoreGripMode.Auto"/>.
	/// </summary>
	public TriPaneViewRestoreGripMode RestoreGripMode
	{
		get => (TriPaneViewRestoreGripMode)GetValue(RestoreGripModeProperty);
		set => SetValue(RestoreGripModeProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="RestoreGripMode"/> dependency property.
	/// </summary>
	public static DependencyProperty RestoreGripModeProperty { get; } =
		DependencyProperty.Register(
			nameof(RestoreGripMode),
			typeof(TriPaneViewRestoreGripMode),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewRestoreGripMode.Auto, OnLayoutPropertyChanged));

	/// <summary>
	/// Gets or sets a value indicating whether the side pane is minimized. Setting it to
	/// <see langword="true"/> is exactly <see cref="MinimizeSidePane"/> and setting it to
	/// <see langword="false"/> is exactly <see cref="RestoreSidePane"/>.
	/// </summary>
	/// <remarks>
	/// A minimized pane keeps its content element in the visual tree - the column is simply given
	/// zero width - so the same instance, with its scroll position, text and focus state, is shown
	/// again when the pane is restored. A request that would leave no pane open is ignored and the
	/// property is set straight back to <see langword="false"/>.
	/// </remarks>
	public bool IsSidePaneMinimized
	{
		get => (bool)GetValue(IsSidePaneMinimizedProperty);
		set => SetValue(IsSidePaneMinimizedProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="IsSidePaneMinimized"/> dependency property.
	/// </summary>
	public static DependencyProperty IsSidePaneMinimizedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsSidePaneMinimized),
			typeof(bool),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(false, OnIsSidePaneMinimizedChanged));

	/// <summary>
	/// Gets or sets a value indicating whether the upper pane is minimized. Setting it to
	/// <see langword="true"/> is exactly <see cref="MinimizeUpperPane"/> and setting it to
	/// <see langword="false"/> is exactly <see cref="RestoreUpperPane"/>.
	/// </summary>
	/// <remarks>
	/// The upper pane also counts as minimized while the whole stack is minimized. A minimized pane
	/// keeps its content element in the visual tree - the row is simply given zero height - so the
	/// same instance, with all of its state, is shown again when the pane is restored. A request
	/// that would leave no pane open is ignored and the property is set straight back to
	/// <see langword="false"/>.
	/// </remarks>
	public bool IsUpperPaneMinimized
	{
		get => (bool)GetValue(IsUpperPaneMinimizedProperty);
		set => SetValue(IsUpperPaneMinimizedProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="IsUpperPaneMinimized"/> dependency property.
	/// </summary>
	public static DependencyProperty IsUpperPaneMinimizedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsUpperPaneMinimized),
			typeof(bool),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(false, OnIsUpperPaneMinimizedChanged));

	/// <summary>
	/// Gets or sets a value indicating whether the lower pane is minimized. Setting it to
	/// <see langword="true"/> is exactly <see cref="MinimizeLowerPane"/> and setting it to
	/// <see langword="false"/> is exactly <see cref="RestoreLowerPane"/>.
	/// </summary>
	/// <remarks>
	/// The lower pane also counts as minimized while the whole stack is minimized. A minimized pane
	/// keeps its content element in the visual tree - the row is simply given zero height - so the
	/// same instance, with all of its state, is shown again when the pane is restored. A request
	/// that would leave no pane open is ignored and the property is set straight back to
	/// <see langword="false"/>.
	/// </remarks>
	public bool IsLowerPaneMinimized
	{
		get => (bool)GetValue(IsLowerPaneMinimizedProperty);
		set => SetValue(IsLowerPaneMinimizedProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="IsLowerPaneMinimized"/> dependency property.
	/// </summary>
	public static DependencyProperty IsLowerPaneMinimizedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsLowerPaneMinimized),
			typeof(bool),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(false, OnIsLowerPaneMinimizedChanged));

	/// <summary>
	/// Gets or sets the vertical scroll bar visibility of the side pane. The default is
	/// <see cref="ScrollBarVisibility.Auto"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="ScrollBarVisibility.Disabled"/> also turns vertical scrolling off, so the pane's
	/// content is measured to the pane height and stretches to fill it.
	/// </remarks>
	public ScrollBarVisibility SidePaneVerticalScrollBarVisibility
	{
		get => (ScrollBarVisibility)GetValue(SidePaneVerticalScrollBarVisibilityProperty);
		set => SetValue(SidePaneVerticalScrollBarVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SidePaneVerticalScrollBarVisibility"/> dependency property.
	/// </summary>
	public static DependencyProperty SidePaneVerticalScrollBarVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(SidePaneVerticalScrollBarVisibility),
			typeof(ScrollBarVisibility),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(ScrollBarVisibility.Auto, OnScrollPropertyChanged));

	/// <summary>
	/// Gets or sets the vertical scroll bar visibility of the upper pane. The default is
	/// <see cref="ScrollBarVisibility.Auto"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="ScrollBarVisibility.Disabled"/> also turns vertical scrolling off, so the pane's
	/// content is measured to the pane height and stretches to fill it.
	/// </remarks>
	public ScrollBarVisibility UpperPaneVerticalScrollBarVisibility
	{
		get => (ScrollBarVisibility)GetValue(UpperPaneVerticalScrollBarVisibilityProperty);
		set => SetValue(UpperPaneVerticalScrollBarVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="UpperPaneVerticalScrollBarVisibility"/> dependency property.
	/// </summary>
	public static DependencyProperty UpperPaneVerticalScrollBarVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(UpperPaneVerticalScrollBarVisibility),
			typeof(ScrollBarVisibility),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(ScrollBarVisibility.Auto, OnScrollPropertyChanged));

	/// <summary>
	/// Gets or sets the vertical scroll bar visibility of the lower pane. The default is
	/// <see cref="ScrollBarVisibility.Auto"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="ScrollBarVisibility.Disabled"/> also turns vertical scrolling off, so the pane's
	/// content is measured to the pane height and stretches to fill it.
	/// </remarks>
	public ScrollBarVisibility LowerPaneVerticalScrollBarVisibility
	{
		get => (ScrollBarVisibility)GetValue(LowerPaneVerticalScrollBarVisibilityProperty);
		set => SetValue(LowerPaneVerticalScrollBarVisibilityProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="LowerPaneVerticalScrollBarVisibility"/> dependency property.
	/// </summary>
	public static DependencyProperty LowerPaneVerticalScrollBarVisibilityProperty { get; } =
		DependencyProperty.Register(
			nameof(LowerPaneVerticalScrollBarVisibility),
			typeof(ScrollBarVisibility),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(ScrollBarVisibility.Auto, OnScrollPropertyChanged));

	/// <summary>
	/// Gets or sets how the side pane scrolls horizontally. The default is
	/// <see cref="TriPaneViewHorizontalScrollMode.Disabled"/>.
	/// </summary>
	public TriPaneViewHorizontalScrollMode SidePaneHorizontalScrollMode
	{
		get => (TriPaneViewHorizontalScrollMode)GetValue(SidePaneHorizontalScrollModeProperty);
		set => SetValue(SidePaneHorizontalScrollModeProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="SidePaneHorizontalScrollMode"/> dependency property.
	/// </summary>
	public static DependencyProperty SidePaneHorizontalScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(SidePaneHorizontalScrollMode),
			typeof(TriPaneViewHorizontalScrollMode),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewHorizontalScrollMode.Disabled, OnScrollPropertyChanged));

	/// <summary>
	/// Gets or sets how the upper pane scrolls horizontally. The default is
	/// <see cref="TriPaneViewHorizontalScrollMode.Disabled"/>.
	/// </summary>
	public TriPaneViewHorizontalScrollMode UpperPaneHorizontalScrollMode
	{
		get => (TriPaneViewHorizontalScrollMode)GetValue(UpperPaneHorizontalScrollModeProperty);
		set => SetValue(UpperPaneHorizontalScrollModeProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="UpperPaneHorizontalScrollMode"/> dependency property.
	/// </summary>
	public static DependencyProperty UpperPaneHorizontalScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(UpperPaneHorizontalScrollMode),
			typeof(TriPaneViewHorizontalScrollMode),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewHorizontalScrollMode.Disabled, OnScrollPropertyChanged));

	/// <summary>
	/// Gets or sets how the lower pane scrolls horizontally. The default is
	/// <see cref="TriPaneViewHorizontalScrollMode.Disabled"/>.
	/// </summary>
	public TriPaneViewHorizontalScrollMode LowerPaneHorizontalScrollMode
	{
		get => (TriPaneViewHorizontalScrollMode)GetValue(LowerPaneHorizontalScrollModeProperty);
		set => SetValue(LowerPaneHorizontalScrollModeProperty, value);
	}

	/// <summary>
	/// Identifies the <see cref="LowerPaneHorizontalScrollMode"/> dependency property.
	/// </summary>
	public static DependencyProperty LowerPaneHorizontalScrollModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LowerPaneHorizontalScrollMode),
			typeof(TriPaneViewHorizontalScrollMode),
			typeof(TriPaneView),
			new FrameworkPropertyMetadata(TriPaneViewHorizontalScrollMode.Disabled, OnScrollPropertyChanged));

	/// <summary>
	/// Occurs when the user finishes interacting with either divider. The pane weights have already
	/// been written back, normalized to sum to 100 on the affected axis, so a handler can persist
	/// them straight away.
	/// </summary>
	public event EventHandler<TriPaneViewDividerDragCompletedEventArgs>? DividerDragCompleted;

	private static void OnWeightPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TriPaneView)sender).OnWeightChanged();

	private static void OnLayoutPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TriPaneView)sender).UpdateState();

	private static void OnPlacementPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TriPaneView)sender;
		owner.ApplyPlacement();
		owner.UpdateState();
	}

	private static void OnScrollPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TriPaneView)sender).ApplyScrollSettings();

	private static void OnIsSidePaneMinimizedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TriPaneView)sender).OnMinimizedFlagChanged(TriPaneViewRegion.Side, (bool)args.NewValue);

	private static void OnIsUpperPaneMinimizedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TriPaneView)sender).OnMinimizedFlagChanged(TriPaneViewRegion.Upper, (bool)args.NewValue);

	private static void OnIsLowerPaneMinimizedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		=> ((TriPaneView)sender).OnMinimizedFlagChanged(TriPaneViewRegion.Lower, (bool)args.NewValue);
}
