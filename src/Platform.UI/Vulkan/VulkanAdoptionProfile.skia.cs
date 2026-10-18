#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.Interop;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan;

/// <summary>How a <see cref="VulkanAdoptionProfile"/> decided what the device enables.</summary>
internal enum VulkanAdoptionRoute
{
	/// <summary>The adopting engine said what it requires of this physical device (<see cref="IVulkanAdoptRequirementsSource"/>).</summary>
	EngineQuery,

	/// <summary>
	/// No engine answer (no source registered, or the query failed): the fallback extension list and every supported
	/// feature.
	/// </summary>
	Fallback,
}

/// <summary>
/// What a head's <c>VkDevice</c> enables, beyond what the head itself needs, so that an add-in can ADOPT it (see
/// <c>INativeGpuDeviceProvider</c>): the device extensions and features the adopting WebGPU engine requires of a device
/// it did not create. Applied by <see cref="VulkanDevice"/> only while direct GPU rendering is enabled
/// (<c>UseDirectGpuRenderingMode()</c>); with the latch off device creation is exactly what it always was.
/// </summary>
/// <remarks>
/// <para>
/// THE ENGINE IS ASKED. The add-in that adopts head devices registers an <see cref="IVulkanAdoptRequirementsSource"/>
/// (<see cref="RequirementsSource"/>), which asks the engine what IT requires of this very physical device: the device
/// extensions, and a ready-made <c>VkPhysicalDeviceFeatures2</c> chain of the features it enables. The device is created
/// with exactly those (plus the head's own VK_KHR_swapchain), the chain as <c>VkDeviceCreateInfo.pNext</c> unchanged.
/// The head needs no features of its own today; a future head feature must be MERGED into that chain (chained in front
/// of it, or a copy with the head's bits OR-ed in), never replace it - the engine assumes, without being able to check,
/// that everything in the chain is enabled.
/// </para>
/// <para>
/// TIMING. The X11 head creates the window's device when the window opens, usually before anything has loaded the
/// add-in's assembly. When no source is registered yet, the add-in is therefore loaded by name
/// (<see cref="AddInBootstrapTypeName"/>) and its module initializer run, which registers the source. An application
/// that does not reference the add-in has nothing to load, and the fallback applies (nothing would adopt the device
/// anyway).
/// </para>
/// <para>
/// FALLBACK. With no source, or when the query fails (the engine cannot use the physical device, its library has no
/// Vulkan backend, ...), the profile enables <see cref="FallbackDeviceExtensions"/> where supported and every SUPPORTED
/// feature of the Vulkan 1.0-1.3 feature structures (and of VK_EXT_robustness2, when that extension is enabled) - a
/// superset of what the engine asks of common adapters, and always valid Vulkan.
/// </para>
/// <para>
/// Every extension is enabled ONLY where the physical device supports it, and nothing here can make device creation
/// fail. A device that lacks something the engine needs is simply refused at adoption, and the adopter falls back to
/// its read-back path.
/// </para>
/// </remarks>
internal sealed unsafe class VulkanAdoptionProfile : IDisposable
{
	/// <summary>
	/// The assembly-qualified name of the WebGpu add-in's bootstrap, whose module initializer registers the add-in's
	/// <see cref="IVulkanAdoptRequirementsSource"/>. Fenced by the add-in's tests: a rename there breaks a test, not an
	/// application.
	/// </summary>
	internal const string AddInBootstrapTypeName =
		"CodeBrix.Platform.UI.WebGpu.Skia.SkiaPlatformBootstrap, CodeBrix.Platform.UI.WebGpu.Skia";

	/// <summary>
	/// The FALLBACK device extensions, used only when the engine cannot be asked (see the class remarks), each enabled
	/// only where supported. VK_KHR_image_format_list is a dependency of VK_KHR_swapchain_mutable_format on devices
	/// older than Vulkan 1.2.
	/// </summary>
	internal static readonly IReadOnlyList<string> FallbackDeviceExtensions = new[]
	{
		"VK_KHR_swapchain_mutable_format",
		"VK_KHR_image_format_list",
		"VK_EXT_robustness2",
		"VK_KHR_external_memory_fd",
		"VK_EXT_external_memory_dma_buf",
		"VK_EXT_memory_budget",
	};

	private const string Swapchain = "VK_KHR_swapchain";
	private const string SwapchainMutableFormat = "VK_KHR_swapchain_mutable_format";
	private const string ImageFormatList = "VK_KHR_image_format_list";
	private const string Robustness2 = "VK_EXT_robustness2";

	// VkStructureType values (vulkan_core.h).
	private const uint PhysicalDeviceFeatures2 = 1000059000;
	private const uint PhysicalDeviceVulkan11Features = 49;
	private const uint PhysicalDeviceVulkan12Features = 51;
	private const uint PhysicalDeviceVulkan13Features = 53;
	private const uint PhysicalDeviceRobustness2FeaturesExt = 1000286000;

	// Larger than any of the feature structures chained (the largest, Vulkan12Features, is about 200 bytes).
	private const int BlockSize = 1024;

	private static IVulkanAdoptRequirementsSource? _requirementsSource;

	private readonly List<IntPtr> _blocks = new();
	private IVulkanAdoptRequirements? _requirements;

	private VulkanAdoptionProfile(VulkanAdoptionRoute route, string[] extensions, uint instanceApiVersion, uint deviceApiVersion)
	{
		Route = route;
		Extensions = extensions;
		InstanceApiVersion = instanceApiVersion;
		DeviceApiVersion = deviceApiVersion;
	}

	/// <summary>
	/// The engine's requirements source, registered by the add-in that adopts head devices (its bootstrap), or null.
	/// Any thread.
	/// </summary>
	internal static IVulkanAdoptRequirementsSource? RequirementsSource
	{
		get => Volatile.Read(ref _requirementsSource);
		set => Volatile.Write(ref _requirementsSource, value);
	}

	/// <summary>Which route decided what the device enables.</summary>
	public VulkanAdoptionRoute Route { get; }

	/// <summary>The extensions to enable beyond the head's own (only supported ones; never VK_KHR_swapchain).</summary>
	public string[] Extensions { get; }

	/// <summary>
	/// The <c>VkPhysicalDeviceFeatures2</c> chain for <c>VkDeviceCreateInfo.pNext</c>: the engine's own chain, or (the
	/// fallback) one filled with every supported feature. Zero when there is none, in which case no features are
	/// enabled.
	/// </summary>
	public IntPtr FeatureChain { get; private set; }

	/// <summary>The fallback's feature structures in the chain, by name (empty on the engine route).</summary>
	public List<string> FeatureStructures { get; } = new();

	/// <summary>The API version the instance was created with.</summary>
	public uint InstanceApiVersion { get; }

	/// <summary>The physical device's API version.</summary>
	public uint DeviceApiVersion { get; }

	/// <summary>Why the fallback was used (null on the engine route).</summary>
	public string? FallbackReason { get; private set; }

	/// <summary>The one Information line the device logs: which route, the extensions enabled, the API versions.</summary>
	public string Description => Route == VulkanAdoptionRoute.EngineQuery
		? "Direct GPU rendering: the Vulkan device enables, for adoption, what the WebGPU engine requires of this GPU "
			+ "(engine query): the extensions [" + string.Join(", ", Extensions) + "] and the engine's feature chain; "
			+ "instance API " + FormatVersion(InstanceApiVersion) + ", device API " + FormatVersion(DeviceApiVersion) + "."
		: "Direct GPU rendering: the WebGPU engine could not be asked (" + FallbackReason + "); the Vulkan device enables, "
			+ "for adoption, the fallback list: the extensions [" + string.Join(", ", Extensions)
			+ "] and every supported feature of [" + string.Join(", ", FeatureStructures) + "]; instance API "
			+ FormatVersion(InstanceApiVersion) + ", device API " + FormatVersion(DeviceApiVersion) + ".";

	/// <summary>
	/// The registered source, or - when there is none yet - the one the add-in registers once its assembly is loaded
	/// by name and its module initializer has run; null when the application does not contain the add-in.
	/// </summary>
	/// <returns>The source, or null.</returns>
	internal static IVulkanAdoptRequirementsSource? FindRequirementsSource()
	{
		if (RequirementsSource is { } registered)
		{
			return registered;
		}

		try
		{
			// Loading an assembly does not run its module initializer; RunModuleConstructor does (once per module).
			if (Type.GetType(AddInBootstrapTypeName, throwOnError: false) is { } bootstrap)
			{
				RuntimeHelpers.RunModuleConstructor(bootstrap.Module.ModuleHandle);
			}
		}
		catch (Exception)
		{
			// An assembly that exists but cannot be loaded is the same as no add-in: the fallback applies.
		}

		return RequirementsSource;
	}

	/// <summary>Which of <see cref="FallbackDeviceExtensions"/> to enable on a device supporting <paramref name="supported"/>.</summary>
	/// <param name="supported">The device's supported extensions.</param>
	/// <returns>The extensions to add, in list order.</returns>
	internal static string[] SelectFallbackExtensions(IReadOnlyCollection<string> supported) =>
		FallbackDeviceExtensions.Where(supported.Contains).ToArray();

	/// <summary>
	/// Which of the engine's <paramref name="required"/> extensions to enable beyond the head's VK_KHR_swapchain: those
	/// the device supports (defensive - the engine names only supported ones), once each, in the engine's order. When
	/// the engine names VK_KHR_swapchain_mutable_format on a device used below Vulkan 1.2, VK_KHR_image_format_list (its
	/// dependency there) is added if the engine did not name it and the device supports it.
	/// </summary>
	/// <param name="required">The engine's required extensions.</param>
	/// <param name="supported">The device's supported extensions.</param>
	/// <param name="usableApiVersion">The lower of the instance's and the device's API versions.</param>
	/// <returns>The extensions to add.</returns>
	internal static string[] SelectEngineExtensions(IReadOnlyList<string> required, IReadOnlyCollection<string> supported, uint usableApiVersion)
	{
		var selected = new List<string>(required.Count + 1);
		foreach (var name in required)
		{
			if (name != Swapchain && supported.Contains(name) && !selected.Contains(name))
			{
				selected.Add(name);
			}
		}

		if (selected.Contains(SwapchainMutableFormat) && !selected.Contains(ImageFormatList)
			&& usableApiVersion < MakeVersion(1, 2) && supported.Contains(ImageFormatList))
		{
			selected.Add(ImageFormatList);
		}

		return selected.ToArray();
	}

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

	/// <summary>Builds the profile for <paramref name="physicalDevice"/>: the engine's answer, or the fallback.</summary>
	/// <param name="instance">The instance.</param>
	/// <param name="physicalDevice">The physical device.</param>
	/// <param name="supported">Its supported device extensions.</param>
	/// <param name="deviceApiVersion">Its <c>VkPhysicalDeviceProperties.apiVersion</c>.</param>
	/// <returns>The profile (dispose it after <c>vkCreateDevice</c>).</returns>
	internal static VulkanAdoptionProfile Create(VulkanInstance instance, VkPhysicalDevice physicalDevice,
		IReadOnlyCollection<string> supported, uint deviceApiVersion)
	{
		var usable = Math.Min(instance.ApiVersion, deviceApiVersion);
		string reason;
		if (FindRequirementsSource() is not { } source)
		{
			reason = "no adopt-requirements source is registered: the application does not contain the add-in that adopts head devices";
		}
		else
		{
			IVulkanAdoptRequirements? requirements = null;
			try
			{
				// Exactly what the adopter later describes the instance with (VulkanContext.CaptureNativeDeviceInfo): the
				// handle, the apiVersion the instance was created with, the loader's vkGetInstanceProcAddr and the
				// enabled instance extensions.
				requirements = source.Query(new VulkanAdoptQuery(
					instance.Handle.Handle,
					physicalDevice.Handle,
					instance.ApiVersion,
					instance.GetInstanceProcAddress(instance.Handle.Handle, "vkGetInstanceProcAddr"),
					instance.EnabledExtensions));
				var profile = new VulkanAdoptionProfile(VulkanAdoptionRoute.EngineQuery,
					SelectEngineExtensions(requirements.RequiredDeviceExtensions, supported, usable),
					instance.ApiVersion, deviceApiVersion);
				profile._requirements = requirements;
				profile.FeatureChain = requirements.PhysicalDeviceFeatures2;
				return profile;
			}
			catch (Exception e)
			{
				requirements?.Dispose();
				reason = "the engine's adopt-requirements query failed: " + e.Message;
			}
		}

		return CreateFallback(instance, physicalDevice, supported, deviceApiVersion, reason);
	}

	/// <summary>The fallback profile: <see cref="FallbackDeviceExtensions"/> where supported, every supported feature.</summary>
	/// <param name="instance">The instance.</param>
	/// <param name="physicalDevice">The physical device.</param>
	/// <param name="supported">Its supported device extensions.</param>
	/// <param name="deviceApiVersion">Its <c>VkPhysicalDeviceProperties.apiVersion</c>.</param>
	/// <param name="reason">Why the engine could not be asked.</param>
	/// <returns>The profile (dispose it after <c>vkCreateDevice</c>).</returns>
	private static VulkanAdoptionProfile CreateFallback(VulkanInstance instance, VkPhysicalDevice physicalDevice,
		IReadOnlyCollection<string> supported, uint deviceApiVersion, string reason)
	{
		var profile = new VulkanAdoptionProfile(VulkanAdoptionRoute.Fallback, SelectFallbackExtensions(supported),
			instance.ApiVersion, deviceApiVersion)
		{
			FallbackReason = reason,
		};
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

	/// <summary>VK_MAKE_API_VERSION(0, major, minor, 0).</summary>
	/// <param name="major">The major version.</param>
	/// <param name="minor">The minor version.</param>
	/// <returns>The packed version.</returns>
	internal static uint MakeVersion(uint major, uint minor) => major << 22 | minor << 12;

	/// <summary>"1.3.280" for a packed version.</summary>
	/// <param name="version">The packed version.</param>
	/// <returns>The text.</returns>
	internal static string FormatVersion(uint version) => $"{version >> 22 & 0x7F}.{version >> 12 & 0x3FF}.{version & 0xFFF}";

	private IntPtr Block(uint sType, IntPtr next, string name)
	{
		var block = (IntPtr)NativeMemory.AllocZeroed(BlockSize);
		_blocks.Add(block);
		*(uint*)block = sType;
		*(IntPtr*)(block + IntPtr.Size) = next;
		FeatureStructures.Add(name);
		return block;
	}

	/// <summary>Frees the feature chain (the engine's, through its requirements; or the fallback's own blocks).</summary>
	public void Dispose()
	{
		_requirements?.Dispose();
		_requirements = null;
		foreach (var block in _blocks)
		{
			NativeMemory.Free((void*)block);
		}

		_blocks.Clear();
		FeatureChain = IntPtr.Zero;
	}
}
