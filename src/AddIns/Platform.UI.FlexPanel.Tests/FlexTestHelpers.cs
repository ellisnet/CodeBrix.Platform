#nullable enable

using CodeBrix.Platform.UI.FlexPanel.Internal;
using Xunit;

namespace CodeBrix.Platform.UI.FlexPanel.Tests;

/// <summary>
/// Shared helpers for the engine test suite, ported from the xamarin/flex C test harness
/// (tests/test.h): TEST_FRAME_EQUAL becomes <see cref="AssertFrame"/>, and flex_layout(root)
/// becomes <see cref="Layout"/>. Assertions use exact float equality, exactly as the C suite does.
/// </summary>
internal static class FlexTestHelpers
{
	/// <summary>Asserts an item's frame (x, y, width, height), like TEST_FRAME_EQUAL.</summary>
	public static void AssertFrame(Item item, float x, float y, float width, float height)
	{
		Assert.Equal(x, item.Frame[0]);
		Assert.Equal(y, item.Frame[1]);
		Assert.Equal(width, item.Frame[2]);
		Assert.Equal(height, item.Frame[3]);
	}

	/// <summary>Runs a full (non-measure-mode) layout pass on a root item, like flex_layout().</summary>
	public static void Layout(Item root) => root.Layout(inMeasureMode: false);
}
