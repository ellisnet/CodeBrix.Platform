using System;
using CodeBrix.Platform.PlayTest.OpenGL.Egl;
using static CodeBrix.Platform.PlayTest.OpenGL.Egl.EglNative;

namespace CodeBrix.Platform.PlayTest.OpenGL.Providers;

// Linux: the system Mesa EGL on its surfaceless platform - no X11, no Wayland, no window. Mesa itself chooses
// the GPU's render node when the machine has one and the llvmpipe CPU rasterizer otherwise; PlayTest adds no
// switch of its own.
internal sealed class LinuxEglProvider : EglProvider
{
    private const string Remedy = "PlayTests with OpenGL need the Mesa EGL stack; on Debian: apt install libegl1 libgl1-mesa-dri";

    public override string Name => "CodeBrix.Platform.PlayTest.OpenGL (Mesa EGL, surfaceless platform)";

    private protected override nint OpenDisplay()
    {
        try { EnsureLoaded(); }
        catch (DllNotFoundException e)
        {
            throw new InvalidOperationException($"libEGL.so.1 could not be loaded ({e.Message}) - {Remedy}", e);
        }
        nint display;
        // eglGetPlatformDisplay is core EGL 1.5 and exported even by the GLVND dispatcher; older stacks
        // only have the EXT variant, reached through eglGetProcAddress.
        try { display = eglGetPlatformDisplay(EGL_PLATFORM_SURFACELESS_MESA, 0, 0); }
        catch (EntryPointNotFoundException) { display = GetPlatformDisplayExt(); }
        return Initialize(display, "eglGetPlatformDisplay(EGL_PLATFORM_SURFACELESS_MESA)", Remedy);
    }

    private static unsafe nint GetPlatformDisplayExt()
    {
        var address = eglGetProcAddress("eglGetPlatformDisplayEXT");
        if (address == 0) return 0;
        return ((delegate* unmanaged<int, nint, int*, nint>)address)(EGL_PLATFORM_SURFACELESS_MESA, 0, null);
    }
}
