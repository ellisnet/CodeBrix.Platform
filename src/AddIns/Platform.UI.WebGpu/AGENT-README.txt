================================================================================
AGENT-README: CodeBrix.Platform.WebGpu
A Guide for AI Coding Agents — CONSUMING the CodeBrix.Platform.WebGpu.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
WebGPU rendering inside a CodeBrix.Platform page, delivered as one
XAML-declarable element: WebGpuRenderView. Put it in a Grid cell, then either
handle its Render event or give it an IWebGpuRenderer, and record WebGPU
commands - the raw CodeBrix.WebGpu.Native API - into the frame it hands you.
Target: .NET 10 or later. Live on every Skia head (Windows Win32-Skia, Windows
WPF-Skia, Linux X11, Linux Wayland, Linux FrameBuffer, macOS) and under the
PlayTest head.

The public types, all in one namespace (CodeBrix.Platform.UI.WebGpu):

  WebGpuRenderView    A [Bindable] FrameworkElement that owns a WebGPU device,
                      renders frames into a texture the size of the element and
                      shows them. It is also an IWebGpuDeviceSource and an
                      IWebGpuTextureTarget (the CodeBrix.WebGpu sharing
                      contracts), and it hands itself to native modules.
  IWebGpuRenderer     The class-shaped alternative to the view's events.
  WebGpuFrame         One open frame: the command encoder, the texture, its
                      size and format, the frame number and the timing.
  WebGpuFrameDriver   Continuous (default), OnDemand, Manual.
  WebGpuRenderPath    None, Readback, DirectVulkan, DirectMetal, DirectSurface.
  WebGpuDeviceInfo    What the view's device is: adapter, backend, the features
                      and limits the device was created with, and the ones its
                      adapter offers.
  WebGpuRenderViewHandoff
                      The view as a native module sees it (C-compatible).
  WebGpuDeviceReadyEventArgs, WebGpuRenderEventArgs, WebGpuOverlayEventArgs,
  WebGpuDeviceLostEventArgs, WebGpuRenderPathChangedEventArgs
                      The event payloads.

Android: the same element exists in CodeBrix.Android (package
CodeBrix.Android.WebGpu.ApacheLicenseForever) - the same WebGpuRenderView, in
the same namespace, with the same API, so XAML pages, renderers and native
modules move between the two unchanged. Its direct path there is DirectSurface
(see RENDER PATHS); that package's own AGENT-README has the Android specifics.

The view never wraps WebGPU: pipelines, buffers, shaders and passes are the
CodeBrix.WebGpu.Native binding, exactly as in C. Read that package's
AGENT-README (inside its package) for the API itself, its async pattern, its
errors and its LIMITS. A higher layer (CodeBrix.WebGpu.Graphics) is optional and
composes with the view through the sharing contracts - see SHARING THE DEVICE.


INSTALLATION
============
    dotnet add package CodeBrix.Platform.WebGpu.ApacheLicenseForever

Reference it from the project that carries your framework package references
(the application's .Core project in the standard CodeBrix.Platform layout); the
XAML in the shared .UI project then resolves the webGpu: namespace.

Recording WebGPU commands takes pointers to descriptor structs, so the project
that writes your renderer needs:

    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>

Dependencies of this package (flow in automatically, no separate install):
  CodeBrix.Platform.ApacheLicenseForever        the core framework
  CodeBrix.WebGpu.Skia.MitLicenseForever        the device and the off-screen
                                                target the view renders into;
                                                brings
  CodeBrix.WebGpu.Native.MitLicenseForever      the raw WebGPU binding, the
                                                sharing contracts, the
                                                native-module hand-off structs
                                                and the WebGPU engine's native
                                                library for every runtime
                                                identifier
  SkiaSharp                                     the framework's own pin (the
                                                overlay canvas)

Nothing else: CodeBrix.WebGpu.Graphics is NOT a dependency (add it to your own
project if you want it). A GPU driver is still needed at run time (Vulkan on
Linux, Direct3D 12 or Vulkan on Windows, Metal on macOS); a machine without one
reports it through DeviceLost and the view shows what is behind it.

License: Apache-2.0 (the WebGpu packages are MIT; the engine's notices travel
with them).


KEY NAMESPACES / USINGS
=======================
    xmlns:webGpu="using:CodeBrix.Platform.UI.WebGpu"         (XAML)
    using CodeBrix.Platform.UI.WebGpu;                        (C#: the view)
    using CodeBrix.WebGpu.Native;                             (C#: WebGpu, handles, structs)
    using SkiaSharp;                                          (C#: the overlay)

Name clashes to expect once CodeBrix.WebGpu.Native is imported:
  - LogLevel exists in CodeBrix.WebGpu.Native AND Microsoft.Extensions.Logging:
        using LogLevel = Microsoft.Extensions.Logging.LogLevel;
  - Color exists in CodeBrix.WebGpu.Native (the clear-value struct) AND
    Windows.UI (ClearColor's type). Alias one of them.
  - Buffer collides with System.Buffer:
        using GpuBuffer = CodeBrix.WebGpu.Native.Buffer;
  - Inside a namespace that starts with CodeBrix.WebGpu, the identifier WebGpu is
    the NAMESPACE; write CodeBrix.WebGpu.Native.WebGpu.Foo(...) there, or alias
    the class (using Wgpu = CodeBrix.WebGpu.Native.WebGpu;).


CORE API REFERENCE
==================

WebGpuRenderView : FrameworkElement, IWebGpuDeviceSource, IWebGpuTextureTarget
------------------------------------------------------------------------------
Dependency properties (XAML-settable):
  WebGpuFrameDriver FrameDriver   Continuous (default) | OnDemand | Manual.
  double TargetFrameRate          Continuous rate, default 60. 0 = as fast as the
                                  UI thread allows (a new frame as soon as the
                                  previous one is presented; no timer, not tied
                                  to the display refresh). Negative, NaN or
                                  infinite throws ArgumentOutOfRangeException
                                  and keeps the old value.
  bool IsPaused                   Pauses Continuous (the last frame stays up).
  IWebGpuRenderer? Renderer       Called before each matching event.
  Stretch Stretch                 Default Fill (the texture IS the element's
                                  size); the others matter only between a
                                  resize and the next frame.
  Windows.UI.Color ClearColor     Default opaque black. Every frame's texture is
                                  cleared to it before the renderer draws; a
                                  transparent colour lets what is behind the view
                                  show through.
  WebGpuSkiaDeviceOptions? DeviceOptions
                                  How the view's OWN device is created
                                  (CodeBrix.WebGpu.Skia: PreferredBackend,
                                  AllowFallbackBackend, PowerPreference,
                                  ForceFallbackAdapter). Null (default) = the
                                  engine's defaults. Consulted at every owned
                                  device creation. Set while the view owns a
                                  device: that device is replaced at the next
                                  frame (DeviceLost("Device options changed."),
                                  then DeviceReady). Assign a NEW object to apply
                                  a change. Does not affect an adopted device.
  bool RequireOwnedDevice         Default false. True: never adopt the head's
                                  device, even with UseDirectGpuRenderingMode()
                                  on - always the view's own device, created from
                                  DeviceOptions, on Readback. Changing it while a
                                  device exists with the latch on replaces the
                                  device at the next frame (same DeviceLost
                                  reason).

Read-only state:
  WebGpuRenderPath RenderPath     None until the first device; then how frames
                                  reach the screen (see RENDER PATHS).
  bool IsDeviceReady              Between DeviceReady and DeviceLost.
  TextureFormat PreferredFormat   BGRA8Unorm: the colour-target format every
                                  pipeline that draws into the view must use.
  double PixelScale               The window's rasterization scale.
  int TextureWidth, TextureHeight The pixel size the NEXT frame renders at
                                  (element size x PixelScale, rounded; 0 before
                                  layout; capped at 8192 per side).
  WebGpuDeviceInfo? DeviceInfo    What the current device is (null without one):
                                  set before DeviceReady, cleared at DeviceLost;
                                  the same on an owned and an adopted device.
                                  Any thread.

Events:
  DeviceReady(WebGpuDeviceReadyEventArgs: IWebGpuDeviceSource Device,
              WebGpuDeviceInfo DeviceInfo)
      A device was created: on the first frame after the view is loaded, and
      after a lost device is replaced. Check your requirements against
      DeviceInfo, then create pipelines and buffers here.
  Render(WebGpuRenderEventArgs: WebGpuFrame Frame)
      One per frame of the BUILT-IN drivers (Continuous, OnDemand), on the UI
      thread. Not raised under Manual (whoever opens the frame renders it).
      The args object is reused; keep nothing.
  Overlay(WebGpuOverlayEventArgs: SKCanvas Canvas, SKSize Size)
      Every paint of the view (during the head's paint, NOT on the frame's
      thread), over the frame, in effective pixels. The args object is reused;
      keep nothing.
  DeviceLost(WebGpuDeviceLostEventArgs: string Reason)
      The view has no usable device any more, or never got one: no adapter
      (the engine's reason), an error the device reported, the view unloaded,
      or a launch that said there is no device. Release device resources here;
      it is raised BEFORE the device is released.
  RenderPathChanged(WebGpuRenderPathChangedEventArgs: OldPath, NewPath)

Methods:
  WebGpuFrame? BeginFrame()       Opens a frame on the CALLING thread. Null when
                                  the view cannot render now (not loaded, no
                                  device, no size): skip, and do not call
                                  EndFrame. A second thread waits until the open
                                  frame ends. Throws InvalidOperationException
                                  when this thread already has a frame open.
  void EndFrame()                 Ends this thread's frame: validates and
                                  submits its commands, reads the picture back
                                  and shows it. Throws InvalidOperationException
                                  without an open frame on this thread, and
                                  WebGpuSkiaValidationException (namespace
                                  CodeBrix.WebGpu.Skia) when the commands fail
                                  WebGPU validation - nothing was submitted, the
                                  previous picture stays, the view stays usable.
  void Invalidate()               OnDemand: one frame soon (calls before it runs
                                  coalesce). No-op under Continuous and Manual.
                                  Any thread.
  CodebrixDeviceHandoff* GetDeviceHandoff()
  WebGpuRenderViewHandoff* GetViewHandoff()
                                  Native-module hand-offs (see NATIVE MODULES).

IWebGpuDeviceSource members (public): Instance, Adapter, Device, Queue, Backend,
  HasUncapturedErrorHandler (true: the view's own device reports errors to the
  view instead of ending the process). Null handles without a device. The view
  OWNS every handle; take your own reference (*AddRef) to keep one.

IWebGpuTextureTarget members: Texture, TextureView, Format (public) and Width,
  Height, SampleCount (1), Usage, IsInWebGpuPass, BeginWebGpuPass(),
  EndWebGpuPass() (explicit interface members - Width/Height on the element are
  its LAYOUT size). Texture and TextureView are valid ONLY between BeginFrame
  and EndFrame (null otherwise). BeginWebGpuPass() hands a library the OPEN
  frame's encoder (a null encoder outside a frame); EndWebGpuPass() closes that
  hand-out; the commands are submitted at EndFrame.

WebGpuDeviceInfo (sealed; one immutable instance per device)
--------------------------------------------------------------
  string AdapterName, AdapterVendor, AdapterDescription   (may be empty)
  AdapterType AdapterType; uint VendorId, DeviceId; BackendType Backend
  IReadOnlyList<FeatureName> Features            ENABLED on the device
  IReadOnlyList<NativeFeature> NativeFeatures    ENABLED engine-native features
  Limits Limits                                  the device's limits
  IReadOnlyList<FeatureName> AdapterFeatures     AVAILABLE on the adapter
  IReadOnlyList<NativeFeature> AdapterNativeFeatures
  Limits AdapterLimits                           the most a device could have
  bool HasFeature(FeatureName | NativeFeature)          enabled on the device
  bool AdapterHasFeature(FeatureName | NativeFeature)   offered by the adapter
All types are CodeBrix.WebGpu.Native's (Limits.NextInChain is always null).
Read through AdapterGetInfo, AdapterGetFeatures/Limits and DeviceGetFeatures/
Limits only (never DeviceGetAdapterInfo, which ends the process). See MODULE
CONTRACT for "enabled" versus "available".

IWebGpuRenderer
---------------
  void OnRender(WebGpuFrame frame);                     required
  void OnDeviceReady(IWebGpuDeviceSource device) { }    optional (default: nothing)
  void OnOverlay(SKCanvas canvas, SKSize size) { }      optional
  void OnDeviceLost(string reason) { }                  optional
The view calls the renderer FIRST, then raises the matching event.

WebGpuFrame : IDisposable (ONE instance per view, reused every frame)
---------------------------------------------------------------------
  CommandEncoder Encoder      record into it; never finish, submit or release it
  Texture Texture, TextureView TextureView, TextureFormat Format (BGRA8Unorm)
  int Width, Height           pixels
  long FrameIndex             0 for the first frame on a device, +1 per frame
  double ElapsedSeconds       since that first frame
  double DeltaSeconds         since the previous frame (0 for the first)
  bool IsOpen
  CodebrixFrameHandoff* GetHandoff()   the frame for a native module
  void Dispose()              == EndFrame() when open

WebGpuRenderViewHandoff (blittable, 40 bytes) - see NATIVE MODULES.


THE FRAME PROTOCOL AND THE THREADING CONTRACT
=============================================
  * A frame is BeginFrame ... EndFrame on ONE thread. The texture arrives
    already cleared to ClearColor: draw with LoadOp.Load to keep it, or
    LoadOp.Clear to clear again. Record only into frame.Encoder, close every
    pass you begin, and never finish, submit or release the encoder.
  * One frame at a time, from any thread: a second thread's BeginFrame waits
    until the open frame ends (WebGPU allows one thread on a device at a time;
    this is that rule enforced). The built-in drivers do not wait: a tick that
    finds a frame open on another thread is skipped.
  * The built-in drivers (Continuous, OnDemand) run frames on the UI thread.
    Manual lets the caller pick the thread - your own render thread included.
  * DeviceReady, Render, DeviceLost (and the renderer's matching calls) run on
    the thread that runs the frame; DeviceLost for an unloaded view runs on the
    UI thread. RenderPathChanged runs on the thread that changed the path.
  * The PAINT runs when the head paints the window - on the desktop heads that
    is the user-interface thread RECORDING the window's frame, which the head's
    render thread replays later - so it is not the frame's thread under Manual,
    and what it draws may be shown after the next frame has started. It only
    draws a finished, immutable picture. The OVERLAY runs inside that paint, so:
    never touch WebGPU objects in Overlay/OnOverlay, and read only state that is
    safe to read from another thread (copy what the HUD shows into a field from
    Render).
  * Your own device resources (buffers, pipelines) may be created and written
    (QueueWriteBuffer) on the frame's thread at any time; queue writes run at the
    start of the frame's submit.


DRIVERS
=======
  Continuous (default)  A frame per period of TargetFrameRate, timed from the
                        previous frame's DEADLINE, so the rate holds while frames
                        take part of a period. A frame that ends late makes the
                        next start at once and drops the missed ones: a slow
                        frame costs frame rate, never a backlog. IsPaused stops
                        it; the last picture stays.
  OnDemand              One frame per Invalidate() (coalesced), plus one when
                        the view is loaded and one when its pixel size changes.
  Manual                Nothing by itself. Call BeginFrame/EndFrame (from any
                        one thread at a time), or let a native module call the
                        hand-off's begin_frame/end_frame.
Switching drivers at run time is allowed; whatever the old driver had pending is
cancelled.


LIFETIME
========
  * Construction creates nothing on the GPU. The device and the target are
    created LAZILY, on the first frame after the view is loaded with a live
    XamlRoot (DeviceReady fires there, on the frame's thread).
  * Unloaded (leaving the visual tree) tears the device down: DeviceLost("The
    view was unloaded.") first, then the picture is dropped and the device
    released; RenderPath becomes None. Loading the view again re-arms the
    driver and the next frame creates a new device (a new DeviceReady). Release
    and re-create your device resources in those two callbacks.
  * A new pixel size (layout or rasterization scale) is a new target - new
    texture handles - at the next frame. While the size keeps changing (a window
    drag) the view re-blits the last picture instead of asking for repaints of
    every new frame; live repainting resumes half a second after the last size
    change. The first size after the view is loaded is not a drag: frames show
    from the first one.
  * No adapter: DeviceLost is raised ONCE with the engine's reason, RenderPath
    stays None, IsDeviceReady stays false, and the view shows what is behind it.
    It is not retried until the view is loaded again.
  * An error the device reports (a validation error outside the frame's
    checked commands, a lost device) means its state cannot be trusted:
    DeviceLost with the error text, one log line, and a fresh device on the next
    frame (DeviceReady again).
  * DeviceOptions set while the view owns its device, or RequireOwnedDevice
    changed while a device exists with the latch on: the device is replaced at
    the next frame - DeviceLost("Device options changed."), RenderPath through
    None, DeviceReady on the new device. A device creation that failed with the
    old options is tried again with the new ones.


RENDER PATHS
============
  Readback   The default on every head. The view owns its device (Vulkan on
             Linux, Direct3D 12 on Windows, Metal on macOS - the engine's
             choice), renders off-screen, reads each finished frame back once
             and draws it through the head's own canvas, so the view clips,
             overlaps, transforms and fades like any other XAML content. Cost:
             one GPU-to-processor copy per frame.
  Direct*    When the application opts in to direct GPU rendering
             (UseDirectGpuRenderingMode() on the host builder - EXPERIMENTAL,
             app-wide, one-way), the view uses the head's own GPU device where
             the head offers a direct path, and Readback everywhere else.
             Readback is ALWAYS the fallback; nothing fails. RenderPath says
             which path is running and RenderPathChanged says when it changes.
             DirectVulkan: the X11 head on its Vulkan renderer (which the latch
             selects by itself unless the app chose another X11 renderer) and
             the Wayland head on its Vulkan renderer (its default).
             DirectMetal: the macOS head in Metal mode. Every other head (Win32,
             WPF, frame buffer, PlayTest) runs Readback. See DIRECT PATHS.
             DirectSurface: the same element in CodeBrix.Android, presenting
             to a native window surface under the element; never reported by
             the CodeBrix.Platform heads.
             A view with RequireOwnedDevice = true runs Readback on its own
             device (from DeviceOptions) even with the latch on: latch +
             RequireOwnedDevice = Readback. Use it when the head's device cannot
             meet your renderer's requirements.
  None       No device (see LIFETIME).
Your renderer code is the same on every path: the frame protocol does not change.


DIRECT PATHS (DirectVulkan, DirectMetal)
========================================
  What happens. The view ADOPTS the head's own GPU device (the device the head
  renders the window with, exposed by the head as INativeGpuDeviceProvider) as
  its WebGPU device, renders each frame into a GPU texture the size of the
  element, and the head's own Skia GPU context draws that texture into the
  window: no processor readback, one GPU draw. Clipping, overlap, opacity and
  the overlay work exactly as on Readback.

  The adopted-device contract (the same one CodeBrix.WebGpu.Graphics documents
  for Wrap()):
  * Device, Queue, Adapter, Instance are the ADOPTED device's. The view owns
    them; the head owns the native device underneath. Never release or destroy
    them; take your own references if you keep them.
  * HasUncapturedErrorHandler is FALSE (and the device hand-off's flags lack
    FlagUncapturedErrorHandler): the engine ends the process on a validation
    error no error scope catches. The view therefore opens a validation error
    scope around every frame (BeginFrame..EndFrame, your Render included) and
    around DeviceReady; what one catches becomes one log line, DeviceLost with
    the error text, and a fresh device on the next frame - the same outcome as
    an owned device's uncaptured error. Work you do OUTSIDE those (a device call
    from a timer, a native module's own thread outside begin_frame/end_frame)
    is not covered: wrap it in DevicePushErrorScope/DevicePopErrorScope
    yourself, or do it inside a frame or DeviceReady.
  * Native modules: the same rule - inside begin_frame/end_frame the view's
    scope covers them; anything else they submit or create must be scoped.

  Threading on a direct path. Exactly the frame protocol above. In addition the
  view takes the head's device lock (the provider's BeginFrame scope) for the
  short moment it submits a frame, because the frame is submitted to the head's
  own queue; a frame never waits on the GPU (the submission's barrier orders it
  before the head's sampling on the same queue). Never call into the head's
  GRContext yourself.

  Frames and formats. Three GPU textures per view, BGRA8Unorm (Vulkan:
  VK_FORMAT_B8G8R8A8_UNORM, Skia Bgra8888 - the format the heads' own surfaces
  use, so it wraps without a swizzle, and the same format as Readback, so your
  pipelines work on every path). A frame never renders into the texture shown
  newest nor the one the head drew last (the head records the window frame and
  replays it later, so two would not be enough). The finished texture is
  transitioned to the shader-read layout in the frame's own submission. A
  resize makes new textures at the next frame; the last picture is drawn until
  then. Textures a recorded window frame still shows outlive the view's
  teardown for exactly as long as Skia needs them.

  When it does not run. Each of these is ONE log line and Readback, with the
  same picture: the latch is off; the head offers no device (Win32, WPF, frame
  buffer, X11 on OpenGL/software); the head's device cannot be adopted (the
  reason is in the line); a frame texture cannot be made; the head's Skia
  context cannot wrap a frame (this one is discovered by the paint, so the view
  switches at the next frame: DeviceLost, RenderPathChanged from the direct path to Readback,
  DeviceReady on its own device - your renderer recreates its resources as on
  any device change). The decision is kept until the view is loaded again.
  A head window that closes takes its device with it: the view lets go of the
  adopted objects without touching them (Information line) and continues on
  Readback if it is still alive.

  Wayland. Its Vulkan renderer (and with it the device) is created for the
  window's FIRST frame, which can come after the view's first frame. With the
  latch on, the view's first device waits for it - up to two seconds after the
  view is loaded, rendering no frame meanwhile - and then takes the direct path
  or, if the device never appears, Readback.

  The head's Vulkan device. To be adoptable, a Vulkan device must have been
  created with the device extensions and features the WebGPU engine expects.
  With the latch on, the X11 and Wayland heads create their device that way
  (only what the GPU supports; one Information line lists it); with the latch
  off their device creation is unchanged. Nothing for the application to do.

  Metal (macOS). The view adopts the window's MTLDevice; WebGPU gets its own
  command queue on it, and each frame's submission is waited for before the
  frame is shown (the engine cannot share Skia's queue on Metal). Requires the
  macOS head in Metal mode (its default when Metal is available).

  Testing notes. The UIReqs harness and PlayTest run on heads that render in
  software and offer no GPU device, so they always see Readback. The add-in's
  own suite and its UIReqs prove the direct path by offering the X11/Wayland
  heads' Vulkan device code, created without a window, as the window's device,
  and read the picture through the view's own capture (a software head cannot
  draw a GPU image). Do not try to prove DirectVulkan pixels through a
  software-rendered screenshot; a real window proof is a live run on the X11
  Vulkan renderer.


SHARING THE DEVICE
==================
The view is an IWebGpuDeviceSource and an IWebGpuTextureTarget, the two sharing
contracts of CodeBrix.WebGpu.Native, so a library that accepts them renders
into the view without either knowing the other. With CodeBrix.WebGpu.Graphics
(your own package reference):

    GraphicsContext graphics;                       // create in DeviceReady
    view.DeviceReady += (s, e) => graphics = GraphicsContext.Wrap(e.Device);
    view.DeviceLost  += (s, e) => { graphics?.Dispose(); graphics = null; };
    view.Render += (s, e) =>
    {
        using GraphicsFrame frame = graphics.BeginFrame(view);   // the open frame's encoder
        using (RenderPass pass = frame.BeginRenderPass(null)) { /* draw */ }   // null = load
        frame.Submit();                                          // closes the hand-out
    };                                          // the view submits at EndFrame

Inside a frame, the view's BeginWebGpuPass() hands out the frame's encoder once;
outside a frame it returns a null encoder.


NATIVE MODULES (Rust, C, C++)
=============================
A native module renders on the view's device, on its own thread, through one C
struct. Set FrameDriver="Manual" and pass the module view.GetViewHandoff():

    #include "codebrix_wgpu_procs.h"   /* WGPUCodebrixDeviceHandoff, WGPUCodebrixFrameHandoff */

    typedef struct WGPUCodebrixRenderViewHandoff {
        uint32_t size;                                             /* offset 0:  40 */
        uint32_t version;                                          /* offset 4:  1  */
        void* context;                                             /* offset 8  */
        WGPUCodebrixFrameHandoff* (*begin_frame)(void* context);   /* offset 16 */
        void (*end_frame)(void* context);                          /* offset 24 */
        WGPUCodebrixDeviceHandoff const * device;                  /* offset 32 */
    } WGPUCodebrixRenderViewHandoff;        /* 40 bytes, 8-byte aligned */

  * The struct lives in memory the view owns, at the same address for the
    view's lifetime. Validate in layers: this struct's size >= 40 and
    version == 1 first, then device->size >= sizeof(WGPUCodebrixDeviceHandoff)
    and device->version == WGPU_CODEBRIX_DEVICE_HANDOFF_VERSION. The device
    hand-off is referenced, not embedded, so it may grow without moving any
    member of this struct; later members of this struct are added after
    `device` (check size before reading them).
  * `device` points into the same view-owned memory and never changes; its
    CONTENTS are refilled whenever the view creates a new device.
  * begin_frame(context) opens a frame exactly like BeginFrame (waiting while
    another thread holds one) and returns its WGPUCodebrixFrameHandoff, or NULL
    when the view cannot render now: skip the frame and do NOT call end_frame.
  * After a non-NULL begin_frame, record into frame->commandEncoder (render into
    frame->textureView, format frame->format) and call end_frame(context) on the
    SAME thread. Close every pass you begin; never finish, submit or release the
    encoder.
  * Read *device after a successful begin_frame: it is refilled whenever the
    view creates a new device, and its device member is NULL while the view has
    none. Its procs member is the function table of the one codebrix_wgpu
    library; its flags carry FLAG_UNCAPTURED_ERROR_HANDLER when the view's own
    device reports errors instead of ending the process (never on an adopted
    device).
  * Both functions are safe from any thread and never let an exception cross
    the boundary: a failure is logged and reported as NULL (begin) or ignored
    (end).
  * Stop calling them before the application lets go of the view. After the
    view is collected, begin_frame returns NULL until the memory is freed.
  * Every handle is BORROWED: device-level handles until DeviceLost, frame
    handles for that frame only. Take your own reference (*AddRef) to keep one.

From C#, the same values are view.GetDeviceHandoff() (what `device` points at)
and, inside a frame, frame.GetHandoff(). The rules a module follows are in
MODULE CONTRACT below.

THE LOAD-ORDER RULE. The view calls CodeBrixWebGpuNative.EnsureLoaded() in its
constructor, so codebrix_wgpu is loaded by .NET before any module library that
links against it - the module then reuses that one copy. If a module's .NET
wrapper can be loaded BEFORE any view exists, its own static constructor must
call CodeBrixWebGpuNative.EnsureLoaded() first (CodeBrix.WebGpu.Native's
AGENT-README, NATIVE MODULES, has the linking rules per operating system).


MODULE CONTRACT (what a native module, or any renderer, may assume and must do)
===============================================================================
  The frame.
  * During a frame a module records into the frame's encoder
    (frame->commandEncoder / WebGpuFrame.Encoder) and renders into the frame's
    texture. It must NOT finish, submit or release that encoder - EndFrame
    (end_frame) does. It closes every pass it begins.
  * Private resources are fine: its own textures, buffers, pipelines, and its
    own encoders for work that does not touch the frame's texture. All
    submissions serialise on the one shared queue (the view's own queue, or the
    head's queue on a direct path). Its own submissions are not validated by
    the view: finish them inside an error scope and never submit on error.

  The target.
  * Format BGRA8Unorm (PreferredFormat), premultiplied alpha, 8 bits per
    channel holding sRGB-ENCODED values (the format is not an *Srgb format:
    what the module writes is what the head shows), origin top-left, one
    sample, size in DEVICE pixels = TextureWidth x TextureHeight (the frame's
    width/height). The texture arrives cleared to ClearColor, premultiplied.
  * A module that produces straight-alpha or linear RGBA16Float/RGBA32Float
    renders into its own texture and converts ON THE GPU into the frame's
    texture inside the frame's encoder (a full-screen pass that premultiplies
    and sRGB-encodes). Never read back to convert.

  Threads and lifetime.
  * The owner thread: the built-in drivers (Continuous, OnDemand) run frames
    on the UI thread; under Manual it is the thread that calls BeginFrame
    (begin_frame). Keep it ONE thread for the whole life of a module session:
    a module whose engine adopts the device through its own runtime needs a
    fixed attachment thread.
  * DeviceLost is raised on the UI thread when the view is unloaded, and on
    the frame's thread for device faults (an error the device reported, a
    direct path failing) and for "Device options changed.".
  * Teardown order on Unloaded: DeviceLost first (the device still exists:
    release your resources), then the picture on screen is dropped, then the
    targets and the device are released and the device hand-off is refilled
    with a NULL device; begin_frame returns NULL until the view is loaded
    again. A frame open on another thread at that moment is waited for (two
    seconds), otherwise the teardown runs at that frame's end_frame.

  Transports. Both are served by the ONE device hand-off:
  * direct linking against codebrix_wgpu - the view calls
    CodeBrixWebGpuNative.EnsureLoaded() in its constructor, so .NET has loaded
    the library before a module links to it (THE LOAD-ORDER RULE above);
  * the function table - device->procs, filled from that same library.

  Capabilities.
  * Export presence is NOT capability evidence: the engine exports functions
    it does not implement and calling one ends the process (the LIMITS list
    of CodeBrix.WebGpu.Native's AGENT-README; "ABORTS" notes in
    codebrix_wgpu_procs.h). The view never calls them. A module keeps to an
    allow-listed profile of functions known to work on this engine.
  * Requirements are checked BEFORE initialising, in DeviceReady, against
    WebGpuDeviceInfo. Features (and limits) cannot be added to a device after
    it was created. Features/NativeFeatures/Limits are what the device HAS
    (enabled when it was created); AdapterFeatures/AdapterNativeFeatures/
    AdapterLimits are what its adapter OFFERS. "Not enabled" (in the adapter's
    list, not the device's) differs from "not available" (in neither): only
    the first could be met by a device created differently.
  * The view's own device is created from DeviceOptions; on a direct path the
    device is the head's (adopted) and reported the same way. A renderer the
    head's device cannot satisfy sets RequireOwnedDevice = true and runs on an
    owned device created from DeviceOptions (Readback).


PLAYTEST
========
PlayTests of an application that uses this view reference
CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever (the PlayTests project
only, beside CodeBrix.Platform.PlayTest.ApacheLicenseForever) and call
CodeBrixPlayTestWebGpu.Register() before PlayTestApplication.LaunchAsync; that
package's AGENT-README has the details. Under the PlayTest head the view asks
the launch whether it may create its own device, BEFORE creating it:
  * PlayTestOptions.WebGpu = Unavailable: no device. RenderPath stays None and
    DeviceLost is raised once with "PlayTestOptions.WebGpu is Unavailable for
    this launch." - so an application's no-GPU behaviour is testable.
  * Available (the default) without a WebGPU provider registered for the launch:
    the test fails with the PlayTest head's message naming the provider package
    and its Register() call; the view itself reports no device.
  * With a provider: the view runs its Readback path (no window needed), so
    PlayTest screenshots carry real pixels.


COMPLETE EXAMPLES
=================

1) XAML + Render event (Continuous)

    <Page xmlns:webGpu="using:CodeBrix.Platform.UI.WebGpu" ...>
      <Grid>
        <webGpu:WebGpuRenderView x:Name="View" ClearColor="#FF101830"
                                 DeviceReady="OnDeviceReady" Render="OnRender"
                                 DeviceLost="OnDeviceLost" />
      </Grid>
    </Page>

    using CodeBrix.Platform.UI.WebGpu;
    using CodeBrix.WebGpu.Native;
    using Wgpu = CodeBrix.WebGpu.Native.WebGpu;

    private RenderPipeline _pipeline;

    private unsafe void OnDeviceReady(object sender, WebGpuDeviceReadyEventArgs e)
    {
        // DeviceCreateShaderModule + DeviceCreateRenderPipeline on e.Device.Device,
        // colour target format = View.PreferredFormat (BGRA8Unorm).
    }

    private unsafe void OnRender(object sender, WebGpuRenderEventArgs e)
    {
        WebGpuFrame frame = e.Frame;
        var color = new RenderPassColorAttachment
        {
            View = frame.TextureView, DepthSlice = Constants.DepthSliceUndefined,
            LoadOp = LoadOp.Load, StoreOp = StoreOp.Store,     // keep the clear colour
        };
        var desc = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color };
        RenderPassEncoder pass = Wgpu.CommandEncoderBeginRenderPass(frame.Encoder, &desc);
        Wgpu.RenderPassEncoderSetPipeline(pass, _pipeline);
        Wgpu.RenderPassEncoderDraw(pass, 3, 1, 0, 0);
        Wgpu.RenderPassEncoderEnd(pass);
        Wgpu.RenderPassEncoderRelease(pass);
    }                                      // the view validates, submits and shows it

    private void OnDeviceLost(object sender, WebGpuDeviceLostEventArgs e)
    {
        if (!_pipeline.IsNull) { Wgpu.RenderPipelineRelease(_pipeline); _pipeline = default; }
    }

2) A renderer class with a HUD overlay

    public sealed class SceneRenderer : IWebGpuRenderer
    {
        private volatile string _hud = "";
        public void OnDeviceReady(IWebGpuDeviceSource device) { /* create resources */ }
        public void OnRender(WebGpuFrame frame)
        {
            /* record into frame.Encoder */
            _hud = $"frame {frame.FrameIndex}  {1 / Math.Max(frame.DeltaSeconds, 1e-6):F0} fps";
        }
        public void OnOverlay(SKCanvas canvas, SKSize size)     // the head's paint: no WebGPU here
        {
            using var font = new SKFont(SKTypeface.Default, 16);
            using var ink = new SKPaint { Color = SKColors.White, IsAntialias = true };
            canvas.DrawText(_hud, 12, 24, font, ink);
        }
        public void OnDeviceLost(string reason) { /* release resources */ }
    }

    <webGpu:WebGpuRenderView Renderer="{x:Bind ViewModel.Renderer}" />

3) OnDemand: render only when something changed

    View.FrameDriver = WebGpuFrameDriver.OnDemand;
    _model.Changed += (_, _) => View.Invalidate();      // any thread

4) Manual, from your own render thread

    View.FrameDriver = WebGpuFrameDriver.Manual;
    new Thread(() =>
    {
        while (_running)
        {
            WebGpuFrame? frame = View.BeginFrame();
            if (frame is null) { Thread.Sleep(16); continue; }   // not loaded / no device yet
            try { RecordScene(frame); }
            finally { View.EndFrame(); }                        // same thread
        }
    }) { IsBackground = true }.Start();

5) A native module driving its own loop

    View.FrameDriver = WebGpuFrameDriver.Manual;
    MyModule.Start(View.GetViewHandoff());   // the module calls begin_frame/end_frame on its thread


MINIMUM VIABLE PROJECT
======================
  MyApp.Core.csproj
      <PackageReference Include="CodeBrix.Platform.WebGpu.ApacheLicenseForever" Version="…" />
      <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  MainPage.xaml      xmlns:webGpu + one <webGpu:WebGpuRenderView Render="…"/> in a Grid cell
  MainPage.xaml.cs   DeviceReady: create the pipeline; Render: record a pass

  That is the whole integration: no head project changes and no GPU wiring.


PERFORMANCE TIPS
================
  - The frame path reuses everything it can: the frame object, the event args,
    the hand-off structs and the read-back pixel buffers (two or three
    circulate; a buffer is only written once no picture of it can still be
    drawn, so a head that replays a recorded window frame later never sees it
    torn). Keep your Render handler the same way - descriptors on the stack,
    "main"u8 literals pinned with fixed, buffers created once in DeviceReady.
  - On a direct path (opt in with UseDirectGpuRenderingMode()) frames never
    leave the GPU: measured at 1024x640 on an integrated GPU, the frame loop
    runs several times faster than on Readback on the same renderer.
  - Readback costs one GPU-to-processor copy per frame, proportional to the
    element's pixel count. A full-window view at 60 frames a second is
    comfortable; for heavier work, size the element to what it shows or lower
    TargetFrameRate.
  - Pause (IsPaused) or switch to OnDemand when the picture does not change: a
    static scene at 60 frames a second is 60 read-backs a second for nothing.
  - TargetFrameRate = 0 renders as fast as the UI thread allows; use it for
    benchmarks, not for applications (it competes with input and layout).
  - Upload with QueueWriteBuffer/QueueWriteTexture inside Render: the writes run
    at the start of the frame's own submit, with no extra synchronisation.


LIMITS (inherited from the WebGPU engine)
=========================================
  - Some WebGPU functions END THE PROCESS when called because the engine does
    not implement them - among them every *SetLabel function,
    ShaderModuleGetCompilationInfo and DeviceGetAdapterInfo (use AdapterGetInfo).
    The full list is LIMITS in CodeBrix.WebGpu.Native's AGENT-README; read it
    before using a function for the first time.
  - Invalid commands inside a frame are caught: EndFrame throws
    WebGpuSkiaValidationException and submits nothing. Work you submit YOURSELF
    (your own encoder + QueueSubmit) is not checked, and an invalid submission
    ends the process. Record into the frame's encoder.
  - One texture format (BGRA8Unorm), one sample per pixel. For multisampling,
    render into your own multisampled texture and resolve into
    frame.TextureView.
  - Device loss is reported as an error from the device (DeviceLost), not
    through WebGPU's device-lost callback.


COMMON PITFALLS TO AVOID
========================
  - Touching WebGPU objects in Overlay/OnOverlay. They run during the head's
    paint, not on the frame's thread. Copy what the HUD needs into a field
    during Render.
  - On a direct path, a device call outside a frame or DeviceReady with no
    error scope of your own: an invalid one ends the process (see DIRECT PATHS).
  - Calling EndFrame on another thread than BeginFrame, or calling EndFrame
    after BeginFrame returned null.
  - Keeping frame.Texture / TextureView / Encoder past EndFrame, or keeping the
    WebGpuFrame object itself: it is reused, and a resize replaces the texture.
  - Keeping device resources across DeviceLost. Unloading the view (navigating
    away) releases the device; DeviceReady on the next load is where they come
    back.
  - A pipeline whose colour-target format is not PreferredFormat (BGRA8Unorm):
    EndFrame throws WebGpuSkiaValidationException.
  - Expecting Render under the Manual driver. Under Manual the code that called
    BeginFrame renders; Render is the built-in drivers' event.
  - Giving the element no size. In a StackPanel with no Height it is measured
    to nothing and renders no frame (BeginFrame returns null). Put it in a Grid
    cell or give it Width/Height.
  - Forgetting AllowUnsafeBlocks in the project that records commands.
  - Ignoring the name clashes listed under KEY NAMESPACES / USINGS.


WHAT THIS PACKAGE DOES NOT DO
=============================
  - No WebGPU object model: pipelines, shaders, buffers and passes are the raw
    CodeBrix.WebGpu.Native API (or CodeBrix.WebGpu.Graphics, which you add).
  - No window surface or swap chain of its own: frames reach the screen through
    the head (see RENDER PATHS).
  - No 3D scene, model loading or text rendering on the GPU. The overlay is the
    place for text, with SkiaSharp.


WORKING EXAMPLES ON GITHUB
==========================
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/WebGpuDemo
      The reference application for this package (all six heads): a raw WGSL
      triangle through the Render event, glTF models drawn into the view with
      CodeBrix.WebGpu.Graphics (Wrap(view) + BeginFrame(view)), an
      IWebGpuRenderer set from XAML, and a Controls page with the three frame
      drivers, pausing, TargetFrameRate and a SkiaSharp HUD. Each head's
      Program.cs shows the UseDirectGpuRenderingMode() opt-in as a commented
      line. Its tests/WebGpuDemo.PlayTests measure the pages with PlayTest's
      PixelStats; tests/WebGpuDemo.NoWebGpu.PlayTests launches without WebGPU.
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.WebGpu
      The add-in's own source, fully XML-documented (WebGpuRenderView.skia.cs,
      and the frame protocol in Internal/WebGpuRenderEngine.skia.cs).
  https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/AddIns/Platform.UI.WebGpu.Tests
      The add-in's tests: a WGSL triangle pipeline drawn into a frame, frames
      from a worker thread, the hand-off driven through its function pointers
      from a "module" thread, and the drivers' timing.


QUICK REFERENCE CARD
====================
namespace CodeBrix.Platform.UI.WebGpu
xmlns:webGpu="using:CodeBrix.Platform.UI.WebGpu"

[Bindable] sealed class WebGpuRenderView : FrameworkElement, IWebGpuDeviceSource, IWebGpuTextureTarget
    WebGpuFrameDriver FrameDriver       Continuous | OnDemand | Manual
    double TargetFrameRate              60; 0 = as fast as possible
    bool IsPaused                       IWebGpuRenderer? Renderer
    Stretch Stretch                     Fill
    Windows.UI.Color ClearColor         opaque black
    WebGpuSkiaDeviceOptions? DeviceOptions  null   bool RequireOwnedDevice  false
    WebGpuDeviceInfo? DeviceInfo (ro)
    WebGpuRenderPath RenderPath (ro)    bool IsDeviceReady (ro)
    TextureFormat PreferredFormat (ro)  double PixelScale (ro)
    int TextureWidth, TextureHeight (ro)
    Instance Instance  Adapter Adapter  Device Device  Queue Queue  BackendType Backend
    bool HasUncapturedErrorHandler
    Texture Texture  TextureView TextureView  TextureFormat Format   (inside a frame only)
    WebGpuFrame? BeginFrame();  void EndFrame();  void Invalidate();
    CodebrixDeviceHandoff* GetDeviceHandoff();  WebGpuRenderViewHandoff* GetViewHandoff();
    event DeviceReady(WebGpuDeviceReadyEventArgs)     Device, DeviceInfo
    event Render(WebGpuRenderEventArgs)               Frame
    event Overlay(WebGpuOverlayEventArgs)             Canvas, Size     (head's paint)
    event DeviceLost(WebGpuDeviceLostEventArgs)       Reason
    event RenderPathChanged(WebGpuRenderPathChangedEventArgs)  OldPath, NewPath

interface IWebGpuRenderer
    OnRender(WebGpuFrame)   [OnDeviceReady(IWebGpuDeviceSource)  OnOverlay(SKCanvas, SKSize)  OnDeviceLost(string)]

sealed class WebGpuFrame : IDisposable
    Encoder  Texture  TextureView  Format  Width  Height  FrameIndex
    ElapsedSeconds  DeltaSeconds  IsOpen  GetHandoff()  Dispose() == EndFrame()

enum WebGpuFrameDriver { Continuous, OnDemand, Manual }
enum WebGpuRenderPath  { None, Readback, DirectVulkan, DirectMetal, DirectSurface }

sealed class WebGpuDeviceInfo
    AdapterName  AdapterVendor  AdapterDescription  AdapterType  VendorId  DeviceId  Backend
    Features  NativeFeatures  Limits                (enabled on the device)
    AdapterFeatures  AdapterNativeFeatures  AdapterLimits   (offered by the adapter)
    HasFeature(...)  AdapterHasFeature(...)

struct WebGpuRenderViewHandoff (40 bytes)
    uint Size; uint Version; void* Context;
    delegate* unmanaged[Cdecl]<void*, CodebrixFrameHandoff*> BeginFrame;
    delegate* unmanaged[Cdecl]<void*, void> EndFrame;
    CodebrixDeviceHandoff* Device;

Frame:   var f = view.BeginFrame(); if (f != null) { record into f.Encoder; view.EndFrame(); }
Overlay: the head's paint, SkiaSharp only.
Opt-in:  UseDirectGpuRenderingMode() - direct path where the head offers one, else Readback.

================================================================================
END OF AGENT-README
================================================================================
