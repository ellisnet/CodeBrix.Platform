using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// Fences the Wayland head's renderer decision after it moved out of WaylandXamlRootHost: Vulkan by default,
/// code before environment, NO_GPU before USE_EGL, and VulkanForced always Vulkan.
/// </summary>
public class WaylandRendererSelectionTests
{
	[Theory]
	// forceVulkan, useVulkan, useEgl, envNoGpu, envUseEgl -> choice (by name: the enum is internal)
	[InlineData(false, null, null, false, false, "Vulkan")]
	[InlineData(false, null, null, true, false, "Software")]
	[InlineData(false, null, null, false, true, "OpenGLES")]
	[InlineData(false, null, null, true, true, "Software")]
	[InlineData(false, true, null, true, true, "Vulkan")]
	[InlineData(false, false, null, false, false, "Software")]
	[InlineData(false, false, true, false, false, "OpenGLES")]
	[InlineData(false, null, true, true, false, "OpenGLES")]
	[InlineData(false, null, false, false, true, "Software")]
	[InlineData(true, false, true, true, false, "Vulkan")]
	public void the_renderer_choice_follows_the_documented_precedence(
		bool forceVulkan, bool? useVulkan, bool? useEgl, bool envNoGpu, bool envUseEgl, string expected)
	{
		//Act
		var choice = WaylandRendererSelection.Choose(forceVulkan, useVulkan, useEgl, envNoGpu, envUseEgl);

		//Assert
		choice.ToString().Should().Be(expected);
	}
}
