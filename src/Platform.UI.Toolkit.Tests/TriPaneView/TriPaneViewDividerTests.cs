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
}
