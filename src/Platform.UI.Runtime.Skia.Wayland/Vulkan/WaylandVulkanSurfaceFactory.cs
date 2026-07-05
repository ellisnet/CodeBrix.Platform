using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.Interop;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Vulkan;

[StructLayout(LayoutKind.Sequential)]
internal struct VkWaylandSurfaceCreateInfoKHR
{
	public const uint VK_STRUCTURE_TYPE_WAYLAND_SURFACE_CREATE_INFO_KHR = 1000006000;
	public uint sType;
	public IntPtr pNext;
	public uint flags;
	public IntPtr display; // wl_display*
	public IntPtr surface; // wl_surface*
}

internal class WaylandVulkanSurfaceFactory : IVulkanPlatformSurfaceFactory
{
	[DllImport("libvulkan.so.1", EntryPoint = "vkGetInstanceProcAddr")]
	private static extern IntPtr NativeGetInstanceProcAddr(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)] string name);

	private static bool _vulkanAvailable;
	private static bool _vulkanChecked;

	private readonly IntPtr _display;

	public WaylandVulkanSurfaceFactory(IntPtr display)
	{
		_display = display;
	}

	public IReadOnlyList<string> RequiredInstanceExtensions { get; } = new[] { "VK_KHR_wayland_surface" };

	public VkGetInstanceProcAddressDelegate GetVkGetInstanceProcAddr()
	{
		EnsureVulkanAvailable();
		return NativeGetInstanceProcAddr;
	}

	public ulong CreateSurface(VulkanInstance instance, IntPtr nativeWindowHandle)
	{
		if (nativeWindowHandle == IntPtr.Zero)
			throw new ArgumentException("wl_surface handle cannot be zero", nameof(nativeWindowHandle));

		var createSurfacePtr = instance.GetInstanceProcAddress(instance.Handle.Handle, "vkCreateWaylandSurfaceKHR");
		if (createSurfacePtr == IntPtr.Zero)
			throw new VulkanException("Failed to load vkCreateWaylandSurfaceKHR");

		var vkCreateWaylandSurfaceKHR = Marshal.GetDelegateForFunctionPointer<PFN_vkCreateWaylandSurfaceKHR>(createSurfacePtr);

		var createInfo = new VkWaylandSurfaceCreateInfoKHR
		{
			sType = VkWaylandSurfaceCreateInfoKHR.VK_STRUCTURE_TYPE_WAYLAND_SURFACE_CREATE_INFO_KHR,
			display = _display,
			surface = nativeWindowHandle
		};

		var result = vkCreateWaylandSurfaceKHR(instance.Handle.Handle, ref createInfo, IntPtr.Zero, out var surface);
		if (result != 0)
			throw new VulkanException($"vkCreateWaylandSurfaceKHR failed with result {result}");

		return surface;
	}

	public static bool IsVulkanAvailable()
	{
		if (!_vulkanChecked)
		{
			_vulkanChecked = true;
			try
			{
				NativeGetInstanceProcAddr(IntPtr.Zero, "vkEnumerateInstanceVersion");
				_vulkanAvailable = true;
			}
			catch (DllNotFoundException)
			{
				_vulkanAvailable = false;
			}
			catch (EntryPointNotFoundException)
			{
				_vulkanAvailable = true;
			}
		}
		return _vulkanAvailable;
	}

	private static void EnsureVulkanAvailable()
	{
		if (!IsVulkanAvailable())
			throw new VulkanException("libvulkan.so.1 not found");
	}

	private delegate int PFN_vkCreateWaylandSurfaceKHR(IntPtr instance, ref VkWaylandSurfaceCreateInfoKHR pCreateInfo, IntPtr pAllocator, out ulong pSurface);
}
