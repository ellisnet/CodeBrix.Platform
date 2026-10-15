using System;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using SkiaSharp;
using Xunit;

namespace CodeBrix.Platform.UI.WebGpu.Tests;

/// <summary>
/// A head's Vulkan device without a window: the X11 and Wayland heads' own <see cref="VulkanContext"/> (the same
/// instance, device and SkiaSharp GRContext creation code), initialized headless, behind the very
/// <see cref="INativeGpuDeviceProvider"/> implementation those heads register. The direct-path GPU tests adopt it as
/// the view would adopt a window's device. Skips the calling test with the reason where this machine has no Vulkan
/// device (or no Vulkan loader).
/// </summary>
internal sealed class HeadlessVulkanHead : IVulkanContextOwner, IDisposable
{
	private VulkanContext? _context;

	private HeadlessVulkanHead(VulkanContext context)
	{
		_context = context;
		Provider = VulkanNativeGpuDeviceProvider.For(this)!;
	}

	/// <summary>The provider the heads register for a Vulkan window, over this context.</summary>
	internal INativeGpuDeviceProvider Provider { get; }

	/// <summary>The head's GPU context.</summary>
	internal GRContext GrContext => _context!.GrContext!;

	/// <inheritdoc/>
	VulkanContext? IVulkanContextOwner.VulkanContext => _context;

	/// <summary>Creates the head's device, or skips the calling test with the reason.</summary>
	/// <returns>The head.</returns>
	internal static HeadlessVulkanHead CreateOrSkip()
	{
		string? reason = null;
		VulkanContext? context = null;
		if (!OperatingSystem.IsLinux())
		{
			reason = "the headless Vulkan head is created through libvulkan.so.1 (Linux only)";
		}
		else
		{
			context = new VulkanContext();
			try
			{
				context.InitializeHeadless(GetInstanceProcAddr);
			}
			catch (Exception e) when (e is VulkanException or DllNotFoundException or EntryPointNotFoundException or InvalidOperationException)
			{
				reason = e.Message;
				context.Dispose();
				context = null;
			}
		}

		Assert.SkipWhen(context is null, "No Vulkan device for a head on this machine: " + reason);
		return new HeadlessVulkanHead(context!);
	}

	/// <summary>Disposes the head's context (the provider becomes unavailable, as when a window closes).</summary>
	public void Dispose()
	{
		_context?.Dispose();
		_context = null;
	}

	/// <summary>Draws <paramref name="image"/> into a new GPU surface on the head's context and reads it back.</summary>
	/// <param name="image">A GPU image on the head's context.</param>
	/// <returns>The pixels (unpremultiplied BGRA).</returns>
	internal SKBitmap ReadThroughHeadSurface(SKImage image) => ReadThroughHeadSurface(canvas => canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest), null), image.Width, image.Height);

	/// <summary>
	/// What the head does with a window frame, in miniature: <paramref name="paint"/> is RECORDED into a picture (the
	/// user-interface thread's part), the picture is played back onto a GPU surface on the head's context (the render
	/// thread's part, inside the provider's frame scope) and the result is read back.
	/// </summary>
	/// <param name="paint">The paint callback.</param>
	/// <param name="width">The surface width.</param>
	/// <param name="height">The surface height.</param>
	/// <returns>The pixels (unpremultiplied BGRA).</returns>
	internal SKBitmap ReadThroughHeadSurface(Action<SKCanvas> paint, int width, int height)
	{
		using var recorder = new SKPictureRecorder();
		var recording = recorder.BeginRecording(new SKRect(0, 0, width, height));
		paint(recording);
		using var picture = recorder.EndRecording();

		using (Provider.BeginFrame())
		{
			var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			using var surface = SKSurface.Create(GrContext, false, info);
			Assert.NotNull(surface);
			surface.Canvas.Clear(SKColors.Transparent);
			surface.Canvas.DrawPicture(picture);
			surface.Canvas.Flush();
			var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
			Assert.True(surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0));
			return bitmap;
		}
	}

	[DllImport("libvulkan.so.1", EntryPoint = "vkGetInstanceProcAddr")]
	private static extern IntPtr NativeGetInstanceProcAddr(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)] string name);

	private static IntPtr GetInstanceProcAddr(IntPtr instance, string name) => NativeGetInstanceProcAddr(instance, name);
}
