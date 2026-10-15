using System.Linq;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.Platform.UI.WebGpu.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// The heads' adoptable Vulkan device profile (src/Platform.UI/Vulkan/VulkanAdoptionProfile.skia.cs): only with the
/// direct GPU rendering latch on, only what the device supports - and with the latch off, device creation exactly as
/// before (the swapchain extension, no features), which the engine then refuses to adopt.
/// </summary>
public sealed class VulkanAdoptionProfileTests
{
	[Fact]
	public void the_engine_extensions_are_selected_only_where_supported_in_list_order()
	{
		//Act
		var selected = VulkanAdoptionProfile.SelectExtensions(new[] { "VK_EXT_memory_budget", "VK_KHR_swapchain", "VK_EXT_robustness2" });

		//Assert
		selected.Should().Equal("VK_EXT_robustness2", "VK_EXT_memory_budget");
	}

	[Fact]
	public void nothing_is_selected_on_a_device_that_supports_none_of_them()
	{
		//Act
		var selected = VulkanAdoptionProfile.SelectExtensions(new[] { "VK_KHR_swapchain" });

		//Assert
		selected.Should().BeEmpty();
	}

	[Fact]
	public void the_list_is_what_the_engine_asks_of_an_adopted_device()
	{
		//Assert
		VulkanAdoptionProfile.EngineDeviceExtensions.Should().Equal(
			"VK_KHR_swapchain_mutable_format", "VK_KHR_image_format_list", "VK_EXT_robustness2",
			"VK_KHR_external_memory_fd", "VK_EXT_external_memory_dma_buf", "VK_EXT_memory_budget");
	}

	[Theory]
	[InlineData(1u, 0u, new int[0])]
	[InlineData(1u, 1u, new[] { 1 })]
	[InlineData(1u, 2u, new[] { 1, 2 })]
	[InlineData(1u, 3u, new[] { 1, 2, 3 })]
	[InlineData(1u, 4u, new[] { 1, 2, 3 })]
	public void core_feature_structures_follow_the_usable_api_version(uint major, uint minor, int[] expected)
	{
		//Act
		var versions = VulkanAdoptionProfile.CoreFeatureMinorVersions(major << 22 | minor << 12);

		//Assert
		versions.Should().Equal(expected);
	}

	[Fact]
	public void with_the_latch_off_the_heads_device_is_created_exactly_as_before_and_cannot_be_adopted()
	{
		//Arrange
		using var off = DirectGpuLatch.Off();
		using var head = HeadlessVulkanHead.CreateOrSkip();

		//Act
		var presenter = WebGpuDirectPresenter.TryCreate(head.Provider, out var reason);

		//Assert
		head.Provider.VkDeviceExtensions.Should().Equal("VK_KHR_swapchain");
		presenter.Should().BeNull();
		reason.Should().Contain("wgpuCodebrixInstanceAdoptVulkanDevice");
	}

	[Fact]
	public void with_the_latch_on_the_heads_device_enables_the_supported_engine_extensions_and_is_adopted()
	{
		//Arrange
		using var on = DirectGpuLatch.On();
		using var head = HeadlessVulkanHead.CreateOrSkip();

		//Act
		using var presenter = WebGpuDirectPresenter.TryCreate(head.Provider, out var reason);

		//Assert
		head.Provider.VkDeviceExtensions.First().Should().Be("VK_KHR_swapchain");
		head.Provider.VkDeviceExtensions.Skip(1).Should().OnlyContain(e => VulkanAdoptionProfile.EngineDeviceExtensions.Contains(e));
		reason.Should().BeNull();
		presenter.Should().NotBeNull();
	}
}
