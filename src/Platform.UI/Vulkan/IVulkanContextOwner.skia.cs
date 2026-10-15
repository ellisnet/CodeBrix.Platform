#nullable enable

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan;

/// <summary>
/// Something that owns (or will own) a head window's <see cref="Vulkan.VulkanContext"/>: a head's Vulkan renderer,
/// or a head window whose renderer is created lazily. <see cref="VulkanNativeGpuDeviceProvider"/> reads it on every
/// call, so the provider follows the context's lifetime.
/// </summary>
internal interface IVulkanContextOwner
{
	/// <summary>The live context, or <see langword="null"/> when there is none (not created yet, or disposed).</summary>
	VulkanContext? VulkanContext { get; }

	/// <summary>
	/// True once the owner has disposed its context (its window closed) - <see cref="VulkanContext"/> is then null, and
	/// this is what tells "disposed" from "not created yet".
	/// </summary>
	bool HasDisposedContext => false;
}
