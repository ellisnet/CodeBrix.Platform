#nullable enable
// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan.Interop; //Was previously: Uno.UI.Runtime.Skia.Vulkan.Interop

internal unsafe partial class VulkanDevice
{
	/// <summary>
	/// Creates a VulkanDevice. Simplified from Avalonia's version:
	/// - Always prefers discrete GPUs
	/// - Always requires VK_KHR_swapchain
	/// - Does not require compute bit by default
	/// </summary>
	/// <param name="instance">The instance.</param>
	/// <param name="instanceApi">Its API.</param>
	/// <param name="checkSurface">A surface the device must be able to present to, or null.</param>
	/// <param name="forAdoption">True to also enable what an adopter needs (<see cref="VulkanAdoptionProfile"/>);
	/// the heads pass <c>DirectGpuRenderingMode.IsEnabled</c>, so with the latch off nothing changes.</param>
	public static VulkanDevice Create(VulkanInstance instance, VulkanInstanceApi instanceApi,
		VkSurfaceKHR? checkSurface = null, bool forAdoption = false)
	{
		uint deviceCount = 0;
		var vkInstance = instance.Handle;
		instanceApi.EnumeratePhysicalDevices(vkInstance, ref deviceCount, null)
			.ThrowOnError("vkEnumeratePhysicalDevices");

		if (deviceCount == 0)
			throw new VulkanException("No devices found");

		var devices = stackalloc VkPhysicalDevice[(int)deviceCount];
		instanceApi.EnumeratePhysicalDevices(vkInstance, ref deviceCount, devices)
			.ThrowOnError("vkEnumeratePhysicalDevices");

		DeviceInfo? compatibleDevice = null, discreteDevice = null;

		for (var c = 0; c < deviceCount; c++)
		{
			var info = CheckDevice(instanceApi, devices[c], checkSurface);
			if (info != null)
			{
				compatibleDevice ??= info;
				if (info.Value.Type == VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU)
					discreteDevice ??= info;
			}

			if (compatibleDevice != null && discreteDevice != null)
				break;
		}

		if (discreteDevice != null)
			compatibleDevice = discreteDevice;

		if (compatibleDevice == null)
			throw new VulkanException("No compatible devices found");

		var dev = compatibleDevice.Value;

		var queuePriorities = stackalloc float[(int)dev.QueueCount];
		for (var c = 0; c < dev.QueueCount; c++)
			queuePriorities[c] = 1f;

		var queueCreateInfo = new VkDeviceQueueCreateInfo
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO,
			queueFamilyIndex = dev.QueueFamilyIndex,
			queueCount = dev.QueueCount,
			pQueuePriorities = queuePriorities,
		};

		var enabledExtensions = new[] { VK_KHR_swapchain }
			.Concat(dev.Extensions.Where(e => e != VK_KHR_swapchain))
			.Distinct()
			.ToArray();

		// Only keep the swapchain extension plus any that are in the supported set
		enabledExtensions = new[] { VK_KHR_swapchain };

		using var adoption = forAdoption
			? VulkanAdoptionProfile.Create(instance, dev.PhysicalDevice, dev.Extensions, dev.ApiVersion)
			: null;
		if (adoption is not null)
		{
			enabledExtensions = enabledExtensions.Concat(adoption.Extensions).Distinct().ToArray();
			typeof(VulkanDevice).Log().Info(adoption.Description);
		}

		using var pEnabledExtensions = new Utf8BufferArray(enabledExtensions);

		// The head enables no features of its own (pEnabledFeatures stays null). With an adoption profile the chain is
		// the adopting engine's (or the fallback's): a future head feature must be MERGED into it - chained in front of
		// it, or OR-ed into a copy - never replace it, or the engine runs on features that are not enabled.
		var createInfo = new VkDeviceCreateInfo
		{
			sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
			pNext = adoption?.FeatureChain ?? IntPtr.Zero,
			queueCreateInfoCount = 1,
			pQueueCreateInfos = &queueCreateInfo,
			ppEnabledExtensionNames = pEnabledExtensions,
			enabledExtensionCount = pEnabledExtensions.UCount,
		};

		instanceApi.CreateDevice(dev.PhysicalDevice, ref createInfo, IntPtr.Zero, out var createdDevice)
			.ThrowOnError("vkCreateDevice");

		instanceApi.GetDeviceQueue(createdDevice, dev.QueueFamilyIndex, 0, out var createdQueue);

		return new VulkanDevice(instanceApi, createdDevice, dev.PhysicalDevice, createdQueue,
			dev.QueueFamilyIndex, enabledExtensions)
		{
			AdoptionRoute = adoption?.Route,
			AdoptionDescription = adoption?.Description,
		};
	}

	struct DeviceInfo
	{
		public VkPhysicalDevice PhysicalDevice;
		public uint QueueFamilyIndex;
		public VkPhysicalDeviceType Type;
		public List<string> Extensions;
		public uint QueueCount;
		public uint ApiVersion;
	}

	static List<string> GetDeviceExtensions(VulkanInstanceApi instance, VkPhysicalDevice physicalDevice)
	{
		uint propertyCount = 0;
		instance.EnumerateDeviceExtensionProperties(physicalDevice, null, ref propertyCount, null);
		var extensionProps = new VkExtensionProperties[propertyCount];
		var extensions = new List<string>((int)propertyCount);
		if (propertyCount != 0)
			fixed (VkExtensionProperties* ptr = extensionProps)
			{
				instance.EnumerateDeviceExtensionProperties(physicalDevice, null, ref propertyCount, ptr);

				for (var c = 0; c < propertyCount; c++)
					extensions.Add(Marshal.PtrToStringAnsi(new IntPtr(ptr[c].extensionName))!);
			}

		return extensions;
	}

	private const string VK_KHR_swapchain = "VK_KHR_swapchain";

	static DeviceInfo? CheckDevice(VulkanInstanceApi instance, VkPhysicalDevice physicalDevice,
		VkSurfaceKHR? surface)
	{
		instance.GetPhysicalDeviceProperties(physicalDevice, out var properties);

		var supportedExtensions = GetDeviceExtensions(instance, physicalDevice);
		if (!supportedExtensions.Contains(VK_KHR_swapchain))
			return null;

		uint familyCount = 0;
		instance.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref familyCount, null);
		var familyProperties = stackalloc VkQueueFamilyProperties[(int)familyCount];
		instance.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, ref familyCount, familyProperties);
		var requiredFlags = VkQueueFlags.VK_QUEUE_GRAPHICS_BIT;

		for (var c = 0; c < familyCount; c++)
		{
			if ((familyProperties[c].queueFlags & requiredFlags) != requiredFlags)
				continue;
			if (surface.HasValue)
			{
				instance.GetPhysicalDeviceSurfaceSupportKHR(physicalDevice, (uint)c, surface.Value, out var supported)
					.ThrowOnError("vkGetPhysicalDeviceSurfaceSupportKHR");
				if (supported == 0)
					continue;
			}

			return new DeviceInfo
			{
				PhysicalDevice = physicalDevice,
				Extensions = supportedExtensions,
				Type = properties.deviceType,
				QueueFamilyIndex = (uint)c,
				QueueCount = familyProperties[c].queueCount,
				ApiVersion = properties.apiVersion,
			};
		}

		return null;
	}
}
