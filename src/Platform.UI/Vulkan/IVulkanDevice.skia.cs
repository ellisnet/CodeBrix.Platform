// Based on the Avalonia project (MIT License, Copyright (c) AvaloniaUI OÜ).
// Original source: https://github.com/AvaloniaUI/Avalonia/tree/master/src/Avalonia.Vulkan
using System;
using System.Collections.Generic;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.UnmanagedInterop;

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan; //Was previously: Uno.UI.Runtime.Skia.Vulkan

internal interface IVulkanInstance
{
	IntPtr Handle { get; }
	IntPtr GetInstanceProcAddress(IntPtr instance, string name);
	IntPtr GetDeviceProcAddress(IntPtr device, string name);
	IReadOnlyList<string> EnabledExtensions { get; }

	/// <summary>
	/// The Vulkan version declared in VkApplicationInfo.apiVersion when the instance was created.
	/// Core functions above this version cannot be resolved through vkGet*ProcAddr.
	/// </summary>
	uint ApiVersion { get; }
}

internal interface IVulkanDevice
{
	IntPtr Handle { get; }
	IntPtr PhysicalDeviceHandle { get; }
	IntPtr MainQueueHandle { get; }
	uint GraphicsQueueFamilyIndex { get; }
	IVulkanInstance Instance { get; }
	IDisposable Lock();
}
