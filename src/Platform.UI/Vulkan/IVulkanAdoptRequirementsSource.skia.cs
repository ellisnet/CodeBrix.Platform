#nullable enable

using System;
using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan;

/// <summary>
/// Answers, for one physical device, what a head's <c>VkDevice</c> must be created with so that an add-in can ADOPT it:
/// the device extensions and the feature chain the adopting engine itself requires. Implemented by the add-in that
/// adopts head devices (Platform.UI cannot reference its engine) and registered in
/// <see cref="VulkanAdoptionProfile.RequirementsSource"/>; asked by <see cref="VulkanAdoptionProfile.Create"/> only while
/// direct GPU rendering is enabled.
/// </summary>
internal interface IVulkanAdoptRequirementsSource
{
	/// <summary>
	/// Asks the engine. The inputs are exactly what the later adoption describes the instance with, so the answer is for
	/// that adoption and no other.
	/// </summary>
	/// <param name="query">The instance and physical device.</param>
	/// <returns>The requirements; the caller disposes them once the device has been created.</returns>
	/// <exception cref="Exception">Anything: the engine cannot use the physical device, it has no Vulkan backend, its
	/// library cannot be loaded... The caller then uses its fallback profile.</exception>
	IVulkanAdoptRequirements Query(VulkanAdoptQuery query);
}

/// <summary>What <see cref="IVulkanAdoptRequirementsSource.Query"/> is asked about.</summary>
/// <param name="Instance">The <c>VkInstance</c>.</param>
/// <param name="PhysicalDevice">The <c>VkPhysicalDevice</c> the device is about to be created on.</param>
/// <param name="InstanceApiVersion">The <c>apiVersion</c> the instance was created with (VK_MAKE_API_VERSION encoding).</param>
/// <param name="GetInstanceProcAddr">The loader's <c>vkGetInstanceProcAddr</c>, as the head hands it to adopters.</param>
/// <param name="EnabledInstanceExtensions">The instance extensions the instance was created with.</param>
internal readonly record struct VulkanAdoptQuery(
	IntPtr Instance,
	IntPtr PhysicalDevice,
	uint InstanceApiVersion,
	IntPtr GetInstanceProcAddr,
	IReadOnlyList<string> EnabledInstanceExtensions);

/// <summary>
/// The engine's answer: the device extensions to enable and the <c>VkPhysicalDeviceFeatures2</c> chain to create the
/// device with. Owns that chain's memory (it is the engine library's) until disposed.
/// </summary>
internal interface IVulkanAdoptRequirements : IDisposable
{
	/// <summary>The device extensions the engine requires on this physical device.</summary>
	IReadOnlyList<string> RequiredDeviceExtensions { get; }

	/// <summary>
	/// A <c>VkPhysicalDeviceFeatures2*</c> with its <c>pNext</c> chain: what the engine enables. For
	/// <c>VkDeviceCreateInfo.pNext</c> as-is (with <c>pEnabledFeatures</c> null); never written into, never enabled
	/// less of. Zero when the engine needs no features. Valid until <see cref="IDisposable.Dispose"/>.
	/// </summary>
	IntPtr PhysicalDeviceFeatures2 { get; }

	/// <summary>The physical device's <c>VkPhysicalDeviceProperties.apiVersion</c>, as the engine saw it.</summary>
	uint PhysicalDeviceApiVersion { get; }
}
