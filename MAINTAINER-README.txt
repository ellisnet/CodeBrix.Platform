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
    src/Platform.UI.Tests/Platform.UI.Automated.Tests.csproj (+ Tests.ViewLibrary,
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

COVERAGE. 266 scenarios per orientation at the time of writing: 265 pass and
one is skipped, the skip being the Harness group's orientation-tagged scenario
that belongs to the other panel (@landscape-only and @portrait-only are the
only way a scenario is excluded from one of the two runs). By coverage group,
again at the time of writing:

    Harness     10  harness smoke: the panel size, a Border fill, TextBlock
                    ink, a Button tap, the between-scenario reset, and the
                    orientation tags (the one skip per run is in the Harness
                    group)
    Layout      56  layout and visuals: Grid, StackPanel, Canvas, Border, the
                    shapes, solid and gradient brushes, Opacity and
                    Visibility, RenderTransform, Clip,
                    margin/padding/alignment
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
    Popups      22  popups and dialogs: Flyout, MenuFlyout, Popup,
                    ContentDialog, TeachingTip, InfoBar
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
orientation beside the core's 266. None of this belongs in an add-in's
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
