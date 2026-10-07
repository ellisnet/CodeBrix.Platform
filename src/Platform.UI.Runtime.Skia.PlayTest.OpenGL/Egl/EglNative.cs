using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.PlayTest.OpenGL.Egl;

// The EGL 1.5 entry points the Linux (Mesa) and macOS (ANGLE) providers use. "libEGL" resolves per OS:
// libEGL.so.1 on Linux (the runtime soname; libEGL.so is a development symlink that is often absent) and
// libEGL.dylib on macOS, which the Graphics3DGL package ships in runtimes/osx/native.
internal static class EglNative
{
    private const string LibEgl = "libEGL";

    internal const int EGL_DEFAULT_DISPLAY = 0;
    internal const int EGL_PLATFORM_SURFACELESS_MESA = 0x31DD;
    internal const int EGL_SUCCESS = 0x3000;
    internal const int EGL_ALPHA_SIZE = 0x3021;
    internal const int EGL_BLUE_SIZE = 0x3022;
    internal const int EGL_GREEN_SIZE = 0x3023;
    internal const int EGL_RED_SIZE = 0x3024;
    internal const int EGL_DEPTH_SIZE = 0x3025;
    internal const int EGL_STENCIL_SIZE = 0x3026;
    internal const int EGL_SURFACE_TYPE = 0x3033;
    internal const int EGL_NONE = 0x3038;
    internal const int EGL_RENDERABLE_TYPE = 0x3040;
    internal const int EGL_HEIGHT = 0x3056;
    internal const int EGL_WIDTH = 0x3057;
    internal const int EGL_DRAW = 0x3059;
    internal const int EGL_READ = 0x305A;
    internal const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
    internal const int EGL_OPENGL_ES_API = 0x30A0;
    internal const int EGL_PBUFFER_BIT = 0x0001;
    internal const int EGL_OPENGL_ES2_BIT = 0x0004;

    static EglNative() => NativeLibrary.SetDllImportResolver(typeof(EglNative).Assembly, Resolve);

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != LibEgl) return 0;
        return NativeLibrary.Load(OperatingSystem.IsLinux() ? "libEGL.so.1" : LibEgl, assembly, searchPath);
    }

    // Loads the library now, so a missing one is reported once, with the provider's hint.
    internal static void EnsureLoaded() => _ = eglGetError();

    [DllImport(LibEgl)] internal static extern int eglGetError();
    [DllImport(LibEgl)] internal static extern nint eglGetDisplay(nint nativeDisplay);
    [DllImport(LibEgl)] internal static extern nint eglGetPlatformDisplay(int platform, nint nativeDisplay, nint attribs);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglInitialize(nint display, out int major, out int minor);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglTerminate(nint display);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglBindAPI(int api);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglChooseConfig(nint display, int[] attribs, [In, Out] nint[] configs, int size, out int count);
    [DllImport(LibEgl)] internal static extern nint eglCreateContext(nint display, nint config, nint share, int[] attribs);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglDestroyContext(nint display, nint context);
    [DllImport(LibEgl)] internal static extern nint eglCreatePbufferSurface(nint display, nint config, int[] attribs);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglDestroySurface(nint display, nint surface);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglMakeCurrent(nint display, nint draw, nint read, nint context);
    [DllImport(LibEgl)] internal static extern nint eglGetCurrentContext();
    [DllImport(LibEgl)] internal static extern nint eglGetCurrentDisplay();
    [DllImport(LibEgl)] internal static extern nint eglGetCurrentSurface(int which);
    [DllImport(LibEgl)] [return: MarshalAs(UnmanagedType.U1)] internal static extern bool eglReleaseThread();
    [DllImport(LibEgl)] internal static extern nint eglGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string name);

    internal static string Error(int code) => code switch
    {
        0x3000 => "EGL_SUCCESS",
        0x3001 => "EGL_NOT_INITIALIZED",
        0x3002 => "EGL_BAD_ACCESS",
        0x3003 => "EGL_BAD_ALLOC",
        0x3004 => "EGL_BAD_ATTRIBUTE",
        0x3005 => "EGL_BAD_CONFIG",
        0x3006 => "EGL_BAD_CONTEXT",
        0x3007 => "EGL_BAD_CURRENT_SURFACE",
        0x3008 => "EGL_BAD_DISPLAY",
        0x3009 => "EGL_BAD_MATCH",
        0x300A => "EGL_BAD_NATIVE_PIXMAP",
        0x300B => "EGL_BAD_NATIVE_WINDOW",
        0x300C => "EGL_BAD_PARAMETER",
        0x300D => "EGL_BAD_SURFACE",
        0x300E => "EGL_CONTEXT_LOST",
        _ => "EGL error",
    } + $" (0x{code:X4})";

    internal static string LastError() => Error(eglGetError());
}
