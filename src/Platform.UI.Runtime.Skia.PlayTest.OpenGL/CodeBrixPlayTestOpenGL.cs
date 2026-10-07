using System;
using CodeBrix.Platform.PlayTest.OpenGL.Providers;

namespace CodeBrix.Platform.PlayTest.OpenGL;

/// <summary>
/// Gives the PlayTest head real OpenGL contexts, so an application that uses the Graphics3DGL add-in
/// (<c>GLCanvasElement</c>, <c>SkiaGLCanvasElement</c>, <c>OffscreenGLContext</c>, <c>SkiaGpuContext</c>) renders
/// its OpenGL content in PlayTests: surfaceless Mesa EGL on Linux, WGL on Windows, ANGLE on Metal on macOS.
/// Call <see cref="Register"/> once, before <see cref="PlayTestApplication.LaunchAsync"/>.
/// </summary>
public static class CodeBrixPlayTestOpenGL
{
    private static readonly object _lock = new();
    private static bool _registered;

    /// <summary>True once <see cref="Register"/> has run in this process.</summary>
    public static bool IsRegistered
    {
        get { lock (_lock) return _registered; }
    }

    /// <summary>Registers this operating system's OpenGL provider with the PlayTest head. Safe to call more than
    /// once. Nothing native is loaded here: the launch creates the first context and fails with the concrete
    /// reason when the machine cannot provide one.</summary>
    /// <exception cref="PlatformNotSupportedException">The operating system is not Linux, Windows or macOS.</exception>
    public static void Register()
    {
        lock (_lock)
        {
            if (_registered) return;
            IPlayTestOpenGLProvider provider =
                OperatingSystem.IsLinux() ? new LinuxEglProvider()
                : OperatingSystem.IsWindows() ? new WindowsWglProvider()
                : OperatingSystem.IsMacOS() ? new MacOSAngleProvider()
                : throw new PlatformNotSupportedException("CodeBrix.Platform.PlayTest.OpenGL supports Linux, Windows and macOS.");
            PlayTestOpenGLProviders.Register(provider);
            _registered = true;
        }
    }
}
