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
      Platform.UI                          the framework (Platform.UI.Skia.csproj
                                           and the .Reference variant)
      Platform.UWP                         the WinRT / Windows.* API surface
      Platform.Foundation, .Foundation.Logging
      Platform.UI.Composition, Platform.UI.Dispatching
      Platform.UI.FluentTheme, .v1, .v2    control styles
      Platform.UI.Toolkit                  ElevatedView, converters, DiagnosticsOverlay,
                                           StorageFileHelper (folded into the core package)
      Platform.UI.Adapter.Microsoft.Extensions.Logging   (folded into the core package)
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

The samples under samples/CodeBrixPlatform consume the framework FROM SOURCE
via ProjectReference (each head references its head csproj under src/ plus the
SourceGenerators project). Because buildTransitive targets do not flow across
a ProjectReference, the sample heads carry the runtime-replace logic
themselves; the macOS sample heads import samples/CodeBrixPlatform/
CodeBrix.MacOSHead.targets for it. A new macOS sample head needs only that one
import line.

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
built with xcodebuild through build.sh (chmod +x it). The csproj only enables
the native step on Apple Silicon; full Xcode is required there.

Wayland protocol bindings under src/Platform.UI.Runtime.Skia.Wayland/
Wayland_Bindings/ are GENERATED and committed; regenerate them with
tools/WaylandBindingsGenerator (see EXTRAS-README.txt), never by hand.

TESTING
=======
Test projects (each is a normal `dotnet test` target; the runner is
Microsoft.Testing.Platform per global.json):

  Framework:
    src/Platform.UI/Platform.UI.Tests.csproj
    src/Platform.UI.Tests/Platform.UI.Unit.Tests.csproj (+ Tests.ViewLibrary,
        Tests.ViewLibraryProps helper projects)
    src/Platform.UWP/Platform.Tests.csproj
    src/Platform.UI.RuntimeTests/Platform.UI.RuntimeTests.Skia.csproj
        (runtime tests hosted in a real Skia head; the .Windows.csproj variant
        is Windows-only and excluded from the Linux solution)
    src/Platform.Foundation/Platform.Foundation.Tests.csproj
    src/Platform.UI.Composition/Platform.UI.Composition.Tests.csproj
    src/Platform.UI.Dispatching/Platform.UI.Dispatching.Tests.csproj
    src/Platform.UI.FluentTheme{,.v1,.v2}/*.Tests.csproj
    src/Platform.UI.Toolkit/Platform.UI.Toolkit.Tests.csproj and
    src/Platform.UI.Toolkit.Tests/Platform.UI.Toolkit.Unit.Tests.csproj
    src/Platform.Analyzers.Tests/Platform.Analyzers.Tests.csproj
    src/SourceGenerators/Platform.UI.SourceGenerators.Tests/
    src/SourceGenerators/XamlGenerationTests/
  Add-ins:
    src/AddIns/Platform.AppSettings.Tests, Platform.UI.AdvancedTextEdit.Tests,
    Platform.UI.CommandBar.Tests, Platform.UI.FlexPanel.Tests,
    Platform.UI.PlotterView.Tests, Platform.UI.TerminalView.Tests,
    Platform.UI.TextLayout.Tests, Platform.UI.VideoPlayer.Tests,
    CodeBrix.Platform.SkiaSharp.Views.Tests (all *.Unit.Tests.csproj),
    src/AddIns/Platform.UI.Lottie/Platform.UI.Lottie.Tests.csproj

    CodeBrix.Platform.SkiaSharp.Views.Tests is the guard for a SkiaSharp version
    bump: it pins the managed/native agreement, the add-in version's tie to
    $(SkiaSharpVersion), and the SKXamlCanvas paint-and-present path down to the
    presented pixels. Run it after changing $(SkiaSharpVersion) in
    src/Directory.Build.targets, before launching any head.

CI scripts under build/test-scripts (linux-skia-runtime-tests.sh,
macos-skia-runtime-tests.sh, android/ios UI-test scripts inherited from
upstream, run-devserver-cli-tests.ps1) drive the runtime tests with
UITEST_RUNTIME_TEST_GROUP / CODEBRIX_TESTS_FAILED_LIST / TEST_RESULTS_FILE
environment variables and expect BUILD_SOURCESDIRECTORY to be set.

Several add-in demos double as scripted smoke tests on the X11 head (an
environment variable makes the app run a self-test and exit PASS/FAIL); each
add-in's AGENT-README and EXTRAS-README.txt name the variable. CommandBarDemo's
is COMMANDBARDEMO_SELFTEST=1, with COMMANDBARDEMO_RESULTS=<path> writing the
PASS/FAIL lines to a file as well; it drives the tool bars through xdotool and
window captures, so run it on the X11 head with DISPLAY set:

    cd samples/CodeBrixPlatform/CommandBarDemo/CommandBarDemo.LinuxX11
    DISPLAY=:0 COMMANDBARDEMO_SELFTEST=1 \
        dotnet bin/Release/net10.0/CommandBarDemo.LinuxX11.dll

Every window it opens is titled "CommandBar Demo", so the self-test elects its
OWN window by probing which one its key presses reach, never by title; a second
demo left running on the same display does not disturb it.

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
                                       "Simple" helpers come from the folded Toolkit)
    Platform.WinUI.Graphics2DSK.nuspec, Platform.WinUI.Graphics3DGL.nuspec,
    Platform.WinUI.Lottie.nuspec, Platform.WinUI.Svg.nuspec,
    CodeBrix.Platform.SkiaSharp.Views.nuspec
    The nuspec names a dependency-version TOKEN, never a literal version.
  - CSPROJ-DRIVEN (`dotnet pack <csproj> -p:PackageVersion=$(BuildVersion)`):
    Platform.UI.Runtime.Skia and the seven heads (including the Emulated head),
    and the add-ins WebView, AudioPlayer, VideoPlayer, MediaPlayer, TextLayout,
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

The macOS head package contains a native dylib that can ONLY be built on Apple
Silicon, so it is NOT produced by the Windows run above (a macOS package built
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

PREREQUISITES on the Mac: full Xcode installed (the native build uses
xcodebuild; the csproj only enables the native step on Apple Silicon) and the
native build script src/Platform.UI.Runtime.Skia.MacOS/PlatformNativeMac/build.sh
must be executable (chmod +x). A correctly built macOS package is a universal
binary and runs on both Apple Silicon and Intel Macs.

VERIFY THE macOS PACKAGE BEFORE UPLOADING. A managed-only package (no native
dylib) packs WITHOUT error on any machine where the native step is skipped
(e.g. not Apple Silicon, or BuildNativeMac=false) and is useless at runtime.
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
  - Root doc filenames use dashes (CODEBRIX-PLATFORM-README.md,
    NOT-IMPLEMENTED.md, THIRD-PARTY-NOTICES.txt).

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
