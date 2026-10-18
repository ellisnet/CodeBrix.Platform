using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.WebGpu.Native;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>
/// The WebGPU engine's answer to "what must a head's <c>VkDevice</c> enable so that this add-in can adopt it?"
/// (<see cref="VulkanAdoptRequirements.Query"/>), for the heads' Vulkan device creation
/// (<see cref="VulkanAdoptionProfile"/>). Registered by <see cref="Skia.SkiaPlatformBootstrap"/>.
/// </summary>
/// <remarks>
/// The query's descriptor is filled exactly as <see cref="CodeBrix.WebGpu.Skia.WebGpuSkiaDevice.AdoptVulkan"/> later
/// fills the adoption's (through <see cref="WebGpuVulkanDirectPresenter"/>): the head's instance and physical device,
/// the API version the head created its instance with, <c>vkGetInstanceProcAddr</c> as the library resolves it through
/// the backend context (the head's entry point asked for itself), the head's enabled instance extensions, and no
/// required features (the view asks for none on an adopted device). Otherwise the answer would be for a different
/// adoption.
/// </remarks>
internal sealed unsafe class WebGpuVulkanAdoptRequirementsSource : IVulkanAdoptRequirementsSource
{
	/// <inheritdoc/>
	public IVulkanAdoptRequirements Query(VulkanAdoptQuery query)
	{
		if (query.Instance == 0 || query.PhysicalDevice == 0 || query.GetInstanceProcAddr == 0)
		{
			throw new ArgumentException("The instance, the physical device and vkGetInstanceProcAddr are all needed.", nameof(query));
		}

		// As the adoption resolves it: the backend context's procedure lookup asks the head's entry point for
		// "vkGetInstanceProcAddr" on the instance.
		var getInstanceProcAddr = Resolve(query.GetInstanceProcAddr, query.Instance, "vkGetInstanceProcAddr");
		if (getInstanceProcAddr == 0)
		{
			throw new InvalidOperationException("The head's vkGetInstanceProcAddr cannot resolve vkGetInstanceProcAddr.");
		}

		var names = AllocateNames(query.EnabledInstanceExtensions, out var allocations);
		try
		{
			var descriptor = new CodebrixVulkanAdoptDescriptor
			{
				Instance = (void*)query.Instance,
				PhysicalDevice = (void*)query.PhysicalDevice,
				InstanceApiVersion = query.InstanceApiVersion,
				GetInstanceProcAddr = (void*)getInstanceProcAddr,
				EnabledInstanceExtensionCount = (nuint)query.EnabledInstanceExtensions.Count,
				EnabledInstanceExtensions = names,
			};

			return new Requirements(VulkanAdoptRequirements.Query(&descriptor));
		}
		finally
		{
			foreach (var allocation in allocations)
			{
				NativeMemory.Free((void*)allocation);
			}
		}
	}

	// The extension names as a CodebrixName array in native memory (the query reads them; it keeps nothing).
	private static CodebrixName* AllocateNames(IReadOnlyList<string> names, out List<nint> allocations)
	{
		allocations = new List<nint>(names.Count + 1);
		if (names.Count == 0)
		{
			return null;
		}

		var array = (CodebrixName*)NativeMemory.AllocZeroed((nuint)(sizeof(CodebrixName) * names.Count));
		allocations.Add((nint)array);
		for (var index = 0; index < names.Count; index++)
		{
			var utf8 = Encoding.UTF8.GetBytes(names[index] ?? string.Empty);
			var text = (byte*)NativeMemory.Alloc((nuint)utf8.Length + 1);
			allocations.Add((nint)text);
			Marshal.Copy(utf8, 0, (nint)text, utf8.Length);
			text[utf8.Length] = 0;
			array[index].Name = WgpuString.FromPinned(text, utf8.Length);
		}

		return array;
	}

	private static nint Resolve(nint getInstanceProcAddr, nint instance, string name)
	{
		var bytes = (byte*)Marshal.StringToCoTaskMemUTF8(name);
		try
		{
			return ((delegate* unmanaged<nint, byte*, nint>)getInstanceProcAddr)(instance, bytes);
		}
		finally
		{
			Marshal.FreeCoTaskMem((nint)bytes);
		}
	}

	/// <summary>The engine's requirements behind the head's interface; disposing frees the engine's feature chain.</summary>
	private sealed class Requirements : IVulkanAdoptRequirements
	{
		private readonly VulkanAdoptRequirements _requirements;

		internal Requirements(VulkanAdoptRequirements requirements) => _requirements = requirements;

		public IReadOnlyList<string> RequiredDeviceExtensions => _requirements.RequiredDeviceExtensions;

		public nint PhysicalDeviceFeatures2 => _requirements.PhysicalDeviceFeatures2;

		public uint PhysicalDeviceApiVersion => _requirements.PhysicalDeviceApiVersion;

		public void Dispose() => _requirements.Dispose();
	}
}
