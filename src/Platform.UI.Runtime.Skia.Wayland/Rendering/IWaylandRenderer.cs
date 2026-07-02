using System;
using SkiaSharp;

namespace CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;

/// <summary>
/// A window renderer: presents the XAML content of one window into its wl_surface. Two
/// implementations exist — the default <see cref="WaylandShmRenderer"/> (software, wl_shm)
/// and the opt-in <see cref="WaylandEglRenderer"/> (GPU, EGL/GLES).
/// </summary>
internal interface IWaylandRenderer : IDisposable
{
	void SetBackgroundColor(SKColor color);

	void Render();
}
