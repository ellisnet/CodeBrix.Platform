# CodeBrix.Platform

**CodeBrix.Platform** is a cross-platform desktop UI framework for .NET. You write your
application once against the WinUI XAML API surface (`Microsoft.UI.Xaml.*` controls, XAML
markup, code-behind, and data binding), and CodeBrix.Platform renders it natively on
**Windows, Linux, and macOS** through a Skia-based rendering engine — one shared UI and
business-logic codebase, plus one thin executable "head" per platform.

CodeBrix.Platform is provided as a .NET 10 framework and a family of NuGet packages: the core
framework package `CodeBrix.Platform.ApacheLicenseForever`, one `CodeBrix.Platform.Runtime.Skia.*`
package per platform head, and a set of optional add-in packages listed below. Every package in
the family is catalogued, with a full description of each one, in
[CODEBRIX-PLATFORM-README.md](CODEBRIX-PLATFORM-README.md).

CodeBrix.Platform supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025; and will be actively supported by Microsoft until Nov 14, 2028.
Please update your C#/.NET code and projects to the latest LTS version of Microsoft .NET.

## Installation

An application needs exactly two packages to start: the core framework in its shared class
library, and one platform head package in each head executable. For a Linux X11 head:

```
dotnet add package CodeBrix.Platform.ApacheLicenseForever
dotnet add package CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever
```

Note that the NuGet package ID and the namespace are different - there is no package named plain `CodeBrix.Platform`:

* NuGet package ID: `CodeBrix.Platform.ApacheLicenseForever`
* Assembly and primary namespaces: `Microsoft.UI.Xaml.*` and `Windows.*` for your UI code, and
  `CodeBrix.Platform.UI.Hosting` for the application bootstrap - i.e.
  `using CodeBrix.Platform.UI.Hosting;`

XML documentation (IntelliSense) ships alongside the assemblies.

The head package brings the core framework and the shared Skia runtime
(`CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever`) in transitively, along with build
targets that set the head's compilation constants. Reference the core package once, in the
shared library, and exactly one head package in each head project.

Everything else is optional. Each add-in package in the table below is added to the shared
library alongside the core package, and each one ships its own `AGENT-README.txt` describing
its API in full — read that file for the add-in you are using:

```
dotnet add package CodeBrix.Platform.AudioPlayer.ApacheLicenseForever
```

## CodeBrix.Platform supports:

* The WinUI XAML API surface — controls, panels, styles, resource dictionaries, templates,
  visual states, animations and data binding — written exactly as documented for WinUI
* Skia rendering on six platform heads: Windows Win32, Windows WPF, Linux X11, native Linux
  Wayland, the Linux frame buffer (kiosk and embedded devices with no desktop), and macOS
  (Apple Silicon and Intel)
* Software and GPU render paths per head (OpenGL on Win32 and X11, Metal on macOS), selected
  by the host builder or by `FeatureConfiguration`
* One shared codebase: a `.Core` class library, a `.UI` shared project of XAML, and one thin
  executable head project per target platform
* Application windowing (`AppWindow`, `OverlappedPresenter`), UI-thread dispatching
  (`DispatcherQueue`) and navigation
* `Windows.Storage` files and folders, file/folder/save pickers, and the clipboard
* Font loading and preloading, Unicode/ICU text handling, and shaped bidirectional text
* Add-in packages for 2D and 3D drawing, SVG, Lottie animation, media, audio and MIDI, video,
  an embedded browser, a terminal view, charts, flex layout, host-free text layout,
  persisted settings, and a full code editor
* A diagnostics overlay, and a logging bridge onto `Microsoft.Extensions.Logging`

Mobile (iOS/Android) and WebAssembly/browser targets are out of scope for this framework.

## How an app is structured

A CodeBrix.Platform solution has three kinds of projects:

1. **`.Core`** — a class library holding your application logic, view models,
   and the framework + add-in NuGet package references.
2. **`.UI`** — a shared project (`.shproj` + `.projitems`) holding the shared
   XAML: `App.xaml` and your views.
3. **One executable "head" per platform** — a thin project that references
   `.Core`, imports the `.UI` shared project, and references exactly one
   platform head package.

## Packages

| Package | Role |
| --- | --- |
| `CodeBrix.Platform.ApacheLicenseForever` | The core UI framework (required) |
| `CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever` | The shared Skia runtime beneath every head — arrives transitively; never reference it directly |
| `CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever` | Full code/text editor control (`AdvancedTextEdit` element: syntax highlighting, folding, completion, search, snippets, line numbers) |
| `CodeBrix.Platform.AppSettings.ApacheLicenseForever` | Application settings persisted as JSON in a portable `settings.sqlite` (`AppSettingsService` / `AppSettingProperty`), with automatic backups, corrupt-file recovery, and export/import — storage only, no settings UI |
| `CodeBrix.Platform.AudioPlayer.ApacheLicenseForever` | Audio playback (`AudioPlayer` element + `SoundEffect`) and MIDI music through a SoundFont or SFZ instrument (`MidiPlayer` element), fully managed on all heads |
| `CodeBrix.Platform.CommandBar.ApacheLicenseForever` | Desktop tool bars (`ToolBarTray` / `ToolBar` / `ToolButton` family with groups, separators, spacers and chevron overflow) bound to `ICommand`, with SVG and raster icons |
| `CodeBrix.Platform.FlexPanel.ApacheLicenseForever` | CSS flexbox-style layout panel (`FlexPanel` element with `Grow` / `Shrink` / `Basis` / `Order` / `AlignSelf` attached properties) |
| `CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever` | 2D SkiaSharp drawing |
| `CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever` | 3D OpenGL drawing (`GLCanvasElement`) |
| `CodeBrix.Platform.Lottie.ApacheLicenseForever` | Lottie / Skottie animations |
| `CodeBrix.Platform.MediaPlayer.LgplLicenseForever` | Makes the XAML `MediaPlayerElement` (audio and video) work on the Win32, WPF, X11, Wayland and frame-buffer heads, via libvlc |
| `CodeBrix.Platform.PlotterView.ApacheLicenseForever` | Chart plotting view (`PlotterControl` element hosting a CodeBrix.Plotter `PlotModel`: 40+ series types, linear / logarithmic / date-time / category / polar axes, annotations, legends) with pan, zoom, data-point tracker and reset from mouse, keyboard and touch |
| `CodeBrix.Platform.Svg.ApacheLicenseForever` | SVG (`SvgImageSource`) support |
| `CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever` | SkiaSharp XAML views |
| `CodeBrix.Platform.TerminalView.ApacheLicenseForever` | Terminal emulator view (`TerminalControl` element rendering a CodeBrix.Terminal engine: VT100 / VT220 / xterm escape sequences, 256-color ANSI attributes, scrollback, selection, clipboard copy and paste) — transport-agnostic, for SSH clients, embedded consoles and build-output panes |
| `CodeBrix.Platform.TextLayout.ApacheLicenseForever` | Shaped, bidirectional text layout without XAML — measurement, caret and hit-test geometry, selection rectangles, glyph outlines, drawing to any `SKCanvas` |
| `CodeBrix.Platform.VideoPlayer.ApacheLicenseForever` | Video playback (`VideoPlayer` element: AV1 in WebM, Matroska and CodeBrix `.cbv` files, with the AudioPlayer transport surface plus muting, `Stretch` letterboxing, a colour-grading effect chain, drawable layers, and captions and chapters as data) — composed on the GPU wherever a head can supply a context, on the processor everywhere else, with the framework's own SkiaSharp and no second copy; AV1 and Opus decoding arrive as separate packages the application registers |
| `CodeBrix.Platform.WebView.ApacheLicenseForever` | WebView on every head (adds Linux support via the system WPE WebKit engine) |
| `CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever` | Windows (Win32) host |
| `CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever` | Windows (WPF) host |
| `CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever` | Linux (X11) host |
| `CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever` | Linux (native Wayland) host |
| `CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever` | Linux framebuffer host |
| `CodeBrix.Platform.Runtime.Skia.FrameBuffer.Emulated.ApacheLicenseForever` | The frame-buffer head rendered off-screen, used by the CodeBrix.Develop IDE when it debugs a frame-buffer application in its emulator — the IDE substitutes it at build time; never reference it directly |
| `CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever` | macOS host |

The framework and add-in packages are referenced by the `.Core` library; each
head project references exactly one of the `Runtime.Skia.*` host packages.

The WebView package is referenced once, in the `.Core` library, like the other
add-in packages — every head gets it, it activates on the Linux heads, and it
is inert on Windows/WPF/macOS (which have built-in WebView support). On Linux it
uses the distro's WPE WebKit engine at run time, which must be installed:

```
sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1
```

## Sample Code

### A platform head's startup

```csharp
using CodeBrix.Platform.UI.Hosting;

var host = CodeBrixPlatformHostBuilder.Create()
    .App(() => new App())
    .UseWindowsWin32()   // or UseWindowsWpf / UseLinuxX11 / UseLinuxWayland / UseLinuxFrameBuffer / UseMacOS
    .Build();

host.Run();
```

### A minimal page

```xml
<Page
    x:Class="MyApp.Views.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
        <TextBlock Text="Hello from CodeBrix.Platform" />
        <Button Content="Click me" Click="OnClick" />
    </StackPanel>
</Page>
```

```csharp
using Microsoft.UI.Xaml.Controls;

namespace MyApp.Views;

public sealed partial class MainPage : Page
{
    public MainPage() => InitializeComponent();
    void OnClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { /* ... */ }
}
```

### Using an add-in package from XAML

```xml
<Page
    x:Class="MyApp.Views.PlayerPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:audio="using:CodeBrix.Platform.UI.AudioPlayer.Skia">
    <Grid>
        <audio:AudioPlayer x:Name="Player" Source="ms-appx:///Assets/song.mp3" AutoPlay="True" />
        <Slider VerticalAlignment="Bottom" Margin="16"
            Maximum="{Binding DurationSeconds, ElementName=Player}"
            Value="{Binding PositionSeconds, ElementName=Player, Mode=TwoWay}" />
    </Grid>
</Page>
```

## Documentation

The NuGet package includes `AGENT-README.txt`, a complete API reference and usage guide written for AI coding agents - point your agent at that file when it is writing code against this library.

Each add-in package ships its own `AGENT-README.txt` in the root of that package, covering that
add-in's API on its own. [README-INDEX.txt](README-INDEX.txt) maps every documentation file in
this repository to the package it documents, and
[CODEBRIX-PLATFORM-README.md](CODEBRIX-PLATFORM-README.md) is the catalogue of the whole
CodeBrix family.

Additional sample code and usage examples are available in the sample applications in this
repository — one per add-in, each building for every head:
https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform

The canonical reference application, **JustBetweenUs**, ships a complete app across all six
platform heads: https://github.com/ellisnet/JustBetweenUs — see the `CodeBrixPlatform/` folder.

## License

CodeBrix.Platform is licensed under the Apache License 2.0 - see the
[LICENSE](https://github.com/ellisnet/CodeBrix.Platform/blob/main/LICENSE) file. Two packages in
the family carry a different license, named in their package IDs:
`CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever` is MIT, and
`CodeBrix.Platform.MediaPlayer.LgplLicenseForever` is LGPL-2.1-or-later.

For licensing and provenance information about the open source code included in
this package, see [THIRD-PARTY-NOTICES.txt](https://github.com/ellisnet/CodeBrix.Platform/blob/main/THIRD-PARTY-NOTICES.txt).
