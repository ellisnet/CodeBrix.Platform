using System;
using SkiaSharp;
using CodeBrix.Platform.Foundation.Logging;
using CodeBrix.Platform.UI.Hosting;
using CodeBrix.Platform.UI.Runtime.Skia.Vulkan;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Protocols.Wayland;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland.Vulkan;
using Microsoft.UI.Xaml.Media;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// GPU renderer (the platform default): Vulkan presents directly to the window's wl_surface
/// through the driver's VK_KHR_wayland_surface WSI, and Skia renders through the shared
/// <see cref="VulkanContext"/> — the same shape as the X11 head's Vulkan path. Used by default,
/// or selected explicitly via
/// <c>WaylandHostBuilder.RenderingBackend(WaylandRenderingBackend.Vulkan)</c> or
/// <c>FeatureConfiguration.Rendering.UseVulkanOnWayland</c>; falls back to the software
/// <see cref="WaylandShmRenderer"/> (never to OpenGL ES) when Vulkan is unavailable.
/// </summary>
/// <remarks>
/// Unlike X11, a Wayland surface has no inherent size: the surface capabilities report an
/// undefined currentExtent, and a window resize never surfaces as VK_ERROR_OUT_OF_DATE_KHR.
/// The swapchain must therefore be recreated proactively (<see cref="VulkanContext.Resize"/>)
/// whenever the composition size changes — resizing only the intermediate render image (the
/// X11 approach) would leave the presented buffer at the old size. vkQueuePresentKHR
/// attaches+commits the buffer to the wl_surface through the driver's own private event
/// queue, so this path does not use wl_shm buffers or the wl_callback frame dance, and it is
/// safe alongside the application's own event pump.
/// </remarks>
internal sealed class WaylandVulkanRenderer : IWaylandRenderer
{
	private readonly object _gate = new();
	private readonly IXamlRootHost _host;
	private readonly WaylandXamlRootHost? _waylandHost;
	private readonly WaylandConnection _connection;
	private readonly WlSurface _wlSurface;
	private readonly VulkanContext _vulkanContext;
	private int _lastViewportWidth = -1;
	private int _lastViewportHeight = -1;

	private SKColor _background = SKColors.White;
	// _width/_height are the BUFFER (physical pixel) dimensions, like the shm/EGL renderers.
	private int _width;
	private int _height;
	private int _bufferScale = 1;
	private bool _sized;
	private bool _disposed;
	private int _renderCount;

	private WaylandVulkanRenderer(IXamlRootHost host, WaylandConnection connection, WlSurface wlSurface,
		VulkanContext vulkanContext, int width, int height)
	{
		_host = host;
		_waylandHost = host as WaylandXamlRootHost;
		_connection = connection;
		_wlSurface = wlSurface;
		_vulkanContext = vulkanContext;
		_width = width;
		_height = height;
	}

	public static WaylandVulkanRenderer Create(IXamlRootHost host, WaylandConnection connection, WlSurface wlSurface)
	{
		if (!WaylandVulkanSurfaceFactory.IsVulkanAvailable())
		{
			throw new InvalidOperationException("Vulkan rendering not available: libvulkan.so.1 not found");
		}

		// Initialize eagerly (at the current window size, or 1x1 before the first configure) so
		// that a missing driver, extension, or presentation support fails HERE — where the host
		// can still fall back to software — instead of inside the first frame. The first
		// composition frame corrects the size via the resize callback if the estimate is stale.
		var waylandHost = host as WaylandXamlRootHost;
		var logical = waylandHost?.CurrentSize ?? default;
		var scale = waylandHost?.EffectiveScale ?? connection.PrimaryOutput.Scale;
		var width = Math.Max(1, (int)Math.Round(logical.Width * scale));
		var height = Math.Max(1, (int)Math.Round(logical.Height * scale));

		var factory = new WaylandVulkanSurfaceFactory(connection.Display.Handle);
		var context = new VulkanContext();
		try
		{
			context.Initialize(factory, wlSurface.Handle, width, height);
		}
		catch
		{
			context.Dispose();
			throw;
		}

		var (deviceName, driverVersion) = context.GetDeviceInfo();
		typeof(WaylandVulkanRenderer).Log().Info($"Wayland Vulkan rendering initialized: {deviceName}, {driverVersion}");

		return new WaylandVulkanRenderer(host, connection, wlSurface, context, width, height);
	}

	public void SetBackgroundColor(SKColor color) => _background = color;

	public void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		if (_host is WaylandXamlRootHost { IsClosed: true })
		{
			return;
		}

		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}

			// Vulkan has no thread-affine context (unlike EGL), but all device access must hold
			// the device lock; it is reentrant, so the resize path below can nest safely.
			IDisposable? deviceLock = null;
			try
			{
				if (_sized)
				{
					deviceLock = _vulkanContext.Device.Lock();
					_vulkanContext.GrContext?.ResetContext();
					_vulkanContext.EnsureCachedSurface();
				}

				var canvas = _sized ? _vulkanContext.CachedSkSurface?.Canvas : null;
				canvas?.Clear(_background);
				_ = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(canvas, size =>
				{
					Resize((int)size.Width, (int)size.Height);
					deviceLock ??= _vulkanContext.Device.Lock();
					_vulkanContext.GrContext?.ResetContext();
					_vulkanContext.EnsureCachedSurface();
					var c = _vulkanContext.CachedSkSurface!.Canvas;
					c.Clear(_background);
					return c;
				});

				if (!_sized || _vulkanContext.CachedSkSurface is not { } surface)
				{
					return; // never sized — nothing to present
				}

				surface.Canvas.Flush();
				_vulkanContext.GrContext?.Flush();

				// Same buffer->logical mapping rules as the shm/EGL renderers: viewport
				// destination (buffer scale 1) when wp_viewporter is available — exact for
				// fractional scales — else the window's integer buffer scale, re-read on resize.
				// These requests enter the connection's outgoing buffer before the present
				// below, so the compositor applies them with the driver's commit.
				_wlSurface.SetBufferScale(_bufferScale);
				if (_waylandHost is { Viewport: { } viewport })
				{
					var logical = _waylandHost.CurrentSize;
					if (logical.Width > 0 && logical.Height > 0
						&& (logical.Width != _lastViewportWidth || logical.Height != _lastViewportHeight))
					{
						viewport.SetDestination(logical.Width, logical.Height);
						_lastViewportWidth = logical.Width;
						_lastViewportHeight = logical.Height;
					}
				}

				// Blit the intermediate render image to the swapchain and present;
				// vkQueuePresentKHR attaches+commits the buffer to the wl_surface.
				_vulkanContext.BlitAndPresent();
			}
			finally
			{
				deviceLock?.Dispose();
			}
		}

		_connection.Flush();
	}

	// Must hold _gate.
	private void Resize(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			return;
		}

		_bufferScale = _waylandHost is { Viewport: not null }
			? 1
			: Math.Max(1, (int)Math.Round(_waylandHost?.EffectiveScale ?? _connection.PrimaryOutput.Scale));

		if (width == _width && height == _height)
		{
			_sized = true; // eager init already created the swapchain at this size
			return;
		}

		_width = width;
		_height = height;
		_vulkanContext.Resize(width, height);
		_sized = true;
	}

	public void Dispose()
	{
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;

			_vulkanContext.Dispose();
		}
	}
}
