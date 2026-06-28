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
  - Supported desktop targets: Windows (Win32 or WPF host), Linux (X11 or
    framebuffer), macOS (Apple Silicon and Intel).
  - Out of scope for this fork: mobile (iOS/Android), WebAssembly/browser.

Source repository:        https://github.com/ellisnet/CodeBrix.Platform
Canonical reference app:  https://github.com/ellisnet/JustBetweenUs
Licenses:                 Apache-2.0 (most packages), MIT (the SkiaSharp.Views
                          package). Every package id carries an explicit license
                          suffix — see "THE NUGET PACKAGES" below.

IMPORTANT: Throughout this guide, NuGet package NAMES carry a license suffix
(".ApacheLicenseForever" or ".MitLicenseForever") while NAMESPACES do NOT. For
example, the package "CodeBrix.Platform.ApacheLicenseForever" provides the
namespaces "CodeBrix.Platform.UI.*", "Microsoft.UI.Xaml.*", and so on. Do not
confuse package ids with namespaces.

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
     - One per platform you ship: Windows, WpfSkia, Linux, LinuxFrameBuffer, MacOs.
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
  CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever Linux framebuffer (no X11; kiosk/embedded)
  CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever      macOS (Apple Silicon + Intel)

  NOTE: A base package, "CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever",
  and the framework aggregate flow in TRANSITIVELY beneath each head package.
  You never reference the base runtime package directly — referencing one head
  package is all a head project needs.

  NOTE: On Windows you have two choices. The Win32 head is the simplest and most
  common. The WPF head is for hosting CodeBrix.Platform content inside a WPF
  desktop app context (see the WPF-specific section below).

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
    .Core project, plus the .UI shared project. It adds nothing else UI-related.

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

STEP 3 — Create one head project per target. For a Windows (Win32) head:

    dotnet new console -n MyApp.Windows --framework net10.0
    cd MyApp.Windows
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

    dotnet build MyApp.Windows/MyApp.Windows.csproj
    dotnet run --project MyApp.Windows/MyApp.Windows.csproj

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

    Linux (X11):          CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever
    Linux (framebuffer):  CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever
    macOS:                CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever

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
  Linux (framebuffer)     ...Runtime.Skia.FrameBuffer...        .UseLinuxFrameBuffer()
  macOS                   ...Runtime.Skia.MacOS...              .UseMacOS()

  These ".Use…()" methods are all extension methods in the
  "CodeBrix.Platform.UI.Hosting" namespace, surfaced by the corresponding head
  package. A head sees only the one ".Use…()" method that matches its package.

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
  - Standard desktop Linux. Use the X11 head.
  - On some Linux ARM64 systems (e.g. Raspberry Pi), the native SkiaSharp library
    may fail to auto-load FreeType, throwing an "undefined symbol" error at
    startup. If you hit this, preload FreeType when launching, e.g.:
        LD_PRELOAD=/usr/lib/aarch64-linux-gnu/libfreetype.so.6 dotnet run ...
    This is a SkiaSharp native-asset packaging issue, not a CodeBrix.Platform
    issue, and is expected to resolve in newer SkiaSharp native packages.

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
    JustBetweenUs.Linux/         Linux (X11) head      -> .UseLinuxX11()
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
1.0.179.226 below:

    dotnet pack src/Platform.UI.Runtime.Skia.MacOS/Platform.UI.Runtime.Skia.MacOS.csproj \
      -c Release \
      -p:PackageVersion=1.0.179.226 \
      --output nugets/Release/1.0.179.226

-p:PackageVersion (NOT -p:Version) sets only the NuGet package version while
still flowing to the ProjectReference dependency versions. This produces:

    nugets/Release/1.0.179.226/CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever.1.0.179.226.nupkg

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
    Lottie       ->   CodeBrix.Platform.Lottie.ApacheLicenseForever (+ SkiaSharp.Skottie)
    Svg          ->   CodeBrix.Platform.Svg.ApacheLicenseForever (+ CodeBrix.SkiaSvg.MitLicenseForever)
    Skia views   ->   CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever

Head packages (exactly one per head) and bootstrap call:
    Windows/Win32  ->  CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever       .UseWindowsWin32()
    Windows/WPF    ->  CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever         .UseWindowsWpf()  (+ Software render)
    Linux/X11      ->  CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever         .UseLinuxX11()
    Linux/FB       ->  CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever .UseLinuxFrameBuffer()
    macOS          ->  CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever       .UseMacOS()

Bootstrap:
    var host = CodeBrixPlatformHostBuilder.Create().App(() => new App()).UseXxx().Build();
    host.Run();   // or: await host.RunAsync();

Reference app:    https://github.com/ellisnet/JustBetweenUs  (main, CodeBrixPlatform/)

================================================================================
