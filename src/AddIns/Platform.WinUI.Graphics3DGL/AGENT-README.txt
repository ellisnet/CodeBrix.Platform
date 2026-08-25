================================================================================
AGENT-README: CodeBrix.Platform.Graphics3DGL
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.Graphics3DGL is the OpenGL add-in for CodeBrix.Platform
applications (.NET 10 or later). It gives a XAML page two GPU-rendered
elements and two helpers for off-screen GPU work:

    GLCanvasElement       an abstract XAML element (a Grid) that you subclass
                          to draw with raw OpenGL 3.0+ through a GL binding
                          object (Init / RenderOverride / OnDestroy).
    SkiaGLCanvasElement   a ready-made XAML element (a Grid) that hands you a
                          GPU-backed SkiaSharp SKSurface each frame — the
                          functional equivalent of SkiaSharp's SKGLView.
    OffscreenGLContext    a headless OpenGL context (plus a GRContext factory)
                          for rendering into your own framebuffer with no
                          XAML element involved.
    SkiaGpuContext        a backend-neutral GPU Skia GRContext (OpenGL/GLES on
                          most heads, Metal on macOS) for off-screen work.

Everything renders OFF-SCREEN and is composited into the Skia scene: the two
elements read the finished frame back to CPU pixels into a WriteableBitmap
that is shown as the element's Background. There is no native child window,
so the element clips, scrolls, overlaps and animates like any other XAML
element, and XAML children can be layered on top of it (both elements derive
from Grid).

Provenance: a port of the upstream project's Graphics3DGL add-in. The
namespace is CodeBrix.Platform.WinUI.Graphics3DGL; do not use the upstream
namespace. The OpenGL binding type (GL) and its enums come from the
CodeBrix.Platform.OpenGL package (namespace CodeBrix.Platform.OpenGL, a
Silk.NET-derived binding) — not from any Silk.NET namespace.

INSTALLATION
============
Package id:   CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever
License:      Apache-2.0 (the macOS ANGLE libraries bundled in the package are
              BSD-3-Clause; see the package's THIRD-PARTY-NOTICES.txt)

    dotnet add package CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever

Reference it ONCE, in the .Core (shared UI) project — never in a head
project. NuGet dependencies (resolved automatically):
    CodeBrix.Platform.ApacheLicenseForever       the core UI framework
    CodeBrix.Platform.OpenGL.MitLicenseForever   the GL binding (GL, GLEnum, ...)
    SkiaSharp                                    GRContext / SKSurface for the
                                                 GPU-Skia members

RUNTIME REQUIREMENT: an OpenGL context of version 3.0 or later, supplied by
the head. Every CodeBrix.Platform head provides one:

    Windows Win32-Skia, WPF-Skia   WGL — needs a desktop OpenGL driver (ICD),
                                   see WINDOWS OPENGL AVAILABILITY below
    Linux X11                      GLX
    Linux Wayland                  EGL (works under the default Vulkan presenter)
    Linux Frame Buffer             DRM/GBM, or Mesa llvmpipe software GL on
                                   GPU-less systems — install libegl1 and
                                   libgl1-mesa-dri there (Debian-based Linux:
                                   apt install libegl1 libgl1-mesa-dri)
    macOS                          ANGLE, bundled in the package (runtimes/osx/
                                   native/libEGL.dylib + libGLESv2.dylib);
                                   nothing to install

The minimum-version check is skipped when the context reports itself as
ANGLE.

WINDOWS OPENGL AVAILABILITY
---------------------------
On Windows the WGL context needs a real OpenGL driver (ICD). Most x64
machines have one from their GPU vendor, but many Windows-on-ARM devices do
NOT ship a desktop-OpenGL ICD; there, OpenGL is supplied by Microsoft's free
"OpenCL and OpenGL Compatibility Pack" (the GLon12 / Mesa-over-Direct3D-12
layer), installed once per device from the Microsoft Store:

    https://apps.microsoft.com/detail/9NQPSL29BFFF

Without it the context cannot be created and the surface stays blank. The
rest of the app is unaffected: the head just renders 2D Skia on the CPU.
This is a device-level prerequisite the end user installs; an app or the
framework cannot supply it. Detect it with GetGLInitializationState() (see
COMPLETE EXAMPLES, "Detecting failure") and tell the user.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.WinUI.Graphics3DGL;  // GLCanvasElement, SkiaGLCanvasElement,
                                                 // GLInitializationState/Status,
                                                 // SkiaGLPaintSurfaceEventArgs,
                                                 // OffscreenGLContext, SkiaGpuContext
    using CodeBrix.Platform.OpenGL;              // GL, GLEnum, ClearBufferMask, EnableCap,
                                                 // BufferTargetARB, ShaderType, ...
                                                 // (from the OpenGL package)
    using CodeBrix.Platform.Graphics;            // SkiaGpuBackend enum (core package)
    using SkiaSharp;                             // SKSurface, GRContext, SKImageInfo
    using Microsoft.UI.Xaml;                     // Window, XamlRoot

XAML: GLCanvasElement is abstract, so the XAML namespace is YOUR subclass's:
    xmlns:render="using:MyApp.Rendering"
    <render:SpinningTriangle x:Name="Triangle" />

CORE API REFERENCE
==================

GLCanvasElement (abstract)
--------------------------
    namespace CodeBrix.Platform.WinUI.Graphics3DGL;

    public abstract partial class GLCanvasElement : Grid, INativeContext
    {
        protected GLCanvasElement(Func<Window>? getWindowFunc);
        protected abstract void Init(GL gl);
        protected abstract void OnDestroy(GL gl);
        protected abstract void RenderOverride(GL gl);
        public void Invalidate();
        public static DependencyProperty IsGLInitializedProperty { get; }
        public bool? IsGLInitialized { get; }          // read-only
        public GLInitializationState GetGLInitializationState();
    }

Constructor: getWindowFunc is used only by the native-WinUI build; on
CodeBrix.Platform pass null (": base(null)"). (INativeContext is the GL
binding's function-loader interface; the element implements it for the
binding's benefit, you never call it.)

Init(GL gl)            Create your GL resources (shaders, VAOs, VBOs,
                       textures). Called with the context current, after the
                       element's offscreen framebuffer exists. It can run
                       more than once: every call after the first is preceded
                       by an OnDestroy call (unload + reload of the element).
OnDestroy(GL gl)       Release everything Init created. Called with the
                       context current when the element is unloaded.
RenderOverride(GL gl)  Draw one frame. Before the call the element's
                       framebuffer is bound and the viewport is set to the
                       element's RenderSize. The context is SHARED with the
                       head's own Skia renderer, so restore every GL state you
                       change before returning (unbind your VAO/program,
                       disable depth test if you enabled it, and so on).
Invalidate()           Queue exactly ONE call to RenderOverride; the result
                       is kept until the next Invalidate. Call it after each
                       state change; call it at the END of RenderOverride to
                       run a continuous animation.
IsGLInitialized        null while the element is not loaded; true after a
                       successful Loaded; false when initialization failed.
                       Read-only: SetValue on the property throws
                       InvalidOperationException.
GetGLInitializationState()
                       A snapshot with Status and (on failure) FailedReason.
                       UI thread only. NotYetInitialized before the element
                       is loaded and again after it is unloaded.

Lifecycle, in order:
  1. Loaded: Status -> Initializing. The head's native OpenGL context is
     created — once per XamlRoot (window) and shared by every
     GLCanvasElement in that window; it is destroyed when the window closes.
     The GL version is checked (3.0 minimum; skipped for ANGLE) and a GL
     binding is created.
  2. With the context current: the offscreen framebuffer is built for the
     element's RenderSize (an RGB colour texture plus a Depth24Stencil8
     renderbuffer), then Init(gl) runs. Status -> Initialized,
     IsGLInitialized = true.
  3. Each Invalidate(): framebuffer bound, viewport = RenderSize,
     RenderOverride(gl), then the colour attachment is read back (BGRA) into
     a WriteableBitmap and presented as the Grid's Background — an ImageBrush
     with ScaleY = -1, because OpenGL's origin is bottom-left.
  4. SizeChanged rebuilds the framebuffer and back buffer and invalidates. A
     zero-sized element (collapsed or not yet arranged) is skipped until it
     has a real size.
  5. Unloaded: OnDestroy(gl) with the context current; Status returns to
     NotYetInitialized and IsGLInitialized to null.

Failure handling: if the context cannot be created, the GL version is too
low, or YOUR Init/RenderOverride throws (typically a shader that will not
compile on this driver), the element records
GLInitializationStatus.InitializationFailed with a FailedReason (for your
own exception: its Message), sets IsGLInitialized = false, logs the error
and stops calling RenderOverride until the element is unloaded and reloaded.
The surface simply stays blank; no exception escapes to the app.

Pixel geometry: the framebuffer, viewport and back buffer are all
RenderSize.Width x RenderSize.Height — the element's LAYOUT size in DIPs,
not physical pixels. On a scaled display the compositor upscales the result.

GLInitializationStatus and GLInitializationState
------------------------------------------------
    public enum GLInitializationStatus
    {
        NotYetInitialized = 0,   // not loaded yet, or unloaded again
        Initializing = 1,        // rarely observed: init is synchronous in Loaded
        Initialized = 2,         // == IsGLInitialized true
        InitializationFailed = 3 // == IsGLInitialized false; see FailedReason
    }

    public sealed class GLInitializationState
    {
        public GLInitializationStatus Status { get; }
        public string? FailedReason { get; }   // non-null if and only if
                                               // Status == InitializationFailed
    }
The constructor is internal; instances come only from
GetGLInitializationState().

SkiaGLCanvasElement
-------------------
    public partial class SkiaGLCanvasElement : Grid
    {
        public SkiaGLCanvasElement(Func<Window>? getWindowFunc = null);
        public event EventHandler<SkiaGLPaintSurfaceEventArgs>? PaintSurface;
        public bool? IsGpuInitialized { get; }        // null until loaded
        protected virtual void OnPaintSurface(SkiaGLPaintSurfaceEventArgs args);
        public void Invalidate();
    }

On Loaded it creates an OffscreenGLContext and a GRContext on it. Each
Invalidate() makes the context current, raises PaintSurface (or your
OnPaintSurface override) with a GPU SKSurface sized to RenderSize
(BGRA8888 premultiplied, falling back to RGBA8888 if the GPU will not render
BGRA), flushes the surface and the GRContext, reads the pixels back in one
copy into a WriteableBitmap and presents it as the Background. No vertical
flip is involved (a GPU SKSurface has a top-left origin). Failure to create
the context or GRContext is logged and reported only as IsGpuInitialized ==
false — this element has no FailedReason. The surface is rebuilt on
SizeChanged; GPU resources are released on Unloaded and when the window
closes. Call Invalidate() from inside the handler to animate.

SkiaGLPaintSurfaceEventArgs
---------------------------
    public sealed class SkiaGLPaintSurfaceEventArgs : EventArgs
    {
        public SkiaGLPaintSurfaceEventArgs(SKSurface surface, GRContext context, SKImageInfo info);
        public SKSurface Surface { get; }     // draw on Surface.Canvas; context is current
        public GRContext Context { get; }
        public SKImageInfo Info { get; }      // width, height and colour type of Surface
    }

OffscreenGLContext
------------------
    public sealed class OffscreenGLContext : IDisposable
    {
        public static bool TryCreate(XamlRoot xamlRoot,
                                     [NotNullWhen(true)] out OffscreenGLContext? context);
        public GL Gl { get; }
        public IDisposable MakeCurrent();
        public IntPtr GetProcAddress(string name);   // IntPtr.Zero when absent; never throws
        public GRContext CreateGrContext();          // throws InvalidOperationException when
                                                     // neither GL flavour works
        public void Dispose();
    }

Rules: only touch Gl (or a GRContext built on it) inside a `using` of
MakeCurrent(); MakeCurrent saves and restores whatever context was current,
so it never disturbs the head's renderer even on the same thread. Keep the
context and its GRContext on the thread that created them. Dispose the
GRContext (inside a MakeCurrent scope) BEFORE disposing the
OffscreenGLContext. TryCreate returns false when the head provides no native
OpenGL context (for example a unit-test host). CreateGrContext tries the GL
flavour the running head implies first (X11, Win32-Skia, WPF-Skia: desktop
GL; Wayland, macOS, Frame Buffer: GLES) and falls back to the other flavour,
so it also works on an unrecognized host.

SkiaGpuContext
--------------
    public sealed class SkiaGpuContext : IDisposable
    {
        public static bool TryCreate(XamlRoot xamlRoot,
                                     [NotNullWhen(true)] out SkiaGpuContext? context);
        public GRContext GrContext { get; }
        public SkiaGpuBackend Backend { get; }   // CodeBrix.Platform.Graphics enum:
                                                 // OpenGL, Metal (diagnostic only)
        public IDisposable BeginFrame();
        public void Dispose();
    }

The backend-neutral way to get GPU Skia: on macOS it resolves the head's
Skia-on-Metal provider (and if that head is in software-rendering mode,
TryCreate returns false — it deliberately does NOT fall through to OpenGL);
on every other head it wraps an OffscreenGLContext. Wrap each frame's GPU
work in a single `using` of BeginFrame() (makes the GL context current, or
is a no-op on Metal). TryCreate returning false means "keep your CPU
fallback". Dispose() releases the GRContext inside a frame scope and then the
underlying context.

COMPLETE EXAMPLES
=================

1. A spinning triangle: GLCanvasElement subclass with a render loop
-------------------------------------------------------------------
SpinningTriangle.cs in the .Core project. Every GL call below is the same
kind used by the repository's own sample (see WORKING EXAMPLES ON GITHUB).

    using System;
    using System.Diagnostics;
    using System.Numerics;
    using CodeBrix.Platform.OpenGL;
    using CodeBrix.Platform.WinUI.Graphics3DGL;

    namespace MyApp.Rendering;

    public sealed class SpinningTriangle : GLCanvasElement
    {
        // "#version 300 es" is the GLES 3.0 dialect the repository's sample
        // uses. If a desktop-GL driver rejects it, use "#version 330 core"
        // instead; a rejected shader is reported through FailedReason.
        const string VertexShaderSource = """
            #version 300 es
            precision highp float;
            layout (location = 0) in vec2 aPosition;
            layout (location = 1) in vec3 aColor;
            uniform mat4 uTransform;
            out vec3 vColor;
            void main()
            {
                gl_Position = uTransform * vec4(aPosition, 0.0, 1.0);
                vColor = aColor;
            }
            """;

        const string FragmentShaderSource = """
            #version 300 es
            precision highp float;
            in vec3 vColor;
            out vec4 fragColor;
            void main() { fragColor = vec4(vColor, 1.0); }
            """;

        uint program, vertexArray, vertexBuffer, indexBuffer;
        int transformLocation;
        readonly Stopwatch clock = Stopwatch.StartNew();

        // getWindowFunc is a WinUI-only concern; null is correct on CodeBrix.Platform.
        public SpinningTriangle() : base(null) { }

        protected override unsafe void Init(GL gl)
        {
            program = BuildProgram(gl);
            transformLocation = gl.GetUniformLocation(program, "uTransform");

            float[] vertices =
            {   //   x      y      r  g  b
                -0.6f, -0.5f,  1, 0, 0,
                 0.6f, -0.5f,  0, 1, 0,
                 0.0f,  0.7f,  0, 0, 1,
            };
            uint[] indices = { 0, 1, 2 };

            vertexArray = gl.GenVertexArray();
            gl.BindVertexArray(vertexArray);

            vertexBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertexBuffer);
            gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

            const uint stride = 5 * sizeof(float);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*) 0);
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride,
                (void*) (2 * sizeof(float)));

            indexBuffer = gl.GenBuffer();
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indexBuffer);
            gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

            gl.BindVertexArray(0);
        }

        protected override unsafe void RenderOverride(GL gl)
        {
            gl.ClearColor(0.09f, 0.10f, 0.13f, 1f);
            gl.Clear((uint) ClearBufferMask.ColorBufferBit);

            var angle = (float) (clock.Elapsed.TotalSeconds * 1.5);
            var width = (float) Math.Max(1, RenderSize.Width);
            var height = (float) Math.Max(1, RenderSize.Height);
            // Keep the triangle's aspect ratio regardless of the element's shape.
            var transform = Matrix4x4.CreateRotationZ(angle)
                          * Matrix4x4.CreateScale(height / width, 1f, 1f);

            gl.UseProgram(program);
            gl.UniformMatrix4(transformLocation, 1, false, (float*) &transform);
            gl.BindVertexArray(vertexArray);
            gl.DrawElements(PrimitiveType.Triangles, 3, DrawElementsType.UnsignedInt, (void*) 0);

            // Restore shared state before returning.
            gl.BindVertexArray(0);
            gl.UseProgram(0);

            Invalidate();   // request the next frame -> continuous animation
        }

        protected override void OnDestroy(GL gl)
        {
            gl.DeleteVertexArray(vertexArray);
            gl.DeleteBuffer(vertexBuffer);
            gl.DeleteBuffer(indexBuffer);
            if (program != 0)
                gl.DeleteProgram(program);
            program = 0;
        }

        static uint BuildProgram(GL gl)
        {
            var vertex = Compile(gl, ShaderType.VertexShader, VertexShaderSource);
            var fragment = Compile(gl, ShaderType.FragmentShader, FragmentShaderSource);

            var handle = gl.CreateProgram();
            gl.AttachShader(handle, vertex);
            gl.AttachShader(handle, fragment);
            gl.LinkProgram(handle);
            gl.GetProgram(handle, ProgramPropertyARB.LinkStatus, out var linked);
            if (linked == 0)
                throw new InvalidOperationException($"Shader link failed: {gl.GetProgramInfoLog(handle)}");

            gl.DetachShader(handle, vertex);
            gl.DetachShader(handle, fragment);
            gl.DeleteShader(vertex);
            gl.DeleteShader(fragment);
            return handle;
        }

        static uint Compile(GL gl, ShaderType type, string source)
        {
            var shader = gl.CreateShader(type);
            gl.ShaderSource(shader, source);
            gl.CompileShader(shader);
            gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);
            if (compiled == 0)
                throw new InvalidOperationException($"{type} compile failed: {gl.GetShaderInfoLog(shader)}");
            return shader;
        }
    }

(The project needs <AllowUnsafeBlocks>true</AllowUnsafeBlocks> for the
pointer arguments.) A throwing Compile/BuildProgram is the right design: the
element catches it, records InitializationFailed with the driver's message,
and your page can show it (example 2).

MainPage.xaml:

    <Page x:Class="MyApp.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:render="using:MyApp.Rendering">
        <Grid>
            <render:SpinningTriangle x:Name="Triangle" />
            <!-- XAML on top of the GL content: the element is a Grid -->
            <TextBlock Text="OpenGL" Margin="8" VerticalAlignment="Top"
                       IsHitTestVisible="False" />
            <TextBlock x:Name="Status" Foreground="OrangeRed" Margin="8"
                       VerticalAlignment="Bottom" TextWrapping="Wrap" />
        </Grid>
    </Page>

2. Detecting failure (and pointing Windows users at the Compatibility Pack)
--------------------------------------------------------------------------
The context is created when the element LOADS, so query the state after it
is loaded — in its Loaded handler, or once the hosting view is visible —
never in the constructor, where Status is still NotYetInitialized.

    // MainPage.xaml.cs
    Triangle.Loaded += (_, _) =>
    {
        var state = Triangle.GetGLInitializationState();
        if (state.Status == GLInitializationStatus.InitializationFailed)
        {
            var msg = "3D rendering is unavailable.\n\n" + state.FailedReason;
            if (OperatingSystem.IsWindows())
            {
                msg += "\n\nOn Windows you may need Microsoft's free "
                    + "\"OpenCL and OpenGL Compatibility Pack\":\n"
                    + "https://apps.microsoft.com/detail/9NQPSL29BFFF";
            }
            Status.Text = msg;   // or a dialog
        }
    };

Because the element's own Loaded handler runs first, the state is already
final when yours runs.

3. GPU Skia in a page: SkiaGLCanvasElement
------------------------------------------
    using CodeBrix.Platform.WinUI.Graphics3DGL;
    using SkiaSharp;

    // code-behind: create it and add it to a Grid named Host
    var gpuCanvas = new SkiaGLCanvasElement();
    var paint = new SKPaint { Color = SKColors.DeepSkyBlue, IsAntialias = true };
    var sw = System.Diagnostics.Stopwatch.StartNew();

    gpuCanvas.PaintSurface += (s, e) =>
    {
        var canvas = e.Surface.Canvas;          // GPU-backed; GL context is current
        canvas.Clear(SKColors.Black);
        var t = (float) sw.Elapsed.TotalSeconds;
        var cx = e.Info.Width / 2f + MathF.Cos(t) * e.Info.Width / 4f;
        var cy = e.Info.Height / 2f + MathF.Sin(t) * e.Info.Height / 4f;
        canvas.DrawCircle(cx, cy, 40, paint);
        gpuCanvas.Invalidate();                  // animate
    };
    gpuCanvas.Loaded += (_, _) =>
    {
        if (gpuCanvas.IsGpuInitialized == false)
            Status.Text = "GPU Skia is unavailable on this device.";
    };
    Host.Children.Add(gpuCanvas);

e.Info.Width/Height are the element's RenderSize (DIPs); draw in those units.

4. Off-screen GPU Skia with a CPU fallback: SkiaGpuContext
----------------------------------------------------------
    using CodeBrix.Platform.WinUI.Graphics3DGL;
    using SkiaSharp;

    // Call on the UI thread once the page is loaded (needs a XamlRoot).
    SKImage RenderThumbnail(int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        if (SkiaGpuContext.TryCreate(XamlRoot, out var gpu))
        {
            using (gpu)
            using (gpu.BeginFrame())                       // GL current / Metal no-op
            using (var surface = SKSurface.Create(gpu.GrContext, true, info))
            {
                Draw(surface.Canvas);
                surface.Flush();
                return surface.Snapshot().ToRasterImage(); // copy off the GPU before Dispose
            }
        }

        using (var surface = SKSurface.Create(info))        // CPU fallback
        {
            Draw(surface.Canvas);
            return surface.Snapshot();
        }
    }

5. Raw OpenGL without an element: OffscreenGLContext
----------------------------------------------------
    if (OffscreenGLContext.TryCreate(XamlRoot, out var ctx))
    {
        using (ctx)
        {
            using (ctx.MakeCurrent())
            {
                var gl = ctx.Gl;
                // create your own FBO, render, gl.ReadPixels(...) into your buffer
            }
        }
    }
    // Or build a GRContext on it yourself: var gr = ctx.CreateGrContext();
    // (dispose gr inside a MakeCurrent scope, before ctx.Dispose()).

MINIMUM VIABLE PROJECT
======================
.Core csproj additions:

    <PropertyGroup>
      <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
      <PackageReference Include="CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever" />
    </ItemGroup>

Then SpinningTriangle.cs and MainPage.xaml/.cs from examples 1 and 2. No
head project changes: each head registers its native OpenGL wrapper when its
host starts. (See the core package's AGENT-README for the head/bootstrap
scaffold.)

PERFORMANCE TIPS
================
  - Every frame of GLCanvasElement / SkiaGLCanvasElement ends with a GPU ->
    CPU read-back of RenderSize pixels plus a bitmap present. Cost scales
    with element area: keep GL elements as small as the design allows, and
    do not animate several large ones at once.
  - Invalidate() produces exactly one frame. For animation, call it at the
    end of RenderOverride / the PaintSurface handler; for static content,
    call it only when state changes — an idle element costs nothing.
  - Do GL resource creation in Init, not in RenderOverride. Init runs again
    only after an unload/reload.
  - The context is shared with the head's renderer on the UI thread: leave
    GL state as you found it, or the whole window can render incorrectly.
  - Prefer SkiaGLCanvasElement over hand-written GL for 2D effects: you get
    the GPU with SkiaSharp's API and a single read-back copy.
  - For work that never needs to be on screen (thumbnails, batch
    rasterization), use SkiaGpuContext / OffscreenGLContext and skip the
    element entirely.
  - Resizing rebuilds the framebuffer and back buffer; avoid continuously
    animating the element's size.

COMMON PITFALLS TO AVOID
========================
  - Querying GetGLInitializationState() or IsGLInitialized in the
    constructor or before Loaded: the status is NotYetInitialized / null
    there. Query in (or after) Loaded.
  - Not restoring GL state (bound VAO, program, depth test, blend, viewport)
    at the end of RenderOverride: the head's own Skia rendering shares the
    context and will glitch.
  - Expecting a top-left origin from GLCanvasElement: OpenGL renders
    bottom-up; the element flips the presented image for you (ScaleY = -1
    on its Background brush). Do not flip again in your projection.
  - Letting exceptions "escape": a throwing Init/RenderOverride is caught
    and turned into InitializationFailed; the element then stays blank and
    silent until reloaded. Surface state.FailedReason to the user.
  - Zero-size element: with no bounded size (Height, MinHeight, star row)
    nothing is rendered. Give it a real size.
  - Windows-on-ARM without an OpenGL ICD: blank surface. Detect it
    (example 2) and point the user at the Compatibility Pack.
  - Frame Buffer head on a GPU-less machine without libegl1 +
    libgl1-mesa-dri: context creation fails; install those packages.
  - Mixing up pixel units: GL viewport / SkiaGLCanvasElement surface are
    RenderSize (DIPs) — the same units as ActualWidth/ActualHeight — not
    physical pixels.
  - Using OffscreenGLContext.Gl or a GRContext outside a MakeCurrent /
    BeginFrame scope, or from another thread: undefined results. Dispose the
    GRContext before the context.
  - Giving GLCanvasElement a non-null getWindowFunc "just in case": it is
    ignored on CodeBrix.Platform; null is the documented value.
  - Referencing Silk.NET.OpenGL directly: the GL type in these signatures is
    CodeBrix.Platform.OpenGL.GL, from the OpenGL package that comes in
    automatically. Do not add another GL binding.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - It does not create a native child window or an on-screen swap chain;
    everything is off-screen + read-back. There is no zero-copy present path.
  - It does not provide Vulkan, Direct3D or Metal APIs to your code
    (SkiaGpuContext uses Metal internally on macOS, but hands you only a
    GRContext).
  - It does not load models, textures or images, and has no scene graph,
    camera or math helpers — bring your own (System.Numerics, your loader).
  - It does not manage a render loop or vsync; you drive frames with
    Invalidate().
  - It does not render at physical-pixel resolution on scaled displays; the
    surface is RenderSize (DIPs) and is upscaled by the compositor.
  - It does not work in a unit-test host or on a head without a native
    OpenGL context: TryCreate returns false, elements report
    InitializationFailed / IsGpuInitialized == false.
  - It is not the CPU-drawing element: for plain 2D Skia use SKCanvasElement
    (CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever) or SKXamlCanvas
    (CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever).

WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/EmulateFrameBufferDemo
      src/EmulateFrameBufferDemo.Core/Rendering/ModelViewerCanvas.cs — a
      complete GLCanvasElement subclass: glTF model loading, shader
      compile/link with error reporting, VAO/VBO/EBO + texture upload in
      Init, a turntable render loop (Invalidate at the end of
      RenderOverride), and cleanup in OnDestroy.
      src/EmulateFrameBufferDemo.UI/Views/MainPage.xaml — hosts that element
      (<render:ModelViewerCanvas x:Name="ModelView" />) in a Grid with XAML
      overlaid on top, next to an SKXamlCanvas pane.

QUICK REFERENCE CARD
====================
    Package:    CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever  (.Core, once)
                pulls in CodeBrix.Platform.OpenGL.MitLicenseForever (GL binding)
    Namespace:  CodeBrix.Platform.WinUI.Graphics3DGL   (GL types: CodeBrix.Platform.OpenGL)
    Needs:      OpenGL 3.0+ from the head (all six heads; Windows needs an ICD)

    GLCanvasElement : Grid (abstract)          subclass; ctor base(null)
      Init(GL) / RenderOverride(GL) / OnDestroy(GL)   protected abstract
      Invalidate()                              one frame; call at end of Render to animate
      IsGLInitialized : bool?                   null / true / false (read-only DP)
      GetGLInitializationState()                .Status, .FailedReason (after Loaded!)
    GLInitializationStatus                      NotYetInitialized, Initializing,
                                                Initialized, InitializationFailed
    SkiaGLCanvasElement : Grid                  new SkiaGLCanvasElement()
      PaintSurface (SkiaGLPaintSurfaceEventArgs: Surface, Context, Info)
      IsGpuInitialized : bool?   Invalidate()
    OffscreenGLContext                          TryCreate(XamlRoot, out ctx); Gl;
                                                MakeCurrent(); GetProcAddress(name);
                                                CreateGrContext(); Dispose()
    SkiaGpuContext                              TryCreate(XamlRoot, out ctx); GrContext;
                                                Backend (OpenGL|Metal); BeginFrame(); Dispose()
    Units:      RenderSize (DIPs) everywhere
    Siblings:   SKCanvasElement -> CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever
                SKXamlCanvas    -> CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever
