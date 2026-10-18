using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// Fences the limits the Wayland head's libdecor path sends for OverlappedPresenter's preferred minimum and maximum
/// size: effective pixels pass through unchanged (no conversion: content size is surface-local logical units), 0 means
/// no limit, and nothing the protocol would reject as invalid_size (negative, or a set maximum below its minimum) is sent.
/// </summary>
public class WaylandContentLimitsTests
{
	[Theory]
	[InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
	[InlineData(320, 240, 1920, 1080, 320, 240, 1920, 1080)]
	[InlineData(320, 240, 0, 0, 320, 240, 0, 0)]
	[InlineData(-5, -1, -10, 0, 0, 0, 0, 0)]
	[InlineData(800, 600, 400, 300, 800, 600, 800, 600)]
	[InlineData(800, 0, 400, 300, 800, 0, 800, 300)]
	public void the_limits_sent_are_the_requested_ones_made_valid_for_the_protocol(
		int minWidth, int minHeight, int maxWidth, int maxHeight,
		int expectedMinWidth, int expectedMinHeight, int expectedMaxWidth, int expectedMaxHeight)
	{
		//Act
		var limits = WaylandContentLimits.Normalize(minWidth, minHeight, maxWidth, maxHeight);

		//Assert
		limits.Should().Be((expectedMinWidth, expectedMinHeight, expectedMaxWidth, expectedMaxHeight));
	}
}
