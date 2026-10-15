# CodeBrix.Platform.WebGpu

WebGPU rendering in a [CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform) page: one XAML element,
`WebGpuRenderView`, that owns a WebGPU device, hands you a frame to record into, and shows the result like any other
XAML content - clipped, overlapped and transformed - on every Skia head (Windows, Linux X11, Wayland and frame
buffer, macOS).

```xml
<Page xmlns:webGpu="using:CodeBrix.Platform.UI.WebGpu">
  <Grid>
    <webGpu:WebGpuRenderView ClearColor="#FF101830"
                             DeviceReady="OnDeviceReady" Render="OnRender" DeviceLost="OnDeviceLost" />
  </Grid>
</Page>
```

```csharp
private unsafe void OnRender(object sender, WebGpuRenderEventArgs e)
{
    WebGpuFrame frame = e.Frame;          // already cleared to ClearColor
    // record render passes into frame.Encoder, drawing into frame.TextureView (BGRA8Unorm)
}                                          // the view validates, submits and shows the frame
```

The WebGPU API itself is the raw [CodeBrix.WebGpu](https://github.com/ellisnet/CodeBrix.WebGpu) binding
(`CodeBrix.WebGpu.Native`), which this package brings with it, together with the WebGPU engine's native library for
every platform. Recording commands takes pointers, so the project with your renderer needs
`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`.

## Three ways to drive frames

| `FrameDriver` | When a frame runs | Thread |
|---|---|---|
| `Continuous` (default) | At `TargetFrameRate` (60; 0 = as fast as possible). A late frame is never caught up - no backlog. `IsPaused` stops it. | UI thread |
| `OnDemand` | Once per `Invalidate()` (calls coalesce), plus on load and on resize. | UI thread |
| `Manual` | Whenever you call `BeginFrame()` / `EndFrame()`. | Any one thread at a time - yours |

## Events or a renderer class

Handle the view's events (`DeviceReady`, `Render`, `Overlay`, `DeviceLost`, `RenderPathChanged`), or set
`Renderer` to an `IWebGpuRenderer` (only `OnRender` is required). The view calls the renderer first, then raises the
event, so the two can be mixed.

## A SkiaSharp overlay

`Overlay` (and `IWebGpuRenderer.OnOverlay`) draws over every frame with SkiaSharp - a HUD, labels, a selection box -
in the view's own coordinates. It runs while the head paints the window, not on the frame's thread, so it must not
touch WebGPU objects: copy what it shows into a field while rendering.

## Render path

`RenderPath` says how frames reach the screen. The default, on every head, is `Readback`: the view renders
off-screen and draws each finished frame through the head's own canvas (one copy per frame). An application that
opts in with `UseDirectGpuRenderingMode()` on the host builder (experimental, app-wide) lets the view render on the
head's own GPU device where the head offers one - `DirectVulkan` on the X11 head's Vulkan renderer and on Wayland,
`DirectMetal` on macOS - with no copy at all; on every other head, and whenever any step of a direct path fails, the
view silently (one log line) uses `Readback` with the same picture. Read `RenderPath` after `DeviceReady`, or handle
`RenderPathChanged`; the renderer code is the same either way.

## Device lifetime

The device is created on the first frame after the view is loaded (`DeviceReady`) and released when the view is
unloaded (`DeviceLost` first, so you can release your resources); loading the view again brings a new device. A
machine without a WebGPU adapter raises `DeviceLost` once with the engine's reason, and the view shows what is
behind it.

## Choosing the device

`DeviceOptions` (a `WebGpuSkiaDeviceOptions` from CodeBrix.WebGpu.Skia: preferred backend, fallback, power preference,
fallback adapter) decides how the view creates its own device; leave it null for the engine's defaults. Setting it
while the view has its own device replaces that device at the next frame (`DeviceLost` with "Device options
changed.", then `DeviceReady`). `RequireOwnedDevice = true` makes the view always create its own device, even where
the application opted in to direct GPU rendering, for a renderer the head's device cannot satisfy. `DeviceInfo` (and
`DeviceReady`'s args) say what the device is: adapter, backend, the features and limits it was created with, and the
ones its adapter offers - check a renderer's requirements there before it initialises.

## PlayTest

Under the PlayTest head, reference `CodeBrix.Platform.PlayTest.WebGpu.ApacheLicenseForever` in the PlayTests project
and call `CodeBrixPlayTestWebGpu.Register()` before the launch: every view then renders on a real device through its
`Readback` path, so its pixels are in screenshots. `PlayTestOptions.WebGpu = PlayTestWebGpu.Unavailable` launches
without WebGPU, to test what the application shows when there is no device.

## Android

The same element exists in CodeBrix.Android (package `CodeBrix.Android.WebGpu.ApacheLicenseForever`): the same
`WebGpuRenderView` in the same `CodeBrix.Platform.UI.WebGpu` namespace, with the same API, so pages and renderers move
between the two unchanged. Its direct path there is `DirectSurface`; its own package documentation has the
Android specifics.

## Sharing and native modules

The view is an `IWebGpuDeviceSource` and an `IWebGpuTextureTarget`, the CodeBrix.WebGpu sharing contracts, so a
library that accepts them - such as `CodeBrix.WebGpu.Graphics`, which you add yourself if you want it - renders into
the view directly. `GetViewHandoff()` hands the whole view to a native (Rust, C, C++) module as a C struct with
`begin_frame` / `end_frame` function pointers, so the module can run its own render loop on its own thread.

The [WebGpuDemo sample](https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/WebGpuDemo)
shows all of it on every head, with its PlayTests. The package's `AGENT-README.txt` documents the full API, the threading contract, the lifetime rules, the native
hand-off's C declaration, the module contract and the limits inherited from the WebGPU engine.

License: Apache-2.0. The CodeBrix.WebGpu packages are MIT; the engine's notices travel with them.
