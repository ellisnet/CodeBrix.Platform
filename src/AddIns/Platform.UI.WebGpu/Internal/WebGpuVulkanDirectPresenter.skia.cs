using System;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Graphics;
using CodeBrix.WebGpu.Native;
using CodeBrix.WebGpu.Skia;
using SkiaSharp;
using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>
/// The DirectVulkan render path (the X11 head on its Vulkan renderer, the Wayland head on its Vulkan renderer): the
/// head's <c>VkDevice</c> adopted as a WebGPU device on the head's own queue, frame textures described to Skia as
/// <c>VkImage</c>s in <c>VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL</c>.
/// </summary>
/// <remarks>
/// The textures are BGRA8Unorm, which the engine creates as <c>VK_FORMAT_B8G8R8A8_UNORM</c>: the format the heads'
/// own Vulkan surfaces use (Skia's <see cref="SKColorType.Bgra8888"/> on its Vulkan backend wraps it directly, no
/// swizzle), and the format every other render path renders in, so a renderer's pipelines stay valid on all of them.
/// The <c>VkFormat</c> handed to Skia is the one the engine reports for the texture, never assumed.
/// </remarks>
internal sealed unsafe class WebGpuVulkanDirectPresenter : WebGpuDirectPresenter
{
	// Vulkan constants (vulkan_core.h).
	private const uint VkImageTilingOptimal = 0;
	private const uint VkImageLayoutShaderReadOnlyOptimal = 5;
	private const uint VkSharingModeExclusive = 0;
	private const uint VkImageUsageTransferSrc = 0x1;
	private const uint VkImageUsageTransferDst = 0x2;
	private const uint VkImageUsageSampled = 0x4;
	private const uint VkImageUsageStorage = 0x8;
	private const uint VkImageUsageColorAttachment = 0x10;

	private GRVkBackendContext? _backendContext;

	internal WebGpuVulkanDirectPresenter(INativeGpuDeviceProvider provider, GRContext grContext)
		: base(provider, grContext)
	{
	}

	/// <inheritdoc/>
	internal override WebGpuRenderPath Path => WebGpuRenderPath.DirectVulkan;

	/// <summary>
	/// Adopts the head's device: the backend context is built from the provider's handles (the same instance,
	/// physical device, device, queue and queue family Skia renders the window with), its enabled extension lists, the
	/// API version the head created its instance with (the library caps Skia's own use itself) and the queue's index
	/// in its family - which makes WebGPU submit to the head's queue.
	/// </summary>
	/// <param name="provider">The provider.</param>
	/// <param name="grContext">The head's GPU context.</param>
	/// <returns>The adopted device.</returns>
	private protected override WebGpuSkiaDevice AdoptDevice(INativeGpuDeviceProvider provider, GRContext grContext)
	{
		var getInstanceProcAddr = provider.VkGetInstanceProcAddr;
		if (getInstanceProcAddr == 0)
		{
			// No loader entry point: the library reports the adoption as unavailable with its reason.
			_backendContext = new GRVkBackendContext();
			return WebGpuSkiaDevice.AdoptVulkan(grContext, _backendContext, provider.VkInstanceExtensions, provider.VkDeviceExtensions);
		}

		_backendContext = new GRVkBackendContext
		{
			VkInstance = provider.VkInstance,
			VkPhysicalDevice = provider.VkPhysicalDevice,
			VkDevice = provider.VkDevice,
			VkQueue = provider.VkQueue,
			GraphicsQueueIndex = provider.VkQueueFamilyIndex,
			MaxAPIVersion = provider.VkApiVersion,
			GetProcedureAddress = (name, instance, device) => Resolve(getInstanceProcAddr, instance != 0 ? instance : provider.VkInstance, name),
		};

		return WebGpuSkiaDevice.AdoptVulkan(grContext, _backendContext, provider.VkInstanceExtensions, provider.VkDeviceExtensions,
			provider.VkApiVersion, provider.VkQueueIndex);
	}

	/// <summary>The frame texture's <c>VkImage</c>, its exact <c>VkFormat</c>, the shader-read layout it is left in and the head's queue family.</summary>
	/// <param name="target">The frame target.</param>
	/// <param name="reason">Why not, or null.</param>
	/// <returns>The backend texture, or null.</returns>
	private protected override GRBackendTexture? DescribeTexture(WebGpuSkiaTarget target, out string? reason)
	{
		CodebrixVulkanTextureInfo info = default;
		if (Wgpu.CodebrixTextureGetVulkanInfo(target.Texture, &info) != Status.Success)
		{
			reason = "wgpuCodebrixTextureGetVulkanInfo refused the frame texture";
			return null;
		}

		var image = new GRVkImageInfo
		{
			Image = (ulong)(nint)info.Image,
			ImageTiling = VkImageTilingOptimal,
			ImageLayout = VkImageLayoutShaderReadOnlyOptimal,
			Format = info.Format,
			ImageUsageFlags = VulkanUsage(target.Usage),
			SampleCount = Math.Max(1u, info.SampleCount),
			LevelCount = 1,
			CurrentQueueFamily = Provider.VkQueueFamilyIndex,
			SharingMode = VkSharingModeExclusive,
			Protected = false,
		};

		var backend = new GRBackendTexture(target.Width, target.Height, image);
		if (!backend.IsValid)
		{
			backend.Dispose();
			reason = "SkiaSharp rejected the frame texture's Vulkan image description";
			return null;
		}

		reason = null;
		return backend;
	}

	/// <inheritdoc/>
	private protected override void DisposeAdapter()
	{
		_backendContext?.Dispose();
		_backendContext = null;
	}

	/// <summary>Maps WebGPU texture usages to the <c>VkImageUsageFlags</c> the engine creates the image with.</summary>
	/// <param name="usage">The WebGPU usages.</param>
	/// <returns>The Vulkan usage flags.</returns>
	internal static uint VulkanUsage(TextureUsage usage)
	{
		uint flags = 0;
		if ((usage & TextureUsage.CopySrc) != 0)
		{
			flags |= VkImageUsageTransferSrc;
		}

		if ((usage & TextureUsage.CopyDst) != 0)
		{
			flags |= VkImageUsageTransferDst;
		}

		if ((usage & TextureUsage.TextureBinding) != 0)
		{
			flags |= VkImageUsageSampled;
		}

		if ((usage & TextureUsage.StorageBinding) != 0)
		{
			flags |= VkImageUsageStorage;
		}

		if ((usage & TextureUsage.RenderAttachment) != 0)
		{
			flags |= VkImageUsageColorAttachment;
		}

		return flags;
	}

	// vkGetInstanceProcAddr through the head's loader. Called only while adopting (a handful of names).
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
}
