using System;
using System.IO;
using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

public partial class WaylandHostBuilder : IPlatformHostBuilder
{
	private int _renderFrameRate = 60;
	private WaylandRenderingBackend? _renderingBackend;

	internal WaylandHostBuilder()
	{
	}

	/// <summary>
	/// Sets the rendering backend for the Wayland host.
	/// This takes precedence over <see cref="FeatureConfiguration.Rendering.UseVulkanOnWayland"/>,
	/// <see cref="FeatureConfiguration.Rendering.UseOpenGLOnWayland"/> and the
	/// CODEBRIX_WAYLAND_NO_GPU / CODEBRIX_WAYLAND_USE_EGL environment variables if set.
	/// </summary>
	public WaylandHostBuilder RenderingBackend(WaylandRenderingBackend backend)
	{
		_renderingBackend = backend;
		return this;
	}

	/// <summary>
	/// Sets the FPS that the application should try to achieve.
	/// </summary>
	public WaylandHostBuilder RenderFrameRate(int renderFrameRate)
	{
		_renderFrameRate = renderFrameRate;
		return this;
	}

	// Deliberately NOT an environment sniff: the AUTHORITATIVE Wayland check is the
	// wl_display_connect result at startup (WaylandApplicationHost.RunLoop). Env-based
	// gating here would make CodeBrixPlatformHostBuilder.Build() die with an opaque
	// "No platform host could be selected" instead of the clean, on-brand
	// "This application requires a Wayland compositor." fail-fast (plan decision 2.(6)).
	bool IPlatformHostBuilder.IsSupported => OperatingSystem.IsLinux();

	CodeBrixPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
	{
		if (_renderingBackend is { } backend)
		{
			ApplyRenderingBackend(backend);
		}

		return new WaylandApplicationHost(appBuilder, _renderFrameRate);
	}

	private static void ApplyRenderingBackend(WaylandRenderingBackend backend)
	{
		switch (backend)
		{
			case WaylandRenderingBackend.Vulkan:
				FeatureConfiguration.Rendering.UseVulkanOnWayland = true;
				FeatureConfiguration.Rendering.ForceVulkanOnWayland = false;
				break;
			case WaylandRenderingBackend.VulkanForced:
				FeatureConfiguration.Rendering.UseVulkanOnWayland = true;
				FeatureConfiguration.Rendering.ForceVulkanOnWayland = true;
				break;
			case WaylandRenderingBackend.OpenGLES:
				FeatureConfiguration.Rendering.UseVulkanOnWayland = false;
				FeatureConfiguration.Rendering.ForceVulkanOnWayland = false;
				FeatureConfiguration.Rendering.UseOpenGLOnWayland = true;
				break;
			case WaylandRenderingBackend.Software:
				FeatureConfiguration.Rendering.UseVulkanOnWayland = false;
				FeatureConfiguration.Rendering.ForceVulkanOnWayland = false;
				FeatureConfiguration.Rendering.UseOpenGLOnWayland = false;
				break;
			case WaylandRenderingBackend.Default:
			default:
				break;
		}
	}
}
