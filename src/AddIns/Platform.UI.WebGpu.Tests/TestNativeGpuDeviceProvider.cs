using System;
using System.Collections.Generic;
using CodeBrix.Platform.Graphics;
using SkiaSharp;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// A provider that forwards to a real one (the headless head's) with knobs the failure tests turn: report itself
/// unavailable (a Wayland window before its first frame, a closed window), hide the GPU context, hide the loader
/// entry point, or claim another kind.
/// </summary>
internal sealed class TestNativeGpuDeviceProvider(INativeGpuDeviceProvider? inner) : INativeGpuDeviceProvider
{
	/// <summary>False reports the provider unavailable (handles zero, as the real one does).</summary>
	internal bool Available { get; set; } = true;

	/// <summary>True hides the GPU context (adoption then has nothing to adopt with).</summary>
	internal bool HideGrContext { get; set; }

	/// <summary>True hides vkGetInstanceProcAddr (the library refuses the adoption).</summary>
	internal bool HideProcAddr { get; set; }

	/// <summary>The kind reported (default: the inner provider's, or Vulkan).</summary>
	internal NativeGpuDeviceKind? KindOverride { get; set; }

	private bool Live => Available && inner is { IsAvailable: true };

	public NativeGpuDeviceKind Kind => KindOverride ?? inner?.Kind ?? NativeGpuDeviceKind.Vulkan;

	public bool IsAvailable => Live;

	public string? Reason => Live ? null : "The window's Vulkan renderer has not been created yet (test).";

	public nint VkInstance => Live ? inner!.VkInstance : 0;

	public nint VkPhysicalDevice => Live ? inner!.VkPhysicalDevice : 0;

	public nint VkDevice => Live ? inner!.VkDevice : 0;

	public nint VkQueue => Live ? inner!.VkQueue : 0;

	public uint VkQueueFamilyIndex => Live ? inner!.VkQueueFamilyIndex : 0;

	public uint VkQueueIndex => Live ? inner!.VkQueueIndex : 0;

	public uint VkApiVersion => Live ? inner!.VkApiVersion : 0;

	public IReadOnlyList<string> VkInstanceExtensions => Live ? inner!.VkInstanceExtensions : Array.Empty<string>();

	public IReadOnlyList<string> VkDeviceExtensions => Live ? inner!.VkDeviceExtensions : Array.Empty<string>();

	public nint VkGetInstanceProcAddr => Live && !HideProcAddr ? inner!.VkGetInstanceProcAddr : 0;

	public nint MetalDevice => 0;

	public nint MetalCommandQueue => 0;

	public GRContext? GRContext => Live && !HideGrContext ? inner!.GRContext : null;

	public IDisposable BeginFrame() => inner?.BeginFrame() ?? new Nothing();

	private sealed class Nothing : IDisposable
	{
		public void Dispose()
		{
		}
	}
}
