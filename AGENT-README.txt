================================================================================
AGENT-README: CodeBrix.Platform
A Guide for AI Coding Agents - CONSUMING the CodeBrix.Platform.ApacheLicenseForever,
CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever and
CodeBrix.Platform.Runtime.Skia.{Win32,Wpf,X11,Wayland,FrameBuffer,
FrameBuffer.Emulated,MacOS}.ApacheLicenseForever NuGet packages
================================================================================

This file covers the CORE framework package, the shared Skia runtime package and
the seven platform HEAD packages - everything an application needs to build and
run with no optional add-ins. Each optional add-in package produced by this
repository has its own AGENT-README.txt; the catalogue in INSTALLATION routes
you to it.

OVERVIEW
========
CodeBrix.Platform is a cross-platform desktop UI application framework for
.NET 10 or later. You write your app ONCE using the WinUI XAML API surface (the
same "Microsoft.UI.Xaml.*" controls, XAML, code-behind, and data binding you
would use in a Windows App SDK app), and CodeBrix.Platform renders it natively
on Windows, Linux, and macOS desktops using a Skia-based rendering engine.

In short: one shared UI + business-logic codebase, multiple thin per-platform
"head" executables. A coding agent that understands this document can scaffold
a complete multi-platform CodeBrix.Platform application from scratch.

Key facts:
  - Target framework: net10.0. The WPF head uses net10.0-windows.
  - UI API surface: WinUI / Microsoft.UI.Xaml (controls, XAML, x:Bind, etc.).
  - Rendering: Skia (SkiaSharp) on every platform.
  - Supported desktop targets: Windows (Win32 or WPF host), Linux (X11, native
    Wayland, or framebuffer), macOS (Apple Silicon and Intel).
  - Out of scope for this framework: mobile (iOS/Android), WebAssembly/browser.

Provenance: CodeBrix.Platform is a fork of an upstream open-source
WinUI-compatible UI framework, re-namespaced and re-packaged under the CodeBrix
name. Framework-specific namespaces are "CodeBrix.Platform.*"; the WinUI/UWP
API surface keeps its standard "Microsoft.UI.*" / "Windows.*" namespaces. Do
NOT use the upstream project's namespaces or package ids - they do not exist
here. Full third-party attribution is in the THIRD-PARTY-NOTICES.txt that ships
in every package.

Source repository:        https://github.com/ellisnet/CodeBrix.Platform
Canonical reference app:  https://github.com/ellisnet/JustBetweenUs

IMPORTANT: Throughout this guide, NuGet package NAMES carry a license suffix
(".ApacheLicenseForever", ".MitLicenseForever", or - for the LibVLC-based media
package - ".LgplLicenseForever") while NAMESPACES do NOT. For example, the
package "CodeBrix.Platform.ApacheLicenseForever" provides the namespaces
"CodeBrix.Platform.UI.*", "Microsoft.UI.Xaml.*", and so on. Do not confuse
package ids with namespaces. The suffix permanently binds that package id to
its license; every package in THIS file is Apache-2.0.

================================================================================

INSTALLATION
============
The nine packages this file covers. Reference them WITHOUT a version attribute
and let NuGet resolve the latest published version; the whole family is always
published together at one version.

  CodeBrix.Platform.ApacheLicenseForever                          [REQUIRED]
      THE core UI framework: the WinUI / Microsoft.UI.Xaml control set, the
      XAML runtime and source generator, layout, data binding, dispatching,
      windowing, storage/pickers/clipboard APIs, the Toolkit helpers and the
      logging glue. Self-contained (it folds in the Foundation, WinRT,
      Dispatching, Toolkit and logging-adapter assemblies).
      Goes in: the .Core project (see WHICH PACKAGE GOES WHERE).

  CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever             [transitive]
      The shared Skia runtime beneath every head (SkiaHost base class,
      FontFamilyHelper). Flows in automatically beneath each head package.
      NEVER reference it directly.

  Platform HEAD packages - reference EXACTLY ONE, in each head project:

  CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever         Windows (Win32 host)
  CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever           Windows (WPF host)
  CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever           Linux desktop (X11 / XWayland)
  CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever       Linux desktop (native Wayland)
  CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever   Linux framebuffer (no desktop; kiosk/embedded)
  CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever         macOS (Apple Silicon + Intel)

  CodeBrix.Platform.Runtime.Skia.FrameBuffer.Emulated.ApacheLicenseForever
      Used by CodeBrix.Develop when debugging a FrameBuffer app in its
      emulator; NEVER reference it directly. (The IDE swaps it in for the real
      FrameBuffer package at build time; your csproj is never modified.) It
      surfaces the same UseLinuxFrameBuffer() bootstrap and builder API.

    dotnet add package CodeBrix.Platform.ApacheLicenseForever            # in .Core
    dotnet add package CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever   # in ONE head

WHICH ONE DO I REFERENCE: the core package in .Core, exactly one head package
per head project, nothing else from this list. A head package brings the core
and the base runtime in transitively, plus buildTransitive targets that set
CodeBrixRuntimeIdentifier=Skia and the head's compilation constants for you.

License: Apache-2.0 (all nine packages).

NuGet dependencies (by id, all automatic): the head packages depend on
CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever and
CodeBrix.Platform.ApacheLicenseForever; the Skia runtime depends on the standard
SkiaSharp packages (SkiaSharp is used AS-IS, not forked). The WPF head flows a
Microsoft.WindowsDesktop.App.WPF FrameworkReference. Standard
Microsoft.Extensions.* packages are used as-is.

Requirements per head:
  - Win32:       Windows. An OpenGL driver (ICD) is optional; without one the
                 head renders with software Skia.
  - WPF:         Windows; the head project must target net10.0-windows and must
                 NOT set <UseWPF>true</UseWPF> (see THE WPF HEAD IS SPECIAL).
  - X11:         Linux with a DISPLAY environment variable set (the head only
                 activates when DISPLAY looks like "[host]:display[.screen]").
                 Runs on X11 desktops and on Wayland desktops through XWayland.
  - Wayland:     Linux with a running Wayland compositor (fails fast otherwise).
                 Client-side decorations on GNOME/Cinnamon use the system's
                 libdecor (packages "libdecor-0-0" + "libdecor-0-plugin-1-gtk").
  - FrameBuffer: Linux with no desktop. P/Invokes the distro's libinput and
                 libxkbcommon for input, and DRM/GBM/EGL for GPU rendering
                 (falls back to software rendering on the /dev/fb0 device).
                 The process must be able to open the framebuffer device, the
                 DRM card (/dev/dri/card*) and the input devices (/dev/input/*)
                 - on a Debian-family system that typically means membership of
                 the "video" and "input" groups, or running as the console user.
  - macOS:       The package contains a native universal (arm64 + x86_64)
                 dylib; Metal rendering by default.

OTHER PACKAGES FROM THIS REPOSITORY (not covered here)
-----------------------------------------------------
Optional add-ins for CodeBrix.Platform apps. Each goes in the .Core project
(see OPTIONAL FEATURE PACKAGES). One line each; read the linked file for the
API, usage and pitfalls.

  CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever      Apache-2.0
      Immediate-mode 2D SkiaSharp drawing surface inside XAML.
      see src/AddIns/Platform.WinUI.Graphics2DSK/AGENT-README.txt
  CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever      Apache-2.0
      OpenGL 3D surface (GLCanvasElement) composited into the Skia scene.
      see src/AddIns/Platform.WinUI.Graphics3DGL/AGENT-README.txt
  CodeBrix.Platform.Lottie.ApacheLicenseForever            Apache-2.0
      Lottie / Skottie vector animation playback in XAML (pair with
      SkiaSharp.Skottie).
      see src/AddIns/Platform.UI.Lottie/AGENT-README.txt
  CodeBrix.Platform.Svg.ApacheLicenseForever               Apache-2.0
      SvgImageSource on Skia targets (pair with CodeBrix.SkiaSvg.MitLicenseForever).
      see src/AddIns/Platform.UI.Svg/AGENT-README.txt
  CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever      MIT
      SkiaSharp XAML views (SKXamlCanvas, SKSwapChainPanel); used by the
      Graphics2DSK / Lottie / Svg add-ins.
      see src/AddIns/CodeBrix.Platform.SkiaSharp.Views/AGENT-README.txt
  CodeBrix.Platform.MediaPlayer.LgplLicenseForever         LGPL-2.1-or-later
      MediaPlayerElement audio/video playback via LibVLC on the Win32, WPF,
      X11, Wayland and FrameBuffer heads (macOS has built-in media support).
      The ONLY non-Apache package in the family.
      see src/AddIns/Platform.UI.MediaPlayer.Skia/AGENT-README.txt
  CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever  Apache-2.0
      Full code/text editor control (syntax highlighting, folding, completion).
      see src/AddIns/Platform.UI.AdvancedTextEdit/AGENT-README.txt
  CodeBrix.Platform.AppSettings.ApacheLicenseForever       Apache-2.0
      Application settings store (JSON values in one portable SQLite file);
      the only add-in that is not a UI control.
      see src/AddIns/Platform.AppSettings/AGENT-README.txt
  CodeBrix.Platform.AudioPlayer.ApacheLicenseForever       Apache-2.0
      AudioPlayer / MidiPlayer / SoundEffect - fully managed audio on all heads.
      see src/AddIns/Platform.UI.AudioPlayer.Skia/AGENT-README.txt
  CodeBrix.Platform.FlexPanel.ApacheLicenseForever         Apache-2.0
      CSS flexbox-style XAML layout panel.
      see src/AddIns/Platform.UI.FlexPanel/AGENT-README.txt
  CodeBrix.Platform.PlotterView.ApacheLicenseForever       Apache-2.0
      Chart view hosting a CodeBrix.Plotter PlotModel with full interaction.
      see src/AddIns/Platform.UI.PlotterView/AGENT-README.txt
  CodeBrix.Platform.TerminalView.ApacheLicenseForever      Apache-2.0
      Terminal emulator control (VT100/xterm) on a Skia surface.
      see src/AddIns/Platform.UI.TerminalView/AGENT-README.txt
  CodeBrix.Platform.TextLayout.ApacheLicenseForever        Apache-2.0
      Text shaping/bidi/caret/hit-test/outline API with no XAML required.
      see src/AddIns/Platform.UI.TextLayout/AGENT-README.txt
  CodeBrix.Platform.VideoPlayer.ApacheLicenseForever       Apache-2.0
      VideoPlayer - AV1 video from WebM/Matroska and .cbv files, on all heads,
      on the GPU where the head can give one. AV1 and Opus decoding arrive as
      separate packages the application registers.
      see src/AddIns/Platform.UI.VideoPlayer.Skia/AGENT-README.txt
  CodeBrix.Platform.WebView.ApacheLicenseForever           Apache-2.0
      Makes the WebView2 control work on every head (WPE WebKit on Linux).
      see src/AddIns/Platform.UI.WebView.Skia/AGENT-README.txt

Toolkits for Microsoft's OWN UI frameworks (NOT for CodeBrix.Platform apps;
they share no build-time code with the framework above):

  CodeBrix.Platform.WinUI.ApacheLicenseForever,
  CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever,
  CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever      Apache-2.0
      Helper toolkits for native WinUI 3 / Windows App SDK apps (MVVM
      foundation; Skia-rendered image and Lottie controls).
      see src-platforms/Platform.WinUI/AGENT-README.txt
  CodeBrix.Platform.WPF.ApacheLicenseForever               Apache-2.0
      Helper toolkit for native WPF apps.
      see src-platforms/Platform.WPF/AGENT-README.txt
  CodeBrix.Platform.Mobile.ApacheLicenseForever            Apache-2.0
      Helper toolkit for .NET MAUI apps.
      see src-platforms/Platform.Mobile/AGENT-README.txt

Companion packages used by the reference app (NOT produced by this repo):

  Microsoft.Extensions.Hosting              (.Core - generic host / DI)
  Microsoft.Extensions.Logging.Console      (.Core - console logging in DEBUG)
  CodeBrix.Platform.Fonts.OpenSans.ApacheLicenseForever  (.Core - bundled font)

================================================================================

KEY NAMESPACES / USINGS
=======================
Your UI code is written against the WinUI API surface:

    using Microsoft.UI.Xaml;                       // Application, Window, FrameworkElement
    using Microsoft.UI.Xaml.Controls;              // Page, Frame, Button, TextBox, ContentDialog, ...
    using Microsoft.UI.Xaml.Navigation;            // navigation event args
    using Microsoft.UI.Xaml.Data;                  // IValueConverter, binding
    using Microsoft.UI.Xaml.Media;                 // brushes, transforms, FontFamily
    using Microsoft.UI.Dispatching;                // DispatcherQueue
    using Microsoft.UI.Windowing;                  // AppWindow, OverlappedPresenter
    using Windows.UI;                              // Colors, Color
    using Windows.Storage;                         // StorageFile, StorageFolder
    using Windows.Storage.Pickers;                 // FileOpenPicker, FileSavePicker, FolderPicker
    using Windows.ApplicationModel.DataTransfer;   // Clipboard, DataPackage
    using Windows.Graphics.Display;                // DisplayOrientations (FrameBuffer head)

CodeBrix.Platform-specific entry points:

    using CodeBrix.Platform.UI.Hosting;            // CodeBrixPlatformHostBuilder, CodeBrixPlatformHost,
                                                   // .Use...() methods, Win32HostBuilder, X11HostBuilder,
                                                   // WaylandHostBuilder, IWindowsSkiaHostBuilder,
                                                   // X11RenderingBackend, WaylandRenderingBackend
    using CodeBrix.Platform.UI.Runtime.Skia;       // FramebufferHostBuilder, FilePickerOptions,
                                                   // FolderPickerOptions, SoftwareKeyboardOptions,
                                                   // SoftwareKeyHeight, UserInterfaceScale, SkiaHost
    using CodeBrix.Platform.UI.Runtime.Skia.Win32; // Win32Host, RenderSurfaceType   (Win32 head only)
    using CodeBrix.Platform.UI.Runtime.Skia.Wpf;   // WpfHost, RenderSurfaceType,
                                                   // WpfDispatcherScheduling        (WPF head only)
    using CodeBrix.Platform.UI.Runtime.Skia.MacOS; // MacSkiaHost, RenderSurfaceType (macOS head only)
    using CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer;  // FrameBufferHost (FrameBuffer head only)
    using CodeBrix.Platform.WinUI.Runtime.Skia.X11;             // X11ApplicationHost
    using CodeBrix.Platform.WinUI.Runtime.Skia.Wayland;         // WaylandApplicationHost
    // CodeBrix.Platform.UI.FeatureConfiguration            -> framework-wide settings
    // CodeBrix.Platform.UI.Xaml.Media.FontFamilyHelper     -> font preloading
    // CodeBrix.Platform.Extensions.LogExtensionPoint       -> logging bridge
    // CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter
    // CodeBrix.Platform.UI.Toolkit                         -> ElevatedView, StorageFileHelper
    // CodeBrix.Platform.UI.Converters                      -> *ToVisibilityConverter and friends
    // CodeBrix.Platform.Diagnostics.UI                     -> DiagnosticsOverlay
    // CodeBrix.Platform.UI.Markup                          -> FromJsonExtension

XAML namespace URIs (in .xaml files) are the standard WinUI ones:

    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"

Toolkit types in XAML: xmlns:toolkit="using:CodeBrix.Platform.UI.Toolkit",
xmlns:conv="using:CodeBrix.Platform.UI.Converters".

================================================================================

THE BIG PICTURE: PROJECT ARCHITECTURE
=====================================
A CodeBrix.Platform solution is built from three kinds of projects. This is THE
canonical structure; follow it exactly.

  1. THE .Core PROJECT  (a net10.0 class library)
     - Holds your application logic, view models, services, and ALL of your
       NuGet package references for the UI framework and its add-ins.
     - This is where "CodeBrix.Platform.ApacheLicenseForever" (the framework
       itself) and any optional add-in packages are referenced.
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

      Head (Exe)  -->  .Core (library)  -->  framework + add-in packages
         |
         +-->  imports .UI shared project (.projitems)  -->  App.xaml + Views
         |
         +-->  references exactly ONE platform head package
                 (e.g. CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever)

Why this split? The framework, your view models, and your XAML are 100% shared.
Only the head project and its single head package change per platform. Adding a
new platform target = adding one more thin head project.

================================================================================

PROJECT & HEAD NAMING
=====================
Name projects so they never collide with an SDK namespace, stay distinct from
each other and from the solution file, and read clearly. The layout below is
canonical - follow it.

THE RULE THAT MATTERS MOST: never give a head project a name whose segments match
a top-level SDK namespace your code uses unqualified - above all "Windows" (the
root of the WinRT "Windows.*" namespaces), and also "System". A head named
"MyApp.Windows" gives that project its own "MyApp.Windows" namespace, which
SHADOWS the global "Windows" namespace: an inline reference such as
"Windows.System.VirtualKey" in shared code then binds to "MyApp.Windows" and
fails to compile with CS0234 - on that ONE head only, which is baffling to
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
                              framework + add-in package references (NOT a head)
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
     XAML source-generator + build-task wiring does NOT flow across a
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
    solution folder for MyApp.WinUI / MyApp.Wpf is one option - but it is not
    required, and keeping them at the solution root is equally fine.
  - If you ever hit a namespace collision you cannot resolve by renaming, setting
    <RootNamespace>MyApp</RootNamespace> on the affected head keeps its generated
    code out of the colliding segment. Use this as a targeted fix for a specific
    collision, not as a blanket policy across all heads.

================================================================================

WHICH PACKAGE GOES WHERE  (the single most important rule)
==========================================================
  - The .Core project references the FRAMEWORK package, every ADD-IN package
    you use, and your companion packages. It NEVER references a head package.
  - Each HEAD project references EXACTLY ONE platform head package, plus the
    .Core project, plus the .UI shared project. It adds NOTHING else
    UI-related: no add-in packages, no second head package. Add-ins (including
    the MediaPlayer and WebView add-ins) are referenced ONCE, in .Core; every
    head inherits them and each add-in activates itself on the heads it
    supports.
  - The ONLY non-CodeBrix package a head project may need is one an add-in's
    own AGENT-README tells you to put there (for example a native runtime
    redistributable for Windows). Never decide that on your own.

If you put a head package in .Core, or more than one head package in a single
head project, the build will be wrong. One head project == one head package.

================================================================================

OPTIONAL FEATURE PACKAGES - HOW TO ADD THEM
===========================================
Each optional capability is one (or two) package references in the .Core
project - never in a head project. Every add-in package:

  - is referenced once, in .Core, WITHOUT a version attribute;
  - flows to every head transitively; an add-in that only works on some heads
    is inert on the others (it never breaks a build);
  - brings its own package dependencies in automatically (a sibling CodeBrix
    library, a SkiaSharp.* package, ...) - the add-in's AGENT-README says
    which, and which companion packages YOU must add alongside it (e.g.
    SkiaSharp.Skottie for Lottie, CodeBrix.SkiaSvg.MitLicenseForever for Svg);
  - ships at the same version as the rest of the family, and needs a core of
    the same generation (several add-ins implement internal framework seams).

Some add-ins need a system-installed engine on Linux (WebView: WPE WebKit;
MediaPlayer: libvlc) - the add-in's AGENT-README gives the exact apt command.
Read the add-in's file before adding it; this file does not repeat that detail.

A bundled font is added the same way (CodeBrix.Platform.Fonts.OpenSans.
ApacheLicenseForever in .Core), then selected with
FeatureConfiguration.Font.DefaultTextFontFamily - see FONTS below.

================================================================================

SETTING UP A NEW APP - STEP BY STEP
===================================
The following creates a JustBetweenUs-style solution. Replace "MyApp" with your
application name.

STEP 1 - Create the solution and the .Core library:

    dotnet new sln -n MyApp
    dotnet new classlib -n MyApp.Core --framework net10.0
    cd MyApp.Core
    dotnet add package CodeBrix.Platform.ApacheLicenseForever
    # add optional add-in packages here as needed (see their AGENT-READMEs)
    cd ..

STEP 2 - Create the .UI Shared Project (App.xaml + Views). A Shared Project is a
".shproj" with a sibling ".projitems". See "THE .UI SHARED PROJECT" below for
the exact file contents to create (App.xaml, App.xaml.cs, Views/MainPage.xaml,
Views/MainPage.xaml.cs, the .projitems, and the .shproj).

STEP 3 - Create one head project per target. For the Skia-on-Win32 head (name it
".Win32Skia", never ".Windows" - see "PROJECT & HEAD NAMING"):

    dotnet new console -n MyApp.Win32Skia --framework net10.0
    cd MyApp.Win32Skia
    dotnet add package CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever
    dotnet add reference ../MyApp.Core/MyApp.Core.csproj
    cd ..

Then edit the head .csproj (see "THE PLATFORM HEAD PROJECTS") to: set
OutputType=Exe, add the HAS_CODEBRIX defines, declare .xaml as <Page> items, and
import the .UI .projitems. Replace the generated Program.cs with the bootstrap
(see "THE BOOTSTRAP").

STEP 4 - Repeat STEP 3 for each additional platform, changing only the head
package and the ".Use...()" call in Program.cs (and, for the WPF head, the TFM -
see its dedicated section).

STEP 5 - Build and run a head:

    dotnet build MyApp.Win32Skia/MyApp.Win32Skia.csproj
    dotnet run --project MyApp.Win32Skia/MyApp.Win32Skia.csproj

================================================================================

THE .Core PROJECT  (class library)
==================================
Holds app logic + ALL framework/add-in package references. Example .csproj:

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

        <!-- Optional add-ins - include only what you use, e.g.: -->
        <PackageReference Include="CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever" />
        <PackageReference Include="CodeBrix.Platform.WebView.ApacheLicenseForever" />
        <!-- Optional bundled font: -->
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

(A) MyApp.UI.projitems - lists the shared files. Note that each XAML file is a
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

(B) MyApp.UI.shproj - the Visual Studio wrapper (lets the IDE open the shared
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

(C) App.xaml - the application's resource dictionary root (WinUI style):

    <Application
        x:Class="MyApp.App"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    </Application>

(D) App.xaml.cs - see "APP.XAML.CS PATTERNS" below for the full, exact pattern.

(E) Views/MainPage.xaml + Views/MainPage.xaml.cs - a normal WinUI Page. Bind to
your view models from .Core.

================================================================================

THE PLATFORM HEAD PROJECTS  (one Exe per target)
================================================
Every head project is nearly identical. The ONLY differences between heads are
(1) the single head package referenced, (2) the ".Use...()" call in Program.cs,
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
the platform with a ".Use...()" call, build, and run. The host builder type is
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

The async form (used by the Windows head in the reference app) is equivalent -
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

--- PLATFORM SELECTOR TABLE (the ".Use...()" method per head) ---

  Platform target        Head package (suffix)                 Bootstrap call
  ---------------------   -----------------------------------   ----------------------
  Windows (Win32)         ...Runtime.Skia.Win32...              .UseWindowsWin32()
  Windows (WPF)           ...Runtime.Skia.Wpf...                .UseWindowsWpf()
  Linux (X11)             ...Runtime.Skia.X11...                .UseLinuxX11()
  Linux (native Wayland)  ...Runtime.Skia.Wayland...            .UseLinuxWayland()
  Linux (framebuffer)     ...Runtime.Skia.FrameBuffer...        .UseLinuxFrameBuffer()
  macOS                   ...Runtime.Skia.MacOS...              .UseMacOS()

  These ".Use...()" methods are all extension methods in the
  "CodeBrix.Platform.UI.Hosting" namespace, surfaced by the corresponding head
  package. A head sees only the one ".Use...()" method that matches its package.
  Every one except UseMacOS() also has an overload taking a configuration
  lambda - see PER-HEAD CONFIGURATION in the CORE API REFERENCE.

--- THE WPF HEAD NEEDS A SOFTWARE-RENDERING LINE ---

The WPF host's default OpenGL renderer draws via raw OpenGL onto WPF's own
DirectX-composited window, which causes "airspace" conflicts on many systems
(the window appears but content never composites - a blank window). Force
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

CORE API REFERENCE
==================

HOST BUILDER (namespace CodeBrix.Platform.UI.Hosting)
-----------------------------------------------------

    public class CodeBrixPlatformHostBuilder : ICodeBrixPlatformHostBuilder
    {
        public static CodeBrixPlatformHostBuilder Create();
        public CodeBrixPlatformHost Build();
    }

    public interface ICodeBrixPlatformHostBuilder
    {
        CodeBrixPlatformHost Build();
    }

    // extension methods (class CodeBrixPlatformHostBuilderExtensions)
    ICodeBrixPlatformHostBuilder App<TApplication>(Func<TApplication> appBuilder)
        where TApplication : Microsoft.UI.Xaml.Application
    ICodeBrixPlatformHostBuilder AfterInit(Action action)
        // runs after the host is initialized and BEFORE the run loop starts
    ICodeBrixPlatformHostBuilder UseDirectSkiaCanvasMode()      // EXPERIMENTAL

    public abstract class CodeBrixPlatformHost
    {
        public void Run();
        public Task RunAsync();
    }

Build() returns the concrete host for the selected head (Win32Host, WpfHost,
X11ApplicationHost, WaylandApplicationHost, FrameBufferHost, MacSkiaHost - all
derive from SkiaHost in CodeBrix.Platform.UI.Runtime.Skia, which derives from
CodeBrixPlatformHost). Pattern-match on it to set host properties between
Build() and Run(), as the WPF example above does.

AfterInit is the place for work that needs the platform up but must precede the
first frame (e.g. reading FrameBufferHost state, wiring diagnostics):

    var host = CodeBrixPlatformHostBuilder.Create()
        .App(() => new App())
        .AfterInit(() => Console.Error.WriteLine("host initialized"))
        .UseLinuxX11()
        .Build();

EXPERIMENTAL: chaining ".UseDirectSkiaCanvasMode()" onto the host builder makes
SKXamlCanvas draw each frame straight into its on-screen bitmap buffer (one fewer
full-frame copy per paint); it is an app-wide, one-way opt-in that changes nothing
if omitted. Order relative to the .Use...() call does not matter. Enable it only
to test performance/stability - it may change or be removed.

PER-HEAD CONFIGURATION
----------------------
Each head's ".Use...()" method has an overload taking a lambda over that head's
builder (macOS excepted). Builder calls are chainable and return the builder.

  WINDOWS / WIN32

    ICodeBrixPlatformHostBuilder UseWindowsWin32()
    ICodeBrixPlatformHostBuilder UseWindowsWin32(Action<Win32HostBuilder> action)

    public class Win32HostBuilder
    {
        public Win32HostBuilder PreloadMediaPlayer(bool preload);
            // pre-initializes the LibVLC media player at startup (only useful
            // with the MediaPlayer add-in; harmless otherwise)
    }

    // host (namespace CodeBrix.Platform.UI.Runtime.Skia.Win32)
    public class Win32Host : SkiaHost
    {
        public RenderSurfaceType? RenderSurfaceType { get; set; }  // null = auto-detect
    }
    public enum RenderSurfaceType { Software, OpenGL }

    Feature flag: FeatureConfiguration.Rendering.UseOpenGLOnWin32 (bool?) - null
    (default) uses OpenGL when available, otherwise software.

  WINDOWS / WPF

    ICodeBrixPlatformHostBuilder UseWindowsWpf(Action<IWindowsSkiaHostBuilder> windowsBuilder = null)

    // extension methods on IWindowsSkiaHostBuilder
    IWindowsSkiaHostBuilder WpfApplication(Func<System.Windows.Application> action)
        // supply your own WPF Application instance to host inside
    IWindowsSkiaHostBuilder DispatcherScheduling(WpfDispatcherScheduling scheduling)

    // host (namespace CodeBrix.Platform.UI.Runtime.Skia.Wpf)
    public class WpfHost : SkiaHost
    {
        public RenderSurfaceType? RenderSurfaceType { get; set; }        // null = auto
        public WpfDispatcherScheduling DispatcherScheduling { get; set; } // default RenderFirst
        public bool IgnorePixelScaling { get; set; }
    }
    public enum RenderSurfaceType { Software, OpenGL }
    public enum WpfDispatcherScheduling
    {
        RenderFirst = 0,  // pump runs at WPF DispatcherPriority.Render, above Input
        InputFair   = 1,  // for continuously-repainting apps: UI work cannot
                          // starve keyboard and pointer input
    }

    DispatcherScheduling is read once when the host initializes (from Run()), so
    set it either through UseWindowsWpf(wpf => wpf.DispatcherScheduling(...)) or
    on the WpfHost after Build(). Set RenderSurfaceType = Software (see THE
    BOOTSTRAP) unless you have verified OpenGL composites on your target.

  LINUX / X11

    ICodeBrixPlatformHostBuilder UseLinuxX11()
    ICodeBrixPlatformHostBuilder UseLinuxX11(Action<X11HostBuilder> action)

    public partial class X11HostBuilder
    {
        public X11HostBuilder RenderingBackend(X11RenderingBackend backend);
            // takes precedence over FeatureConfiguration.Rendering.UseOpenGLOnX11
        public X11HostBuilder RenderFrameRate(int renderFrameRate);   // default 60
        public X11HostBuilder PreloadMediaPlayer(bool preload);
    }
    public enum X11RenderingBackend
    {
        Default,   // try OpenGL, fall back to software
        OpenGL,    // OpenGL via GLX, fall back to software
        OpenGLES,  // OpenGL ES via EGL, fall back to software
        Software,  // software rendering only
    }

    Example:
        .UseLinuxX11(x11 => x11
            .RenderingBackend(X11RenderingBackend.OpenGLES)
            .RenderFrameRate(30))

    Feature flags (set before Build()): FeatureConfiguration.Rendering.
    UseOpenGLOnX11 (bool?; null = OpenGL if available) and PreferGLESOverGLOnX11
    (bool). A Vulkan renderer exists in the repository but is NOT a supported
    configuration: the enum has no Vulkan member and package consumers cannot
    select it.

    The host type is X11ApplicationHost (namespace
    CodeBrix.Platform.WinUI.Runtime.Skia.X11); it implements IDisposable.
    Pointer, keyboard and touch input (XInput2) are supported.

  LINUX / WAYLAND

    ICodeBrixPlatformHostBuilder UseLinuxWayland()
    ICodeBrixPlatformHostBuilder UseLinuxWayland(Action<WaylandHostBuilder> action)

    public partial class WaylandHostBuilder
    {
        public WaylandHostBuilder RenderingBackend(WaylandRenderingBackend backend);
            // takes precedence over the feature flags AND the environment variables
        public WaylandHostBuilder RenderFrameRate(int renderFrameRate);   // default 60
    }
    public enum WaylandRenderingBackend
    {
        Default      = 0,   // Vulkan, falling back to software (same as omitting)
        Vulkan       = 1,   // same Vulkan-else-software selection, stated explicitly
        OpenGLES     = 2,   // OpenGL ES via EGL, falling back to software
        Software     = 3,   // wl_shm software rendering only
        VulkanForced = 11,  // Vulkan with NO fallback: if the Vulkan renderer cannot
                            // be created the app prints a clean two-line "requires
                            // Vulkan rendering" message to stderr and exits with
                            // code 1 (hardware qualification, perf tests)
    }

    Example:
        .UseLinuxWayland(wayland =>
            wayland.RenderingBackend(WaylandRenderingBackend.Vulkan))

    The two GPU paths (Vulkan and OpenGL ES) are peers: each falls back directly
    to software, never to the other. The same choices exist as feature flags
    (set before Build()): FeatureConfiguration.Rendering.UseVulkanOnWayland
    (bool?), .UseOpenGLOnWayland (bool?) and .ForceVulkanOnWayland (bool).
    Environment variables are consulted ONLY when neither the builder backend
    nor the feature flags decided: CODEBRIX_WAYLAND_NO_GPU=1 forces software
    rendering; CODEBRIX_WAYLAND_USE_EGL=1 selects the OpenGL ES path. If both
    are set, NO_GPU wins. Code always beats environment.

    The host type is WaylandApplicationHost (namespace
    CodeBrix.Platform.WinUI.Runtime.Skia.Wayland).

  LINUX / FRAMEBUFFER  (namespace CodeBrix.Platform.UI.Runtime.Skia for the
  builder and option types; CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer
  for FrameBufferHost)

    ICodeBrixPlatformHostBuilder UseLinuxFrameBuffer()
    ICodeBrixPlatformHostBuilder UseLinuxFrameBuffer(Action<FramebufferHostBuilder> action)

    public partial class FramebufferHostBuilder
    {
        // rendering
        public FramebufferHostBuilder UseKMSDRM(string? cardPath = null,
            DRMFourCCColorFormat? gbmSurfaceColorFormat = null,
            DRMConnectorChooserDelegate? connectorChooser = null);
        public FramebufferHostBuilder DisableKMSDRM();
        public FramebufferHostBuilder ScaleUserInterface(UserInterfaceScale scale);

        // mouse cursor
        public FramebufferHostBuilder EnableMouseCursor(float radius, System.Drawing.Color color);
        public FramebufferHostBuilder DisableMouseCursor();

        // orientation
        public FramebufferHostBuilder Orientation(DisplayOrientations orientation,
            bool isPreferredOrientation = false);
        public FramebufferHostBuilder AutoRotationEnabled(params DisplayOrientations[] orientations);
        public FramebufferHostBuilder AutoRotationEnabled(bool enabled);
        public FramebufferHostBuilder UseOrientationSensor();

        // keyboard
        public FramebufferHostBuilder XkbKeymap(XKBKeymapParams keymapParams);
        public FramebufferHostBuilder EnableSoftwareKeyboard(SoftwareKeyboardOptions? options = null);

        // in-application dialogs and clipboard (all OFF unless enabled)
        public FramebufferHostBuilder EnableFileOpenPicker(FilePickerOptions? options = null);
        public FramebufferHostBuilder EnableFileSavePicker(FilePickerOptions? options = null);
        public FramebufferHostBuilder EnableFolderPicker(FolderPickerOptions? options = null);
        public FramebufferHostBuilder EnableSimpleTextClipboard();

        // process policy
        public FramebufferHostBuilder AllowMultipleApplicationInstances();

        public readonly record struct DRMFourCCColorFormat(char C1, char C2, char C3, char C4);
        public readonly record struct DRMConnector(uint connectorType, uint connectorTypeId,
            uint connectorId, string connectorStringRepresentation);
        public delegate int DRMConnectorChooserDelegate(IReadOnlyList<DRMConnector> connector);
        public readonly record struct XKBKeymapParams(string? model = null, string? rules = null,
            string? layout = null, string? variant = null, string? options = null);
    }

    public class FrameBufferHost : SkiaHost, IDisposable
    {
        public float? DisplayScale { get; set; }
            // overrides the framebuffer's default scale; the
            // CODEBRIX_DISPLAY_SCALE_OVERRIDE environment variable overrides it
    }

    public enum UserInterfaceScale { Percent100 = 100, Percent150 = 150, Percent200 = 200 }

    public class FilePickerOptions
    {
        public bool AllowNewFolderCreate { get; set; }
        public string? RestrictToFolder { get; set; }
        public string? RequiredExtension { get; set; }
        public string? StartFolder { get; set; }
        public bool AllowMultipleFileSelect { get; set; } = true;
        public bool ShowHiddenFiles { get; set; }
        public bool ShowHiddenFolders { get; set; }
    }
    public class FolderPickerOptions
    {
        public bool AllowNewFolderCreate { get; set; }
        public string? RestrictToFolder { get; set; }
        public string? StartFolder { get; set; }
        public bool ShowHiddenFolders { get; set; }
    }
    public class SoftwareKeyboardOptions
    {
        public string? Layout { get; set; }               // null = resolved from the system
        public IList<string>? EnabledLayouts { get; set; }
        public bool ShowDismissKey { get; set; } = true;
        public bool AllowLockOn { get; set; }
        public SoftwareKeyHeight KeyHeight { get; set; } = SoftwareKeyHeight.PortraitFullLandscapeFull;
    }
    public enum SoftwareKeyHeight
    {
        PortraitFullLandscapeFull, PortraitHalfLandscapeHalf,
        PortraitFullLandscapeHalf, PortraitHalfLandscapeFull,
    }

    How the pieces behave:

    - RENDERING: DRM/KMS vs /dev/fb0. By default the host tries to create an
      OpenGL ES context through DRM + GBM (scanning /dev/dri/card[0-9]+ unless
      UseKMSDRM gives a cardPath) and, if that fails, logs an error and falls
      back to software rendering onto the framebuffer device (the FRAMEBUFFER
      environment variable names the device; default /dev/fb0). UseKMSDRM()
      requires the DRM path (no fallback); DisableKMSDRM() forces software.
      A launcher can pin the choice with CODEBRIX_FRAMEBUFFER_USE_DRM, which
      overrides the builder - this is how a remote (SSH) run forces software
      /dev/fb0 rendering, because DRM master is never available to a process
      that is not the active console. On GPU-less systems software rendering
      is the normal mode, not a degraded one.

    - PICKERS AND KEYBOARD are opt-in. Without EnableFileOpenPicker /
      EnableFileSavePicker / EnableFolderPicker the standard
      Windows.Storage.Pickers APIs THROW NotSupportedException on this head.
      Enabled, they show a modal in-application dialog drawn on top of all
      app content, inside the application frame. EnableSoftwareKeyboard shows
      an on-screen keyboard automatically when a TextBox or PasswordBox gains
      focus (and honors InputPane.TryShow()/TryHide()); while visible, the
      application's layout height is reduced so the focused field is never
      covered. Without EnableSimpleTextClipboard the head has no clipboard at
      all; with it, a text-only, in-process, last-in-only-out clipboard exists
      (nothing reaches a system clipboard - there is none).

    - INPUT comes from libinput (mouse, touch, keyboard). The mouse cursor is
      drawn by the head: by default it appears after the first MOUSE event and
      never appears for touch-only use; EnableMouseCursor forces a small circle
      of the given radius/color, DisableMouseCursor hides it. Keyboard layouts
      come from libxkbcommon: XkbKeymap sets RMLVO parameters; if unset, the
      system default (XKB_DEFAULT_LAYOUT is consulted) is used.

    - ORIENTATION. Orientation(...) with isPreferredOrientation=false (the
      default) is a ROTATION applied relative to the panel's scanout
      (Landscape = no rotation - which leaves a portrait-native panel
      portrait). With isPreferredOrientation=true it states the orientation the
      application WANTS TO BE and the rotation is worked out from the panel's
      native geometry. AutoRotationEnabled(...) lists the device orientations
      honored at run time (sugar over DisplayInformation.AutoRotationPreferences,
      which remains the source of truth); AutoRotationEnabled(false) locks the
      app. UseOrientationSensor() follows the accelerometer through
      iio-sensor-proxy (apt install iio-sensor-proxy); the launcher can
      override the source with CODEBRIX_FRAMEBUFFER_ORIENTATION_SOURCE
      ("develop" = instructions from the CodeBrix.Develop IDE, "sensor",
      "none"; unset honors the builder).

    - SCALE. ScaleUserInterface draws the UI larger for a dense panel: layout
      happens in logical units (pixels / scale) while drawing keeps every real
      pixel, so nothing is upscaled. Honored under the emulator too.

    - SECOND INSTANCE. By default a second instance of the same application
      refuses to start with an informative error (both would share the one
      framebuffer and each would receive every touch). Call
      AllowMultipleApplicationInstances() only when that is wanted.

    Example (a touch kiosk with pickers and an on-screen keyboard):

        using CodeBrix.Platform.UI.Hosting;
        using CodeBrix.Platform.UI.Runtime.Skia;
        using Windows.Graphics.Display;

        var host = CodeBrixPlatformHostBuilder.Create()
            .App(() => new App())
            .UseLinuxFrameBuffer(fb => fb
                .Orientation(DisplayOrientations.Landscape, isPreferredOrientation: true)
                .AutoRotationEnabled(DisplayOrientations.Landscape, DisplayOrientations.LandscapeFlipped)
                .DisableMouseCursor()
                .ScaleUserInterface(UserInterfaceScale.Percent150)
                .EnableFileOpenPicker(new FilePickerOptions { RestrictToFolder = "/data", AllowMultipleFileSelect = false })
                .EnableFolderPicker()
                .EnableSoftwareKeyboard(new SoftwareKeyboardOptions { KeyHeight = SoftwareKeyHeight.PortraitFullLandscapeHalf })
                .EnableSimpleTextClipboard())
            .Build();
        host.Run();

    THE EMULATED HEAD (CodeBrix.Platform.Runtime.Skia.FrameBuffer.Emulated.
    ApacheLicenseForever) exposes the same UseLinuxFrameBuffer() and
    FramebufferHostBuilder surface and renders offscreen for the CodeBrix.Develop
    emulator; UseOrientationSensor and AllowMultipleApplicationInstances are
    no-ops there. You never reference it - the IDE substitutes it at build time.

  MACOS

    ICodeBrixPlatformHostBuilder UseMacOS()          // no configuration overload

    // host (namespace CodeBrix.Platform.UI.Runtime.Skia.MacOS)
    public class MacSkiaHost : SkiaHost
    {
        public RenderSurfaceType RenderSurfaceType { get; set; }
    }
    public enum RenderSurfaceType { Auto, Metal, Software }

    Feature flag: FeatureConfiguration.Rendering.UseMetalOnMacOS (bool?) - null
    (default) uses Metal if available, otherwise software.

FEATURE CONFIGURATION (static class CodeBrix.Platform.UI.FeatureConfiguration)
-----------------------------------------------------------------------------
Framework-wide switches, set from App's constructor (before
InitializeComponent()) or from Program.Main before Build(). They are static
nested classes; the ones an app author is most likely to need:

  Font
    string DefaultTextFontFamily        default "Segoe UI" (not present on
                                        Linux/macOS - set a bundled font, see FONTS)
    string SymbolsFont                  font for SymbolIcon glyphs; set AFTER
                                        App.InitializeComponent()
    IReadOnlyList<string> FallbackFontFamilies   fonts tried, in order, for a
                                        character the requested font lacks -
                                        BEFORE the host machine's fonts; set
                                        before the first text is measured
    bool RestrictToEmbeddedFonts        confine resolution to fonts the app ships
                                        (the frame-buffer emulator sets it)
    bool IgnoreTextScaleFactor; float? MaximumTextScaleFactor
  Rendering
    bool? UseOpenGLOnWin32; bool? UseOpenGLOnX11; bool PreferGLESOverGLOnX11
    bool? UseVulkanOnWayland; bool ForceVulkanOnWayland; bool? UseOpenGLOnWayland
    bool? UseMetalOnMacOS
    bool EnableVisualSubtreeSkippingOptimization (+ ...CleanFramesThreshold,
         ...VisualCountThreshold)      skip re-rendering unchanged subtrees
  TextBlock
    bool IsMeasureCacheEnabled          default true
  TextBox
    bool HideCaret; bool UseOverlayOnSkia (native TextBox overlay instead of
    the Skia TextBox)
  ScrollViewer
    ScrollViewerUpdatesMode DefaultUpdatesMode   (default AsynchronousIdle;
         Synchronous for backward compatibility)
    TimeSpan? DefaultAutoHideDelay      scrollbar auto-hide (default 4 s;
                                        TimeSpan.MaxValue disables hiding)
    TimeSpan SnapDelay                  default 250 ms
  Popup
    bool EnableLightDismissByDefault; bool PreventLightDismissOnWindowDeactivated
    bool ConstrainByVisibleBounds; bool UseNativePopup
  ToolTip
    bool UseToolTips; int ShowDelay (1000 ms); int ShowDuration (5000 ms)
  Page / Frame
    Page.IsPoolingEnabled              reuse Page instances across navigation
    Frame.UseWinUIBehavior             Skia already uses WinUI behavior
  ListViewBase
    double? DefaultCacheLength (1.0); bool AnimateScrollIntoView (true)
  Control
    bool UseLegacyContentAlignment; bool UseLegacyLazyApplyTemplate;
    bool UseDeferredOnApplyTemplate
  UIElement
    bool UseInvalidateMeasurePath (true); bool UseInvalidateArrangePath (true);
    bool AssignDOMXamlProperties (layout debugging aid)
  Xaml / XamlReader
    Xaml.ForceHotReloadDisabled; XamlReader.FailOnUnknownProperties
  ResourceDictionary
    bool IncludeUnreferencedDictionaries
  Cursors
    bool UseHandForInteraction (true)

Other nested classes exist for narrower cases: ApiInformation, AutomationPeer,
ComboBox, CompositionTarget, ContentPresenter, DataTemplateSelector,
DependencyObject, DependencyProperty, FrameworkElement, FrameworkTemplate,
Image, Interop, Binding, BindingExpression, ProgressRing, NativeListViewBase,
PointerRoutedEventArgs, ManipulationRoutedEventArgs, SelectorItem, Style,
ThemeAnimation, NativeFramePresenter, VisualState, WebView, WebView2,
DatePicker, TimePicker, TimePickerFlyout, CommandBar, AppBarButton, Timeline,
Shape, AndroidSettings (inert on desktop). Read the XML doc comments on the
class in your IDE before flipping one of these - most exist for compatibility
with older behavior and the defaults are right for new apps.

FONTS
-----
Set a bundled font as the default text font in the App constructor. The
"ms-appx:///<PackageId-without-suffix>/Fonts/<file>.ttf" form loads a font
shipped inside a referenced package:

    global::CodeBrix.Platform.UI.FeatureConfiguration.Font.DefaultTextFontFamily =
        "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf";

Fonts your app ships itself are addressed as "ms-appx:///Assets/Fonts/x.ttf"
(a Content item in .Core). To avoid a re-layout when a font arrives late,
preload it (namespace CodeBrix.Platform.UI.Xaml.Media):

    public static partial class FontFamilyHelper
    {
        public static Task<bool> PreloadAsync(FontFamily family, FontWeight weight,
            FontStretch stretch, FontStyle style);
        public static Task<bool> PreloadAsync(string familyName, FontWeight weight,
            FontStretch stretch, FontStyle style);
        public static Task<bool> PreloadAllFontsInManifest(Uri uri);
            // uri of the font (ending with .ttf, without .manifest)
    }

    await FontFamilyHelper.PreloadAsync(
        "ms-appx:///CodeBrix.Platform.Fonts.OpenSans/Fonts/OpenSans.ttf",
        Windows.UI.Text.FontWeights.Normal, Windows.UI.Text.FontStretch.Normal,
        Windows.UI.Text.FontStyle.Normal);

A character no font can supply renders as the font's .notdef glyph (blank or
a box, depending on the font) - the framework never substitutes the host
system's fonts unless FallbackFontFamilies is left empty and the app's fonts
have no glyph.

LOGGING BRIDGE
--------------
The framework logs through Microsoft.Extensions.Logging abstractions. Enable
the bridge by setting "CodeBrix.Platform.Extensions.LogExtensionPoint.
AmbientLoggerFactory" and calling "CodeBrix.Platform.UI.Adapter.Microsoft.
Extensions.Logging.LoggingAdapter.Initialize()" - see APP.XAML.CS PATTERNS for
the exact code. The LoggingAdapter is folded into the core package; there is no
separate adapter package to install. Framework categories start with
"CodeBrix.Platform"; filter them to Warning in normal use.

THE APP-FACING FRAMEWORK AREAS
------------------------------
Everything below is the standard WinUI API; it is listed so you know it is
implemented on the Skia heads and how the framework expects it to be used.

  WINDOW AND APPWINDOW (Microsoft.UI.Xaml.Window / Microsoft.UI.Windowing)

    Window: Title, Content, Activate(), Close(), ExtendsContentIntoTitleBar,
    AppWindow (public when HAS_CODEBRIX_WINUI is defined - which it is),
    DispatcherQueue, events Activated / SizeChanged / VisibilityChanged.

    AppWindow: Title, Size, ClientSize (SizeInt32), Position (PointInt32),
    IsVisible, Presenter, TitleBar, Show(), Show(bool activateWindow),
    Move(PointInt32), Resize(SizeInt32), SetPresenter(AppWindowPresenter),
    SetPresenter(AppWindowPresenterKind), SetIcon(string iconPath),
    static GetFromWindowId(WindowId), events Changed / Closing.

    AppWindow.Size is the window in screen coordinates - it includes whatever
    non-client frame the windowing system draws around it - and Resize takes a
    size of that same kind, so Resize(AppWindow.Size) is a no-op. ClientSize is
    the client area only, and Window.Bounds is that same client area in
    effective pixels, which is what the page is laid out into.

    OverlappedPresenter: IsAlwaysOnTop, IsMaximizable, IsMinimizable, IsModal,
    IsResizable, HasBorder, HasTitleBar, PreferredMinimumWidth/Height,
    PreferredMaximumWidth/Height, State, Maximize(), Minimize(), Restore(),
    SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar).

        MainWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));
        if (MainWindow.AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsResizable = false;
            p.Maximize();
        }

    Several of these are PERMANENT no-ops on the Wayland head (see WHAT THIS
    PACKAGE DOES NOT DO).

  DISPATCHING (Microsoft.UI.Dispatching.DispatcherQueue)

    public bool TryEnqueue(DispatcherQueueHandler callback);
    public bool TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback);
    public bool HasThreadAccess { get; }
    public DispatcherQueueTimer CreateTimer();
    public static DispatcherQueue GetForCurrentThread();

    Every DependencyObject exposes a DispatcherQueue property, and so does
    Window. All UI access must happen on the UI thread - marshal from
    background work like this:

        var queue = this.DispatcherQueue;           // captured on the UI thread
        _ = Task.Run(async () =>
        {
            var result = await LoadAsync();
            queue.TryEnqueue(() => StatusText.Text = result);
        });

  DATA BINDING

    Both {Binding} and {x:Bind} work. {x:Bind} is compiled by the XAML source
    generator (faster, type-checked, defaults to Mode=OneTime - say
    Mode=OneWay/TwoWay explicitly). {Binding} is runtime, reflection-free
    through the generated metadata, and needs a DataContext. View models
    implement System.ComponentModel.INotifyPropertyChanged (or derive from a
    helper of your own). ObservableCollection<T> drives ItemsSource updates.
    Converters implement Microsoft.UI.Xaml.Data.IValueConverter (the Toolkit
    ships the common ones - see TOOLKIT TYPES below).

  CONTENTDIALOG

    public object Title { get; set; }
    public string PrimaryButtonText, SecondaryButtonText, CloseButtonText { get; set; }
    public IAsyncOperation<ContentDialogResult> ShowAsync();
    public IAsyncOperation<ContentDialogResult> ShowAsync(ContentDialogPlacement placement);

        var dialog = new ContentDialog
        {
            Title = "Delete file?",
            Content = "This cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,          // REQUIRED on this framework
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary) { ... }

    You MUST set XamlRoot (the framework does not auto-fill it under
    HAS_CODEBRIX_WINUI). Calling ShowAsync on a dialog that is already showing
    throws InvalidOperationException ("A ContentDialog is already opened."), so
    keep one dialog on screen at a time - as on WinUI - and await the result
    before showing the next.

  FRAME NAVIGATION (Microsoft.UI.Xaml.Controls.Frame)

    public bool Navigate(Type sourcePageType);
    public bool Navigate(Type sourcePageType, object parameter);
    public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride);
    public void GoBack();  public void GoBack(NavigationTransitionInfo transitionInfoOverride);
    public void GoForward();
    public bool CanGoBack { get; }  public bool CanGoForward { get; }
    public IList<PageStackEntry> BackStack { get; }
    public Type SourcePageType { get; set; }

    Pages receive the parameter in OnNavigatedTo(NavigationEventArgs e) via
    e.Parameter. Frame follows WinUI behavior on Skia (a new Page instance per
    navigation unless FeatureConfiguration.Page.IsPoolingEnabled is set).

  RESOURCES AND THEMING

    Application.RequestedTheme (ApplicationTheme.Light / Dark) may be set ONLY
    before initialization completes - i.e. in the App constructor before
    InitializeComponent(); afterwards the setter throws NotSupportedException.
    Per-element: FrameworkElement.RequestedTheme (ElementTheme.Default / Light
    / Dark) can be changed at run time on any element (set it on the Window's
    root element to switch the whole app). RequestedTheme does NOT inherit down
    the visual tree: an element's ActualTheme is its OWN RequestedTheme, or the
    application's theme when that is Default, so setting RequestedTheme halfway
    down a page changes that element alone and nothing under it. Set it on the
    element the XamlRoot holds - that syncs the application theme, and every
    element that has not asked for a theme of its own follows.
    In XAML, {ThemeResource Key} and
    {StaticResource Key} resolve against merged ResourceDictionary entries;
    put app-wide dictionaries in App.xaml:

        <Application.Resources>
            <ResourceDictionary>
                <ResourceDictionary.MergedDictionaries>
                    <ResourceDictionary Source="ms-appx:///Styles/Colors.xaml" />
                </ResourceDictionary.MergedDictionaries>
                <x:Double x:Key="BodyFontSize">14</x:Double>
            </ResourceDictionary>
        </Application.Resources>

    The Fluent control styles (theme dictionaries for Light/Dark/HighContrast)
    are built into the core package; nothing extra is referenced.

  PICKERS (Windows.Storage.Pickers)

    FileOpenPicker:  IList<string> FileTypeFilter;
                     IAsyncOperation<StorageFile?> PickSingleFileAsync();
                     IAsyncOperation<IReadOnlyList<StorageFile>> PickMultipleFilesAsync();
    FileSavePicker:  IAsyncOperation<StorageFile?> PickSaveFileAsync();
    FolderPicker:    IAsyncOperation<StorageFolder?> PickSingleFolderAsync();

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        var file = await picker.PickSingleFileAsync();
        if (file is not null) { using var stream = await file.OpenReadAsync(); ... }

    Every head provides the pickers natively (Win32, X11, Wayland and macOS
    each register their own picker extension); the FrameBuffer head only after
    EnableFileOpenPicker / EnableFileSavePicker / EnableFolderPicker.

  CLIPBOARD (Windows.ApplicationModel.DataTransfer.Clipboard)

    public static void SetContent(DataPackage content);
    public static DataPackageView? GetContent();
    public static void Clear();  public static void Flush();
    public static event EventHandler<object> ContentChanged;

        var package = new DataPackage();
        package.SetText("copied");
        Clipboard.SetContent(package);
        var text = await Clipboard.GetContent()?.GetTextAsync();

    Rich formats (text, HTML, PNG images, file lists, custom formats) work on
    the desktop heads; the FrameBuffer head has only the opt-in text clipboard.

TOOLKIT TYPES FOLDED INTO THE CORE PACKAGE
------------------------------------------
The Toolkit assembly ships inside CodeBrix.Platform.ApacheLicenseForever; no
extra package is needed.

  namespace CodeBrix.Platform.UI.Toolkit
    public sealed partial class ElevatedView : Control
        double Elevation; Color ShadowColor; object ElevatedContent; Brush Background
        // a drop-shadow container:
        <toolkit:ElevatedView Elevation="12" ShadowColor="#66000000" Background="White">
            <TextBlock Text="Card" Margin="16" />
        </toolkit:ElevatedView>
    public sealed partial class TriPaneView : Control
        UIElement SidePane, UpperPane, LowerPane;   SidePanePlacement {Left, Right}
        double SidePanePercent/StackPercent (width pair), UpperPanePercent/LowerPanePercent (height pair)
            star weights, so only the ratio inside a pair matters; 0 means that pane is minimized
        double SidePaneMinLength/StackMinLength/UpperPaneMinLength/LowerPaneMinLength   // pixel floors
        bool CanUserDragSideDivider, CanUserDragStackDivider, IsDragToMinimizeEnabled
        double DividerThickness;  Brush DividerBrush/DividerPointerOverBrush/DividerPressedBrush
        RestoreGripMode {Auto, Always, Never}   // Auto: a grip only on a pane the USER dragged shut
        bool IsSidePaneMinimized/IsUpperPaneMinimized/IsLowerPaneMinimized   (two-way friendly)
        void MinimizeSidePane/UpperPane/LowerPane(), RestoreSidePane/UpperPane/LowerPane(), RestoreAll()
        <Side|Upper|Lower>PaneVerticalScrollBarVisibility, <Side|Upper|Lower>PaneHorizontalScrollMode
            {Disabled, Enabled, AutoOnPortrait}   // AutoOnPortrait: on while taller than wide
        event EventHandler<TriPaneViewDividerDragCompletedEventArgs> DividerDragCompleted
        // a side pane and a stack of two, with two draggable dividers:
        <toolkit:TriPaneView SidePanePercent="25" StackPercent="75"
                             UpperPaneVerticalScrollBarVisibility="Disabled">
            <toolkit:TriPaneView.SidePane><ListView ItemsSource="{x:Bind Items}" /></toolkit:TriPaneView.SidePane>
            <toolkit:TriPaneView.UpperPane>
                <Grid RowDefinitions="Auto,*"><TextBox Grid.Row="1" /></Grid>
            </toolkit:TriPaneView.UpperPane>
            <toolkit:TriPaneView.LowerPane><TextBlock Text="Output" /></toolkit:TriPaneView.LowerPane>
        </toolkit:TriPaneView>
        Every pane is a ScrollViewer, so its content is measured UNBOUNDED along any axis that
        scrolls: star rows fill the pane only where that pane's VerticalScrollBarVisibility is
        Disabled, as the upper pane above. Dividers are pointer-driven; no keyboard resize.
        A minimized pane is given zero width/height, NEVER detached - the same element instance,
        with its text, scroll position and selection, is what comes back on restore.
    public partial class StorageFileHelper
        public static Task<bool> ExistsInPackage(string fileName)   // "Assets/x.png"

  namespace CodeBrix.Platform.UI.Converters   (all IValueConverter)
    BoolToVisibilityConverter        { bool Invert }
    NullToVisibilityConverter        { bool Invert }
    StringToVisibilityConverter      { bool Invert }   (empty/null -> Collapsed)
    CollectionToVisibilityConverter  { bool Invert }   (empty collection -> Collapsed)
    BoolNegationConverter
    BoolToObjectConverter
    StringFormatConverter
        <Page.Resources>
            <conv:BoolToVisibilityConverter x:Key="BoolToVis" />
            <conv:BoolToVisibilityConverter x:Key="InvBoolToVis" Invert="True" />
        </Page.Resources>
        <ProgressRing Visibility="{x:Bind ViewModel.IsBusy, Mode=OneWay,
                                   Converter={StaticResource BoolToVis}}" />

  namespace CodeBrix.Platform.Diagnostics.UI
    public sealed partial class DiagnosticsOverlay : Control
        public static DiagnosticsOverlay Get(XamlRoot root);
        public void Show(bool? isExpanded = null);
        public void Hide();
        // DiagnosticsOverlay.Get(this.XamlRoot).Show();   -> in-app diagnostics panel

  namespace CodeBrix.Platform.UI.Markup
    public sealed class FromJsonExtension : MarkupExtension   // inline JSON -> object in XAML

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

            // (Optional) RequestedTheme = ApplicationTheme.Dark;  // ONLY here, before InitializeComponent
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

COMPILATION CONSTANTS
=====================
Define these in EVERY project that participates in the UI (the .Core library and
every head). The framework uses them for internal conditional compilation, and
some public API (Window.AppWindow, for one) is only public when
HAS_CODEBRIX_WINUI is defined:

    HAS_CODEBRIX
    HAS_CODEBRIX_WINUI

Set them via:

    <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>

The core package's build targets also add these constants to projects that
reference it; declaring them yourself is the reference-app convention and is
harmless. The head packages additionally define the head-specific constants
(HAS_CODEBRIX_SKIA, and per head e.g. HAS_CODEBRIX_SKIA_WIN32, __DESKTOP__) for
you - do not define those by hand.

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

Bind to view models from your .Core project using standard {Binding} / {x:Bind}
- a full example is in COMPLETE EXAMPLES. Controls, panels, styles, visual
states, animations, ListView/GridView with data templates, NavigationView,
TabView, Flyouts, MenuBar, CommandBar, ScrollViewer, SplitView, Slider,
ToggleSwitch, ComboBox, DatePicker/TimePicker, ProgressRing, Image (with the
Svg add-in for SVG), TextBox/PasswordBox/RichEditBox, and the rest of the
Microsoft.UI.Xaml.Controls surface are written exactly as in WinUI
documentation. A member that is present but not backed by an implementation
throws a "not implemented" exception naming it - see WHAT THIS PACKAGE DOES
NOT DO.

ACCESS KEYS (Alt+letter) work on the Skia heads. Put an AccessKey on any
element and Alt plus that letter invokes it: the element's AccessKeyInvoked
event is raised, and when nothing handles that event the element is invoked
through its automation peer, so a Button is clicked and a MenuFlyoutItem runs
its Click/Command with no code behind.

    <MenuBar>
        <MenuBarItem Title="File" AccessKey="F">
            <MenuFlyoutItem Text="Exit" AccessKey="X" Click="OnExit" />
        </MenuBarItem>
        <MenuBarItem Title="Edit" AccessKey="E" />
    </MenuBar>

Alt+F opens the File menu; while it is open a bare "X" invokes Exit; Escape
closes the menu. Tapping Alt on its own enters access-key display mode and
raises AccessKeyDisplayRequested on the elements of the active scope (key tips
- the floating letter badges - are not drawn yet, so nothing appears on screen
unless your own handler draws it); Escape or an unmatched letter leaves display
mode and raises AccessKeyDisplayDismissed. IsAccessKeyScope and
AccessKeyScopeOwner are honoured, and an open popup's content is the active
scope, which is what makes an open menu's items answer their letters.
AccessKeyManager.IsDisplayModeEnabled / EnterDisplayMode(XamlRoot) /
ExitDisplayMode() / IsDisplayModeEnabledChanged are available;
AccessKeyManager.AreKeyTipsEnabled has no visual effect for the same reason.

To open a menu from your own code, call MenuBarItem.Invoke() - it toggles the
item's flyout with the menu bar's own placement and arrow-key wiring.

COMMAND BARS work on the Skia heads, written exactly as in WinUI: CommandBar
with PrimaryCommands and SecondaryCommands, AppBarButton, AppBarToggleButton,
AppBarSeparator and AppBarElementContainer, DefaultLabelPosition
(Bottom / Right / Collapsed), ClosedDisplayMode (Compact / Minimal / Hidden),
IsOpen, IsSticky, IsDynamicOverflowEnabled, a Flyout on an AppBarButton, and a
KeyboardAccelerator whose text a command in the overflow shows for itself.
Pasted WinUI CommandBar XAML needs no prefix changes, because the default XAML
namespace of a page is already Microsoft.UI.Xaml.Controls:

    <CommandBar DefaultLabelPosition="Right">
        <AppBarButton Icon="Save" Label="Save">
            <AppBarButton.KeyboardAccelerators>
                <KeyboardAccelerator Key="S" Modifiers="Control" />
            </AppBarButton.KeyboardAccelerators>
        </AppBarButton>
        <AppBarToggleButton Icon="Highlight" Label="Highlight" />
        <AppBarSeparator />
        <AppBarElementContainer>
            <ComboBox MinWidth="110" VerticalAlignment="Center" />
        </AppBarElementContainer>
        <CommandBar.SecondaryCommands>
            <AppBarButton Icon="Setting" Label="Settings" />
        </CommandBar.SecondaryCommands>
    </CommandBar>

An open bar OVERLAYS the content below it rather than reflowing the page: the
bar keeps its closed display mode's height and reveals the label row by moving
its own clip, which is WinUI's own behaviour.

ICONS ON AN AppBarButton: SymbolIcon, FontIcon, PathIcon, BitmapIcon (a PNG or
any other format the image decoder reads) and IconSourceElement over any
IconSource all work with the core package alone. SVG does NOT: the core package
deliberately has no dependency on an SVG library, so an SVG icon on an
AppBarButton requires the application to reference the Svg add-in
(CodeBrix.Platform.Svg.ApacheLicenseForever), or the CommandBar add-in
(CodeBrix.Platform.CommandBar.ApacheLicenseForever), which brings it and adds
icon types that drop straight into AppBarButton.Icon.

================================================================================

PLATFORM-SPECIFIC NOTES
=======================

WINDOWS:
  - Use the Win32 head for the simplest desktop experience. It renders with
    OpenGL when a driver is present and with software Skia otherwise
    (Win32Host.RenderSurfaceType / FeatureConfiguration.Rendering.UseOpenGLOnWin32).
  - The WPF head needs net10.0-windows, no <UseWPF>, and the software-rendering
    line (see the WPF sections above). Use WpfDispatcherScheduling.InputFair for
    continuously-repainting content.

macOS:
  - The macOS head package contains a small native library (a universal
    binary; runs on Apple Silicon and Intel Macs). Rendering is Metal by
    default with a software fallback (MacSkiaHost.RenderSurfaceType).
  - UseMacOS() has no configuration overload; configure through
    FeatureConfiguration and the host after Build().

LINUX (X11):
  - The broad-compatibility desktop Linux head: runs on X11 desktops and on
    Wayland desktops via XWayland. Activates only when DISPLAY is set.
  - Renders with OpenGL (GLX) by default, OpenGL ES (EGL) on request, and falls
    back to software; choose with X11HostBuilder.RenderingBackend.
  - On some Linux ARM64 systems (e.g. Raspberry Pi), the native SkiaSharp library
    may fail to auto-load FreeType, throwing an "undefined symbol" error at
    startup. If you hit this, preload FreeType when launching, e.g.:
        LD_PRELOAD=/usr/lib/aarch64-linux-gnu/libfreetype.so.6 dotnet run ...
    This is a SkiaSharp native-asset packaging issue, not a CodeBrix.Platform
    issue, and is expected to resolve in newer SkiaSharp native packages.
  - On Raspberry Pi OS (labwc) the window may come up borderless; a labwc
    windowRule with serverDecoration="yes" in ~/.config/labwc/rc.xml fixes it.
  - Drag & drop: accepting drops from other applications works; initiating a
    drag is not implemented. IME (composed CJK / dead-key) text input is not
    implemented.

LINUX (native Wayland):
  - A pure Wayland client: it speaks the Wayland protocol directly and never
    uses X11/XWayland. It REQUIRES a Wayland compositor; without one it fails
    fast at startup with a clean "This application requires a Wayland
    compositor." message and exit code 1 (use the X11 head for X11/XWayland
    environments).
  - Permissively licensed (Apache/MIT) top to bottom - no LGPL/GPL components.
  - Window decorations: on KDE/wlroots-family compositors the server draws them;
    on GNOME/Cinnamon they are drawn client-side via the system's libdecor
    library. For a native-looking title bar on Debian/Ubuntu-family desktops the
    libdecor GTK plugin should be present (packages "libdecor-0-0" +
    "libdecor-0-plugin-1-gtk"; preinstalled on most GNOME desktops).
  - Rendering defaults to Vulkan (VK_KHR_wayland_surface), falling back to
    wl_shm software rendering when Vulkan is unavailable; see PER-HEAD
    CONFIGURATION for the backend selector, feature flags and environment
    variables.
  - Working, at parity with the X11 head: flyout-based controls (ComboBox
    dropdowns, MenuFlyout, ToolTip, dialogs), rich clipboard (text, HTML, PNG
    images, file lists, custom formats - copy AND paste), fractional
    (non-integer) display scaling, custom title bars
    (ExtendsContentIntoTitleBar), and window activation (xdg-activation;
    compositor focus policy applies). ACCEPTING drag-and-drop from other
    applications is implemented but may not work on some compositors - see the
    "Drag & drop MAY NOT WORK" note under WHAT THIS PACKAGE DOES NOT DO.
  - Not yet implemented in this head (deferred): touch input, native-view
    hosting in a ContentPresenter (needs subsurfaces), and IME text input.
  - The window/taskbar icon comes from a .desktop file whose name matches the
    app id (the appxmanifest package name, falling back to the entry assembly
    name), placed in ~/.local/share/applications or /usr/share/applications
    with an Icon= entry.
  - Window self-activation (Window.Activate()) rides xdg-activation-v1 and is
    subject to compositor focus-stealing policy: without a recent user
    interaction the compositor may only flag the window as demanding attention
    rather than focusing it.
  - For the protocol-inherent gaps that will never change (window positioning,
    forced resize, always-on-top, and friends) see WHAT THIS PACKAGE DOES NOT DO.

LINUX (framebuffer):
  - Use the framebuffer head for embedded/kiosk devices with no X11/desktop
    environment. Same app code; different head package and
    ".UseLinuxFrameBuffer()". The application owns the whole panel: there is
    no window manager, no window chrome, and one surface.
  - Rendering: DRM/KMS + GBM (OpenGL ES) when the process is the active console
    and a GPU is present, otherwise software onto the FRAMEBUFFER device
    (default /dev/fb0). Over SSH or on a GPU-less board, expect (and prefer)
    software rendering; a launcher pins it with CODEBRIX_FRAMEBUFFER_USE_DRM.
    The process needs read/write access to the framebuffer device, the DRM
    card and the input devices - typically the "video" and "input" groups on a
    Debian-family system - and a getty must not be fighting for the console.
  - Input: libinput (touch, mouse, keyboard); libxkbcommon keymaps. Touch
    works with no configuration; a touch-only device wants DisableMouseCursor().
  - Pickers, on-screen keyboard and clipboard are opt-in builder calls (see
    PER-HEAD CONFIGURATION). Without them the picker APIs throw
    NotSupportedException and clipboard use logs "not implemented".
  - Debug it from CodeBrix.Develop: the IDE runs the app against the Emulated
    head package in its emulator window; nothing in your project changes.

================================================================================

THE CANONICAL REFERENCE APPLICATION
===================================
"JustBetweenUs" is THE reference application that demonstrates the entire
structure described in this document. When in doubt, read it.

    Repository:  https://github.com/ellisnet/JustBetweenUs
    Branch:      main
    Folder:      CodeBrixPlatform/

Project map (under CodeBrixPlatform/; the repository's own folder names are
authoritative, and they follow the PROJECT & HEAD NAMING rules above):

    JustBetweenUs.Core/              The .Core library (framework + add-in package
                                     references, view models, services).
    JustBetweenUs.UI/                The .UI shared project (.shproj + .projitems):
                                     App.xaml, App.xaml.cs, Views/MainPage.xaml(.cs).
    JustBetweenUs.Win32Skia/         Windows (Win32) head   -> .UseWindowsWin32()
    JustBetweenUs.WinWpfSkia/        Windows (WPF) head     -> .UseWindowsWpf() + software render
    JustBetweenUs.LinuxX11/          Linux (X11) head       -> .UseLinuxX11()
    JustBetweenUs.LinuxWayland/      Linux (native Wayland) -> .UseLinuxWayland()
    JustBetweenUs.LinuxFrameBuffer/  Linux framebuffer      -> .UseLinuxFrameBuffer()
    JustBetweenUs.MacOS/             macOS head             -> .UseMacOS()

The native heads live beside that folder (JustBetweenUs.WinUI/, JustBetweenUs.Wpf/,
Mobile/) and are NOT CodeBrix.Platform heads; ignore them for this framework.

To read a file directly, fetch its raw content, e.g.:

    https://raw.githubusercontent.com/ellisnet/JustBetweenUs/main/CodeBrixPlatform/JustBetweenUs.Win32Skia/Program.cs

Study these files to scaffold your own app:
  - JustBetweenUs.Core/JustBetweenUs.Core.csproj  (which packages go in .Core)
  - JustBetweenUs.UI/JustBetweenUs.UI.projitems   (shared-project file layout)
  - JustBetweenUs.UI/App.xaml.cs                  (font + logging + launch pattern)
  - JustBetweenUs.<Head>/JustBetweenUs.<Head>.csproj  (per-head package + TFM)
  - JustBetweenUs.<Head>/Program.cs               (per-head bootstrap)

================================================================================

COMPLETE EXAMPLES
=================
The project files (csproj, projitems, shproj), App.xaml, App.xaml.cs and every
head's Program.cs are given verbatim in the sections above. What follows is a
complete view + view model that exercises binding, dispatching, a dialog, a
picker and the clipboard, and runs unchanged on every head.

MyApp.Core/ViewModels/MainViewModel.cs:

    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    namespace MyApp.ViewModels;

    public class MainViewModel : INotifyPropertyChanged
    {
        private string _status = "Ready";
        private bool _isBusy;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> Files { get; } = new();

        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
        }

        void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

MyApp.UI/Views/MainPage.xaml:

    <Page
        x:Class="MyApp.Views.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:conv="using:CodeBrix.Platform.UI.Converters"
        xmlns:toolkit="using:CodeBrix.Platform.UI.Toolkit">
        <Page.Resources>
            <conv:BoolToVisibilityConverter x:Key="BoolToVis" />
        </Page.Resources>
        <Grid Padding="16" RowSpacing="8">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Content="Open file..." Click="OnOpenFile" />
                <Button Content="Copy status" Click="OnCopyStatus" />
                <Button Content="Confirm" Click="OnConfirm" />
                <Button Content="Slow work" Click="OnSlowWork" />
                <ToggleSwitch Header="Dark" Toggled="OnThemeToggled" />
                <ProgressRing IsActive="True"
                              Visibility="{x:Bind ViewModel.IsBusy, Mode=OneWay, Converter={StaticResource BoolToVis}}" />
            </StackPanel>

            <toolkit:ElevatedView Grid.Row="1" Elevation="8" Background="{ThemeResource LayerFillColorDefaultBrush}">
                <ListView ItemsSource="{x:Bind ViewModel.Files}" />
            </toolkit:ElevatedView>

            <TextBlock Grid.Row="2" Text="{x:Bind ViewModel.Status, Mode=OneWay}" />
        </Grid>
    </Page>

MyApp.UI/Views/MainPage.xaml.cs:

    using System;
    using System.Threading.Tasks;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using MyApp.ViewModels;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Storage.Pickers;

    namespace MyApp.Views;

    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new();

        public MainPage() => InitializeComponent();

        async void OnOpenFile(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            var file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                ViewModel.Files.Add(file.Path);
                ViewModel.Status = $"Opened {file.Name}";
            }
        }

        void OnCopyStatus(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(ViewModel.Status);
            Clipboard.SetContent(package);
        }

        async void OnConfirm(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Clear the list?",
                Content = $"{ViewModel.Files.Count} entries will be removed.",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Keep",
                XamlRoot = XamlRoot,
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.Files.Clear();
                ViewModel.Status = "Cleared";
            }
        }

        void OnSlowWork(object sender, RoutedEventArgs e)
        {
            ViewModel.IsBusy = true;
            var queue = DispatcherQueue;                    // UI thread's queue
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);                     // background work
                queue.TryEnqueue(() =>
                {
                    ViewModel.Status = $"Finished at {DateTime.Now:T}";
                    ViewModel.IsBusy = false;
                });
            });
        }

        void OnThemeToggled(object sender, RoutedEventArgs e)
        {
            // per-element theme, applied to the page's whole subtree at run time
            RequestedTheme = ((ToggleSwitch)sender).IsOn ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

Head-specific configuration examples (Program.cs) are in PER-HEAD CONFIGURATION.

================================================================================

MINIMUM VIABLE PROJECT
======================
The smallest runnable app: one .Core library, one .UI shared project, one head
(here Linux X11). Eight files:

    MyApp.Core/MyApp.Core.csproj          THE .Core PROJECT, keeping only the
                                          CodeBrix.Platform.ApacheLicenseForever
                                          reference
    MyApp.UI/MyApp.UI.projitems           THE .UI SHARED PROJECT (A)
    MyApp.UI/MyApp.UI.shproj              THE .UI SHARED PROJECT (B)
    MyApp.UI/App.xaml                     THE .UI SHARED PROJECT (C)
    MyApp.UI/App.xaml.cs                  APP.XAML.CS PATTERNS (drop the font
                                          line if you ship no font package)
    MyApp.UI/Views/MainPage.xaml(.cs)     WRITING XAML AND VIEWS
    MyApp.LinuxX11/MyApp.LinuxX11.csproj  THE PLATFORM HEAD PROJECTS with
                                          CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever
    MyApp.LinuxX11/Program.cs             THE BOOTSTRAP with .UseLinuxX11()

    dotnet run --project MyApp.LinuxX11/MyApp.LinuxX11.csproj

The smallest .Core csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <RootNamespace>MyApp</RootNamespace>
        <DefineConstants>$(DefineConstants);HAS_CODEBRIX;HAS_CODEBRIX_WINUI</DefineConstants>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="Microsoft.Extensions.Logging.Console" />
        <PackageReference Include="CodeBrix.Platform.ApacheLicenseForever" />
      </ItemGroup>
    </Project>

Adding a second platform = one more head folder with the other head package and
the other .Use...() call. Nothing else changes.

================================================================================

PERFORMANCE TIPS
================
  - PICK THE RENDER BACKEND DELIBERATELY. Defaults per head: Win32 OpenGL
    (else software); WPF OpenGL - but set Software (airspace); X11 OpenGL/GLX
    (else software; OpenGL ES via PreferGLESOverGLOnX11 or
    X11RenderingBackend.OpenGLES); Wayland Vulkan (else software; OpenGL ES is a
    peer path); FrameBuffer DRM/GBM OpenGL ES (else software on /dev/fb0);
    macOS Metal (else software). GPU paths win for large windows and animation;
    software Skia is perfectly adequate for form-style UIs and is the ONLY
    option over SSH, in VMs without 3D, and on GPU-less boards. Use
    WaylandRenderingBackend.VulkanForced when you need proof that the GPU path
    is really in use.
  - THROTTLE THE FRAME RATE where it helps: X11HostBuilder.RenderFrameRate and
    WaylandHostBuilder.RenderFrameRate (default 60). A kiosk dashboard that
    updates once a second, or a battery-powered device, does not need 60 fps.
  - WPF HEAD: for continuously-repainting content (games, live plots) set
    DispatcherScheduling = WpfDispatcherScheduling.InputFair so rendering
    cannot starve keyboard and pointer input.
  - SKXamlCanvas-HEAVY APPS (Graphics2DSK): try UseDirectSkiaCanvasMode() -
    one fewer full-frame copy per paint. Experimental; measure.
  - LET THE FRAMEWORK SKIP CLEAN SUBTREES:
    FeatureConfiguration.Rendering.EnableVisualSubtreeSkippingOptimization (and
    its two thresholds) avoids re-rendering subtrees that have not changed.
  - TEXT: keep FeatureConfiguration.TextBlock.IsMeasureCacheEnabled at its
    default (true). Preload fonts with FontFamilyHelper.PreloadAsync so the
    first screen does not re-layout when the font arrives.
  - LISTS: ListView/GridView virtualize; FeatureConfiguration.ListViewBase.
    DefaultCacheLength (1.0) trades memory for scroll smoothness. Use
    ObservableCollection<T> rather than resetting ItemsSource.
  - NAVIGATION: FeatureConfiguration.Page.IsPoolingEnabled reuses Page
    instances across Frame navigations in navigation-heavy apps.
  - BINDING: prefer {x:Bind} (compiled) over {Binding} in hot templates.
  - LOGGING: the DEBUG-only logging block in App.InitializeLogging is DEBUG-only
    on purpose; console logging at Information level in Release costs frames.
    Keep "CodeBrix.Platform" filtered to Warning.
  - FRAMEBUFFER: ScaleUserInterface keeps every real pixel (no upscaling), so
    it costs no fill rate; software rendering at very high resolutions does -
    keep the panel's native resolution in mind.

================================================================================

COMMON PITFALLS TO AVOID
========================
 1. DO NOT confuse package ids with namespaces. Package ids carry a license
    suffix (".ApacheLicenseForever" / ".MitLicenseForever" /
    ".LgplLicenseForever"); namespaces do not (they are "CodeBrix.Platform.*",
    "Microsoft.UI.Xaml.*").

 2. DO NOT reference a platform head package in the .Core library, and DO NOT
    put more than one head package in a single head project. One head project ==
    one head package. DO NOT put add-in packages in a head project either -
    they belong in .Core, referenced once.

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
    not a standalone assembly. Do not move the Views into .Core.

 8. DO NOT target a framework below .NET 10. CodeBrix.Platform requires net10.0.

 9. DO NOT call CodeBrixPlatformHostBuilder before App.InitializeLogging(). The
    reference app calls InitializeLogging() first in every head's Main.

10. DO NOT expect the Wayland head to run in an X11-only session - it requires a
    Wayland compositor and fails fast (by design) when none is present. For an
    app that must run everywhere on desktop Linux, ship the X11 head (alone, or
    alongside a Wayland head).

11. DO NOT name the Win32 head ".Windows" (CS0234 on that head only - see
    PROJECT & HEAD NAMING).

12. DO NOT show a ContentDialog without setting XamlRoot, and DO NOT keep two
    on screen at once - re-showing a dialog that is already showing throws
    InvalidOperationException ("A ContentDialog is already opened.").

13. DO NOT set Application.RequestedTheme after InitializeComponent() - it
    throws NotSupportedException. Switch themes at run time with
    FrameworkElement.RequestedTheme on the root element instead.

14. DO NOT touch UI objects from a background thread. Capture DispatcherQueue
    on the UI thread and TryEnqueue back; check HasThreadAccess when unsure.

15. DO NOT expect pickers, an on-screen keyboard or a clipboard on the
    FrameBuffer head unless you enabled them on the FramebufferHostBuilder;
    the picker APIs throw NotSupportedException otherwise.

16. DO NOT reference CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever or the
    FrameBuffer.Emulated package directly. The first is transitive; the second
    is the IDE's business.

17. DO NOT rely on window positioning, forced resize or always-on-top on the
    Wayland head - they are protocol-level no-ops (see below). Design the UI
    so it does not need them, or accept that they only work on X11/Windows/macOS.

18. DO NOT set FeatureConfiguration.Font.DefaultTextFontFamily after the first
    text has been measured; set it in the App constructor. A bundled font must
    be referenced from .Core so every head ships it.

19. DO NOT expect a "not implemented" member to work by trying harder: the
    message names the member; find an implemented alternative (NOT-IMPLEMENTED.md
    on GitHub explains the policy).

20. DO NOT expect a modifier key the desktop reserved for itself to reach your
    application: Cinnamon takes Alt for its window drag and its window menu, so
    bind Shift and Control for a modifier-aware click. The key state
    (InputKeyboardSource.GetKeyStateForCurrentThread, CoreWindow.GetKeyState)
    follows the modifier mask carried by every routed key and pointer event and
    is cleared when the window loses focus, so a press or a release the desktop
    swallowed cannot leave a modifier reading "held".

================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No mobile (iOS/Android) and no WebAssembly/browser targets. Ever.
  - No Vulkan on X11 for consumers: the renderer is in the repository behind an
    internal-only flag with no public API; X11RenderingBackend has no Vulkan
    member.
  - The optional capabilities (2D/3D canvases, Lottie, SVG, media/audio
    playback, web view, editor, terminal, charts, flex layout, settings store)
    are NOT in these nine packages; they are separate add-in packages (see the
    catalogue in INSTALLATION).
  - Every WinUI/UWP type and member exists so code and XAML compile unchanged,
    but a subset are not backed by an implementation and throw a "not
    implemented" exception that names the member. See
    https://github.com/ellisnet/CodeBrix.Platform/blob/main/NOT-IMPLEMENTED.md
  - Not implemented on any Skia head: IME (composed CJK / dead-key) text input;
    initiating drag-and-drop (accepting drops works on X11/Windows/macOS, and on
    Wayland subject to the compositor); access-key KEY TIPS (access keys
    themselves work - see ACCESS KEYS under WRITING XAML AND VIEWS - but the
    floating letter badges are not drawn, and multi-character access keys typed
    as a sequence are not matched).
  - Not implemented on the Wayland head (deferred): touch input, native-view
    hosting in a ContentPresenter (the content is ignored with a one-time
    warning; the WebView and MediaPlayer add-ins are windowing-agnostic and
    unaffected).
  - The FrameBuffer head has no system clipboard (only the opt-in in-process
    text clipboard), no window management, and no pickers/keyboard unless
    enabled.

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

  Related:
  - Drag & drop MAY NOT WORK on the Wayland head, depending on the compositor.
    The head's drop-target support (wl_data_device) is implemented and behaves
    correctly per protocol, but compositors with experimental Wayland sessions
    can deliver unusable drag events (observed on Cinnamon/Muffin: drags from
    XWayland sources arrive with garbage enter coordinates, so hit-testing
    never finds a drop target and the drop silently does nothing). This is a
    compositor-side bug; drag & drop works normally on the X11 head.
  - The xdg-toplevel-icon-v1 protocol is pinned in the bindings for a future
    in-process window-icon path, but common desktops do not support it yet;
    use the .desktop-file route described under PLATFORM-SPECIFIC NOTES.

================================================================================

WORKING EXAMPLES ON GITHUB
==========================
Reference application (the canonical structure, six heads):
    https://github.com/ellisnet/JustBetweenUs/tree/main/CodeBrixPlatform

Samples in this repository (each is a complete .Core + .UI + heads solution;
they consume the framework from source, so their csproj files use
ProjectReference where yours use PackageReference - copy the structure, not
the reference lines):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/JustBetweenUs
        in-repo copy of the reference app (six heads + Tests)
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/EmulateFrameBufferDemo
        FrameBuffer head configured with Orientation(..., isPreferredOrientation)
        and AutoRotationEnabled; runs in the CodeBrix.Develop emulator
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/FileFolderDialogDemo
        file/folder pickers on every head (six heads)
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/ParityDemo
        X11 vs native-Wayland behavior side by side (two Linux heads)
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/TriPaneViewDemo
        the Toolkit's TriPaneView driven from a control strip: proportions, minimum
        lengths, minimize and restore, the grip modes and per-pane scrolling (six heads)
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/AdvancedTextEditDemo
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/AudioPlayerDemo
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/FlexPanelDemo
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/MediaPlayerDemo
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/PlotterViewDemo
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/TerminalViewDemo
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/VideoPlayerDemo
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/WebViewDemo
        one demo per add-in (six heads each); their AGENT-READMEs describe them

Framework tests that double as API examples:
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/Platform.UI.RuntimeTests
        runtime tests for controls, binding, navigation and windowing
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/Platform.UI.Toolkit.Tests
        Toolkit converters and helpers

================================================================================

QUICK REFERENCE CARD
====================

Architecture:     .Core (library) + .UI (shared project) + one Exe head per platform
Target:           net10.0  (WPF head: net10.0-windows)
Defines (all):    HAS_CODEBRIX;HAS_CODEBRIX_WINUI
UI API:           WinUI / Microsoft.UI.Xaml.*
Host builder:     CodeBrixPlatformHostBuilder.Create() (namespace CodeBrix.Platform.UI.Hosting)
License:          Apache-2.0 for every package in this file

THIS FILE'S PACKAGES:
    Core framework (in .Core):  CodeBrix.Platform.ApacheLicenseForever
    Base runtime (transitive):  CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever
    Head packages (exactly one per head) and bootstrap call:
    Windows/Win32  ->  CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever       .UseWindowsWin32([Action<Win32HostBuilder>])
    Windows/WPF    ->  CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever         .UseWindowsWpf([Action<IWindowsSkiaHostBuilder>])  (+ Software render)
    Linux/X11      ->  CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever         .UseLinuxX11([Action<X11HostBuilder>])
    Linux/Wayland  ->  CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever     .UseLinuxWayland([Action<WaylandHostBuilder>])  (needs a compositor)
    Linux/FB       ->  CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever .UseLinuxFrameBuffer([Action<FramebufferHostBuilder>])
    macOS          ->  CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever       .UseMacOS()
    IDE-only       ->  CodeBrix.Platform.Runtime.Skia.FrameBuffer.Emulated.ApacheLicenseForever  (never reference)

ADD-INS (all in .Core; each has its own AGENT-README):
    CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever      src/AddIns/Platform.WinUI.Graphics2DSK/
    CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever      src/AddIns/Platform.WinUI.Graphics3DGL/
    CodeBrix.Platform.Lottie.ApacheLicenseForever            src/AddIns/Platform.UI.Lottie/          (+ SkiaSharp.Skottie)
    CodeBrix.Platform.Svg.ApacheLicenseForever               src/AddIns/Platform.UI.Svg/             (+ CodeBrix.SkiaSvg.MitLicenseForever)
    CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever      src/AddIns/CodeBrix.Platform.SkiaSharp.Views/
    CodeBrix.Platform.MediaPlayer.LgplLicenseForever         src/AddIns/Platform.UI.MediaPlayer.Skia/
    CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever  src/AddIns/Platform.UI.AdvancedTextEdit/
    CodeBrix.Platform.AppSettings.ApacheLicenseForever       src/AddIns/Platform.AppSettings/
    CodeBrix.Platform.AudioPlayer.ApacheLicenseForever       src/AddIns/Platform.UI.AudioPlayer.Skia/
    CodeBrix.Platform.FlexPanel.ApacheLicenseForever         src/AddIns/Platform.UI.FlexPanel/
    CodeBrix.Platform.PlotterView.ApacheLicenseForever       src/AddIns/Platform.UI.PlotterView/
    CodeBrix.Platform.TerminalView.ApacheLicenseForever      src/AddIns/Platform.UI.TerminalView/
    CodeBrix.Platform.TextLayout.ApacheLicenseForever        src/AddIns/Platform.UI.TextLayout/
    CodeBrix.Platform.VideoPlayer.ApacheLicenseForever       src/AddIns/Platform.UI.VideoPlayer.Skia/  (+ CodeBrix.VideoPlayback)
    CodeBrix.Platform.WebView.ApacheLicenseForever           src/AddIns/Platform.UI.WebView.Skia/

TOOLKITS FOR MICROSOFT'S OWN FRAMEWORKS (not for CodeBrix.Platform apps):
    CodeBrix.Platform.WinUI.ApacheLicenseForever, .WinUI.Skia..., .WinUI.Lottie...   src-platforms/Platform.WinUI/
    CodeBrix.Platform.WPF.ApacheLicenseForever                                       src-platforms/Platform.WPF/
    CodeBrix.Platform.Mobile.ApacheLicenseForever                                    src-platforms/Platform.Mobile/

Bootstrap:
    var host = CodeBrixPlatformHostBuilder.Create().App(() => new App()).UseXxx().Build();
    host.Run();   // or: await host.RunAsync();

Per-head knobs:
    Win32:   Win32HostBuilder.PreloadMediaPlayer(bool); Win32Host.RenderSurfaceType? {Software, OpenGL}
    WPF:     .WpfApplication(Func<Application>), .DispatcherScheduling(RenderFirst|InputFair);
             WpfHost.RenderSurfaceType? {Software, OpenGL}, IgnorePixelScaling
    X11:     .RenderingBackend(Default|OpenGL|OpenGLES|Software), .RenderFrameRate(int), .PreloadMediaPlayer(bool)
    Wayland: .RenderingBackend(Default|Vulkan|OpenGLES|Software|VulkanForced), .RenderFrameRate(int)
             env: CODEBRIX_WAYLAND_NO_GPU=1, CODEBRIX_WAYLAND_USE_EGL=1 (code beats env)
    FB:      .UseKMSDRM(...)/.DisableKMSDRM(), .ScaleUserInterface(Percent100|150|200),
             .EnableMouseCursor(radius, color)/.DisableMouseCursor(),
             .Orientation(DisplayOrientations, isPreferredOrientation), .AutoRotationEnabled(...),
             .UseOrientationSensor(), .XkbKeymap(XKBKeymapParams),
             .EnableFileOpenPicker/.EnableFileSavePicker(FilePickerOptions?), .EnableFolderPicker(FolderPickerOptions?),
             .EnableSoftwareKeyboard(SoftwareKeyboardOptions?), .EnableSimpleTextClipboard(),
             .AllowMultipleApplicationInstances(); FrameBufferHost.DisplayScale
             env: FRAMEBUFFER, CODEBRIX_FRAMEBUFFER_USE_DRM, CODEBRIX_DISPLAY_SCALE_OVERRIDE,
                  CODEBRIX_FRAMEBUFFER_ORIENTATION_SOURCE
    macOS:   MacSkiaHost.RenderSurfaceType {Auto, Metal, Software}
    All:     .AfterInit(Action), .UseDirectSkiaCanvasMode() (experimental)

Framework-wide:   CodeBrix.Platform.UI.FeatureConfiguration.{Font, Rendering, TextBox, ScrollViewer, Popup, ...}
Fonts:            FeatureConfiguration.Font.DefaultTextFontFamily = "ms-appx:///<Pkg>/Fonts/<file>.ttf";
                  CodeBrix.Platform.UI.Xaml.Media.FontFamilyHelper.PreloadAsync(...)
Logging:          LogExtensionPoint.AmbientLoggerFactory = factory; LoggingAdapter.Initialize();
Toolkit:          ElevatedView, *ToVisibilityConverter (Invert), StorageFileHelper, DiagnosticsOverlay, FromJsonExtension
                  TriPaneView (side pane + upper/lower stack, draggable dividers, minimize/restore)
UI thread:        DispatcherQueue.TryEnqueue(...)  (Microsoft.UI.Dispatching)
Dialog:           new ContentDialog { XamlRoot = XamlRoot, ... }.ShowAsync()

Reference app:    https://github.com/ellisnet/JustBetweenUs  (main, CodeBrixPlatform/)

================================================================================
