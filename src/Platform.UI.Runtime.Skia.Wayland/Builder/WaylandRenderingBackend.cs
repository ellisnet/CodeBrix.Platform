namespace CodeBrix.Platform.UI.Hosting;

/// <summary>
/// Specifies the rendering backend for the Wayland Skia host.
/// </summary>
/// <remarks>
/// Member values are pinned (mirroring the X11RenderingBackend convention).
/// There is no OpenGL/GLX member: GLX does not exist on Wayland, so the only OpenGL
/// flavor is OpenGL ES via EGL.
/// </remarks>
public enum WaylandRenderingBackend
{
	/// <summary>
	/// Platform default: Vulkan hardware acceleration falling back to wl_shm software
	/// rendering, unless the CODEBRIX_WAYLAND_NO_GPU=1 environment variable forces
	/// software rendering or CODEBRIX_WAYLAND_USE_EGL=1 selects OpenGL ES.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Vulkan hardware acceleration, presenting natively via VK_KHR_wayland_surface.
	/// Falls back to software rendering if unavailable (never to OpenGL ES: Vulkan and
	/// OpenGL ES are peer GPU paths that each fall back directly to software).
	/// </summary>
	Vulkan = 1,

	/// <summary>
	/// OpenGL ES via EGL. Falls back to software if unavailable.
	/// </summary>
	OpenGLES = 2,

	/// <summary>
	/// CPU-based wl_shm software rendering. No GPU acceleration.
	/// </summary>
	Software = 3,

	/// <summary>
	/// Vulkan hardware acceleration only, with NO software fallback: if a Vulkan renderer
	/// cannot be created, the application writes a clear error to stderr and exits with a
	/// non-zero exit code. Use this to verify that a device really renders with Vulkan —
	/// with <see cref="Vulkan"/> or <see cref="Default"/>, a device without Vulkan support
	/// silently falls back to wl_shm software rendering and can be mistaken for a working
	/// Vulkan configuration.
	/// </summary>
	VulkanForced = 11,
}
