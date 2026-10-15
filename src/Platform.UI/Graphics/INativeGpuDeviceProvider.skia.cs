#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace CodeBrix.Platform.Graphics;

/// <summary>
/// A head's native GPU device, exposed so that an add-in can ADOPT it - create its own GPU objects on the very
/// device, queue and Skia <see cref="SkiaSharp.GRContext"/> the head renders the window with - and composite GPU
/// content without reading frames back to the processor. Resolved through
/// <c>CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility.CreateInstance</c> with the element's
/// <c>XamlRoot</c> as owner, exactly like the heads' other per-window extensions.
/// </summary>
/// <remarks>
/// <para>
/// A head registers a provider only for a window whose renderer has a device of its own: the X11 head on its
/// Vulkan renderer, the Wayland head on its Vulkan renderer (its default), and the macOS head in Metal mode. On
/// every other head and renderer (OpenGL, OpenGL ES, software, Win32, WPF, the frame buffer, PlayTest) resolution
/// returns no provider at all, and the add-in uses its own device with a read-back path.
/// </para>
/// <para>
/// A resolved provider is live: it reads the window's renderer on every call. <see cref="IsAvailable"/> turns
/// false when the window's renderer is gone (the window closed), and on the Wayland head - whose renderer is
/// created lazily on the first frame - it may be false until that first frame, with <see cref="Reason"/> saying
/// so. The device belongs to the head: an adopter never destroys, releases or waits idle on these handles.
/// </para>
/// <para>
/// Handles are native pointers (<see langword="nint"/>): <c>VkInstance</c>, <c>VkPhysicalDevice</c>,
/// <c>VkDevice</c>, <c>VkQueue</c> for <see cref="NativeGpuDeviceKind.Vulkan"/>; <c>id&lt;MTLDevice&gt;</c>,
/// <c>id&lt;MTLCommandQueue&gt;</c> for <see cref="NativeGpuDeviceKind.Metal"/>. Members that do not apply to
/// <see cref="Kind"/>, and every handle while <see cref="IsAvailable"/> is false, are zero (lists empty).
/// </para>
/// </remarks>
public interface INativeGpuDeviceProvider
{
	/// <summary>The graphics API of the head's device.</summary>
	NativeGpuDeviceKind Kind { get; }

	/// <summary>
	/// True while the head's device exists and can be adopted; false before the window's renderer has been
	/// created and after it has been disposed.
	/// </summary>
	bool IsAvailable { get; }

	/// <summary>Why <see cref="IsAvailable"/> is false, or <see langword="null"/> when it is true.</summary>
	string? Reason { get; }

	/// <summary>Vulkan: the <c>VkInstance</c> the head created.</summary>
	nint VkInstance { get; }

	/// <summary>Vulkan: the <c>VkPhysicalDevice</c> the head's device was created on.</summary>
	nint VkPhysicalDevice { get; }

	/// <summary>Vulkan: the head's <c>VkDevice</c>.</summary>
	nint VkDevice { get; }

	/// <summary>
	/// Vulkan: the <c>VkQueue</c> the head renders and presents on. Vulkan requires external synchronization of
	/// queue access: submit to it only inside a <see cref="BeginFrame"/> scope.
	/// </summary>
	nint VkQueue { get; }

	/// <summary>Vulkan: the queue family index of <see cref="VkQueue"/> (a graphics + present family).</summary>
	uint VkQueueFamilyIndex { get; }

	/// <summary>Vulkan: the index of <see cref="VkQueue"/> within its family.</summary>
	uint VkQueueIndex { get; }

	/// <summary>
	/// Vulkan: the <c>apiVersion</c> the instance was created with (<c>VK_MAKE_API_VERSION</c> encoding). Core
	/// entry points above this version cannot be resolved through <see cref="VkGetInstanceProcAddr"/>.
	/// </summary>
	uint VkApiVersion { get; }

	/// <summary>Vulkan: the instance extensions the head enabled.</summary>
	IReadOnlyList<string> VkInstanceExtensions { get; }

	/// <summary>Vulkan: the device extensions the head enabled.</summary>
	IReadOnlyList<string> VkDeviceExtensions { get; }

	/// <summary>
	/// Vulkan: the address of the <c>vkGetInstanceProcAddr</c> the head loads every Vulkan entry point through
	/// (the system Vulkan loader's), so an adopter resolves functions from the same loader.
	/// </summary>
	nint VkGetInstanceProcAddr { get; }

	/// <summary>Metal: the <c>id&lt;MTLDevice&gt;</c> the window renders with.</summary>
	nint MetalDevice { get; }

	/// <summary>
	/// Metal: the <c>id&lt;MTLCommandQueue&gt;</c> the head's <see cref="GRContext"/> submits on.
	/// </summary>
	nint MetalCommandQueue { get; }

	/// <summary>
	/// The head's own Skia GPU context for this window (Vulkan or Metal backed), or <see langword="null"/> when
	/// <see cref="IsAvailable"/> is false. Use it - inside a <see cref="BeginFrame"/> scope, on the head's
	/// render thread - to wrap GPU-resident images (for example a <c>GRBackendTexture</c> over a texture made on
	/// the adopted device) that the window then draws. Never dispose or abandon it.
	/// </summary>
	GRContext? GRContext { get; }

	/// <summary>
	/// Returns a scope in which the head's device, queue and <see cref="GRContext"/> may be used from the
	/// calling thread. On Vulkan it holds the head's device lock (re-entrant: the head's own render pass already
	/// holds it, so inside a paint callback it costs nothing, and from any other thread it waits for the
	/// current frame); keep the scope short. On Metal, which has no thread-current state, it is a no-op scope.
	/// Dispose it to release.
	/// </summary>
	/// <returns>The scope; an empty scope when <see cref="IsAvailable"/> is false.</returns>
	IDisposable BeginFrame();
}
