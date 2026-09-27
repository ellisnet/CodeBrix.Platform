using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests;

[TestClass]
public class TriPaneViewDividerTests
{
	[TestMethod]
	public void Orientation_defaults_to_vertical() =>
		new TriPaneViewDivider().Orientation.Should().Be(Orientation.Vertical);

	[TestMethod]
	public void Orientation_can_be_set_to_horizontal() =>
		new TriPaneViewDivider { Orientation = Orientation.Horizontal }
			.Orientation.Should().Be(Orientation.Horizontal);

	[TestMethod]
	public void IsRestoreGrip_defaults_to_false() =>
		new TriPaneViewDivider().IsRestoreGrip.Should().BeFalse();

	[TestMethod]
	public void IsGripTowardStart_defaults_to_false() =>
		new TriPaneViewDivider().IsGripTowardStart.Should().BeFalse();

	[TestMethod]
	public void IsDragging_defaults_to_false() =>
		new TriPaneViewDivider().IsDragging.Should().BeFalse();

	[TestMethod]
	public void IsTabStop_is_false_so_the_divider_never_takes_focus() =>
		new TriPaneViewDivider().IsTabStop.Should().BeFalse();

	[TestMethod]
	public void PointerOverBrush_defaults_to_null() =>
		new TriPaneViewDivider().PointerOverBrush.Should().BeNull();

	[TestMethod]
	public void PressedBrush_defaults_to_null() =>
		new TriPaneViewDivider().PressedBrush.Should().BeNull();

	[TestMethod]
	public void SetRestoreGripState_turns_the_grip_on()
	{
		//Arrange
		var divider = new TriPaneViewDivider();

		//Act
		divider.SetRestoreGripState(true, true);

		//Assert
		divider.IsRestoreGrip.Should().BeTrue();
		divider.IsGripTowardStart.Should().BeTrue();
	}

	[TestMethod]
	public void SetRestoreGripState_turns_the_grip_off()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		divider.SetRestoreGripState(true, true);

		//Act
		divider.SetRestoreGripState(false, false);

		//Assert
		divider.IsRestoreGrip.Should().BeFalse();
		divider.IsGripTowardStart.Should().BeFalse();
	}

	[TestMethod]
	public void RaiseDragStarted_raises_the_drag_started_event()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		DragStartedEventArgs raised = null;
		divider.DragStarted += (_, e) => raised = e;

		//Act
		divider.RaiseDragStarted();

		//Assert
		raised.Should().NotBeNull();
		raised.HorizontalOffset.Should().Be(0d);
		raised.VerticalOffset.Should().Be(0d);
	}

	[TestMethod]
	public void RaiseDragDelta_carries_the_change_since_the_previous_move()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		DragDeltaEventArgs raised = null;
		divider.DragDelta += (_, e) => raised = e;

		//Act
		divider.RaiseDragDelta(12d, -7d);

		//Assert
		raised.Should().NotBeNull();
		raised.HorizontalChange.Should().Be(12d);
		raised.VerticalChange.Should().Be(-7d);
	}

	[TestMethod]
	public void RaiseDragCompleted_carries_the_cancelled_flag()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		DragCompletedEventArgs raised = null;
		divider.DragCompleted += (_, e) => raised = e;

		//Act
		divider.RaiseDragCompleted(true);

		//Assert
		raised.Should().NotBeNull();
		raised.Canceled.Should().BeTrue();
		raised.HorizontalChange.Should().Be(0d);
		raised.VerticalChange.Should().Be(0d);
	}

	[TestMethod]
	public void CancelDrag_does_nothing_when_no_drag_is_running()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		var raisedCount = 0;
		divider.DragCompleted += (_, _) => raisedCount++;

		//Act
		divider.CancelDrag();

		//Assert
		raisedCount.Should().Be(0);
		divider.IsDragging.Should().BeFalse();
	}

	[TestMethod]
	public void CancelDrag_completes_a_running_drag_as_cancelled()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		divider.SetValue(TriPaneViewDivider.IsDraggingProperty, true);
		DragCompletedEventArgs raised = null;
		divider.DragCompleted += (_, e) => raised = e;

		//Act
		divider.CancelDrag();

		//Assert
		divider.IsDragging.Should().BeFalse();
		raised.Should().NotBeNull();
		raised.Canceled.Should().BeTrue();
	}

	[TestMethod]
	public void IsEnabled_turning_false_cancels_a_running_drag()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		divider.SetValue(TriPaneViewDivider.IsDraggingProperty, true);
		DragCompletedEventArgs raised = null;
		divider.DragCompleted += (_, e) => raised = e;

		//Act
		divider.IsEnabled = false;

		//Assert
		divider.IsDragging.Should().BeFalse();
		raised.Should().NotBeNull();
		raised.Canceled.Should().BeTrue();
	}

	[TestMethod]
	public void ManipulationMode_is_none_so_direct_manipulation_cannot_steal_a_drag() =>
		new TriPaneViewDivider().ManipulationMode.Should().Be(ManipulationModes.None);

	[TestMethod]
	public void CancelDrag_releases_the_pointer_capture()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		divider.SetValue(TriPaneViewDivider.IsDraggingProperty, true);

		//Act
		divider.CancelDrag();

		//Assert
		divider.IsDragging.Should().BeFalse();
		(divider.PointerCaptures == null || divider.PointerCaptures.Count == 0).Should().BeTrue();
	}

	[TestMethod]
	public void CancelDrag_is_safe_to_call_twice()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		divider.SetValue(TriPaneViewDivider.IsDraggingProperty, true);
		var raisedCount = 0;
		divider.DragCompleted += (_, _) => raisedCount++;

		//Act
		divider.CancelDrag();
		divider.CancelDrag();

		//Assert
		raisedCount.Should().Be(1);
		divider.IsDragging.Should().BeFalse();
	}

	// WPE1-13 (e): platform drag entry points like Thumb's. FIXLIST [AP7-B TriPaneView]: before them, a platform-driven
	// gesture after a Core pointer drag reported the pointer drag's travel in DragCompleted.

	[TestMethod]
	public void A_platform_drag_after_a_pointer_drag_reports_only_its_own_travel()
	{
		//Arrange: the state a Core pointer drag of +120 leaves behind
		var divider = new TriPaneViewDivider();
		typeof(TriPaneViewDivider).GetField("_origin", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
			.SetValue(divider, new Windows.Foundation.Point(10d, 0d));
		typeof(TriPaneViewDivider).GetField("_previousPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
			.SetValue(divider, new Windows.Foundation.Point(130d, 0d));
		DragStartedEventArgs started = null;
		var deltas = new System.Collections.Generic.List<double>();
		DragCompletedEventArgs completed = null;
		divider.DragStarted += (_, e) => started = e;
		divider.DragDelta += (_, e) => deltas.Add(e.HorizontalChange);
		divider.DragCompleted += (_, e) => completed = e;

		//Act
		divider.RaiseDragStartedFromPlatform();
		var draggingAfterStart = divider.IsDragging;
		divider.RaiseDragDeltaFromPlatform(10d, 0d);
		divider.RaiseDragDeltaFromPlatform(-4d, 2d);
		divider.RaiseDragCompletedFromPlatform(false);

		//Assert
		draggingAfterStart.Should().BeTrue();
		started.Should().NotBeNull();
		started.HorizontalOffset.Should().Be(0d);
		deltas.Should().Equal(10d, -4d);
		completed.Should().NotBeNull();
		completed.HorizontalChange.Should().Be(6d);
		completed.VerticalChange.Should().Be(2d);
		completed.Canceled.Should().BeFalse();
		divider.IsDragging.Should().BeFalse();
	}

	[TestMethod]
	public void A_platform_tap_reports_zero_travel_after_an_earlier_platform_drag()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		var travels = new System.Collections.Generic.List<double>();
		divider.DragCompleted += (_, e) => travels.Add(e.HorizontalChange);
		divider.RaiseDragStartedFromPlatform();
		divider.RaiseDragDeltaFromPlatform(120d, 0d);
		divider.RaiseDragCompletedFromPlatform(false);

		//Act: a tap (no movement)
		divider.RaiseDragStartedFromPlatform();
		divider.RaiseDragCompletedFromPlatform(false);

		//Assert
		travels.Should().Equal(120d, 0d);
	}

	[TestMethod]
	public void Platform_deltas_and_completion_without_a_started_drag_are_ignored()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		var raised = 0;
		divider.DragDelta += (_, _) => raised++;
		divider.DragCompleted += (_, _) => raised++;

		//Act
		divider.RaiseDragDeltaFromPlatform(5d, 0d);
		divider.RaiseDragCompletedFromPlatform(false);

		//Assert
		raised.Should().Be(0);
		divider.IsDragging.Should().BeFalse();
	}

	[TestMethod]
	public void A_platform_drag_start_is_ignored_on_a_disabled_divider_or_during_a_drag()
	{
		//Arrange
		var disabled = new TriPaneViewDivider { IsEnabled = false };
		var dragging = new TriPaneViewDivider();
		dragging.RaiseDragStartedFromPlatform();
		var started = 0;
		disabled.DragStarted += (_, _) => started++;
		dragging.DragStarted += (_, _) => started++;

		//Act
		disabled.RaiseDragStartedFromPlatform();
		dragging.RaiseDragStartedFromPlatform();

		//Assert
		started.Should().Be(0);
		disabled.IsDragging.Should().BeFalse();
		dragging.IsDragging.Should().BeTrue();
	}

	[TestMethod]
	public void A_cancelled_platform_drag_completes_as_cancelled()
	{
		//Arrange
		var divider = new TriPaneViewDivider();
		DragCompletedEventArgs completed = null;
		divider.DragCompleted += (_, e) => completed = e;
		divider.RaiseDragStartedFromPlatform();
		divider.RaiseDragDeltaFromPlatform(0d, 30d);

		//Act
		divider.RaiseDragCompletedFromPlatform(true);

		//Assert
		completed.Should().NotBeNull();
		completed.Canceled.Should().BeTrue();
		completed.VerticalChange.Should().Be(30d);
		divider.IsDragging.Should().BeFalse();
	}
}
