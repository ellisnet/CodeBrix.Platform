using System;
using Microsoft.UI.Windowing;
using CodeBrix.Platform.WinUI.Runtime.Skia.X11;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// Fences the decision behind the two Error lines every X11 launch used to write: a window that has
/// never been mapped has no <c>_NET_WM_STATE</c> yet, which says nothing about the window manager.
/// </summary>
public class X11WindowStateRulesTests
{
	[Fact]
	public void a_window_that_has_never_been_mapped_is_expected_to_have_no_wm_state()
	{
		//Act
		var expected = X11WindowStateRules.IsMissingWMStateExpected(MapState.IsUnmapped);

		//Assert
		expected.Should().BeTrue();
	}

	[Fact]
	public void a_viewable_window_with_no_wm_state_is_a_fault()
	{
		//Act
		var expected = X11WindowStateRules.IsMissingWMStateExpected(MapState.IsViewable);

		//Assert
		expected.Should().BeFalse();
	}

	[Fact]
	public void an_unviewable_window_with_no_wm_state_is_a_fault()
	{
		//Act
		var expected = X11WindowStateRules.IsMissingWMStateExpected(MapState.IsUnviewable);

		//Assert
		expected.Should().BeFalse();
	}

	// WPE1-14 (FIXLIST_platform_core_split [WPE1-13] FOUND): on Cinnamon X11 a maximized window stayed Maximized after
	// OverlappedPresenter.Restore(), because Restore only raised the window; a minimized maximized window was reported
	// Maximized, because the first maximized/hidden atom in _NET_WM_STATE decided.

	private static readonly IntPtr Hidden = 11, MaxHorz = 12, MaxVert = 13, Above = 14;

	[Fact]
	public void hidden_wins_over_maximized_wherever_it_is_listed()
	{
		//Act
		var state = X11WindowStateRules.ToPresenterState([MaxHorz, MaxVert, Hidden], Hidden, MaxHorz, MaxVert);

		//Assert
		state.Should().Be(OverlappedPresenterState.Minimized);
	}

	[Fact]
	public void either_maximized_atom_reports_maximized_and_none_reports_restored()
	{
		//Act
		var vertical = X11WindowStateRules.ToPresenterState([Above, MaxVert], Hidden, MaxHorz, MaxVert);
		var none = X11WindowStateRules.ToPresenterState([Above], Hidden, MaxHorz, MaxVert);
		var empty = X11WindowStateRules.ToPresenterState([], Hidden, MaxHorz, MaxVert);

		//Assert
		vertical.Should().Be(OverlappedPresenterState.Maximized);
		none.Should().Be(OverlappedPresenterState.Restored);
		empty.Should().Be(OverlappedPresenterState.Restored);
	}

	[Fact]
	public void restoring_a_maximized_window_removes_the_maximized_state()
	{
		//Act
		var plan = X11WindowStateRules.PlanRestore(wasShown: true, MapState.IsViewable, hidden: false, maximized: true, activateWindow: false);

		//Assert
		plan.Should().Be(X11RestoreActions.Unmaximize);
	}

	[Fact]
	public void restoring_a_minimized_window_maps_and_activates_it()
	{
		//Act
		var plan = X11WindowStateRules.PlanRestore(wasShown: true, MapState.IsUnmapped, hidden: true, maximized: false, activateWindow: false);

		//Assert
		plan.Should().Be(X11RestoreActions.Deiconify | X11RestoreActions.Activate);
	}

	[Fact]
	public void restoring_a_minimized_maximized_window_leaves_both_states()
	{
		//Act
		var plan = X11WindowStateRules.PlanRestore(wasShown: true, MapState.IsUnmapped, hidden: true, maximized: true, activateWindow: false);

		//Assert
		plan.Should().Be(X11RestoreActions.Unmaximize | X11RestoreActions.Deiconify | X11RestoreActions.Activate);
	}

	[Fact]
	public void restoring_a_normal_window_only_activates_when_asked()
	{
		//Act
		var quiet = X11WindowStateRules.PlanRestore(wasShown: true, MapState.IsViewable, hidden: false, maximized: false, activateWindow: false);
		var activate = X11WindowStateRules.PlanRestore(wasShown: true, MapState.IsViewable, hidden: false, maximized: false, activateWindow: true);

		//Assert
		quiet.Should().Be(X11RestoreActions.None);
		activate.Should().Be(X11RestoreActions.Activate);
	}

	[Fact]
	public void before_the_window_is_shown_restore_only_raises_as_before()
	{
		//Act
		var plan = X11WindowStateRules.PlanRestore(wasShown: false, MapState.IsUnmapped, hidden: false, maximized: false, activateWindow: false);

		//Assert
		plan.Should().Be(X11RestoreActions.Activate);
	}
}
