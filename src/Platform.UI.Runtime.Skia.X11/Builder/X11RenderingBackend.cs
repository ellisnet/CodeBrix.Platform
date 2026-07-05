namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

/// <summary>
/// Specifies the rendering backend for the X11 Skia host.
/// </summary>
/// <remarks>
/// Explicit member values match the Uno Platform 6.7.x enum so that re-enabling the
/// commented-out Vulkan member does not renumber the members that follow it.
/// </remarks>
public enum X11RenderingBackend
{
	/// <summary>
	/// Platform default: try OpenGL, fall back to software.
	/// </summary>
	Default = 0,

	// The Vulkan renderer is present in the repo but is not a completed/tested/supported
	// configuration yet, so it is not selectable. Uncomment this member (and the matching
	// case in X11HostBuilder.ApplyRenderingBackend) when Vulkan rendering is officially
	// offered; see also FeatureConfiguration.Rendering.UseVulkanOnX11.
	///// <summary>
	///// Vulkan hardware acceleration. Falls back to OpenGL or software if unavailable.
	///// </summary>
	//Vulkan = 1,

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
