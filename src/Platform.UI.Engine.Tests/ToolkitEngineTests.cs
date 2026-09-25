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

		public double SidePanePercent { get => _side; set { _side = value; State.OnWeightChanged(); } }

		public double StackPercent { get => _stack; set { _stack = value; State.OnWeightChanged(); } }

		public double UpperPanePercent { get => _upper; set { _upper = value; State.OnWeightChanged(); } }

		public double LowerPanePercent { get => _lower; set { _lower = value; State.OnWeightChanged(); } }

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
