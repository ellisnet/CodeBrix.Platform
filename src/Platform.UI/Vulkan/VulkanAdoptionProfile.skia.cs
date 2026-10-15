#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.Interop;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan;

/// <summary>
/// What a head's <c>VkDevice</c> enables, beyond what the head itself needs, so that an add-in can ADOPT it (see
/// <c>INativeGpuDeviceProvider</c>): the device extensions and features the WebGPU engine requires of a device it did
/// not create. Applied by <see cref="VulkanDevice"/> only while direct GPU rendering is enabled
/// (<c>UseDirectGpuRenderingMode()</c>); with the latch off device creation is exactly what it always was.
/// </summary>
/// <remarks>
/// <para>
/// Everything is enabled ONLY where the physical device supports it; nothing here can make device creation fail. A
/// device that lacks something the engine needs is simply refused at adoption, and the adopter falls back to its
/// read-back path.
/// </para>
/// <para>
/// THE LIST TRACKS THE ENGINE'S EXPECTATIONS for wgpu 29.0.3 via wgpu-native v29.0.1.1 and must be revisited on an
/// engine bump. The engine refuses a device created without the extensions its Vulkan backend would enable on that
/// adapter (the ones below, where supported), and it assumes - without being able to check - that the core and
/// extension features it would request are enabled. Since the set it requests depends on the adapter, every
/// SUPPORTED feature of the Vulkan 1.0-1.3 feature structures (and of VK_EXT_robustness2, when that extension is
/// enabled) is enabled, which is a superset and always valid Vulkan.
/// </para>
/// </remarks>
internal sealed unsafe class VulkanAdoptionProfile : IDisposable
{
	/// <summary>
	/// The device extensions the engine requires of an adopted device (wgpu 29.0.3 via wgpu-native v29.0.1.1), each
	/// enabled only where supported. VK_KHR_image_format_list is a dependency of VK_KHR_swapchain_mutable_format on
	/// devices older than Vulkan 1.2.
	/// </summary>
	internal static readonly IReadOnlyList<string> EngineDeviceExtensions = new[]
	{
		"VK_KHR_swapchain_mutable_format",
		"VK_KHR_image_format_list",
		"VK_EXT_robustness2",
		"VK_KHR_external_memory_fd",
		"VK_EXT_external_memory_dma_buf",
		"VK_EXT_memory_budget",
	};

	private const string Robustness2 = "VK_EXT_robustness2";

	// VkStructureType values (vulkan_core.h).
	private const uint PhysicalDeviceFeatures2 = 1000059000;
	private const uint PhysicalDeviceVulkan11Features = 49;
	private const uint PhysicalDeviceVulkan12Features = 51;
	private const uint PhysicalDeviceVulkan13Features = 53;
	private const uint PhysicalDeviceRobustness2FeaturesExt = 1000286000;

	// Larger than any of the feature structures chained (the largest, Vulkan12Features, is about 200 bytes).
	private const int BlockSize = 1024;

	private readonly List<IntPtr> _blocks = new();

	private VulkanAdoptionProfile(string[] extensions) => Extensions = extensions;

	/// <summary>The extensions to enable beyond the head's own (only supported ones).</summary>
	public string[] Extensions { get; }

	/// <summary>
	/// The <c>VkPhysicalDeviceFeatures2</c> chain filled with every supported feature, for <c>VkDeviceCreateInfo.pNext</c>
	/// (zero when the instance cannot query features, in which case no features are enabled).
	/// </summary>
	public IntPtr FeatureChain { get; private set; }

	/// <summary>The feature structures in the chain, by name (for the log line and the tests).</summary>
	public List<string> FeatureStructures { get; } = new();

	/// <summary>Which of <see cref="EngineDeviceExtensions"/> to enable on a device supporting <paramref name="supported"/>.</summary>
	/// <param name="supported">The device's supported extensions.</param>
	/// <returns>The extensions to add, in list order.</returns>
	internal static string[] SelectExtensions(IReadOnlyCollection<string> supported) =>
		EngineDeviceExtensions.Where(supported.Contains).ToArray();

	/// <summary>
	/// Which Vulkan core feature structures may be chained for a device usable at <paramref name="apiVersion"/>
	/// (the lower of the instance's and the device's versions): 1.1, 1.2 and 1.3 each need that version.
	/// </summary>
	/// <param name="apiVersion">The usable API version (VK_MAKE_API_VERSION encoding).</param>
	/// <returns>The minor versions whose feature structure may be chained.</returns>
	internal static int[] CoreFeatureMinorVersions(uint apiVersion)
	{
		var major = apiVersion >> 22 & 0x7F;
		var minor = major > 1 ? 3 : (int)(apiVersion >> 12 & 0x3FF);
		return Enumerable.Range(1, 3).Where(m => m <= minor).ToArray();
	}

	/// <summary>Builds the profile for <paramref name="physicalDevice"/>.</summary>
	/// <param name="instance">The instance.</param>
	/// <param name="physicalDevice">The physical device.</param>
	/// <param name="supported">Its supported device extensions.</param>
	/// <param name="deviceApiVersion">Its <c>VkPhysicalDeviceProperties.apiVersion</c>.</param>
	/// <returns>The profile (dispose it after <c>vkCreateDevice</c>).</returns>
	internal static VulkanAdoptionProfile Create(VulkanInstance instance, VkPhysicalDevice physicalDevice,
		IReadOnlyCollection<string> supported, uint deviceApiVersion)
	{
		var profile = new VulkanAdoptionProfile(SelectExtensions(supported));
		var getFeatures2 = instance.GetInstanceProcAddress(instance.Handle.Handle, "vkGetPhysicalDeviceFeatures2");
		if (getFeatures2 == IntPtr.Zero)
		{
			return profile;
		}

		var usable = Math.Min(instance.ApiVersion, deviceApiVersion);
		var chain = IntPtr.Zero;
		if (profile.Extensions.Contains(Robustness2))
		{
			chain = profile.Block(PhysicalDeviceRobustness2FeaturesExt, chain, "Robustness2");
		}

		foreach (var minor in CoreFeatureMinorVersions(usable).Reverse())
		{
			var sType = minor switch
			{
				3 => PhysicalDeviceVulkan13Features,
				2 => PhysicalDeviceVulkan12Features,
				_ => PhysicalDeviceVulkan11Features,
			};
			chain = profile.Block(sType, chain, "Vulkan1" + minor);
		}

		chain = profile.Block(PhysicalDeviceFeatures2, chain, "Vulkan10");
		profile.FeatureStructures.Reverse();

		// Filled with what the device SUPPORTS; handed to vkCreateDevice unchanged, that enables exactly those.
		((delegate* unmanaged<IntPtr, IntPtr, void>)getFeatures2)(physicalDevice.Handle, chain);
		profile.FeatureChain = chain;
		return profile;
	}

	private IntPtr Block(uint sType, IntPtr next, string name)
	{
		var block = (IntPtr)NativeMemory.AllocZeroed(BlockSize);
		_blocks.Add(block);
		*(uint*)block = sType;
		*(IntPtr*)(block + IntPtr.Size) = next;
		FeatureStructures.Add(name);
		return block;
	}

	/// <summary>Frees the feature chain.</summary>
	public void Dispose()
	{
		foreach (var block in _blocks)
		{
			NativeMemory.Free((void*)block);
		}

		_blocks.Clear();
		FeatureChain = IntPtr.Zero;
	}
}
