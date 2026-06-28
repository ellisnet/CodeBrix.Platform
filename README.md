# CodeBrix.Platform

**CodeBrix.Platform** is a cross-platform desktop UI framework for **.NET 10**.
You write your application once using the WinUI XAML API surface
(`Microsoft.UI.Xaml.*` controls, XAML, code-behind, and data binding), and
CodeBrix.Platform renders it natively on **Windows, Linux, and macOS** using a
Skia-based rendering engine.

One shared UI and business-logic codebase, multiple thin per-platform
executables.

## Supported targets

- **Windows** — Win32 host or WPF host
- **Linux** — X11 desktop, or framebuffer (for kiosk/embedded devices)
- **macOS** — Apple Silicon and Intel

> Mobile (iOS/Android) and WebAssembly/browser targets are out of scope for this
> framework.

## How an app is structured

A CodeBrix.Platform solution has three kinds of projects:

1. **`.Core`** — a class library holding your application logic, view models,
   and the framework + extension NuGet package references.
2. **`.UI`** — a shared project (`.shproj` + `.projitems`) holding the shared
   XAML: `App.xaml` and your views.
3. **One executable "head" per platform** — a thin project that references
   `.Core`, imports the `.UI` shared project, and references exactly one
   platform package.

## Packages

| Package | Role |
| --- | --- |
| `CodeBrix.Platform.ApacheLicenseForever` | The core UI framework (required) |
| `CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever` | 2D SkiaSharp drawing |
| `CodeBrix.Platform.Lottie.ApacheLicenseForever` | Lottie / Skottie animations |
| `CodeBrix.Platform.Svg.ApacheLicenseForever` | SVG (`SvgImageSource`) support |
| `CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever` | SkiaSharp XAML views |
| `CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever` | Windows (Win32) host |
| `CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever` | Windows (WPF) host |
| `CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever` | Linux (X11) host |
| `CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever` | Linux framebuffer host |
| `CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever` | macOS host |

The framework and extension packages are referenced by the `.Core` library; each
head project references exactly one of the `Runtime.Skia.*` host packages.

## Getting started

The fastest way to learn the structure is the canonical reference application,
**JustBetweenUs**, which ships a complete app across all five platform heads:

> **https://github.com/ellisnet/JustBetweenUs** — see the `CodeBrixPlatform/`
> folder (on the `main` branch).

A typical head's startup looks like:

```csharp
using CodeBrix.Platform.UI.Hosting;

var host = CodeBrixPlatformHostBuilder.Create()
    .App(() => new App())
    .UseWindowsWin32()   // or UseWindowsWpf / UseLinuxX11 / UseLinuxFrameBuffer / UseMacOS
    .Build();

host.Run();
```

## For AI coding agents

A detailed, machine-oriented guide for setting up a complete CodeBrix.Platform
application is included in **`AGENT-README.txt`** (shipped in the root of every
CodeBrix.Platform NuGet package, and in this repository).

## License

Most CodeBrix.Platform packages are licensed under **Apache-2.0**; the
`SkiaSharp.Views` package is **MIT**. Each package id carries an explicit license
suffix (`.ApacheLicenseForever` or `.MitLicenseForever`).

---

The CodeBrix.Platform codebase is a fork of the Uno Platform (version 6.5.x),
re-licensed and re-packaged under the CodeBrix.Platform name. For complete
third-party attribution, component provenance, and license texts, see the
[`THIRD-PARTY-NOTICES.txt`](THIRD-PARTY-NOTICES.txt) file in this repository (and
in the root of every CodeBrix.Platform NuGet package).
