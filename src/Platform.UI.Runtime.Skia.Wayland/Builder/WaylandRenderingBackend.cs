namespace CodeBrix.Platform.UI.Hosting;

/// <summary>
/// Specifies the rendering backend for the Wayland Skia host.
/// </summary>
/// <remarks>
/// Member values are pinned (mirroring the X11RenderingBackend convention) so that
/// re-enabling the commented-out Vulkan member does not renumber the members after it.
/// There is no OpenGL/GLX member: GLX does not exist on Wayland, so the only OpenGL
/// flavor is OpenGL ES via EGL.
/// </remarks>
public enum WaylandRenderingBackend
{
	/// <summary>
	/// Platform default: wl_shm software rendering, unless the
	/// CODEBRIX_WAYLAND_USE_GPU=1 environment variable opts into OpenGL ES.
	/// </summary>
	Default = 0,

	// There is no Vulkan renderer for the Wayland head yet. When one is added (the shared
	// VulkanContext in src/Platform.UI/Vulkan/ only needs a VK_KHR_wayland_surface factory),
	// uncomment this member and the matching case in WaylandHostBuilder.ApplyRenderingBackend,
	// and add the corresponding FeatureConfiguration.Rendering.UseVulkanOnWayland flag.
	///// <summary>
	///// Vulkan hardware acceleration. Falls back to OpenGL ES or software if unavailable.
	///// </summary>
	//Vulkan = 1,

	/// <summary>
	/// OpenGL ES via EGL. Falls back to software if unavailable.
	/// </summary>
	OpenGLES = 2,

	/// <summary>
	/// CPU-based wl_shm software rendering. No GPU acceleration.
	/// </summary>
	Software = 3,
}
