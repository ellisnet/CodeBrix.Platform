using System;
using CodeBrix.Platform.PlayTest.WebGpu.Providers;

namespace CodeBrix.Platform.PlayTest.WebGpu;

/// <summary>
/// Gives the PlayTest head real WebGPU devices, so an application that uses the WebGpu add-in
/// (<c>WebGpuRenderView</c>) renders its WebGPU content in PlayTests: the launch proves that an adapter and a device can
/// be created (Vulkan on Linux, Direct3D 12 or Vulkan on Windows, Metal on macOS), and every view then creates its own
/// device and renders through its read-back path. Call <see cref="Register"/> once, before
/// <see cref="PlayTestApplication.LaunchAsync"/>.
/// </summary>
public static class CodeBrixPlayTestWebGpu
{
    private static readonly object _lock = new();
    private static bool _registered;

    /// <summary>True once <see cref="Register"/> has run in this process.</summary>
    public static bool IsRegistered
    {
        get { lock (_lock) return _registered; }
    }

    /// <summary>Registers the WebGPU provider with the PlayTest head. Safe to call more than once. No device is
    /// created here: the launch creates the first one and fails with the concrete reason when the machine cannot
    /// provide one.</summary>
    /// <exception cref="PlatformNotSupportedException">The operating system is not Linux, Windows or macOS.</exception>
    public static void Register()
    {
        lock (_lock)
        {
            if (_registered) return;
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
                throw new PlatformNotSupportedException("CodeBrix.Platform.PlayTest.WebGpu supports Linux, Windows and macOS.");
            PlayTestWebGpuProviders.Register(new WebGpuSkiaDeviceProvider());
            _registered = true;
        }
    }
}
