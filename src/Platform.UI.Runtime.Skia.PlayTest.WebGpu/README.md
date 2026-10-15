# CodeBrix.Platform.PlayTest.WebGpu

Real WebGPU for [CodeBrix.Platform PlayTests](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/Platform.UI.Runtime.Skia.PlayTest):
an application that uses the WebGpu add-in (`WebGpuRenderView`) renders its WebGPU content in a PlayTest
run on a real GPU device, and the pixels appear in screenshots.

```csharp
using CodeBrix.Platform.PlayTest.WebGpu;

CodeBrixPlayTestWebGpu.Register();
var app = await PlayTestApplication.LaunchAsync(() => new App());
```

| Operating system | Device | Needs |
|---|---|---|
| Linux | Vulkan (the GPU's driver, or a software Vulkan driver) | `apt install mesa-vulkan-drivers` |
| Windows | Direct3D 12, then Vulkan | A GPU driver with Direct3D 12 or Vulkan support |
| macOS | Metal | A Mac with a Metal device |

- Reference it from the PlayTests project, beside `CodeBrix.Platform.PlayTest.ApacheLicenseForever`. It
  depends on `CodeBrix.Platform.WebGpu.ApacheLicenseForever`, which brings the WebGPU engine.
- The launch proves that an adapter and a device exist (a small clear is rendered and read back); a
  machine that cannot create one fails the launch with the engine's reason. An application that
  initializes a `WebGpuRenderView` in a run with no provider registered fails the test, naming this package.
- Under PlayTest every view creates its own device and renders through its read-back path, so its
  `RenderPath` is `Readback` and its pixels are in every screenshot.
- `PlayTestOptions.WebGpu = PlayTestWebGpu.Unavailable` launches without WebGPU, to test an application's
  fallback: the views report no device.
- WebGPU content is asserted in the running test with `PixelStats` (coverage, bounds, centroid, halves,
  variance, symmetry, before/after difference) - never against saved images.

The package's `AGENT-README.txt` and the PlayTest head's `AGENT-README.txt` (section WEBGPU) document the
full API.

License: Apache-2.0.
