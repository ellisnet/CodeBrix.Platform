using System;
using System.Runtime.InteropServices;

namespace CodeBrix.Platform.PlayTest.OpenGL.Providers;

// Windows: WGL on opengl32.dll, the path the Win32 head takes - the GPU vendor's OpenGL driver (ICD), or on
// Windows on ARM Microsoft's "OpenCL and OpenGL Compatibility Pack". Each context lives on its own hidden,
// never-shown window (the Win32 head's dedicated-window recipe: GetDC -> ChoosePixelFormat -> SetPixelFormat
// -> wglCreateContext), created on the PlayTest UI thread, which also pumps that window's messages. Microsoft's
// built-in "GDI Generic" OpenGL 1.1 is too old for the OpenGL elements, so a machine without a driver fails
// the launch with that reason instead of rendering nothing.
internal sealed class WindowsWglProvider : IPlayTestOpenGLProvider
{
    private const string ClassName = "CodeBrixPlayTestOpenGLWindow";
    private readonly object _lock = new();
    private ushort _classAtom;
    private bool _disposed;

    public string Name => "CodeBrix.Platform.PlayTest.OpenGL (WGL on opengl32.dll)";

    public IPlayTestOpenGLContext CreateContext()
    {
        var hwnd = CreateHiddenWindow();
        var dc = WglNative.GetDC(hwnd);
        try
        {
            if (dc == 0) throw new InvalidOperationException($"GetDC failed on the hidden OpenGL window: {WglNative.LastError()}.");
            var descriptor = WglNative.PixelFormatDescriptor.Rgba32();
            var format = WglNative.ChoosePixelFormat(dc, ref descriptor);
            if (format == 0) throw new InvalidOperationException($"ChoosePixelFormat found no OpenGL pixel format: {WglNative.LastError()}.");
            if (!WglNative.SetPixelFormat(dc, format, ref descriptor))
                throw new InvalidOperationException($"SetPixelFormat failed: {WglNative.LastError()}.");
            var context = WglNative.wglCreateContext(dc);
            if (context == 0)
                throw new InvalidOperationException(
                    $"wglCreateContext failed: {WglNative.LastError()} - PlayTests with OpenGL need an interactive desktop session with a GPU "
                    + "OpenGL driver (on Windows on ARM: the OpenCL and OpenGL Compatibility Pack from the Microsoft Store).");
            var result = new WglContext(hwnd, dc, context);
            try
            {
                ContextCheck.Verify(result);
            }
            catch (Exception e)
            {
                result.Dispose();
                throw new InvalidOperationException(
                    e.Message + " PlayTests with OpenGL on Windows need an interactive desktop session with a GPU OpenGL driver "
                    + "(on Windows on ARM: the OpenCL and OpenGL Compatibility Pack from the Microsoft Store).", e);
            }
            return result;
        }
        catch
        {
            if (dc != 0) WglNative.ReleaseDC(hwnd, dc);
            WglNative.DestroyWindow(hwnd);
            throw;
        }
    }

    private nint CreateHiddenWindow()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_classAtom == 0)
            {
                // The class's window procedure is DefWindowProcW itself: the window only hosts a device context.
                var user32 = NativeLibrary.Load("user32.dll");
                var windowClass = new WglNative.WndClassEx
                {
                    cbSize = (uint)Marshal.SizeOf<WglNative.WndClassEx>(),
                    style = WglNative.CS_OWNDC,
                    lpfnWndProc = NativeLibrary.GetExport(user32, "DefWindowProcW"),
                    hInstance = WglNative.GetModuleHandleW(null),
                    lpszClassName = ClassName,
                };
                _classAtom = WglNative.RegisterClassExW(ref windowClass);
                if (_classAtom == 0) throw new InvalidOperationException($"RegisterClassEx failed for the hidden OpenGL window: {WglNative.LastError()}.");
            }
        }
        // Never shown (no WS_VISIBLE, never ShowWindow): no taskbar entry, nothing on screen.
        var hwnd = WglNative.CreateWindowExW(0, ClassName, "", WglNative.WS_OVERLAPPED, 0, 0, 8, 8, 0, 0, WglNative.GetModuleHandleW(null), 0);
        return hwnd != 0 ? hwnd : throw new InvalidOperationException($"CreateWindowEx failed for the hidden OpenGL window: {WglNative.LastError()}.");
    }

    public void Dispose()
    {
        // Contexts and their windows belong to the PlayTest UI thread, which has ended when the head disposes
        // the provider; Windows destroys a thread's windows with it. Only the class remains.
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            if (_classAtom != 0) WglNative.UnregisterClassW(ClassName, WglNative.GetModuleHandleW(null));
            _classAtom = 0;
        }
    }
}

internal sealed class WglContext : IPlayTestOpenGLContext
{
    private readonly nint _hwnd;
    private nint _dc;
    private nint _context;

    internal WglContext(nint hwnd, nint dc, nint context)
    {
        _hwnd = hwnd;
        _dc = dc;
        _context = context;
    }

    public bool IsGles => false;

    public nint GetProcAddress(string name)
    {
        // Skia probes egl* names even on WGL and needs zero for them; opengl32.dll exports only OpenGL 1.1,
        // and wglGetProcAddress (which needs a current context) serves everything newer.
        if (name.StartsWith("egl", StringComparison.Ordinal)) return 0;
        if (NativeLibrary.TryGetExport(WglNative.OpenGL32, name, out var address)) return address;
        using (WglNative.wglGetCurrentContext() == 0 && _context != 0 ? MakeCurrent() : null)
        {
            address = WglNative.wglGetProcAddress(name);
        }
        // Some drivers return small sentinel values instead of zero for an unknown name.
        return address is 0 or 1 or 2 or 3 or -1 ? 0 : address;
    }

    public IDisposable MakeCurrent()
    {
        ObjectDisposedException.ThrowIf(_context == 0, this);
        var previousDc = WglNative.wglGetCurrentDC();
        var previousContext = WglNative.wglGetCurrentContext();
        if (!WglNative.wglMakeCurrent(_dc, _context)) throw new InvalidOperationException($"wglMakeCurrent failed: {WglNative.LastError()}.");
        return new Restore(() => WglNative.wglMakeCurrent(previousDc, previousContext));
    }

    public void Dispose()
    {
        if (_context == 0) return;
        if (WglNative.wglGetCurrentContext() == _context) WglNative.wglMakeCurrent(0, 0);
        WglNative.wglDeleteContext(_context);
        WglNative.ReleaseDC(_hwnd, _dc);
        // On the UI thread that created it, where the elements dispose their contexts.
        WglNative.DestroyWindow(_hwnd);
        _context = 0;
        _dc = 0;
    }
}

internal static class WglNative
{
    internal const uint CS_OWNDC = 0x0020;
    internal const uint WS_OVERLAPPED = 0x00000000;

    private static readonly Lazy<nint> _openGL32 = new(() => NativeLibrary.Load("opengl32.dll"));

    internal static nint OpenGL32 => _openGL32.Value;

    internal static string LastError()
    {
        var code = Marshal.GetLastPInvokeError();
        return $"{Marshal.GetPInvokeErrorMessage(code)} (0x{code:X8})";
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WndClassEx
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PixelFormatDescriptor
    {
        public ushort nSize;
        public ushort nVersion;
        public uint dwFlags;
        public byte iPixelType;
        public byte cColorBits;
        public byte cRedBits;
        public byte cRedShift;
        public byte cGreenBits;
        public byte cGreenShift;
        public byte cBlueBits;
        public byte cBlueShift;
        public byte cAlphaBits;
        public byte cAlphaShift;
        public byte cAccumBits;
        public byte cAccumRedBits;
        public byte cAccumGreenBits;
        public byte cAccumBlueBits;
        public byte cAccumAlphaBits;
        public byte cDepthBits;
        public byte cStencilBits;
        public byte cAuxBuffers;
        public byte iLayerType;
        public byte bReserved;
        public uint dwLayerMask;
        public uint dwVisibleMask;
        public uint dwDamageMask;

        // The Win32 head's descriptor: RGBA8, a 16-bit depth and a stencil buffer, drawable to a window.
        internal static PixelFormatDescriptor Rgba32() => new()
        {
            nSize = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),
            nVersion = 1,
            dwFlags = 0x00000004 | 0x00000020 | 0x00000001, // PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER
            iPixelType = 0, // PFD_TYPE_RGBA
            cColorBits = 32,
            cRedBits = 8,
            cGreenBits = 8,
            cBlueBits = 8,
            cAlphaBits = 8,
            cDepthBits = 16,
            cStencilBits = 1,
            iLayerType = 0, // PFD_MAIN_PLANE
        };
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern nint GetModuleHandleW(string? moduleName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern ushort RegisterClassExW(ref WndClassEx windowClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UnregisterClassW(string className, nint instance);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DestroyWindow(nint hwnd);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint GetDC(nint hwnd);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(nint hwnd, nint dc);
    [DllImport("gdi32.dll", SetLastError = true)] internal static extern int ChoosePixelFormat(nint dc, ref PixelFormatDescriptor descriptor);
    [DllImport("gdi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetPixelFormat(nint dc, int format, ref PixelFormatDescriptor descriptor);
    [DllImport("opengl32.dll", SetLastError = true)] internal static extern nint wglCreateContext(nint dc);
    [DllImport("opengl32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool wglDeleteContext(nint context);
    [DllImport("opengl32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool wglMakeCurrent(nint dc, nint context);
    [DllImport("opengl32.dll")] internal static extern nint wglGetCurrentContext();
    [DllImport("opengl32.dll")] internal static extern nint wglGetCurrentDC();
    [DllImport("opengl32.dll")] internal static extern nint wglGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string name);
}
