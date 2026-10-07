using System;
using CodeBrix.Platform.PlayTest.OpenGL.Egl;
using static CodeBrix.Platform.PlayTest.OpenGL.Egl.EglNative;

namespace CodeBrix.Platform.PlayTest.OpenGL.Providers;

// macOS: ANGLE on Metal, through the libEGL.dylib / libGLESv2.dylib the Graphics3DGL package ships in
// runtimes/osx/native - the same default display the macOS head uses. There is no software fallback: a Mac
// without a Metal device cannot run PlayTests with OpenGL.
internal sealed class MacOSAngleProvider : EglProvider
{
    public override string Name => "CodeBrix.Platform.PlayTest.OpenGL (ANGLE on Metal)";

    private protected override nint OpenDisplay()
    {
        try { EnsureLoaded(); }
        catch (DllNotFoundException e)
        {
            throw new InvalidOperationException(
                $"libEGL.dylib (ANGLE) could not be loaded ({e.Message}) - it ships in CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever "
                + "(runtimes/osx/native), which this package depends on; make sure the test project's output contains it.", e);
        }
        return Initialize(eglGetDisplay(EGL_DEFAULT_DISPLAY), "ANGLE's default display (eglGetDisplay(EGL_DEFAULT_DISPLAY))",
            "ANGLE renders through Metal, so PlayTests with OpenGL need a Mac with a Metal device");
    }
}
