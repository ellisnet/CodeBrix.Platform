using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CodeBrix.Platform.OpenGL;

namespace CodeBrix.Platform.PlayTest.OpenGL;

// Proves a new context is one the Graphics3DGL elements can use: it reports OpenGL (ES) 3.0 or later (the
// elements' floor, which they skip for ANGLE), and clearing a small framebuffer object and reading it back
// returns the clear colour - "a context exists" then really means "it renders".
internal static partial class ContextCheck
{
    [GeneratedRegex(@"(\d+)\.(\d+)")]
    private static partial Regex VersionNumber();

    internal static void Verify(IPlayTestOpenGLContext context)
    {
        using (context.MakeCurrent())
        using (var gl = GL.GetApi(name => context.GetProcAddress(name)))
        {
            var version = gl.GetStringS(StringName.Version);
            if (string.IsNullOrEmpty(version)) throw new InvalidOperationException("The context reports no GL_VERSION.");
            var renderer = gl.GetStringS(StringName.Renderer);
            if (!version.Contains("ANGLE", StringComparison.Ordinal))
            {
                var match = VersionNumber().Match(version);
                var major = match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
                if (major < 3)
                    throw new InvalidOperationException(
                        $"The context provides only GL_VERSION '{version}' (GL_RENDERER '{renderer}'); OpenGL elements need OpenGL 3.0 or later.");
            }
            ClearAndReadBack(gl);
        }
    }

    private static unsafe void ClearAndReadBack(GL gl)
    {
        const int Size = 4;
        var texture = gl.GenTexture();
        var framebuffer = gl.GenFramebuffer();
        try
        {
            gl.BindTexture(GLEnum.Texture2D, texture);
            gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, Size, Size, 0, GLEnum.Rgba, GLEnum.UnsignedByte, (void*)0);
            gl.BindFramebuffer(GLEnum.Framebuffer, framebuffer);
            gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, GLEnum.Texture2D, texture, 0);
            var status = gl.CheckFramebufferStatus(GLEnum.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
                throw new InvalidOperationException($"A {Size}x{Size} RGBA framebuffer object is not complete ({status}).");
            gl.Viewport(0, 0, Size, Size);
            gl.ClearColor(0.25f, 0.5f, 0.75f, 1f);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            var pixel = stackalloc byte[4];
            gl.ReadPixels(1, 1, 1, 1, GLEnum.Rgba, GLEnum.UnsignedByte, pixel);
            var error = gl.GetError();
            if (error != GLEnum.NoError) throw new InvalidOperationException($"Clearing and reading back a framebuffer object raised {error}.");
            if (Math.Abs(pixel[0] - 64) > 2 || Math.Abs(pixel[1] - 128) > 2 || Math.Abs(pixel[2] - 191) > 2 || pixel[3] != 255)
                throw new InvalidOperationException(
                    $"Clearing a framebuffer object to (64, 128, 191, 255) read back ({pixel[0]}, {pixel[1]}, {pixel[2]}, {pixel[3]}): the context does not render.");
        }
        finally
        {
            gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            gl.BindTexture(GLEnum.Texture2D, 0);
            gl.DeleteFramebuffer(framebuffer);
            gl.DeleteTexture(texture);
        }
    }
}
