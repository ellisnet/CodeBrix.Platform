================================================================================
EXTRAS-README: CodeBrix.Platform
Samples, tools and other content in this repository that is not part of a NuGet package
================================================================================

This file inventories everything in the repository that is NOT a packaged
library or a library test project: the sample applications, the developer
tools, the build/CI scripts and the application template.

  - Consumer documentation for the packages: the AGENT-README.txt files
    (README-INDEX.txt maps them).
  - Building, testing, packaging and publishing the repository itself:
    MAINTAINER-README.txt.

Nothing described in this file is packed into a NuGet package.

================================================================================

HOW THE SAMPLE APPLICATIONS ARE STRUCTURED
==========================================
Every sample under samples/CodeBrixPlatform is a complete CodeBrix.Platform
application in the canonical shape:

    <Demo>.Core          a net10.0 library: view models, helpers, assets
    <Demo>.UI            a SHARED project (.shproj/.projitems): App.xaml and
                         the XAML pages, compiled into each head
    <Demo>.LinuxX11      one Exe head per platform
    <Demo>.LinuxWayland
    <Demo>.LinuxFrameBuffer
    <Demo>.MacOS
    <Demo>.Win32Skia
    <Demo>.WinWpfSkia

Some demos keep those projects directly under the demo folder; the newer ones
put them under a src/ sub-folder. The run commands below spell out the correct
path for each demo.

Solutions are per build OS: <Demo>.Linux.slnx, <Demo>.MacOS.slnx and
<Demo>.Windows.slnx, each holding only the projects that build on that OS.

CONSUMED FROM SOURCE, NOT FROM PACKAGES
---------------------------------------
With ONE exception (EmulateFrameBufferDemo, below), the samples reference the
framework and add-ins through ProjectReference into src/ rather than through
PackageReference. That is the opposite of what a real application does: copy
the samples' project STRUCTURE and their XAML/C#, not their reference lines.

Two consequences worth knowing:

  - Building a sample builds the framework, so build the platform solution for
    your OS in the configuration you want first.
  - buildTransitive props/targets do not flow across a ProjectReference, so the
    sample heads carry the runtime-replace logic themselves. The macOS sample
    heads get it by importing one shared file,
    samples/CodeBrixPlatform/CodeBrix.MacOSHead.targets; a new macOS sample
    head needs only that single Import line. That file is gated on the project
    name ending in ".MacOS", so it is inert anywhere else.

RUNNING A HEAD
--------------
    dotnet run --project samples/CodeBrixPlatform/<Demo>/<Demo>.LinuxX11
    dotnet run --project samples/CodeBrixPlatform/<Demo>/src/<Demo>.LinuxX11   (src/ layout)

Head requirements are the platform's own: the X11 head needs DISPLAY, the
Wayland head needs a running compositor, the FrameBuffer head needs a real
framebuffer device (or the CodeBrix.Develop emulator), the WPF and Win32 heads
need Windows, and the macOS head needs macOS.

================================================================================

SAMPLE APPLICATIONS (samples/CodeBrixPlatform/)
===============================================

JustBetweenUs
-------------
    samples/CodeBrixPlatform/JustBetweenUs
    Six heads + JustBetweenUs.Encryption + Tests/JustBetweenUs.Encryption.Tests

The in-repo copy of the reference application, and the closest thing the
repository has to a "what a real CodeBrix.Platform app looks like" example. A
text encrypt/decrypt page (enter text, enter a key, Encrypt or Decrypt) bound
to a view model, with the encryption itself behind an injected
IEncryptionService in a separate library.

Demonstrates: the .Core + .UI + heads layout; dependency injection and
view-model binding; a Shared/ folder of view models and assets linked into
.Core rather than copied (the src-platforms sample has a parallel Shared/
folder of its own); and four add-ins at once - Lottie
(Shared/Assets/star_icon.json), SVG (Shared/Assets/*.svg), SkiaSharp.Views and
Graphics2DSK. The assets are embedded resources, not content files.

Run:
    dotnet run --project samples/CodeBrixPlatform/JustBetweenUs/JustBetweenUs.LinuxX11
Test the encryption library:
    dotnet test samples/CodeBrixPlatform/JustBetweenUs/Tests/JustBetweenUs.Encryption.Tests

EmulateFrameBufferDemo
----------------------
    samples/CodeBrixPlatform/EmulateFrameBufferDemo
    Linux heads only (X11, Wayland, FrameBuffer); one EmulateFrameBufferDemo.slnx

THE ONE SAMPLE THAT CONSUMES THE PLATFORM FROM NUGET PACKAGES, exactly as a
real application does. That is the point of it: it is the end-to-end test rig
for the CodeBrix.Develop frame-buffer emulator. Its .LinuxFrameBuffer head
holds a real PackageReference to the FrameBuffer head package, and running it
inside the IDE silently swaps ONLY that reference for the Emulated head
package at build time - the csproj is never modified.

The page has two panes: an OpenGL 3D model viewer (Spin/Pause, Reset Model)
and an offscreen WebView (address bar, "Wikipedia Home"), plus a sketch pad
(Draw here, Brush Colour, Undo Stroke, Clear Drawing).

Demonstrates: package-shaped consumption; the FrameBuffer builder's
Orientation(..., isPreferredOrientation: true) and AutoRotationEnabled
options; UseDirectSkiaCanvasMode(); the Graphics3DGL, WebView and
SkiaSharp.Views add-ins together on one page.

Run (X11):
    dotnet run --project samples/CodeBrixPlatform/EmulateFrameBufferDemo/src/EmulateFrameBufferDemo.LinuxX11
The WebView pane needs the system WPE WebKit runtime on Linux:
    sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1

ParityDemo
----------
    samples/CodeBrixPlatform/ParityDemo
    Two Linux heads only (X11, Wayland); ParityDemo.Linux.slnx

The X11-versus-Wayland behavior harness. One page exercises popups and
flyouts (ComboBox drop-down, MenuFlyout with a sub-menu, ContentDialog,
ToolTip), clipboard (copy rich content, paste and inspect the offered
formats), drag-and-drop (a drop zone for files and text), and window chrome
(Activate(), Maximize/Restore, window-state query, ExtendContentIntoTitleBar),
with a live diagnostics log. Key presses log their numeric VirtualKey; F2
toggles a relative-mouse session and F4 toggles cursor hiding.

It also runs unattended. Set exactly one of these to "1" before launching and
the app runs that suite, logs PASS/FAIL lines prefixed "PARITY|" to the
console, and exits with the failure count:

    PARITYDEMO_SELFTEST      popups, flyouts and dialogs
    PARITYDEMO_CLIPTEST      clipboard
    PARITYDEMO_CHROMETEST    window chrome
    PARITYDEMO_TOUCHTEST     touch

Setting PARITYDEMO_RESULTS=<path> additionally writes the result lines to that
file.

Run:
    dotnet run --project samples/CodeBrixPlatform/ParityDemo/ParityDemo.LinuxX11
    dotnet run --project samples/CodeBrixPlatform/ParityDemo/ParityDemo.LinuxWayland

FileFolderDialogDemo
--------------------
    samples/CodeBrixPlatform/FileFolderDialogDemo    six heads

The file and folder pickers on every head, with a status line reporting what
came back. Buttons: Select File (any type), Select File (.txt / .md only),
Select Multiple Files, Select Folder, and Save File (writes a small .txt
file).

Demonstrates: Windows.Storage.Pickers - FileOpenPicker, FileSavePicker and
FolderPicker - with SuggestedStartLocation, file-type filters, multi-select,
and writing to a picked file.

Run:
    dotnet run --project samples/CodeBrixPlatform/FileFolderDialogDemo/FileFolderDialogDemo.LinuxX11

AdvancedTextEditDemo
--------------------
    samples/CodeBrixPlatform/AdvancedTextEditDemo    six heads (src/ layout)
    Add-in: CodeBrix.Platform.AdvancedTextEdit

A working code editor: Open and Save, Undo/Redo, Cut/Copy/Paste, a syntax
highlighting selector (the highlighting definitions come from
HighlightingManager.Instance), and toggles for line numbers, word wrap and
end-of-line marks, over a status line showing the current file and caret
state. A property pane lists the properties of whichever object the
Editor / TextArea / Options selector picks.

Run:
    dotnet run --project samples/CodeBrixPlatform/AdvancedTextEditDemo/src/AdvancedTextEditDemo.LinuxX11

AudioPlayerDemo
---------------
    samples/CodeBrixPlatform/AudioPlayerDemo    six heads
    Add-in: CodeBrix.Platform.AudioPlayer

Three panes: a song player (two sample songs, one of five formats - WAV, MP3,
OGG/Vorbis, FLAC, Opus - and a source selector for ms-appx:/// versus
embedded://), sound effects (Click, Chime), and a MIDI pane that synthesizes a
MIDI file through an SFZ instrument. Transport (Play/Pause/Stop, Loop, Jump to
1:00, Volume, Tempo) is bound to Position/Duration/Speed/ActiveVoiceCount on
the player elements.

Self-test: AUDIOPLAYERDEMO_SELFTEST=1 makes the app exercise the whole player
once loaded, print "APD-SELFTEST: PASS|FAIL <step>" lines and exit with the
failure count - the scripted X11 smoke verification.

Media assets come from samples/assets (see SHARED SAMPLE ASSETS below).

Run:
    dotnet run --project samples/CodeBrixPlatform/AudioPlayerDemo/AudioPlayerDemo.LinuxX11

FlexPanelDemo
-------------
    samples/CodeBrixPlatform/FlexPanelDemo    six heads
    Add-in: CodeBrix.Platform.FlexPanel

A live flexbox playground: Direction, JustifyContent, AlignItems,
AlignContent and Wrap drop-downs re-lay-out eight children as you change them.
Individual children carry the attached properties FlexPanel.Order,
FlexPanel.Grow, FlexPanel.AlignSelf and FlexPanel.Basis; one child is a nested
FlexPanel and one can be collapsed to show how Visibility affects the line.
Below the playground are two worked examples: a Grow/Basis row and a
navigation bar (JustifyContent SpaceBetween + AlignItems Center).

Run:
    dotnet run --project samples/CodeBrixPlatform/FlexPanelDemo/FlexPanelDemo.LinuxX11

MediaPlayerDemo
---------------
    samples/CodeBrixPlatform/MediaPlayerDemo    six heads
    Add-in: CodeBrix.Platform.MediaPlayer

A URL box, a Load button and a Stretch selector (None, Uniform, UniformToFill,
Fill) over a MediaPlayerElement with transport controls. It sets
Player.Source = MediaSource.CreateFromUri(uri) and swaps Player.Stretch from
the drop-down.

Native runtime: the Linux heads need the system libvlc
(sudo apt install libvlc5 vlc-plugin-base); the Windows heads carry a
VideoLAN.LibVLC.Windows PackageReference that lays the native runtime into the
app output. The macOS head uses the platform's built-in AVFoundation support.

Run:
    dotnet run --project samples/CodeBrixPlatform/MediaPlayerDemo/MediaPlayerDemo.LinuxX11

PlotterViewDemo
---------------
    samples/CodeBrixPlatform/PlotterViewDemo    six heads (src/ layout)
    Add-in: CodeBrix.Platform.PlotterView

A chart gallery driven by a drop-down: live streaming signal, function series,
bar chart, scatter, heat map and pie, plus a Reset view button. The hint line
documents the interaction model it demonstrates - right-drag pans, wheel
zooms, middle-drag draws a zoom box, left-click tracks a point, double-middle-
click (or A / Home) resets, one finger pans and two fingers pinch on touch.

Run:
    dotnet run --project samples/CodeBrixPlatform/PlotterViewDemo/src/PlotterViewDemo.LinuxX11

TerminalViewDemo
----------------
    samples/CodeBrixPlatform/TerminalViewDemo    six heads (src/ layout)
    Add-in: CodeBrix.Platform.TerminalView

A terminal emulator view that echoes what you type locally, with a "Replay
showcase" button that plays an ANSI / SGR feature tour, a colour-scheme
selector (Default / Light), a Reset terminal button and a live grid-size
readout. Selection plus right-click or Ctrl+Shift+C / Ctrl+Shift+V does copy
and paste.

Run:
    dotnet run --project samples/CodeBrixPlatform/TerminalViewDemo/src/TerminalViewDemo.LinuxX11

WebViewDemo
-----------
    samples/CodeBrixPlatform/WebViewDemo    six heads
    Add-in: CodeBrix.Platform.WebView

An address box with Go / Back / Forward over a WebView2 control, plus download
handling: the page subscribes to CoreWebView2.DownloadStarting and reports the
resolved target path, showing where to accept the default, redirect via
args.ResultFilePath, or cancel.

Self-test: WEBVIEWDEMO_SELFTEST_DOWNLOAD_URL=<url> makes the app navigate
straight to that URL, log "WVD-SELFTEST:" lines for the download, and exit
when it completes.

The Linux heads need the system WPE WebKit runtime:
    sudo apt install libwpewebkit-2.0-1 libwpebackend-fdo-1.0-1 libwpe-1.0-1

Run:
    dotnet run --project samples/CodeBrixPlatform/WebViewDemo/WebViewDemo.LinuxX11

================================================================================

SAMPLE FOR THE MICROSOFT-FRAMEWORK TOOLKITS (samples/Platforms/)
================================================================
    samples/Platforms/JustBetweenUs
    JustBetweenUs.Windows.slnx

The same encrypt/decrypt application, built instead on Microsoft's own UI
frameworks with the src-platforms toolkits: a WinUI head
(JustBetweenUs.WinUI), a WPF head (JustBetweenUs.Wpf) and a .NET MAUI head
(Mobile/JustBetweenUs.Mobile), all sharing Shared/ViewModels and the
JustBetweenUs.Encryption library, with Tests/JustBetweenUs.Encryption.Tests
alongside.

The solution references the src-platforms toolkit projects directly, so it
demonstrates - and exercises - CodeBrix.Platform.WinUI, .WinUI.Skia,
.WinUI.Lottie, .WPF and .Mobile from source. It builds on Windows only.

    dotnet build samples/Platforms/JustBetweenUs/JustBetweenUs.Windows.slnx

================================================================================

SHARED SAMPLE ASSETS (samples/assets/)
======================================
Media used by the samples, referenced in place (linked into the consuming
project's output as Assets\..., not copied into each demo folder). Currently
only AudioPlayerDemo consumes them.

    sample_song_1.{wav,mp3,ogg,opus,flac}   the same short song in five
    sample_song_2.{wav,mp3,ogg,opus,flac}   formats, for the format selector
    debussy_Ste_Bergamesq_Clair.mid         the MIDI file the MIDI pane plays
    SplendidGrandPiano/                     the SFZ instrument it plays through

SplendidGrandPiano is a public-domain Akai Steinway sample set (4 velocity
layers, converted to FLAC and mapped to SFZ with ARIA extensions); see its own
README.md in that folder. It is large - it decodes to well over half a
gigabyte at load time, which is why the demo loads it in the background. An
.sfz instrument must be a real file on disk (it is loaded by path, not as an
embedded resource), so it is copied to the output rather than embedded.

================================================================================

DEVELOPER TOOLS (tools/)
========================
Neither tool is packed into any NuGet package.

WaylandBindingsGenerator
------------------------
    tools/WaylandBindingsGenerator

Generates the committed C# Wayland protocol bindings for the native Wayland
head (src/Platform.UI.Runtime.Skia.Wayland/Wayland_Bindings/). It is a frozen,
CodeBrix-owned fork of the MIT-licensed NWayland bindings generator, driven by
pinned copies of the MIT protocol XML from the freedesktop wayland and
wayland-protocols repositories under tools/WaylandBindingsGenerator/protocols.

Regenerate, then review and commit the diff under Wayland_Bindings/:

    dotnet run --project tools/WaylandBindingsGenerator/src/GeneratorRunner

Never hand-edit the generated bindings. PORTING-NOTES.txt in that folder holds
the upstream commit SHAs, the fork patch list, and how to add or upgrade a
protocol; LICENSE-NWayland.md holds the upstream MIT license text. Attribution
for the whole Wayland effort is in THIRD-PARTY-NOTICES.txt at the repo root.

ResourcesExtractor
------------------
    tools/ResourcesExtractor    (ResourcesExtractor.sln, Windows only)

A small WinUI utility that extracts the localized WinUI string resources from
a running WinUI application, given the localizedResource.h header from the
WinUI sources, and emits them per language. Its README.md explains the
MAKELANGID arithmetic used to turn a primary-language / sub-language pair into
the language ids the tool's Languages enum uses (for example en-US = 0x409).
Its output is what populates the framework's WinUI string resource tables; it
is run by hand, rarely, when those resources are refreshed.

================================================================================

IN-REPO BUILD AND VALIDATION TOOLS (src/)
=========================================
These are console projects that live alongside the libraries but are never
packed. Each is a net10.0 Exe.

    src/Platform.PackageDependencyValidator
        The package dependency gate. Guarantees a .nuspec can never state a
        dependency version that differs from the PackageReference version of
        the project(s) it packs; it emits the pack-time version tokens before
        packing (--emit-properties) and re-validates the produced .nupkg files
        afterwards (--package-dir). It never contacts a package feed and never
        judges whether a version is up to date - version SELECTION stays a
        maintainer decision. Driven automatically by the pack driver; see
        MAINTAINER-README.txt.
    src/Platform.ReferenceImplComparer
        Takes a produced .nupkg, unpacks it, and compares each reference
        assembly under lib/<targetframework> against every same-named runtime
        assembly under codebrix-platform-runtime/<targetframework>, member by
        member over the publicly accessible surface. It exits non-zero if a
        runtime assembly has drifted from the reference assembly that stands
        in for it at compile time.
    src/Platform.ResourceTrimmingValidator
        Checks that a built assembly does (or does not) still carry named
        embedded resources after trimming: -a <assembly>, -r <resource> to
        require one, plus an exclude list.
    src/Platform.XamlTrimmingValidator
        The same idea for XAML-driven types: --target-assembly <assembly>
        --hints-file <file>, where the hints file is the one under
        build/assets (MyAppXamlTrim-hints.txt).
    src/Platform.NUnitTransformTool
        Post-processes NUnit result XML for CI: "list-failed" writes the failed
        test list that a pipeline retry re-runs, "fail-empty" fails a run that
        produced no results at all.
    src/Platform.UWPSyncGenerator (+ .Reference)
        Regenerates the WinRT / Windows.* API surface: it deletes and rebuilds
        the Generated/ folders under Platform.UI, Platform.UWP,
        Platform.Foundation, Platform.UI.Composition and
        Platform.UI.Dispatching. Modes: "sync" (code), "doc"
        (the per-platform implementation tables) and "all".
    src/Platform.Docs.InlineTOCGenerator
        Generates the inline table-of-contents include files from a docs
        toc.yml. Inherited from the upstream project's documentation site; the
        docs tree it reads is not part of this repository.

================================================================================

BUILD, CI AND TEST SCRIPTS (build/)
===================================
The pack driver (build/CodeBrix.Platform.Build.csproj), the nuspecs under
build/nuget and the pack shim are documented in MAINTAINER-README.txt - they
produce the packages and are not "extras". What follows is the rest of build/.

build/test-scripts/
-------------------
Shell and PowerShell drivers for the runtime and UI test suites. The Linux and
macOS runtime-test scripts (linux-skia-runtime-tests.sh,
macos-skia-runtime-tests.sh) expect BUILD_SOURCESDIRECTORY and
SamplesAppArtifactPath to be set, run the test host under xvfb-run on Linux,
and use UITEST_RUNTIME_TEST_GROUP, CODEBRIX_TESTS_FAILED_LIST,
UITEST_RUNTIME_TESTS_FILTER and TEST_RESULTS_FILE to shard a run and to re-run
only the previous run's failures (through Platform.NUnitTransformTool).

Several scripts in this folder are inherited from the upstream project's CI
and target things this repository does not build: the Android and iOS UI-test
runners, the WebAssembly runners, the template tests, and the runtime-test
scripts' expectation of a "SamplesApp" host application that is not in this
repository. Treat them as historical unless you are reviving that pipeline.

build/ci/
---------
The Azure DevOps pipeline definitions (stages, per-platform test jobs,
setup/lint/spell-check jobs and shared YAML templates) plus two PowerShell
helpers, check-master-status.ps1 and determine-test-scope.ps1. Inherited from
the upstream project's pipeline; no GitHub Actions workflows exist in this
repository and none should be added.

build/gitpod/
-------------
A Gitpod container definition and helper scripts (build-skia.sh,
build-wasm.sh, serve-*.sh) from the upstream project's cloud dev environment.
Historical.

Other files in build/
---------------------
    copy-winui-styles.ps1        imports theme-resource XAML from a local
                                 checkout of the WinUI controls repository into
                                 src/Platform.UI/Themes/WinUI - edit the
                                 $winui_path placeholder before running
    Install-Tizen.ps1            installs the Tizen SDK/CLI on a CI agent
                                 (adapted from SkiaSharp's install-tizen.ps1)
    Install-WindowsSdkISO.ps1    installs a Windows 10 SDK, by build number,
                                 from its ISO on a CI agent
    assets/MyAppXamlTrim-hints.txt  the trimming-hints fixture used with the
                                 XAML trimming validator
    cSpell.json, .markdownlint.json, .commitsar.yml
                                 configuration for the spell-check, markdown
                                 lint and commit-message CI jobs

================================================================================

APPLICATION TEMPLATE (templates/)
=================================
    templates/TemplateApp.zip

The scaffold behind CodeBrix.Develop's "New CodeBrix.Platform Application".
It contains a complete six-head application named TemplateApp - TemplateApp.slnx
plus src/TemplateApp.Core, src/TemplateApp.UI (App.xaml, Views/MainPage.xaml)
and the LinuxX11, LinuxWayland, LinuxFrameBuffer, MacOS, Win32Skia and
WinWpfSkia heads - which the IDE unpacks and renames. It is data, not a
buildable project in this repository; keep it in step with the reference
structure documented in AGENT-README.txt.

================================================================================

OTHER NON-PACKAGE CONTENT
=========================
    TestResults/                 scratch output directory for local test runs;
                                 nothing in it is source
    src/PackageCache/            a placeholder directory whose .gitignore
                                 excludes its entire contents; not source
    src/Common/                  GlobalAssemblyInfo.cs, a leftover from the
                                 upstream build system; no project references
                                 it today
    src/Common_ViewLibraryProps/ Globbing.props, consumed only by the test
                                 helper project Platform.UI.Tests.ViewLibraryProps

Test projects are not covered here: they are the packages' own tests, and
MAINTAINER-README.txt lists them and explains how to run them.
