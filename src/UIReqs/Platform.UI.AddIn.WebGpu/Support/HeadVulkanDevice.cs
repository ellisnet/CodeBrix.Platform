using System;
using System.Reflection;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Foundation.Extensibility;
using CodeBrix.Platform.Graphics;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using Microsoft.UI.Xaml;

namespace CodeBrix.Platform.UI.AddIn.WebGpu.UIReqs.Support;

/// <summary>
/// The direct-path scenarios' stand-in for a Vulkan head window. The UIReqs harness runs every scenario on the
/// Emulated frame-buffer head, which renders in software and offers no GPU device; these scenarios therefore register,
/// as the window's <see cref="INativeGpuDeviceProvider"/>, the X11 and Wayland heads' own Vulkan device code
/// (<see cref="VulkanContext"/>, initialized headless, adoptable because the scenario turned the direct GPU rendering
/// latch on first) behind the very provider class those heads register. The view then really adopts a head device and
/// runs DirectVulkan; its picture is checked through the view's own capture (the head-side GPU image, sampled on that
/// device), because a software head cannot composite a GPU image.
/// </summary>
internal static class HeadVulkanDevice
{
	private static readonly object Gate = new();
	private static readonly FieldInfo Latch =
		typeof(DirectGpuRenderingMode).GetField("_isEnabled", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException("DirectGpuRenderingMode no longer has a private static _isEnabled latch.");

	private static bool _registered;
	private static Owner? _owner;
	private static volatile INativeGpuDeviceProvider? _offered;

	/// <summary>Turns the application-wide latch on or off (a ONE-WAY latch in applications; scenarios set it by reflection).</summary>
	/// <param name="enabled">The value.</param>
	internal static void SetLatch(bool enabled) => Latch.SetValue(null, enabled);

	/// <summary>
	/// Registers (once per process) the window-provider builder: it answers the provider a scenario offered, and no
	/// provider otherwise - exactly what a head without a GPU device answers.
	/// </summary>
	internal static void EnsureRegistered()
	{
		lock (Gate)
		{
			if (_registered || ApiExtensibility.IsRegistered<INativeGpuDeviceProvider>())
			{
				_registered = true;
				return;
			}

			ApiExtensibility.Register<XamlRoot>(typeof(INativeGpuDeviceProvider), _ => _offered!);
			_registered = true;
		}
	}

	/// <summary>
	/// Offers the head's Vulkan device to every window from now on, creating it the first time (with the latch on, so
	/// it is created adoptable). The device lives for the rest of the run: a picture the frame-buffer head recorded may
	/// still hold a frame of it after the scenario ends.
	/// </summary>
	/// <param name="reason">Why there is no device, or null.</param>
	/// <returns>True when a device is offered.</returns>
	internal static bool TryOffer(out string? reason)
	{
		lock (Gate)
		{
			if (_owner is null)
			{
				var context = new VulkanContext();
				try
				{
					context.InitializeHeadless(GetInstanceProcAddr);
				}
				catch (Exception e) when (e is VulkanException or DllNotFoundException or EntryPointNotFoundException or InvalidOperationException)
				{
					context.Dispose();
					reason = e.Message;
					return false;
				}

				_owner = new Owner(context);
			}

			_offered = VulkanNativeGpuDeviceProvider.For(_owner);
			reason = null;
			return true;
		}
	}

	/// <summary>Stops offering a device (the next window resolution answers "no provider").</summary>
	internal static void Withdraw() => _offered = null;

	[DllImport("libvulkan.so.1", EntryPoint = "vkGetInstanceProcAddr")]
	private static extern IntPtr NativeGetInstanceProcAddr(IntPtr instance, [MarshalAs(UnmanagedType.LPStr)] string name);

	private static IntPtr GetInstanceProcAddr(IntPtr instance, string name) => NativeGetInstanceProcAddr(instance, name);

	private sealed class Owner(VulkanContext context) : IVulkanContextOwner
	{
		public VulkanContext? VulkanContext => context;
	}
}
