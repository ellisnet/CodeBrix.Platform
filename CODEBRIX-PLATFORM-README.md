# The CodeBrix Family of NuGet Packages

## 1. About CodeBrix and CodeBrix.Platform

CodeBrix is a family of open-source .NET libraries and application-framework packages, published on nuget.org under the Owner account **"Ellisnet"**. Every CodeBrix-family package listed in this document has that nuget.org Owner. The packages target .NET 10 and later, and the strong preference across the family is fully managed, cross-platform code that behaves identically on Windows, Linux, and macOS.

The CodeBrix project is built on a few consistent principles:

- **License permanence.** Every package ID carries a `.{license}LicenseForever` suffix that permanently binds that package ID to its open source license. A consumer can never be moved onto different license terms by upgrading a CodeBrix package. Section 3 explains this guarantee in detail.
- **Proven code, kept available.** Many CodeBrix packages are faithful ports or forks of well-known open source libraries — preserved at (or derived from) permissively licensed versions, re-namespaced under CodeBrix, modernized for .NET 10, and actively maintained. Where several upstream ecosystems have moved flagship libraries to commercial licenses, the CodeBrix equivalents remain open source, permanently, under their suffixed package IDs.
- **Documentation for humans and tooling alike.** Every package's source repository carries the same trio of documentation files (see Section 2), including a comprehensive `AGENT-README.txt` that explains exactly how to consume the package, with working examples and known pitfalls.

The family has three parts, and they fit together like this:

1. **The CodeBrix.Platform UI framework.** A cross-platform UI application framework for .NET 10: you write your application once against the WinUI XAML API surface (the same `Microsoft.UI.Xaml.*` controls, XAML markup, and data binding used in Windows App SDK apps), and it renders natively on Windows, Linux, and macOS desktops through a Skia-based rendering engine. An application is structured as one shared core library and UI project plus one thin "head" executable per target platform. These packages are produced by the [CodeBrix.Platform repository](https://github.com/ellisnet/CodeBrix.Platform) and are cataloged in Section 5.1.
2. **Native-framework toolkits.** Separate, smaller package families — `CodeBrix.Platform.WinUI.*`, `CodeBrix.Platform.WPF.*`, and `CodeBrix.Platform.Mobile.*` — that are helper toolkits (MVVM foundation, plus Skia-rendered image and Lottie controls for WinUI) for applications built on Microsoft's *own* UI frameworks: WinUI 3 / Windows App SDK, WPF, and .NET MAUI. They are not part of the cross-platform framework above and share no build-time code with it. These are cataloged in Section 5.2.
3. **General-purpose libraries.** A broad set of standalone libraries — imaging, audio, video processing, PDF creation and rasterization, compression, HTML/CSS/SVG/YAML parsing, templating, terminal emulation, Excel files, Python interop, assembly manipulation, testing tools, and more — usable in any .NET 10 application, with or without either UI-framework family. A handful of `CodeBrix.Platform.*`-named packages in this group (fonts, ICU/Unicode binaries, core extensions) exist primarily as companions to the CodeBrix.Platform UI framework. These are cataloged in Section 5.3.

Section 5 — the package catalog — is the point of this document: it is the authoritative list of current CodeBrix-family NuGet packages. A CodeBrix-named package that does not appear in that list (or that is not owned by "Ellisnet" on nuget.org) is not part of the family.

## 2. Where to Find Documentation

Every CodeBrix package comes from a source repository that carries the same three documentation files:

- **`README.md`** — the human-facing overview of the package (also displayed on nuget.org and GitHub).
- **`AGENT-README.txt`** — a comprehensive how-to-use guide: API reference, installation, working code examples, architectural guidance, and common pitfalls. This is the most complete usage documentation for each package, and the recommended starting point when writing code against it.
- **`THIRD-PARTY-NOTICES.txt`** — the complete provenance record for any open source code and asset files incorporated into the repository, along with all software licensing information.

For most packages, these three files live at the **root of the package's source repository** (all repositories live under `https://github.com/ellisnet/`; each catalog entry in Section 5 links to the right one).

**One exception to note:** the **Native-framework toolkit** NuGet packages described above (`CodeBrix.Platform.WinUI.*`, `CodeBrix.Platform.WPF.*`, and `CodeBrix.Platform.Mobile.*`), which are produced from the `src-platforms` folder of the CodeBrix.Platform repository, have their own `README.md`, `AGENT-README.txt`, and `THIRD-PARTY-NOTICES.txt` files inside their family subfolders:

- [`src-platforms/Platform.WinUI/`](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.WinUI)
- [`src-platforms/Platform.WPF/`](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.WPF)
- [`src-platforms/Platform.Mobile/`](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.Mobile)

The files at the *root* of the CodeBrix.Platform repository document the cross-platform **CodeBrix.Platform UI framework** packages (Section 5.1), not these native toolkit families.

**Practical guidance:** before writing code against any CodeBrix package, fetch and read its `AGENT-README.txt` from the repository linked in the catalog below. Raw file content can be fetched directly, for example:

```
https://raw.githubusercontent.com/ellisnet/CodeBrix.Imaging/main/AGENT-README.txt
```

## 3. The `.{license}LicenseForever` Package ID Guarantee

Every CodeBrix-family NuGet package ID ends with a suffix that names its open source license — for example `.MitLicenseForever`, `.ApacheLicenseForever`, `.MsplLicenseForever`, `.LgplLicenseForever`, `.BsdLicenseForever`, or `.OflLicenseForever`.

That suffix is a **permanent guarantee from the maintainer of the CodeBrix packages: a package with that exact package ID will never, ever have its license change.** If you add a dependency on `CodeBrix.Imaging.ApacheLicenseForever`, you will never go to upgrade that package — under that same package ID — and find that the newest version carries a different license (for example, a commercial license). Every version ever published under a `.{license}LicenseForever` package ID carries the license named in the suffix. The license terms you accepted on day one are the license terms of every version that package ID will ever offer.

This guarantee exists because the opposite has recently happened elsewhere in the NuGet ecosystem: well-known, heavily used packages kept their package IDs while their newest versions quietly switched to a different license. The `.{license}LicenseForever` suffix makes that scenario impossible for CodeBrix packages — the license is part of the package's identity.

**An unambiguous disclaimer, so the scope of the guarantee is clear:** the license of the *source code* behind a CodeBrix package could change in the future — no promise is made that a library's code will remain under its current license forever. The specific guarantee is that the license of a given *package ID* will never change. If the source code behind a package were ever relicensed, new versions could **not** be published under the old package ID; they would have to be released under a new package ID whose suffix names the new license. For example, if the code behind `CodeBrix.Imaging.ApacheLicenseForever` were ever moved to the LGPL license, new versions would have to be released as `CodeBrix.Imaging.LgplLicenseForever` — and the `CodeBrix.Imaging.ApacheLicenseForever` package ID would remain locked, forever, to the Apache license, with its already-published versions remaining available as-is. These two statements are two sides of the same mechanism: because the license is baked into the package ID, any future relicensing is forced into the open, and consumers pinned to a suffixed package ID are always protected.

Note that the suffix appears in **package IDs only** — namespaces do not carry it. For example, the package `CodeBrix.Platform.ApacheLicenseForever` provides namespaces such as `CodeBrix.Platform.UI.*` and `Microsoft.UI.Xaml.*`.

## 4. Which Package Family Do I Need?

Choose by the kind of application you are building:

- **A cross-platform desktop application (Windows, Linux, and/or macOS) from one shared codebase** → use the **CodeBrix.Platform framework family** (Section 5.1). You write WinUI XAML once; the framework renders it via Skia on every target. Your app's core library references `CodeBrix.Platform.ApacheLicenseForever` (plus optional extension packages), and each per-platform head executable references exactly one platform head package (`CodeBrix.Platform.Runtime.Skia.*`).
- **A native WinUI 3 / Windows App SDK application** → use the **`CodeBrix.Platform.WinUI.*` toolkit family** (Section 5.2): the Core MVVM toolkit, plus the Skia and Lottie packages for vector-crisp SVG images and Lottie animation playback.
- **A WPF application** → use the **`CodeBrix.Platform.WPF.*` toolkit family** (Section 5.2).
- **A .NET MAUI application** → use the **`CodeBrix.Platform.Mobile.*` toolkit family** (Section 5.2).
- **Any .NET 10 application that needs a specific capability** (image processing, PDF generation, audio files, video processing, HTML parsing, templating, testing, and so on) → pick the matching **general-purpose library** from Section 5.3. These are UI-framework-agnostic.

Rules to follow:

- **Do not mix the two UI-framework families in one application head.** The CodeBrix.Platform framework packages (Section 5.1) *are* the UI framework — they provide the entire WinUI XAML implementation. The native toolkit packages (Section 5.2) assume Microsoft's own UI stack (WinUI 3, WPF, or MAUI) is providing the UI framework. A given application head (executable) uses one family or the other, never both.
- The Section 5.1 and 5.2 families share an identical "Simple" MVVM API across the Skia-based framework, WinUI, WPF, and MAUI — so an application shipping the CodeBrix.Platform Skia-based heads and/or the WinUI, WPF, and MAUI native heads can share its view models across all heads (requires adding the matching Core package in the native heads).
- Several `CodeBrix.Platform.*`-named packages in Section 5.3 (the font packages, the Unicode/ICU packages, `CodeBrix.Platform.Extensions`) exist primarily to support the CodeBrix.Platform framework; others in that group (`CodeBrix.Platform.MediaPlayerCore`, `CodeBrix.Platform.LinuxDBus`, `CodeBrix.Platform.OpenGL`) are fully usable on their own in any .NET 10 application.

### Reference applications

Two GitHub repositories contain real, working, permissively licensed open source applications that consume CodeBrix-family packages — the best place to see correct project structure, package references, and initialization code:

- **[JustBetweenUs](https://github.com/ellisnet/JustBetweenUs)** — the canonical reference application for the CodeBrix.Platform framework: one shared codebase with Windows (Win32 and WPF-hosted), Linux (X11, native Wayland, and framebuffer), and macOS heads, demonstrating the `.Core` + `.UI` + per-platform-head architecture end to end.
- **[CodeBrix.Samples](https://github.com/ellisnet/CodeBrix.Samples)** — sample applications demonstrating CodeBrix-family packages in use.

## 5. The CodeBrix Package Catalog

This is the authoritative list of the current CodeBrix-family NuGet packages. For each package: its name, its full NuGet package ID (how it is listed on nuget.org), the source repository, and a summary of what it does. For deeper usage documentation, read the `AGENT-README.txt` in the linked repository (see Section 2).

### 5.1 The CodeBrix.Platform framework family

All packages in this group are produced by the [CodeBrix.Platform repository](https://github.com/ellisnet/CodeBrix.Platform) and are documented by the `README.md` / `AGENT-README.txt` / `THIRD-PARTY-NOTICES.txt` files at that repository's root. They are listed in dependency order, most foundational first. All framework packages in a given release share one version and are published together.

---

**CodeBrix.Platform**
NuGet Package ID: `CodeBrix.Platform.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The core cross-platform UI framework, and the one required package for every CodeBrix.Platform Skia-based-UI application. It provides the WinUI XAML API surface — the `Microsoft.UI.Xaml.*` control set, the XAML runtime, layout, data binding, dispatching, and logging integration — rendered through a Skia-based engine on Windows, Linux, and macOS desktops. The package is self-contained (it folds in the Foundation, WinRT, dispatching, and logging-adapter assemblies), so a single reference in an application's core library delivers the full framework. It requires .NET 10, and is consumed alongside exactly one platform head package per target platform (see the `CodeBrix.Platform.Runtime.Skia.*` packages below). The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.SkiaSharp.Views**
NuGet Package ID: `CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

Provides the SkiaSharp XAML view types — `SKXamlCanvas` and `SKSwapChainPanel` — for hosting SkiaSharp-drawn content inside CodeBrix.Platform XAML. It is used internally by the Graphics2DSK, Lottie, and Svg extension packages; reference it directly only if your own code uses these view types. Unlike the rest of the family, this package is versioned to track the SkiaSharp release it vendors rather than the shared framework version. The code for this package was derived from a small portion of the open source SkiaSharp library (its XAML view components), version 4.148.0.

---

**CodeBrix.Platform.Graphics2DSK**
NuGet Package ID: `CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing an immediate-mode 2D drawing surface backed by SkiaSharp, for custom drawing inside CodeBrix.Platform XAML. It is referenced in an application's core library alongside the core framework package, and works on every platform head. Use it when your application needs to render custom 2D graphics directly rather than composing standard XAML controls. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Lottie**
NuGet Package ID: `CodeBrix.Platform.Lottie.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing Lottie vector-animation playback in CodeBrix.Platform XAML, rendered through the Skottie engine. It is referenced in an application's core library and paired with the standard `SkiaSharp.Skottie` package (and the `CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever` package), giving smooth, resolution-independent animation playback on every platform head. Use it to play Lottie/Bodymovin JSON animations exported from tools such as After Effects. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Svg**
NuGet Package ID: `CodeBrix.Platform.Svg.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing SVG image support (`SvgImageSource`) on the Skia platform heads. It is referenced in an application's core library and paired with the `CodeBrix.SkiaSvg.MitLicenseForever` package, which supplies the underlying SVG parsing and Skia rendering. Use it to display scalable vector images in XAML with crisp results at any display resolution. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Runtime.Skia**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The base Skia runtime layer that every platform head package builds on, providing the shared windowing and rendering host infrastructure that the per-platform head packages specialize. Application projects never reference this package directly — it flows in transitively beneath whichever head package a head project references. It is published so that the head packages restore correctly, and it is listed here so the complete authorized package set is visible. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Runtime.Skia.Win32**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for Windows desktop applications hosted in a Win32 window — the simplest and most common choice for targeting Windows. A Windows head project references exactly this one head package (plus the application's core library) and bootstraps with `.UseWindowsWin32()`. Choose this head unless you specifically need to host CodeBrix.Platform content inside a WPF application context. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Runtime.Skia.Wpf**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for hosting CodeBrix.Platform content inside a WPF desktop application context on Windows. A WPF head differs from the other heads in a few documented ways: it targets `net10.0-windows`, it must not set `UseWPF` (WPF is loaded by the host at runtime), it bootstraps with `.UseWindowsWpf()`, and forcing software rendering after host construction is recommended to avoid rendering conflicts. For a plain Windows desktop app, prefer the Win32 head instead. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Runtime.Skia.X11**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The broad-compatibility platform head package for desktop Linux: it runs on X11 desktops and also on Wayland desktops through XWayland (the X11 compatibility layer). A Linux head project references this package and bootstraps with `.UseLinuxX11()`. Ship this head for maximum desktop-Linux reach — alone, or alongside a native Wayland head. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Runtime.Skia.Wayland**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for a pure, native Wayland client on desktop Linux: it speaks the Wayland protocol directly and never uses X11 or XWayland. It requires a Wayland compositor, and fails fast with a clean error when none is present (it never falls back to X11); the head bootstraps with `.UseLinuxWayland()`. Rendering is GPU-accelerated Vulkan by default, falling back automatically to shared-memory software rendering when Vulkan is unavailable; an OpenGL ES (EGL) path, a software-only mode, and a no-fallback `VulkanForced` mode can be selected in code (`RenderingBackend(...)` on the head builder) or via environment variables (`CODEBRIX_WAYLAND_NO_GPU=1`, `CODEBRIX_WAYLAND_USE_EGL=1`). Flyout-based popups (ComboBox dropdowns, MenuFlyout, ToolTip, and similar controls), rich clipboard formats (plain text, HTML, PNG images, and file lists), fractional display scaling, custom title bars, and window activation all work, at parity with the X11 head; accepting drag-and-drop from other applications is implemented, but delivery depends on the compositor (some experimental Wayland desktops send unusable drag coordinates). Remaining gaps: touch input, native-view hosting in XAML, native OpenGL interop, and initiating drags from the application are not yet implemented (the last is missing on the X11 head too); programmatic window positioning and resizing and always-on-top are unavailable by Wayland protocol design (each logs a one-time warning naming the API); window icons come from a .desktop file rather than the app manifest; and IME text input is not yet available on either Linux head. Prefer the X11 head if your application depends on touch input or native-view hosting today. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Runtime.Skia.FrameBuffer**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for Linux framebuffer targets — embedded and kiosk devices with no X11 or desktop environment at all. The same shared application code runs unchanged; the head project simply references this package and bootstraps with `.UseLinuxFrameBuffer()`. Use it to put a full XAML UI on dedicated-purpose Linux hardware. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.Runtime.Skia.MacOS**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for macOS desktop applications, bootstrapped with `.UseMacOS()`. The package contains a small native library shipped as a universal binary, so applications run on both Apple Silicon and Intel Macs. As with the other heads, a macOS head project references exactly this one head package plus the application's core library. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.WebView**
NuGet Package ID: `CodeBrix.Platform.WebView.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional add-on package that makes the XAML `WebView2` control work on every platform head with a single reference in the application's core library. Its real delivery is Linux: on the X11, Wayland, and FrameBuffer heads, web content is rendered offscreen by the system-installed WPE WebKit engine and composited directly into the Skia scene — no native child windows, no airspace problems, and clipping, transforms, and z-order behave like any other XAML content. On the Windows, WPF, and macOS heads — which have built-in WebView support via Microsoft Edge WebView2 and WKWebView — the package is inert and harmless to reference. No engine binaries ship in the package; Linux machines must have the system WPE WebKit packages installed, and a missing engine produces a clear exception naming the exact install command. Custom User-Agent strings and page-to-host messaging (both the WebView2 and WebKit JavaScript idioms) are supported on every head. The code for this package was derived from the open source library Uno Platform version 6.5.x.

---

**CodeBrix.Platform.MediaPlayer**
NuGet Package ID: `CodeBrix.Platform.MediaPlayer.LgplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional add-on package that brings the XAML `MediaPlayerElement` (audio and video playback) to the Win32, WPF, X11, Wayland, and FrameBuffer heads with a single reference in the application's core library. LibVLC decodes media into memory and the frames are composited directly into the Skia scene — no native child windows, no airspace problems, and the native-Wayland head stays a pure Wayland client. The package is inert on the macOS head, which has built-in AVFoundation media support and needs neither this package nor libvlc. The native libvlc runtime is not shipped in the package: on Linux it is installed via the system package manager, and on Windows the `VideoLAN.LibVLC.Windows` package is added to the Windows head project(s). The code for this package was derived from the open source library Uno Platform version 6.5.x, with playback delivered through the `CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever` package (see Section 5.3).

---

### 5.2 The native-framework toolkit families

These packages are produced from the `src-platforms` folder of the [CodeBrix.Platform repository](https://github.com/ellisnet/CodeBrix.Platform). They are helper toolkits for applications built on Microsoft's own UI frameworks — they are **not** part of, and must not be mixed with, the cross-platform framework in Section 5.1. Each family's documentation lives in its own subfolder (see Section 2). The three families share an identical "Simple" MVVM API — which the Section 5.1 framework also provides — so view models can be shared across CodeBrix.Platform Skia-based, WinUI, WPF, and MAUI heads of the same application. Within the WinUI family the dependency direction is: Lottie → Skia → Core.

---

**CodeBrix.Platform.WinUI**
NuGet Package ID: `CodeBrix.Platform.WinUI.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform — src-platforms/Platform.WinUI](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.WinUI)

The CodeBrix "Simple" MVVM toolkit for native WinUI 3 / Windows App SDK applications, and the foundation of the WinUI toolkit family. It provides `SimpleViewModel` (an `INotifyPropertyChanged` base class with attribute-driven cascading notifications), `SimpleCommand` (an `ICommand` supporting sync and async handlers with main-thread marshalling), `SimpleDialog` (ContentDialog-backed dialogs), `SimpleMessaging` (weak-reference pub/sub), `SimpleServiceResolver` (a .NET Generic Host dependency-injection wrapper with auto-registration scanning), plus `SimpleEnum` and `SimpleOsInfo` helpers. Its dependency-injection and hosting dependencies are abstractions-only — the consuming application owns the concrete Generic Host reference. It suits WinUI 3 apps that want a lightweight, opinionated MVVM + DI + messaging foundation without pulling in a heavy framework.

---

**CodeBrix.Platform.WinUI.Skia**
NuGet Package ID: `CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform — src-platforms/Platform.WinUI](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.WinUI)

Adds Skia-rendered image controls to native WinUI 3 / Windows App SDK applications: `EmbeddedImage` and `EmbeddedImageButton`, with an `embedded://` URI scheme for loading images directly from embedded assembly resources (alongside `ms-appx` URIs). Its headline capability is vector-direct SVG rendering: SVG images are drawn as vectors at full display resolution with no intermediate rasterization, producing crisp, resolution-independent results — pixel-for-pixel matching the SVG output of the cross-platform CodeBrix.Platform framework. It depends on the `CodeBrix.Platform.WinUI.ApacheLicenseForever` core package. Portions of the code for this package were derived from the open source Uno Platform (via the Uno-based CodeBrix.Platform sources in the same repository).

---

**CodeBrix.Platform.WinUI.Lottie**
NuGet Package ID: `CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform — src-platforms/Platform.WinUI](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.WinUI)

A Lottie animation player for native WinUI 3 / Windows App SDK applications, rendered with the SkiaSharp Skottie engine rather than the Windows-native Composition/Win2D pipeline. Because the stock Windows App SDK `AnimatedVisualPlayer` requires Composition/Win2D animation sources, this package ships its own `AnimatedVisualPlayer` control hosting a Skia render surface, along with `LottieVisualSource` and `ThemableLottieVisualSource`. It supports `embedded://`, `ms-appx:///`, and `ms-appdata:///` URI schemes, a Play/Stop/Pause/Resume/SetProgress playback API, and runtime color theming of animations. It renders animations identically to the cross-platform CodeBrix.Platform Lottie package, and depends on both the WinUI Skia and Core packages. Portions of the code for this package were derived from the open source Uno Platform and Lottie-Windows (Windows Community Toolkit) libraries.

---

**CodeBrix.Platform.WPF**
NuGet Package ID: `CodeBrix.Platform.WPF.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform — src-platforms/Platform.WPF](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.WPF)

The CodeBrix "Simple" MVVM toolkit compiled for WPF applications: the same `SimpleViewModel`, `SimpleCommand`, `SimpleDialog`, `SimpleMessaging`, `SimpleServiceResolver`, `SimpleEnum`, and `SimpleOsInfo` surface as the WinUI and MAUI editions, with platform specifics adapted to WPF (MessageBox-backed dialogs, Dispatcher-based thread marshalling, WPF-correct visibility semantics, and designer-mode detection). Dependency-injection and hosting dependencies are abstractions-only, with the application supplying the concrete Generic Host. Because the API is identical across the three toolkit families (and the Section 5.1 framework), view models written against this package can be shared with CodeBrix.Platform Skia-based, WinUI, and MAUI heads of the same application.

---

**CodeBrix.Platform.Mobile**
NuGet Package ID: `CodeBrix.Platform.Mobile.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform — src-platforms/Platform.Mobile](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.Mobile)

The .NET MAUI edition of the CodeBrix "Simple" MVVM toolkit, offering the same view model, command, dialog, messaging, dependency-injection, enum, and OS-information API as the WinUI and WPF editions. MAUI-specific behavior includes dialogs backed by `Page.DisplayAlert`, main-thread marshalling, and device model/manufacturer information where the platform provides it. Dependency-injection and hosting dependencies are abstractions-only, with the application owning the concrete Generic Host. It targets MAUI apps that want to share view models with CodeBrix.Platform Skia-based, WinUI, and WPF siblings using one consistent MVVM API.

---

### 5.3 General-purpose CodeBrix library packages

Standalone libraries usable in any .NET 10 application. Each comes from its own repository under `https://github.com/ellisnet/`, with documentation at the repository root (see Section 2). The `CodeBrix.Platform.*`-named packages are listed first; packages produced from the same repository are grouped together.

---

**CodeBrix.Platform.Extensions**
NuGet Package ID: `CodeBrix.Platform.Extensions.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Extensions](https://github.com/ellisnet/CodeBrix.Platform.Extensions)

A class library bundling a set of proven low-level helper libraries into a single CodeBrix-owned assembly: general-purpose extensions (string, memoization, stream, URI, weak references), collection helpers, a rich disposables toolkit (`CompositeDisposable`, `SerialDisposable`, `RefCountDisposable`, and more), equality/comparison builders, logging helpers, and threading primitives (`FastAsyncLock`, `AsyncEvent`, transactional updates). It exists so that CodeBrix.Platform can take one dependency instead of a fan-out of several small upstream packages, but the helpers are equally usable in any .NET 10 project. Namespaces root at `CodeBrix.Platform.Extensions.*`. The code for this package was derived from the open source library Uno.Core.Extensions version 4.1.1 (seven of its projects, merged into one assembly).

---

**CodeBrix.Platform.Fonts.Fluent**
NuGet Package ID: `CodeBrix.Platform.Fonts.Fluent.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Fonts.Fluent](https://github.com/ellisnet/CodeBrix.Platform.Fonts.Fluent)

A redistribution of the Fluent icon font (Windows 11 iconography) for CodeBrix.Platform applications, providing the default symbols font used by `SymbolIcon`, `FontIcon`, and the `SymbolThemeFontFamily` theme resource. The assembly is metadata-only with no managed API; the payload is the icon font file plus a buildTransitive MSBuild `.props` that automatically registers it as the default symbols font in consuming apps (with an opt-out property). Fonts are referenced via `ms-appx:///CodeBrix.Platform.Fonts.Fluent/Fonts/...` URIs. The code for this package was derived from the open source library Uno.Fonts.Fluent version 2.8.1.

---

**CodeBrix.Platform.Fonts.OpenSans**
NuGet Package ID: `CodeBrix.Platform.Fonts.OpenSans.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Fonts.OpenSans](https://github.com/ellisnet/CodeBrix.Platform.Fonts.OpenSans)

A redistribution of the Open Sans font family as a content-only NuGet package for CodeBrix.Platform applications — commonly used as the application's default text font. It ships a variable font covering weights 300–800 plus 36 static instances across weights, styles, and stretches, together with a font manifest and a buildTransitive MSBuild `.targets` that prunes redundant static fonts at consumer-build time while always keeping the variable font. Fonts are referenced via `ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/...` URIs, or registered framework-wide via `FeatureConfiguration.Font.DefaultTextFontFamily`. The code for this package was derived from the open source library Uno.Fonts.OpenSans version 2.8.1.

---

**CodeBrix.Platform.Fonts.Roboto**
NuGet Package ID: `CodeBrix.Platform.Fonts.Roboto.OflLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Fonts.Roboto](https://github.com/ellisnet/CodeBrix.Platform.Fonts.Roboto)

A redistribution of the Roboto font family, structured like the sibling OpenSans package: a variable `Roboto.ttf` covering the full weight and width axes plus 36 static instances, a font manifest, and a buildTransitive MSBuild `.targets` that prunes redundant static fonts at consumer-build time while always keeping the variable font. It is designed for CodeBrix.Platform applications (referenced via `ms-appx:///CodeBrix.Platform.Fonts.Roboto/Fonts/...` URIs or set as the default text font) and is equally usable as a plain content-files NuGet in any .NET 10 project. The assembly is metadata-only with no managed API. The fonts are the open source Roboto family published by Google.

---

**CodeBrix.Platform.LinuxDBus**
NuGet Package ID: `CodeBrix.Platform.LinuxDBus.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.LinuxDBus](https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus)

A fully managed, low-level D-Bus protocol library for Linux that speaks the D-Bus wire protocol directly: connecting to the session bus, system bus, or any D-Bus transport; sending and receiving messages; subscribing to signals; and registering method handlers to expose D-Bus objects. The primary API is the `Connection` class, which preserves the upstream public API shape so migration is a namespace change. It requires a Linux runtime with a running D-Bus daemon, and has no NuGet dependencies beyond the shared framework. The code for this package was derived from the open source library Tmds.DBus.Protocol version 0.21.3.

---

**CodeBrix.Platform.MediaPlayerCore**
NuGet Package ID: `CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.MediaPlayerCore](https://github.com/ellisnet/CodeBrix.Platform.MediaPlayerCore)

A fully managed, cross-platform audio/video media-player library that wraps the native libvlc dynamic library, exposing high-level managed classes: `LibVLC`, `Media`, `MediaPlayer`, `MediaList`, media and renderer discovery (Chromecast/UPnP), `Equalizer`, and a UI-agnostic media-element management layer. A notable CodeBrix addition is `VideoFrameSink`, which renders video frames into CPU memory and raises per-frame BGRA events — enabling windowing-system-agnostic video rendering on hosts with no window-embedding API (this is what powers the CodeBrix.Platform MediaPlayer add-on). The native libvlc runtime must be present at run time (a NuGet package on Windows, system packages on Linux, VLC on macOS). The code for this package was derived from the open source library LibVLCSharp version 3.9.7.

---

**CodeBrix.Platform.OpenGL**
NuGet Package ID: `CodeBrix.Platform.OpenGL.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.OpenGL](https://github.com/ellisnet/CodeBrix.Platform.OpenGL)

A fully managed, cross-platform OpenGL bindings library whose public API is signature-identical to Silk.NET.OpenGL, with only namespace renames. The main entry point is the `GL` class — constructed over a native context (for example `opengl32` on Windows or `libGL.so` on Linux) — with a method for every OpenGL core-profile entry point, plus the ported native-loader infrastructure and math types. The upstream's build-time source generator is eliminated: its generated interop code is pre-captured and committed as static source, so consumers need no source-generator tooling. OpenGL extensions, OpenGL ES, and legacy profiles are out of scope for the current version, and actual GL calls require a live GL context at runtime. The code for this package was derived from the open source library Silk.NET.OpenGL version 2.23.0 (with its Silk.NET.Core and Silk.NET.Maths dependencies).

---

**CodeBrix.Platform.Unicode**
NuGet Package ID: `CodeBrix.Platform.Unicode.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Unicode](https://github.com/ellisnet/CodeBrix.Platform.Unicode)

A redistribution of the ICU (International Components for Unicode) version 77 native binaries for Windows, packaged for CodeBrix.Platform applications. The assembly is metadata-only; the payload is the ICU native DLLs for both win-x64 and win-arm64, plus the full ICU data archive (Unicode character properties, CLDR locale data, collation, normalization, BiDi, time zones, and more). A buildTransitive MSBuild `.targets` automatically embeds the data archive in consumer builds, with a shared sentinel ensuring it is embedded exactly once even when the macOS sibling package is also present. The code for this package was derived from the open source library Uno.icu-win version 77.2.1.

---

**CodeBrix.Platform.UnicodeMacOs**
NuGet Package ID: `CodeBrix.Platform.UnicodeMacOs.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Unicode](https://github.com/ellisnet/CodeBrix.Platform.Unicode)

The macOS counterpart to CodeBrix.Platform.Unicode, built from the same repository: a redistribution of the ICU version 77 native binaries for macOS. The metadata-only assembly ships two universal (x86_64 + arm64) dylibs plus the same ICU data archive as the Windows package, with the same buildTransitive auto-embed mechanism — installing both OS packages in one build embeds the data archive only once. The code for this package was derived from the open source library Uno.icu-macos version 77.2.1.

---

**CodeBrix.ArgumentParser**
NuGet Package ID: `CodeBrix.ArgumentParser.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.ArgumentParser](https://github.com/ellisnet/CodeBrix.ArgumentParser)

A fully managed, cross-platform command-line option parser with no dependencies beyond .NET itself. It provides Getopt::Long-style option parsing supporting short, long, and Windows-style option prefixes, typed option callbacks, multi-value options, option bundling, and response-file (`@file`) expansion, plus a Command/CommandSet model for building git-style multi-command CLI suites with automatic help generation. Response-file handling is security-hardened with cycle detection, nesting-depth caps, and strict quote handling. The code for this package was derived from the open source library Mono.Options version 6.12.0.148.

---

**CodeBrix.AssemblyTools**
NuGet Package ID: `CodeBrix.AssemblyTools.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.AssemblyTools](https://github.com/ellisnet/CodeBrix.AssemblyTools)

A library giving full programmatic read/write/rewrite access to managed .NET assemblies — modules, types, methods, fields, properties, events, custom attributes, IL, and debug symbols (portable PDB, native PDB, Mono MDB). Its API surface matches Mono.Cecil essentially one-to-one with only the namespace prefix changed, so migration is a find-and-replace of `using` directives. It ships as a single merged assembly combining what upstream shipped as four packages (core, Rocks extension helpers, and the two symbol providers); key entry points are `AssemblyDefinition.ReadAssembly`, `ModuleDefinition.ReadModule`, and `ILProcessor` for IL rewriting. The code for this package was derived from the open source library Mono.Cecil version 0.11.6.

---

**CodeBrix.Audio**
NuGet Package ID: `CodeBrix.Audio.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Audio](https://github.com/ellisnet/CodeBrix.Audio)

A fully managed, cross-platform audio-file library with no native code or platform interop, behaving identically on Windows, macOS, and Linux. It reads WAV and MP3 audio (MP3 decoding is fully managed — no OS codec needed), writes WAV, reads MP3 ID3v2 metadata tags, and reads/writes Standard MIDI Files with a full MIDI event hierarchy. It also exposes DSP analysis primitives: FFT, biquad filters, an envelope follower, and an energy-based voice-activity detector. Audio-device playback/recording, resampling, and synthesis are explicit non-goals — this is a file and signal-analysis library. The code for this package was derived from the open source libraries NAudio version 3.0.0-preview and NLayer version 1.16.0.

---

**CodeBrix.Compression**
NuGet Package ID: `CodeBrix.Compression.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Compression](https://github.com/ellisnet/CodeBrix.Compression)

A library for creating, reading, updating, and extracting compressed archives in Zip, GZip, Tar, and BZip2 formats, with zero external dependencies beyond .NET. Zip support is the most complete and includes encryption (AES-128, AES-256, ZipCrypto) and Zip64 extensions for archives over 4 GB; GZip, Tar, and BZip2 support create/read/extract. It handles streaming (non-seekable) input and output, in-memory archive operations, checksums, Unicode filenames, and path-traversal attack prevention. The API mirrors SharpZipLib closely, with namespaces renamed to `CodeBrix.Compression`. The code for this package was derived from the open source library SharpZipLib version 1.4.2.

---

**CodeBrix.Imaging**
NuGet Package ID: `CodeBrix.Imaging.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Imaging](https://github.com/ellisnet/CodeBrix.Imaging)

A fully managed, cross-platform 2D image-processing and font-handling library with zero external dependencies. It reads and writes BMP, GIF, JPEG, PBM, PNG, TGA, TIFF, and WebP with format auto-detection, and provides processing operations (resize, crop, mutation pipelines, and more), strongly typed pixel formats, and drawing/text rendering. It can construct images from raw pixel buffers — including a dedicated SIMD-optimized path for BGRA output from native renderers such as PDFium or Direct2D — which makes it the image backbone for several other CodeBrix packages (PDF, video processing, Excel). The API mirrors SixLabors.ImageSharp, with all namespaces under `CodeBrix.Imaging`. The code for this package was derived from the open source libraries SixLabors.ImageSharp version 2.1.3 and SixLabors.Fonts version 1.0.0.

---

**CodeBrix.MarkupParse**
NuGet Package ID: `CodeBrix.MarkupParse.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.MarkupParse](https://github.com/ellisnet/CodeBrix.MarkupParse)

A fully managed, cross-platform HTML parsing and DOM manipulation library with zero external dependencies. It parses HTML from strings, streams, or URLs into a fully navigable DOM tree, queryable via CSS selectors (`QuerySelector`/`QuerySelectorAll`) or LINQ, with full traversal and manipulation of nodes, attributes, classes, and text content. It serializes the DOM back to HTML with standard, pretty-printed, minified, or XHTML formatters, and supports fragment parsing, source-position tracking, async URL loading with cookies, and forms. It is deliberately HTML-to-DOM only — no CSS evaluation, JavaScript execution, or rendering. The code for this package was derived from the open source library AngleSharp version 7.1.0.

---

**CodeBrix.PdfDocuments**
NuGet Package ID: `CodeBrix.PdfDocuments.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.PdfDocuments](https://github.com/ellisnet/CodeBrix.PdfDocuments)

A low-level, pure managed PDF library for creating, reading, merging, and manipulating PDF documents using direct graphics drawing via the XGraphics API (`DrawString`, `DrawImage`, shape drawing, and wrapped-text formatting). It supports document metadata, page sizing and orientation, fonts with styles, embedding PNG/JPEG/BMP/WebP/GIF images (including images processed via CodeBrix.Imaging), and opening existing PDFs for modification or page import and merging. It is the foundation package of the repository's PDF trio — the PdfDocCreate and PdfRasterizer packages build on it. Use it when you need fine-grained control over page layout and drawing, or to work with existing PDF files. The code for this package was derived from the open source library PdfSharpCore version 1.3.67.

---

**CodeBrix.PdfDocCreate**
NuGet Package ID: `CodeBrix.PdfDocCreate.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.PdfDocuments](https://github.com/ellisnet/CodeBrix.PdfDocuments)

A high-level document object model for building richly formatted PDFs: structured documents composed of sections, paragraphs, styles, tables, charts, images, and headers/footers, rendered to PDF via `PdfDocumentRenderer`. Choose it over the lower-level CodeBrix.PdfDocuments package when you want to describe a document declaratively with a structured model rather than drawing at coordinates — the two can also be used together. Installing it automatically brings in CodeBrix.PdfDocuments (on which it is built) plus the CodeBrix.Imaging and CodeBrix.Compression packages. The code for this package was derived from the open source library MigraDocCore version 1.3.67.

---

**CodeBrix.PdfRasterizer**
NuGet Package ID: `CodeBrix.PdfRasterizer.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.PdfDocuments](https://github.com/ellisnet/CodeBrix.PdfDocuments)

A PDF page rasterizer that renders PDF pages to images (PNG, JPEG, BMP, GIF, TIFF) using the PDFium native rendering engine, through a `PageRasterizer` API that also supports thumbnails, page-dimension queries, and render flags. Pre-built PDFium native binaries are bundled in the package for Windows (x64/x86/ARM64), macOS (x64/ARM64), Linux (x64/ARM64/ARM/RISC-V 64), and Android ARM64 — no separate PDFium install is required, though platforms without a bundled binary (such as iOS and WebAssembly) are not supported. It is the "PDF-to-image" member of the repository's trio and depends on CodeBrix.PdfDocuments and CodeBrix.Imaging; note that PDFium is not thread-safe, so rasterization calls are serialized. The rendering logic for this package was derived from the open source Docnet.Core library, combined with bundled pre-built PDFium binaries.

---

**CodeBrix.Python**
NuGet Package ID: `CodeBrix.Python.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Python](https://github.com/ellisnet/CodeBrix.Python)

A cross-platform Python-to-.NET interoperability library that embeds a CPython interpreter inside a .NET process and marshals objects across the Python/CLR boundary; through the embedded `clr` module, Python code can also load and call .NET assemblies. Core entry points are `PythonEngine` (interpreter lifecycle), `Py.GIL()` for lock acquisition, and `PyObject` with typed wrappers (`PyList`, `PyDict`, and so on) for dynamic dispatch and conversion, plus pluggable encoders/decoders for custom type conversion. It targets scenarios where a .NET application needs to run Python code, use Python libraries, or expose .NET APIs to Python scripts. A discoverable CPython shared library (versions 3.10 through 3.14) is required at run time. The code for this package was derived from the open source library Python.NET (pythonnet) version 3.1.0.

---

**CodeBrix.ServiceLocator**
NuGet Package ID: `CodeBrix.ServiceLocator.MsplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.ServiceLocator](https://github.com/ellisnet/CodeBrix.ServiceLocator)

A shared abstraction over IoC containers and service locators, letting libraries and frameworks resolve services without a hard reference to any specific container. It defines `IServiceLocator` (resolve by type, or type plus string key, with `GetAllInstances`), the static ambient `ServiceLocator.Current` accessor, and the abstract `ServiceLocatorImplBase`, which lets a container adapter implement the full surface by overriding just two template methods; resolution failures are uniformly wrapped in `ActivationException`. It is a drop-in replacement for the CommonServiceLocator package — migration is a namespace change. The code for this package was derived from the open source library CommonServiceLocator version 2.0.7.

---

**CodeBrix.SkiaSvg**
NuGet Package ID: `CodeBrix.SkiaSvg.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.SkiaSvg](https://github.com/ellisnet/CodeBrix.SkiaSvg)

An SVG loading and rendering library built on SkiaSharp, which also loads Android VectorDrawables and renders to SkiaSharp surfaces. Beyond basic rendering via the `SKSvg` entry point, it provides hit testing (point and rectangle, at element and scene-node level), a retained scene graph enabling efficient partial mutations, manually driven animation, pointer interaction, and pluggable typeface providers for headless environments. It exports to raster formats (PNG, JPEG, BMP, GIF, TIFF) and vector formats (SVG, PDF, XPS), and consolidates several upstream companion packages into a single library. It is also the SVG engine behind the CodeBrix.Platform framework's SVG support. The code for this package was derived from the open source library Svg.Skia version 4.2.0.

---

**CodeBrix.StyleSheetParse**
NuGet Package ID: `CodeBrix.StyleSheetParse.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.StyleSheetParse](https://github.com/ellisnet/CodeBrix.StyleSheetParse)

A fully managed, cross-platform CSS stylesheet parsing library that parses CSS text into a strongly typed object model which can be queried, manipulated, and serialized back to CSS. The `StylesheetParser` entry point supports sync and async parsing with configurable tolerance modes, and the resulting model exposes typed collections for style, media, container, import, font-face, page, keyframes, and other rule types. Style declarations offer over one hundred strongly typed CSS properties plus name-based access, and parsed selectors include CSS specificity calculation. It has no dependencies beyond .NET and serves as the CSS engine underneath the CodeBrix SVG libraries. The code for this package was derived from the open source library ExCSS version 4.3.1.

---

**CodeBrix.SvgParse**
NuGet Package ID: `CodeBrix.SvgParse.MsplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.SvgParse](https://github.com/ellisnet/CodeBrix.SvgParse)

A renderer-agnostic SVG document object model library providing comprehensive SVG parsing, element modeling, styling, and serialization. It loads SVG documents from files, streams, strings, or XmlReaders via `SvgDocument`, and exposes a rich object model — visual elements, paint servers (colors, gradients, patterns), path segments, transforms, filter effects, and CSS selector matching — for querying and manipulation. Built-in security controls govern external entity, image, and element resolution (XXE prevention by default). Because it depends on no rendering engine, it can serve as the foundation for any SVG rendering backend; within the CodeBrix family it underpins CodeBrix.SkiaSvg. The code for this package was derived from the open source library Svg.Custom (part of the Svg.Skia project family) version 4.2.0.

---

**CodeBrix.Templating**
NuGet Package ID: `CodeBrix.Templating.BsdLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Templating](https://github.com/ellisnet/CodeBrix.Templating)

A text-templating and scripting-language library that parses and renders templates written in the Scriban and Liquid template languages. The immutable, cacheable `Template` class parses templates and renders them synchronously or asynchronously against models; `TemplateContext` controls evaluation state, including template loaders for includes, culture, strict-variable mode, and safety limits (loop, recursion, and regex timeouts). `ScriptObject` provides dictionary-like model binding with reflection-based import of objects, delegates, and static classes, plus a large built-in function library (string, array, math, date, regex, HTML, and more). It suits code generation, HTML pages, reports, configuration files, and any text produced from a model. The code for this package was derived from the open source library Scriban version 7.1.0.

---

**CodeBrix.Terminal**
NuGet Package ID: `CodeBrix.Terminal.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Terminal](https://github.com/ellisnet/CodeBrix.Terminal)

A .NET terminal emulation engine with Unicode text support: a virtual terminal (VT100/VT220/VT400/xterm-compatible) with a full ANSI/DEC escape-sequence parser, terminal buffer management with scrollback, and Unicode text utilities. Features include cursor and scroll-region control, text attributes, 8/16/256-color support, mouse tracking protocols, alternate screen buffers, terminal resize with reflow strategies, and search/selection services; PTY fork/exec is available on Unix and macOS only. The core `Terminal` class is fed text or bytes and exposes the resulting buffer, making it suitable for building terminal UI controls or headless terminal processing, with zero dependencies beyond the .NET runtime. The code for this package was derived from the open source libraries XtermSharp and NStack version 1.1.1.

---

**CodeBrix.TestMocks**
NuGet Package ID: `CodeBrix.TestMocks.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.TestMocks](https://github.com/ellisnet/CodeBrix.TestMocks)

A single-package .NET testing library that combines mocking and auto-generated test data into one unified dependency with built-in xUnit v3 integration. It provides `Mock<T>` creation for interfaces and classes (loose and strict behavior, setups, returns, callbacks, async support), a rich argument-matcher set, and full call verification, plus an AutoFixture-style `Fixture` for anonymous test data and data-driven xUnit v3 attributes (`[AutoData]`, `[AutoMockData]`, and friends). Its API surfaces mirror Moq and AutoFixture, but all namespaces use the `CodeBrix.TestMocks` prefix, and its only NuGet dependency is the xUnit v3 extensibility core. The code for this package was derived from the open source Moq and AutoFixture libraries.

---

**CodeBrix.VideoProcessing**
NuGet Package ID: `CodeBrix.VideoProcessing.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.VideoProcessing](https://github.com/ellisnet/CodeBrix.VideoProcessing)

A fully managed, cross-platform FFmpeg/FFprobe wrapper that launches the external `ffmpeg`/`ffprobe` executables and parses their output — it is a wrapper, not a codec, and bundles no binaries. It provides media analysis (durations, streams, codecs, resolutions, bitrates), a fluent builder for converting, transcoding, and muxing video and audio, snapshot and GIF extraction, raw-frame and byte-stream piping in and out of FFmpeg, and a bridge between video frames and in-memory images via CodeBrix.Imaging. Progress and log callbacks plus a graceful cancellation model are included. The `ffmpeg` and `ffprobe` executables must be installed and on the PATH (or configured explicitly) at run time. The code for this package was derived from the open source library FFMpegCore version 5.4.0 (with the Instances version 3.0.2 process wrapper vendored in).

---

**CodeBrix.YamlParse**
NuGet Package ID: `CodeBrix.YamlParse.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.YamlParse](https://github.com/ellisnet/CodeBrix.YamlParse)

A fully managed, cross-platform YAML library with no third-party dependencies, offering three layers: a low-level streaming scanner/parser/emitter, an XmlDocument-style representation model (`YamlStream`/`YamlDocument`/`YamlNode`) for loading, editing, and saving documents, and a high-level object serialization layer (`SerializerBuilder`/`DeserializerBuilder`) for reading and writing .NET objects to and from YAML. The builders are fluent, with extensive extension hooks for type converters, node deserializers, and naming conventions (camelCase, PascalCase, hyphenated, and more). The API matches YamlDotNet's shape under `CodeBrix.YamlParse` namespaces. The code for this package was derived from the open source library YamlDotNet version 18.1.0.

---

**FreePPlus**
NuGet Package ID: `FreePPlus.LgplLicenseForever`
Source: [github.com/ellisnet/FreePPlus](https://github.com/ellisnet/FreePPlus)

A .NET library that reads and writes Excel (`.xlsx`) files using the Office Open XML format, with no need for Microsoft Excel or COM interop. Feature coverage is broad: cell values and ranges, styling, data validation, conditional formatting, charts, pictures, shapes, comments, tables, pivot tables, a formula calculation engine, AutoFilter, merged cells, rich text, sparklines, workbook/worksheet protection, AES password encryption, and VBA macro support. It keeps the same `OfficeOpenXml` namespaces as EPPlus 4.x, so existing EPPlus 4.x code works with minimal changes beyond swapping the package reference; image and font handling is provided by CodeBrix.Imaging. The code for this package was derived from the open source library EPPlus version 4.5.3.3.

---

**SilverAssertions**
NuGet Package ID: `SilverAssertions.ApacheLicenseForever`
Source: [github.com/ellisnet/SilverAssertions](https://github.com/ellisnet/SilverAssertions)

A fluent assertion API for .NET unit tests, letting you express expected outcomes with the readable, chainable `.Should()` extension-method pattern (for example `value.Should().BeGreaterThan(0).And.BeLessThan(100)`) plus "because" failure messages. It covers strings, numerics (including approximate floating-point comparisons), booleans, collections, and much more, all exposed through a single `using SilverAssertions;` namespace. It works with all major .NET test frameworks — xUnit v3, NUnit v4, MSTest v4, and MSpec — with automatic framework detection and no configuration. The API surface is essentially identical to FluentAssertions, under the SilverAssertions namespace; the two libraries must not be mixed in one test project. The code for this package was derived from the open source library FluentAssertions version 7.1.0.
