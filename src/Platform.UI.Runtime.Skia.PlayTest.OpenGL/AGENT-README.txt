================================================================================
AGENT-README: CodeBrix.Platform.PlayTest.OpenGL
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever gives the PlayTest test
head (CodeBrix.Platform.PlayTest.ApacheLicenseForever) real OpenGL contexts, so
an application that uses the Graphics3DGL add-in
(CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever) renders its OpenGL content
in PlayTests exactly as it does on a desktop head:

    GLCanvasElement       draws, and its pixels appear in screenshots
    SkiaGLCanvasElement   IsGpuInitialized is true; GPU Skia pixels appear too
    OffscreenGLContext    TryCreate succeeds
    SkiaGpuContext        TryCreate succeeds (e.g. the VideoPlayer add-in's GPU path)

It serves applications that use Graphics3DGL; that add-in is its one package
dependency. Without this package a PlayTest run has no OpenGL at all.

One line in the test fixture switches it on:

    CodeBrixPlayTestOpenGL.Register();
    Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
    {
        ConfigurationAssembly = typeof(AppFixture).Assembly,
    });

How each operating system gets its context (nothing to choose, nothing to set):

    Linux     the system Mesa EGL on its surfaceless platform - no X11, no
              Wayland, no window. Mesa uses the GPU's render node when there is
              one and the llvmpipe software rasterizer otherwise. OpenGL ES.
    Windows   WGL on opengl32.dll, on a hidden window - the Win32 head's path
              (the GPU vendor's OpenGL driver). Desktop OpenGL.
    macOS     ANGLE on Metal, through the libEGL.dylib / libGLESv2.dylib that
              the Graphics3DGL package ships - the macOS head's path. OpenGL ES.

Vulkan is not supported.

INSTALLATION
============
PackageId:   CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever
License:     Apache-2.0
Assembly:    CodeBrix.Platform.PlayTest.OpenGL
Namespace:   CodeBrix.Platform.PlayTest.OpenGL

    dotnet add package CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever

Reference it from the PlayTests project, BESIDE
CodeBrix.Platform.PlayTest.ApacheLicenseForever (it plugs into that package and
does not bring it), at the same family version. Never reference it from an
application or a head project. NuGet dependency (pulled in automatically):
  - CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever   (same family version;
    brings the GL binding CodeBrix.Platform.OpenGL and the macOS ANGLE libraries)

System requirements, per operating system:

  Linux     Mesa's EGL and DRI packages. On Debian:
                apt install libegl1 libgl1-mesa-dri
            No GPU, display server or desktop session is needed.
  Windows   An interactive desktop session with a GPU OpenGL driver. On Windows
            on ARM, Microsoft's "OpenCL and OpenGL Compatibility Pack" (Microsoft
            Store). Microsoft's built-in "GDI Generic" OpenGL 1.1 is too old: the
            launch fails with that reason.
  macOS     A Mac with a Metal device. ANGLE comes with Graphics3DGL; nothing to
            install. There is no software fallback.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.PlayTest;          // PlayTestApplication, PlayTestOpenGL,
                                               // PlayTestOpenGLInfo, PixelStats
    using CodeBrix.Platform.PlayTest.OpenGL;   // CodeBrixPlayTestOpenGL

CORE API REFERENCE
==================

CodeBrixPlayTestOpenGL  (static)
--------------------------------
    static void Register()
        Registers this operating system's provider with the PlayTest head. Call it
        once, before PlayTestApplication.LaunchAsync; further calls do nothing.
        Nothing native is loaded here. Throws PlatformNotSupportedException on an
        operating system other than Linux, Windows and macOS.
    static bool IsRegistered

Everything else - switching OpenGL off for a launch, the information the launch
reports, measuring OpenGL pixels - is in the PlayTest head package; its
AGENT-README has the full OPENGL section. In short:

    PlayTestOptions.OpenGL = PlayTestOpenGL.Unavailable
        a launch with no OpenGL, to test the application's fallback
    PlayTestApplication.OpenGL                 (PlayTestOpenGLInfo)
        IsAvailable, Provider, Renderer, Version, IsGles, IsSoftware,
        UnavailableReason
    PixelStats.FromPng(await locator.ScreenshotAsync(...))
        coverage, bounds, centroid, halves, luminance, variance, symmetry and
        the difference between two captures taken in the same test

FAILURES ARE LOUD
=================
  - Registered, but this machine cannot create a context: LaunchAsync throws a
    PlayTestException that names the provider and the concrete reason, e.g.
        The OpenGL provider "CodeBrix.Platform.PlayTest.OpenGL (Mesa EGL,
        surfaceless platform)" could not create a context on Linux ...:
        libEGL.so.1 could not be loaded (...) - PlayTests with OpenGL need the
        Mesa EGL stack; on Debian: apt install libegl1 libgl1-mesa-dri
    Every new context is checked before it is handed out: it must report OpenGL
    (ES) 3.0 or later (the Graphics3DGL elements' floor; ANGLE is exempt, as in
    the add-in) and clearing a small framebuffer object must read back the clear
    colour.
  - NOT registered, and the application initializes an OpenGL element: the test
    fails, naming this package and CodeBrixPlayTestOpenGL.Register() (see the
    PlayTest AGENT-README, OPENGL).
Nothing is ever skipped.

COMPLETE EXAMPLE
================
    using System.Threading.Tasks;
    using CodeBrix.Platform.PlayTest;
    using CodeBrix.Platform.PlayTest.OpenGL;
    using Windows.UI;
    using Xunit;

    public sealed class AppFixture : IAsyncLifetime
    {
        public PlayTestApplication Application { get; private set; }

        public async ValueTask InitializeAsync()
        {
            CodeBrixPlayTestOpenGL.Register();
            Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
            {
                ConfigurationAssembly = typeof(AppFixture).Assembly,
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (Application != null) await Application.DisposeAsync();
        }
    }

    // In a test:
    var canvas = PixelStats.FromPng(await Page.GetByTestId("Viewport").ScreenshotAsync(new() { Stable = true }));
    var background = Color.FromArgb(255, 26, 26, 26);
    Assert.False(canvas.IsBlank);
    Assert.InRange(1 - canvas.Coverage(background), 0.2, 0.3);   // the model fills about a quarter
    Assert.True(canvas.Half(PixelHalf.Left).MeanLuminance(background)
              > canvas.Half(PixelHalf.Right).MeanLuminance(background));   // lit from the left

COMMON PITFALLS TO AVOID
========================
  - Registering after LaunchAsync: the launch has already decided there is no
    provider. Register first.
  - Referencing this package without CodeBrix.Platform.PlayTest: it does not
    bring the head.
  - Expecting the same renderer everywhere: Linux may render on llvmpipe or on
    the GPU, Windows on the GPU driver, macOS on Metal. Assert structure
    (coverage, bounds, lighting direction, change after input) with tolerances,
    never exact pixel values; PlayTestApplication.OpenGL.Renderer and
    IsSoftware say what rendered.
  - Running on a Windows service account, a headless Windows runner or a Mac
    without Metal: there is no OpenGL there, and the launch says so.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No Vulkan and no Metal surfaces of its own (Metal is used only underneath
    ANGLE on macOS).
  - No software fallback on Windows or macOS.
  - No comparison with saved images, ever: GL content is asserted with
    PixelStats in the running test and documented with screenshots.

WORKING EXAMPLES ON GITHUB
==========================
  - PlayTestDemo's OpenGL page and its tests (a lit square drawn with raw
    OpenGL, a circle drawn with GPU Skia, a rotate button), and the
    PlayTestDemo.NoOpenGL.PlayTests project that launches without OpenGL:
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/PlayTestDemo

QUICK REFERENCE CARD
====================
Package     CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever (PlayTests projects only)
Using       using CodeBrix.Platform.PlayTest.OpenGL;
Register    CodeBrixPlayTestOpenGL.Register();   // before LaunchAsync
Opt out     new PlayTestOptions { OpenGL = PlayTestOpenGL.Unavailable }
Info        app.OpenGL.IsAvailable / Provider / Renderer / Version / IsGles / IsSoftware
Measure     PixelStats.FromPng(await locator.ScreenshotAsync(new() { Stable = true }))
Linux       apt install libegl1 libgl1-mesa-dri
Windows     interactive desktop + GPU OpenGL driver (ARM: Compatibility Pack)
macOS       Metal device; ANGLE comes with Graphics3DGL
