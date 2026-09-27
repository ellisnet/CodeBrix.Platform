#nullable enable

using System.Collections.Generic;
using CodeBrix.Platform.UI.Toolkit;
using CodeBrix.Platform.UI.Toolkit.Engine;
using CodeBrix.Platform.UI.Toolkit.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The Toolkit engine (WPE1 C12): TriPaneLayoutState runs TriPaneView's pane logic - minimize/restore with snapshots
/// and causes, divider drags (drag-to-minimize, tap on a restore grip, cancel), the state pass with its minimized flags
/// and restore grips - over a host that keeps the weights in plain fields: no WinUI assembly loads.
/// </summary>
public class ToolkitEngineTests
{
	[Fact]
	public void When_Panes_Are_Minimized_Restored_And_Dragged_Then_The_State_Model_Follows_And_No_WinUI_Assembly_Loads()
	{
		//Arrange
		var host = new TestHost();
		var state = host.State;

		//Act + Assert: the initial pass
		state.UpdateState();
		host.Layout.SideWeight.Should().BeApproximately(33.3, 1e-9);
		host.Layout.IsSideDividerVisible.Should().BeTrue();
		host.Layout.IsSideGripVisible.Should().BeFalse();
		host.Flags.Should().Be((false, false, false, false));

		//A code minimize: no restore grip in Auto mode; restore goes back to the snapshot
		state.MinimizeSidePane();
		host.SidePanePercent.Should().Be(0d);
		host.Flags.Should().Be((true, false, false, false));
		host.Layout.SideWeight.Should().Be(0d);
		host.Layout.IsSideGripVisible.Should().BeFalse();
		state.RestoreSidePane();
		host.SidePanePercent.Should().BeApproximately(33.3, 1e-9);

		//A drag that closes the side pane: a DRAG cause, so the divider becomes a restore grip pointing left
		state.StartDividerDrag(TriPaneViewDividerKind.Side, 300, 600);
		state.IsSideDragActive.Should().BeTrue();
		state.UpdateDividerDrag(TriPaneViewDividerKind.Side, -400);
		var reportDrag = state.CompleteDividerDrag(TriPaneViewDividerKind.Side, -400, canceled: false);
		reportDrag.Should().BeTrue();
		host.SidePanePercent.Should().Be(0d);
		host.Layout.IsSideGripVisible.Should().BeTrue();
		host.Layout.IsSideGripTowardStart.Should().BeTrue();

		//A tap on the grip restores the pane at the weight it had before the drag
		state.StartDividerDrag(TriPaneViewDividerKind.Side, 0, 900);
		var reportTap = state.CompleteDividerDrag(TriPaneViewDividerKind.Side, 1, canceled: false);
		reportTap.Should().BeTrue();
		host.SidePanePercent.Should().BeApproximately(33.3, 1e-9);
		host.Layout.IsSideGripVisible.Should().BeFalse();

		//Minimizing the upper and then the lower pane collapses the whole stack; RestoreAll brings everything back
		var stackBeforeMinimize = host.StackPercent;
		state.MinimizeUpperPane();
		state.MinimizeLowerPane();
		host.StackPercent.Should().Be(0d);
		host.Flags.Should().Be((false, true, true, true));
		state.RestoreAll();
		host.StackPercent.Should().Be(stackBeforeMinimize); // the stack's restore snapshot
		host.UpperPanePercent.Should().Be(50d);
		host.LowerPanePercent.Should().Be(50d);

		//A cancelled drag puts the axis back and reports nothing
		state.StartDividerDrag(TriPaneViewDividerKind.Stack, 200, 200);
		state.UpdateDividerDrag(TriPaneViewDividerKind.Stack, 50);
		var movedUpper = host.UpperPanePercent;
		var reportCancel = state.CompleteDividerDrag(TriPaneViewDividerKind.Stack, 50, canceled: true);
		movedUpper.Should().BeGreaterThan(50d);
		reportCancel.Should().BeFalse();
		host.UpperPanePercent.Should().Be(50d);

		//A minimized flag set from outside (a two-way binding) minimizes the region
		state.SetMinimized(TriPaneViewRegion.Side, true);
		host.SidePanePercent.Should().Be(0d);
		state.SetMinimized(TriPaneViewRegion.Side, false);
		host.SidePanePercent.Should().BeApproximately(33.3, 1e-9);
		host.Passes.Should().BeGreaterThan(5);

		EngineIsolation.LoadedPlatformAssemblies().Should().Contain("CodeBrix.Platform.UI.Toolkit.Core");
		EngineIsolation.AssertNoWinUILoaded("Toolkit (TriPaneLayoutState)");
	}

	// WPE1-13 (f; AP7_TriPaneView_REPORT D-P7B-TP-4): a platform's display override changes what is LAID OUT - a hidden
	// region shows minimized with a restore grip whose tap goes to the platform - and never writes the application's
	// weights or minimized flags.
	[Fact]
	public void When_A_Display_Override_Hides_A_Pane_Then_The_Layout_Follows_And_The_Application_Weights_Do_Not()
	{
		//Arrange
		var host = new TestHost();
		var state = host.State;
		state.UpdateState();
		var displayOverride = new TestDisplayOverride { HideSide = true, HideLower = true };
		var writesBefore = host.Writes;

		//Act: the override hides the side pane and the lower pane (a Compact form)
		state.DisplayOverride = displayOverride;
		var hidden = host.Layout;
		var flagsWhileHidden = host.Flags;

		//A drag on the hidden side pane's grip does not move it
		state.StartDividerDrag(TriPaneViewDividerKind.Side, 0, 900);
		state.UpdateDividerDrag(TriPaneViewDividerKind.Side, 200);
		var reportDragOnGrip = state.CompleteDividerDrag(TriPaneViewDividerKind.Side, 200, canceled: false);
		var restoreRequestsAfterDrag = displayOverride.Requests.Count;

		//A tap on the stack divider's grip asks the platform to show the lower pane; the platform switches panes
		state.StartDividerDrag(TriPaneViewDividerKind.Stack, 600, 0);
		var reportTap = state.CompleteDividerDrag(TriPaneViewDividerKind.Stack, 1, canceled: false);
		var switched = host.Layout;

		//The override goes away (an Expanded window): the application's layout is displayed again
		state.DisplayOverride = null;
		var restored = host.Layout;

		//Assert
		hidden.SideWeight.Should().Be(0d);
		hidden.StackWeight.Should().Be(100d);
		hidden.LowerWeight.Should().Be(0d);
		hidden.UpperWeight.Should().Be(100d);
		hidden.IsSideGripVisible.Should().BeTrue();
		hidden.IsSideGripTowardStart.Should().BeTrue();
		hidden.IsStackGripVisible.Should().BeTrue();
		hidden.IsStackGripTowardStart.Should().BeFalse();
		flagsWhileHidden.Should().Be((false, false, false, false));

		reportDragOnGrip.Should().BeFalse();
		restoreRequestsAfterDrag.Should().Be(0);
		host.SidePanePercent.Should().Be(33.3);

		reportTap.Should().BeFalse(); // the application's weights did not change: nothing to report
		displayOverride.Requests.Should().Equal(TriPaneViewRegion.Lower);
		switched.LowerWeight.Should().Be(100d);
		switched.UpperWeight.Should().Be(0d);
		switched.IsStackGripVisible.Should().BeTrue();
		switched.IsStackGripTowardStart.Should().BeTrue();

		restored.SideWeight.Should().BeApproximately(33.3, 1e-9);
		restored.LowerWeight.Should().Be(50d);
		restored.IsSideGripVisible.Should().BeFalse();
		restored.IsStackGripVisible.Should().BeFalse();

		host.Writes.Should().Be(writesBefore); // no percent property was written at any point
		(host.SidePanePercent, host.StackPercent, host.UpperPanePercent, host.LowerPanePercent).Should().Be((33.3, 66.7, 50d, 50d));
		host.Flags.Should().Be((false, false, false, false));

		EngineIsolation.AssertNoWinUILoaded("Toolkit (TriPaneLayoutState display override)");
	}

	/// <summary>A Compact-like form: hides the side pane and one stacked pane; a grip tap switches the stacked pane.</summary>
	private sealed class TestDisplayOverride : ITriPaneDisplayOverride
	{
		internal bool HideSide { get; set; }

		internal bool HideLower { get; set; }

		internal List<TriPaneViewRegion> Requests { get; } = new();

		public TriPaneDisplayWeights GetDisplayWeights(TriPaneDisplayWeights applicationWeights)
			=> applicationWeights with
			{
				Side = HideSide ? 0d : applicationWeights.Side,
				Upper = HideLower ? applicationWeights.Upper : 0d,
				Lower = HideLower ? 0d : applicationWeights.Lower,
			};

		public bool RestoreRequested(TriPaneViewRegion region)
		{
			Requests.Add(region);
			if (region == TriPaneViewRegion.Lower)
			{
				HideLower = false;
				return true;
			}

			return false;
		}
	}

	/// <summary>A host with the weights in plain fields; writing a weight runs the engine's weight-change pass, as the control's property callback does.</summary>
	private sealed class TestHost : ITriPaneLayoutHost
	{
		private double _side = 33.3;
		private double _stack = 66.7;
		private double _upper = 50;
		private double _lower = 50;

		internal TestHost() => State = new TriPaneLayoutState(this);

		internal TriPaneLayoutState State { get; }

		internal TriPaneLayoutResult Layout { get; private set; }

		internal (bool Side, bool Stack, bool Upper, bool Lower) Flags { get; private set; }

		internal int Passes { get; private set; }

		internal int Writes { get; private set; }

		public double SidePanePercent { get => _side; set { _side = value; Writes++; State.OnWeightChanged(); } }

		public double StackPercent { get => _stack; set { _stack = value; Writes++; State.OnWeightChanged(); } }

		public double UpperPanePercent { get => _upper; set { _upper = value; Writes++; State.OnWeightChanged(); } }

		public double LowerPanePercent { get => _lower; set { _lower = value; Writes++; State.OnWeightChanged(); } }

		public TriPaneViewSidePanePlacement SidePanePlacement => TriPaneViewSidePanePlacement.Left;

		public TriPaneViewRestoreGripMode RestoreGripMode => TriPaneViewRestoreGripMode.Auto;

		public bool IsDragToMinimizeEnabled => true;

		public double SidePaneMinLength => 0;

		public double StackMinLength => 0;

		public double UpperPaneMinLength => 0;

		public double LowerPaneMinLength => 0;

		public void SyncMinimizedFlags(int version, bool isSideMinimized, bool isStackMinimized, bool isUpperMinimized, bool isLowerMinimized)
			=> Flags = (isSideMinimized, isStackMinimized, isUpperMinimized, isLowerMinimized);

		public void ApplyLayout(TriPaneLayoutResult layout)
		{
			Layout = layout;
			Passes++;
		}
	}
}
