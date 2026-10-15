#nullable enable

using System;
using System.Collections.Generic;
using CodeBrix.Platform.Extensions.Disposables;
using CodeBrix.Platform.Graphics;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan;

/// <summary>
/// The <see cref="INativeGpuDeviceProvider"/> of a head window that renders through the shared
/// <see cref="VulkanContext"/> (the X11 and Wayland Vulkan renderers). Every member reads the owner's live context,
/// so the provider becomes unavailable when the renderer is disposed and - on a head that creates its renderer
/// lazily - available once it exists.
/// </summary>
internal sealed class VulkanNativeGpuDeviceProvider : INativeGpuDeviceProvider
{
	internal const string NotCreatedReason = "The window's Vulkan renderer has not been created yet (it is created for the first frame).";
	internal const string DisposedReason = "The window's Vulkan renderer has been disposed (the window was closed).";

	private readonly IVulkanContextOwner _owner;

	// Constructing must not throw (ApiExtensibility builders run while an element resolves its extensions).
	internal VulkanNativeGpuDeviceProvider(IVulkanContextOwner owner) => _owner = owner;

	/// <summary>
	/// The provider for <paramref name="candidate"/> when it owns (or will own) a Vulkan context, else null: what a
	/// head's ApiExtensibility builder returns for a window, null meaning "no provider for this window".
	/// </summary>
	/// <param name="candidate">The window's renderer, or the window itself when its renderer is created lazily.</param>
	/// <returns>The provider, or null.</returns>
	internal static INativeGpuDeviceProvider? For(object? candidate)
		=> candidate is IVulkanContextOwner owner ? new VulkanNativeGpuDeviceProvider(owner) : null;

	private VulkanContext? Live => _owner.VulkanContext is { IsInitialized: true } context ? context : null;

	private VulkanNativeDeviceInfo? Info => Live?.NativeDeviceInfo;

	/// <inheritdoc />
	public NativeGpuDeviceKind Kind => NativeGpuDeviceKind.Vulkan;

	/// <inheritdoc />
	public bool IsAvailable => Info is not null;

	/// <inheritdoc />
	public string? Reason => IsAvailable
		? null
		: _owner.VulkanContext is null && !_owner.HasDisposedContext ? NotCreatedReason : DisposedReason;

	/// <inheritdoc />
	public nint VkInstance => Info?.Instance ?? 0;

	/// <inheritdoc />
	public nint VkPhysicalDevice => Info?.PhysicalDevice ?? 0;

	/// <inheritdoc />
	public nint VkDevice => Info?.Device ?? 0;

	/// <inheritdoc />
	public nint VkQueue => Info?.Queue ?? 0;

	/// <inheritdoc />
	public uint VkQueueFamilyIndex => Info?.QueueFamilyIndex ?? 0;

	/// <inheritdoc />
	public uint VkQueueIndex => Info?.QueueIndex ?? 0;

	/// <inheritdoc />
	public uint VkApiVersion => Info?.ApiVersion ?? 0;

	/// <inheritdoc />
	public IReadOnlyList<string> VkInstanceExtensions => Info?.InstanceExtensions ?? Array.Empty<string>();

	/// <inheritdoc />
	public IReadOnlyList<string> VkDeviceExtensions => Info?.DeviceExtensions ?? Array.Empty<string>();

	/// <inheritdoc />
	public nint VkGetInstanceProcAddr => Info?.GetInstanceProcAddr ?? 0;

	/// <inheritdoc />
	public nint MetalDevice => 0;

	/// <inheritdoc />
	public nint MetalCommandQueue => 0;

	/// <inheritdoc />
	public GRContext? GRContext => Live?.GrContext;

	/// <inheritdoc />
	// The head's renderers take the same (re-entrant) device lock around each frame, so holding it makes queue
	// submissions and GRContext use from the caller's thread externally synchronized with the head's own.
	public IDisposable BeginFrame() => Live is { } context ? context.Device.Lock() : Disposable.Empty;
}
