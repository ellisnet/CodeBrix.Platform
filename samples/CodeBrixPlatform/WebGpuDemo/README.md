# WebGpuDemo

The demo application for the WebGpu add-in (`WebGpuRenderView`). The same XAML
runs in six desktop heads and in the PlayTest head. Four pages, one button each:

- **Triangle** - the minimal dependency story: the add-in alone. A WGSL pipeline
  is created in `DeviceReady`, a triangle whose colour walks round the colour
  wheel is recorded into the frame's encoder in `Render` (raw
  `CodeBrix.WebGpu.Native` calls, a uniform written every frame), and the
  pipeline is released in `DeviceLost`
  (`src/WebGpuDemo.Core/Rendering/AnimatedTriangle.cs`, `WgslPipeline.cs`).
- **Models** - the optional higher layer, chosen by the APPLICATION: the demo's
  `.Core` references `CodeBrix.WebGpu.Graphics` (the add-in never does), which
  wraps the view's device (`GraphicsContext.Wrap(view)`) and renders into the
  view itself (`GraphicsContext.BeginFrame(view, depth)`) - the view is an
  `IWebGpuDeviceSource` and an `IWebGpuTextureTarget`, so neither package knows
  about the other. Three Cube Pets animals and the Avian round a treasure chest
  that opens and closes, glTF animation clips, physically based shading, a sun
  and a procedural sky (image-based lighting) shown by the skybox, an orbit
  camera, and a grass plane built in memory with `CodeBrix.Graphics3D.Gltf2`
  (`Rendering/ModelScene.cs`). A slider turns the camera.
- **Renderer class** - an `IWebGpuRenderer` declared as a XAML resource and set
  with `Renderer="{StaticResource Squares}"`: four coloured squares turning round
  the middle (one instanced draw) and an overlay label
  (`Rendering/QuadrantSquaresRenderer.cs`). The view calls the renderer first,
  then raises its events; the page still handles `DeviceReady`/`DeviceLost` for
  its status line.
- **Controls** - the frame drivers (`Continuous`, `OnDemand`, `Manual`; under
  Manual the "Render one frame" button opens the frame with `BeginFrame()`,
  records it and calls `EndFrame()`), `Invalidate()`, pausing (`IsPaused`), a
  `TargetFrameRate` slider (0 = as fast as possible), the current `RenderPath`,
  a SkiaSharp HUD drawn by the `Overlay` event: measured frames per second,
  render path and driver (`Rendering/HudOverlay.cs`), and two lines of
  `WebGpuDeviceInfo` from `DeviceReady`: the adapter, and how many features the
  device has enabled out of those the adapter offers, with two of its limits.

Every page shows its own message when its view has no device (the view raises
`DeviceLost` with the reason and shows what is behind it). The pages are kept
by `MainPage`, so leaving one unloads its view - which releases its device - and
coming back creates a new device (`DeviceLost`, then `DeviceReady` again).

## Direct GPU rendering (the opt-in latch)

By default every view renders through its **Readback** path: its own device,
one read-back per frame, drawn through the head's canvas. Each head's
`Program.cs` shows the opt-in as a commented line:

```csharp
// .UseDirectGpuRenderingMode()
```

Uncommented (EXPERIMENTAL, app-wide, one-way), the views take the head's own
GPU device where the head offers one: **DirectVulkan** on the X11 head's Vulkan
renderer (the latch selects it when the application chose no X11 renderer) and
on the Wayland head's Vulkan renderer, **DirectMetal** on macOS; Win32, WPF and
the frame buffer stay on Readback. The X11 head's `Program.cs` also shows the
explicit Vulkan renderer choice, commented:

```csharp
// .UseLinuxX11(x11 => x11.RenderingBackend(X11RenderingBackend.Vulkan))
```

The Controls page's "Render path" line and HUD say which path is running:
`Readback` by default and under PlayTest (the PlayTest head renders in software
and offers no window device), `DirectVulkan` in a live X11 run on the Vulkan
renderer with the latch on.

## Projects

- `src/WebGpuDemo.Core`: the renderers (`Rendering/`), the host helper, the
  framework, the WebGpu add-in (from source) and the
  `CodeBrix.WebGpu.Graphics` / `CodeBrix.Graphics3D.Gltf2` package references,
  plus the models under `Assets/models` (copy-to-output).
- `src/WebGpuDemo.UI`: shared App, MainPage and the four pages, compiled into
  each head.
- `src/WebGpuDemo.{LinuxX11,LinuxWayland,LinuxFrameBuffer,Win32Skia,WinWpfSkia,MacOS}`:
  ordinary desktop applications.
- `tests/WebGpuDemo.PlayTests`: the running application with real WebGPU
  devices - the fixture calls `CodeBrixPlayTestWebGpu.Register()` (the
  CodeBrix.Platform.PlayTest.WebGpu provider) before the launch.
- `tests/WebGpuDemo.NoWebGpu.PlayTests`: the same application launched with
  `PlayTestOptions.WebGpu = PlayTestWebGpu.Unavailable`, in a process of its
  own: every page shows its message, no view has a device, nothing throws.

Open the solution for your OS: `WebGpuDemo.Linux.slnx`, `WebGpuDemo.Windows.slnx`
or `WebGpuDemo.MacOS.slnx`. Framework, add-in and PlayTest references build from
source; `WebGpuDemo.Head.targets` supplies the source-based XAML generator and
Skia runtime wiring shared by the heads and the PlayTests projects.

## Run

From this directory:

```sh
# The interactive X11 application.
dotnet run --project src/WebGpuDemo.LinuxX11 -c Release

# Offscreen tests (headless; a WebGPU adapter is all they need - Vulkan on Linux).
dotnet test --project tests/WebGpuDemo.PlayTests/WebGpuDemo.PlayTests.csproj -c Release
dotnet test --project tests/WebGpuDemo.NoWebGpu.PlayTests/WebGpuDemo.NoWebGpu.PlayTests.csproj -c Release

# Watch them in the view-only preview window.
dotnet test --project tests/WebGpuDemo.PlayTests/WebGpuDemo.PlayTests.csproj -c Release --headed
```

In a Debug build the console shows one line per view device, for example
`WebGPU device ready: <adapter> (Vulkan), render path Readback.`

## What the PlayTests check

WebGPU pixels have no visual tree, so the tests measure the views' pixels in
the running test with `PixelStats` (never against saved images): the
triangle's share of the view, its bounds and symmetry, and that its colour keeps
changing; the models scene's colour variety, sky above and grass below, and that
it moves; each of the renderer's four squares and its overlay label; the HUD
panel and its text; `RenderPath` = `Readback`; `OnDemand` renders exactly one
frame per `Invalidate`; under `Manual` only the button renders; pausing stops
and resuming restarts the continuous driver; the frame-rate slider sets
`TargetFrameRate`; and a page that is left and shown again renders on a new
device. The views animate, so no capture asks for `Stable`. Element
screenshots of each page are written below `TestResults/WebGpuDemo` in the
test process directory.

On Linux the PlayTests need a Vulkan driver (`mesa-vulkan-drivers`); the launch
fails with the engine's reason on a machine without a WebGPU adapter.

## Credits

The models are public-domain (CC0, Creative Commons Zero) assets, copied from
the CodeBrix.WebGpu repository's WebGpuModelViewer sample; only the files this
demo uses are included, each pack's licence beside its models in
`src/WebGpuDemo.Core/Assets/models/`:

- **Cube Pets** by Kenney (www.kenney.nl) - `cube-pets/` (cat, dog, fox),
  licence `LICENSE-Kenney-cube-pets.txt`
- **Survival Kit** by Kenney (www.kenney.nl) - `survival-kit/` (chest), licence
  `LICENSE-Kenney-survival-kit.txt`
- **Crystals and Avian** by Jeremy Ellis for the Hakunaverse project, made in the
  style of the Kenney kits - `crystals-and-avian/avian.glb`, licence
  `LICENSE-JeremyEllis-crystals-and-avian.txt`

Thank you, Kenney, for the wonderful free assets.
