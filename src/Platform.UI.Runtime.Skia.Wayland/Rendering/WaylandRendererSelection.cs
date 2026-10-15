using System;
using CodeBrix.Platform.UI;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>The renderer the Wayland head creates for a window.</summary>
internal enum WaylandRendererChoice
{
	/// <summary>Vulkan via VK_KHR_wayland_surface (falls back to software unless forced).</summary>
	Vulkan,

	/// <summary>OpenGL ES via EGL (falls back to software).</summary>
	OpenGLES,

	/// <summary>wl_shm software rendering.</summary>
	Software,
}

/// <summary>
/// The Wayland head's renderer decision, kept apart from <see cref="WaylandXamlRootHost"/> so it can be
/// tested without a compositor, and so the native GPU device provider can tell - before the lazily
/// created renderer exists - whether the window is going to render with Vulkan.
/// </summary>
internal static class WaylandRendererSelection
{
	/// <summary>
	/// Chooses the renderer. Precedence: <c>WaylandHostBuilder.RenderingBackend</c> / FeatureConfiguration
	/// first, then the environment (CODEBRIX_WAYLAND_NO_GPU=1 forces software rendering,
	/// CODEBRIX_WAYLAND_USE_EGL=1 selects OpenGL ES; NO_GPU wins), then Vulkan.
	/// </summary>
	/// <param name="forceVulkan"><see cref="FeatureConfiguration.Rendering.ForceVulkanOnWayland"/>.</param>
	/// <param name="useVulkan"><see cref="FeatureConfiguration.Rendering.UseVulkanOnWayland"/>.</param>
	/// <param name="useEgl"><see cref="FeatureConfiguration.Rendering.UseOpenGLOnWayland"/>.</param>
	/// <param name="environmentNoGpu">CODEBRIX_WAYLAND_NO_GPU is "1".</param>
	/// <param name="environmentUseEgl">CODEBRIX_WAYLAND_USE_EGL is "1".</param>
	/// <returns>The renderer to create first.</returns>
	internal static WaylandRendererChoice Choose(bool forceVulkan, bool? useVulkan, bool? useEgl, bool environmentNoGpu, bool environmentUseEgl)
	{
		if (forceVulkan)
		{
			return WaylandRendererChoice.Vulkan;
		}

		// The environment is consulted ONLY when code decided nothing: code always beats environment.
		if (useVulkan == null && useEgl == null)
		{
			if (environmentNoGpu)
			{
				return WaylandRendererChoice.Software;
			}

			return environmentUseEgl ? WaylandRendererChoice.OpenGLES : WaylandRendererChoice.Vulkan;
		}

		if (useVulkan == true)
		{
			return WaylandRendererChoice.Vulkan;
		}

		return useEgl == true ? WaylandRendererChoice.OpenGLES : WaylandRendererChoice.Software;
	}

	/// <summary><see cref="Choose(bool, bool?, bool?, bool, bool)"/> over the current flags and environment.</summary>
	/// <returns>The renderer to create first.</returns>
	internal static WaylandRendererChoice Choose()
		=> Choose(
			FeatureConfiguration.Rendering.ForceVulkanOnWayland,
			FeatureConfiguration.Rendering.UseVulkanOnWayland,
			FeatureConfiguration.Rendering.UseOpenGLOnWayland,
			IsEnvironmentFlagSet("CODEBRIX_WAYLAND_NO_GPU"),
			IsEnvironmentFlagSet("CODEBRIX_WAYLAND_USE_EGL"));

	private static bool IsEnvironmentFlagSet(string name)
		=> string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.Ordinal);
}
