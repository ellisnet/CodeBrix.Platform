================================================================================
AGENT-README: CodeBrix.Platform
A Comprehensive Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.Platform is a cross-platform UI application framework for .NET 10. You
write your app ONCE using the WinUI XAML API surface (the same
"Microsoft.UI.Xaml.*" controls, XAML, code-behind, and data binding you would
use in a Windows App SDK app), and CodeBrix.Platform renders it natively on
Windows, Linux, and macOS desktops using a Skia-based rendering engine.

In short: one shared UI + business-logic codebase, multiple thin per-platform
"head" executables. A coding agent that understands this document can scaffold a
complete multi-platform CodeBrix.Platform application from scratch.

Key facts:
  - Target framework: .NET 10.0 (net10.0). The WPF head uses net10.0-windows.
  - UI API surface: WinUI / Microsoft.UI.Xaml (controls, XAML, x:Bind, etc.).
  - Rendering: Skia (SkiaSharp) on every platform.
  - Supported desktop targets: Windows (Win32 or WPF host), Linux (X11, native
    Wayland, or framebuffer), macOS (Apple Silicon and Intel).
  - Out of scope for this fork: mobile (iOS/Android), WebAssembly/browser.

Source repository:        https://github.com/ellisnet/CodeBrix.Platform
Canonical reference app:  https://github.com/ellisnet/JustBetweenUs
Licenses:                 Apache-2.0 (most packages), MIT (the SkiaSharp.Views
                          package). Every package id carries an explicit license
                          suffix — see "THE NUGET PACKAGES" below.

IMPORTANT: Throughout this guide, NuGet package NAMES carry a license suffix
(".ApacheLicenseForever", ".MitLicenseForever", or — for the LibVLC-based media
packages — ".LgplLicenseForever") while NAMESPACES do NOT. For example, the
package "CodeBrix.Platform.ApacheLicenseForever" provides the namespaces
"CodeBrix.Platform.UI.*", "Microsoft.UI.Xaml.*", and so on. Do not confuse
package ids with namespaces. The suffix reflects the license under which that
package is delivered; the vast majority of the framework is Apache-2.0. The ONLY
LGPL packages are the optional media-player add-ons (see "MEDIA PLAYER ADD-ON
PACKAGES" below) and the CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever
library they depend on.

================================================================================

THE BIG PICTURE: PROJECT ARCHITECTURE
=====================================
A CodeBrix.Platform solution is built from three kinds of projects. This is THE
canonical structure; follow it exactly.

  1. THE .Core PROJECT  (a net10.0 class library)
     - Holds your application logic, view models, services, and ALL of your
       NuGet package references for the UI framework and its extensions.
     - This is where "CodeBrix.Platform.ApacheLicenseForever" (the framework
       itself) and any optional extension packages are referenced.
     - It does NOT reference any platform "head" package.

  2. THE .UI SHARED PROJECT  (an MSBuild "Shared Project": .shproj + .projitems)
     - Holds the shared XAML: App.xaml, App.xaml.cs, and your Views
       (e.g. Views/MainPage.xaml + MainPage.xaml.cs).
     - A Shared Project is NOT compiled on its own. Its files are compiled INTO
       each head project that imports its .projitems file.

  3. ONE .<Platform> HEAD PROJECT PER TARGET  (a net10.0 executable, OutputType=Exe)
     - One per platform you ship: Win32Skia, WinWpfSkia, LinuxX11, LinuxWayland,
       LinuxFrameBuffer, MacOS.  (The names matter - see "PROJECT & HEAD NAMING"
       below. In particular, NEVER name the Win32 head ".Windows".)
     - Each head is tiny: it imports the .UI shared project, references the
       .Core project, references EXACTLY ONE platform "head" NuGet package, and
       contains a Program.cs with the startup bootstrap.

Dependency flow (arrows = "references"):

      Head (Exe)  ──►  .Core (library)  ──►  framework + extension packages
         │
         ├──►  imports .UI shared project (.projitems)  ──► App.xaml + Views
         │
         └──►  references exactly ONE platform head package
                 (e.g. CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever)

Why this split? The framework, your view models, and your XAML are 100% shared.
Only the head project and its single head package change per platform. Adding a
new platform target = adding one more thin head project.

================================================================================

PROJECT & HEAD NAMING
=====================
Name projects so they never collide with an SDK namespace, stay distinct from
each other and from the solution file, and read clearly. The layout below is
canonical — follow it.

THE RULE THAT MATTERS MOST: never give a head project a name whose segments match
a top-level SDK namespace your code uses unqualified — above all "Windows" (the
root of the WinRT "Windows.*" namespaces), and also "System". A head named
"MyApp.Windows" gives that project its own "MyApp.Windows" namespace, which
SHADOWS the global "Windows" namespace: an inline reference such as
"Windows.System.VirtualKey" in shared code then binds to "MyApp.Windows" and
fails to compile with CS0234 — on that ONE head only, which is baffling to
diagnose. (A "using Windows.System;" directive still resolves globally, so the
breakage is inconsistent and easy to miss.) This is why the Skia-on-Win32 head is
named ".Win32Skia", never ".Windows".

Also keep every project name distinct from the solution file's base name (a
"MyApp.Windows.csproj" sitting next to a "MyApp.Windows.slnx" is confusing), and
use the exact casing shown below.

RECOMMENDED LAYOUT (for an app named "MyApp"):

  1) CodeBrix.Platform (Skia-based) projects.
     Put these under a "CodeBrixPlatform" solution folder when the solution also
     contains non-Skia heads:

       MyApp.Core             shared class library: view models, services, and the
                              framework + extension package references (NOT a head)
       MyApp.UI               shared PROJECT (.shproj/.projitems, NOT an assembly):
                              App.xaml + the Views/XAML  (see note below)
       MyApp.LinuxFrameBuffer Linux framebuffer head
       MyApp.LinuxWayland     Linux native-Wayland head
       MyApp.LinuxX11         Linux X11 head
       MyApp.MacOS            macOS head
       MyApp.Win32Skia        Skia-on-Win32 head   (NEVER "MyApp.Windows")
       MyApp.WinWpfSkia       Skia-on-WPF head

     The "Skia" suffix appears ONLY on the two Windows heads, because Windows is
     the only OS that also ships a native head (WinUI / WPF) to disambiguate from.
     The Linux and macOS heads have no native counterpart, so they take no suffix.

     Why .UI is a shared project and not folded into .Core: the CodeBrix.Platform
     (Uno) XAML source-generator + build-task wiring does NOT flow across a
     ProjectReference. The XAML must be compiled INTO each head, which is exactly
     what a Shared Project (its .projitems imported by each head) does and a
     referenced .Core assembly cannot. Do not "tidy" the Views into .Core.

  2) .NET MAUI project.
     Put it under a "Mobile" solution folder when the solution also contains
     non-mobile heads:

       MyApp.Mobile           the .NET MAUI head

  3) Native (non-Skia, non-MAUI) heads:

       MyApp.WinUI            native WinUI 3 head
       MyApp.Wpf              native WPF head

OPTIONAL SUGGESTIONS:
  - If a solution has several native heads and you want them grouped, a "Native"
    solution folder for MyApp.WinUI / MyApp.Wpf is one option — but it is not
    required, and keeping them at the solution root is equally fine.
  - If you ever hit a namespace collision you cannot resolve by renaming, setting
    <RootNamespace>MyApp</RootNamespace> on the affected head keeps its generated
    code out of the colliding segment. Use this as a targeted fix for a specific
    collision, not as a blanket policy across all heads.

================================================================================

THE NUGET PACKAGES
==================
CodeBrix.Platform produces the following packages. Reference them WITHOUT a
version attribute and let NuGet resolve the latest published version (all of the
framework packages in a given release share one version; the SkiaSharp.Views
package is versioned to track the SkiaSharp release it vendors).

--- FRAMEWORK + EXTENSION PACKAGES (reference these in the .Core project) ---

  CodeBrix.Platform.ApacheLicenseForever          [REQUIRED]
      THE core UI framework. Provides the WinUI / Microsoft.UI.Xaml control set,
      the XAML runtime, layout, data binding, dispatching, and logging glue.
      Every CodeBrix.Platform app references this. It is self-contained (it folds
      in the Foundation, WinRT, Dispatching, and logging assemblies).

  CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever     [optional]
      Immediate-mode 2D drawing surface backed by SkiaSharp, for custom drawing
      inside XAML.

  CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever     [optional]
      OpenGL 3D drawing surface (GLCanvasElement) for embedding GPU-rendered
      content inside XAML; renders offscreen and composites into the Skia
      scene. Depends on CodeBrix.Platform.OpenGL.MitLicenseForever (added
      automatically). Requires an OpenGL 3.0+ context from the head; all six
      heads provide one: Win32/WPF (WGL), X11 (GLX), macOS (bundled ANGLE),
      Wayland (EGL, works under the default Vulkan presenter), FrameBuffer
      (DRM/GBM, or Mesa llvmpipe software GL on GPU-less systems — install
      libegl1 + libgl1-mesa-dri there).

  CodeBrix.Platform.Lottie.ApacheLicenseForever           [optional]
      Lottie / Skottie vector animation playback in XAML. Pair it with the
      "SkiaSharp.Skottie" package.

  CodeBrix.Platform.Svg.ApacheLicenseForever              [optional]
      SVG support (SvgImageSource) on Skia targets. Pair it with the
      "CodeBrix.SkiaSvg.MitLicenseForever" package.

  CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever     [optional]
      SkiaSharp XAML views (SKXamlCanvas, SKSwapChainPanel). Used internally by
      the Graphics2DSK / Lottie / Svg packages; reference it directly only if you
      use those view types yourself.

--- PLATFORM HEAD PACKAGES (reference EXACTLY ONE, in the head project) ---

  CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever      Windows (Win32 host)
  CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever        Windows (WPF host)
  CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever        Linux desktop (X11)
  CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever    Linux desktop (native Wayland)
  CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever Linux framebuffer (no desktop; kiosk/embedded)
  CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever      macOS (Apple Silicon + Intel)

  NOTE: A base package, "CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever",
  and the framework aggregate flow in TRANSITIVELY beneath each head package.
  You never reference the base runtime package directly — referencing one head
  package is all a head project needs.

  NOTE: On Windows you have two choices. The Win32 head is the simplest and most
  common. The WPF head is for hosting CodeBrix.Platform content inside a WPF
  desktop app context (see the WPF-specific section below).

  NOTE: On desktop Linux you also have two choices. The X11 head is the
  broad-compatibility option: it runs on X11 desktops AND on Wayland desktops
  (through XWayland, the X11 compatibility layer). The Wayland head is a pure,
  native Wayland client: it requires a Wayland compositor and fails fast with a
  clean error when none is present (it never falls back to X11/XWayland). Ship
  the X11 head for maximum reach, the Wayland head for a native, forward-looking
  Wayland experience — or both, as separate heads.

  NOTE: The X11 head renders with OpenGL, falling back to software rendering.
  A Vulkan renderer (pulled from the Uno Platform 6.7.x development line) also
  exists in the repo but is NOT a supported configuration yet: it is gated
  behind an internal-only FeatureConfiguration.Rendering.UseVulkanOnX11 flag
  with no public API to enable it, and package consumers cannot select it.

--- PERMANENT WAYLAND DIFFERENCES (protocol-inherent; not bugs, not planned work) ---

  The Wayland protocol deliberately withholds some window control from clients.
  APIs that work on the X11 head but are PERMANENT no-ops on the Wayland head
  (each logs a one-time Warning naming the API on first use):

  - AppWindow.Move / any window positioning. The compositor owns placement;
    clients cannot set global window coordinates, and cannot read them back
    either - AppWindow.Position always reports (0,0) on Wayland.
  - AppWindow.Resize and ApplicationView.TryResizeView. A client cannot force
    its outer window size; the compositor has the last word. (The window's
    INITIAL size, via ApplicationView.PreferredLaunchViewSize, does work.)
  - OverlappedPresenter.IsAlwaysOnTop. Core Wayland/xdg-shell has no
    always-on-top for regular application windows.
  - OverlappedPresenter.IsMinimizable / IsMaximizable. xdg-shell cannot remove
    those capabilities; compositor/decoration policy decides.
  - Minimized-state READBACK. A client can request minimize, but Wayland never
    tells it whether/when the window was unminimized, so
    OverlappedPresenter.State may report Minimized while the window is visible
    again. (Maximize/restore state DOES reflect correctly, including external
    maximize from the titlebar.)

  Related notes:
  - Drag & drop MAY NOT WORK on the Wayland head, depending on the compositor.
    The head's drop-target support (wl_data_device) is implemented and behaves
    correctly per protocol, but compositors with experimental Wayland sessions
    can deliver unusable drag events: on Cinnamon/Muffin (observed 2026-07),
    drags from XWayland sources arrive with garbage enter coordinates
    (wl_fixed minimum), so hit-testing never finds a drop target and the drop
    silently does nothing. This is a compositor-side bug, not planned work in
    this repo; drag & drop works normally on the X11 head.
  - Native content in a ContentPresenter is not hosted yet on Wayland (needs
    subsurfaces; parity plan P7). Until then the content is ignored with a
    one-time warning. The shipping WebView (offscreen WPE) and MediaPlayer
    (vmem) add-ins are windowing-agnostic by design and are NOT affected.
  - The window/taskbar icon comes from a .desktop file whose name matches the
    app id (the appxmanifest package name, falling back to the entry assembly
    name), placed in ~/.local/share/applications or /usr/share/applications
    with an Icon= entry. The xdg-toplevel-icon-v1 protocol is pinned in the
    bindings for a future in-process path, but common desktops (including
    Cinnamon/Muffin) do not support it yet.
  - Window self-activation (Window.Activate()) rides xdg-activation-v1 and is
    subject to compositor focus-stealing policy: without a recent user
    interaction the compositor may only flag the window as demanding attention
    rather than focusing it.

--- MEDIA PLAYER ADD-ON PACKAGE (optional; ONE package covers five heads) ---

  CodeBrix.Platform.MediaPlayer.LgplLicenseForever      Win32, WPF, X11, Wayland, FrameBuffer
      Adds MediaPlayerElement (audio / video playback) to every Skia head except
      macOS. LibVLC decodes into memory (the "vmem" output, via MediaPlayerCore's
      VideoFrameSink) and the frames are composited directly into the Skia scene
      (src/AddIns/Platform.UI.MediaPlayer.Skia) - no native child windows, no
      airspace problems, and NO XWayland: the Wayland head stays native. Reference
      it ONCE, in your app's .Core project, like the WebView add-on: every head
      inherits it, the Windows and Linux heads activate it (OS-gated ApiExtension
      registrations), and it is inert on the macOS head, which has built-in
      AVFoundation media support and needs no package or libvlc at all.
      This is the ONLY published CodeBrix.Platform package that is NOT Apache-2.0:
      playback is delivered via LibVLC, so it depends on
      "CodeBrix.Platform.MediaPlayerCore.LgplLicenseForever" (a managed port of
      LibVLCSharp) - all LGPL-2.1-or-later; the ".LgplLicenseForever" suffix is
      truth-in-labeling. The native libvlc runtime is NOT shipped in the package:
      on Linux install it via the system package manager
      (sudo apt install libvlc5 vlc-plugin-base - the base plugin set is enough,
      the full vlc application is NOT needed), and on Windows add the
      "VideoLAN.LibVLC.Windows" package to the Windows head project(s) only.
      Linux hardware decoding (optional): Debian's libvlc probes VAAPI/VDPAU
      regardless of --avcodec-hw, and with only vlc-plugin-base installed those
      probes fail (no GPU-surface-to-CPU converter) and VLC falls back to
      software decoding - playback works, at the cost of ~2s extra startup and
      "Failed to adapt decoder format to display" log noise. Installing
      vlc-plugin-video-output adds the VAAPI converter (libvaapi_filters) so
      hardware decode-with-copyback succeeds on the first attempt.
      Sample: samples/CodeBrixPlatform/MediaPlayerDemo.

      LEGACY, NEVER PUBLISH: Platform.UI.MediaPlayer.Skia.X11 / .Win32
      (package ids CodeBrix.Platform.WinUI.MediaPlayer.Skia.{X11,Win32}.LgplLicenseForever)
      are the superseded native-child-window add-ons (set_xwindow / set_hwnd
      embedding; X11/Win32 only, incompatible with Wayland and FrameBuffer). They
      remain in-repo for reference, are not packed by the central build driver,
      and must never be published.

--- WEBVIEW ADD-ON PACKAGE (optional; ONE package covers every head) ---

  CodeBrix.Platform.WebView.ApacheLicenseForever                     all heads
      Makes the XAML WebView2 control work on ALL Skia heads with a single
      package. What it delivers differs by head:
        - Windows (Win32) and Skia-on-WPF: the package bundles the Microsoft
          Edge WebView2 SDK redistributable (the native loader plus the managed
          WebView2 control assemblies) and copies it to the app output, backing
          the control with the Microsoft Edge WebView2 runtime. Only the SDK is
          shipped here — the Edge WebView2 runtime itself comes from the end
          user's Windows install. See THIRD-PARTY-NOTICES.txt (item 21).
        - macOS: inert — WKWebView is built into the OS.
        - Linux (X11, Wayland, AND FrameBuffer): web content is rendered
          offscreen by the system-installed WPE WebKit engine and composited
          directly into the Skia scene (no native child windows, no airspace
          problems — clipping, transforms, and z-order behave like any other
          XAML content). This Linux path is 100% Apache-2.0 managed code that
          P/Invokes the distro's WPE WebKit at run time; no WPE engine binaries
          ship in the package. Linux machines must have the engine installed:
          sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1
      When the engine is missing, creating a WebView throws
      PlatformNotSupportedException naming the missing library and that exact
      apt command. Reference this package ONCE, in the .Core project, like the
      other extension add-ons: every head gets it transitively. It activates the
      WPE path on the Linux heads, delivers the Microsoft Edge WebView2 payload
      to the app output on the Windows and Skia-on-WPF heads, and is inert on
      macOS (WKWebView is built in).
      CUSTOM USER-AGENT: on every head, app code can set the User-Agent string
      the WebView sends (an empty string restores the engine's default):
          myWebView.CoreWebView2.Settings.UserAgent = "MyApp/1.0";
      It may be set before or after the control loads, and applies to the next
      request. Backed natively on all six heads (WPE WebKit on Linux, Edge
      WebView2 on Windows/WPF, WKWebView customUserAgent on macOS). The default
      (no value set) is each engine's own desktop User-Agent - on Linux:
          Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/60.5 Safari/605.1.15
      Page-to-host messaging supports both the WebView2 idiom
      (window.chrome.webview.postMessage) and the WebKit idiom
      (window.webkit.messageHandlers.codebrixWebView.postMessage).
      This package ships at the same version as the rest of the family (the
      whole family is always published together) and requires a core of the
      same generation: the AddIn implements internal framework seams, so the
      core's InternalsVisibleTo grants must match. It supersedes the in-repo
      legacy Platform.UI.WebView.Skia.X11 project (GTK/WebKitGTK window
      embedding, X11-only, never published); never reference both.
      TODO / PLANNED DELETION: the legacy Platform.UI.WebView.Skia.X11 project
      is slated to be DELETED from this repository in a future release (kept
      temporarily "just in case", decision 2026-07-02). It is NOT supported.
      Its self-packed nupkg (CodeBrix.Platform.WinUI.WebView.Skia.X11), which
      Release builds still produce, must NEVER be published to nuget.org. When
      deleting it, also remove: its entries in the three root .slnx files, its
      _AdjustedOutputProjects line in src/Directory.Build.props, and its
      InternalsVisibleTo grants in Platform.UI + Platform.UWP AssemblyInfo.cs.
      Known v1 limitations on Linux: no IME (composed CJK/deadkey) text input,
      popups/new windows navigate the current view, and the mouse cursor does
      not change shape over links.

--- COMPANION PACKAGES used by the reference app (NOT produced by this repo) ---

  Microsoft.Extensions.Hosting              (.Core — generic host / DI)
  Microsoft.Extensions.Logging.Console      (.Core — console logging in DEBUG)
  SkiaSharp.Skottie                         (.Core — only if using Lottie)
  CodeBrix.SkiaSvg.MitLicenseForever        (.Core — only if using SVG)
  CodeBrix.Platform.Fonts.OpenSans.ApacheLicenseForever  (.Core — optional bundled font)

  RULE: All standard "SkiaSharp.*" packages are used AS-IS (SkiaSharp is not
  forked). Standard "Microsoft.Extensions.*" packages are used as-is.

================================================================================

WHICH PACKAGE GOES WHERE  (the single most important rule)
==========================================================
  - The .Core project references the FRAMEWORK + EXTENSION packages and your
    companion packages. It NEVER references a head package.
  - Each HEAD project references EXACTLY ONE platform head package, plus the
    .Core project, plus the .UI shared project. It adds nothing else UI-related,
    with ONE allowed exception, optional: the media-player add-on that matches
    that head (see "MEDIA PLAYER ADD-ON PACKAGES").
  - The WebView add-on (CodeBrix.Platform.WebView.ApacheLicenseForever) goes in
    .Core with the other extension add-ons — one reference, all heads get it,
    Linux heads activate it, the rest ignore it. On Linux machines the system
    WPE WebKit engine must be installed for it to work:
        sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1

If you put a head package in .Core, or more than one head package in a single
head project, the build will be wrong. One head project == one head package.

================================================================================

SETTING UP A NEW APP — STEP BY STEP
===================================
The following creates a JustBetweenUs-style solution. Replace "MyApp" with your
application name.

STEP 1 — Create the solution and the .Core library:

    dotnet new sln -n MyApp
    dotnet new classlib -n MyApp.Core --framework net10.0
    cd MyApp.Core
    dotnet add package CodeBrix.Platform.ApacheLicenseForever
    # add optional extension packages as needed (Graphics2DSK / Lottie / Svg ...)
    cd ..

STEP 2 — Create the .UI Shared Project (App.xaml + Views). A Shared Project is a
".shproj" with a sibling ".projitems". See "THE .UI SHARED PROJECT" below for
the exact file contents to create (App.xaml, App.xaml.cs, Views/MainPage.xaml,
Views/MainPage.xaml.cs, the .projitems, and the .shproj).

STEP 3 — Create one head project per target. For the Skia-on-Win32 head (name it
".Win32Skia", never ".Windows" — see "PROJECT & HEAD NAMING"):

    dotnet new console -n MyApp.Win32Skia --framework net10.0
    cd MyApp.Win32Skia
    dotnet add package CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever
    dotnet add reference ../MyApp.Core/MyApp.Core.csproj
    cd ..

Then edit the head .csproj (see "THE PLATFORM HEAD PROJECTS") to: set
OutputType=Exe, add the HAS_CODEBRIX defines, declare .xaml as <Page> items, and
import the .UI .projitems. Replace the generated Program.cs with the bootstrap
(see "THE BOOTSTRAP").

STEP 4 — Repeat STEP 3 for each additional platform, changing only the head
package and the ".Use…()" call in Program.cs (and, for the WPF head, the TFM —
see its dedicated section).

STEP 5 — Build and run a head:

    dotnet build MyApp.Win32Skia/MyApp.Win32Skia.csproj
    dotnet run --project MyApp.Win32Skia/MyApp.Win32Skia.csproj

================================================================================

THE .Core PROJECT  (class library)
==================================
Holds app logic + ALL framework/extension package references. Example .csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>MyApp</RootNamespace>
        <!-- CodeBrix.Platform uses these for internal conditional compilation -->
        <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>
      </PropertyGroup>

      <ItemGroup>
        <PackageReference Include="Microsoft.Extensions.Hosting" />
        <PackageReference Include="Microsoft.Extensions.Logging.Console" />

        <!-- The core UI framework (REQUIRED) -->
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />

        <!-- Optional extensions — include only what you use: -->
        <PackageReference Include="CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.Lottie.ApacheLicenseForever" />
        <!-- WebView2 control on every head (Linux needs the system WPE WebKit engine): -->
        <PackageReference Include="CodeBrix.Platform.WebView.ApacheLicenseForever" />
        <PackageReference Include="SkiaSharp.Skottie" />
        <PackageReference Include="CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.Svg.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.SkiaSvg.MitLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.Fonts.OpenSans.ApacheLicenseForever" />
      </ItemGroup>
    </Project>

Put your view models, services, and non-UI logic here. In the reference app the
view models are plain C# classes (e.g. MainViewModel) that the XAML binds to.

================================================================================

THE .UI SHARED PROJECT  (.shproj + .projitems)
==============================================
This is a Visual Studio "Shared Project". It is two files plus your XAML. Its
contents are compiled into whichever head imports the .projitems.

(A) MyApp.UI.projitems — lists the shared files. Note that each XAML file is a
<Page> with Generator "MSBuild:Compile", and each code-behind is <Compile> with
<DependentUpon>:

    <?xml version="1.0" encoding="utf-8"?>
    <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
      <PropertyGroup>
        <HasSharedItems>true</HasSharedItems>
        <SharedGUID>PUT-A-NEW-GUID-HERE</SharedGUID>
      </PropertyGroup>
      <PropertyGroup Label="Configuration">
        <Import_RootNamespace>MyApp.UI</Import_RootNamespace>
      </PropertyGroup>
      <ItemGroup>
        <Page Include="$(MSBuildThisFileDirectory)App.xaml">
          <SubType>Designer</SubType>
          <Generator>MSBuild:Compile</Generator>
        </Page>
        <Page Include="$(MSBuildThisFileDirectory)Views\MainPage.xaml">
          <SubType>Designer</SubType>
          <Generator>MSBuild:Compile</Generator>
        </Page>
      </ItemGroup>
      <ItemGroup>
        <Compile Include="$(MSBuildThisFileDirectory)App.xaml.cs">
          <DependentUpon>App.xaml</DependentUpon>
        </Compile>
        <Compile Include="$(MSBuildThisFileDirectory)Views\MainPage.xaml.cs">
          <DependentUpon>MainPage.xaml</DependentUpon>
        </Compile>
      </ItemGroup>
    </Project>

(B) MyApp.UI.shproj — the Visual Studio wrapper (lets the IDE open the shared
project). It imports the .projitems and the CodeSharing targets:

    <?xml version="1.0" encoding="utf-8"?>
    <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
      <PropertyGroup Label="Globals">
        <ProjectGuid>PUT-THE-SAME-GUID-HERE</ProjectGuid>
        <MinimumVisualStudioVersion>14.0</MinimumVisualStudioVersion>
      </PropertyGroup>
      <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
      <Import Project="$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.Default.props" />
      <Import Project="$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.props" />
      <PropertyGroup />
      <Import Project="MyApp.UI.projitems" Label="Shared" />
      <Import Project="$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.CSharp.targets" />
    </Project>

(C) App.xaml — the application's resource dictionary root (WinUI style):

    <Application
        x:Class="MyApp.App"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    </Application>

(D) App.xaml.cs — see "APP.XAML.CS PATTERNS" below for the full, exact pattern.

(E) Views/MainPage.xaml + Views/MainPage.xaml.cs — a normal WinUI Page. Bind to
your view models from .Core.

================================================================================

THE PLATFORM HEAD PROJECTS  (one Exe per target)
================================================
Every head project is nearly identical. The ONLY differences between heads are
(1) the single head package referenced, (2) the ".Use…()" call in Program.cs,
and (3) for the WPF head, the target framework. A standard (non-WPF) head:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <OutputType>Exe</OutputType>
        <RootNamespace>MyApp</RootNamespace>
        <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>
      </PropertyGroup>

      <!-- Treat .xaml files as CodeBrix.Platform XAML pages -->
      <ItemGroup>
        <Page Include="**\*.xaml" Exclude="bin\**\*.xaml;obj\**\*.xaml" />
        <None Remove="**\*.xaml" />
      </ItemGroup>

      <!-- Pull in the shared App.xaml + Views -->
      <Import Project="..\MyApp.UI\MyApp.UI.projitems" Label="Shared" />

      <ItemGroup>
        <ProjectReference Include="..\MyApp.Core\MyApp.Core.csproj" />
      </ItemGroup>

      <!-- EXACTLY ONE platform head package (this one = Windows/Win32): -->
      <ItemGroup>
        <PackageReference Include="CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever" />
      </ItemGroup>
    </Project>

For the OTHER non-WPF heads, change ONLY the head package line:

    Linux (X11):            CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever
    Linux (native Wayland): CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever
    Linux (framebuffer):    CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever
    macOS:                  CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever

--- THE WPF HEAD IS SPECIAL ---

The WPF head differs in two ways:

  1. Target framework is net10.0-windows (NOT plain net10.0). The Skia.Wpf
     runtime package flows a Microsoft.WindowsDesktop.App.WPF FrameworkReference,
     and the SDK requires a Windows target platform for that (otherwise you get
     NETSDK1136).

  2. Do NOT set <UseWPF>true</UseWPF>. Setting it would make WPF's build targets
     try to treat your CodeBrix.Platform .xaml <Page> items as WPF XAML. WPF is
     loaded by the host at RUNTIME; your XAML stays CodeBrix.Platform XAML.

WPF head .csproj (only the PropertyGroup + package line differ from above):

    <PropertyGroup>
      <TargetFramework>net10.0-windows</TargetFramework>
      <OutputType>Exe</OutputType>
      <RootNamespace>MyApp</RootNamespace>
      <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>
    </PropertyGroup>
    ...
    <PackageReference Include="CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever" />

================================================================================

THE BOOTSTRAP  (Program.cs in each head)
========================================
Every head has the same shape: create the host builder, supply your App, select
the platform with a ".Use…()" call, build, and run. The host builder type is
"CodeBrixPlatformHostBuilder" in namespace "CodeBrix.Platform.UI.Hosting".

Standard head (Linux / macOS / framebuffer / Win32 synchronous form):

    using CodeBrix.Platform.UI.Hosting;
    using System;

    namespace MyApp;

    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            App.InitializeLogging();

            var host = CodeBrixPlatformHostBuilder.Create()
                .App(() => new App())
                .UseWindowsWin32()   // <-- platform selector; see table below
                .Build();

            host.Run();
        }
    }

The async form (used by the Windows head in the reference app) is equivalent —
use whichever you prefer:

    [STAThread]
    public static async Task Main(string[] args)
    {
        App.InitializeLogging();
        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            .UseWindowsWin32()
            .Build();
        await host.RunAsync();
    }

--- PLATFORM SELECTOR TABLE (the ".Use…()" method per head) ---

  Platform target        Head package (suffix)                 Bootstrap call
  ---------------------   -----------------------------------   ----------------------
  Windows (Win32)         ...Runtime.Skia.Win32...              .UseWindowsWin32()
  Windows (WPF)           ...Runtime.Skia.Wpf...                .UseWindowsWpf()
  Linux (X11)             ...Runtime.Skia.X11...                .UseLinuxX11()
  Linux (native Wayland)  ...Runtime.Skia.Wayland...            .UseLinuxWayland()
  Linux (framebuffer)     ...Runtime.Skia.FrameBuffer...        .UseLinuxFrameBuffer()
  macOS                   ...Runtime.Skia.MacOS...              .UseMacOS()

  These ".Use…()" methods are all extension methods in the
  "CodeBrix.Platform.UI.Hosting" namespace, surfaced by the corresponding head
  package. A head sees only the one ".Use…()" method that matches its package.

--- OPTIONAL host-builder flag: .UseDirectSkiaCanvasMode() (EXPERIMENTAL) ---

EXPERIMENTAL: chaining ".UseDirectSkiaCanvasMode()" onto the host builder makes
SKXamlCanvas draw each frame straight into its on-screen bitmap buffer (one fewer
full-frame copy per paint); it is an app-wide, one-way opt-in that changes nothing
if omitted. Enable it only to test performance/stability — it may change or be
removed.

--- THE WPF HEAD NEEDS A SOFTWARE-RENDERING LINE ---

The WPF host's default OpenGL renderer draws via raw OpenGL onto WPF's own
DirectX-composited window, which causes "airspace" conflicts on many systems
(the window appears but content never composites — a blank window). Force
software rendering right after Build(). This requires an extra using:

    using CodeBrix.Platform.UI.Hosting;
    using CodeBrix.Platform.UI.Runtime.Skia.Wpf;   // for WpfHost + RenderSurfaceType
    using System;

    namespace MyApp;

    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            App.InitializeLogging();

            var host = CodeBrixPlatformHostBuilder.Create()
                .App(() => new App())
                .UseWindowsWpf()
                .Build();

            if (host is WpfHost wpfHost)
            {
                wpfHost.RenderSurfaceType = RenderSurfaceType.Software;
            }

            host.Run();
        }
    }

================================================================================

APP.XAML.CS PATTERNS
====================
App.xaml.cs lives in the .UI shared project and is compiled into every head. It
derives from Microsoft.UI.Xaml.Application. The reference pattern:

    using Microsoft.Extensions.Logging;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Navigation;
    using System;

    namespace MyApp;

    public partial class App : Application
    {
        public App()
        {
            // (Optional) set a default font, e.g. the bundled Open Sans package.
            // The "ms-appx:///<PackageId-without-suffix>/Fonts/<file>.ttf" form
            // loads a font shipped inside a referenced package:
            global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily =
                "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";

            // (Optional) register your DI services here, then:
            InitializeComponent();
        }

        protected Window MainWindow { get; private set; }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindow = new Window { Title = "My App" };

            if (MainWindow.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                MainWindow.Content = rootFrame;
                rootFrame.NavigationFailed += OnNavigationFailed;
            }

            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(Views.MainPage), args.Arguments);
            }

            MainWindow.Activate();
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) =>
            throw new InvalidOperationException(
                $"Failed to load {e.SourcePageType.FullName}: {e.Exception}");

        // Called from each head's Program.Main BEFORE building the host.
        public static void InitializeLogging()
        {
    #if DEBUG
            var factory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("CodeBrix.Platform", LogLevel.Warning);
                builder.AddFilter("Microsoft", LogLevel.Warning);
            });

            global::CodeBrix.Platform.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

    #if HAS_CODEBRIX
            global::CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
    #endif
    #endif
        }
    }

Notes:
  - The framework's logging bridge is enabled by setting
    "CodeBrix.Platform.Extensions.LogExtensionPoint.AmbientLoggerFactory" and
    then calling "CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging
    .LoggingAdapter.Initialize()". The LoggingAdapter is folded into the core
    framework package (there is no separate adapter package to install).
  - Call App.InitializeLogging() from Program.Main BEFORE CodeBrixPlatformHost
    Builder.Create(), exactly as shown in every head.

================================================================================

KEY NAMESPACES
==============
Your UI code is written against the WinUI API surface:

    using Microsoft.UI.Xaml;             // Application, Window, FrameworkElement
    using Microsoft.UI.Xaml.Controls;    // Page, Frame, Button, TextBox, ...
    using Microsoft.UI.Xaml.Navigation;  // navigation event args
    using Microsoft.UI.Xaml.Data;        // binding, converters
    using Microsoft.UI.Xaml.Media;       // brushes, transforms
    using Windows.UI;                     // Colors, Color

CodeBrix.Platform-specific entry points:

    using CodeBrix.Platform.UI.Hosting;  // CodeBrixPlatformHostBuilder + .Use…() methods
    // CodeBrix.Platform.UI.FeatureConfiguration  -> framework-wide settings (fonts, etc.)
    // CodeBrix.Platform.Extensions.LogExtensionPoint -> logging bridge
    // CodeBrix.Platform.UI.Runtime.Skia.Wpf -> WpfHost + RenderSurfaceType (WPF head only)

XAML namespace URIs (in .xaml files) are the standard WinUI ones:

    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"

================================================================================

COMPILATION CONSTANTS
=====================
Define these in EVERY project that participates in the UI (the .Core library and
every head). The framework uses them for internal conditional compilation:

    HAS_CODEBRIX
    HAS_CODEBRIX_WINUI

Set them via:

    <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>

================================================================================

WRITING XAML AND VIEWS
======================
Write standard WinUI XAML. A minimal MainPage:

    <Page
        x:Class="MyApp.Views.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
            <TextBlock Text="Hello from CodeBrix.Platform" />
            <Button Content="Click me" Click="OnClick" />
        </StackPanel>
    </Page>

Code-behind (MainPage.xaml.cs):

    using Microsoft.UI.Xaml.Controls;

    namespace MyApp.Views;

    public sealed partial class MainPage : Page
    {
        public MainPage() => InitializeComponent();
        void OnClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) { /* ... */ }
    }

Bind to view models from your .Core project using standard {Binding} / {x:Bind}.

================================================================================

OPTIONAL FEATURE PACKAGES — HOW TO ADD THEM
===========================================
Each optional capability is one (or two) package references in the .Core project.

  2D SkiaSharp drawing:
      CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever

  3D OpenGL drawing (GLCanvasElement):
      CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever

  Lottie animations:
      CodeBrix.Platform.Lottie.ApacheLicenseForever
      SkiaSharp.Skottie
      CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever

  SVG (SvgImageSource):
      CodeBrix.Platform.Svg.ApacheLicenseForever
      CodeBrix.SkiaSvg.MitLicenseForever

  Bundled Open Sans font:
      CodeBrix.Platform.Fonts.OpenSans.ApacheLicenseForever
      (then set FeatureConfiguration.Font.DefaultTextFontFamily as shown above)

================================================================================

PLATFORM-SPECIFIC NOTES
=======================

WINDOWS:
  - Use the Win32 head for the simplest desktop experience.
  - The WPF head needs net10.0-windows, no <UseWPF>, and the software-rendering
    line (see the WPF sections above).

macOS:
  - The macOS head package contains a small native library. A macOS package
    BUILT ON WINDOWS is managed-only (no native library) — fine to COMPILE
    against, but to RUN on a Mac the macOS head package must have been produced
    on Apple Silicon. A correctly built macOS package is a universal binary and
    runs on both Apple Silicon and Intel Macs.

LINUX (X11):
  - The broad-compatibility desktop Linux head: runs on X11 desktops and on
    Wayland desktops via XWayland.
  - On some Linux ARM64 systems (e.g. Raspberry Pi), the native SkiaSharp library
    may fail to auto-load FreeType, throwing an "undefined symbol" error at
    startup. If you hit this, preload FreeType when launching, e.g.:
        LD_PRELOAD=/usr/lib/aarch64-linux-gnu/libfreetype.so.6 dotnet run ...
    This is a SkiaSharp native-asset packaging issue, not a CodeBrix.Platform
    issue, and is expected to resolve in newer SkiaSharp native packages.

LINUX (native Wayland):
  - A pure Wayland client: it speaks the Wayland protocol directly and never
    uses X11/XWayland. It REQUIRES a Wayland compositor; without one it fails
    fast at startup with a clean "This application requires a Wayland
    compositor." message and exit code 1 (use the X11 head for X11/XWayland
    environments).
  - Permissively licensed (Apache/MIT) top to bottom — no LGPL/GPL components —
    unlike other .NET native-Wayland offerings.
  - Window decorations: on KDE/wlroots-family compositors the server draws them;
    on GNOME/Cinnamon they are drawn client-side via the system's libdecor
    library. For a native-looking title bar on Debian/Ubuntu-family desktops the
    libdecor GTK plugin should be present (packages "libdecor-0-0" +
    "libdecor-0-plugin-1-gtk"; preinstalled on most GNOME desktops).
  - Rendering defaults to Vulkan (VK_KHR_wayland_surface), falling back to
    wl_shm software rendering when Vulkan is unavailable. The two GPU paths
    (Vulkan and OpenGL ES via EGL) are peers: each falls back directly to
    software, never to the other. An explicit backend can be selected in code:

        .UseLinuxWayland(wayland =>
            wayland.RenderingBackend(WaylandRenderingBackend.Vulkan))

    with WaylandRenderingBackend members:
        Default      Vulkan, falling back to software (same as omitting this).
        Vulkan       Same Vulkan-else-software selection, stated explicitly.
        VulkanForced Vulkan with NO fallback: if the Vulkan renderer cannot be
                     created, the app prints a clean two-line "requires Vulkan
                     rendering" message to stderr and exits with code 1. Use
                     this when silent software fallback could be mistaken for
                     working Vulkan (e.g. hardware qualification, perf tests).
        OpenGLES     OpenGL ES via EGL, falling back to software.
        Software     wl_shm software rendering only.
    The same choices exist as feature flags (set before Build()):
    FeatureConfiguration.Rendering.UseVulkanOnWayland, .UseOpenGLOnWayland,
    and .ForceVulkanOnWayland.
    Environment variables are consulted ONLY when neither the builder backend
    nor the feature flags decided: CODEBRIX_WAYLAND_NO_GPU=1 forces software
    rendering; CODEBRIX_WAYLAND_USE_EGL=1 selects the OpenGL ES path. If both
    are set, NO_GPU wins. Code always beats environment.
  - Working, at parity with the X11 head: flyout-based controls (ComboBox
    dropdowns, MenuFlyout, ToolTip, dialogs), rich clipboard (text, HTML, PNG
    images, file lists, custom formats — copy AND paste), fractional
    (non-integer) display scaling, custom title bars
    (ExtendContentIntoTitleBar), and window activation (xdg-activation;
    compositor focus policy applies). ACCEPTING drag-and-drop from other
    applications is implemented (initiating a drag is not implemented on the
    X11 head either) but may not work on some compositors — see the "Drag &
    drop MAY NOT WORK" note in the PERMANENT WAYLAND DIFFERENCES section.
  - Not yet implemented in this head (deferred): touch input, native-view
    hosting in a ContentPresenter (needs subsurfaces), and IME text input (IME
    is missing on the X11 head too).
  - For the protocol-inherent gaps that will never change (window positioning,
    forced resize, always-on-top, and friends) see the "PERMANENT WAYLAND
    DIFFERENCES" section earlier in this file.

LINUX (framebuffer):
  - Use the framebuffer head for embedded/kiosk devices with no X11/desktop
    environment. Same app code; different head package and ".UseLinuxFrameBuffer()".

================================================================================

COMMON PITFALLS TO AVOID
========================
 1. DO NOT confuse package ids with namespaces. Package ids carry a license
    suffix (".ApacheLicenseForever" / ".MitLicenseForever"); namespaces do not
    (they are "CodeBrix.Platform.*", "Microsoft.UI.Xaml.*").

 2. DO NOT reference a platform head package in the .Core library, and DO NOT
    put more than one head package in a single head project. One head project ==
    one head package.

 3. DO NOT forget the HAS_CODEBRIX and HAS_CODEBRIX_WINUI defines in the .Core
    library AND in every head. Missing them causes incorrect conditional
    compilation.

 4. DO NOT set <UseWPF>true</UseWPF> on the WPF head. WPF is loaded at runtime;
    setting UseWPF makes the WPF build targets misinterpret your XAML pages.

 5. DO NOT use plain net10.0 for the WPF head. It must be net10.0-windows
    (otherwise NETSDK1136 from the WPF FrameworkReference).

 6. DO NOT forget the software-rendering line on the WPF head, or you may get a
    blank/black window from the OpenGL airspace conflict.

 7. DO NOT forget to declare your .xaml as <Page> items in each head and to
    import the .UI .projitems. The shared XAML is compiled INTO the head; it is
    not a standalone assembly.

 8. DO NOT try to publish a macOS package built on Windows for actually RUNNING
    on macOS — it lacks the native library. Build the macOS head package on
    Apple Silicon.

 9. DO NOT target a framework below .NET 10. CodeBrix.Platform requires net10.0.

10. DO NOT call CodeBrixPlatformHostBuilder before App.InitializeLogging(). The
    reference app calls InitializeLogging() first in every head's Main.

11. DO NOT expect the Wayland head to run in an X11-only session — it requires a
    Wayland compositor and fails fast (by design) when none is present. For an
    app that must run everywhere on desktop Linux, ship the X11 head (alone, or
    alongside a Wayland head).

================================================================================

THE CANONICAL REFERENCE APPLICATION
===================================
"JustBetweenUs" is THE reference application that demonstrates the entire
structure described in this document. When in doubt, read it.

    Repository:  https://github.com/ellisnet/JustBetweenUs
    Branch:      main
    Folder:      CodeBrixPlatform/

Project map (under CodeBrixPlatform/):

    JustBetweenUs.Core/          The .Core library (framework + extension package
                                 references, view models, services).
    JustBetweenUs.UI/            The .UI shared project (.shproj + .projitems):
                                 App.xaml, App.xaml.cs, Views/MainPage.xaml(.cs).
    JustBetweenUs.Windows/       Windows (Win32) head  -> .UseWindowsWin32()
    JustBetweenUs.WpfSkia/       Windows (WPF) head    -> .UseWindowsWpf() + software render
    JustBetweenUs.LinuxX11/      Linux (X11) head      -> .UseLinuxX11()
    JustBetweenUs.LinuxWayland/  Linux (native Wayland) head -> .UseLinuxWayland()
    JustBetweenUs.LinuxFrameBuffer/  Linux framebuffer -> .UseLinuxFrameBuffer()
    JustBetweenUs.MacOs/         macOS head            -> .UseMacOS()

To read a file directly, fetch its raw content, e.g.:

    https://raw.githubusercontent.com/ellisnet/JustBetweenUs/main/CodeBrixPlatform/JustBetweenUs.Windows/Program.cs

Study these files to scaffold your own app:
  - JustBetweenUs.Core/JustBetweenUs.Core.csproj  (which packages go in .Core)
  - JustBetweenUs.UI/JustBetweenUs.UI.projitems   (shared-project file layout)
  - JustBetweenUs.UI/App.xaml.cs                  (font + logging + launch pattern)
  - JustBetweenUs.<Head>/JustBetweenUs.<Head>.csproj  (per-head package + TFM)
  - JustBetweenUs.<Head>/Program.cs               (per-head bootstrap)

================================================================================

BUILDING THE NUGET PACKAGES  (maintainers only)
===============================================
This section is for maintainers building/publishing CodeBrix.Platform itself —
NOT for app authors consuming the packages. The package set is produced by the
pack-only driver project:

    build/CodeBrix.Platform.Build.csproj

It gathers the already-built Release outputs of the platform projects and packs
them into NuGet packages under:

    nugets/<Configuration>/<BuildVersion>/

VERSIONING: the driver computes a date-stamped BuildVersion automatically
(format 1.<years-since-2026>.<dayOfYear>.<minuteOfDay>, all from UTC now) and
stamps that ONE version on every package in the run. Packing only runs in the
Release configuration. You can override the version with -p:BuildVersion=1.x.y.z
to reuse an EXISTING version instead of stamping a fresh one.

--- ON WINDOWS: build the ENTIRE package set (auto version) ---

This is the normal full build. BuildVersion is auto-computed, so you do NOT set
it. Build the solution in Release first (the packer gathers already-built
Release outputs), then build the driver in Release:

    dotnet build CodeBrix.Platform.Windows.slnx -c Release
    dotnet build build\CodeBrix.Platform.Build.csproj -c Release

All packages land in  nugets\Release\<auto-version>\  sharing that one version.

BUILD FAILS WITH CS2012 (task DLL locked): the solution build may fail with
    error CS2012: Cannot open '...\Platform.XamlMerge.Task\obj\Release\
    CodeBrix.Platform.XamlMerge.Task.v0.dll' for writing -- The process cannot
    access the file ... because it is being used by another process.
This is NOT a code error. The XamlMerge.Task assembly is an MSBuild build-task
DLL, and a lingering MSBuild node / compiler server (MSBuild node reuse or
VBCSCompiler) from a prior build is still holding it open. Fix: shut down the
build servers, then rebuild:
    dotnet build-server shutdown
    dotnet build CodeBrix.Platform.Windows.slnx -c Release -nodeReuse:false
Passing -nodeReuse:false keeps the node from re-locking the DLL across back-to-
back Release builds (harmless to add to the driver build too). An open Visual
Studio instance can also hold the lock; close it if the shutdown does not clear
it. (Observed 2026-07-08.)

--- ON macOS (Apple Silicon): build ONLY the macOS package (pinned version) ---

The macOS head package contains a native dylib that can ONLY be built on Apple
Silicon, so it is NOT produced by the Windows run above. Rebuild it on an Apple
Silicon Mac, pinning the version to the SAME version the Windows run already
produced and published to nuget.org. That keeps its sibling dependencies
(aggregate / base runtime / FrameBuffer) version-locked to the published set, so
publishing ONLY the rebuilt macOS package still restores cleanly.

Do NOT run the full driver on macOS — it would also try to pack the Windows-only
packages. Instead pack just the macOS csproj (exactly what the driver does for
that one project) from the repo root, substituting the published version for
1.0.197.800 below:

    dotnet pack src/Platform.UI.Runtime.Skia.MacOS/Platform.UI.Runtime.Skia.MacOS.csproj \
      -c Release \
      -p:PackageVersion=1.0.197.800 \
      --output nugets/Release/1.0.197.800

-p:PackageVersion (NOT -p:Version) sets only the NuGet package version while
still flowing to the ProjectReference dependency versions. This produces:

    nugets/Release/1.0.197.800/CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever.1.0.197.800.nupkg

PREREQUISITES on the Mac: full Xcode installed (the native build uses xcodebuild;
the driver only enables the native step on Apple Silicon) and the native build
script src/Platform.UI.Runtime.Skia.MacOS/PlatformNativeMac/build.sh must be
executable (chmod +x). A correctly built macOS package is a universal binary and
runs on both Apple Silicon and Intel Macs.

VERIFY THE macOS PACKAGE BEFORE UPLOADING. A managed-only package (no native
dylib) packs WITHOUT error on any machine where the native step is skipped (e.g.
not Apple Silicon, or BuildNativeMac=false) and is useless at runtime. After
packing, confirm the native universal binary is inside the .nupkg:

    unzip -l nugets/Release/<version>/CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever.<version>.nupkg \
      | grep runtimes/osx/native
    # Must list: runtimes/osx/native/libCodeBrixNativeMac.dylib
    # Then confirm it is a fat binary (extract it first, then):
    #   lipo -info .../runtimes/osx/native/libCodeBrixNativeMac.dylib
    #   expect: "Architectures in the fat file: ... x86_64 arm64"

On an Apple-Silicon build the csproj FAILS the pack with an explicit error if the
native dylib is absent (so a green pack there means the dylib is present); the
verify step above still matters when packing anywhere the native step is skipped.

================================================================================

PROVENANCE
==========
The CodeBrix.Platform codebase is a fork of the Uno Platform (version 6.5.x),
re-licensed and re-packaged under the CodeBrix.Platform name. For complete
third-party attribution, component provenance, and license texts, see the
THIRD-PARTY-NOTICES.txt file that ships in the root of every CodeBrix.Platform
NuGet package (and in the source repository).

================================================================================

QUICK REFERENCE CARD
====================

Architecture:     .Core (library) + .UI (shared project) + one Exe head per platform
Target:           net10.0  (WPF head: net10.0-windows)
Defines (all):    HAS_CODEBRIX;HAS_CODEBRIX_WINUI
UI API:           WinUI / Microsoft.UI.Xaml.*
Host builder:     CodeBrixPlatformHostBuilder.Create() (namespace CodeBrix.Platform.UI.Hosting)

Core framework pkg:   CodeBrix.Platform.ApacheLicenseForever            (in .Core)
Extensions (in .Core):
    Graphics2DSK ->   CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever
    Graphics3DGL ->   CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever
    Lottie       ->   CodeBrix.Platform.Lottie.ApacheLicenseForever (+ SkiaSharp.Skottie)
    Svg          ->   CodeBrix.Platform.Svg.ApacheLicenseForever (+ CodeBrix.SkiaSvg.MitLicenseForever)
    Skia views   ->   CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever

Head packages (exactly one per head) and bootstrap call:
    Windows/Win32  ->  CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever       .UseWindowsWin32()
    Windows/WPF    ->  CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever         .UseWindowsWpf()  (+ Software render)
    Linux/X11      ->  CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever         .UseLinuxX11()
    Linux/Wayland  ->  CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever     .UseLinuxWayland()  (needs a Wayland compositor)
    Linux/FB       ->  CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever .UseLinuxFrameBuffer()
    macOS          ->  CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever       .UseMacOS()

Bootstrap:
    var host = CodeBrixPlatformHostBuilder.Create().App(() => new App()).UseXxx().Build();
    host.Run();   // or: await host.RunAsync();

Reference app:    https://github.com/ellisnet/JustBetweenUs  (main, CodeBrixPlatform/)

================================================================================
