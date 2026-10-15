using CodeBrix.Platform.UI;
using CodeBrix.Platform.UI.Hosting;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.X11;

/// <summary>
/// The X11 head's "which renderer to try first" decision, kept apart from the window creation in
/// <see cref="X11XamlRootHost"/> so it can be tested without an X server.
/// </summary>
internal static class X11RendererSelection
{
	/// <summary>
	/// Whether the head tries its Vulkan renderer before the OpenGL/software chain.
	/// </summary>
	/// <param name="useVulkanOnX11"><see cref="FeatureConfiguration.Rendering.UseVulkanOnX11"/>: an explicit
	/// true or false always decides.</param>
	/// <param name="useOpenGLOnX11"><see cref="FeatureConfiguration.Rendering.UseOpenGLOnX11"/>; any value other
	/// than null is an explicit renderer choice by the application.</param>
	/// <param name="preferGlesOverGlOnX11"><see cref="FeatureConfiguration.Rendering.PreferGLESOverGLOnX11"/>; true
	/// is an explicit renderer choice by the application.</param>
	/// <param name="directGpuRenderingEnabled"><see cref="DirectGpuRenderingMode.IsEnabled"/>.</param>
	/// <returns>True to try Vulkan first (falling back to the OpenGL/software chain on failure).</returns>
	/// <remarks>
	/// With no explicit Vulkan choice, Vulkan is preferred only when the application opted into
	/// <see cref="DirectGpuRenderingMode"/> AND made no other X11 renderer choice. With the latch off, the
	/// result is false whenever <paramref name="useVulkanOnX11"/> is not true, so the X11 renderer selection is
	/// exactly what it was before the latch existed (OpenGL by default).
	/// </remarks>
	internal static bool ShouldTryVulkan(bool? useVulkanOnX11, bool? useOpenGLOnX11, bool preferGlesOverGlOnX11, bool directGpuRenderingEnabled)
		=> useVulkanOnX11 ?? (directGpuRenderingEnabled && useOpenGLOnX11 is null && !preferGlesOverGlOnX11);

	/// <summary>
	/// <see cref="ShouldTryVulkan(bool?, bool?, bool, bool)"/> over the current feature flags and latch.
	/// </summary>
	/// <returns>True to try Vulkan first.</returns>
	internal static bool ShouldTryVulkan()
		=> ShouldTryVulkan(
			FeatureConfiguration.Rendering.UseVulkanOnX11,
			FeatureConfiguration.Rendering.UseOpenGLOnX11,
			FeatureConfiguration.Rendering.PreferGLESOverGLOnX11,
			DirectGpuRenderingMode.IsEnabled);

	/// <summary>
	/// Why the head tried Vulkan, for the one log line written when it falls back.
	/// </summary>
	/// <returns>A short description of the setting that selected Vulkan.</returns>
	internal static string VulkanReason()
		=> FeatureConfiguration.Rendering.UseVulkanOnX11 == true
			? "selected with X11RenderingBackend.Vulkan / FeatureConfiguration.Rendering.UseVulkanOnX11"
			: "preferred by UseDirectGpuRenderingMode()";
}
