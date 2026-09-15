using System;
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
}
