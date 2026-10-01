CodeBrix.Platform.PlayTest.ApacheLicenseForever — maintainer notes

Read README.md for the supported 0.1 surface and consumer setup. Namespace:
CodeBrix.Platform.PlayTest. Target: net10.0. Nullable and implicit usings are off.

This is a separate head; do not add PlayTest branches to existing desktop heads.
VirtualHost owns the UI dispatcher, CPU Skia renderer, window/display extensions,
isolated clipboard and synthetic input. Shared framework assembly friend entries
permit the same internal extension and compositor APIs used by other heads.
Virtual resolution is 1920x1080 or 1080x1920, with rasterization scale 1.
Orientation can change between serialized tests. A screen generation and its
dimensions travel with each immutable frame; discard frames from old generations.
SetContentAsync applies an optional orientation before creating the page and
restores the launch preference when no override is supplied.

Preview precedence: explicit PlayTestOptions.Headless, then the MTP command-line
option (--headed / --nonheadless / --headless), then CODEBRIX_PLAYTEST_HEADED,
then headless. Conflicting headed/headless flags are rejected before tests run.
The package compiles buildTransitive/CodeBrix.PlayTest.TestingPlatform.cs into
Microsoft.Testing.Platform consumers and registers its TestingPlatformBuilderHook
before MTP generates/caches SelfRegisteredExtensions. Keep that adapter out of
the runtime assembly and its dependencies: it uses the consumer's MTP version.
Validation records a nullable Boolean in AppContext under
CodeBrix.Platform.PlayTest.CommandLineHeadless without changing the environment.
PlayTestOptions reads that setting when constructed. Isolate/restore that setting
as well as environment variables in configuration tests. Other runners retain
environment/code configuration; custom MTP entry points must invoke the generated
AddSelfRegisteredExtensions hook. The source-based demo imports these targets too.

Preference order: explicit PlayTestOptions.Orientation, --orientation, environment variable
CODEBRIX_PLAYTEST_ORIENTATION, CodeBrixPlayTestPreferredOrientation project
metadata in ConfigurationAssembly (entry assembly by default), then Landscape.
An omitted Orientation differs from an explicitly assigned Landscape.
PlayTestOrientationAttribute is runner-neutral metadata; a fixture hook must
resolve executing row traits before method attributes and apply the result.

Theme preference order: explicit PlayTestOptions.Theme, --theme,
CODEBRIX_PLAYTEST_THEME, CodeBrixPlayTestPreferredTheme
project metadata in ConfigurationAssembly, then Light. Values are Light or Dark,
case-insensitive. VirtualSystemTheme implements ISystemThemeHelperExtension and
is registered before app construction; refresh the framework theme cache then.
This simulates an OS preference, including UISettings system colors, while still
allowing explicit app theme choices. PlayTestApplication.SystemTheme reports the
launch preference, which stays fixed for the process and ignores the real desktop.
Headless rendering and preview share the resulting pixels; SDL has no theme logic.

PreviewProgram is an executable entry point in this assembly. PreviewConnection
launches it with the consumer's deps/runtimeconfig. SDL requires the process main
thread on macOS. Frames cross a bounded queue and stdin pipe, each prefixed by
two little-endian Int32 dimensions. Input never crosses back. The SDL window
keeps the launch preference's proportions and letterboxes opposite-orientation
frames; resize textures/logical presentation on the SDL main thread, never
resize the native window in response to test orientation. No SDL initialization
occurs in the offscreen application process.

VirtualInput always uses Control-based shortcuts. VirtualHost disables
FeatureConfiguration.TextBox.UsePlatformKeyboardShortcuts before app construction
so text editing also follows that model on macOS. Keep this setting in feature
configuration: touching TextBox itself before dispatcher setup initializes brushes
too early. Normal desktop hosts retain their native keyboard conventions.

SlowMo precedence: explicit PlayTestOptions.SlowMo (including zero), then
CODEBRIX_PLAYTEST_SLOWMO, then 250 milliseconds headed or zero headless. The
default uses the final Headless option, not just the environment. Keep an omitted
SlowMo distinct from an explicit zero. Resolve and validate once at launch, then
store the resolved delay in the application's options snapshot. Environment
values use invariant culture and must be finite and non-negative; unset/empty
means default. Fixtures should leave SlowMo unset instead of substituting zero.

Normal packaging: build/CodeBrix.Platform.Build.csproj includes this project in
_CsprojPackage and applies the shared PackageVersion. Do not set assembly Version
to a NuGet version. The core/runtime packages must include the PlayTest friend
declarations. The older published core alone cannot run the new head.

Application tests must assert actual outcomes. Preserve lazy locator resolution,
strictness, bounded retry, hit testing, and dispatcher confinement when extending
the API. Do not replace clicks with direct command invocation. Use a serialized
fixture and reset page/application state between tests. Test both timeout and
successful paths when changing the locator engine.

PlayTestFilePickers registers the framework folder/open/save extensions before app
construction. Responses are application-owned FIFO queues, never native dialogs.
Null is explicit cancellation; missing responses fail. Existing save files must
not be truncated. FilePickers.Clear is the fixture's responsibility on reset.
Check/Uncheck dispatch pointer input, including to a ToggleSwitch template thumb,
and verify the outcome; never set IsChecked/IsOn directly. Bring-into-view supports
attached controls, not unrealized virtualized items. Page replacement disposes a
Frame-hosted page's disposable DataContext as well as a direct page's DataContext.

Control/picker contract tests live in samples/CodeBrixPlatform/PlayTestDemo/tests/
PlayTestDemo.PlayTests, alongside the dedicated six-head demo application. They
exercise that application's actual shared XAML and build the framework from source.
Put general control demonstrations there, not into unrelated sample applications.
JustBetweenUs and the five additional CodeBrix.Samples suites exercise their own
application screens; the latter share PlayTestSupport in that repository.
They use SilverAssertions and existing app service interfaces for offline data.
Linux WPE, Windows Edge WebView2 and macOS WKWebView work through the app's
own WebView add-in; PlayTest adds no WebView package or native browser dependency.
On Windows, its
STA dispatcher also pumps native messages for WebView2's COM callbacks. The
optional add-in supplies an offscreen composition controller and captures its
frames into Skia; pointer and keyboard input go through the browser input APIs.
Browser profiles live below TestResults/PlayTest/WebView2, per process. The head
flows packaged ICU assets/data for Windows/macOS text initialization. On macOS the
optional add-in launches an AppKit/WKWebView helper on its process main thread,
with a nonpersistent browser data store and no visible native window.
Navigation decisions, script results and PNG frames travel through redirected
pipes; callbacks and presentation return to the XAML dispatcher. Input uses
native NSEvent dispatch, not DOM actions. Page unload closes that helper.
GPU-only GL controls remain unsupported. Validate each OS on its own host before
claiming runtime support.

AriaRole.cs retains its original MIT notice. Update the root third-party notice
whenever adapting additional upstream code. SDL3 is a normal NuGet dependency.

Screenshot recording: --screenshotfolder claims an existing empty folder only at
execution, never during option validation/discovery. Record PNGs from the virtual
Skia surface, not the desktop preview. buildTransitive/CodeBrix.PlayTest.Xunit.cs
is source-compiled only into supported reflection-based xUnit consumers; assembly
fixture lifecycle records identity/outcome, BeforeAfter hooks capture after setup
and before teardown. No runtime xUnit dependency or consumer test edits. Other
runners reject recording until they have equivalent lifecycle adapters.

Recording/PlayTestRecording.cs owns a versioned JSON index, namespace/class/method/
theory-row paths, exclusive folder claim, atomic index replacement, and serialized
PNG writes. Keep every path under the claimed folder, sanitize for Windows too,
and never clear or overwrite user artifacts. Case numbers are per-method execution
order, not discovery order. Keep IDs/arguments in the index. Missing UI and skipped
tests must not get fabricated images. Source locations use PDB-backed stack frames
and may be null. Local timestamps include offsets.

UI operations use a nested async scope so only the outer API boundary records;
retry probes and internal Evaluate calls must not multiply screenshots. Preserve
the no-recording fast path, caller location before awaiting, final capture on
failure, and capture before application disposal. Tests must remain serialized.
Preference regression tests isolate and restore CLI AppContext keys as well as
environment variables. The standalone build/test-scripts/PlayTestRecordingProbe
contains an intentional failure; run it through playtest-recording.py, which checks
that failure, PNG/index integrity, hierarchy, source lines, precedence and rejected
folders. It is not part of the normal demo suite.
