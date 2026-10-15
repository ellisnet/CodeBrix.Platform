namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

/// <summary>
/// Specifies the rendering backend for the X11 Skia host.
/// </summary>
/// <remarks>
/// Member values are pinned: they never renumber when a member is added or removed.
/// </remarks>
public enum X11RenderingBackend
{
	/// <summary>
	/// Platform default: try OpenGL, fall back to software. When
	/// <c>DirectGpuRenderingMode</c> is enabled (<c>UseDirectGpuRenderingMode()</c>), Vulkan is tried
	/// first, as with <see cref="Vulkan"/>.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Vulkan hardware acceleration (VK_KHR_xlib_surface). Falls back to OpenGL (GLX, else OpenGL ES)
	/// and then to software if Vulkan is unavailable.
	/// </summary>
	Vulkan = 1,

	/// <summary>
	/// OpenGL via GLX. Falls back to software if unavailable.
	/// </summary>
	OpenGL = 2,

	/// <summary>
	/// OpenGL ES via EGL. Falls back to software if unavailable.
	/// </summary>
	OpenGLES = 3,

	/// <summary>
	/// CPU-based software rendering. No GPU acceleration.
	/// </summary>
	Software = 4,
}
