================================================================================
MAINTAINER-README: CodeBrix.Platform
Notes for people and agents MAINTAINING this repository - not for package consumers
================================================================================

PURPOSE AND SCOPE
=================
This repository produces the CodeBrix.Platform cross-platform UI framework
(WinUI XAML API surface rendered with Skia on Windows, Linux and macOS), its
platform head packages, its optional add-in packages, and three helper
toolkits for Microsoft's own UI frameworks. Consumer documentation lives in
per-package AGENT-README.txt files; README-INDEX.txt maps them.

Package ids and the AGENT-README that covers each:

  AGENT-README.txt (repo root) - core, base runtime and the seven heads:
    CodeBrix.Platform.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.Win32.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.Wpf.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.X11.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.Wayland.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.FrameBuffer.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.FrameBuffer.Emulated.ApacheLicenseForever
    CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever

  Add-ins (one AGENT-README.txt in each source folder):
    CodeBrix.Platform.Graphics2DSK.ApacheLicenseForever      src/AddIns/Platform.WinUI.Graphics2DSK
    CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever      src/AddIns/Platform.WinUI.Graphics3DGL
    CodeBrix.Platform.Lottie.ApacheLicenseForever            src/AddIns/Platform.UI.Lottie
    CodeBrix.Platform.Svg.ApacheLicenseForever               src/AddIns/Platform.UI.Svg
    CodeBrix.Platform.SkiaSharp.Views.MitLicenseForever      src/AddIns/CodeBrix.Platform.SkiaSharp.Views
    CodeBrix.Platform.MediaPlayer.LgplLicenseForever         src/AddIns/Platform.UI.MediaPlayer.Skia
    CodeBrix.Platform.AdvancedTextEdit.ApacheLicenseForever  src/AddIns/Platform.UI.AdvancedTextEdit
    CodeBrix.Platform.AppSettings.ApacheLicenseForever       src/AddIns/Platform.AppSettings
    CodeBrix.Platform.AudioPlayer.ApacheLicenseForever       src/AddIns/Platform.UI.AudioPlayer.Skia
    CodeBrix.Platform.CommandBar.ApacheLicenseForever        src/AddIns/Platform.UI.CommandBar
    CodeBrix.Platform.FlexPanel.ApacheLicenseForever         src/AddIns/Platform.UI.FlexPanel
    CodeBrix.Platform.PlotterView.ApacheLicenseForever       src/AddIns/Platform.UI.PlotterView
    CodeBrix.Platform.PlayTest.ApacheLicenseForever          src/Platform.UI.Runtime.Skia.PlayTest
        (a test head, not an add-in: referenced by test projects only)
    CodeBrix.Platform.TerminalView.ApacheLicenseForever      src/AddIns/Platform.UI.TerminalView
    CodeBrix.Platform.TextLayout.ApacheLicenseForever        src/AddIns/Platform.UI.TextLayout
    CodeBrix.Platform.VideoPlayer.ApacheLicenseForever       src/AddIns/Platform.UI.VideoPlayer.Skia
    CodeBrix.Platform.WebView.ApacheLicenseForever           src/AddIns/Platform.UI.WebView.Skia

  Toolkits for Microsoft's own frameworks (src-platforms/):
    CodeBrix.Platform.WinUI.ApacheLicenseForever,
    CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever,
    CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever      src-platforms/Platform.WinUI
    CodeBrix.Platform.WPF.ApacheLicenseForever               src-platforms/Platform.WPF
    CodeBrix.Platform.Mobile.ApacheLicenseForever            src-platforms/Platform.Mobile

  NOT published (in-repo only):
    src/AddIns/Platform.UI.MediaPlayer.Skia.X11 and .Win32 (package ids
    CodeBrix.Platform.WinUI.MediaPlayer.Skia.{X11,Win32}.LgplLicenseForever) -
    the superseded native-child-window media add-ons (set_xwindow / set_hwnd
    embedding; X11/Win32 only, incompatible with Wayland and FrameBuffer).
    They remain for reference. Both csprojs are packable and self-pack on a
    Release build (GeneratePackageOnBuild), so .nupkg files for those two ids do
    appear in a local Release output - but they are deliberately excluded from
    the central pack driver's _CsprojPackage list, never reach nuget.org, and
    must NEVER be published.
    src/AddIns/Platform.UI.MSAL / Platform.UI.Maps - not in the pack list.

Every package id carries a license suffix that permanently binds the id to its
license: Apache-2.0 for all but CodeBrix.Platform.SkiaSharp.Views (MIT) and
CodeBrix.Platform.MediaPlayer (LGPL-2.1-or-later, because of LibVLC).

REPOSITORY LAYOUT
=================
  CodeBrix.Platform.Windows.slnx / .Linux.slnx / .Macos.slnx
      One solution per build OS. Each has a "/Tests/" solution folder holding
      the test projects buildable on that OS.
  Directory.Build.props
      Family-wide package metadata (RepositoryUrl, Authors, Copyright,
      PackageLicenseExpression=Apache-2.0). PackageIcon / PackageReadmeFile /
      PackageTags are set per packable project, not here.
  global.json
      Pins the MSBuild SDKs (MSBuild.Sdk.Extras, Microsoft.Build.NoTargets) and
      selects Microsoft.Testing.Platform as the test runner. allowPrerelease=false.
  version.json
      Leftover from the upstream build system. The published package version
      does NOT come from it - see PACKAGING AND PUBLISHING.
  src/
      Platform.UI                          the framework: Platform.UI.Core.csproj and
                                           Platform.UI.Skia.csproj (see "CORE AND SKIA
                                           ASSEMBLIES" below)
      Platform.UWP                         the WinRT / Windows.* API surface
                                           (Platform.Core.csproj + Platform.Skia.csproj)
      Platform.UI.Composition, Platform.UI.Dispatching   (a .Core.csproj + a .Skia.csproj each)
      Platform.Foundation, .Foundation.Logging   (.Core.csproj only - Core by whole)
      Platform.UI.FluentTheme, .v1, .v2    control styles (.Core.csproj only - Core by whole)
      Platform.UI.Toolkit                  ElevatedView, converters, DiagnosticsOverlay,
                                           StorageFileHelper (folded into the core package;
                                           .Core.csproj + .Skia.csproj)
      Platform.UI.Adapter.Microsoft.Extensions.Logging, CodeBrix.Platform.Extensions.Logging
                                           (folded into the core package; .Core.csproj only)
      Platform.UI.Core.Tests               the host-free Core suite (Core assemblies only)
      Platform.UI.Engine.Tests             the host-free engine proof (engine Cores only; no WinUI loads)
      The *.Tests.csproj files beside a library (Platform.UI.Tests.csproj, ...) are the
      unit-test FLAVOR of that library (the shared tree compiled with IS_UNIT_TESTS),
      consumed by Platform.UI.Automated.Tests; they are not test runners.
      Platform.UI.Runtime.Skia             base Skia runtime (SkiaHost, FontFamilyHelper)
      Platform.UI.Runtime.Skia.Win32, .Win32.Support, .Wpf, .X11, .Wayland,
      Platform.UI.Runtime.Skia.Linux.FrameBuffer, .Linux.FrameBuffer.Emulated,
      Platform.UI.Runtime.Skia.MacOS       the heads
      Platform.UI.XamlHost, .XamlHost.Skia.Wpf
      SourceGenerators/                    the XAML source generator (+ tests)
      Platform.Analyzers (+ .Tests)
      Platform.PackageDependencyValidator  the pack-time dependency gate
      Platform.ResourceTrimmingValidator, Platform.XamlTrimmingValidator,
      Platform.ReferenceImplComparer, Platform.UWPSyncGenerator (+ .Reference),
      Platform.NUnitTransformTool, Platform.Docs.InlineTOCGenerator   build/dev tools
      Common, Common_ViewLibraryProps, Directory.Build.props/.targets,
      *.props override files, PackageCache
      AddIns/                              one folder per add-in (+ *.Tests folders)
  CORE AND SKIA ASSEMBLIES
      Every framework library that has Skia code is TWO projects in one folder:
        <Name>.Core.csproj   AssemblyName <old name>.Core, CodeBrixRuntimeIdentifier=Core.
                             Compiles the SHARED tree (every unsuffixed file and the
                             *.crossruntime.cs files), the framework's XAML, string resources
                             and embedded resources. References only Core projects,
                             Platform.Xaml and Microsoft packages - never SkiaSharp/HarfBuzz.
        <Name>.Skia.csproj   the OLD AssemblyName (CodeBrix.Platform.UI, ...), compiles ONLY
                             the *.skia.cs files (EnableDefaultCompileItems=false) and
                             references its Core project. It implements the platform
                             contracts the Core assembly calls (namespaces <root>.Contracts;
                             implementations in <project>/Skia/*.skia.cs) and registers them
                             from Skia/SkiaPlatformBootstrap.skia.cs.
      Pairs: Platform.UI, Platform.UI.Composition, Platform.UWP (CodeBrix.Platform),
      Platform.UI.Dispatching, Platform.UI.Toolkit. Libraries with no Skia code are Core by
      WHOLE: renamed to .Core with no shell under the old name (Foundation, the three
      FluentTheme projects, Foundation.Logging, the logging adapter, Extensions.Logging).
      The Reference flavor is retired: there are no *.Reference.csproj framework projects and
      no *.reference.cs files (the add-ins' own Reference projects retire when each add-in
      splits). A file belongs to Core unless it is named *.skia.cs.
      BOOTSTRAP: the UI Skia bootstrap first runs the Dispatching, WinRT, Composition and
      Toolkit Skia bootstraps; SkiaHost (every head's host) and the host-free test suites'
      DispatcherInitializer call CodeBrix.Platform.UI.Skia.SkiaPlatformBootstrap.
      EnsureRegistered() first thing, and each bootstrap is also a module initializer.
      INTERNALSVISIBLETO: a Core assembly's AssemblyInfo.cs carries the union of the grants
      the pre-split assembly had, its Skia twin, and (by rule) CodeBrix.Android.<Y>,
      CodeBrix.Mobile.<Y> and their .Tests; the Skia twin's AssemblyInfo.skia.cs keeps the
      same pre-split grants.
      A NuGet PackageId can be carried by only one project of a solution: the Skia project
      of Platform.UI carries the aggregate id (CodeBrix.Platform.ApacheLicenseForever), so a
      packed project that must depend on the aggregate references Platform.UI.Skia.csproj
      (or references a Core project with PrivateAssets="all").
      ADD-INS split the same way (Phase C): <AddIn>.Core.csproj (AssemblyName <old>.Core,
      Core framework projects only) beside the add-in's .Skia.csproj, which keeps the
      AssemblyName and PackageId and holds only *.skia.cs; an add-in with no platform code is
      Core by whole (FlexPanel: CodeBrix.Platform.UI.FlexPanel.Core.dll, no Skia project).
      An add-in's contracts are in <root>.Contracts, found through its PlatformContract, which
      loads the add-in's platform assembly by name and runs its module initializer (an app only
      names Core types, so nothing else would load it). Packing: an add-in csproj that packs
      imports src/AddIns/AddIn.CorePackaging.targets - CodeBrixBundleInPackage="true" packs
      its Core project's dll+xml into the SAME package, CodeBrixPackAs="<csproj>" keeps a Core-by-
      whole add-in's dependency on the framework package id, and the pack takes the Release
      build's output (NoBuild), so build Release before packing.
      AppSettings: Core = the settings API, JSON values, change notification and property
      handles; the database (CodeBrix.Sqlite), its folder and the file lifecycle (backup,
      quarantine/restore, export, import) are the storage contract IAppSettingsStoragePlatform,
      so where Android/iOS keep the file is their implementation's concern. AudioPlayer: the
      Core is CodeBrix.Platform.UI.AudioPlayer.Core.dll (not ".Skia.Core") with AudioPlayer and
      SoundEffect over two output contracts; MidiPlayer stays in the Skia assembly because its
      public API is made of CodeBrix.Audio types.
      SkiaSharp.Views: its Core holds SKXamlCanvas, SKSwapChainPanel and the Skia-canvas host
      seam the drawing add-ins build on (Hosting/: SKCanvasHost and SKCanvasHostElement, an
      SKCanvas-typed paint over the framework's SKCanvasVisualBase; a consumer needs the Core's
      InternalsVisibleTo grant); its Skia assembly holds the views' surfaces (per-element
      contracts). Graphics2DSK is Core by whole (CodeBrix.Platform.WinUI.Graphics2DSK.Core.dll).
      Svg: Core = SvgProvider and SvgCanvas (on the seam); the Skia assembly is the
      [assembly: ApiExtension] registration of SvgProvider. Lottie: Core = the sources, the
      provider and the Skottie playback; the Skia assembly is the canvas supply
      (ILottieCanvasPlatform, a Graphics2DSK SKCanvasElement).
      TextLayout: Core BY WHOLE (CodeBrix.Platform.UI.TextLayout.Core.dll, no Skia assembly):
      the code API plus its own copy of the text engine. The engine is ONE source
      (src/Platform.UI/UI/Xaml/Documents: UnicodeText*, TextFormatting/FontDetails*,
      TextRunSpec, TextEngineTypes, TextGlyphOutline) compiled into the framework's Skia
      assembly AND link-compiled into TextLayout.Core (see its csproj); those files must name no
      XAML/WinRT type (the XAML-typed partials UnicodeText.Inlines, FontDetailsCache.Xaml and
      TextEngineXamlConversions are framework-only). The only platform input is the font
      source, CodeBrix.Platform.Foundation.Contracts.IFontSourcePlatform<SKTypeface> (Skia:
      FontSourceSkiaPlatform, one typeface cache for both copies). TextLayout.Core references
      only Foundation(.Logging).Core, SkiaSharp, HarfBuzz and ICU.
      AdvancedTextEdit, TerminalView, PlotterView: Core = the control and all its drawing on an
      SKCanvas; the Skia assembly is the canvas supply (IRenderCanvasPlatform: the add-in's
      software RenderCanvas, unchanged). PlotterView's typefaces come straight from the
      platform's font source (IFontSourcePlatform<SKTypeface>, the text engine's typeface
      cache), so its package does NOT depend on the TextLayout package.
      Graphics3DGL: Core = GLCanvasElement (CodeBrix.Platform.WinUI.Graphics3DGL.Core.dll, over
      the managed CodeBrix.Platform.OpenGL binding, whose GL type its API names); the Skia
      assembly is the element's visual (IGLCanvasPlatform) plus SkiaGLCanvasElement,
      SkiaGpuContext and OffscreenGLContext (their API is made of SkiaSharp types). Both are in
      the nuspec, from bin/<project name>/. VideoPlayer: the element stays in the Skia assembly
      (its API is made of CodeBrix.VideoPlayback and SkiaSharp types); its Core holds the rules,
      the source resolver and VideoPlayerFailedEventArgs. WebView and MediaPlayer are not split:
      their Core content (WebView2/CoreWebView2, MediaPlayerElement/MediaPlayer) is the
      framework's own, and the add-in assemblies are only the per-OS plumbing (WPE, LibVLC).
      ENGINES (WPE1): inside a Core, the part a non-XAML platform (CodeBrix.Mobile) reuses is
      an ENGINE - an internal type that names NO XAML/WinRT type in its bases, fields,
      parameters, returns or static initialisers (SkiaSharp and BCL only), with the control
      wrapping it. A Core's engine types live in its Engine/ folder (<root namespace>.Engine):
      TerminalView TerminalRenderer + TerminalInputEncoder; PlotterView PlotHost; Lottie
      LottiePlayer + LottieColorTheme; AudioPlayer AudioTransport; Toolkit TriPaneLayoutState;
      CommandBar IconResourceScheme/IconUri/SvgTintCss (the public IconResourceScheme forwards);
      AdvancedTextEdit keeps its public highlighting types with NEUTRAL storage behind the
      XAML-typed members (HighlightingColor/XshdColor/SimpleHighlightingBrush; HighlightingBrush
      .GetColorValue), FoldingManager over Engine/IFoldingHost, Engine/CompletionFilter.
      Timers reach an engine as its own ITickSource (Lottie, AudioPlayer), the UI thread as a
      delegate, and asset locations as the IAssetLocation contract (AudioPlayer, VideoPlayer
      resolvers; Skia: AssetLocationSkiaPlatform). Proof: src/Platform.UI.Engine.Tests drives
      every engine and asserts that UI.Core, Composition.Core, Platform.Core and
      Dispatching.Core never load; a XAML type added to an engine path fails it.
  src-platforms/
      CodeBrix.Platforms.slnx; Platform.WinUI, Platform.WPF, Platform.Mobile,
      Platform.Simple - the helper toolkits for Microsoft's own frameworks.
      They share no build-time code with src/.
  build/
      CodeBrix.Platform.Build.csproj       the pack driver (see below)
      nuget/*.nuspec, package-dependency-map.json, platform.winui.*.props/.targets
                                           nuspec-driven packages and the
                                           buildTransitive assets they ship
      nuget-pack-shim/                     turns -p: values into NuspecFile/NuspecProperties
      test-scripts/, ci/, assets/          CI scripts and assets
  samples/, tools/, templates/             see EXTRAS-README.txt
  THIRD-PARTY-NOTICES.txt                  attribution for every vendored component;
                                           ships in every package
  NOT-IMPLEMENTED.md                       target of every "not implemented" message
  CODEBRIX-PLATFORM-README.md              the family-wide package catalogue

BUILDING
========
Build the solution for the OS you are on, then (Windows only) the pack driver:

    dotnet build CodeBrix.Platform.Windows.slnx -c Release     # Windows
    dotnet build CodeBrix.Platform.Linux.slnx   -c Release     # Linux
    dotnet build CodeBrix.Platform.Macos.slnx   -c Release     # macOS

Each Skia project references its Core project, so a head (or a test) that
references Platform.UI.Skia.csproj gets the Core assemblies transitively. The
Core projects build the framework's XAML and string resources exactly as the
Skia flavor did (they set CodeBrixUIRuntimeIdentifier=Skia and
_CodeBrixUnderlyingPlatform=skia), and Platform.UI.Core.csproj keeps the
string-resource map name CodeBrix.Platform.UI/Resources (a pair of targets in
that csproj; see its comment).

The samples under samples/CodeBrixPlatform consume the framework FROM SOURCE
via ProjectReference (each head references its head csproj under src/ plus the
SourceGenerators project). Because buildTransitive targets do not flow across
a ProjectReference, the sample heads carry the runtime-replace logic
themselves; the macOS sample heads import samples/CodeBrixPlatform/
CodeBrix.MacOSHead.targets for it. A new macOS sample head needs only that one
import line. (Since the Core/Skia split there is no Reference flavor for that
logic to swap out; it re-adds the same Skia files and is kept inert.)

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
it.

macOS native library: src/Platform.UI.Runtime.Skia.MacOS/PlatformNativeMac is
built by the head csproj on macOS. Apple Silicon keeps its normal Xcode build
through build.sh; Intel defaults to build-clang.sh using Apple's Command Line
Tools. Either Release route produces one universal x86_64 + arm64 dylib. Select the
Command Line Tools route explicitly with -p:NativeMacBuildTool=Clang on either
CPU. Both builds include CDBRXMenus.m (also listed in the Xcode project), which
implements the opt-in system menu bar; the native dylib is packaged by the
existing _AddNativeMacToPackage target. BuildNativeMac=false remains a managed-
only build. Native sources and build instructions live in this repository.

Wayland protocol bindings under src/Platform.UI.Runtime.Skia.Wayland/
Wayland_Bindings/ are GENERATED and committed; regenerate them with
tools/WaylandBindingsGenerator (see EXTRAS-README.txt), never by hand.

WINDOW SIZING, ONE RULE ACROSS THE HEADS. The two public seams an application
sets - ApplicationView.PreferredLaunchViewSize and
OverlappedPresenter.PreferredMinimum*/Maximum* - are in EFFECTIVE (logical)
PIXELS on every head, and each head converts to its own native units at the
native call. The arithmetic is in one place,
src/Platform.UI.Runtime.Skia/Xaml/Window/WindowSizeConversion.cs; the X11 and
Win32 heads multiply by the display scale, the Wayland, macOS and WPF heads
pass the numbers through because their native calls already speak logical
units, and every call site says which it is. AppWindow.Size and
AppWindow.Resize are the exception: they are RAW pixels of the FRAMED window,
a matched pair, on every head. AppWindow.ResizeClient follows the rule: it
takes EFFECTIVE pixels of the CLIENT area, the pair of Window.Bounds. X11
converts to raw pixels and sizes its own (client) window; Wayland and macOS
pass the number through as the content size; Win32 converts to raw pixels and
adds the frame it measures (window rect minus client rect, or
AdjustWindowRectEx[ForDpi] when there is no client rect to measure); WPF adds
the chrome it measures (window size minus content-host size, corrected once
after the first layout if it was not measurable yet). The frame-buffer and
PlayTest heads are whole-screen, so ResizeClient is a no-op there, as Resize
is. Whether a seam means the client area or the framed window still differs
per head and is stated in the XML docs of each member.

TESTING
=======
Test projects (each is a normal `dotnet test` target; the runner is
Microsoft.Testing.Platform per global.json):

  Framework:
    src/Platform.UI/Platform.UI.Tests.csproj
    src/Platform.UI.Tests/Platform.UI.Automated.Tests.csproj (+ Tests.ViewLibrary,
        Tests.ViewLibraryProps helper projects)
    src/Platform.UWP/Platform.Tests.csproj
    src/Platform.UI.RuntimeTests/Platform.UI.RuntimeTests.Skia.csproj
        (runtime tests hosted in a real Skia head; the .Windows.csproj variant
        is Windows-only and excluded from the Linux solution)
    src/Platform.UI.Runtime.Skia.Tests/Platform.UI.Runtime.Skia.Tests.csproj
        (host-free unit tests for the Skia runtime's pure window-sizing
        arithmetic - effective-pixel to raw-pixel conversion, framed-to-client
        conversion - and for the X11 head's EWMH window-state rule; xunit.v3
        under Microsoft.Testing.Platform, no window, no X server, no Skia
        surface. Linux solution only)
    src/Platform.UI.Runtime.Skia.PlayTest.Tests/Platform.UI.Runtime.Skia.PlayTest.Tests.csproj
        (host-free unit tests for the PlayTest head's preferences, command-line
        adapter, recording-folder rules, preview frame format, OpenGL seam and
        PixelStats; all three solutions - see THE PLAYTEST HEAD PACKAGE)
    src/Platform.Foundation/Platform.Foundation.Tests.csproj
    src/Platform.UI.Composition/Platform.UI.Composition.Tests.csproj
    src/Platform.UI.Dispatching/Platform.UI.Dispatching.Tests.csproj
    src/Platform.UI.FluentTheme{,.v1,.v2}/*.Tests.csproj
    src/Platform.UI.Toolkit/Platform.UI.Toolkit.Tests.csproj and
    src/Platform.UI.Toolkit.Tests/Platform.UI.Toolkit.Automated.Tests.csproj
    src/Platform.Analyzers.Tests/Platform.Analyzers.Tests.csproj
    src/SourceGenerators/Platform.UI.SourceGenerators.Tests/
    src/SourceGenerators/XamlGenerationTests/
  Add-ins:
    src/AddIns/Platform.AppSettings.Tests, Platform.UI.AdvancedTextEdit.Tests,
    Platform.UI.CommandBar.Tests, Platform.UI.FlexPanel.Tests,
    Platform.UI.PlotterView.Tests, Platform.UI.TerminalView.Tests,
    Platform.UI.TextLayout.Tests, Platform.UI.VideoPlayer.Tests,
    CodeBrix.Platform.SkiaSharp.Views.Tests (all *.Tests.csproj),
    src/AddIns/Platform.UI.Lottie/Platform.UI.Lottie.Tests.csproj

    CodeBrix.Platform.SkiaSharp.Views.Tests is the guard for a SkiaSharp version
    bump: it pins the managed/native agreement, the add-in version's tie to
    $(SkiaSharpVersion), and the SKXamlCanvas paint-and-present path down to the
    presented pixels. Run it after changing $(SkiaSharpVersion) in
    src/Directory.Build.targets, before launching any head.

    A host-free add-in suite that builds XAML objects must run its
    XAML-touching test classes one at a time: the bootstrap those suites use
    makes every thread report that it has dispatcher access, and the object
    model is not thread-safe. Platform.UI.CommandBar.Tests does it
    assembly-wide with [assembly: Parallelization(Mode = ParallelMode.None)];
    a bigger suite can serialise only the XAML classes with a
    [CollectionDefinition(..., DisableParallelization = true)] collection. The
    add-in's AGENT-README has the consumer-facing version of this.

UI REQUIREMENTS (UIReqs): a second kind of test project, and the only one in
this repository that looks at rendered pixels. Requirements are written as
Gherkin feature files; Reqnroll generates one xunit v3 test per scenario at
build time and the Microsoft Testing Platform runs them - nobody hand-writes a
[Fact]. A scenario builds a piece of user interface in C#, drives it with
touch and key input, and then states what must be true: geometry and state
come from the visual tree, appearance comes from the frame the renderer
actually published. Every scenario runs in BOTH panel orientations. One
process is one orientation, so the same source tree is built twice - as the
Landscape project and as its Portrait twin:

    src/UIReqs/Platform.UI.Core/Platform.UI.Core.UIReqs.csproj
        the Landscape run (1920 x 1080 panel), and the project that later
        add-in UIReqs projects reference for the hosting and the canvas
        vocabulary
    src/UIReqs/Platform.UI.Core.Portrait/Platform.UI.Core.UIReqs.Portrait.csproj
        the Portrait run (1080 x 1920 panel)

Frames come from the in-process test target of the Emulated frame-buffer head
(see THE EMULATED HEAD'S TEST-TARGET MODE below), so a run needs no display,
no desktop session and no window - but that head is Linux-only, and so is the
suite. Both projects sit in the "UI Requirements" solution folder of
CodeBrix.Platform.Linux.slnx, so one solution-wide run covers both
orientations:

    dotnet test --solution CodeBrix.Platform.Linux.slnx -c Release
    dotnet test --project src/UIReqs/Platform.UI.Core/Platform.UI.Core.UIReqs.csproj -c Release
    dotnet test --project src/UIReqs/Platform.UI.Core.Portrait/Platform.UI.Core.UIReqs.Portrait.csproj -c Release

One coverage group at a time - the generated class namespace is
<RootNamespace>.Features.<Group>, so one filter matches in both assemblies:

    dotnet test --project src/UIReqs/Platform.UI.Core/Platform.UI.Core.UIReqs.csproj \
        -c Release -- --filter-class "*Features.Text*"

Everything after the bare -- goes to the runner. Each assembly is also
self-executing, which is the quickest way to cross-check a count or to see an
option rejected by name:

    dotnet src/UIReqs/Platform.UI.Core/bin/Release/net10.0/CodeBrix.Platform.UI.Core.UIReqs.dll

A failing scenario saves the frame it was looking at to
<test project>/TestResults/UIReqs/<feature>/<scenario>.png and attaches it to
the test result. That path is per project, so the two orientations never
collide, and it is already gitignored. The failure message carries the canvas
report with it: the region and its size, the frame's sequence and render
generation, the panel background, the top five colours with percentages, the
ink bounds and the ink share, and the path of that PNG.

Every frame a PASSING scenario looked at can be kept too, for review by eye.
Set CODEBRIX_UIREQS_FRAME_SAVE to a folder and the run saves every frame it
evaluates under <folder>/<Orientation>/<Domain>/<feature file>/ - the
orientation keeps the two concurrent assemblies apart, the domain is the
Features sub-folder, and a copy of the .feature source is put in each feature
folder so the requirement and its pictures sit together. A PNG is named
Scenario<N>_<slugged scenario title>_<capture number>[_<capture name>].png,
where N is the scenario's position in that feature file; a re-run replaces the
frames of the scenarios it runs rather than piling more beside them. The
folder is created if it is missing, and if it cannot be created or written to
the run says so in one line and carries on without saving. With the variable
unset nothing is read, created or written, and the run behaves exactly as it
does without the feature.

Two saved frame folders can be compared pixel by pixel, e.g. a known-good run
against the run after a change:

    build/test-scripts/compare-uireqs-frames.sh <baseline-folder> <current-folder>

It runs tools/UIReqsFrameCompare. A frame passes when it is byte-identical or
decodes to identical pixels; otherwise the report gives the differing-pixel
count and bounding box and a diff image (red on a faded copy) is written under
<current-folder>/_diff/. Missing and extra frames are reported. The groups
listed in build/test-scripts/uireqs-frame-compare.informational depend on
playback timing, so they are reported but never fail; every other group must
match exactly. An entry there is a whole group (<Group>) or one feature of a
group (<Group>/<feature file name without extension>); the feature-level
entries are the features measured to vary between identical runs (animation,
drag inertia, GL timing, indeterminate progress), each with a comment saying
why. Exit code 0 means no differences. Options: --threshold <n>,
--report <file>, --informational <entry>, and --self-test <baseline-folder>.

COVERAGE. 275 scenarios per orientation at the time of writing: 274 pass and
one is skipped, the skip being the Harness group's orientation-tagged scenario
that belongs to the other panel (@landscape-only and @portrait-only are the
only way a scenario is excluded from one of the two runs). By coverage group,
again at the time of writing:

    Harness     10  harness smoke: the panel size, a Border fill, TextBlock
                    ink, a Button tap, the between-scenario reset, and the
                    orientation tags (the one skip per run is in the Harness
                    group)
    Layout      62  layout and visuals: Grid, StackPanel, Canvas, Border, the
                    shapes, solid, linear-gradient and radial-gradient
                    brushes (including an off-centre GradientOrigin),
                    Opacity and Visibility, RenderTransform, Clip,
                    margin/padding/alignment, and the width a container
                    offers its content, which arrange alone cannot show
    Text        28  text: TextBlock appearance and wrapping, TextBox focus,
                    typing, MaxLength and read-only, PasswordBox masking
    Buttons     36  buttons and toggles: Button, Command and CanExecute,
                    ToggleButton, CheckBox, RadioButton, ToggleSwitch,
                    HyperlinkButton, RepeatButton, SplitButton, DropDownButton
    Range       21  range and progress: Slider, ProgressBar, ProgressRing,
                    RatingControl, ScrollBar
    Items       35  items and selection: ListView, GridView, ComboBox,
                    ItemsControl, ItemsRepeater, FlipView, TreeView, Pivot,
                    TabView, SelectorBar
    Popups      25  popups and dialogs: Flyout, MenuFlyout, Popup,
                    ContentDialog, TeachingTip, InfoBar, and the two
                    requirements a popup surface has to meet: a flyout too
                    tall for the panel stays on it and scrolls, and a
                    flyout's presenter covers what is behind it
    Navigation  32  navigation and containers: Frame, NavigationView,
                    SplitView, Expander, ScrollViewer, TwoPaneView
    ThemeFocus  26  theme, focus, keyboard, animation and images: the dark
                    theme, focus visuals and Tab, Enter/Space/Escape,
                    Storyboard, VisualState, and Image under each Stretch mode

ADDING A COVERAGE GROUP. Feature files go in Features/<Group>/, one file per
control. Step definitions go in Steps/<Group>Steps.cs, one [Binding] class per
file - read the sentences the existing step classes already define first,
because a near-duplicate makes both definitions match and the scenario fails as
ambiguous. Any new assertion primitive goes in a partial
Canvas/CanvasAssert.<Group>.cs, never inline in a step. Element kinds,
settable properties and colour names are taught to the shared tables through
the public ElementFactory.RegisterKind / RegisterProperty and
Colors.RegisterName, called from the group's own [BeforeTestRun] hook - never
by editing those files. One name means one thing to the whole assembly:
registering a name another group has taken, with a different implementation
behind it, throws rather than quietly winning, so anything that is not
universal gets a control-specific name ("RatingValue", not "Value").
Registering the identical delegate again is allowed, so a registration hook may
safely run twice. The Portrait twin needs no edit at all.

ADD-IN UIREQS PROJECTS. Every add-in under src/AddIns that ships as its own
package and has something to look at has its own pair, alongside the core
pair, under src/UIReqs/Platform.UI.AddIn.<Name>/ and .../<Name>.Portrait/,
with the assembly and namespace CodeBrix.Platform.UI.AddIn.<Name>.UIReqs. The
fifteen at the time of writing: FlexPanel, Graphics2DSK, SkiaSharpViews, Svg,
PlotterView, TextLayout, CommandBar, AdvancedTextEdit, TerminalView, Lottie,
Graphics3DGL, WebView, AudioPlayer, VideoPlayer and MediaPlayer (AppSettings
has no UI and MSAL is not packed from this repository). All thirty projects
sit in the "UI Requirements/AddIns" folder of CodeBrix.Platform.Linux.slnx and
run from the same solution-wide command as the core pair.

An add-in pair duplicates nothing from the core: its csproj imports
src/UIReqs/UIReqs.Common.targets (the runner, the Skia flavour, the packages,
the fonts, the feature root, the Skia-assembly swap; a twin adds one property,
UIReqsLandscapeProjectDir, and the targets link every source, feature and
asset of the Landscape project), it PROJECT-REFERENCES the core Landscape
project, and its reqnroll.json names that assembly under "bindingAssemblies",
which is what makes the core steps, hooks and argument transformations run
for the add-in's scenarios. Each project therefore holds only a
PanelOrientation.cs, an AssemblyInfo.cs, a physical reqnroll.json (a linked
one is invisible to the generator), Features/<Name>/, Steps/<Name>Steps.cs,
Support/ fixtures as C# literals or small Assets, and - only where the add-in
needs it - a Registration.cs. The orientation and the feature root are read
from the ENTRY assembly, so the core harness serves whichever executable is
running. A step text that exists in both an add-in assembly and the core is
ambiguous and fails every scenario using it: vocabulary that more than one
add-in needs lives in the core, and an add-in defines only the sentences the
core cannot say. The frame review archive files an add-in's frames under
<Orientation>/<Name>/.

REGISTRATION. An application's XAML source generator turns the add-ins'
[assembly: ApiExtension] attributes into ApiExtensibility.Register calls; a
UIReqs project compiles no XAML, so the add-ins that extend the framework
through that mechanism register themselves from a [ModuleInitializer] guarded
by ApiExtensibility.IsRegistered<T>() (Svg's ISvgProvider, Lottie's
ILottieVisualSourceProvider, WebView's INativeWebViewProvider, MediaPlayer's
two extensions). An add-in that carries default styles (CommandBar) calls its
generated GlobalStaticResources.Initialize / RegisterDefaultStyles /
RegisterResourceDictionariesBySource from a [BeforeTestRun(Order = 10)] hook
on the UI thread, after the core's Order 0 hook has launched the virtual
application; a module initializer runs too early for that. An add-in whose
package references CodeBrix.Platform.Extensions.Logging with
PrivateAssets="all" (the media add-ins and WebView) needs an explicit
ProjectReference to it, or every failure path throws FileNotFoundException
from its first this.Log() call instead of raising MediaFailed.

PREREQUISITES AND FIXTURES. A system engine an add-in needs (libvlc, the WPE
WebKit libraries, an EGL context, an audio output device) is probed in the
add-in's [BeforeTestRun]; a missing one is recorded through
Support/Prerequisite.cs and every scenario tagged @needs-<name> is then
skipped with that reason, so another machine reports "skipped: libvlc not
found" rather than "the region is blank". Nothing is skipped on a machine
that has everything. Media fixtures are small synthetic clips authored from
one filter graph (red for the first second, blue for the second; every audio
track is digital silence; players are muted as well) and committed as Assets
of the Landscape project; nothing is downloaded and nothing is written
outside the test output folder. Audio fixtures are 22.05 kHz: the shared
audio output opens once per process at the rate of the first thing played,
and the SFZ synthesizer refuses anything below 16 kHz. Every wait is a signal
or a bounded poll with a stated budget; engines get larger budgets on the
first scenario of a feature. Timers that repaint (a caret, a cursor,
animation ticks) are stopped or hidden by an explicit Given before any
two-frame comparison.

RUNNING THEM TOGETHER. WebView spawns WebKit child processes, MediaPlayer
holds a libvlc instance, Graphics3DGL and VideoPlayer take the one off-screen
EGL context a process gets, and AudioPlayer opens the audio device, so cap the
solution-wide run at four test modules at a time:

    dotnet test --solution CodeBrix.Platform.Linux.slnx -c Release --max-parallel-test-modules 4

Scenario counts per orientation at the time of writing: FlexPanel 15,
Graphics2DSK 13, SkiaSharpViews 14, Svg 13, PlotterView 15, TextLayout 15,
CommandBar 16, AdvancedTextEdit 15, TerminalView 14, Lottie 14, Graphics3DGL
14, WebView 15, AudioPlayer 14, VideoPlayer 16, MediaPlayer 16 - 219 per
orientation beside the core's 275. None of this belongs in an add-in's
AGENT-README: self-test material is maintainer material.

MAINTENANCE FACTS, none of them obvious from the source:

  - The Portrait project is a twin, not a copy: it links every .cs and every
    .feature of the Landscape project and carries only its own
    PanelOrientation.cs, which is the one file it excludes from the link. It
    does need a PHYSICAL reqnroll.json of its own, though - the generator only
    reads a reqnroll.json that Exists() in the project directory, so a linked
    one is honoured at run time and invisible at generation time. Keep the two
    files identical.
  - ReqnrollUseIntermediateOutputPathForCodeBehind is mandatory in BOTH
    projects. Without it Reqnroll writes <Feature>.feature.cs beside the
    .feature file, which means the twin writes its generated code into the
    Landscape project's source folder: every scenario then exists twice, and
    the next build warns about stale code-behind and spoils the 0-warning gate.
  - Neither project name contains "Test", so src/Directory.Build.props does not
    turn the analyzers down for them: NetAnalyzers runs at AllEnabledByDefault
    for performance and globalization, under warnings-as-errors. Fix at source;
    no NoWarn, no #pragma.
  - The font packages' .ttf files AND their .ttf.manifest files are copied
    beside the test Exe by the _UIReqsCopyFontAssets target. A head does this
    through the XAML asset machinery, which these projects deliberately do not
    import. The manifest is how the text engine resolves a FontWeight to the
    right face; without it every weight renders as the one face the URI names.
    Both steps of that target are hard errors, because a missing font renders
    as nothing and only a pixel assertion would notice.
  - The module initializer that loads the native text library is generated for
    heads only, so Hosting/TextEngineBootstrap.cs calls the framework's
    internal EnsureEngineInitialized by reflection from the virtual
    application's constructor - the same thing the two host-free add-in test
    projects do. Without it the first TextBlock measure throws
    ArgumentNullException on the 'handle' parameter.
  - --report-trx needs the Microsoft.Testing.Extensions.TrxReport package (the
    test SDK brings only its abstractions), and ANY runner option the platform
    does not recognise is reported as "Zero tests ran" with exit code 5 rather
    than as a bad argument. Try a new option by running the assembly directly
    first, where it is named in the error.
  - The between-scenario reset closes every popup a scenario left open before
    it clears the content: a Flyout, a MenuFlyout, a ContentDialog and a bare
    Popup are hosted BESIDE the application's root, so emptying the root does
    not take them off the panel, and a light-dismiss layer left over the panel
    would swallow the next scenario's first tap. A group that opens popups
    needs no cleanup of its own; a Harness scenario fences that.

DELIBERATELY OUT OF SCOPE: hover of every kind - the panel is touch-only, so
there is no pointer-over state to reach and ToolTip cannot be tested at all;
golden-image comparison, which is brittle and machine-dependent; and OCR - text
CONTENT is asserted on the tree, and only its appearance on pixels.

FRAMEWORK GAPS THE SUITE FOUND AND DID NOT PAPER OVER. Each was measured and
then left alone, because closing it is a framework feature rather than a small
fix; no scenario asserts the behaviour a gap describes, in either direction, so
none of these gaps is frozen into a requirement:

  - RichTextBlock draws nothing on the Skia runtime. The control is a bare
    FrameworkElement carrying a Blocks collection, marked not implemented, with
    no measure, no arrange and no rendering.
  - TextBlock.TextTrimming is never read by the Skia text formatter: text is
    clipped at the block's edge and no ellipsis is drawn, whatever the property
    says.
  - A swipe that carries a FlipView past about half a page turns TWO pages
    rather than one. A short swipe does turn exactly one page, and a scenario
    asserts that; but the mandatory-snap arithmetic adds one whole snap
    interval to the LIVE, mid-pan offset and then rounds that sum to the
    nearest snap point (ScrollViewer.AdjustOffsetsForSnapPoints, and
    AdjustOffsetWithMandatorySnapPoints under it), so a finger that has already
    taken the content more than half way lands two pages on and FlipView
    selects the item it came to rest over. It is a known issue: the runtime
    test When_TouchMoveMoreThanHalfItem_Then_FlipOneItem is ignored on Skia for
    exactly this. No scenario asserts the two-page behaviour.
  - A selected GridView tile is not filled with the accent colour a ListView
    row gets: the fallback tile template draws a selection border and a check
    instead of the fill its theme dictionary names.
  - The Image element lays itself out around its picture rather than around the
    box it was given, under Uniform, UniformToFill and None - so an Image's
    device rectangle is not the box the scenario asked for, and the Image
    scenarios are written about the picture instead.
  - A Button's painted fill is not the brush its Background property reports on
    this build: the same low-alpha fill is painted in both themes, so no
    scenario names a Button's fill colour.

THE EMULATED HEAD'S TEST-TARGET MODE is what those scenarios run on:
src/Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated has a second front door
beside UseLinuxFrameBuffer(). It is purely ADDITIVE - the CodeBrix.Develop
emulator path (FrameBufferHost, EmulatorConnection, the CODEBRIX_FBEMU_*
contract, the shared memory, the socket, the _exit-on-disconnect semantics) is
untouched, and the two front doors share the renderer through one internal
transport interface.

    var session = LinuxTestTarget.Setup(TestDisplayOrientation.Landscape);
    await session.LaunchAsync(() => new MyVirtualApp(), timeout);

Setup configures and runs nothing; the application's builder chain then names
the mode with UseLinuxTestTarget(session), and LaunchAsync starts the host on a
thread of its own and returns once the first frame has been published.
TestTargetSession is the whole handle:

  - RequestFrameAsync(timeout) is the frame call to reach for. It renders TWO
    passes on purpose. The compositor DRAWS the picture it recorded last and
    RECORDS the current tree on the UI thread, and it only queues that
    recording when a frame is asked for - so a single pass publishes the panel
    as it was BEFORE whatever was just applied. The first pass makes the
    compositor record the tree as it is now, a lowest-priority dispatcher drain
    lets the UI thread run that recording, and the second pass draws it.
  - WaitForFrameAsync(afterSequence, timeout) makes the weaker promise: a frame
    ARRIVED after that sequence number, not that it shows anything particular.
  - CaptureLatestFrame() returns the last published frame with no waiting.
  - TouchPress / TouchMove / TouchRelease / Tap and KeyDown / KeyUp / TypeText
    go through the very methods the emulator's socket input thread calls, so a
    scenario's finger and a person's finger take the same path.
  - RunOnUIThreadAsync(...) is the only way to touch the tree.
  - ShutdownAsync(timeout) calls Application.Current.Exit() on the UI thread,
    which opens the termination gate, ends the run loop and lets the host
    thread finish. Nothing in this mode can end the process: there is no _exit
    anywhere in it.

TestFrame is a deliberately small immutable BGRA snapshot - Width, Height,
Sequence, RenderGeneration, GetPixel, ToBitmap, SavePng. The assertion
vocabulary belongs to the consumer, not to a shipped head.

The fixed facts of the mode: ONE virtual application per process, because the
window wrapper, the pointer source and the dispatcher overrides are all
process-wide (LinuxTestTarget.Setup throws on a second call); the panel is
1920 x 1080 Landscape or 1080 x 1920 Portrait, chosen once for the life of the
process and never rotated, with the application laid out upright either way;
the display scale is fixed at 1.0 so a logical pixel is a device pixel, and
CODEBRIX_DISPLAY_SCALE_OVERRIDE is deliberately NOT consulted, so nothing
outside the process can skew a run; and the host thread is a background thread,
so a host that somehow gets stuck can never be the reason a test runner refuses
to exit.

Applications are not the audience for any of this. The mode exists for the
in-repo UIReqs projects and for the add-in UIReqs projects that will follow,
and it is public only because those projects are separate assemblies. It is
absent from the package documentation on purpose; keep it that way.

CI scripts under build/test-scripts (linux-skia-runtime-tests.sh,
macos-skia-runtime-tests.sh, android/ios UI-test scripts inherited from
upstream, run-devserver-cli-tests.ps1) drive the runtime tests with
UITEST_RUNTIME_TEST_GROUP / CODEBRIX_TESTS_FAILED_LIST / TEST_RESULTS_FILE
environment variables and expect BUILD_SOURCESDIRECTORY to be set.

Several demos double as scripted smoke tests: an environment variable makes
the app run a self-test once loaded, print PASS/FAIL lines and exit with the
failure count. EXTRAS-README.txt names each variable in the demo's entry (the
package AGENT-READMEs do not; this is maintainer material). The full set:

    AudioPlayerDemo   AUDIOPLAYERDEMO_SELFTEST=1
                      X11 head; "APD-SELFTEST: PASS|FAIL <step>" lines
    CommandBarDemo    COMMANDBARDEMO_SELFTEST=1  (+ COMMANDBARDEMO_RESULTS=<path>)
                      X11 head with DISPLAY set; xdotool and window captures
    ParityDemo        exactly one of PARITYDEMO_SELFTEST / PARITYDEMO_CLIPTEST /
                      PARITYDEMO_CHROMETEST / PARITYDEMO_TOUCHTEST = 1
                      (+ PARITYDEMO_RESULTS=<path>); X11 or Wayland head; "PARITY|" lines
    TriPaneViewDemo   TRIPANEVIEWDEMO_SELFTEST=1  (+ TRIPANEVIEWDEMO_RESULTS=<path>)
                      X11 head with DISPLAY set; injected pointer input and xdotool
    WebViewDemo       WEBVIEWDEMO_SELFTEST_DOWNLOAD_URL=<url>
                      navigates there, logs "WVD-SELFTEST:" lines, exits when the
                      download completes (pair it with a local server that sends
                      Content-Disposition: attachment)

CommandBarDemo drives the tool bars through xdotool and window captures, so
run it on the X11 head with DISPLAY set:

    cd samples/CodeBrixPlatform/CommandBarDemo/CommandBarDemo.LinuxX11
    DISPLAY=:0 COMMANDBARDEMO_SELFTEST=1 \
        dotnet bin/Release/net10.0/CommandBarDemo.LinuxX11.dll

Every window it opens is titled "CommandBar Demo", so the self-test never goes
by title: it takes the windows the desktop attributes to its own process id and
elects its OWN one by probing which of them its key presses reach; a second demo
left running on the same display does not disturb it.

TriPaneViewDemo works the same way: TRIPANEVIEWDEMO_SELFTEST=1, with
TRIPANEVIEWDEMO_RESULTS=<path> for the file, drags the dividers with injected
pointer input and reshapes the window with xdotool, so it too needs the X11 head
with DISPLAY set:

    cd samples/CodeBrixPlatform/TriPaneViewDemo/TriPaneViewDemo.LinuxX11
    DISPLAY=:0 TRIPANEVIEWDEMO_SELFTEST=1 \
        dotnet bin/Release/net10.0/TriPaneViewDemo.LinuxX11.dll

PACKAGING AND PUBLISHING
========================
THE PACK DRIVER: build/CodeBrix.Platform.Build.csproj (a NoTargets project).
It gathers the already-built Release outputs of the platform projects and
packs them into NuGet packages under:

    nugets/<Configuration>/<BuildVersion>/

Packing only runs in the Release configuration. Two kinds of package:

  - NUSPEC-DRIVEN (build/nuget/*.nuspec, packed through build/nuget-pack-shim):
    Platform.WinUI.nuspec           -> CodeBrix.Platform.ApacheLicenseForever
                                       (folds in Foundation, WinRT, Dispatching,
                                       Toolkit and the logging adapter; the
                                       "Simple" helpers come from the folded Toolkit).
                                       lib/net10.0 carries every *.Core.dll AND the
                                       Skia assemblies (CodeBrix.Platform.UI.dll, ...);
                                       codebrix-platform-runtime/net10.0/skia carries
                                       the same Skia files AND every *.Core.dll
                                       (byte-identical to lib/): runtime-replace swaps
                                       EVERY runtime assembly of the package for that
                                       folder's content, so an assembly shipped only in
                                       lib/ would be missing from the app's output.
                                       There are no Reference-flavor files any more.
    Platform.WinUI.Graphics2DSK.nuspec, Platform.WinUI.Graphics3DGL.nuspec,
    Platform.WinUI.Lottie.nuspec, Platform.WinUI.Svg.nuspec,
    CodeBrix.Platform.SkiaSharp.Views.nuspec
                                       Lottie and Svg: lib/net10.0 carries the add-in's
                                       *.Core.dll (+pdb) AND its Skia dll (the compile
                                       references: the Skia dll carries Svg's
                                       registration attribute and the name Lottie's
                                       analyzer looks for), and the runtime folder
                                       carries both too. SkiaSharp.Views ships its Core
                                       and Skia dll (+pdb) in lib/ only; Graphics2DSK's
                                       only dll is its .Core.
    The nuspec names a dependency-version TOKEN, never a literal version.
  - CSPROJ-DRIVEN (`dotnet pack <csproj> -p:PackageVersion=$(BuildVersion)`):
    Platform.UI.Runtime.Skia and the seven heads (including the Emulated head),
    the PlayTest head and its OpenGL provider (Platform.UI.Runtime.Skia.PlayTest
    .OpenGL, whose one package dependency is Graphics3DGL - the third project
    that ProjectReferences an add-in), and the add-ins WebView, AudioPlayer, VideoPlayer, MediaPlayer, TextLayout,
    FlexPanel, AdvancedTextEdit, TerminalView, PlotterView, AppSettings,
    CommandBar.
    CommandBar is the second add-in that ProjectReferences another add-in (Svg,
    for the SVG icon route, which is a HARD dependency - there is no version of
    it without SVG icons); that is why Platform.UI.Svg.Skia.csproj carries its
    PUBLISHED PackageId (CodeBrix.Platform.Svg.ApacheLicenseForever) for the
    same reason Graphics3DGL does.
    VideoPlayer is the only add-in that ProjectReferences another add-in
    (Graphics3DGL, for its off-screen GPU Skia context); that is why
    Platform.WinUI.Graphics3DGL.csproj carries its PUBLISHED PackageId
    (CodeBrix.Platform.Graphics3DGL.ApacheLicenseForever) rather than its legacy
    assembly name - the SDK packer reads a referenced project's PackageId to
    build the dependency, and the legacy id is never published.
    VideoPlayer must NEVER depend on CodeBrix.VideoPlayback.Skia. That package is
    the playback engine's presenter for hosts outside this family and it pins its
    own SkiaSharp; this family publishes as one unit and pins one SkiaSharp
    ($(SkiaSharpVersion) in src/Directory.Build.targets), and an assembly compiled
    against one SkiaSharp and run against another fails as soon as SkiaSharp
    changes a signature it uses. The Skia-bound part - the composing presenter and
    the colour-shader binding - is therefore the add-in's OWN code, ported into
    src/AddIns/Platform.UI.VideoPlayer.Skia/ and built against the family's pin.
    Five files carry the "Ported from CodeBrix.VideoPlayback.Skia" header: the
    public drawing seams IVideoLayer.cs and VideoComposingEventArgs.cs at the
    project root, and Internal/VideoPresenter.cs, Internal/YuvSurfaceRenderer.cs
    and Internal/VideoRectangles.cs.
    Everything that needs no canvas - the render paths, the letterbox arithmetic,
    the effect chain, the shader SOURCE, the composition context - stays in the
    engine and is consumed from it, never re-declared. The same rule applies to
    any future add-in that draws frames from a library with its own Skia package.
    The PackageId in each csproj is conditional on CODEBRIX_UWP_BUILD; the
    buildTransitive props/targets are packed renamed to <PackageId>.props/.targets.

VERSIONING: the driver computes a date-stamped BuildVersion automatically
(format 1.<years-since-2026>.<dayOfYear>.<minuteOfDay>, all from UTC now) and
stamps that ONE version on every package in the run. The whole family is
always published together at one version (an add-in implements internal
framework seams, so the core's InternalsVisibleTo grants must match). The
SkiaSharp.Views package is the exception: its nuspec carries a literal version
tracking the SkiaSharp release it vendors. NEVER pass -p:BuildVersion on
Windows - every Windows run takes a fresh auto-stamped version, and reusing
one is never wanted. Pinning a version belongs ONLY to the macOS rebuild below.

THE PACKAGE DEPENDENCY GATE (src/Platform.PackageDependencyValidator, driven by
build/nuget/package-dependency-map.json) runs inside the driver automatically,
twice: before packing, to generate each nuspec's dependency version tokens
from the packed projects' own PackageReferences (--emit-properties into
build/obj/nuspec-props); and after packing, as a HARD GATE over the produced
nuspec-driven packages (--package-dir). A mismatch FAILS the pack. Fix it in
the offending project's .csproj PackageReference - the .csproj is the single
version authority; never author a version literal into a nuspec.

WHAT SHIPS IN A NUPKG: the assemblies, the buildTransitive props/targets,
icon-codebrix-128.png, README, THIRD-PARTY-NOTICES.txt, and an AGENT-README.txt
at the package root. The head csprojs (Win32, Wpf, X11, Wayland, MacOS,
FrameBuffer.Emulated) pack the REPO-ROOT AGENT-README.txt
(`<None Include="..\..\AGENT-README.txt" Pack="true" PackagePath="\" />`);
each add-in csproj packs its own folder's AGENT-README.txt (the legacy
MediaPlayer .X11/.Win32 and WebView .X11 projects point at the sibling's file).
When you rename or split an AGENT-README, update these None items.

--- ON WINDOWS: build the ENTIRE package set (auto version) ---

This is the normal full build. Build the solution in Release first (the packer
gathers already-built Release outputs), then build the driver in Release:

    dotnet build CodeBrix.Platform.Windows.slnx -c Release
    dotnet build build\CodeBrix.Platform.Build.csproj -c Release

All packages land in nugets\Release\<auto-version>\ sharing that one version.
The driver captures the git branch and commit for the nuspec <repository>
tokens. The Emulated frame-buffer head is pure managed code and packs on
Windows with the rest of the set.

--- ON macOS (Apple Silicon): build ONLY the macOS package (pinned version) ---

The macOS head package contains a native dylib that must be built on a Mac,
so it is NOT produced by the Windows run above (a macOS package built
on Windows is managed-only: fine to compile against, useless at run time).
Rebuild it on an Apple Silicon Mac, pinning the version to the SAME version
the Windows run already produced and published to nuget.org. That keeps its
sibling dependencies (aggregate / base runtime / FrameBuffer) version-locked to
the published set, so publishing ONLY the rebuilt macOS package still restores
cleanly.

Do NOT run the full driver on macOS - it would also try to pack the
Windows-only packages. Instead pack just the macOS csproj (exactly what the
driver does for that one project) from the repo root, substituting the
published version for <version>:

    dotnet pack src/Platform.UI.Runtime.Skia.MacOS/Platform.UI.Runtime.Skia.MacOS.csproj \
      -c Release \
      -p:PackageVersion=<version> \
      --output nugets/Release/<version>

-p:PackageVersion (NOT -p:Version) sets only the NuGet package version while
still flowing to the ProjectReference dependency versions. This produces:

    nugets/Release/<version>/CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever.<version>.nupkg

PREREQUISITES: full Xcode for the default Apple Silicon build; Apple's Command
Line Tools for the default Intel build or -p:NativeMacBuildTool=Clang on either
CPU. The latter links the restored SkiaSharp native asset, without downloading
an extra copy. A correctly built macOS package is a universal binary and runs
on both Apple Silicon and Intel Macs.

VERIFY THE macOS PACKAGE BEFORE UPLOADING. A managed-only package (no native
dylib) packs WITHOUT error on any machine where the native step is skipped
(e.g. a non-Mac host, or BuildNativeMac=false) and is useless at runtime.
After packing, confirm the native universal binary is inside the .nupkg:

    unzip -l nugets/Release/<version>/CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever.<version>.nupkg \
      | grep runtimes/osx/native
    # Must list: runtimes/osx/native/libCodeBrixNativeMac.dylib
    # Then confirm it is a fat binary (extract it first, then):
    #   lipo -info .../runtimes/osx/native/libCodeBrixNativeMac.dylib
    #   expect: "Architectures in the fat file: ... x86_64 arm64"

On an Apple-Silicon build the csproj FAILS the pack with an explicit error if
the native dylib is absent (so a green pack there means the dylib is present);
the verify step above still matters when packing anywhere the native step is
skipped. The WebView add-in's macOS download support needs a dylib rebuilt
from PlatformNativeMac sources dated 2026-07-17 or later.

--- ON LINUX: a VERIFICATION aid only - it never publishes ---

build/test-scripts/pack-linux-local-feed.sh packs the Linux-buildable subset
of the family into a local folder (usable as a folder feed) with the driver's
own pack commands and its dependency gate, to check on Linux what a change
does to the packages. It is not part of the publish recipe above. After a
Release build of CodeBrix.Platform.Linux.slnx:

    build/test-scripts/pack-linux-local-feed.sh <output-folder> [version]

The Win32, Wpf and macOS head packages are left out; the header of the script
says why. Linux file names are case-sensitive, so a nuspec <file src> path
whose letter case differs from the folder on disk (which Windows would not
notice) stops the script with the path named.

THE EMULATED FRAME-BUFFER HEAD PACKAGE
(CodeBrix.Platform.Runtime.Skia.FrameBuffer.Emulated.ApacheLicenseForever,
src/Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated): a compile-time
drop-in for the FrameBuffer head that renders offscreen at one fixed resolution
and exchanges frames and touch input with the CodeBrix.Develop frame-buffer
emulator over shared memory and a socket (CODEBRIX_FBEMU_WIDTH / _HEIGHT /
_SHM_PATH / _SOCKET_PATH / _LANGUAGE / _FONT_ISOLATION environment variables).
Applications must NEVER reference it directly: when a .LinuxFrameBuffer head is
run or debugged inside CodeBrix.Develop, the IDE builds the app against this
package instead of the real FrameBuffer package (an MSBuild-property-injected
swap; the user's csproj is never modified) and hosts the app's screen in its
emulator window. It surfaces the same UseLinuxFrameBuffer() bootstrap and the
same buildTransitive behavior as the real head. It LINKS the head-neutral
sources from src/Platform.UI.Runtime.Skia.Linux.FrameBuffer/Shared/ (builder
options, pickers, software keyboard, clipboard) from its csproj - keep those
files head-neutral, and add new shared FrameBuffer features there so both
heads get them.

--- THE src-platforms TOOLKITS PACK THEMSELVES (not the driver) ---

The three helper toolkits for Microsoft's own UI frameworks are NOT built or
packed by the pack driver above. src-platforms is a deliberately isolated,
standalone package tree:

  - src-platforms/Directory.Build.props exists ONLY to stop MSBuild walking
    further up and importing the repo-root and src/ build machinery. Never add
    an Import to it - that would re-couple the tree and defeat the isolation.
  - Every packable .csproj there sets GeneratePackageOnBuild=true, so a plain
    Release build of a project produces a ready-to-upload .nupkg. There is no
    driver, no nuspec and no dependency gate in this tree.
  - Each of those csprojs declares its OWN NuGet metadata and carries its own
    copy of the canonical date-stamped version block - the same
    1.<years-since-2026>.<dayOfYear>.<minuteOfDay> shape the driver computes,
    evaluated from UTC now inside the project. Consequences: every build
    produces a new version, and two builds within the same UTC minute produce
    the SAME version, so never publish two packages from within one minute.
    Re-baselining the minor number means changing _VersionBaseYear in each
    csproj.
  - The five packable projects and their ids:
        Platform.WinUI/Core    CodeBrix.Platform.WinUI.ApacheLicenseForever
        Platform.WinUI/Skia    CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever
        Platform.WinUI/Lottie  CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever
        Platform.WPF/Core      CodeBrix.Platform.WPF.ApacheLicenseForever
        Platform.Mobile/Core   CodeBrix.Platform.Mobile.ApacheLicenseForever
    License for all five: PackageLicenseExpression Apache-2.0, matching the
    .ApacheLicenseForever id suffix. Third-party provenance for ported code is
    in the toolkit folder's own THIRD-PARTY-NOTICES.txt.
  - Each package packs four extra files: icon-codebrix-128.png from the REPO
    ROOT, and README.md, AGENT-README.txt and THIRD-PARTY-NOTICES.txt from its
    own src-platforms/<Toolkit>/ folder (so the three WinUI packages all ship
    the one Platform.WinUI AGENT-README.txt). When you rename or split one of
    those files, update these None items.
  - Platform.Simple is a shared-SOURCE folder, not a project: the WinUI, WPF
    and Mobile Core csprojs each Compile-Include its seven files with
    Link="Simple\...". The WinUI Skia and Lottie companions do not.
  - Which branch of those shared sources compiles is chosen by a per-toolkit
    DefineConstants in the Core csproj, with no Configuration condition so it
    applies in Debug AND Release:
        Platform.WinUI/Core    WIN_UI   (Microsoft.UI.Xaml / Microsoft.UI.Dispatching)
        Platform.Mobile/Core   MAUI     (Microsoft.Maui.*)
        Platform.WPF/Core      none     (the #else branch: System.Windows, reached
                                        by defining none of WIN_UI / MAUI /
                                        HAS_CODEBRIX; UseWPF plus the -windows
                                        target framework supply System.Windows)
    HAS_CODEBRIX - the CodeBrix.Platform branch - is deliberately NOT defined in
    any of the three. Adding a branch to a shared source file means touching all
    four paths.
  - WHERE NEW WinUI CODE GOES: anything to do with Skia graphics/rendering on
    WinUI that is NOT Lottie belongs in Platform.WinUI/Skia; Platform.WinUI/
    Lottie adds only Lottie parsing/rendering. The dependency direction is
    strictly Lottie -> Skia -> Core and must stay that way (Skia
    ProjectReferences Core; Lottie ProjectReferences Core and Skia).
  - RENDERING FIDELITY IS A DESIGN REQUIREMENT: the SVG and Lottie code in
    Platform.WinUI/Skia and Platform.WinUI/Lottie is PORTED from the
    CodeBrix.Platform add-ins under src/AddIns/Platform.UI.Svg and
    src/AddIns/Platform.UI.Lottie so the same SVG file or Lottie JSON renders
    identically here and on the Skia heads. Each ported file names its origin
    in a header comment. Check the original before changing rendering
    behavior, and change both when the behavior must move.
  - Target frameworks differ per toolkit (WinUI net10.0-windows10.0.19041.0,
    WPF net10.0-windows, Mobile net10.0-android/-ios/-maccatalyst plus a
    Windows target added only when building on Windows), so a full build of
    src-platforms/CodeBrix.Platforms.slnx is a Windows operation with the MAUI
    workloads installed.

PROVENANCE AND VENDORED SOURCES
===============================
CodeBrix.Platform is a fork of the upstream open-source WinUI-compatible UI
framework, taken from its 6.5.x development line, re-licensed and re-packaged
under the CodeBrix.Platform name. Every renamed namespace carries a
"//Was previously: <upstream namespace>" comment on its namespace line; that
comment is the record of the mapping - keep it when touching a file. The
Vulkan renderer under the X11 head was pulled from the upstream 6.7.x line and
is gated behind the internal FeatureConfiguration.Rendering.UseVulkanOnX11
flag with no public API (X11RenderingBackend has its Vulkan member commented
out; enable both together when Vulkan is officially offered).

Other vendored / derived components:
  - tools/WaylandBindingsGenerator: a frozen fork of the MIT-licensed NWayland
    bindings generator (LICENSE-NWayland.md, PORTING-NOTES.txt there), driven
    by pinned copies of the freedesktop wayland / wayland-protocols XML.
  - The FrameBuffer head's libinput and xkbcommon interop derives from
    Avalonia (MIT) - see the file headers under
    src/Platform.UI.Runtime.Skia.Linux.FrameBuffer/Native and Devices/Input.
  - SkiaSharp is used AS-IS (never forked); CodeBrix.Platform.SkiaSharp.Views
    vendors the SkiaSharp views for the framework.
  - The add-ins vendor their own upstreams (MAUI FlexLayout, AvalonEdit, ...);
    each add-in's AGENT-README states its provenance.
  - The VideoPlayer add-in's five Skia-bound files (IVideoLayer.cs,
    VideoComposingEventArgs.cs and the three under Internal/) carry a "Ported
    from CodeBrix.VideoPlayback.Skia" header. That is the same author's own MIT
    package, so it is recorded here and in the add-in's AGENT-README rather than
    as a numbered THIRD-PARTY-NOTICES item.
  - Complete attribution and license texts: THIRD-PARTY-NOTICES.txt (numbered
    items; the add-in docs cite item numbers).

The upstream project's name must not appear in consumer documentation; say
"the upstream project".

TRIMMING
========
  - Trimmable assemblies: every framework Core is IsTrimmable (Foundation.Core,
    Extensions.Logging.Core and UI.Core with IsAotCompatible); the add-in Cores
    AdvancedTextEdit, AudioPlayer, CommandBar, FlexPanel, Graphics2DSK,
    Graphics3DGL, Lottie, PlotterView, SkiaSharp.Views, TerminalView,
    TextLayout and VideoPlayer, plus Foundation.Logging.Core and the
    M.E.Logging adapter Core, set IsAotCompatible (implies IsTrimmable and
    turns on the trim/AOT/single-file analyzers; src has TreatWarningsAsErrors,
    so a new finding fails the build and is fixed at source). NOT marked yet:
    AppSettings.Core (reflection-based JSON over the open Get<T>/Set(object)
    API - a public-surface decision) and Svg.Core (CodeBrix.SkiaSvg's
    SKSvg.Load is RequiresUnreferencedCode - fixed in that repository). The
    reason stands in a comment in each csproj.
  - Embedded linker descriptors: ILLink honors both ILLink.Descriptors.xml and
    the legacy resource name <AssemblyName>.xml (verified with the .NET 10
    SDK). Toolkit.Core's descriptor (LinkerDefinition.net6.0.xml) roots its
    XAML-facing types (the extension classes, FromJsonExtension, TriPaneView,
    TriPaneViewDivider) behind the feature switch
    CodeBrix.Platform.UI.Toolkit.RootXamlControls (featuredefault true): the
    Skia heads and CodeBrix.Android keep them with no setting; a consumer that
    uses no XAML from Toolkit.Core (a CodeBrix.Mobile app that uses only
    engines) drops them with
      <RuntimeHostConfigurationOption Include="CodeBrix.Platform.UI.Toolkit.RootXamlControls" Value="false" Trim="true" />
    The same RuntimeHostConfigurationOption mechanism carries the framework's
    own trimming switches (build/nuget/platform.winui.common.targets). The
    M.E.Logging adapter Core's descriptor roots that whole (small) assembly.
  - XAML resource trimming (CodeBrixXamlResourcesTrimming=true in an app's
    head csproj, with PublishTrimmed; opt-in, off by default): the
    linker-hint passes (LinkerHintGeneratorTask, Platform.UI.Tasks.targets)
    trim the app repeatedly to find which XAML types survive, and the final
    trim drops the default styles and the generated bindable metadata of the
    types that did not. The generated BindableMetadataProvider hints are ON
    in every pass and in the final trim (decision D10, WPE1-11:
    LinkerHintBindableMetadata.cs); they used to stay off, which compiled the
    app's bindable metadata away and trimmed navigated pages' constructors
    (MissingMethodException from Frame.Navigate at startup). A page must be
    named with typeof(...) somewhere (Frame.Navigate(typeof(MainPage)) is
    enough); a page reached only by a type-name string is trimmed. Verify a
    change here by publishing the template app trimmed in this mode and
    starting it (frame buffer and X11): MainPage must load. Fence:
    SourceGenerators.Tests Given_LinkerHintBindableMetadata.
  - Engine proof: src/Platform.UI.Engine.Tests drives each add-in engine
    host-free and asserts (EngineIsolation) that no WinUI assembly
    (UI.Core, Composition.Core, the WinRT Core, Dispatching.Core, Xaml,
    FluentTheme/Toolkit Cores) and no Skia twin was loaded. It references
    engine Cores only; an engine test that makes one of them load fails the
    whole project on purpose.

CODING CONVENTIONS
==================
  - Never say the upstream framework's name in type names, docs or discussion;
    "//Was previously:" comments in source are the one sanctioned place it
    appears.
  - Package ids: every id ends in .ApacheLicenseForever / .MitLicenseForever /
    .LgplLicenseForever. Namespaces never carry the suffix. A csproj sets its
    PackageId conditionally on CODEBRIX_UWP_BUILD.
  - Dependency versions live in csproj PackageReferences only; nuspecs use
    tokens. The dependency gate enforces this.
  - Public API in the core keeps the full WinUI/UWP shape; a member with no
    implementation throws a "not implemented" exception that names the member
    and points at NOT-IMPLEMENTED.md - do not delete unimplemented members.
  - Head-neutral FrameBuffer code goes under
    src/Platform.UI.Runtime.Skia.Linux.FrameBuffer/Shared/ so the Emulated
    head links it.
  - Head-specific compilation constants (HAS_CODEBRIX_SKIA, HAS_CODEBRIX_SKIA_WIN32,
    __DESKTOP__, ...) are injected by each head's buildTransitive props;
    HAS_CODEBRIX / HAS_CODEBRIX_WINUI by the core package's common targets.
    Consumer docs still tell apps to declare HAS_CODEBRIX;HAS_CODEBRIX_WINUI
    explicitly (the reference-app convention).
  - Documentation files: plain-text AGENT-README.txt per package (consumer),
    MAINTAINER-README.txt and EXTRAS-README.txt at the root, README-INDEX.txt
    as the map. No version numbers in AGENT-README files. The AI-agent pointer
    stubs (AGENTS.md, CLAUDE.md, .clinerules, .cursorrules,
    .cursor/rules/agent-readme.mdc, .windsurfrules,
    .github/copilot-instructions.md, .junie/guidelines.md) all point at
    AGENT-README.txt and are maintained centrally across the family.
  - XML documentation: the eleven packable add-ins under src/AddIns (the
    _CsprojPackage list in build/CodeBrix.Platform.Build.csproj) each set
    GenerateDocumentationFile, so their packages carry lib/net10.0/*.xml and a
    consumer gets IntelliSense text; CS1591 is fixed at source in those
    projects, never suppressed. The CORE packages do NOT ship XML docs yet:
    the switch has not been turned on for them, because the ported framework
    sources would raise CS1591 in the thousands and each one is a comment to
    be written rather than a warning to be silenced. That sweep is a separate,
    later decision. The five add-ins packed by the hand-written nuspecs in
    build/nuget (Svg, Lottie, SkiaSharp.Views, Graphics2DSK, Graphics3DGL)
    also ship no .xml: their nuspecs name each lib file explicitly, so adding
    one is a packaging change.
  - Root doc filenames use dashes (CODEBRIX-PLATFORM-README.md,
    NOT-IMPLEMENTED.md, THIRD-PARTY-NOTICES.txt).
  - The Generated/3.0.0.0 folders (Platform.UI, Platform.UWP,
    Platform.Foundation, Platform.UI.Composition, Platform.UI.Dispatching) are
    the OUTPUT of the API sync generator (src/Platform.UWPSyncGenerator, "sync"
    mode) and are never edited by hand: a regeneration rewrites every file. To
    implement a generated member, declare it (same public signature) in a
    hand-written partial outside Generated/ and regenerate; the generator then
    writes "// Skipping already declared ..." in place of the stub. A type
    that gets a hand-written part loses the generated type-level
    [NotImplemented] marker and internal constructor, so declare those in the
    hand-written part if they must stay. Members the generator's metadata lacks
    (it compares against Windows SDK 10.0.22000 contracts and Windows App SDK
    1.8) must live in hand-written files too, or a regeneration deletes them
    (example: src/Platform.UWP/UI/Notifications/ToastNotificationMode.cs).
    The generator runs on Linux: build it (Debug), restore
    src/Platform.UI/Platform.UI.Core.csproj, .Skia.csproj and .Tests.csproj,
    then run "dotnet CodeBrix.Platform.UWPSyncGenerator.dll sync" from its
    bin/Debug folder (a few minutes, CPU-heavy). Its reference metadata comes
    from the NuGet cache (the packages listed in
    src/Platform.UWPSyncGenerator/Helpers/WinRTReferenceList.cs, fetched with a
    PackageDownload-only project); it writes nothing outside Generated/. Diff
    the result before keeping it: anything beyond format normalization means a
    hand edit or a hand-written member was missed.

DELIBERATE DIFFERENCES FROM WINUI
=================================
Behaviour that intentionally does not match WinUI. Keep it when porting newer
upstream code, and add an entry here for each new one.
  - (None at present.) Automation names follow WinUI:
    AutomationPeer.GetName() returns GetNameCore() and never reads
    ToolTipService. Naming an icon-only control by its tooltip is a PlayTest
    locator rule (src/Platform.UI.Runtime.Skia.PlayTest/VisualTree.cs Name:
    peer name, then the visible text inside the element, then the tooltip),
    not a peer rule. Do not move it back into GetName: framework callers of
    GetName (LabeledBy, Expander, TreeView drag text, TabViewItem headers,
    the simple-accessibility child aggregation) would read tooltips, and a
    tooltip there outranks the visible text of a button whose Content is a
    panel, so name-based locators for such buttons stop matching.

XAML GENERATOR CONTRACTS
========================
  - Add-in registrations: for every [assembly: ApiExtension(...)] an
    application references, the App code the generator emits calls
    ApiExtensibility.Register inside an
    "if (!ApiExtensibility.IsRegistered<TExtension>())" guard
    (XamlFileGenerator.WriteApiExtensionRegistration). The FIRST registration
    wins, for every add-in: a test double registered before the application
    object is created is kept, and a second application object in the same
    process does not throw on the duplicate.
  - Diagnostic ids. XAML generation: UXAML0001 (error), UXAML0002 (warning),
    UXAML0003 (resource error) - XamlCodeGeneration.Diagnostics.cs. The
    UnoNNNN family (analyzers and generators): Uno0001 not-implemented
    member, Uno0002 native-view disposal, Uno0003 native view implementing
    DependencyObject, Uno0006 InitializeComponent, Uno0007 missing assembly,
    Uno0008 binding target property not found (a warning: a {Binding},
    {x:Bind} or {TemplateBinding} on a member the element's type does not
    have; checked only for DependencyObject types whose base chain has no
    partial type in the compilation being built, because other generators add
    members the XAML generator cannot see). Uno0004/Uno0005 are unused; the
    next new id is Uno0009.
  - Generator test baselines. The XamlCodeGeneratorTests suite in
    src/SourceGenerators/Platform.UI.SourceGenerators.Tests compares the
    generated sources with baselines under XamlCodeGeneratorTests/Out/
    (embedded into the test assembly at build time). Out/ is git-ignored and
    NOT committed, so a fresh clone has no baselines: a test whose generator
    produced files but that has no baseline at all is reported SKIPPED
    (inconclusive), with a message naming the missing Out/<class>/<test>
    folder (Verifiers/CSGenerator.cs). A test that has a baseline is still
    compared file by file and FAILS on any mismatch. To create or refresh
    baselines: uncomment "#define WRITE_EXPECTED" at the top of
    XamlCodeGeneratorTests/Verifiers/CSGenerator.cs, run the tests concerned
    once in Debug, review the files written under Out/, comment the define
    again and rebuild (the define must never stay on: under it the content
    check is skipped).

ELEMENT THEMES (FrameworkElement.RequestedTheme)
================================================
Element-level RequestedTheme themes the element's subtree, the WinUI way
("requested theme for the subtree" during theme-reference resolution):
  - ResourceDictionary.Themes.Active is the theme ThemeDictionaries lookups
    use: an element theme scope (thread-static, opened with
    ResourceDictionary.PushThemeScope) when one is open, else the application
    theme (SetActiveTheme). A dictionary's key-not-found cache is only used
    while the active theme is the one it was filled for.
  - FrameworkElement.EnterThemeScope(owner, contextProvider) opens the scope
    for the effective theme of the owner (an element, the context provider, or
    the nearest element above a non-element owner). It is opened in
    DependencyObjectStore.UpdateResourceBindings (load, theme walks, styles,
    visual states, brushes/storyboards/key frames of an element),
    ResourceResolver.ApplyResource, Setter.ApplyValue (visual-state setters)
    and around an element's own Resources in UpdateThemeBindings. Objects with
    no element context keep the surrounding theme (application/system
    dictionaries resolve in the application theme).
  - Effective theme (FrameworkElement.Theming.cs): own RequestedTheme, else
    the nearest ancestor's along FrameworkElement.Parent (so a popup child
    follows its Popup), else Default = the application's. Cached per element
    against a global generation that RequestedTheme changes, parent changes
    and logical-parent changes bump; a read is a field compare. Until the first
    element in the process sets a non-Default RequestedTheme, none of this
    runs (HasElementThemeOverrides) and lookups are exactly the application's.
  - A RequestedTheme change re-resolves the element's subtree with
    Application.PropagateResourcesChanged(..., themeRoot), which skips
    descendants that set their own theme; the XamlRoot content instead syncs
    the application theme, whose walk covers the tree.
  - Default text foreground: DependencyProperty.GetDefaultValue gives the six
    text Foreground properties DefaultBrushes.GetTextForegroundBrush(element),
    the element theme's DefaultTextForegroundThemeBrush. An element with its
    own RequestedTheme is an inheritance boundary for inherited "Foreground"
    properties (DependencyObjectStore.OnParentPropertyChangedCallback and
    OnThemeBoundaryChanged), and a Foreground that falls back to its default is
    passed to children as unset, never as the parent's default brush instance.
  - Not implemented: a Flyout forwarding its placement target's theme to the
    presenter; a popup child's inherited text Foreground follows the popup
    host, not the Popup's place in the tree; ThemeResource references inside a
    resource that is not in ThemeDictionaries are one shared object.
  - Tests: src/Platform.UI.Tests/Windows_UI_Xaml/Theming/
    Given_Element_Theme_Inheritance.cs and the PlayTestDemo ElementThemeTests.

NOTES
=====
  - The X11 head's DISPLAY check is a regex over "[host]:display[.screen]";
    the Wayland head deliberately does NOT sniff the environment - the
    authoritative check is wl_display_connect at startup, so a missing
    compositor produces the clean "This application requires a Wayland
    compositor." fail-fast rather than an opaque "No platform host could be
    selected".
  - The FrameBuffer head reads CODEBRIX_FRAMEBUFFER_USE_DRM before the builder's
    UseKMSDRM/DisableKMSDRM so a launcher (CodeBrix.Develop, an SSH remote run)
    can pin software rendering; it also reads FRAMEBUFFER, XKB_DEFAULT_LAYOUT,
    CODEBRIX_DISPLAY_SCALE_OVERRIDE, CODEBRIX_FRAMEBUFFER_ORIENTATION_SOURCE and
    CODEBRIX_FRAMEBUFFER_TOUCH_ROTATION.
  - The Wayland head's drag-and-drop is implemented per protocol; failures on
    Cinnamon/Muffin (garbage enter coordinates from XWayland sources) are a
    compositor bug, not planned work here. Touch, subsurface-hosted native
    content (parity plan P7) and IME remain deferred on Wayland; IME is
    missing on X11 too.
  - Legacy MediaPlayer .X11/.Win32 add-ins: never publish (see PURPOSE AND
    SCOPE).
  - templates/TemplateApp.zip is the scaffold CodeBrix.Develop's "New
    CodeBrix.Platform Application" uses; keep it in step with the reference
    structure documented in AGENT-README.txt.
  - ProgressRing animations (decision D3, WPE1-11): the default
    FeatureConfiguration.ProgressRing URIs name the Core assembly
    (embedded://CodeBrix.Platform.UI.Core/...). The Skia assembly
    CodeBrix.Platform.UI still embeds the same two .json files (the
    EmbeddedResource item in src/Platform.UI/Platform.UI.Skia.csproj) so an
    app that set the old embedded://CodeBrix.Platform.UI/... URIs keeps
    working for ONE release. Remove that item after the first nuget.org
    release that carries the Core split (the first family version published
    after 1.0.254.167): the copy ships in that release and is gone from the
    one after it.

MACOS APPLICATION NAMING AND SYSTEM MENU VALIDATION
--------------------------------------------------
UseMacOS(mac => mac.UseSystemAppName("My App")) applies a startup-only,
process-wide name independently of menu projection. It sets NSProcessInfo's
name and AppKit's process-local bundle-name caches before creating NSApplication,
then synchronizes the running-application display name during
applicationWillFinishLaunching. No bundle/executable/assembly files are renamed
or modified. See PlatformNativeMac/README.md under the macOS head for the guarded
LaunchServices SPI and cache-mutability compatibility behavior.

The opt-in is UseMacOS(mac => mac.UseSystemMenuBar()). MacOSMenuBarExtension
selects the first visible MenuBar in visual-tree order per window. Only that
bar suppresses its layout/rendering; its properties and logical lifetime stay
intact, and additional bars remain in-window. The extension is registered only
by an opted-in macOS host. Native callbacks queue command invocation after
AppKit menu tracking, and do not bypass the application's Quit handler.
Native menu/item objects are reconciled by managed element ID, with AppKit-owned
items preserved. NSApplication.helpMenu points to an unlisted menu while the
projection is active, using AppKit's supported opt-out from automatic Spotlight
Help search. This avoids Help opening and immediately closing on macOS 15 and
keeps the Help contents defined by the application. Deactivation restores the
host's original Help-menu policy.

A real-AppKit regression probe (run from an interactive macOS desktop):

    dotnet build build/test-scripts/MacOSMenuBarProbe/MacOSMenuBarProbe.csproj -c Release
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll native
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll composition
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll default
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll close
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll validation
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll name
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll name-menu
    dotnet build/test-scripts/MacOSMenuBarProbe/bin/Release/net10.0/MacOSMenuBarProbe.dll name-menu-reversed

The probe checks first-visible selection, zero menu-row footprint, additional
bars, hiding/removing/reloading menus, dynamic submenus, commands, toggle state,
shortcuts, disabled entries, focus/pointer exclusion, modal dialogs and window
switching. MenuTracking.m drives the actual Help menu-bar accessibility action
in process, observes sustained AppKit tracking before dismissal, and repeats
after refreshes. It needs an interactive desktop but no Accessibility or Screen
Recording grant. The close mode finishes through Window.Close
rather than Application.Exit, checking the existing last-window exit policy.
The native mode uses DirectSkiaCanvasMode; composition uses the default renderer.
These checks use the real macOS host, not PlayTest's virtual host. On the Intel
Mac (2026-09-30), both name+menu option orders passed 49 checks, name-only passed
6, menu-only passed 45, and default passed 4. Validation rejects null/blank/
embedded-null names and checks that configuring the builder is fluent and does
not mutate process state.
The naming checks include Unicode, NSProcessInfo.processName, AppKit's actual
application-menu accessibility title, NSRunningApplication.localizedName, and
unchanged assembly identity. Apple Silicon execution still needs validation on
that machine. The native Release binary contains both CPUs.

Local prerelease packages for testing without a public release:

    python3 build/pack-macos-preview.py --version 1.0.273.1-macosmenu.4

Choose a fresh prerelease version after changing code. The script builds the
core, shared runtime, framebuffer dependency and macOS head into nugets/MacOSPreview,
checks both native architectures and the menu/name exports, and never publishes.
Preview 1.0.273.1-macosmenu.4 was consumed by Fresco.Brix's macOS head on Intel:
Cocoa's process name, NSRunningApplication.localizedName and the actual AppKit
application-menu caption all reported Fresco.Brix, while the assembly remained
Fresco.Brix.MacOS. Help opened and stayed visible on five successive menu-bar
activations, Help > About opened its dialog, and Help worked again after that dialog closed. Help
contained only the application's commands, without an automatic search item.
Native File > New Document and File > Quit also passed with clean process exit.
The head's normal build and osx-arm64 cross-build passed without warnings or
errors; Apple Silicon runtime validation still needs that machine.

THE PLAYTEST HEAD PACKAGE
=========================
(CodeBrix.Platform.PlayTest.ApacheLicenseForever,
src/Platform.UI.Runtime.Skia.PlayTest; consumer guide: its AGENT-README.txt.)

--- Project shape ---

  - Namespace CodeBrix.Platform.PlayTest; assembly
    CodeBrix.Platform.UI.Runtime.Skia.PlayTest; TargetFrameworks
    $(NetSkiaPreviousAndCurrent) with ../targetframework-override-noplatform.props,
    like the heads. Nullable is ENABLED, as in every head and add-in; implicit
    usings are off (src/Directory.Build.props). GenerateDocumentationFile is
    on: CS1591 is fixed by writing the doc comment, never by NoWarn.
  - OutputType Exe + StartupObject (Preview/PreviewProgram): the ONLY packed
    Platform project that is an executable. The package assembly doubles as
    the headed preview's process entry point - PreviewConnection starts
    `dotnet exec --depsfile <consumer>.deps.json --runtimeconfig
    <consumer>.runtimeconfig.json CodeBrix.Platform.UI.Runtime.Skia.PlayTest.dll
    --preview <w> <h>` - so no second package or executable is needed. Keep it.
  - Packs the ROOT README.md (like every head/add-in), its OWN AGENT-README.txt
    (the consumer guide, add-in pattern), the icon and THIRD-PARTY-NOTICES.txt;
    buildTransitive/*.props|.targets are packed renamed to the PackageId; the
    two runner adapters buildTransitive/*.cs are packed as content and
    Compile-Removed from the assembly.
  - Foundation/UWP ProjectReferences carry TreatAsPackageReference="false"
    PrivateAssets="all" exactly like the heads (folded into the core package);
    Extensions.Logging is PrivateAssets="all" for the same reason.
  - The Unicode/UnicodeMacOs pins follow the TextLayout add-in: PlayTest
    renders text on Windows/macOS without a desktop head to supply ICU.
  - SDL3: the preview's bindings + natives come from the family's own
    CodeBrix.Sdl3.ZlibLicenseForever PackageReference (the usings in
    Preview/PreviewProgram.cs are CodeBrix.Sdl3). It is a normal NuGet
    dependency with none of its own; no SDL initialization occurs in the offscreen
    application process.
  - AssemblyInfo.cs grants InternalsVisibleTo to
    CodeBrix.Platform.UI.Runtime.Skia.PlayTest.Tests (the Runtime.Skia/X11
    pattern).
  - Normal packaging: build/CodeBrix.Platform.Build.csproj includes this
    project in _CsprojPackage and applies the shared PackageVersion. Do not set
    the assembly Version to a NuGet version. The core/runtime packages must
    include the PlayTest friend declarations; an older published core alone
    cannot run a newer PlayTest head. build/pack-playtest-preview.py builds a
    local, never-published preview feed (EXTRAS-README.txt).

--- Architecture ---

This is a separate head; do not add PlayTest branches to the existing desktop
heads. VirtualHost owns the UI dispatcher, the CPU Skia renderer, the
window/display extensions, the isolated clipboard and synthetic input; it was
adapted from the FrameBuffer.Emulated head but is deliberately independent of
it. Shared framework assembly friend entries permit the same internal extension
and compositor APIs used by the other heads. The virtual resolution is
1920x1080 or 1080x1920 at rasterization scale 1. Orientation can change between
serialized tests: a screen generation and its dimensions travel with each
immutable frame, and frames from old generations are discarded.
SetContentAsync applies an optional orientation before creating the page and
restores the launch preference when no override is supplied. Page replacement
disposes a Frame-hosted page's disposable DataContext as well as a direct
page's DataContext.

--- Preferences (keep the precedence chains exactly) ---

  Preview: explicit PlayTestOptions.Headless, then the MTP command-line option
  (--headed / --nonheadless / --headless), then CODEBRIX_PLAYTEST_HEADED, then
  headless. Conflicting headed/headless flags are rejected before tests run.
  The package compiles buildTransitive/CodeBrix.PlayTest.TestingPlatform.cs
  into Microsoft.Testing.Platform consumers and registers its
  TestingPlatformBuilderHook before MTP generates/caches
  SelfRegisteredExtensions. Keep that adapter out of the runtime assembly and
  its dependencies: it uses the consumer's MTP version. It is compiled with the
  CONSUMER's nullable setting, so it must stay warning-free with nullable
  enabled and disabled (no reference-type '?' annotations; nullable values flow
  through var locals). Validation records a nullable Boolean in AppContext
  under CodeBrix.Platform.PlayTest.CommandLineHeadless (and the theme,
  orientation and screenshot-folder values under their CommandLine* keys)
  without changing the environment. PlayTestOptions reads the headless setting
  when constructed. Other runners retain environment/code configuration;
  custom MTP entry points must invoke the generated AddSelfRegisteredExtensions
  hook. The source-based PlayTestDemo imports these targets too.

  Orientation: explicit PlayTestOptions.Orientation, --orientation, environment
  variable CODEBRIX_PLAYTEST_ORIENTATION, CodeBrixPlayTestPreferredOrientation
  project metadata in ConfigurationAssembly (entry assembly by default), then
  Landscape. An omitted Orientation differs from an explicitly assigned
  Landscape. PlayTestOrientationAttribute is runner-neutral metadata; a fixture
  hook must resolve executing row traits before method attributes and apply
  the result.

  Theme: explicit PlayTestOptions.Theme, --theme, CODEBRIX_PLAYTEST_THEME,
  CodeBrixPlayTestPreferredTheme project metadata in ConfigurationAssembly,
  then Light. Values are Light or Dark, case-insensitive. VirtualSystemTheme
  implements ISystemThemeHelperExtension and is registered before app
  construction; refresh the framework theme cache then. This simulates an OS
  preference, including UISettings system colors, while still allowing
  explicit app theme choices. PlayTestApplication.SystemTheme reports the
  launch preference, which stays fixed for the process and ignores the real
  desktop. Headless rendering and preview share the resulting pixels; SDL has
  no theme logic.

  SlowMo: explicit PlayTestOptions.SlowMo (including zero), then
  CODEBRIX_PLAYTEST_SLOWMO, then 250 milliseconds headed or zero headless. The
  default uses the final Headless option, not just the environment. Keep an
  omitted SlowMo distinct from an explicit zero. Resolve and validate once at
  launch, then store the resolved delay in the application's options snapshot.
  Environment values use invariant culture and must be finite and
  non-negative; unset/empty means default. Fixtures should leave SlowMo unset
  instead of substituting zero.

--- Preview process ---

PreviewProgram is the executable entry point in this assembly; PreviewConnection
launches it with the consumer's deps/runtimeconfig so native NuGet assets
resolve on every RID. SDL requires the process main thread on macOS. Frames
cross a bounded queue and the stdin pipe, each prefixed by two little-endian
Int32 dimensions (both orientations have the same byte count). Input never
crosses back. The SDL window keeps the launch preference's proportions and
letterboxes opposite-orientation frames; resize textures/logical presentation
on the SDL main thread, and never resize the native window in response to test
orientation. A truncated or malformed frame must fail the headed run.

--- Window, launcher ---

VirtualWindow reports SupportsClosingCancellation = true, like the desktop
heads, so AppWindow.Closing and Window.Closed handlers can keep the window
open. RequestCloseAsync follows the X11/Wayland close-button path (RaiseClosing;
the framework runs Closing/Closed and hides the window); an accepted close
never stops the process or the application. MinimizeAsync/RestoreAsync follow
the X11/Wayland reports: Deactivated + IsVisible false, then IsVisible true +
CodeActivated (the Win32/WPF heads differ: WPF also raises Entered/
LeavingBackground, Win32 raises Suspending on close - PlayTest does not).
SetContentAsync calls ShowForNextTest so a closed or minimized window never
leaks into the next test. Note the framework itself raises
Window.VisibilityChanged(false) twice for an accepted close on single-window
heads (BaseWindowImplementation.Close, then Shutdown's Hide()).
PlayTestLauncher registers ILauncherExtension before app construction (the
desktop heads each register one); without it Launcher falls back to
Process.Start. It records URIs and returns configurable results; it must
never start a process.

--- Input model ---

VirtualInput always uses Control-based shortcuts. VirtualHost disables
FeatureConfiguration.TextBox.UsePlatformKeyboardShortcuts before app
construction so text editing also follows that model on macOS. Keep this
setting in feature configuration: touching TextBox itself before dispatcher
setup initializes brushes too early. Normal desktop hosts retain their native
keyboard conventions.

Held keys: Keyboard.DownAsync/UpAsync and PressAsync's Delay exist for code
that samples key state once per frame or engine cycle (the GameEngine keyboard
adapter defers releases by one dispatcher pass and InputActionMap samples
IsDown per cycle, so a same-step down/up is invisible to it). Keyboard keeps
the held set on the UI thread (HeldKeys); SetContentAsync releases it before
removing the old page and DisposeAsync releases it before stopping. No auto
repeat. Locator.PressAsync/PressSequentiallyAsync focus Controls and any
element with IsTabStop (game surfaces).

--- Locator engine and actions ---

Application tests must assert actual outcomes. Preserve lazy locator
resolution, strictness, bounded retry, hit testing, and dispatcher confinement
when extending the API. Every locator factory filters with VisualTree.Visible
unless IncludeHidden is set; Visible also rejects cells an ItemsRepeater parks
at ClearedElementsArrangePosition (-10000, -10000). Single() marks its
strict-mode PlayTestException (Locator.StrictModeKey in Exception.Data);
RetryAsync keeps retrying such failures and reports the last one at the
timeout, while one-shot reads report it at once. A ContentDialog's text is the
text of its whole template (title, content, buttons), whatever its Content.
SelectOptionAsync (SelectorOptions.cs) reads options from Selector.Items, not
from realized containers, and sets SelectedIndex / SelectedItems so the
control raises its own SelectionChanged; it never opens a drop-down. Do not replace clicks with direct command invocation.
Use a serialized fixture and reset page/application state between tests. Test
both timeout and successful paths when changing the locator engine.
Check/Uncheck dispatch pointer input, including to a ToggleSwitch template
thumb, and verify the outcome; never set IsChecked/IsOn directly.
Bring-into-view supports attached controls, not unrealized virtualized items.

Desktop control contracts: GetByType<T> keeps add-in types in the application,
not in PlayTest's dependency graph. Fill/value assertions fall back to the
standard IValueProvider; checked actions fall back to IToggleProvider.
AdvancedTextEdit owns its peer and focus forwarding. Before typing, preserve
existing focus in a composite control's subtree; refocusing its outer control
can defer forwarding and lose input. Prefer the automation peer's SetFocus for a
new focus target. TypeAsync/PressSequentiallyAsync dispatch actual character
key events, including Enter/Tab, rather than replacing Document.Text or calling
editor handlers.

Menu names use their labels without template arrows/check glyphs. Menubar,
Menuitem (including submenus), Menuitemcheckbox, Menu and Toolbar are
supported. Toggle menu items may leave the visible tree after a check; verify
that item's result rather than waiting for its flyout to reopen. Hover,
positioned/multi/right clicks and stepped drags retain strictness, bounds
checks and hit testing. Always release a drag's pointer on failure. One outer
recording scope represents each logical action.

--- File pickers ---

PlayTestFilePickers registers the framework folder/open/save extensions before
app construction. Responses are application-owned FIFO queues, never native
dialogs. Null is explicit cancellation; missing responses fail. Existing save
files must not be truncated. FilePickers.Clear is the fixture's responsibility
on reset.

--- WebView ---

Linux WPE, Windows Edge WebView2 and macOS WKWebView work through the app's own
WebView add-in; PlayTest adds no WebView package or native browser dependency.
On Windows, PlayTest's STA dispatcher also pumps native messages for WebView2's
COM callbacks. The optional add-in supplies an offscreen composition controller
and captures its frames into Skia; pointer and keyboard input go through the
browser input APIs. Browser profiles live below TestResults/PlayTest/WebView2,
per process. The head flows packaged ICU assets/data for Windows/macOS text
initialization. On macOS the optional add-in launches an AppKit/WKWebView helper
on its process main thread, with a nonpersistent browser data store and no
visible native window. Navigation decisions, script results and PNG frames
travel through redirected pipes; callbacks and presentation return to the XAML
dispatcher. Input uses native NSEvent dispatch, not DOM actions. Page unload
closes that helper. OpenGL controls need the separate OpenGL provider package
(see "OpenGL provider" below). Validate each OS on its own host before claiming
runtime support.

--- Screenshots ---

Screenshots.cs crops frames (whole pixels, clipped to the screen), encodes
PNGs and implements stable capture (two consecutive identical captures in the
same run, bounded by the timeout) for Page.ScreenshotAsync and
Locator.ScreenshotAsync.

--- Screenshot recording ---

--screenshotfolder claims an existing empty folder only at execution, never
during option validation/discovery. Record PNGs from the virtual Skia surface,
not the desktop preview. buildTransitive/CodeBrix.PlayTest.Xunit.cs is
source-compiled only into supported reflection-based xUnit consumers; the
assembly fixture lifecycle records identity/outcome, and BeforeAfter hooks
capture after setup and before teardown. No runtime xUnit dependency and no
consumer test edits. Other runners reject recording until they have equivalent
lifecycle adapters.

Recording/PlayTestRecording.cs owns a versioned JSON index,
namespace/class/method/theory-row paths, the exclusive folder claim, atomic
index replacement, and serialized PNG writes. Keep every path under the claimed
folder, sanitize for Windows too, and never clear or overwrite user artifacts.
Case numbers are per-method execution order, not discovery order. Keep
IDs/arguments in the index. Missing UI and skipped tests must not get
fabricated images. Source locations use PDB-backed stack frames and may be
null. Local timestamps include offsets.

UI operations use a nested async scope so only the outer API boundary records;
retry probes and internal Evaluate calls must not multiply screenshots.
Preserve the no-recording fast path, caller location before awaiting, final
capture on failure, and capture before application disposal. Tests must remain
serialized.

--- Tests ---

  - src/Platform.UI.Runtime.Skia.PlayTest.Tests (host-free, xunit.v3 under
    MTP, SilverAssertions; in the /Tests/ folder of all three solutions):
    preference precedence for headless/theme/orientation/slowmo, the
    command-line adapter (buildTransitive/CodeBrix.PlayTest.TestingPlatform.cs
    is LINKED into the project, compiled with nullable enabled and
    CODEBRIX_PLAYTEST_XUNIT), orientation parsing and PlayTestOrientation
    resolution, recording-folder validation and path sanitizing, the preview
    frame format, text normalization, the key-name table and held-key order,
    picker failure queues, the recording launcher, option matching for
    SelectOptionAsync, strict-mode marking and screenshot cropping. It
    launches no application. The suite is serialized and every test isolates and restores
    the CLI AppContext keys as well as the PlayTest environment variables
    (PlayTestSettingsScope). Put new configuration-only cases HERE.
  - samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests: the
    control/picker contract tests that need the running application, next to
    the dedicated six-head demo application, including the OpenGL page (it
    registers CodeBrix.Platform.PlayTest.OpenGL); PlayTestDemo.NoOpenGL.PlayTests
    beside it launches the same application with OpenGL Unavailable. They exercise that application's
    actual shared XAML and build the framework from source. Put general control
    demonstrations there, not into unrelated sample applications.
  - JustBetweenUs and the additional CodeBrix.Samples suites exercise their own
    application screens; the latter share PlayTestSupport in that repository.
    They use SilverAssertions and existing app service interfaces for offline
    data.
  - The standalone build/test-scripts/PlayTestRecordingProbe contains an
    intentional failure; run it through build/test-scripts/playtest-recording.py,
    which checks that failure, PNG/index integrity, hierarchy, source lines,
    precedence and rejected folders. It is not part of the normal demo suite.

--- OpenGL provider (CodeBrix.Platform.PlayTest.OpenGL) ---

The essentials:

  - The HEAD owns a small public, BCL-only seam (OpenGL/PlayTestOpenGL.cs):
    IPlayTestOpenGLProvider, IPlayTestOpenGLContext, PlayTestOpenGLProviders,
    the launch option PlayTestOptions.OpenGL (PlayTestOpenGL Available |
    Unavailable) and PlayTestApplication.OpenGL (PlayTestOpenGLInfo). It takes
    no dependency on CodeBrix.Platform.OpenGL and needs no new
    InternalsVisibleTo. OpenGL/PlayTestOpenGLSetup.cs holds the decisions
    (Plan, Probe, WrapperFactory) apart from the host, and the internal
    PlayTestNativeOpenGLWrapper adapts a provider context to the framework's
    internal INativeOpenGLWrapper, declaring its flavour through
    INativeOpenGLWrapper.UsesGles (a default interface member, null on every
    other head; OffscreenGLContext prefers it over Graphics3DGLHeadDetection).
  - VirtualHost.InitializeApplication, on the UI thread before Application.Start:
    Probe (a registered provider creates one context, glGetString fills the
    info; a failure fails the launch with the provider's reason), then
    ApiExtensibility.Register<XamlRoot>(INativeOpenGLWrapper) unless the launch
    opted out. VirtualHost.Dispose disposes the provider after both threads
    have stopped; an opted-out launch never touches it.
  - The "no provider registered" failure: the registered factory throws, but
    GLCanvasElement and SkiaGLCanvasElement catch every exception and report
    "initialization failed", so the factory ALSO records the PlayTestException
    as the host's failure (VirtualHost.FailConfiguration). Every following
    PlayTest call, including the one during which the element loaded, then
    throws it with its own message (ThrowIfFailed keeps a configuration
    failure's message instead of "The application or renderer failed.").
    Measured: a demo test without Register() fails in the ClickAsync that
    opened the OpenGL page, naming the package and Register().
  - The PACKAGE, src/Platform.UI.Runtime.Skia.PlayTest.OpenGL (assembly and
    namespace CodeBrix.Platform.PlayTest.OpenGL), implements the seam:
    Egl/ (EglNative: "libEGL" resolved to libEGL.so.1 on Linux and libEGL.dylib
    on macOS; EglProvider: ONE display per process, terminated only by the
    provider's Dispose - Mesa and ANGLE return the same EGLDisplay for the
    same platform and eglTerminate destroys every context on it; contexts are
    ES 3 (then 2) with a 1x1 pbuffer or none), Providers/LinuxEglProvider
    (eglGetPlatformDisplay(EGL_PLATFORM_SURFACELESS_MESA)), MacOSAngleProvider
    (ANGLE's EGL_DEFAULT_DISPLAY), WindowsWglProvider (a hidden window per
    context, GetDC -> ChoosePixelFormat -> SetPixelFormat -> wglCreateContext,
    proc addresses from opengl32.dll exports then wglGetProcAddress, egl*
    names answered with 0), and ContextCheck (the GL binding from
    CodeBrix.Platform.OpenGL: GL_VERSION 3.0 or later unless ANGLE, and a
    clear-and-read-back of a small FBO). CodeBrixPlayTestOpenGL.Register()
    picks the provider with OperatingSystem.Is*.
  - Packaging: the head is a PrivateAssets ProjectReference (compile only);
    Graphics3DGL is the one package dependency (its PackageId is the published
    one, as for VideoPlayer). It packs its OWN README.md and AGENT-README.txt.
    It is in the _CsprojPackage list right after the head. The local
    preview-feed script build/pack-playtest-preview.py does not pack it (pack
    it with the command below), and build/test-scripts/pack-linux-local-feed.sh
    packs neither the head nor this package.
  - GPU readback needs nothing from the head: both elements read their frame
    back into a WriteableBitmap on the UI thread, which the CPU renderer
    composites. A single CaptureAsync (two passes with a UI barrier) already
    shows the GL frame an action caused (measured with non-Stable captures).

Build, pack and test it (Linux):

    dotnet build src/Platform.UI.Runtime.Skia.PlayTest.OpenGL/Platform.UI.Runtime.Skia.PlayTest.OpenGL.csproj -c Release
    dotnet pack src/Platform.UI.Runtime.Skia.PlayTest.OpenGL/Platform.UI.Runtime.Skia.PlayTest.OpenGL.csproj \
        -c Release -p:PackageVersion=<version> --output <folder>
    dotnet test --project src/Platform.UI.Runtime.Skia.PlayTest.Tests/Platform.UI.Runtime.Skia.PlayTest.Tests.csproj -c Release
    dotnet test --project samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests/PlayTestDemo.PlayTests.csproj -c Release
    dotnet test --project samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.NoOpenGL.PlayTests/PlayTestDemo.NoOpenGL.PlayTests.csproj -c Release

The project is plain net10.0, so all three providers COMPILE on every OS; only
the Linux provider has RUN (Mesa surfaceless EGL, on this repository's Linux
machine). Still to verify on the maintainer's machines:

  Windows (interactive desktop, GPU driver): the PlayTestDemo suites, default
  and --theme=dark --orientation=portrait. Expect PlayTestApplication.OpenGL to
  report the WGL provider, the vendor's renderer, IsGles false. Check that the
  OpenGL tests pass (the "#version 300 es" shaders compile on desktop OpenGL
  through ARB_ES3_compatibility, as the UIReqs fixtures assume) and that no
  hidden window lingers after the run. On Windows on ARM without the
  Compatibility Pack the launch must fail naming GDI Generic / OpenGL 1.1.
  macOS (Metal): the same two suites. First the U1 probe - whether SkiaSharp's
  macOS binary builds a GRContext on ANGLE-GLES (source comments say it
  cannot): run PlayTestDemo.PlayTests with --filter-method "*GPU_Skia*". If
  SkiaGLStatus reads "GPU Skia unavailable" while GLStatus reads
  "OpenGL: initialized", U1 is answered "no": SkiaGLCanvasElement then fails on
  the real macOS head too, which is a Graphics3DGL/macOS-head matter (stop and
  report; do not work round it in PlayTest). Also confirm ANGLE initializes on
  the PlayTest UI thread (a background thread, no AppKit) - U4.

--- Third-party code ---

AriaRole.cs retains its original MIT notice (Microsoft Playwright for .NET) and
is the only PlayTest file carrying a //was previously: marker. Update the root
THIRD-PARTY-NOTICES.txt whenever adapting additional upstream code.

PLAYTEST DESKTOP CONTROL AND EDITOR COVERAGE
==========================================
The PlayTestDemo shared UI now includes DesktopControlsView, accessible through
"Try desktop controls" in every head. DesktopControlTests exercises typed/role
locators, composite editor focus, IValueProvider fill/undo/read-only, empty
completion, toolbar commands/toggles/overflow/split buttons, nested menus,
right/double clicks, dismissed toggle-menu state, element tab headers and invalid
pointer positions. The demo application's add-in project references do not add
dependencies to the PlayTest package.

    dotnet test --project samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests/PlayTestDemo.PlayTests.csproj -c Release
    dotnet test --project samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests/PlayTestDemo.PlayTests.csproj -c Release --nonheadless

Fresco.Brix in CodeBrix.Samples.Gpl3 provides application-level coverage of nested
TriPaneView drags, split editors, document tabs, menus/dialogs and engraving. Its
test-only head consumes a local PlayTest/AdvancedTextEdit set; build both with:

    python3 build/pack-playtest-preview.py --version <fresh-prerelease> --with-editor

The optional switch adds TextLayout and AdvancedTextEdit to the local feed.
Nothing is published. Existing app heads retain their own package versions.

Intel macOS validation (2026-09-30): the full PlayTestDemo suite passed headless
and headed dark/portrait, including protected editor sections and disabled
drags. The desktop-control cases have not yet been executed on Windows, Linux or
Apple Silicon. Fresco's package-consumer suite passed in full headlessly and in a
dark portrait preview against a local preview feed, with every automatic PNG
validated. Its README records the application fixes, final wizard checks and
remaining limits. (The configuration-only cases that used to live in the demo
now run in src/Platform.UI.Runtime.Skia.PlayTest.Tests.)
