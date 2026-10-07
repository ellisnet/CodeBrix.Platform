# CodeBrix.Platform.PlayTest.OpenGL

Real OpenGL for [CodeBrix.Platform PlayTests](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/Platform.UI.Runtime.Skia.PlayTest):
an application that uses the Graphics3DGL add-in (`GLCanvasElement`, `SkiaGLCanvasElement`,
`OffscreenGLContext`, `SkiaGpuContext`) renders its OpenGL content in a PlayTest run exactly as it does
on a desktop head, and the pixels appear in screenshots.

```csharp
using CodeBrix.Platform.PlayTest.OpenGL;

CodeBrixPlayTestOpenGL.Register();
var app = await PlayTestApplication.LaunchAsync(() => new App());
```

| Operating system | Context | Needs |
|---|---|---|
| Linux | Mesa EGL, surfaceless platform (GPU render node or llvmpipe) | `apt install libegl1 libgl1-mesa-dri` |
| Windows | WGL on `opengl32.dll`, hidden window | An interactive desktop with a GPU OpenGL driver (Windows on ARM: the OpenCL and OpenGL Compatibility Pack) |
| macOS | ANGLE on Metal (ships with Graphics3DGL) | A Mac with a Metal device |

- Reference it from the PlayTests project, beside `CodeBrix.Platform.PlayTest.ApacheLicenseForever`. It
  depends on `CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever`, which brings the GL binding.
- A machine that cannot create a context fails the launch with the concrete reason; an application that
  initializes an OpenGL element in a run with no provider registered fails the test, naming this package.
- `PlayTestOptions.OpenGL = PlayTestOpenGL.Unavailable` launches without OpenGL, to test an application's
  fallback.
- OpenGL content is asserted in the running test with `PixelStats` (coverage, bounds, centroid, lighting
  halves, variance, symmetry, before/after difference) - never against saved images.
- Vulkan is not supported.

The package's `AGENT-README.txt` and the PlayTest head's `AGENT-README.txt` (section OPENGL) document the
full API.

License: Apache-2.0.
