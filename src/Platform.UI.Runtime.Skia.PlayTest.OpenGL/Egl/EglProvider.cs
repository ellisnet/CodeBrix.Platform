using System;
using static CodeBrix.Platform.PlayTest.OpenGL.Egl.EglNative;

namespace CodeBrix.Platform.PlayTest.OpenGL.Egl;

// An off-screen OpenGL ES provider on one EGL display, shared by every context of the process. Mesa and ANGLE
// hand back the SAME EGLDisplay for the same platform, and eglTerminate on it destroys every context made
// on it, so no context ends the display: the provider does, once, when the head disposes it at the end of
// the run (unlike the Frame Buffer heads' wrappers, which each end their display on their own Dispose).
internal abstract class EglProvider : IPlayTestOpenGLProvider
{
    private readonly object _lock = new();
    private nint _display;
    private Exception? _failure;
    private bool _disposed;

    public abstract string Name { get; }

    // Loads libEGL and returns an initialized display, or throws with the concrete reason and what to do.
    private protected abstract nint OpenDisplay();

    public IPlayTestOpenGLContext CreateContext()
    {
        var display = Display();
        // The bound API is per thread; ES is the default, but a host could have changed it.
        eglBindAPI(EGL_OPENGL_ES_API);
        var config = ChooseConfig(display, pbuffer: true, out var withPbuffer)
            ?? ChooseConfig(display, pbuffer: false, out withPbuffer)
            ?? throw new InvalidOperationException($"eglChooseConfig found no RGBA8 OpenGL ES configuration: {LastError()}.");
        // Graphics3DGL needs OpenGL ES 3.0 or later; ask for 3 first (Mesa and ANGLE then give their highest 3.x).
        var context = eglCreateContext(display, config, 0, [EGL_CONTEXT_CLIENT_VERSION, 3, EGL_NONE]);
        if (context == 0) context = eglCreateContext(display, config, 0, [EGL_CONTEXT_CLIENT_VERSION, 2, EGL_NONE]);
        if (context == 0) throw new InvalidOperationException($"eglCreateContext failed: {LastError()}.");
        // The elements render into their own framebuffer objects; the 1x1 pbuffer only gives the context
        // something to be current with. Without one the context is made current with no surface
        // (EGL_KHR_surfaceless_context).
        var surface = withPbuffer ? eglCreatePbufferSurface(display, config, [EGL_WIDTH, 1, EGL_HEIGHT, 1, EGL_NONE]) : 0;
        var result = new EglContext(this, display, context, surface);
        try
        {
            ContextCheck.Verify(result);
            return result;
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    internal bool IsTerminated { get { lock (_lock) return _disposed; } }

    private nint Display()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_failure != null) throw new InvalidOperationException(_failure.Message, _failure);
            if (_display != 0) return _display;
            try { return _display = OpenDisplay(); }
            catch (Exception e)
            {
                _failure = e;
                throw;
            }
        }
    }

    private static nint? ChooseConfig(nint display, bool pbuffer, out bool withPbuffer)
    {
        withPbuffer = pbuffer;
        int[] attribs = pbuffer
            ?
            [
                EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8, EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
                EGL_DEPTH_SIZE, 8, EGL_STENCIL_SIZE, 1,
                EGL_SURFACE_TYPE, EGL_PBUFFER_BIT, EGL_RENDERABLE_TYPE, EGL_OPENGL_ES2_BIT, EGL_NONE,
            ]
            :
            [
                EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8, EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
                EGL_DEPTH_SIZE, 8, EGL_STENCIL_SIZE, 1,
                EGL_RENDERABLE_TYPE, EGL_OPENGL_ES2_BIT, EGL_NONE,
            ];
        var configs = new nint[1];
        return eglChooseConfig(display, attribs, configs, 1, out var count) && count > 0 ? configs[0] : null;
    }

    // Shared by the subclasses: initializes a display they obtained, or explains why it cannot be used.
    private protected static nint Initialize(nint display, string what, string remedy)
    {
        if (display == 0) throw new InvalidOperationException($"{what} returned no display ({LastError()}) - {remedy}");
        if (!eglInitialize(display, out _, out _))
            throw new InvalidOperationException($"eglInitialize failed on {what}: {LastError()} - {remedy}");
        return display;
    }

    public void Dispose()
    {
        nint display;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            display = _display;
            _display = 0;
        }
        if (display == 0) return;
        // Every context is still alive (the elements cache theirs for the life of the window); terminating
        // the display releases them all. Nothing renders any more when the head calls this.
        eglTerminate(display);
        eglReleaseThread();
    }
}

internal sealed class EglContext : IPlayTestOpenGLContext
{
    private readonly EglProvider _provider;
    private readonly nint _display;
    private nint _context;
    private nint _surface;

    internal EglContext(EglProvider provider, nint display, nint context, nint surface)
    {
        _provider = provider;
        _display = display;
        _context = context;
        _surface = surface;
    }

    public bool IsGles => true;

    // eglGetProcAddress serves core and extension GL ES entry points (EGL 1.5) as well as the egl* names Skia
    // probes; it returns zero for names it does not know.
    public nint GetProcAddress(string name)
    {
        try { return eglGetProcAddress(name); }
        catch (Exception) { return 0; }
    }

    public IDisposable MakeCurrent()
    {
        ObjectDisposedException.ThrowIf(_context == 0, this);
        var previousDisplay = eglGetCurrentDisplay();
        var previousContext = eglGetCurrentContext();
        var previousDraw = eglGetCurrentSurface(EGL_DRAW);
        var previousRead = eglGetCurrentSurface(EGL_READ);
        if (!eglMakeCurrent(_display, _surface, _surface, _context))
            throw new InvalidOperationException($"eglMakeCurrent failed: {LastError()}.");
        var display = _display;
        return new Restore(() =>
        {
            // Nothing was current before: release ours through our own display, since restoring
            // EGL_NO_DISPLAY is itself an error (the X11 head's lesson).
            if (previousDisplay == 0) eglMakeCurrent(display, 0, 0, 0);
            else eglMakeCurrent(previousDisplay, previousDraw, previousRead, previousContext);
        });
    }

    public void Dispose()
    {
        if (_context == 0) return;
        // After the provider ended the display, its contexts are already gone.
        if (!_provider.IsTerminated)
        {
            if (eglGetCurrentContext() == _context) eglMakeCurrent(_display, 0, 0, 0);
            if (_surface != 0) eglDestroySurface(_display, _surface);
            eglDestroyContext(_display, _context);
        }
        _surface = 0;
        _context = 0;
    }
}
