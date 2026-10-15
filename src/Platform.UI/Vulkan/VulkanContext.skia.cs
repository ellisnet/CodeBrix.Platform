#nullable enable
using System;
using System.Runtime.InteropServices;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.Interop;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan.UnmanagedInterop;
using CodeBrix.Platform.UI.Hosting;
using SkiaSharp;

namespace CodeBrix.Platform.UI.Runtime.Skia.Vulkan; //Was previously: Uno.UI.Runtime.Skia.Vulkan

/// <summary>
/// Unified Vulkan rendering context for CodeBrix.Platform.
/// Implements IVulkanPlatformGraphicsContext so it can be passed to all Interop/ classes.
/// Manages the full lifecycle: instance, device, surface, swapchain, and SkiaSharp integration.
/// </summary>
internal sealed class VulkanContext : IVulkanPlatformGraphicsContext, IDisposable
{
	private IVulkanInstance? _instance;
	private VulkanDevice? _device;
	private VulkanDisplay? _display;
	private VulkanImage? _renderImage;
	private GRContext? _grContext;
	private VulkanInstanceApi? _instanceApi;
	private VulkanDeviceApi? _deviceApi;
	private bool _disposed;
	private IVulkanPlatformSurfaceFactory? _factory;
	private IntPtr _nativeWindowHandle;

	// Cached per-size Skia resources — reused across frames, only recreated on resize
	private GRBackendRenderTarget? _cachedRenderTarget;
	private SKSurface? _cachedSkSurface;

	public GRContext? GrContext => _grContext;

	/// <summary>
	/// The handles, extensions and versions an add-in needs to adopt this context's device, captured once when
	/// <see cref="Initialize"/> succeeds (under the device lock, where the queue handle may be read); null before
	/// that and after <see cref="Dispose"/>.
	/// </summary>
	public VulkanNativeDeviceInfo? NativeDeviceInfo { get; private set; }
	public SKSurface? CachedSkSurface => _cachedSkSurface;
	public bool IsInitialized => _grContext != null;

	// IVulkanPlatformGraphicsContext implementation
	public IVulkanDevice Device => _device!;
	public IVulkanInstance Instance => _instance!;
	public VulkanInstanceApi InstanceApi => _instanceApi!;
	public VulkanDeviceApi DeviceApi => _deviceApi!;
	public VkDevice DeviceHandle => new() { Handle = _device!.Handle };
	public VkPhysicalDevice PhysicalDeviceHandle => new() { Handle = _device!.PhysicalDeviceHandle };
	public VkInstance InstanceHandle => new() { Handle = _instance!.Handle };
	public VkQueue MainQueueHandle => new() { Handle = _device!.MainQueueHandle };
	public uint GraphicsQueueFamilyIndex => _device!.GraphicsQueueFamilyIndex;

	/// <summary>
	/// Initialize the Vulkan context with a platform-specific surface factory and native window handle.
	/// </summary>
	public void Initialize(IVulkanPlatformSurfaceFactory factory, IntPtr nativeWindowHandle, int width, int height)
	{
		_factory = factory;
		_nativeWindowHandle = nativeWindowHandle;

		// Create Vulkan instance
		var getProcAddr = factory.GetVkGetInstanceProcAddr();
		_instance = VulkanInstance.Create(getProcAddr, factory.RequiredInstanceExtensions);

		// Create instance API
		_instanceApi = new VulkanInstanceApi(_instance);

		// Create surface so we can check device presentation support
		var surfaceHandle = factory.CreateSurface((VulkanInstance)_instance, nativeWindowHandle);
		var vkSurface = new VkSurfaceKHR { Handle = surfaceHandle };

		// Create device (checks surface presentation support). With direct GPU rendering on, the device also enables
		// what an add-in needs to adopt it (VulkanAdoptionProfile); with it off, creation is unchanged.
		_device = VulkanDevice.Create((VulkanInstance)_instance, _instanceApi, vkSurface, DirectGpuRenderingMode.IsEnabled);

		// Destroy the temporary surface used for device selection
		_instanceApi.DestroySurfaceKHR(new VkInstance { Handle = _instance.Handle }, vkSurface, IntPtr.Zero);

		// Create device API
		_deviceApi = new VulkanDeviceApi(_device);

		// All subsequent operations access device handles and require the device lock
		using (_device.Lock())
		{
			// Create display (swapchain) via platform surface wrapper
			var platformSurface = new DirectVulkanSurface(nativeWindowHandle, new SKSizeI(width, height), factory);
			_display = VulkanDisplay.CreateDisplay(this, platformSurface);

			// Create intermediate render image (TransitionLayout submits commands, needs lock)
			_renderImage = new VulkanImage(this, _display.CommandBufferPool,
				_display.SurfaceFormat.format, new SKSizeI(width, height));

			// Create SkiaSharp Vulkan context
			CreateGrContext();

			CaptureNativeDeviceInfo();
		}
	}

	/// <summary>
	/// Initializes the instance, device and SkiaSharp context exactly as <see cref="Initialize"/> does, but with no
	/// window: no surface, swapchain or intermediate render image (<see cref="RenderFrame"/> and
	/// <see cref="BlitAndPresent"/> do nothing). The context then owns a real device and <see cref="GrContext"/> for
	/// off-screen GPU work - what the test suites use to exercise device adoption (<see cref="NativeDeviceInfo"/>)
	/// on this machine's GPU without opening a window. Like <see cref="Initialize"/>, the device is made adoptable
	/// only while direct GPU rendering is enabled.
	/// </summary>
	/// <param name="getProcAddr">The Vulkan loader's <c>vkGetInstanceProcAddr</c>.</param>
	/// <exception cref="VulkanException">No Vulkan device, or SkiaSharp could not create its context.</exception>
	internal void InitializeHeadless(VkGetInstanceProcAddressDelegate getProcAddr)
	{
		_instance = VulkanInstance.Create(getProcAddr, Array.Empty<string>());
		_instanceApi = new VulkanInstanceApi(_instance);

		// No surface to check presentation support against: the first graphics-capable device is taken, the same
		// selection Initialize makes on a machine whose devices can all present.
		_device = VulkanDevice.Create((VulkanInstance)_instance, _instanceApi, null, DirectGpuRenderingMode.IsEnabled);
		_deviceApi = new VulkanDeviceApi(_device);

		using (_device.Lock())
		{
			CreateGrContext();
			CaptureNativeDeviceInfo();
		}
	}

	// Device lock held (VulkanDevice.MainQueueHandle throws outside it).
	private void CaptureNativeDeviceInfo()
	{
		var instance = _instance!;
		var device = _device!;
		NativeDeviceInfo = new VulkanNativeDeviceInfo(
			instance: instance.Handle,
			physicalDevice: device.PhysicalDeviceHandle,
			device: device.Handle,
			queue: device.MainQueueHandle,
			queueFamilyIndex: device.GraphicsQueueFamilyIndex,
			// VulkanDevice.Create takes queue 0 of the graphics family as the main queue.
			queueIndex: 0,
			apiVersion: instance.ApiVersion,
			instanceExtensions: instance.EnabledExtensions,
			deviceExtensions: device.EnabledExtensions,
			// The loader's own vkGetInstanceProcAddr, resolved through the delegate the instance was created with.
			getInstanceProcAddr: instance.GetInstanceProcAddress(instance.Handle, "vkGetInstanceProcAddr"));
	}

	private void CreateGrContext()
	{
		if (_device == null || _instance == null)
			throw new InvalidOperationException("Vulkan device not initialized");

		IntPtr GetProcAddressWrapper(string name, IntPtr instance, IntPtr device)
		{
			if (device != IntPtr.Zero)
			{
				var addr = _instance.GetDeviceProcAddress(device, name);
				if (addr != IntPtr.Zero)
					return addr;
			}

			if (instance != IntPtr.Zero)
			{
				var addr = _instance.GetInstanceProcAddress(instance, name);
				if (addr != IntPtr.Zero)
					return addr;
			}

			return _instance.GetInstanceProcAddress(IntPtr.Zero, name);
		}

		var ctx = new GRVkBackendContext
		{
			VkInstance = _device.Instance.Handle,
			VkPhysicalDevice = _device.PhysicalDeviceHandle,
			VkDevice = _device.Handle,
			VkQueue = _device.MainQueueHandle,
			GraphicsQueueIndex = _device.GraphicsQueueFamilyIndex,
			GetProcedureAddress = GetProcAddressWrapper,
			// Cap Skia's Vulkan function loading at the version the instance was created with;
			// core functions above VkApplicationInfo.apiVersion resolve to null through
			// vkGet*ProcAddr, which would make GRContext.CreateVulkan fail its validation.
			MaxAPIVersion = _instance.ApiVersion
		};

		_grContext = GRContext.CreateVulkan(ctx)
			?? throw new VulkanException("Unable to create SkiaSharp GRContext from Vulkan device");
	}

	/// <summary>
	/// Full resize: destroys and recreates the display, swapchain, and render image.
	/// Use for major lifecycle events (window recreation, reinitialization).
	/// </summary>
	public void Resize(int width, int height)
	{
		if (_display == null || _device == null || _factory == null)
			return;

		using (_device.Lock())
		{
			_deviceApi!.DeviceWaitIdle(DeviceHandle);

			// Dispose cached Skia resources first (they reference the render image)
			DisposeCachedSkiaSurface();

			_renderImage?.Dispose();
			_display.Dispose();

			var platformSurface = new DirectVulkanSurface(_nativeWindowHandle, new SKSizeI(width, height), _factory);
			_display = VulkanDisplay.CreateDisplay(this, platformSurface);
			_renderImage = new VulkanImage(this, _display.CommandBufferPool,
				_display.SurfaceFormat.format, new SKSizeI(width, height));
			// _cachedSkSurface will be lazily recreated on next EnsureCachedSurface
		}
	}

	/// <summary>
	/// Lightweight resize: only recreates the intermediate render image and cached SKSurface.
	/// The swapchain handles its own resize via VK_ERROR_OUT_OF_DATE_KHR during presentation.
	/// Use for window resize events where only the render target dimensions change.
	/// Must be called while holding the device lock.
	/// </summary>
	public void ResizeRenderImage(int width, int height)
	{
		if (_display == null || _device == null)
			return;

		_deviceApi!.DeviceWaitIdle(DeviceHandle);
		DisposeCachedSkiaSurface();

		_renderImage?.Dispose();
		_renderImage = new VulkanImage(this, _display.CommandBufferPool,
			_display.SurfaceFormat.format, new SKSizeI(width, height));
		// _cachedSkSurface will be lazily recreated on next EnsureCachedSurface
	}

	/// <summary>
	/// Render a complete frame: acquire lock, create SKSurface, invoke the render callback,
	/// flush, blit to swapchain, and present — all within a single device lock scope.
	/// The callback receives the SKSurface to render into.
	/// </summary>
	public bool RenderFrame(Action<SKSurface> renderCallback)
	{
		if (_display == null || _grContext == null || _renderImage == null || _device == null)
			return false;

		using (_device.Lock())
		{
			try
			{
				_display.EnsureSwapchainAvailable();
				_grContext.ResetContext();

				// Lazily create or reuse the cached SKSurface and render target
				EnsureCachedSkiaSurface();

				if (_cachedSkSurface == null)
					return false;

				// Invoke the composition rendering callback
				renderCallback(_cachedSkSurface);

				// Flush Skia commands to the Vulkan intermediate image
				_cachedSkSurface.Canvas.Flush();
				_grContext.Flush();

				// StartPresentation acquires next swapchain image and begins a command buffer
				var commandBuffer = _display.StartPresentation();

				// Blit intermediate render image to the swapchain image
				_display.BlitImageToCurrentImage(commandBuffer, _renderImage);

				// End presentation: transition to present layout, submit, and present
				_display.EndPresentation(commandBuffer);

				return true;
			}
			catch (VulkanException ex) when (
				ex.Message.Contains("OUT_OF_DATE") ||
				ex.Message.Contains("SUBOPTIMAL"))
			{
				return false;
			}
		}
	}

	private void EnsureCachedSkiaSurface()
	{
		if (_cachedSkSurface != null)
			return;

		var imageInfo = _renderImage!.ImageInfo;
		var vkImageInfo = new GRVkImageInfo
		{
			CurrentQueueFamily = _device!.GraphicsQueueFamilyIndex,
			Format = imageInfo.Format,
			Image = (ulong)imageInfo.Handle,
			ImageLayout = imageInfo.Layout,
			ImageTiling = imageInfo.Tiling,
			ImageUsageFlags = imageInfo.UsageFlags,
			LevelCount = imageInfo.LevelCount,
			SampleCount = imageInfo.SampleCount,
			Protected = imageInfo.IsProtected,
			Alloc = new GRVkAlloc
			{
				Memory = (ulong)imageInfo.MemoryHandle,
				Size = imageInfo.MemorySize
			}
		};

		_cachedRenderTarget = new GRBackendRenderTarget(imageInfo.PixelSize.Width, imageInfo.PixelSize.Height, vkImageInfo);
		var colorType = OperatingSystem.IsAndroid() ? SKColorType.Rgba8888 : SKColorType.Bgra8888;
		_cachedSkSurface = SKSurface.Create(_grContext!, _cachedRenderTarget,
			GRSurfaceOrigin.TopLeft, colorType, SKColorSpace.CreateSrgb());
	}

	private void DisposeCachedSkiaSurface()
	{
		if (_cachedSkSurface != null && _grContext != null)
		{
			// Submit any ops still recorded against the wrapped render image while the
			// VkImage is alive. Skia's drawing manager keeps unflushed ops (e.g. a
			// Canvas.Clear recorded just before a resize) referencing the wrapped image;
			// destroying the image first leaves the eventual GrContext.Flush() beginning
			// a render pass on freed driver memory (observed as an ANV segfault when
			// resizing the Wayland head).
			_cachedSkSurface.Canvas.Flush();
			_grContext.Flush();
		}
		if (_device != null)
		{
			// Wait for GPU to finish all pending work before disposing Skia resources
			_deviceApi?.DeviceWaitIdle(DeviceHandle);
		}
		_cachedSkSurface?.Dispose();
		_cachedSkSurface = null;
		_cachedRenderTarget?.Dispose();
		_cachedRenderTarget = null;
	}

	/// <summary>
	/// Invalidate the cached surface reference without disposing it.
	/// Call when the caller has already disposed the SKSurface externally
	/// (e.g., X11Renderer base class disposes _surface before calling UpdateSize).
	/// </summary>
	public void InvalidateCachedSurface()
	{
		_cachedSkSurface = null;
		_cachedRenderTarget?.Dispose();
		_cachedRenderTarget = null;
	}

	/// <summary>
	/// Ensure the cached Skia surface is created. Call while holding the device lock.
	/// Used by platforms (Wayland) that use a split render pattern instead of <see cref="RenderFrame"/>.
	/// </summary>
	public void EnsureCachedSurface()
	{
		EnsureCachedSkiaSurface();
	}

	/// <summary>
	/// Blit the intermediate render image to the swapchain and present.
	/// Call after Skia canvas/context flush, while holding the device lock.
	/// Used by platforms (Wayland) that use a split render pattern instead of <see cref="RenderFrame"/>.
	/// </summary>
	public void BlitAndPresent()
	{
		if (_display == null || _renderImage == null || _deviceApi == null)
			return;

		// Try to acquire next swapchain image without retrying.
		var acquireResult = _display.TryAcquireNextImage();
		if (acquireResult != 0) // VK_SUCCESS = 0
		{
			// Swapchain out of date — recreate it now.
			// This is safe because we're inside the device lock and not
			// mid-presentation. The previous segfaults were caused by
			// StartPresentation's retry loop calling RecreateSwapchain
			// while already holding partial presentation state.
			try
			{
				_deviceApi.DeviceWaitIdle(DeviceHandle);
				_display.RecreateSwapchainSafe();

				// Retry acquire after recreation
				acquireResult = _display.TryAcquireNextImage();
				if (acquireResult != 0)
					return; // Still failing — skip this frame
			}
			catch
			{
				return; // Recreation failed — skip this frame
			}
		}

		try
		{
			var commandBuffer = _display.CommandBufferPool.CreateCommandBuffer();
			commandBuffer.BeginRecording();
			_display.PrepareCurrentImageForBlit(commandBuffer);
			_display.BlitImageToCurrentImage(commandBuffer, _renderImage);
			_display.EndPresentation(commandBuffer);
		}
		catch (VulkanException)
		{
			// Presentation failed — skip this frame
		}
	}

	/// <summary>
	/// Get information about the initialized Vulkan device for diagnostic logging.
	/// </summary>
	public unsafe (string DeviceName, string DriverVersion) GetDeviceInfo()
	{
		if (_device == null || _instanceApi == null)
			return ("Unknown", "Unknown");

		_instanceApi.GetPhysicalDeviceProperties(PhysicalDeviceHandle, out var properties);

		var deviceName = Marshal.PtrToStringAnsi(new IntPtr(properties.deviceName)) ?? "Unknown";
		var major = (properties.driverVersion >> 22) & 0x3FF;
		var minor = (properties.driverVersion >> 12) & 0x3FF;
		var patch = properties.driverVersion & 0xFFF;
		var driverVersion = $"{major}.{minor}.{patch}";

		return (deviceName, driverVersion);
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		NativeDeviceInfo = null;

		if (_device != null)
		{
			using (_device.Lock())
			{
				_deviceApi?.DeviceWaitIdle(DeviceHandle);
				DisposeCachedSkiaSurface();
			}
		}

		_grContext?.AbandonContext();
		_grContext?.Dispose();
		_grContext = null;

		_renderImage?.Dispose();
		_renderImage = null;

		_display?.Dispose();
		_display = null;

		_device?.Dispose();
		_device = null;

		(_instance as IDisposable)?.Dispose();
		_instance = null;
	}

	/// <summary>
	/// Simple IVulkanKhrSurfacePlatformSurface implementation that wraps a native window handle directly.
	/// The surface was already created during Initialize — this provides the CreateSurface callback for
	/// VulkanDisplay/VulkanKhrSurface to use.
	/// </summary>
	private sealed class DirectVulkanSurface : IVulkanKhrSurfacePlatformSurface
	{
		private readonly IntPtr _nativeWindowHandle;
		private readonly IVulkanPlatformSurfaceFactory _factory;

		public DirectVulkanSurface(IntPtr nativeWindowHandle, SKSizeI size, IVulkanPlatformSurfaceFactory factory)
		{
			_nativeWindowHandle = nativeWindowHandle;
			_factory = factory;
			Size = size;
		}

		public SKSizeI Size { get; }

		public ulong CreateSurface(IVulkanPlatformGraphicsContext context)
		{
			// Create a new VkSurfaceKHR from the native window handle
			return _factory.CreateSurface((VulkanInstance)context.Instance, _nativeWindowHandle);
		}

		public void Dispose() { }
	}
}

/// <summary>
/// An immutable snapshot of a <see cref="VulkanContext"/>'s device for adoption by an add-in (see
/// <see cref="VulkanNativeGpuDeviceProvider"/>). Handles are raw Vulkan handles; nothing here is owned by the snapshot.
/// </summary>
internal sealed class VulkanNativeDeviceInfo
{
	public VulkanNativeDeviceInfo(IntPtr instance, IntPtr physicalDevice, IntPtr device, IntPtr queue,
		uint queueFamilyIndex, uint queueIndex, uint apiVersion,
		System.Collections.Generic.IReadOnlyList<string> instanceExtensions,
		System.Collections.Generic.IReadOnlyList<string> deviceExtensions, IntPtr getInstanceProcAddr)
	{
		Instance = instance;
		PhysicalDevice = physicalDevice;
		Device = device;
		Queue = queue;
		QueueFamilyIndex = queueFamilyIndex;
		QueueIndex = queueIndex;
		ApiVersion = apiVersion;
		// Copied so a later change to the context's own lists can never show through.
		InstanceExtensions = System.Linq.Enumerable.ToArray(instanceExtensions);
		DeviceExtensions = System.Linq.Enumerable.ToArray(deviceExtensions);
		GetInstanceProcAddr = getInstanceProcAddr;
	}

	public IntPtr Instance { get; }
	public IntPtr PhysicalDevice { get; }
	public IntPtr Device { get; }
	public IntPtr Queue { get; }
	public uint QueueFamilyIndex { get; }
	public uint QueueIndex { get; }
	public uint ApiVersion { get; }
	public System.Collections.Generic.IReadOnlyList<string> InstanceExtensions { get; }
	public System.Collections.Generic.IReadOnlyList<string> DeviceExtensions { get; }
	public IntPtr GetInstanceProcAddr { get; }
}

// Extension to convert IntPtr to Vulkan handle structs for API calls
internal static class VulkanHandleExtensions
{
	public static VkDevice ToVkDevice(this IntPtr handle) => new VkDevice { Handle = handle };
	public static VkInstance ToVkInstance(this IntPtr handle) => new VkInstance { Handle = handle };
	public static VkPhysicalDevice ToVkPhysicalDevice(this IntPtr handle) => new VkPhysicalDevice { Handle = handle };
	public static VkQueue ToVkQueue(this IntPtr handle) => new VkQueue { Handle = handle };
}
