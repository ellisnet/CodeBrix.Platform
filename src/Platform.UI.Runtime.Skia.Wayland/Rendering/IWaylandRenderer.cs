using System;
using SkiaSharp;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// A window renderer: presents the XAML content of one window into its wl_surface. Three
/// implementations exist — the default <see cref="WaylandVulkanRenderer"/> (GPU, Vulkan), the
/// opt-in <see cref="WaylandEglRenderer"/> (GPU, EGL/GLES), and <see cref="WaylandShmRenderer"/>
/// (software, wl_shm), which is also what a window falls back to when the GPU renderers cannot
/// start.
/// </summary>
internal interface IWaylandRenderer : IDisposable
{
	void SetBackgroundColor(SKColor color);

	void Render();
}
