using System;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;
using CodeBrix.Platform.WinUI.Runtime.Skia.X11;
using SilverAssertions;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.Runtime.Skia.Tests;

/// <summary>
/// The native GPU device provider rule on the X11 and Wayland heads: a window gets a provider when (and only when)
/// it renders with the Vulkan renderer - or, on Wayland before its lazily created renderer exists, is going to -
/// and a provider over a context that is gone reports unavailable with zero handles. The live handles are checked
/// against a real X11 Vulkan window by hand (WP1 report); no window is created here.
/// </summary>
public class NativeGpuDeviceProviderTests
{
	[Fact]
	public void only_the_x11_vulkan_renderer_owns_a_vulkan_context()
	{
		//Assert
		typeof(IVulkanContextOwner).IsAssignableFrom(typeof(X11VulkanRenderer)).Should().BeTrue();
		typeof(IVulkanContextOwner).IsAssignableFrom(typeof(X11OpenGLRenderer)).Should().BeFalse();
		typeof(IVulkanContextOwner).IsAssignableFrom(typeof(X11EGLRenderer)).Should().BeFalse();
		typeof(IVulkanContextOwner).IsAssignableFrom(typeof(X11SoftwareRenderer)).Should().BeFalse();
	}

	[Fact]
	public void only_the_wayland_vulkan_renderer_owns_a_vulkan_context()
	{
		//Assert
		typeof(IVulkanContextOwner).IsAssignableFrom(typeof(WaylandVulkanRenderer)).Should().BeTrue();
		typeof(IVulkanContextOwner).IsAssignableFrom(typeof(WaylandEglRenderer)).Should().BeFalse();
		typeof(IVulkanContextOwner).IsAssignableFrom(typeof(WaylandShmRenderer)).Should().BeFalse();
	}

	[Fact]
	public void a_window_whose_renderer_has_no_vulkan_context_gets_no_provider()
	{
		//Assert
		//What the X11 builder answers for a window on OpenGL, OpenGL ES or software: null = no provider.
		VulkanNativeGpuDeviceProvider.For(null).Should().BeNull();
		VulkanNativeGpuDeviceProvider.For(new object()).Should().BeNull();
	}

	[Fact]
	public void a_vulkan_owner_gets_a_vulkan_provider()
	{
		//Act
		var provider = VulkanNativeGpuDeviceProvider.For(new FakeOwner());

		//Assert
		provider.Should().NotBeNull();
		provider!.Kind.Should().Be(NativeGpuDeviceKind.Vulkan);
	}

	[Fact]
	public void before_the_renderer_exists_the_provider_is_unavailable_and_says_why()
	{
		//Arrange
		var provider = VulkanNativeGpuDeviceProvider.For(new FakeOwner())!;

		//Assert
		provider.IsAvailable.Should().BeFalse();
		provider.Reason.Should().Be(VulkanNativeGpuDeviceProvider.NotCreatedReason);
		AssertEmpty(provider);
	}

	[Fact]
	public void after_the_context_is_disposed_the_provider_is_unavailable_and_says_why()
	{
		//Arrange
		var context = new VulkanContext();
		context.Dispose();
		var provider = VulkanNativeGpuDeviceProvider.For(new FakeOwner { VulkanContext = context })!;

		//Assert
		provider.IsAvailable.Should().BeFalse();
		provider.Reason.Should().Be(VulkanNativeGpuDeviceProvider.DisposedReason);
		AssertEmpty(provider);
	}

	[Fact]
	public void the_provider_follows_its_owner_live()
	{
		//Arrange
		var owner = new FakeOwner();
		var provider = VulkanNativeGpuDeviceProvider.For(owner)!;
		var before = provider.Reason;

		//Act
		var context = new VulkanContext();
		context.Dispose();
		owner.VulkanContext = context;

		//Assert
		before.Should().Be(VulkanNativeGpuDeviceProvider.NotCreatedReason);
		provider.Reason.Should().Be(VulkanNativeGpuDeviceProvider.DisposedReason);
	}

	[Fact]
	public void wayland_answers_a_provider_before_the_first_frame_only_when_vulkan_will_be_chosen()
	{
		//Arrange
		var window = new FakeOwner();

		//Assert
		WaylandXamlRootHost.NativeGpuDeviceProviderFor(null, WaylandRendererChoice.Vulkan, window).Should().BeOfType<VulkanNativeGpuDeviceProvider>();
		WaylandXamlRootHost.NativeGpuDeviceProviderFor(null, WaylandRendererChoice.OpenGLES, window).Should().BeNull();
		WaylandXamlRootHost.NativeGpuDeviceProviderFor(null, WaylandRendererChoice.Software, window).Should().BeNull();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void wayland_answers_no_provider_once_a_non_vulkan_renderer_exists(bool vulkanWasChosen)
	{
		//Assert
		//e.g. Vulkan was chosen but fell back to wl_shm: the existing renderer decides, not the choice.
		WaylandXamlRootHost.NativeGpuDeviceProviderFor(new FakeWaylandRenderer(),
			vulkanWasChosen ? WaylandRendererChoice.Vulkan : WaylandRendererChoice.Software, new FakeOwner()).Should().BeNull();
	}

	[Fact]
	public void wayland_answers_a_provider_once_the_vulkan_renderer_exists()
	{
		//Assert
		WaylandXamlRootHost.NativeGpuDeviceProviderFor(new FakeVulkanWaylandRenderer(), WaylandRendererChoice.Software, new FakeOwner())
			.Should().BeOfType<VulkanNativeGpuDeviceProvider>();
	}

	private static void AssertEmpty(INativeGpuDeviceProvider provider)
	{
		provider.VkInstance.Should().Be(0);
		provider.VkPhysicalDevice.Should().Be(0);
		provider.VkDevice.Should().Be(0);
		provider.VkQueue.Should().Be(0);
		provider.VkQueueFamilyIndex.Should().Be(0u);
		provider.VkQueueIndex.Should().Be(0u);
		provider.VkApiVersion.Should().Be(0u);
		provider.VkInstanceExtensions.Should().BeEmpty();
		provider.VkDeviceExtensions.Should().BeEmpty();
		provider.VkGetInstanceProcAddr.Should().Be(0);
		provider.MetalDevice.Should().Be(0);
		provider.MetalCommandQueue.Should().Be(0);
		provider.GRContext.Should().BeNull();
		using (provider.BeginFrame())
		{
			//An empty scope: nothing to lock when there is no device.
		}
	}

	[Theory]
	[InlineData(typeof(X11VulkanRenderer))]
	[InlineData(typeof(WaylandVulkanRenderer))]
	public void a_disposed_heads_renderer_says_disposed_not_not_created_yet(Type rendererType)
	{
		//Arrange: a renderer in its disposed state (built without a window; only the flag the owner reads is set).
		var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
		rendererType.GetField("_disposed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
			.SetValue(renderer, true);
		var provider = VulkanNativeGpuDeviceProvider.For(renderer)!;

		//Assert
		provider.IsAvailable.Should().BeFalse();
		provider.Reason.Should().Be(VulkanNativeGpuDeviceProvider.DisposedReason);
		AssertEmpty(provider);
	}

	private sealed class FakeOwner : IVulkanContextOwner
	{
		public VulkanContext? VulkanContext { get; set; }
	}

	private class FakeWaylandRenderer : IWaylandRenderer
	{
		public void SetBackgroundColor(SKColor color) { }
		public void Render() { }
		public void Dispose() { }
	}

	private sealed class FakeVulkanWaylandRenderer : FakeWaylandRenderer, IVulkanContextOwner
	{
		public VulkanContext? VulkanContext => null;
	}
}
