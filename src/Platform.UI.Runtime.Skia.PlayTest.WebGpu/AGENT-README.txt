================================================================================
AGENT-README: CodeBrix.Platform.PlayTest.WebGpu
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever gives the PlayTest test
head (CodeBrix.Platform.PlayTest.ApacheLicenseForever) real WebGPU devices, so
an application that uses the WebGpu add-in
(CodeBrix.Platform.WebGpu.ApacheLicenseForever) renders its WebGPU content in
PlayTests on a real GPU device:

    WebGpuRenderView      gets a device (DeviceReady), renders every frame
                          (Render, or its IWebGpuRenderer) and draws its
                          overlay; its pixels appear in screenshots
    RenderPath            Readback - the view's own device, one read-back per
                          frame (PlayTest offers no window device, so never a
                          direct path)

It serves applications that use the WebGpu add-in; that add-in is its one
package dependency. Without this package a PlayTest run has no WebGPU at all.

One line in the test fixture switches it on:

    CodeBrixPlayTestWebGpu.Register();
    Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
    {
        ConfigurationAssembly = typeof(AppFixture).Assembly,
    });

How each operating system gets its device (nothing to choose, nothing to set):
the WebGpu add-in's own route, the one a WebGpuRenderView takes on a desktop
head.

    Linux     Vulkan - the GPU's driver, or a software Vulkan driver (lavapipe)
              when that is all the machine has. No display, window or desktop
              session is needed.
    Windows   Direct3D 12, then Vulkan.
    macOS     Metal.

INSTALLATION
============
PackageId:   CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever
License:     Apache-2.0
Assembly:    CodeBrix.Platform.PlayTest.WebGpu
Namespace:   CodeBrix.Platform.PlayTest.WebGpu

    dotnet add package CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever

Reference it from the PlayTests project, BESIDE
CodeBrix.Platform.PlayTest.ApacheLicenseForever (it plugs into that package and
does not bring it), at the same family version. Never reference it from an
application or a head project. NuGet dependency (pulled in automatically):
  - CodeBrix.Platform.WebGpu.ApacheLicenseForever   (same family version;
    brings CodeBrix.WebGpu.Skia and CodeBrix.WebGpu.Native - the WebGPU
    engine's native library for every runtime identifier)

System requirements, per operating system:

  Linux     A Vulkan driver. On Debian:
                apt install mesa-vulkan-drivers
            (the GPU's Mesa driver, and lavapipe as the software fallback).
            No GPU, display server or desktop session is needed when lavapipe
            is installed.
  Windows   A GPU driver with Direct3D 12 or Vulkan support.
  macOS     A Mac with a Metal device. There is no software fallback.

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.PlayTest;          // PlayTestApplication, PlayTestWebGpu,
                                               // PlayTestWebGpuInfo, PixelStats
    using CodeBrix.Platform.PlayTest.WebGpu;   // CodeBrixPlayTestWebGpu

Inside a namespace that starts with CodeBrix.Platform.PlayTest.WebGpu (or any
other *.WebGpu namespace), the identifier WebGpu is a NAMESPACE: alias the
WebGPU binding's class (using Wgpu = CodeBrix.WebGpu.Native.WebGpu;).

CORE API REFERENCE
==================

CodeBrixPlayTestWebGpu  (static)
--------------------------------
    static void Register()
        Registers the provider with the PlayTest head. Call it once, before
        PlayTestApplication.LaunchAsync; further calls do nothing. No device is
        created here. Throws PlatformNotSupportedException on an operating
        system other than Linux, Windows and macOS.
    static bool IsRegistered

Everything else - switching WebGPU off for a launch, the information the launch
reports, measuring WebGPU pixels - is in the PlayTest head package; its
AGENT-README has the full WEBGPU section. In short:

    PlayTestOptions.WebGpu = PlayTestWebGpu.Unavailable
        a launch with no WebGPU, to test the application's fallback: every
        WebGpuRenderView raises DeviceLost once with "PlayTestOptions.WebGpu is
        Unavailable for this launch." and its RenderPath stays None
    PlayTestApplication.WebGpu                 (PlayTestWebGpuInfo)
        IsAvailable, Provider, AdapterName, BackendName, DeviceDescription,
        IsSoftware, UnavailableReason
    PixelStats.FromPng(await locator.ScreenshotAsync(...))
        coverage, bounds, centroid, halves, luminance, variance, symmetry and
        the difference between two captures taken in the same test

What the launch probe does: it creates a device exactly as a WebGpuRenderView
does, clears a small off-screen target of the view's format (BGRA8Unorm) and
reads it back, records the adapter's name, backend and driver description, and
releases that device again. Each view then creates its own device on its first
frame, as on a desktop head; nothing of the probe is kept.

FAILURES ARE LOUD
=================
  - Registered, but this machine cannot create a device: LaunchAsync throws a
    PlayTestException that names the provider and the engine's reason, e.g.
        The WebGPU provider "CodeBrix.Platform.PlayTest.WebGpu (the WebGpu
        add-in's own device, Vulkan)" could not create a device on Linux ...:
        No WebGPU device could be created: Vulkan: no adapter (...). PlayTests
        with WebGPU on Linux need a Vulkan driver; on Debian: apt install
        mesa-vulkan-drivers
    A device that exists but cannot clear and read back a small target fails
    the launch the same way ("the device does not render").
  - NOT registered, and the application initializes a WebGpuRenderView: the
    test fails, naming this package and CodeBrixPlayTestWebGpu.Register() (see
    the PlayTest AGENT-README, WEBGPU).
Nothing is ever skipped.

COMPLETE EXAMPLE
================
    using System.Threading.Tasks;
    using CodeBrix.Platform.PlayTest;
    using CodeBrix.Platform.PlayTest.WebGpu;
    using CodeBrix.Platform.UI.WebGpu;        // WebGpuRenderPath
    using Windows.UI;
    using Xunit;

    public sealed class AppFixture : IAsyncLifetime
    {
        public PlayTestApplication Application { get; private set; }

        public async ValueTask InitializeAsync()
        {
            CodeBrixPlayTestWebGpu.Register();
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

    // In a test: the view clears to a known colour and draws a triangle over it.
    var clear = Color.FromArgb(255, 16, 24, 48);
    await Application.WaitForAsync(() => view.RenderPath, path => path == WebGpuRenderPath.Readback);
    var viewport = PixelStats.FromPng(await Page.GetByTestId("Viewport").ScreenshotAsync());
    Assert.False(viewport.IsBlank);
    Assert.InRange(1 - viewport.Coverage(clear), 0.1, 0.6);   // the triangle's share

COMMON PITFALLS TO AVOID
========================
  - Registering after LaunchAsync: the launch has already decided there is no
    provider. Register first.
  - Referencing this package without CodeBrix.Platform.PlayTest: it does not
    bring the head.
  - Screenshots with Stable = true of a view under the Continuous driver: an
    animated picture never settles. Pause the view (IsPaused), switch it to
    OnDemand, or capture without Stable and assert structure.
  - Expecting DirectVulkan or DirectMetal: PlayTest renders in software and
    offers no window device, so RenderPath is always Readback (or None without
    a device). Prove direct paths in a live run on a desktop head.
  - Expecting the same adapter everywhere: Linux may render on the GPU or on
    lavapipe, Windows on Direct3D 12 or Vulkan, macOS on Metal. Assert
    structure (coverage, bounds, change after input) with tolerances, never
    exact pixel values; PlayTestApplication.WebGpu.AdapterName and IsSoftware
    say what rendered.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No direct GPU paths: views run Readback under PlayTest.
  - No software fallback of its own: a machine without a WebGPU adapter fails
    the launch with the reason (on Linux, lavapipe is such an adapter when it
    is installed).
  - No comparison with saved images, ever: WebGPU content is asserted with
    PixelStats in the running test and documented with screenshots.

WORKING EXAMPLES ON GITHUB
==========================
  - WebGpuDemo and its tests: a raw WebGPU triangle, glTF models through
    CodeBrix.WebGpu.Graphics, an IWebGpuRenderer class and a HUD with the frame
    drivers, measured with PixelStats; and the WebGpuDemo.NoWebGpu.PlayTests
    project that launches without WebGPU:
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/WebGpuDemo

QUICK REFERENCE CARD
====================
Package     CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever (PlayTests projects only)
Using       using CodeBrix.Platform.PlayTest.WebGpu;
Register    CodeBrixPlayTestWebGpu.Register();   // before LaunchAsync
Opt out     new PlayTestOptions { WebGpu = PlayTestWebGpu.Unavailable }
Info        app.WebGpu.IsAvailable / Provider / AdapterName / BackendName / DeviceDescription / IsSoftware
Path        WebGpuRenderView.RenderPath == Readback under PlayTest
Measure     PixelStats.FromPng(await locator.ScreenshotAsync())
Linux       apt install mesa-vulkan-drivers
Windows     GPU driver with Direct3D 12 or Vulkan
macOS       Metal device
