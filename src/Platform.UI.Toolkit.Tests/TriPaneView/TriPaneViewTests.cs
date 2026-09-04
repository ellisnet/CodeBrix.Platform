using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests;

[TestClass]
public class TriPaneViewTests
{
	private const double Tolerance = 1e-9;
	private const double DefaultSidePercent = 33.3d;
	private const double DefaultStackPercent = 66.7d;

	[TestMethod]
	public void SidePanePercent_defaults_to_a_third_of_the_width() =>
		new TriPaneView().SidePanePercent.Should().Be(DefaultSidePercent);

	[TestMethod]
	public void StackPercent_defaults_to_two_thirds_of_the_width() =>
		new TriPaneView().StackPercent.Should().Be(DefaultStackPercent);

	[TestMethod]
	public void UpperPanePercent_defaults_to_half_the_stack() =>
		new TriPaneView().UpperPanePercent.Should().Be(50d);

	[TestMethod]
	public void LowerPanePercent_defaults_to_half_the_stack() =>
		new TriPaneView().LowerPanePercent.Should().Be(50d);

	[TestMethod]
	public void SidePanePlacement_defaults_to_left() =>
		new TriPaneView().SidePanePlacement.Should().Be(TriPaneViewSidePanePlacement.Left);

	[TestMethod]
	public void DividerThickness_defaults_to_six_pixels() =>
		new TriPaneView().DividerThickness.Should().Be(6d);

	[TestMethod]
	public void RestoreGripMode_defaults_to_auto() =>
		new TriPaneView().RestoreGripMode.Should().Be(TriPaneViewRestoreGripMode.Auto);

	[TestMethod]
	public void IsDragToMinimizeEnabled_defaults_to_false() =>
		new TriPaneView().IsDragToMinimizeEnabled.Should().BeFalse();

	[TestMethod]
	public void CanUserDragSideDivider_defaults_to_true() =>
		new TriPaneView().CanUserDragSideDivider.Should().BeTrue();

	[TestMethod]
	public void CanUserDragStackDivider_defaults_to_true() =>
		new TriPaneView().CanUserDragStackDivider.Should().BeTrue();

	[TestMethod]
	public void SidePaneMinLength_and_the_other_minimums_default_to_zero()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.SidePaneMinLength.Should().Be(0d);
		view.StackMinLength.Should().Be(0d);
		view.UpperPaneMinLength.Should().Be(0d);
		view.LowerPaneMinLength.Should().Be(0d);
	}

	[TestMethod]
	public void SidePaneVerticalScrollBarVisibility_and_the_others_default_to_auto()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.SidePaneVerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
		view.UpperPaneVerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
		view.LowerPaneVerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
	}

	[TestMethod]
	public void SidePaneHorizontalScrollMode_and_the_others_default_to_disabled()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.SidePaneHorizontalScrollMode.Should().Be(TriPaneViewHorizontalScrollMode.Disabled);
		view.UpperPaneHorizontalScrollMode.Should().Be(TriPaneViewHorizontalScrollMode.Disabled);
		view.LowerPaneHorizontalScrollMode.Should().Be(TriPaneViewHorizontalScrollMode.Disabled);
	}

	[TestMethod]
	public void SidePane_and_the_other_panes_default_to_null()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.SidePane.Should().BeNull();
		view.UpperPane.Should().BeNull();
		view.LowerPane.Should().BeNull();
	}

	[TestMethod]
	public void IsSidePaneMinimized_and_the_others_default_to_false()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.IsLowerPaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void SidePaneEffectiveWeight_defaults_to_the_normalized_share()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.SidePaneEffectiveWeight.Should().BeApproximately(DefaultSidePercent, Tolerance);
		view.StackEffectiveWeight.Should().BeApproximately(DefaultStackPercent, Tolerance);
		view.UpperPaneEffectiveWeight.Should().BeApproximately(50d, Tolerance);
		view.LowerPaneEffectiveWeight.Should().BeApproximately(50d, Tolerance);
	}

	[TestMethod]
	public void IsSideDividerVisible_defaults_to_true()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.IsSideDividerVisible.Should().BeTrue();
		view.IsStackDividerVisible.Should().BeTrue();
		view.IsSideRestoreGripVisible.Should().BeFalse();
		view.IsStackRestoreGripVisible.Should().BeFalse();
	}

	[TestMethod]
	public void SidePanePercent_equal_weights_produce_an_even_effective_split()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 60d, StackPercent = 60d };

		//Assert
		view.SidePaneEffectiveWeight.Should().BeApproximately(50d, Tolerance);
		view.StackEffectiveWeight.Should().BeApproximately(50d, Tolerance);
	}

	[TestMethod]
	public void SidePanePercent_unequal_weights_keep_their_ratio()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 40d, StackPercent = 120d };

		//Assert
		view.SidePaneEffectiveWeight.Should().BeApproximately(25d, Tolerance);
		view.StackEffectiveWeight.Should().BeApproximately(75d, Tolerance);
	}

	[TestMethod]
	public void SidePanePercent_zero_minimizes_the_side_pane()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Assert
		view.IsSidePaneMinimized.Should().BeTrue();
		view.SidePaneEffectiveWeight.Should().Be(0d);
	}

	[TestMethod]
	public void SidePanePercent_negative_value_minimizes_the_side_pane() =>
		new TriPaneView { SidePanePercent = -20d }.IsSidePaneMinimized.Should().BeTrue();

	[TestMethod]
	public void SidePanePercent_nan_minimizes_the_side_pane() =>
		new TriPaneView { SidePanePercent = double.NaN }.IsSidePaneMinimized.Should().BeTrue();

	[TestMethod]
	public void SidePanePercent_zero_alongside_a_zero_stack_minimizes_nothing()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d, StackPercent = 0d };

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.IsLowerPaneMinimized.Should().BeFalse();
		view.SidePaneEffectiveWeight.Should().Be(50d);
		view.StackEffectiveWeight.Should().Be(50d);
	}

	[TestMethod]
	public void StackPercent_zero_minimizes_both_stack_panes()
	{
		//Arrange
		var view = new TriPaneView { StackPercent = 0d };

		//Assert
		view.IsUpperPaneMinimized.Should().BeTrue();
		view.IsLowerPaneMinimized.Should().BeTrue();
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void UpperPanePercent_zero_minimizes_only_the_upper_pane()
	{
		//Arrange
		var view = new TriPaneView { UpperPanePercent = 0d };

		//Assert
		view.IsUpperPaneMinimized.Should().BeTrue();
		view.IsLowerPaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void MinimizeSidePane_minimizes_the_side_pane()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeSidePane();

		//Assert
		view.IsSidePaneMinimized.Should().BeTrue();
		view.SidePanePercent.Should().Be(0d);
	}

	[TestMethod]
	public void MinimizeSidePane_leaves_no_restore_grip_under_auto()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeSidePane();

		//Assert
		view.IsSideRestoreGripVisible.Should().BeFalse();
		view.IsSideDividerVisible.Should().BeFalse();
	}

	[TestMethod]
	public void MinimizeSidePane_leaves_a_restore_grip_under_always()
	{
		//Arrange
		var view = new TriPaneView { RestoreGripMode = TriPaneViewRestoreGripMode.Always };

		//Act
		view.MinimizeSidePane();

		//Assert
		view.IsSideRestoreGripVisible.Should().BeTrue();
		view.IsSideDividerVisible.Should().BeTrue();
	}

	[TestMethod]
	public void SidePanePercent_zero_leaves_a_restore_grip_under_auto()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Assert
		view.IsSideRestoreGripVisible.Should().BeTrue();
		view.IsSideDividerVisible.Should().BeTrue();
		view.IsSideRestoreGripTowardStart.Should().BeTrue();
	}

	[TestMethod]
	public void SidePanePercent_zero_leaves_no_restore_grip_under_never()
	{
		//Arrange
		var view = new TriPaneView
		{
			RestoreGripMode = TriPaneViewRestoreGripMode.Never,
			SidePanePercent = 0d
		};

		//Assert
		view.IsSideRestoreGripVisible.Should().BeFalse();
		view.IsSideDividerVisible.Should().BeFalse();
	}

	[TestMethod]
	public void SidePanePlacement_right_points_the_restore_grip_the_other_way()
	{
		//Arrange
		var view = new TriPaneView
		{
			SidePanePlacement = TriPaneViewSidePanePlacement.Right,
			SidePanePercent = 0d
		};

		//Assert
		view.IsSideRestoreGripVisible.Should().BeTrue();
		view.IsSideRestoreGripTowardStart.Should().BeFalse();
	}

	[TestMethod]
	public void StackPercent_zero_turns_the_side_divider_into_the_stack_grip()
	{
		//Arrange
		var view = new TriPaneView { StackPercent = 0d };

		//Assert
		view.IsSideRestoreGripVisible.Should().BeTrue();
		view.IsSideRestoreGripTowardStart.Should().BeFalse();
		view.IsStackDividerVisible.Should().BeFalse();
	}

	[TestMethod]
	public void UpperPanePercent_zero_points_the_stack_grip_upward()
	{
		//Arrange
		var view = new TriPaneView { UpperPanePercent = 0d };

		//Assert
		view.IsStackRestoreGripVisible.Should().BeTrue();
		view.IsStackRestoreGripTowardStart.Should().BeTrue();
		view.IsStackDividerVisible.Should().BeTrue();
	}

	[TestMethod]
	public void LowerPanePercent_zero_points_the_stack_grip_downward()
	{
		//Arrange
		var view = new TriPaneView { LowerPanePercent = 0d };

		//Assert
		view.IsStackRestoreGripVisible.Should().BeTrue();
		view.IsStackRestoreGripTowardStart.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreSidePane_restores_the_snapshotted_weight()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 25d };

		//Act
		view.MinimizeSidePane();
		view.RestoreSidePane();

		//Assert
		view.SidePanePercent.Should().Be(25d);
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreSidePane_uses_the_default_weight_when_there_is_no_snapshot()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Act
		view.RestoreSidePane();

		//Assert
		view.SidePanePercent.Should().Be(DefaultSidePercent);
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreSidePane_does_nothing_when_the_pane_is_already_open()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 25d };

		//Act
		view.RestoreSidePane();

		//Assert
		view.SidePanePercent.Should().Be(25d);
	}

	[TestMethod]
	public void MinimizeUpperPane_minimizes_only_the_upper_pane()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeUpperPane();

		//Assert
		view.IsUpperPaneMinimized.Should().BeTrue();
		view.IsLowerPaneMinimized.Should().BeFalse();
		view.StackPercent.Should().Be(DefaultStackPercent);
	}

	[TestMethod]
	public void MinimizeLowerPane_after_the_upper_pane_minimizes_the_whole_stack()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeUpperPane();
		view.MinimizeLowerPane();

		//Assert
		view.StackPercent.Should().Be(0d);
		view.IsUpperPaneMinimized.Should().BeTrue();
		view.IsLowerPaneMinimized.Should().BeTrue();
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void MinimizeUpperPane_is_ignored_when_it_would_leave_no_pane_open()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeSidePane();
		view.MinimizeLowerPane();
		view.MinimizeUpperPane();

		//Assert
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.UpperPanePercent.Should().Be(50d);
	}

	[TestMethod]
	public void MinimizeSidePane_is_ignored_when_the_stack_is_already_minimized()
	{
		//Arrange
		var view = new TriPaneView { StackPercent = 0d };

		//Act
		view.MinimizeSidePane();

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.SidePanePercent.Should().Be(DefaultSidePercent);
	}

	[TestMethod]
	public void RestoreUpperPane_restores_the_stack_and_leaves_the_lower_pane_minimized()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeLowerPane();
		view.MinimizeUpperPane();
		view.RestoreUpperPane();

		//Assert
		view.StackPercent.Should().Be(DefaultStackPercent);
		view.UpperPanePercent.Should().Be(50d);
		view.LowerPanePercent.Should().Be(0d);
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.IsLowerPaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void RestoreLowerPane_restores_the_stack_and_leaves_the_upper_pane_minimized()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeUpperPane();
		view.MinimizeLowerPane();
		view.RestoreLowerPane();

		//Assert
		view.StackPercent.Should().Be(DefaultStackPercent);
		view.LowerPanePercent.Should().Be(50d);
		view.UpperPanePercent.Should().Be(0d);
		view.IsLowerPaneMinimized.Should().BeFalse();
		view.IsUpperPaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void RestoreAll_restores_every_minimized_region()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeUpperPane();
		view.MinimizeLowerPane();
		view.MinimizeSidePane();
		view.RestoreAll();

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.IsLowerPaneMinimized.Should().BeFalse();
		view.SidePanePercent.Should().Be(DefaultSidePercent);
		view.StackPercent.Should().Be(DefaultStackPercent);
		view.UpperPanePercent.Should().Be(50d);
		view.LowerPanePercent.Should().Be(50d);
	}

	[TestMethod]
	public void IsSidePaneMinimized_setting_true_minimizes_the_pane()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.IsSidePaneMinimized = true;

		//Assert
		view.SidePanePercent.Should().Be(0d);
		view.IsSidePaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void IsSidePaneMinimized_setting_false_restores_the_pane()
	{
		//Arrange
		var view = new TriPaneView();
		view.IsSidePaneMinimized = true;

		//Act
		view.IsSidePaneMinimized = false;

		//Assert
		view.SidePanePercent.Should().Be(DefaultSidePercent);
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void IsSidePaneMinimized_hides_the_grip_exactly_as_the_minimize_method_does()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.IsSidePaneMinimized = true;

		//Assert
		view.IsSideRestoreGripVisible.Should().BeFalse();
	}

	[TestMethod]
	public void IsUpperPaneMinimized_is_coerced_back_when_the_request_is_ignored()
	{
		//Arrange
		var view = new TriPaneView();
		view.MinimizeSidePane();
		view.MinimizeLowerPane();

		//Act
		view.IsUpperPaneMinimized = true;

		//Assert
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.UpperPanePercent.Should().Be(50d);
	}

	[TestMethod]
	public void SidePane_keeps_the_same_element_instance_across_a_minimize_and_restore()
	{
		//Arrange
		var content = new Border();
		var view = new TriPaneView { SidePane = content };

		//Act
		view.MinimizeSidePane();
		var whileMinimized = view.SidePane;
		view.RestoreSidePane();

		//Assert
		whileMinimized.Should().BeSameAs(content);
		view.SidePane.Should().BeSameAs(content);
	}

	[TestMethod]
	public void UpperPane_keeps_the_same_element_instance_across_a_stack_collapse()
	{
		//Arrange
		var upper = new Border();
		var lower = new Border();
		var view = new TriPaneView { UpperPane = upper, LowerPane = lower };

		//Act
		view.MinimizeUpperPane();
		view.MinimizeLowerPane();
		view.RestoreAll();

		//Assert
		view.UpperPane.Should().BeSameAs(upper);
		view.LowerPane.Should().BeSameAs(lower);
	}

	[TestMethod]
	public void SidePanePlacement_left_puts_the_side_pane_in_the_leading_column()
	{
		//Arrange
		var view = new TriPaneView();

		//Assert
		view.SidePaneColumnIndex.Should().Be(0);
		view.StackColumnIndex.Should().Be(2);
	}

	[TestMethod]
	public void SidePanePlacement_right_puts_the_side_pane_in_the_trailing_column()
	{
		//Arrange
		var view = new TriPaneView { SidePanePlacement = TriPaneViewSidePanePlacement.Right };

		//Assert
		view.SidePaneColumnIndex.Should().Be(2);
		view.StackColumnIndex.Should().Be(0);
	}

	[TestMethod]
	public void UpdateDividerDrag_writes_the_normalized_weights_of_the_side_pair()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 100d);

		//Assert
		view.SidePanePercent.Should().BeApproximately(50d, Tolerance);
		view.StackPercent.Should().BeApproximately(50d, Tolerance);
	}

	[TestMethod]
	public void UpdateDividerDrag_accumulates_successive_moves()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 60d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 40d);

		//Assert
		view.SidePanePercent.Should().BeApproximately(50d, Tolerance);
	}

	[TestMethod]
	public void UpdateDividerDrag_maps_the_delta_to_the_stack_when_the_side_pane_is_on_the_right()
	{
		//Arrange
		var view = new TriPaneView { SidePanePlacement = TriPaneViewSidePanePlacement.Right };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 300d, 100d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 50d);

		//Assert
		view.StackPercent.Should().BeApproximately(87.5d, Tolerance);
		view.SidePanePercent.Should().BeApproximately(12.5d, Tolerance);
	}

	[TestMethod]
	public void UpdateDividerDrag_honours_the_minimum_length()
	{
		//Arrange
		var view = new TriPaneView { SidePaneMinLength = 80d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -90d);

		//Assert
		view.SidePanePercent.Should().BeApproximately(20d, Tolerance);
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void UpdateDividerDrag_minimizes_the_pane_when_the_minimum_length_is_zero()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -200d);

		//Assert
		view.SidePanePercent.Should().Be(0d);
		view.IsSidePaneMinimized.Should().BeTrue();
		view.IsSideRestoreGripVisible.Should().BeTrue();
	}

	[TestMethod]
	public void UpdateDividerDrag_snaps_to_zero_when_drag_to_minimize_is_enabled()
	{
		//Arrange
		var view = new TriPaneView { IsDragToMinimizeEnabled = true, SidePaneMinLength = 80d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -40d);

		//Assert
		view.SidePanePercent.Should().Be(0d);
		view.IsSidePaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void UpdateDividerDrag_reopens_at_the_minimum_when_drag_to_minimize_is_enabled()
	{
		//Arrange
		var view = new TriPaneView { IsDragToMinimizeEnabled = true, SidePaneMinLength = 80d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 90d);

		//Assert
		view.SidePanePercent.Should().BeApproximately(22.5d, Tolerance);
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void UpdateDividerDrag_writes_the_normalized_weights_of_the_stack_pair()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Stack, 200d, 200d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Stack, 100d);

		//Assert
		view.UpperPanePercent.Should().BeApproximately(75d, Tolerance);
		view.LowerPanePercent.Should().BeApproximately(25d, Tolerance);
	}

	[TestMethod]
	public void UpdateDividerDrag_ignores_movement_below_the_tap_threshold()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 1d);

		//Assert
		view.SidePanePercent.Should().Be(0d);
		view.IsSidePaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void UpdateDividerDrag_a_shaky_grip_click_still_restores_the_pane()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 1d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 1d, false);

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.SidePanePercent.Should().Be(DefaultSidePercent);
	}

	[TestMethod]
	public void UpdateDividerDrag_is_ignored_without_a_matching_start()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 100d);

		//Assert
		view.SidePanePercent.Should().Be(DefaultSidePercent);
	}

	[TestMethod]
	public void CompleteDividerDrag_raises_the_divider_drag_completed_event()
	{
		//Arrange
		var view = new TriPaneView();
		TriPaneViewDividerKind? raised = null;
		view.DividerDragCompleted += (_, e) => raised = e.Divider;

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Stack, 200d, 200d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Stack, 50d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Stack, 50d, false);

		//Assert
		raised.Should().Be(TriPaneViewDividerKind.Stack);
	}

	[TestMethod]
	public void CompleteDividerDrag_raises_nothing_without_a_matching_start()
	{
		//Arrange
		var view = new TriPaneView();
		var raisedCount = 0;
		view.DividerDragCompleted += (_, _) => raisedCount++;

		//Act
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 0d, false);

		//Assert
		raisedCount.Should().Be(0);
	}

	[TestMethod]
	public void CompleteDividerDrag_restores_a_minimized_pane_when_the_grip_is_tapped()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 0.5d, false);

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.SidePanePercent.Should().Be(DefaultSidePercent);
	}

	[TestMethod]
	public void CompleteDividerDrag_leaves_a_minimized_pane_alone_when_the_pointer_moved()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 40d, false);

		//Assert
		view.IsSidePaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void CompleteDividerDrag_does_not_restore_a_pane_that_has_no_grip()
	{
		//Arrange
		var view = new TriPaneView();
		view.MinimizeSidePane();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 0d, false);

		//Assert
		view.IsSidePaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void CompleteDividerDrag_restores_the_stack_when_its_grip_is_tapped()
	{
		//Arrange
		var view = new TriPaneView { StackPercent = 0d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 400d, 0d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 0d, false);

		//Assert
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.IsLowerPaneMinimized.Should().BeFalse();
		view.StackPercent.Should().Be(DefaultStackPercent);
	}

	[TestMethod]
	public void CompleteDividerDrag_restores_the_upper_pane_when_its_grip_is_tapped()
	{
		//Arrange
		var view = new TriPaneView { UpperPanePercent = 0d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Stack, 0d, 400d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Stack, 0d, false);

		//Assert
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.UpperPanePercent.Should().Be(50d);
	}

	[TestMethod]
	public void CompleteDividerDrag_ignores_a_cancelled_drag_for_the_tap_rule()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 0d, true);

		//Assert
		view.IsSidePaneMinimized.Should().BeTrue();
	}

	[TestMethod]
	public void MinimizeSidePane_after_a_drag_replaces_the_drag_cause_with_a_code_cause()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -200d);
		var gripAfterDrag = view.IsSideRestoreGripVisible;
		view.MinimizeSidePane();

		//Assert
		gripAfterDrag.Should().BeTrue();
		view.IsSideRestoreGripVisible.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreSidePane_clears_the_minimize_cause()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };

		//Act
		view.RestoreSidePane();

		//Assert
		view.IsSideRestoreGripVisible.Should().BeFalse();
		view.IsSideDividerVisible.Should().BeTrue();
	}

	[TestMethod]
	public void MinimizeUpperPane_does_not_take_the_side_panes_drag_grip_away()
	{
		//Arrange
		var view = new TriPaneView();
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -200d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, -200d, false);
		var gripAfterDrag = view.IsSideRestoreGripVisible;

		//Act
		view.MinimizeUpperPane();

		//Assert
		gripAfterDrag.Should().BeTrue();
		view.IsSideRestoreGripVisible.Should().BeTrue();
	}

	[TestMethod]
	public void MinimizeSidePane_does_not_take_the_upper_panes_drag_grip_away()
	{
		//Arrange
		var view = new TriPaneView();
		view.StartDividerDrag(TriPaneViewDividerKind.Stack, 200d, 200d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Stack, -200d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Stack, -200d, false);
		var gripAfterDrag = view.IsStackRestoreGripVisible;

		//Act
		view.MinimizeSidePane();

		//Assert
		gripAfterDrag.Should().BeTrue();
		view.IsStackRestoreGripVisible.Should().BeTrue();
	}

	[TestMethod]
	public void RestoreLowerPane_keeps_the_upper_panes_code_cause()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeUpperPane();
		view.MinimizeLowerPane();
		view.RestoreLowerPane();

		//Assert
		view.IsUpperPaneMinimized.Should().BeTrue();
		view.IsStackRestoreGripVisible.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreUpperPane_keeps_the_lower_panes_code_cause()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeLowerPane();
		view.MinimizeUpperPane();
		view.RestoreUpperPane();

		//Assert
		view.IsLowerPaneMinimized.Should().BeTrue();
		view.IsStackRestoreGripVisible.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreLowerPane_keeps_a_drag_cause_on_the_upper_pane()
	{
		//Arrange
		var view = new TriPaneView();
		view.StartDividerDrag(TriPaneViewDividerKind.Stack, 200d, 200d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Stack, -200d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Stack, -200d, false);

		//Act
		view.MinimizeLowerPane();
		view.RestoreLowerPane();

		//Assert
		view.IsUpperPaneMinimized.Should().BeTrue();
		view.IsStackRestoreGripVisible.Should().BeTrue();
	}

	[TestMethod]
	public void RestoreSidePane_after_a_drag_returns_the_pre_drag_weight()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 20d, StackPercent = 80d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 400d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -100d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, -100d, false);
		view.RestoreSidePane();

		//Assert
		view.SidePanePercent.Should().Be(20d);
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreUpperPane_after_a_stack_drag_returns_the_pre_drag_weight()
	{
		//Arrange
		var view = new TriPaneView { UpperPanePercent = 70d, LowerPanePercent = 30d };

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Stack, 280d, 120d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Stack, -280d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Stack, -280d, false);
		view.RestoreUpperPane();

		//Assert
		view.UpperPanePercent.Should().Be(70d);
		view.IsUpperPaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void RestoreSidePane_does_not_use_a_snapshot_the_user_dragged_past()
	{
		//Arrange
		var view = new TriPaneView { RestoreGripMode = TriPaneViewRestoreGripMode.Always };
		view.MinimizeSidePane();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 240d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 240d, false);

		view.StartDividerDrag(TriPaneViewDividerKind.Side, 240d, 160d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -240d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, -240d, false);

		view.RestoreSidePane();

		//Assert
		view.SidePanePercent.Should().BeApproximately(60d, Tolerance);
	}

	[TestMethod]
	public void UpdateDividerDrag_applies_a_move_that_returns_near_the_origin()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 40d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -39d);

		//Assert
		view.SidePanePercent.Should().BeApproximately(25.25d, Tolerance);
	}

	[TestMethod]
	public void UpdateDividerDrag_keeps_applying_once_the_tap_threshold_is_crossed()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 10d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, -9d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 1d);

		//Assert
		view.SidePanePercent.Should().BeApproximately(25.5d, Tolerance);
	}

	[TestMethod]
	public void RestoreAll_leaves_a_deliberate_zero_zero_pair_alone()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d, StackPercent = 0d };

		//Act
		view.RestoreAll();

		//Assert
		view.SidePanePercent.Should().Be(0d);
		view.StackPercent.Should().Be(0d);
		view.IsSidePaneMinimized.Should().BeFalse();
		view.SidePaneEffectiveWeight.Should().Be(50d);
		view.StackEffectiveWeight.Should().Be(50d);
	}

	[TestMethod]
	public void RestoreAll_restores_a_side_pane_that_really_was_minimized()
	{
		//Arrange
		var view = new TriPaneView();

		//Act
		view.MinimizeSidePane();
		view.MinimizeUpperPane();
		view.RestoreAll();

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.IsUpperPaneMinimized.Should().BeFalse();
		view.IsLowerPaneMinimized.Should().BeFalse();
		view.SidePanePercent.Should().Be(DefaultSidePercent);
		view.UpperPanePercent.Should().Be(50d);
	}

	[TestMethod]
	public void RestoreAll_brings_a_drag_collapsed_stack_back_in_its_own_proportions()
	{
		//Arrange
		var view = new TriPaneView { UpperPanePercent = 70d, LowerPanePercent = 30d, StackPercent = 0d };

		//Act
		view.RestoreAll();

		//Assert
		view.StackPercent.Should().Be(DefaultStackPercent);
		view.UpperPanePercent.Should().Be(70d);
		view.LowerPanePercent.Should().Be(30d);
	}

	[TestMethod]
	public void MinimizeSidePane_is_ignored_when_only_the_side_pane_is_open_and_coerces_the_flag()
	{
		//Arrange
		var view = new TriPaneView { StackPercent = 0d };

		//Act
		view.IsSidePaneMinimized = true;

		//Assert
		view.IsSidePaneMinimized.Should().BeFalse();
		view.SidePanePercent.Should().Be(DefaultSidePercent);
	}

	[TestMethod]
	public void IsLowerPaneMinimized_is_coerced_back_when_the_request_is_ignored()
	{
		//Arrange
		var view = new TriPaneView();
		view.MinimizeSidePane();
		view.MinimizeUpperPane();

		//Act
		view.IsLowerPaneMinimized = true;

		//Assert
		view.IsLowerPaneMinimized.Should().BeFalse();
		view.LowerPanePercent.Should().Be(50d);
	}

	[TestMethod]
	public void DividerDragCompleted_is_not_raised_by_a_click_that_changes_nothing()
	{
		//Arrange
		var view = new TriPaneView();
		var raisedCount = 0;
		view.DividerDragCompleted += (_, _) => raisedCount++;

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 300d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 0d, false);

		//Assert
		raisedCount.Should().Be(0);
		view.SidePanePercent.Should().Be(DefaultSidePercent);
	}

	[TestMethod]
	public void DividerDragCompleted_is_raised_by_a_grip_tap_that_restores_a_pane()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 0d };
		var raisedCount = 0;
		view.DividerDragCompleted += (_, _) => raisedCount++;

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 0d, 400d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 0d, false);

		//Assert
		raisedCount.Should().Be(1);
		view.IsSidePaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void CompleteDividerDrag_a_cancelled_drag_puts_the_weights_back()
	{
		//Arrange
		var view = new TriPaneView { SidePanePercent = 20d, StackPercent = 80d };
		var raisedCount = 0;
		view.DividerDragCompleted += (_, _) => raisedCount++;

		//Act
		view.StartDividerDrag(TriPaneViewDividerKind.Side, 100d, 400d);
		view.UpdateDividerDrag(TriPaneViewDividerKind.Side, 150d);
		view.CompleteDividerDrag(TriPaneViewDividerKind.Side, 150d, true);

		//Assert
		view.SidePanePercent.Should().Be(20d);
		view.StackPercent.Should().Be(80d);
		raisedCount.Should().Be(0);
	}

	[TestMethod]
	public void SidePanePercent_two_way_write_during_a_flag_sync_is_not_taken_as_a_command()
	{
		//Arrange
		var view = new TriPaneView();
		var hasWritten = false;

		view.RegisterPropertyChangedCallback(
			TriPaneView.IsSidePaneMinimizedProperty,
			(_, _) =>
			{
				if (hasWritten)
				{
					return;
				}

				hasWritten = true;
				view.UpperPanePercent = 0d;
			});

		//Act
		view.MinimizeSidePane();

		//Assert
		hasWritten.Should().BeTrue();
		view.UpperPanePercent.Should().Be(0d);
		view.IsUpperPaneMinimized.Should().BeTrue();
		view.IsSidePaneMinimized.Should().BeTrue();
	}
}
