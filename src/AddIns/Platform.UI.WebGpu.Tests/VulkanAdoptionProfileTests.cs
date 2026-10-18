using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.Platform.UI.WebGpu.Internal;
using CodeBrix.Platform.UI.WebGpu.Skia;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// The heads' adoptable Vulkan device profile (src/Platform.UI/Vulkan/VulkanAdoptionProfile.skia.cs): only with the
/// direct GPU rendering latch on; what the WebGPU engine itself says it requires of the physical device (the add-in's
/// adopt-requirements source), or - with no answer - the fallback list and every supported feature; only what the
/// device supports. With the latch off, device creation exactly as before (the swapchain extension, no features),
/// which the engine then refuses to adopt.
/// </summary>
public sealed class VulkanAdoptionProfileTests
{
	private static readonly uint Vulkan11 = VulkanAdoptionProfile.MakeVersion(1, 1);
	private static readonly uint Vulkan12 = VulkanAdoptionProfile.MakeVersion(1, 2);

	[Fact]
	public void the_fallback_extensions_are_selected_only_where_supported_in_list_order()
	{
		//Act
		var selected = VulkanAdoptionProfile.SelectFallbackExtensions(new[] { "VK_EXT_memory_budget", "VK_KHR_swapchain", "VK_EXT_robustness2" });

		//Assert
		selected.Should().Equal("VK_EXT_robustness2", "VK_EXT_memory_budget");
	}

	[Fact]
	public void nothing_from_the_fallback_list_is_selected_on_a_device_that_supports_none_of_it()
	{
		//Act
		var selected = VulkanAdoptionProfile.SelectFallbackExtensions(new[] { "VK_KHR_swapchain" });

		//Assert
		selected.Should().BeEmpty();
	}

	[Fact]
	public void the_fallback_list_is_what_the_engine_asked_of_common_adapters()
	{
		//Assert
		VulkanAdoptionProfile.FallbackDeviceExtensions.Should().Equal(
			"VK_KHR_swapchain_mutable_format", "VK_KHR_image_format_list", "VK_EXT_robustness2",
			"VK_KHR_external_memory_fd", "VK_EXT_external_memory_dma_buf", "VK_EXT_memory_budget");
	}

	[Fact]
	public void the_engines_extensions_are_kept_in_its_order_once_each_without_the_heads_swapchain_and_only_where_supported()
	{
		//Arrange
		var required = new[] { "VK_KHR_swapchain", "VK_EXT_robustness2", "VK_KHR_unknown_to_this_gpu", "VK_EXT_memory_budget", "VK_EXT_robustness2" };
		var supported = new[] { "VK_KHR_swapchain", "VK_EXT_memory_budget", "VK_EXT_robustness2" };

		//Act
		var selected = VulkanAdoptionProfile.SelectEngineExtensions(required, supported, Vulkan12);

		//Assert
		selected.Should().Equal("VK_EXT_robustness2", "VK_EXT_memory_budget");
	}

	[Theory]
	[InlineData(1u, 1u, true, true)]    // below 1.2, supported, not named: added
	[InlineData(1u, 2u, true, false)]   // core from 1.2: not added
	[InlineData(1u, 3u, true, false)]
	[InlineData(1u, 1u, false, false)]  // not supported: never added
	public void image_format_list_follows_swapchain_mutable_format_below_vulkan_1_2(uint major, uint minor, bool supportedByTheDevice, bool added)
	{
		//Arrange
		var supported = new List<string> { "VK_KHR_swapchain", "VK_KHR_swapchain_mutable_format" };
		if (supportedByTheDevice)
		{
			supported.Add("VK_KHR_image_format_list");
		}

		//Act
		var selected = VulkanAdoptionProfile.SelectEngineExtensions(new[] { "VK_KHR_swapchain", "VK_KHR_swapchain_mutable_format" },
			supported, VulkanAdoptionProfile.MakeVersion(major, minor));

		//Assert
		selected.Contains("VK_KHR_image_format_list").Should().Be(added);
		selected.First().Should().Be("VK_KHR_swapchain_mutable_format");
	}

	[Fact]
	public void image_format_list_is_never_added_twice_or_without_swapchain_mutable_format()
	{
		//Arrange
		var supported = new[] { "VK_KHR_swapchain", "VK_KHR_swapchain_mutable_format", "VK_KHR_image_format_list" };

		//Act
		var named = VulkanAdoptionProfile.SelectEngineExtensions(new[] { "VK_KHR_image_format_list", "VK_KHR_swapchain_mutable_format" }, supported, Vulkan11);
		var withoutMutable = VulkanAdoptionProfile.SelectEngineExtensions(new[] { "VK_KHR_swapchain" }, supported, Vulkan11);

		//Assert
		named.Should().Equal("VK_KHR_image_format_list", "VK_KHR_swapchain_mutable_format");
		withoutMutable.Should().BeEmpty();
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
	public void the_heads_load_the_add_ins_bootstrap_by_a_name_that_resolves_to_it()
	{
		//Act
		var resolved = Type.GetType(VulkanAdoptionProfile.AddInBootstrapTypeName, throwOnError: false);

		//Assert: a rename of the bootstrap (or of this assembly) breaks this test instead of the X11 head's adoption.
		resolved.Should().BeSameAs(typeof(SkiaPlatformBootstrap));
		typeof(SkiaPlatformBootstrap).Assembly.GetName().Name.Should().Be("CodeBrix.Platform.UI.WebGpu.Skia");
	}

	[Fact]
	public void the_add_ins_bootstrap_registers_the_engines_requirements_source()
	{
		//Act
		var source = VulkanAdoptionProfile.FindRequirementsSource();

		//Assert
		source.Should().BeOfType<WebGpuVulkanAdoptRequirementsSource>();
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
		head.Device.AdoptionRoute.Should().BeNull();
		presenter.Should().BeNull();
		reason.Should().Contain("wgpuCodebrixInstanceAdoptVulkanDevice");
	}

	[Fact]
	public void with_the_latch_on_the_heads_device_enables_exactly_what_the_engine_requires_and_is_adopted()
	{
		//Arrange
		using var on = DirectGpuLatch.On();
		VulkanAdoptionProfile.FindRequirementsSource().Should().BeOfType<WebGpuVulkanAdoptRequirementsSource>();
		using var head = HeadlessVulkanHead.CreateOrSkip();

		// The engine's own answer for this physical device, asked independently with the head's adoption inputs.
		var provider = head.Provider;
		using var expected = new WebGpuVulkanAdoptRequirementsSource().Query(new VulkanAdoptQuery(
			provider.VkInstance, provider.VkPhysicalDevice, provider.VkApiVersion, provider.VkGetInstanceProcAddr, provider.VkInstanceExtensions));

		//Act
		using var presenter = WebGpuDirectPresenter.TryCreate(provider, out var reason);

		//Assert
		TestContext.Current.TestOutputHelper?.WriteLine(head.Device.AdoptionDescription ?? "(no adoption profile)");
		head.Device.AdoptionRoute.Should().Be(VulkanAdoptionRoute.EngineQuery);
		head.Device.AdoptionDescription.Should().Contain("engine query");
		provider.VkDeviceExtensions.Should().Equal(
			new[] { "VK_KHR_swapchain" }.Concat(expected.RequiredDeviceExtensions.Where(e => e != "VK_KHR_swapchain")).Distinct());
		expected.PhysicalDeviceFeatures2.Should().NotBe(IntPtr.Zero);
		reason.Should().BeNull();
		presenter.Should().NotBeNull();
	}

	[Fact]
	public void with_the_latch_on_and_a_source_that_throws_the_device_is_created_with_the_fallback_list()
	{
		//Arrange
		using var on = DirectGpuLatch.On();
		using var source = RequirementsSourceScope.Set(new ThrowingSource());

		//Act
		using var head = HeadlessVulkanHead.CreateOrSkip();

		//Assert: creation never fails because of the profile; the one line says why the fallback was used.
		head.Device.AdoptionRoute.Should().Be(VulkanAdoptionRoute.Fallback);
		head.Device.AdoptionDescription.Should().Contain("fallback list");
		head.Device.AdoptionDescription.Should().Contain("the engine's adopt-requirements query failed: " + ThrowingSource.Message);
		head.Provider.VkDeviceExtensions.First().Should().Be("VK_KHR_swapchain");
		head.Provider.VkDeviceExtensions.Skip(1).Should().OnlyContain(e => VulkanAdoptionProfile.FallbackDeviceExtensions.Contains(e));
	}

	[Fact]
	public void with_the_latch_on_and_no_requirements_source_the_heads_device_enables_the_supported_fallback_extensions_and_is_adopted()
	{
		//Arrange: no source, as in an application without the add-in (this one's initializer has already run, so the
		// by-name load registers nothing again).
		using var on = DirectGpuLatch.On();
		using var source = RequirementsSourceScope.Set(null);
		using var head = HeadlessVulkanHead.CreateOrSkip();

		//Act
		using var presenter = WebGpuDirectPresenter.TryCreate(head.Provider, out var reason);

		//Assert
		head.Device.AdoptionRoute.Should().Be(VulkanAdoptionRoute.Fallback);
		head.Device.AdoptionDescription.Should().Contain("no adopt-requirements source is registered");
		head.Provider.VkDeviceExtensions.First().Should().Be("VK_KHR_swapchain");
		head.Provider.VkDeviceExtensions.Skip(1).Should().OnlyContain(e => VulkanAdoptionProfile.FallbackDeviceExtensions.Contains(e));
		reason.Should().BeNull();
		presenter.Should().NotBeNull();
	}

	/// <summary>A requirements source that fails as the engine's does when it cannot use the physical device.</summary>
	private sealed class ThrowingSource : IVulkanAdoptRequirementsSource
	{
		internal const string Message = "wgpu cannot use this physical device (test)";

		public IVulkanAdoptRequirements Query(VulkanAdoptQuery query) => throw new InvalidOperationException(Message);
	}

	/// <summary>Replaces the registered requirements source for one test and puts the previous one back.</summary>
	private sealed class RequirementsSourceScope : IDisposable
	{
		private readonly IVulkanAdoptRequirementsSource? _previous;

		private RequirementsSourceScope(IVulkanAdoptRequirementsSource? source)
		{
			_previous = VulkanAdoptionProfile.FindRequirementsSource();
			VulkanAdoptionProfile.RequirementsSource = source;
		}

		internal static RequirementsSourceScope Set(IVulkanAdoptRequirementsSource? source) => new(source);

		public void Dispose() => VulkanAdoptionProfile.RequirementsSource = _previous;
	}
}
