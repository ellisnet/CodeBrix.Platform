using CodeBrix.Platform.UI.Toolkit.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests;

/// <summary>
/// One <see cref="TriPaneView"/> inside another one's pane. Nesting is the only way an application
/// gets four regions out of this control, so the pair is worth a fence of its own: the inner
/// control's state model, drags and floors have to be entirely its own, and the outer control's
/// minimize must not disturb them.
/// </summary>
/// <remarks>
/// The half of the nested case that needs a live visual tree - the pointer reaching a nested
/// control's dividers - is fenced by the TriPaneViewDemo sample, whose nested configuration drives
/// both of the inner control's dividers with real pointer input on a head.
/// </remarks>
[TestClass]
public class TriPaneViewNestedTests
{
	private const double Tolerance = 1e-9;

	/// <summary>The floor the entry's measured case put on every region of the inner control.</summary>
	private const double NestedFloor = 200d;

	private static (TriPaneView Outer, TriPaneView Inner) NestedPair()
	{
		var inner = new TriPaneView { SidePanePlacement = TriPaneViewSidePanePlacement.Right };
		var outer = new TriPaneView { UpperPane = inner };

		return (outer, inner);
	}

	[TestMethod]
	public void A_nested_control_keeps_its_own_weights()
	{
		//Arrange
		var (outer, inner) = NestedPair();

		//Act
		outer.SidePanePercent = 10d;
		outer.StackPercent = 90d;

		//Assert
		inner.SidePanePercent.Should().Be(33.3d);
		inner.StackPercent.Should().Be(66.7d);
	}

	[TestMethod]
	public void A_nested_control_stack_divider_drags_like_any_other()
	{
		//Arrange
		var (_, inner) = NestedPair();
		TriPaneViewDividerKind? raised = null;
		inner.DividerDragCompleted += (_, e) => raised = e.Divider;

		//Act
		inner.StartDividerDrag(TriPaneViewDividerKind.Stack, 200d, 200d);
		inner.UpdateDividerDrag(TriPaneViewDividerKind.Stack, -50d);
		inner.CompleteDividerDrag(TriPaneViewDividerKind.Stack, -50d, false);

		//Assert
		inner.UpperPanePercent.Should().BeApproximately(37.5d, Tolerance);
		inner.LowerPanePercent.Should().BeApproximately(62.5d, Tolerance);
		raised.Should().Be(TriPaneViewDividerKind.Stack);
	}

	[TestMethod]
	public void A_nested_control_side_divider_drag_leaves_the_outer_control_alone()
	{
		//Arrange
		var (outer, inner) = NestedPair();
		var outerRaisedCount = 0;
		outer.DividerDragCompleted += (_, _) => outerRaisedCount++;

		//Act
		inner.StartDividerDrag(TriPaneViewDividerKind.Side, 300d, 100d);
		inner.UpdateDividerDrag(TriPaneViewDividerKind.Side, -80d);
		inner.CompleteDividerDrag(TriPaneViewDividerKind.Side, -80d, false);

		//Assert
		outerRaisedCount.Should().Be(0);
		outer.SidePanePercent.Should().Be(33.3d);
		outer.StackPercent.Should().Be(66.7d);
		outer.UpperPanePercent.Should().Be(50d);
		outer.LowerPanePercent.Should().Be(50d);
	}

	[TestMethod]
	public void An_outer_drag_leaves_the_nested_control_alone()
	{
		//Arrange
		var (outer, inner) = NestedPair();

		//Act
		outer.StartDividerDrag(TriPaneViewDividerKind.Stack, 200d, 200d);
		outer.UpdateDividerDrag(TriPaneViewDividerKind.Stack, 100d);
		outer.CompleteDividerDrag(TriPaneViewDividerKind.Stack, 100d, false);

		//Assert
		inner.UpperPanePercent.Should().Be(50d);
		inner.LowerPanePercent.Should().Be(50d);
		inner.IsUpperPaneMinimized.Should().BeFalse();
	}

	[TestMethod]
	public void Minimizing_the_pane_a_nested_control_lives_in_keeps_the_very_same_instance()
	{
		//Arrange
		var (outer, inner) = NestedPair();
		inner.StartDividerDrag(TriPaneViewDividerKind.Stack, 200d, 200d);
		inner.UpdateDividerDrag(TriPaneViewDividerKind.Stack, -50d);
		inner.CompleteDividerDrag(TriPaneViewDividerKind.Stack, -50d, false);
		var upperBefore = inner.UpperPanePercent;

		//Act
		outer.MinimizeUpperPane();
		var whileMinimized = outer.UpperPane;
		outer.RestoreUpperPane();

		//Assert
		whileMinimized.Should().BeSameAs(inner);
		outer.UpperPane.Should().BeSameAs(inner);
		inner.UpperPanePercent.Should().Be(upperBefore);
	}

	[TestMethod]
	public void A_nested_control_given_less_room_than_its_floors_lays_neither_region_out_at_zero()
	{
		//Arrange - the entry's measured case: the outer control's stack floor is 200, and the
		//control living in it wants 200 + 6 + 200, so 194 pixels are left for two regions asking
		//for 400 between them.
		const double outerStackFloor = 200d;
		const double dividerThickness = 6d;
		var room = outerStackFloor - dividerThickness;

		//Act
		var (side, stack) = TriPaneViewLayoutMath.ResolveMinLengths(
			NestedFloor,
			NestedFloor,
			room,
			isFirstMinimized: false,
			isSecondMinimized: false);

		var (upper, lower) = TriPaneViewLayoutMath.ResolveMinLengths(
			NestedFloor,
			NestedFloor,
			room,
			isFirstMinimized: false,
			isSecondMinimized: false);

		//Assert
		side.Should().BeGreaterThan(0d);
		stack.Should().BeGreaterThan(0d);
		upper.Should().BeGreaterThan(0d);
		lower.Should().BeGreaterThan(0d);
		(side + stack).Should().BeApproximately(room, Tolerance);
		(upper + lower).Should().BeApproximately(room, Tolerance);
	}
}
