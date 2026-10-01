# CodeBrix.Platform PlayTest

Package: `CodeBrix.Platform.PlayTest.ApacheLicenseForever` (.NET 10).

PlayTest 0.1 is an offscreen Skia application head with a C# testing API modeled
on Microsoft Playwright. It hosts the application's actual XAML and view models.
It does not start a browser or use the Playwright driver.

```csharp
await using var application = await PlayTestApplication.LaunchAsync(() => new App());
var page = application.Page;
await page.GetByRole(AriaRole.Textbox, new() { Name = "Message" }).FillAsync("Hello");
await page.GetByRole(AriaRole.Button, new() { Name = "Send", Exact = true }).ClickAsync();
await Assertions.Expect(page.GetByTestId("status")).ToHaveTextAsync("Sent");
```

Create an executable test project in `tests/MyApplication.PlayTests`, reference
this package and `MyApplication.Core`, and import the application's `.UI.projitems`.
Set `Nullable` and `ImplicitUsings` to `disable`. Use an xUnit, NUnit or MSTest
fixture to launch one application and dispose it when the suite finishes.
`PageTest` supplies `Page` and `Expect` without depending on a particular runner.
Do not run tests that share an application in parallel.

The virtual screen is 1920×1080 landscape or 1080×1920 portrait, at scale 1.
Its preferred orientation is resolved once at launch, in this order:

1. An explicitly assigned `PlayTestOptions.Orientation` in code.
2. `--orientation=portrait` or `--orientation=landscape`.
3. `CODEBRIX_PLAYTEST_ORIENTATION=portrait` or `landscape`.
4. `<CodeBrixPlayTestPreferredOrientation>Portrait</CodeBrixPlayTestPreferredOrientation>`
   in the test `.csproj` (either orientation is accepted).
5. Landscape.

Leave `Orientation` unset to use that fallback chain. The package emits the project
preference as assembly metadata; set `ConfigurationAssembly = typeof(AppFixture).Assembly`
in the launch options so it works even when a runner uses a different entry assembly.
Names are case-insensitive; invalid selected values fail with a descriptive error.
`application.PreferredOrientation` is the resolved launch preference, while
`application.Orientation`, `Width` and `Height` describe the current virtual screen.
The host uses software Skia, embedded fonts, and a stable default culture;
application-specific OS services can still differ across systems.

The simulated operating-system theme is resolved once, before constructing the
application, in this order:

1. An explicitly assigned `PlayTestOptions.Theme` in the application fixture.
2. `--theme=dark` or `--theme=light`.
3. `CODEBRIX_PLAYTEST_THEME=dark` or `light`.
4. `<CodeBrixPlayTestPreferredTheme>Dark</CodeBrixPlayTestPreferredTheme>` in the
   test `.csproj` (Light is also accepted).
5. Light.

Names are case-insensitive. An unset or empty environment value falls back to
the project; invalid selected values fail with a descriptive error. Project
preferences use the same `ConfigurationAssembly` metadata mechanism as orientation.
The host supplies this preference to the framework's normal system-theme provider,
so application startup, theme resources and `UISettings` system-color queries see
it. `application.SystemTheme` reports the simulated preference. It remains fixed
for the run and never follows the real desktop's theme. An application can still
explicitly choose its own theme. Headless rendering, screenshots and the visible
preview all use the same application pixels. Run separate processes to test
startup under both OS themes; orientation changes do not change the theme.

Tests can override the preference without starting another application. Use
`Page.SetContentAsync(() => new MainPage(), ScreenOrientation.Portrait)` to apply
orientation **before** constructing a fresh page and wait for layout/rendering.
Omitting the second argument restores the launch preference on every reset.
`application.SetOrientationAsync(...)` changes orientation on an existing page;
omitting its argument restores the preference. These operations require serialized
tests and must not run concurrently with actions or screenshots. Application
singletons remain alive; testing process startup in each orientation still needs
separate processes.

### Test and theory-row requirements (xUnit v3 example)

The package is runner-neutral. `[PlayTestOrientation(ScreenOrientation.Portrait)]`
declares a method requirement; a fixture hook must apply it. An individual theory
row can use the `PlayTestOrientation` trait to override that method requirement:

```csharp
[Fact]
[PlayTestOrientation(ScreenOrientation.Portrait)]
public async Task Portrait_screen() { /* assertions and actions */ }

[Theory]
[PlayTestOrientation(ScreenOrientation.Landscape)]
[InlineData("portrait case", Traits = new[] { "PlayTestOrientation", "Portrait" })]
[InlineData("landscape case")] // inherits the method requirement
public async Task Layout(string scenario) { /* assertions and actions */ }
```

In the test class's `IAsyncLifetime.InitializeAsync`, resolve the **executing test's**
traits (not the discovery test case's traits, which may group deferred data rows):

```csharp
var test = (Xunit.v3.IXunitTest)TestContext.Current.Test;
test.Traits.TryGetValue(PlayTestOrientationAttribute.CaseTraitName, out var rowOrientations);
var orientation = PlayTestOrientationAttribute.Resolve(test.TestMethod.Method, rowOrientations);
await fixture.Application.Page.SetContentAsync(() => new MainPage(), orientation);
// Then finish application-specific initialization, such as dismissing startup dialogs.
```

Row requirement > method requirement > fixture preference. Conflicting row
requirements are errors. Other runners can pass their method and row metadata to
the same resolver. The attribute itself does not install runner hooks.

Run the project with `dotnet test`. Headless is the default. With .NET 10's
Microsoft.Testing.Platform runner, the PlayTest package automatically enables:

```sh
dotnet test MyApp.PlayTests.csproj --headed
dotnet test MyApp.PlayTests.csproj --nonheadless  # Alias for --headed
dotnet test MyApp.PlayTests.csproj --headless
dotnet test MyApp.PlayTests.csproj --nonheadless --theme=dark --orientation=portrait
```

These switches work on Windows, macOS and Linux (X11/Wayland previews). No extra
project properties or fixture changes are needed beyond the test project's
existing MTP setup and the updated PlayTest package. They appear in `--help`.
Use one mode per invocation; combining `--headless` with either headed spelling
is an error. Mode precedence is **explicit `PlayTestOptions.Headless` in code >
command-line option > `CODEBRIX_PLAYTEST_HEADED` > headless**. Thus `--headed`
overrides `CODEBRIX_PLAYTEST_HEADED=0`, and `--headless` overrides a value of `1`.
Leave `Headless` unset in fixtures to let the command line control the preview.

`--theme` accepts only `light` or `dark`; `--orientation` accepts only `landscape`
or `portrait`, all case-insensitive. Both `--theme=dark` and `--theme dark` work.
These options override environment and project preferences, but explicit fixture
options, orientation requirements applied by tests/theory rows, and application or
element `RequestedTheme` choices retain priority. Theme selects the simulated OS
preference at launch; it does not force every element to use that theme.

The standard generated xUnit/MSTest/NUnit MTP entry points register the adapter
automatically. A custom MTP entry point must call its generated
`AddSelfRegisteredExtensions(builder, args)` method. Direct `dotnet test` extension
options require MTP mode in `global.json`, as in the setup above; VSTest mode does
not gain these switches. Environment/code configuration remains available for
other runners. When running a solution, use the option on PlayTest projects or
modules that register it; unrelated test projects may reject the unknown switch.

### Automatic screenshot recording

In xUnit v3 projects using version 4 or later (including all the sample suites),
the updated NuGet automatically installs recording hooks. No test methods, fixtures,
attributes or additional project properties are needed. The command-line options
are shared across Windows, Linux and macOS. Screenshot recording uses the virtual
Skia screen in both headless and headed modes, without desktop capture permissions.

First create a **new, empty folder**, then run:

```sh
mkdir -p "$HOME/Temp/JustBetweenUs PlayTest run 2026-09-30"
dotnet test JustBetweenUs.PlayTests.csproj --nonheadless --theme=dark --orientation=portrait \
  --screenshotfolder="$HOME/Temp/JustBetweenUs PlayTest run 2026-09-30"
```

The option accepts relative paths (relative to the test process working directory),
absolute paths and a leading `~/`, including inside a quoted value. Quoting the
entire value is recommended for spaces. The folder must already exist and contain
**nothing**, including hidden files or empty subdirectories. Invalid paths and
nonempty folders fail before tests run; the recorder never empties a destination.
Use a different empty folder for every run and every test module. Discovery/help
does not claim the folder. Microsoft.Testing.Platform can summarize invalid options
as "Zero tests ran" in `dotnet test`; invoking the compiled test DLL with
`dotnet exec MyApp.PlayTests.dll ...` also shows the detailed option diagnostic.

For namespace `JustBetweenUs.PlayTests.SendButton.Clicking`, class `ClickTests`,
and method `clicking_button_adds_test`, the output is:

```text
screenshot-index.json
SendButton/Clicking/ClickTests/clicking_button_adds_test/
  screenshot-start.png
  screenshot-1.png
  screenshot-2.png
  screenshot-final.png
```

Theory methods add `test-case-1/`, `test-case-2/`, etc. below the method folder.
Case numbers follow **execution order within that method for this invocation**;
use recorded IDs, display names and arguments to identify a row across filtered
runs or runner ordering changes. The test project's `RootNamespace` is omitted
only when it is an exact namespace prefix. Other namespaces remain complete.
Names unsafe on Windows are escaped; long names and collisions get a hash suffix.

The starting image is taken **after test setup and before the test body**. A PNG
is then captured after each PlayTest action, including clicks, text/keyboard/mouse
input, check/uncheck, scrolling, page replacement, orientation changes and explicit
`EvaluateAsync`. Completed PlayTest assertions and waits also capture the UI so
asynchronous outcomes are represented. Nested implementation calls and retry polls
do not generate extra images. The final image is captured before test cleanup,
including when a test throws. All captures use the actual app pixels, without
preview letterboxing. No recording work runs when the option is absent.

This records **PlayTest API boundaries**, not every C# statement or animation frame.
For example, changes made by arbitrary background tasks appear at the next action,
PlayTest wait/assertion or final capture. Existing tests using PlayTest's normal UI
and waiting APIs need no changes. A skipped test has no executing lifecycle and
produces no recording entry. An executed configuration-only test without a running
app, or a test whose setup fails before the body, has metadata and an explanatory
`screenshotNote`; it cannot produce images of a nonexistent UI. Shared-app tests
must be serialized, as required by PlayTest itself.

`screenshot-index.json` has `schemaVersion: 1` and is replaced atomically after
each capture/test. It contains:

- Run ID, completion status, local start/end timestamps with UTC offsets, OS,
  architecture, .NET version, time zone, machine name and process ID.
- Test/app/PlayTest assembly identities and versions, root namespace, CLI and
  environment preferences, resolved preview/theme/orientation and action delay.
- Each executed test's ID, display name, namespace, class, method/signature,
  row number and formatted argument values, timestamps, duration, outcome and errors.
- Each PNG's relative path, start/step/final kind, step number, local capture time,
  operation/locator, dimensions, orientation, simulated OS theme, application and
  root-element theme choices, and source file/line when available from debug symbols.

Step source locations point to the calling test or helper; optimized code or missing
PDBs can make them unavailable (`null`). A normally finished run is `completed` even
when some tests failed; inspect the individual outcomes. An interrupted process can
leave a valid partial index marked `running`. Capture/write failures are reported
and fail the affected test. This feature currently requires the reflection-based
xUnit v3 runner; other MTP runners retain theme/orientation/preview options and
reject `--screenshotfolder` with an explanatory error.

You can still set `CODEBRIX_PLAYTEST_HEADED=1` to open the preview. The preview
runs in a separate process. Its initial size and
aspect ratio follow the launch preference and stay unchanged when tests switch
orientation. Portrait frames in a landscape preview get black bars on the left
and right; landscape frames in a portrait preview get bars above and below.
The developer can still resize the window manually; frames scale without
stretching or cropping. Screenshots contain the actual virtual screen, without
preview bars. All physical
keyboard, pointer, wheel and touch input is discarded. Closing the preview
does not close the test application. Headless execution needs no display server.

Headed runs assume **250 ms between actions**; headless runs assume zero.
Set `CODEBRIX_PLAYTEST_SLOWMO` to another delay in milliseconds, including `0`
for full speed. An unset or empty variable uses the headed/headless default.
An explicit `PlayTestOptions.SlowMo` in code overrides the environment, including
an explicit zero. Leave it unset in fixtures to use the normal preferences.
The default follows the final `Headless` option, including an explicit code
override. The delay is resolved once at launch and does not change during a run.
Values must be finite and non-negative, with `.` as the decimal separator.

For an application whose fixture should always use 750 ms, set it in the launch
options (this also overrides any `CODEBRIX_PLAYTEST_SLOWMO` value):

```csharp
Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
{
    ConfigurationAssembly = typeof(AppFixture).Assembly,
    SlowMo = 750,
});
```

Locators resolve on every use. `GetByRole` uses automation peers and accessible
names; `GetByLabel` uses accessible names; `GetByTestId` uses
`AutomationProperties.AutomationId`. Give controls meaningful automation names.
Actions and assertions retry up to the configured timeout (10 seconds by default).
Single-element operations reject ambiguous matches. Clicks wait for visibility,
enablement, stable bounds and a successful hit test, then inject mouse events.
`FillAsync` focuses an editable text control, updates its value, and raises its
normal change/binding behavior. `PressAsync` injects keyboard events.
Input values expose LF (`\n`) line endings, matching browser textareas; the
application's native TextBox value retains WinUI's own line-ending convention.
The virtual head uses a consistent Control-based shortcut model on all OSs;
`ControlOrMeta` therefore maps to Control.
TextBox selection, paste and word editing follow that model on macOS too.

Use retrying assertions for the outcome of an action, not just its dispatch.
For nonvisual outcomes use `application.WaitForAsync(() => service.Completed,
value => value)`; the probe executes on the application UI thread. Clipboard
operations use an isolated in-process clipboard, readable with
`Page.ClipboardTextAsync()`. `ScreenshotAsync` captures the virtual screen as PNG.
Timeout failures include a PNG path and a visual-tree description under
`TestResults/PlayTest` (override `ArtifactsDirectory` when needed).

### Scripted file and folder pickers

The [PlayTestDemo application](../../samples/CodeBrixPlatform/PlayTestDemo/README.md)
contains runnable control/picker examples and their regression tests. It uses
the framework from source and also has six normal desktop heads. Generic control
demonstrations belong there; application suites should test their own UI.

Queue a response before the action that opens the application's normal
`Windows.Storage.Pickers` picker. No native picker or replacement dialog is
displayed, including in headed mode:

```csharp
application.FilePickers.EnqueueFolder(assetsDirectory);
await Page.GetByRole(AriaRole.Button, new() { Name = "Choose assets folder…", Exact = true }).ClickAsync();

application.FilePickers.EnqueueOpenFile(inputFile);
application.FilePickers.EnqueueOpenFiles(firstFile, secondFile);
application.FilePickers.EnqueueSaveFile(outputFile);

// Cancel the next picker of the corresponding kind:
application.FilePickers.EnqueueFolder(null);
application.FilePickers.EnqueueOpenFile(null);
application.FilePickers.EnqueueSaveFile(null);
```

Folder, open-file and save-file responses have separate FIFO queues. Single and
multiple open pickers share the open-file queue. Cancel returns null for single
selection and an empty list for multiple selection. A single-file picker rejects
a multiple-file response. Paths become absolute when queued. Selected folders
and input files must exist; tests should create their own fixture data. A save
file's parent folder must exist. Selecting a new save path creates an empty file,
matching a native picker; selecting an existing file leaves its contents intact.
Tests and application code remain responsible for actual reads and writes.

An unqueued request throws `PlayTestException` instead of silently cancelling.
Request counts and `LastSuggestedFileName` are available for assertions.
Call `application.FilePickers.Clear()` between serialized tests to clear queued
responses and reset this history; `Page.SetContentAsync` does not clear it.

### Checked controls and scrolling

`CheckAsync`, `UncheckAsync` and `SetCheckedAsync(bool)` support CheckBox,
RadioButton, ToggleButton and ToggleSwitch. They use real pointer input when a
state change is needed and do nothing when the requested state is already set.
The resulting state is retried and checked. Use `IsCheckedAsync()` to read it or
`Expect(locator).ToBeCheckedAsync()` / `.Not.ToBeCheckedAsync()` to wait for it.
An unsupported control type throws a descriptive exception. Normal disabled,
visibility and hit-test checks apply to state-changing actions.
Radio buttons follow their usual UI behavior: checking works, but clicking a
selected radio button cannot uncheck it, so that requested transition times out.

`ScrollIntoViewIfNeededAsync()` uses XAML's bring-into-view mechanism to reveal an
attached element in a ScrollViewer before clicking it. Virtualized items must
first be materialized by scrolling their container.

Version 0.1 supports a single window and a single application per process.
`Page.SetContentAsync(() => new MainPage())` installs a fresh page between tests;
application singletons are still shared and should be reset explicitly as needed.
Supported API includes role/text/label/test-ID/type locators, scoped locators,
filtering, First/Last/Nth, click/hover/drag/fill/press/check/scroll, mouse and keyboard input,
value/text/state/count assertions, scripted storage pickers, state evaluation and screenshots. Unsupported
Playwright browser features (DOM/CSS/JavaScript, network routing, browser contexts,
tracing and browser downloads) are not simulated. GPU-only controls, other native
dialogs, multiple windows and horizontal wheel input are outside this prototype.
An application that references the matching WebView add-in can use its offscreen
browser, including rendering and XAML pointer/keyboard input: WPE WebKit on Linux
(system WPE libraries required), or Edge WebView2 on Windows (installed WebView2
runtime required), or WKWebView on macOS 12 and later. The macOS add-in supplies
a separate native helper using the system WebKit engine, with a nonpersistent
data store. Its snapshots are composited into Skia and its native pointer/keyboard
events are driven by PlayTest. No native browser window is shown. Packages built
on macOS include a universal Intel/Apple Silicon helper; packages built elsewhere
compile the bundled helper source during the first macOS PlayTest build using
Apple's command-line tools. The helper's complete source and standalone build
instructions live in [tools/MacOsWebViewHelper](../../tools/MacOsWebViewHelper/README.md)
in this repository. Windows browser profiles are isolated per process under
`TestResults/PlayTest/WebView2` in the test output directory. PlayTest supplies the
Windows STA message pump; the optional add-in supplies the browser provider and
composites its frames into Skia, including screenshots and the SDL preview.
PlayTest does not supply a browser engine or DOM locators. Native browser file
uploads, script dialogs and downloads are outside the macOS offscreen adapter's
current surface; file upload/dialog requests are cancelled without displaying UI.

### Editors, toolbars, menus and split panes

`GetByType<T>()` finds visible controls by an application-owned type. It is lazy
and supports the same strictness, scoping, filtering and retries as role locators.
Pass `includeHidden: true` when inspecting attached hidden controls. Referencing
an add-in from the test application does not add that dependency to PlayTest.

```csharp
var editor = Page.GetByRole(AriaRole.Textbox, new() { Name = "Source editor", Exact = true });
await editor.FillAsync("first value");
await editor.FillAsync("second value");
await editor.PressAsync("Control+z");
await Expect(editor).ToHaveValueAsync("first value");
await editor.PressSequentiallyAsync("\nnext line");

await Page.GetByRole(AriaRole.Menuitem, new() { Name = "More", Exact = true }).HoverAsync();
await Page.GetByType<MyDivider>().DragByAsync(80, 0);
await editor.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 100, Y = 30 } });
```

`PressSequentiallyAsync` focuses the control (preserving focus on a child of a
composite editor) and sends each character through routed key events. Enter and
Tab therefore exercise normal indentation/completion handlers. `Keyboard.TypeAsync`
does the same on the already focused control; `FillAsync` replaces the whole
value. Custom editors support fill/value assertions through `IValueProvider`.
Read-only providers are respected. AdvancedTextEdit implements this contract in
its own package, including undoable replacement and text-area focus.

Menus expose `Menubar`, `Menu`, `Menuitem` and `Menuitemcheckbox`; names exclude
template arrow/check glyphs. Toolbar peers expose `Toolbar` and buttons, including
split buttons. Checked actions also support standard `IToggleProvider` peers.
Checking a toggle menu item verifies its result after the flyout dismisses.

Click options accept a relative logical-pixel `Position`, a left/right/middle
`Button`, and `ClickCount` (1–3). `HoverAsync` accepts a relative position too.
`DragByAsync(deltaX, deltaY)` presses, moves in bounded steps (default 10), then
releases in a `finally` block. Start points must be visible, enabled, stable and
hit-testable; positions must be inside the target and drag endpoints inside the
virtual screen. These actions use pointer input rather than invoking handlers.
They are automatically included in screenshot recordings as one logical step.

The AriaRole enum is adapted from MIT-licensed Microsoft Playwright for .NET.
See the packaged `THIRD-PARTY-NOTICES.txt` for attribution and license text.
