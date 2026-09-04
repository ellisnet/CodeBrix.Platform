# The CodeBrix Family of NuGet Packages

## 1. About CodeBrix and CodeBrix.Platform

CodeBrix is a family of open-source .NET libraries and application-framework packages, published on nuget.org under the Owner account **"Ellisnet"**. Every CodeBrix-family package listed in this document has that nuget.org Owner. The packages target .NET 10 and later, and the strong preference across the family is fully managed, cross-platform code that behaves identically on Windows, Linux, and macOS.

The CodeBrix project is built on a few consistent principles:

- **License permanence.** Every package ID carries a `.{license}LicenseForever` suffix that permanently binds that package ID to its open source license. A consumer can never be moved onto different license terms by upgrading a CodeBrix package. Section 3 explains this guarantee in detail.
- **Proven code, kept available.** CodeBrix packages carry proven .NET capabilities forward: permissively licensed, namespaced under CodeBrix, modernized for .NET 10, and actively maintained. Where widely used libraries elsewhere have moved to commercial licenses, the CodeBrix equivalents remain open source, permanently, under their suffixed package IDs. The complete provenance record for each package — what its code came from, and under which licenses — is the `THIRD-PARTY-NOTICES.txt` file in its repository.
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

This is the authoritative list of the current CodeBrix-family NuGet packages. For each package: its name, its full NuGet package ID (how it is listed on nuget.org), the source repository, and a summary of what it does. For deeper usage documentation, read the `AGENT-README.txt` in the linked repository (see Section 2); for the provenance and licensing of the open source code in a package, read that repository's `THIRD-PARTY-NOTICES.txt`.

### 5.1 The CodeBrix.Platform framework family

All packages in this group are produced by the [CodeBrix.Platform repository](https://github.com/ellisnet/CodeBrix.Platform) and are documented by the `README.md` / `AGENT-README.txt` / `THIRD-PARTY-NOTICES.txt` files at that repository's root. They are listed in dependency order, most foundational first. All framework packages in a given release share one version and are published together.

---

**CodeBrix.Platform**
NuGet Package ID: `CodeBrix.Platform.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The core cross-platform UI framework, and the one required package for every CodeBrix.Platform Skia-based-UI application. It provides the WinUI XAML API surface — the `Microsoft.UI.Xaml.*` control set, the XAML runtime, layout, data binding, dispatching, and logging integration — rendered through a Skia-based engine on Windows, Linux, and macOS desktops. The package is self-contained (it folds in the Foundation, WinRT, dispatching, and logging-adapter assemblies), so a single reference in an application's core library delivers the full framework. It also folds in the Toolkit assembly, whose `TriPaneView` control splits a page into three resizable panes — a full-height side pane beside an upper and a lower pane — with draggable dividers and per-pane minimize and restore. It requires .NET 10, and is consumed alongside exactly one platform head package per target platform (see the `CodeBrix.Platform.Runtime.Skia.*` packages below).

---

**CodeBrix.Platform.SkiaSharp.Views**
NuGet Package ID: `CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

Provides the SkiaSharp XAML view types — `SKXamlCanvas` and `SKSwapChainPanel` — for hosting SkiaSharp-drawn content inside CodeBrix.Platform XAML. It is used internally by the Graphics2DSK, Lottie, and Svg extension packages; reference it directly only if your own code uses these view types. Unlike the rest of the family, this package carries its own version line rather than the shared framework version.

---

**CodeBrix.Platform.Graphics2DSK**
NuGet Package ID: `CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing an immediate-mode 2D drawing surface backed by SkiaSharp, for custom drawing inside CodeBrix.Platform XAML. It is referenced in an application's core library alongside the core framework package, and works on every platform head. Use it when your application needs to render custom 2D graphics directly rather than composing standard XAML controls.

---

**CodeBrix.Platform.Graphics3DGL**
NuGet Package ID: `CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing `GLCanvasElement` — a XAML element for embedding OpenGL-rendered 3D content inside CodeBrix.Platform applications. Rendering happens in an offscreen OpenGL framebuffer (through the `CodeBrix.Platform.OpenGL.MitLicenseForever` bindings package, which this package depends on) and is composited into the Skia scene, so it is independent of the head's presentation backend. It requires an OpenGL 3.0+ context, and every platform head supplies one: Windows Win32 and WPF (WGL), Linux X11 (GLX), macOS (via the bundled ANGLE libraries — see THIRD-PARTY-NOTICES), Linux Wayland (EGL — works under the head's default Vulkan presenter), and Linux FrameBuffer (DRM/GBM when a GPU is present, otherwise Mesa's llvmpipe software renderer — on GPU-less systems install Mesa's software GL, e.g. `libegl1` and `libgl1-mesa-dri`).

---

**CodeBrix.Platform.Lottie**
NuGet Package ID: `CodeBrix.Platform.Lottie.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing Lottie vector-animation playback in CodeBrix.Platform XAML, rendered through the Skottie engine. It is referenced in an application's core library and paired with the standard `SkiaSharp.Skottie` package (and the `CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever` package), giving smooth, resolution-independent animation playback on every platform head. Use it to play Lottie/Bodymovin JSON animations exported from tools such as After Effects.

---

**CodeBrix.Platform.Svg**
NuGet Package ID: `CodeBrix.Platform.Svg.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing SVG image support (`SvgImageSource`) on the Skia platform heads. It is referenced in an application's core library and paired with the `CodeBrix.SkiaSvg.MitLicenseForever` package, which supplies the underlying SVG parsing and Skia rendering. Use it to display scalable vector images in XAML with crisp results at any display resolution.

---

**CodeBrix.Platform.TextLayout**
NuGet Package ID: `CodeBrix.Platform.TextLayout.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package that exposes the framework's text engine as a plain code API, with no XAML and no running application required. It shapes text with HarfBuzz, resolves bidirectional runs per Unicode UAX #9, and itemizes across fallback fonts, then reports the geometry a text editor or custom renderer needs: measured size, caret rectangles, cluster-correct hit testing, per-line metrics, selection rectangles, and glyph outlines as SkiaSharp paths — per glyph or combined — for filled, stroked, and outlined text. A completed layout draws onto any `SKCanvas`, offscreen surfaces and bitmaps included. Text is supplied as a single string or as a list of styled runs mixing fonts, sizes, weights, and slants in one layout; indices are text indices rather than glyph indices, and wrapping is off unless a maximum width is given, so an application that models its own line breaks gets exactly the lines it asked for. Use it wherever text must be laid out or drawn outside the XAML visual tree — in a document model, a game, an image-processing pipeline, or a unit test. It is a façade over the same engine that renders every `TextBlock` in the framework.

---

**CodeBrix.Platform.AdvancedTextEdit**
NuGet Package ID: `CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing `AdvancedTextEdit` - a full-featured code and text editor control for CodeBrix.Platform XAML. It brings the editing model of a professional code editor to every platform head: an efficient rope-backed document with text anchors and grouped undo/redo; syntax highlighting driven by XSHD definition files, with twenty-one built-in definitions covering C#, XML, HTML, JavaScript, JSON, Python, PowerShell, SQL, and more; code folding with pluggable strategies (XML folding included, brace folding easily added); a code-completion popup with camel-case filtering; an in-editor search panel with regular-expression support; text snippets with linked editable fields; smart indentation; line numbers; word wrap; and rectangular (Alt+drag) selection. Rendering is virtualized line by line and driven by the family's single text engine - the same shaping, bidirectional-text resolution, and font fallback that lays out every `TextBlock` - so the editor stays responsive on very large documents and its measurements always agree with the rest of the framework (the `CodeBrix.Platform.TextLayout.ApacheLicenseForever` package flows in automatically as a dependency). Use it for code editors, log viewers, configuration editing, and anywhere a plain `TextBox` is not enough.

---

**CodeBrix.Platform.FlexPanel**
NuGet Package ID: `CodeBrix.Platform.FlexPanel.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing `FlexPanel` — a CSS flexbox-style layout panel for CodeBrix.Platform XAML. Children are arranged in optionally wrapping rows or columns using the familiar flexbox model: `Direction` selects the main axis (rows or columns, both reversible), `JustifyContent` distributes free space along it (including `SpaceBetween`, `SpaceAround`, and `SpaceEvenly`), `AlignItems` aligns children across it, `Wrap` allows multiple lines, and `AlignContent` distributes those lines. Per-child behavior is set with attached properties: `FlexPanel.Grow` and `FlexPanel.Shrink` control how a child absorbs free space or gives up overflow, `FlexPanel.Basis` sets a child's starting main-axis size (absolute, or a percentage such as `"25%"`), `FlexPanel.Order` rearranges children without reordering the XAML, and `FlexPanel.AlignSelf` overrides the panel's alignment for one child; child margins participate in the layout exactly as CSS margins do. The package is pure managed layout with no native dependencies, and works on every platform head. Use it for toolbar rows, tag and chip clouds, responsive card layouts, and any UI where flexbox thinking fits better than `Grid` rows and columns.

---

**CodeBrix.Platform.CommandBar**
NuGet Package ID: `CodeBrix.Platform.CommandBar.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing a tool bar / command bar / button bar family of XAML controls for CodeBrix.Platform, in the desktop tool bar tradition: a bar of small icon buttons with tooltips, bound to view-model commands, with grouping, separators, spacers, inline controls and overflow. A `ToolBarTray` hosts several `ToolBar`s in a wrapping row, and a `ToolBar` takes any `UIElement` as an item — `ToolButton`, `ToolToggleButton`, `ToolDropDownButton`, `ToolBarGroup`, `ToolBarSeparator`, `ToolBarSpacer`, or an ordinary control such as a `ComboBox` — laying them out with grouping, separators, spacers, and overflow into a chevron flyout. Buttons bind to an `ICommand` (their `IsEnabled` follows `CanExecute`) or to a `XamlUICommand` / `StandardUICommand` action object, show icon only, text only, or both, and carry auto-composed tooltips with shortcut text; bar-level presentation — icon size, label mode, label position, whether tooltips are shown — comes from inherited attached properties on `ToolBarProperties`, so a bar states it once and any single item can override it. Icons are `ToolIconSource` objects: SVG, themed, tintable and re-rasterised at the display scale, or PNG and the other raster formats the platform's image decoder reads. Because `ToolIconSource` derives from the framework's `IconSource`, the same artwork also works anywhere the framework takes an icon source, including on a WinUI `AppBarButton`. Use it for the tool bars of a desktop-shaped application, where the vocabulary and semantics of a tool bar fit better than a WinUI `CommandBar` app bar. The code for this package is original to CodeBrix.

---

**CodeBrix.Platform.TerminalView**
NuGet Package ID: `CodeBrix.Platform.TerminalView.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing `TerminalControl` — a terminal emulator view for CodeBrix.Platform XAML on every platform head. It renders a `CodeBrix.Terminal` engine (VT100/VT220/xterm-compatible escape sequence parsing, terminal buffer management, and 256-color ANSI/SGR attributes) as a fixed monospace cell grid on a Skia surface, laid out by the family's single text engine (the `CodeBrix.Platform.TextLayout.ApacheLicenseForever` package flows in automatically as a dependency, and `CodeBrix.Terminal.MitLicenseForever` comes in the same way). The control is transport-agnostic — feed it bytes or text from an SSH shell stream, a PTY, or a local process, and wire its VT-encoded keyboard input and grid-resize notifications back to the transport — and it provides scrollback with a scrollbar, mouse-wheel and keyboard paging, text selection with word/expression double-click, built-in clipboard copy and paste (context menu and Ctrl+Shift+C/V), window-title reporting, and a bundled Roboto Mono default font. Use it for SSH clients, embedded consoles, build-output panes, and anywhere an application hosts a live terminal.

---

**CodeBrix.Platform.PlotterView**
NuGet Package ID: `CodeBrix.Platform.PlotterView.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing `PlotterControl` — a chart-plotting view for CodeBrix.Platform XAML on every platform head. It hosts a `CodeBrix.Plotter` `PlotModel` (the CodeBrix port of the open source OxyPlot library: forty-plus series types including line, scatter, area, bar, pie, heat-map, contour, histogram, candlestick and box-plot, with linear, logarithmic, date-time, category and polar axes, annotations, and legends; the `CodeBrix.Plotter.MitLicenseForever` package flows in automatically as a dependency) on a Skia surface, with the library's full interaction model wired in: pan with right-drag or the arrow keys, zoom with the mouse wheel, the +/- keys, or a middle-drag zoom rectangle, a data-point tracker on left-click, reset with a double middle-click or the A/Home keys, and on touch heads single-finger panning and two-finger pinch zoom — all rebindable through the control's `Controller` property. Every piece of chart text — titles, axis labels, legends, the tracker — renders through the application's own fonts (the app's default font, or another application font named per control or per model element), never a host system font, so a chart looks identical on a desktop and on a bare frame-buffer device. Set the `Model` property (it is a dependency property, so it binds), and after changing data from any thread call `PlotModel.InvalidatePlot` — the control marshals, updates, and repaints. Use it for dashboards, oscilloscope-style live streaming charts, data-analysis views, and anywhere an application draws a chart. The control code is original to CodeBrix; the plotting engine behind it is the author's CodeBrix.Plotter port of the open source OxyPlot library.

---

**CodeBrix.Platform.AppSettings**
NuGet Package ID: `CodeBrix.Platform.AppSettings.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional extension package providing a persistent application-settings system — the one extension package in the family that is not a UI control, and deliberately so: it ships no settings screen, leaving an application free to build its own or to have none at all and simply save in the background. Everything an application wants to remember between runs is stored as JSON in a single portable `settings.sqlite` database (the `CodeBrix.Sqlite.ApacheLicenseForever` package flows in automatically as a dependency), reached through the static `AppSettingsService` facade over an `AppSettingsStore`: `Get`, `Set`, `HasValue`, change notification both per key and across the store, and typed `AppSettingProperty<T>` handles that read and write one setting as an ordinary property and can migrate a value from a previous key name. Initialization asks for nothing but the application name — `AppSettingsService.Initialize("MyApp")` — and the database is placed in the right per-user configuration location for the platform, grouped under a `CodeBrix` folder; an explicit folder can be supplied instead. The store manages its own file lifecycle so an application does not have to: a timestamped automatic backup with retention pruning on every start, quarantine of a corrupt database and restore from the newest good backup, silent creation on first run, export to a self-contained single file, and import of a settings file that is validated when selected and adopted on the next start. Values are text — a `byte[]` does round-trip, since it serializes to a base64 string, but binary belongs elsewhere. Use it for window geometry, recently-used lists, chosen folders, tool and view state, feature toggles, and anything else an application should still know next time it starts. The code for this package is original to CodeBrix.

---

**CodeBrix.Platform.Runtime.Skia**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The base Skia runtime layer that every platform head package builds on, providing the shared windowing and rendering host infrastructure that the per-platform head packages specialize. Application projects never reference this package directly — it flows in transitively beneath whichever head package a head project references. It is published so that the head packages restore correctly, and it is listed here so the complete authorized package set is visible.

---

**CodeBrix.Platform.Runtime.Skia.Win32**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for Windows desktop applications hosted in a Win32 window — the simplest and most common choice for targeting Windows. A Windows head project references exactly this one head package (plus the application's core library) and bootstraps with `.UseWindowsWin32()`. Choose this head unless you specifically need to host CodeBrix.Platform content inside a WPF application context.

---

**CodeBrix.Platform.Runtime.Skia.Wpf**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for hosting CodeBrix.Platform content inside a WPF desktop application context on Windows. A WPF head differs from the other heads in a few documented ways: it targets `net10.0-windows`, it must not set `UseWPF` (WPF is loaded by the host at runtime), it bootstraps with `.UseWindowsWpf()`, and forcing software rendering after host construction is recommended to avoid rendering conflicts. For a plain Windows desktop app, prefer the Win32 head instead.

---

**CodeBrix.Platform.Runtime.Skia.X11**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The broad-compatibility platform head package for desktop Linux: it runs on X11 desktops and also on Wayland desktops through XWayland (the X11 compatibility layer). A Linux head project references this package and bootstraps with `.UseLinuxX11()`. Ship this head for maximum desktop-Linux reach — alone, or alongside a native Wayland head.

---

**CodeBrix.Platform.Runtime.Skia.Wayland**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for a pure, native Wayland client on desktop Linux: it speaks the Wayland protocol directly and never uses X11 or XWayland. It requires a Wayland compositor, and fails fast with a clean error when none is present (it never falls back to X11); the head bootstraps with `.UseLinuxWayland()`. Rendering is GPU-accelerated Vulkan by default, falling back automatically to shared-memory software rendering when Vulkan is unavailable; an OpenGL ES (EGL) path, a software-only mode, and a no-fallback `VulkanForced` mode can be selected in code (`RenderingBackend(...)` on the head builder) or via environment variables (`CODEBRIX_WAYLAND_NO_GPU=1`, `CODEBRIX_WAYLAND_USE_EGL=1`). Flyout-based popups (ComboBox dropdowns, MenuFlyout, ToolTip, and similar controls), rich clipboard formats (plain text, HTML, PNG images, and file lists), fractional display scaling, custom title bars, and window activation all work, at parity with the X11 head; accepting drag-and-drop from other applications is implemented, but delivery depends on the compositor (some experimental Wayland desktops send unusable drag coordinates). Remaining gaps: touch input, native-view hosting in XAML, native OpenGL interop, and initiating drags from the application are not yet implemented (the last is missing on the X11 head too); programmatic window positioning and resizing and always-on-top are unavailable by Wayland protocol design (each logs a one-time warning naming the API); window icons come from a .desktop file rather than the app manifest; and IME text input is not yet available on either Linux head. Prefer the X11 head if your application depends on touch input or native-view hosting today.

---

**CodeBrix.Platform.Runtime.Skia.FrameBuffer**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for Linux framebuffer targets — embedded and kiosk devices with no X11 or desktop environment at all. The same shared application code runs unchanged; the head project simply references this package and bootstraps with `.UseLinuxFrameBuffer()`. Use it to put a full XAML UI on dedicated-purpose Linux hardware.

---

**CodeBrix.Platform.Runtime.Skia.MacOS**
NuGet Package ID: `CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

The platform head package for macOS desktop applications, bootstrapped with `.UseMacOS()`. The package contains a small native library shipped as a universal binary, so applications run on both Apple Silicon and Intel Macs. As with the other heads, a macOS head project references exactly this one head package plus the application's core library.

---

**CodeBrix.Platform.WebView**
NuGet Package ID: `CodeBrix.Platform.WebView.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional add-on package that makes the XAML `WebView2` control work on every platform head with a single reference in the application's core library. Its real delivery is Linux: on the X11, Wayland, and FrameBuffer heads, web content is rendered offscreen by the system-installed WPE WebKit engine and composited directly into the Skia scene — no native child windows, no airspace problems, and clipping, transforms, and z-order behave like any other XAML content. On the Windows, WPF, and macOS heads — which have built-in WebView support via Microsoft Edge WebView2 and WKWebView — the package is inert and harmless to reference. No engine binaries ship in the package; Linux machines must have the system WPE WebKit packages installed, and a missing engine produces a clear exception naming the exact install command. Custom User-Agent strings and page-to-host messaging (both the WebView2 and WebKit JavaScript idioms) are supported on every head.

---

**CodeBrix.Platform.MediaPlayer**
NuGet Package ID: `CodeBrix.Platform.MediaPlayer.LgplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional add-on package that brings the XAML `MediaPlayerElement` (audio and video playback) to the Win32, WPF, X11, Wayland, and FrameBuffer heads with a single reference in the application's core library. LibVLC decodes media into memory and the frames are composited directly into the Skia scene — no native child windows, no airspace problems, and the native-Wayland head stays a pure Wayland client. The package is inert on the macOS head, which has built-in AVFoundation media support and needs neither this package nor libvlc. The native libvlc runtime is not shipped in the package: on Linux it is installed via the system package manager, and on Windows the `VideoLAN.LibVLC.Windows` package is added to the Windows head project(s). Playback is delivered through the `CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever` package (see Section 5.3).

---

**CodeBrix.Platform.AudioPlayer**
NuGet Package ID: `CodeBrix.Platform.AudioPlayer.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional add-on package providing audio playback (WAV, MP3, Ogg Vorbis and FLAC) and MIDI music with a single reference in the application's core library and no native setup at all — unlike the WebView and MediaPlayer add-ons there is no per-OS engine and nothing to install, so the package is live on all six platform heads, macOS included. It provides three public types: `AudioPlayer`, a non-visual XAML-declarable element with `Play`/`Pause`/`Stop`/`Seek` control and bindable `Source`, `Volume`, `IsLooping`, `Duration`, and `Position` properties — the position is two-way bindable and debounced, so binding a `Slider` to it yields a working scrubber with clean seek-on-release behavior; `MidiPlayer`, which renders a MIDI file through a SoundFont (`.sf2`) or SFZ (`.sfz`) instrument and carries that same transport surface — so the same scrubber markup drives either player — plus `Speed` (tempo without pitch change), a live voice count, per-channel mixing, and an observe-only hook for reacting to the notes; and `SoundEffect`, a fire-and-forget one-liner for overlapping sound effects with in-memory caching. Sources accept a filesystem path, an `ms-appx:///` asset URI, an `embedded://` embedded-resource URI, or a stream — with one exception: an `.sfz` instrument must be a path or `ms-appx:///` URI, because it references its sample files as separate files beside it. Opus plays as well, once the application references the separate `CodeBrix.Audio.Opus.BsdLicenseForever` package and registers it at start-up; this add-on neither depends on it nor needs to. Because instruments can be very large, `MidiPlayer` loads them in the background and reports progress through `IsLoading` and `MediaOpened`. The code for this package is original to CodeBrix, with playback delivered through the fully managed `CodeBrix.Audio.MitLicenseForever` package and its bundled native audio backend (see Section 5.3).

---

**CodeBrix.Platform.VideoPlayer**
NuGet Package ID: `CodeBrix.Platform.VideoPlayer.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform](https://github.com/ellisnet/CodeBrix.Platform)

An optional add-on package providing video playback with a single reference in the application's core library and no native setup at all — like the AudioPlayer add-on, and unlike the WebView and MediaPlayer add-ons, there is no per-OS engine and nothing to install, so the package is live on all six platform heads, macOS included. It provides one XAML-declarable element, `VideoPlayer`, that plays AV1 video from WebM and Matroska containers and from CodeBrix `.cbv` video files, with Ogg Vorbis or Opus sound. Its transport surface is a superset of the AudioPlayer element's — the same `Play`/`Pause`/`Stop`/`Seek` with bindable `Source`, `Volume`, `IsLooping`, `Duration` and a two-way debounced `Position`, plus `IsMuted` — so one scrubber markup drives either kind of player. On top of that it adds what video needs: `Stretch` letterboxing (`None`/`Fill`/`Uniform`/`UniformToFill`), an ordered colour-grading effect chain composed into a single lookup per pixel, drawable layers and a `Composing` hook for subtitles, overlays or a webcam picture-in-picture, captions and chapters carried as data, a `SourceMode` for streaming, memory-mapping or preloading a file, and `CapturePresentedFrame()` for a screenshot of what is on screen. The picture is composed on the graphics device wherever the running head can supply one — the package depends on `CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever` for that, so an application needs no GPU wiring of its own — and on the processor everywhere else, with `RenderPath` stating the intent and `ActiveRenderPath` reporting what actually runs. AV1 decoding is BSD-2-Clause and Opus is BSD-3-Clause, so neither is a dependency of this Apache-2.0 package: an application references `CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever` and, for Opus sound, `CodeBrix.Audio.Opus.BsdLicenseForever`, and registers them at start-up. The code for this package is original to CodeBrix, with playback delivered through the fully managed `CodeBrix.VideoPlayback.MitLicenseForever` package (see Section 5.3) — the engine only, never its `CodeBrix.VideoPlayback.Skia` companion, which is the presenter for hosts outside this family and pins its own SkiaSharp: the composing presenter and its colour shader are this add-on's own internal code, compiled against the family's single SkiaSharp pin, so an application never carries two.

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

Adds Skia-rendered image controls to native WinUI 3 / Windows App SDK applications: `EmbeddedImage` and `EmbeddedImageButton`, with an `embedded://` URI scheme for loading images directly from embedded assembly resources (alongside `ms-appx` URIs). Its headline capability is vector-direct SVG rendering: SVG images are drawn as vectors at full display resolution with no intermediate rasterization, producing crisp, resolution-independent results — pixel-for-pixel matching the SVG output of the cross-platform CodeBrix.Platform framework. It depends on the `CodeBrix.Platform.WinUI.ApacheLicenseForever` core package.

---

**CodeBrix.Platform.WinUI.Lottie**
NuGet Package ID: `CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform — src-platforms/Platform.WinUI](https://github.com/ellisnet/CodeBrix.Platform/tree/main/src-platforms/Platform.WinUI)

A Lottie animation player for native WinUI 3 / Windows App SDK applications, rendered with the SkiaSharp Skottie engine rather than the Windows-native Composition/Win2D pipeline. Because the stock Windows App SDK `AnimatedVisualPlayer` requires Composition/Win2D animation sources, this package ships its own `AnimatedVisualPlayer` control hosting a Skia render surface, along with `LottieVisualSource` and `ThemableLottieVisualSource`. It supports `embedded://`, `ms-appx:///`, and `ms-appdata:///` URI schemes, a Play/Stop/Pause/Resume/SetProgress playback API, and runtime color theming of animations. It renders animations identically to the cross-platform CodeBrix.Platform Lottie package, and depends on both the WinUI Skia and Core packages.

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

A class library bundling a set of proven low-level helper libraries into a single CodeBrix-owned assembly: general-purpose extensions (string, memoization, stream, URI, weak references), collection helpers, a rich disposables toolkit (`CompositeDisposable`, `SerialDisposable`, `RefCountDisposable`, and more), equality/comparison builders, logging helpers, and threading primitives (`FastAsyncLock`, `AsyncEvent`, transactional updates). It exists so that CodeBrix.Platform can take one dependency instead of a fan-out of several small packages, but the helpers are equally usable in any .NET 10 project. Namespaces root at `CodeBrix.Platform.Extensions.*`.

---

**CodeBrix.Platform.Fonts.Fluent**
NuGet Package ID: `CodeBrix.Platform.Fonts.Fluent.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Fonts.Fluent](https://github.com/ellisnet/CodeBrix.Platform.Fonts.Fluent)

A redistribution of the Fluent icon font (Windows 11 iconography) for CodeBrix.Platform applications, providing the default symbols font used by `SymbolIcon`, `FontIcon`, and the `SymbolThemeFontFamily` theme resource. The assembly is metadata-only with no managed API; the payload is the icon font file plus a buildTransitive MSBuild `.props` that automatically registers it as the default symbols font in consuming apps (with an opt-out property). Fonts are referenced via `ms-appx:///CodeBrix.Platform.Fonts.Fluent/Fonts/...` URIs.

---

**CodeBrix.Platform.Fonts.OpenSans**
NuGet Package ID: `CodeBrix.Platform.Fonts.OpenSans.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Fonts.OpenSans](https://github.com/ellisnet/CodeBrix.Platform.Fonts.OpenSans)

A redistribution of the Open Sans font family as a content-only NuGet package for CodeBrix.Platform applications — commonly used as the application's default text font. It ships a variable font covering weights 300–800 plus 36 static instances across weights, styles, and stretches, together with a font manifest and a buildTransitive MSBuild `.targets` that prunes redundant static fonts at consumer-build time while always keeping the variable font. Fonts are referenced via `ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/...` URIs, or registered framework-wide via `FeatureConfiguration.Font.DefaultTextFontFamily`.

---

**CodeBrix.Platform.Fonts.Roboto**
NuGet Package ID: `CodeBrix.Platform.Fonts.Roboto.OflLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Fonts.Roboto](https://github.com/ellisnet/CodeBrix.Platform.Fonts.Roboto)

A redistribution of the Roboto font family, structured like the sibling OpenSans package: a variable `Roboto.ttf` covering the full weight and width axes plus 36 static instances, a font manifest, and a buildTransitive MSBuild `.targets` that prunes redundant static fonts at consumer-build time while always keeping the variable font. It is designed for CodeBrix.Platform applications (referenced via `ms-appx:///CodeBrix.Platform.Fonts.Roboto/Fonts/...` URIs or set as the default text font) and is equally usable as a plain content-files NuGet in any .NET 10 project. The assembly is metadata-only with no managed API. The fonts are the open source Roboto family published by Google.

---

**CodeBrix.Platform.LinuxDBus**
NuGet Package ID: `CodeBrix.Platform.LinuxDBus.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.LinuxDBus](https://github.com/ellisnet/CodeBrix.Platform.LinuxDBus)

A fully managed, low-level D-Bus protocol library for Linux that speaks the D-Bus wire protocol directly: connecting to the session bus, system bus, or any D-Bus transport; sending and receiving messages; subscribing to signals; and registering method handlers to expose D-Bus objects. The primary API is the `Connection` class. It requires a Linux runtime with a running D-Bus daemon, and has no NuGet dependencies beyond the shared framework.

---

**CodeBrix.Platform.MediaPlayerCore**
NuGet Package ID: `CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.MediaPlayerCore](https://github.com/ellisnet/CodeBrix.Platform.MediaPlayerCore)

A fully managed, cross-platform audio/video media-player library that wraps the native libvlc dynamic library, exposing high-level managed classes: `LibVLC`, `Media`, `MediaPlayer`, `MediaList`, media and renderer discovery (Chromecast/UPnP), `Equalizer`, and a UI-agnostic media-element management layer. A notable CodeBrix addition is `VideoFrameSink`, which renders video frames into CPU memory and raises per-frame BGRA events — enabling windowing-system-agnostic video rendering on hosts with no window-embedding API (this is what powers the CodeBrix.Platform MediaPlayer add-on). The native libvlc runtime must be present at run time (a NuGet package on Windows, system packages on Linux, VLC on macOS).

---

**CodeBrix.Platform.OpenGL**
NuGet Package ID: `CodeBrix.Platform.OpenGL.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.OpenGL](https://github.com/ellisnet/CodeBrix.Platform.OpenGL)

A fully managed, cross-platform OpenGL bindings library. The main entry point is the `GL` class — constructed over a native context (for example `opengl32` on Windows or `libGL.so` on Linux) — with a method for every OpenGL core-profile entry point, plus the ported native-loader infrastructure and math types. The interop code is committed as static source, so consumers need no source-generator tooling. OpenGL extensions, OpenGL ES, and legacy profiles are out of scope for the current version, and actual GL calls require a live GL context at runtime.

---

**CodeBrix.Platform.Unicode**
NuGet Package ID: `CodeBrix.Platform.Unicode.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Unicode](https://github.com/ellisnet/CodeBrix.Platform.Unicode)

A redistribution of the ICU (International Components for Unicode) version 77 native binaries for Windows, packaged for CodeBrix.Platform applications. The assembly is metadata-only; the payload is the ICU native DLLs for both win-x64 and win-arm64, plus the full ICU data archive (Unicode character properties, CLDR locale data, collation, normalization, BiDi, time zones, and more). A buildTransitive MSBuild `.targets` automatically embeds the data archive in consumer builds, with a shared sentinel ensuring it is embedded exactly once even when the macOS sibling package is also present.

---

**CodeBrix.Platform.UnicodeMacOs**
NuGet Package ID: `CodeBrix.Platform.UnicodeMacOs.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Platform.Unicode](https://github.com/ellisnet/CodeBrix.Platform.Unicode)

The macOS counterpart to CodeBrix.Platform.Unicode, built from the same repository: a redistribution of the ICU version 77 native binaries for macOS. The metadata-only assembly ships two universal (x86_64 + arm64) dylibs plus the same ICU data archive as the Windows package, with the same buildTransitive auto-embed mechanism — installing both OS packages in one build embeds the data archive only once.

---

**CodeBrix.ArgumentParser**
NuGet Package ID: `CodeBrix.ArgumentParser.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.ArgumentParser](https://github.com/ellisnet/CodeBrix.ArgumentParser)

A fully managed, cross-platform command-line option parser with no dependencies beyond .NET itself. It provides Getopt::Long-style option parsing supporting short, long, and Windows-style option prefixes, typed option callbacks, multi-value options, option bundling, and response-file (`@file`) expansion, plus a Command/CommandSet model for building git-style multi-command CLI suites with automatic help generation. Response-file handling is security-hardened with cycle detection, nesting-depth caps, and strict quote handling.

---

**CodeBrix.AssemblyTools**
NuGet Package ID: `CodeBrix.AssemblyTools.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.AssemblyTools](https://github.com/ellisnet/CodeBrix.AssemblyTools)

A library giving full programmatic read/write/rewrite access to managed .NET assemblies — modules, types, methods, fields, properties, events, custom attributes, IL, and debug symbols (portable PDB, native PDB, Mono MDB). It ships as a single merged assembly combining the core reader/writer, the Rocks extension helpers, and the two symbol providers; key entry points are `AssemblyDefinition.ReadAssembly`, `ModuleDefinition.ReadModule`, and `ILProcessor` for IL rewriting.

---

**CodeBrix.Audio**
NuGet Package ID: `CodeBrix.Audio.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Audio](https://github.com/ellisnet/CodeBrix.Audio)

A fully managed, cross-platform audio-file library with no native code or platform interop, behaving identically on Windows, macOS, and Linux. It reads WAV and MP3 audio (MP3 decoding is fully managed — no OS codec needed), writes WAV, reads MP3 ID3v2 metadata tags, and reads/writes Standard MIDI Files with a full MIDI event hierarchy. It also exposes DSP analysis primitives: FFT, biquad filters, an envelope follower, and an energy-based voice-activity detector. Audio-device playback/recording, resampling, and synthesis are explicit non-goals — this is a file and signal-analysis library.

---

**CodeBrix.Compression**
NuGet Package ID: `CodeBrix.Compression.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Compression](https://github.com/ellisnet/CodeBrix.Compression)

A library for creating, reading, updating, and extracting compressed archives in Zip, GZip, Tar, and BZip2 formats, with zero external dependencies beyond .NET. Zip support is the most complete and includes encryption (AES-128, AES-256, ZipCrypto) and Zip64 extensions for archives over 4 GB; GZip, Tar, and BZip2 support create/read/extract. It handles streaming (non-seekable) input and output, in-memory archive operations, checksums, Unicode filenames, and path-traversal attack prevention. All of its namespaces are under `CodeBrix.Compression`.

---

**CodeBrix.Imaging**
NuGet Package ID: `CodeBrix.Imaging.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Imaging](https://github.com/ellisnet/CodeBrix.Imaging)

A fully managed, cross-platform 2D image-processing and font-handling library with zero external dependencies. It reads and writes BMP, GIF, JPEG, PBM, PNG, TGA, TIFF, and WebP with format auto-detection, and provides processing operations (resize, crop, mutation pipelines, and more), strongly typed pixel formats, and drawing/text rendering. It can construct images from raw pixel buffers — including a dedicated SIMD-optimized path for BGRA output from native renderers such as PDFium or Direct2D — which makes it the image backbone for several other CodeBrix packages (PDF, video processing, Excel). All of its namespaces are under `CodeBrix.Imaging`.

---

**CodeBrix.MarkupParse**
NuGet Package ID: `CodeBrix.MarkupParse.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.MarkupParse](https://github.com/ellisnet/CodeBrix.MarkupParse)

A fully managed, cross-platform HTML parsing and DOM manipulation library with zero external dependencies. It parses HTML from strings, streams, or URLs into a fully navigable DOM tree, queryable via CSS selectors (`QuerySelector`/`QuerySelectorAll`) or LINQ, with full traversal and manipulation of nodes, attributes, classes, and text content. It serializes the DOM back to HTML with standard, pretty-printed, minified, or XHTML formatters, and supports fragment parsing, source-position tracking, async URL loading with cookies, and forms. It is deliberately HTML-to-DOM only — no CSS evaluation, JavaScript execution, or rendering.

---

**CodeBrix.PdfDocuments**
NuGet Package ID: `CodeBrix.PdfDocuments.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.PdfDocuments](https://github.com/ellisnet/CodeBrix.PdfDocuments)

A low-level, pure managed PDF library for creating, reading, merging, and manipulating PDF documents using direct graphics drawing via the XGraphics API (`DrawString`, `DrawImage`, shape drawing, and wrapped-text formatting). It supports document metadata, page sizing and orientation, fonts with styles, embedding PNG/JPEG/BMP/WebP/GIF images (including images processed via CodeBrix.Imaging), and opening existing PDFs for modification or page import and merging. It is the foundation package of the repository's PDF trio — the PdfDocCreate and PdfRasterizer packages build on it. Use it when you need fine-grained control over page layout and drawing, or to work with existing PDF files.

---

**CodeBrix.PdfDocCreate**
NuGet Package ID: `CodeBrix.PdfDocCreate.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.PdfDocuments](https://github.com/ellisnet/CodeBrix.PdfDocuments)

A high-level document object model for building richly formatted PDFs: structured documents composed of sections, paragraphs, styles, tables, charts, images, and headers/footers, rendered to PDF via `PdfDocumentRenderer`. Choose it over the lower-level CodeBrix.PdfDocuments package when you want to describe a document declaratively with a structured model rather than drawing at coordinates — the two can also be used together. Installing it automatically brings in CodeBrix.PdfDocuments (on which it is built) plus the CodeBrix.Imaging and CodeBrix.Compression packages.

---

**CodeBrix.PdfRasterizer**
NuGet Package ID: `CodeBrix.PdfRasterizer.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.PdfDocuments](https://github.com/ellisnet/CodeBrix.PdfDocuments)

A PDF page rasterizer that renders PDF pages to images (PNG, JPEG, BMP, GIF, TIFF) using the PDFium native rendering engine, through a `PageRasterizer` API that also supports thumbnails, page-dimension queries, and render flags. Pre-built PDFium native binaries are bundled in the package for Windows (x64/x86/ARM64), macOS (x64/ARM64), Linux (x64/ARM64/ARM/RISC-V 64), and Android ARM64 — no separate PDFium install is required, though platforms without a bundled binary (such as iOS and WebAssembly) are not supported. It is the "PDF-to-image" member of the repository's trio and depends on CodeBrix.PdfDocuments and CodeBrix.Imaging; note that PDFium is not thread-safe, so rasterization calls are serialized.

---

**CodeBrix.Python**
NuGet Package ID: `CodeBrix.Python.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Python](https://github.com/ellisnet/CodeBrix.Python)

A cross-platform Python-to-.NET interoperability library that embeds a CPython interpreter inside a .NET process and marshals objects across the Python/CLR boundary; through the embedded `clr` module, Python code can also load and call .NET assemblies. Core entry points are `PythonEngine` (interpreter lifecycle), `Py.GIL()` for lock acquisition, and `PyObject` with typed wrappers (`PyList`, `PyDict`, and so on) for dynamic dispatch and conversion, plus pluggable encoders/decoders for custom type conversion. It targets scenarios where a .NET application needs to run Python code, use Python libraries, or expose .NET APIs to Python scripts. A discoverable CPython shared library (versions 3.10 through 3.14) is required at run time.

---

**CodeBrix.ServiceLocator**
NuGet Package ID: `CodeBrix.ServiceLocator.MsplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.ServiceLocator](https://github.com/ellisnet/CodeBrix.ServiceLocator)

A shared abstraction over IoC containers and service locators, letting libraries and frameworks resolve services without a hard reference to any specific container. It defines `IServiceLocator` (resolve by type, or type plus string key, with `GetAllInstances`), the static ambient `ServiceLocator.Current` accessor, and the abstract `ServiceLocatorImplBase`, which lets a container adapter implement the full surface by overriding just two template methods; resolution failures are uniformly wrapped in `ActivationException`.

---

**CodeBrix.SkiaSvg**
NuGet Package ID: `CodeBrix.SkiaSvg.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.SkiaSvg](https://github.com/ellisnet/CodeBrix.SkiaSvg)

An SVG loading and rendering library built on SkiaSharp, which also loads Android VectorDrawables and renders to SkiaSharp surfaces. Beyond basic rendering via the `SKSvg` entry point, it provides hit testing (point and rectangle, at element and scene-node level), a retained scene graph enabling efficient partial mutations, manually driven animation, pointer interaction, and pluggable typeface providers for headless environments. It exports to raster formats (PNG, JPEG, BMP, GIF, TIFF) and vector formats (SVG, PDF, XPS), and consolidates several companion capabilities into a single library. It is also the SVG engine behind the CodeBrix.Platform framework's SVG support.

---

**CodeBrix.StyleSheetParse**
NuGet Package ID: `CodeBrix.StyleSheetParse.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.StyleSheetParse](https://github.com/ellisnet/CodeBrix.StyleSheetParse)

A fully managed, cross-platform CSS stylesheet parsing library that parses CSS text into a strongly typed object model which can be queried, manipulated, and serialized back to CSS. The `StylesheetParser` entry point supports sync and async parsing with configurable tolerance modes, and the resulting model exposes typed collections for style, media, container, import, font-face, page, keyframes, and other rule types. Style declarations offer over one hundred strongly typed CSS properties plus name-based access, and parsed selectors include CSS specificity calculation. It has no dependencies beyond .NET and serves as the CSS engine underneath the CodeBrix SVG libraries.

---

**CodeBrix.SvgParse**
NuGet Package ID: `CodeBrix.SvgParse.MsplLicenseForever`
Source: [github.com/ellisnet/CodeBrix.SvgParse](https://github.com/ellisnet/CodeBrix.SvgParse)

A renderer-agnostic SVG document object model library providing comprehensive SVG parsing, element modeling, styling, and serialization. It loads SVG documents from files, streams, strings, or XmlReaders via `SvgDocument`, and exposes a rich object model — visual elements, paint servers (colors, gradients, patterns), path segments, transforms, filter effects, and CSS selector matching — for querying and manipulation. Built-in security controls govern external entity, image, and element resolution (XXE prevention by default). Because it depends on no rendering engine, it can serve as the foundation for any SVG rendering backend; within the CodeBrix family it underpins CodeBrix.SkiaSvg.

---

**CodeBrix.Templating**
NuGet Package ID: `CodeBrix.Templating.BsdLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Templating](https://github.com/ellisnet/CodeBrix.Templating)

A text-templating and scripting-language library that parses and renders templates written in the Scriban and Liquid template languages. The immutable, cacheable `Template` class parses templates and renders them synchronously or asynchronously against models; `TemplateContext` controls evaluation state, including template loaders for includes, culture, strict-variable mode, and safety limits (loop, recursion, and regex timeouts). `ScriptObject` provides dictionary-like model binding with reflection-based import of objects, delegates, and static classes, plus a large built-in function library (string, array, math, date, regex, HTML, and more). It suits code generation, HTML pages, reports, configuration files, and any text produced from a model.

---

**CodeBrix.Terminal**
NuGet Package ID: `CodeBrix.Terminal.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.Terminal](https://github.com/ellisnet/CodeBrix.Terminal)

A .NET terminal emulation engine with Unicode text support: a virtual terminal (VT100/VT220/VT400/xterm-compatible) with a full ANSI/DEC escape-sequence parser, terminal buffer management with scrollback, and Unicode text utilities. Features include cursor and scroll-region control, text attributes, 8/16/256-color support, mouse tracking protocols, alternate screen buffers, terminal resize with reflow strategies, and search/selection services; PTY fork/exec is available on Unix and macOS only. The core `Terminal` class is fed text or bytes and exposes the resulting buffer, making it suitable for building terminal UI controls or headless terminal processing, with zero dependencies beyond the .NET runtime.

---

**CodeBrix.TestMocks**
NuGet Package ID: `CodeBrix.TestMocks.ApacheLicenseForever`
Source: [github.com/ellisnet/CodeBrix.TestMocks](https://github.com/ellisnet/CodeBrix.TestMocks)

A single-package .NET testing library that combines mocking and auto-generated test data into one unified dependency with built-in xUnit v3 integration. It provides `Mock<T>` creation for interfaces and classes (loose and strict behavior, setups, returns, callbacks, async support), a rich argument-matcher set, and full call verification, plus a `Fixture` for anonymous test data and data-driven xUnit v3 attributes (`[AutoData]`, `[AutoMockData]`, and friends). All of its namespaces use the `CodeBrix.TestMocks` prefix, and its only NuGet dependency is the xUnit v3 extensibility core.

---

**CodeBrix.VideoPlayback**
NuGet Package ID: `CodeBrix.VideoPlayback.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.VideoPlayback](https://github.com/ellisnet/CodeBrix.VideoPlayback)

A fully managed video playback engine with no native binaries and no drawing dependency. It reads WebM and Matroska files (`.webm`, `.mkv`) carrying AV1 video with Opus or Vorbis audio and any number of text caption tracks, and `.cbv` — a container this package writes and reads, laid out so the whole index and every caption cue sit ahead of the media data. It supplies the machinery around a codec rather than a codec itself: a demultiplexer, a playback session with a transport and a clock, a zero-copy frame-buffer pool, a newest-frame presenter mailbox, a managed SIMD YUV-to-BGRA converter, and a muxer. Uncompressed video (`V_UNCOMPRESSED`) decodes in the package itself; a coded format's decoder arrives as a separate package that registers itself. It also carries everything a presenter is made of, short of the drawing: the render-path enums, the letterbox arithmetic, the composition context handed to an overlay layer, the composed-effect chain, and the colour shader's source text — so a presenter supplies a canvas and nothing else. Audio plays through CodeBrix.Audio. Two companion packages come from the same repository: `CodeBrix.VideoPlayback.Skia.MitLicenseForever`, a SkiaSharp presenter for hosts outside the CodeBrix.Platform family (a CodeBrix.Platform application uses the VideoPlayer add-in of Section 5.1 instead, which does its own composing), and `CodeBrix.VideoPlayback.Authoring.MitLicenseForever`, a developer-machine tool that authors `.cbv` files by driving the workstation's FFmpeg through CodeBrix.VideoProcessing.

---

**CodeBrix.VideoPlayback.Dav1d**
NuGet Package ID: `CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever`
Source: [github.com/ellisnet/CodeBrix.VideoPlayback.Dav1d](https://github.com/ellisnet/CodeBrix.VideoPlayback.Dav1d)

The AV1 video decoder for CodeBrix.VideoPlayback: a binding over dav1d, the reference software AV1 decoder, shipping the native dav1d libraries for Windows x64 and ARM64, macOS Intel and Apple Silicon, and Linux x64, ARM64 and RISC-V 64. CodeBrix.VideoPlayback deliberately ships no video decoder of its own, because a decoder carries a license and a set of native binaries that not every application wants; an application that plays AV1 references this package and makes one call at start-up, and nothing else in the application ever names a decoder type. The decoder writes decoded frames straight into the playback session's own frame-buffer pool, so there is no copy between what it produces and what a presenter uploads to the graphics device. It is licensed under BSD-2-Clause, dav1d's own license, which is why it is a separate package from the MIT engine and the Apache-2.0 VideoPlayer add-in.

---

**CodeBrix.VideoProcessing**
NuGet Package ID: `CodeBrix.VideoProcessing.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.VideoProcessing](https://github.com/ellisnet/CodeBrix.VideoProcessing)

A fully managed, cross-platform FFmpeg/FFprobe wrapper that launches the external `ffmpeg`/`ffprobe` executables and parses their output — it is a wrapper, not a codec, and bundles no binaries. It provides media analysis (durations, streams, codecs, resolutions, bitrates), a fluent builder for converting, transcoding, and muxing video and audio, snapshot and GIF extraction, raw-frame and byte-stream piping in and out of FFmpeg, and a bridge between video frames and in-memory images via CodeBrix.Imaging. Progress and log callbacks plus a graceful cancellation model are included. The `ffmpeg` and `ffprobe` executables must be installed and on the PATH (or configured explicitly) at run time.

---

**CodeBrix.YamlParse**
NuGet Package ID: `CodeBrix.YamlParse.MitLicenseForever`
Source: [github.com/ellisnet/CodeBrix.YamlParse](https://github.com/ellisnet/CodeBrix.YamlParse)

A fully managed, cross-platform YAML library with no third-party dependencies, offering three layers: a low-level streaming scanner/parser/emitter, an XmlDocument-style representation model (`YamlStream`/`YamlDocument`/`YamlNode`) for loading, editing, and saving documents, and a high-level object serialization layer (`SerializerBuilder`/`DeserializerBuilder`) for reading and writing .NET objects to and from YAML. The builders are fluent, with extensive extension hooks for type converters, node deserializers, and naming conventions (camelCase, PascalCase, hyphenated, and more). All of its namespaces are under `CodeBrix.YamlParse`.

---

**FreePPlus**
NuGet Package ID: `FreePPlus.LgplLicenseForever`
Source: [github.com/ellisnet/FreePPlus](https://github.com/ellisnet/FreePPlus)

A .NET library that reads and writes Excel (`.xlsx`) files using the Office Open XML format, with no need for Microsoft Excel or COM interop. Feature coverage is broad: cell values and ranges, styling, data validation, conditional formatting, charts, pictures, shapes, comments, tables, pivot tables, a formula calculation engine, AutoFilter, merged cells, rich text, sparklines, workbook/worksheet protection, AES password encryption, and VBA macro support. Its types live in the `OfficeOpenXml` namespaces; image and font handling is provided by CodeBrix.Imaging.

---

**SilverAssertions**
NuGet Package ID: `SilverAssertions.ApacheLicenseForever`
Source: [github.com/ellisnet/SilverAssertions](https://github.com/ellisnet/SilverAssertions)

A fluent assertion API for .NET unit tests, letting you express expected outcomes with the readable, chainable `.Should()` extension-method pattern (for example `value.Should().BeGreaterThan(0).And.BeLessThan(100)`) plus "because" failure messages. It covers strings, numerics (including approximate floating-point comparisons), booleans, collections, and much more, all exposed through a single `using SilverAssertions;` namespace. It works with all major .NET test frameworks — xUnit v3, NUnit v4, MSTest v4, and MSpec — with automatic framework detection and no configuration. It must not be combined with another fluent-assertion library in the same test project.
