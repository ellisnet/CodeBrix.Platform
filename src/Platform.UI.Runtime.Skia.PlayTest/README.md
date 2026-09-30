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
2. `CODEBRIX_PLAYTEST_ORIENTATION=portrait` or `landscape`.
3. `<CodeBrixPlayTestPreferredOrientation>Portrait</CodeBrixPlayTestPreferredOrientation>`
   in the test `.csproj` (either orientation is accepted).
4. Landscape.

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

1. `CODEBRIX_PLAYTEST_THEME=dark` or `light`.
2. `<CodeBrixPlayTestPreferredTheme>Dark</CodeBrixPlayTestPreferredTheme>` in the
   test `.csproj` (Light is also accepted).
3. Light.

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

Run the project with `dotnet test`. Headless is the default. Set
`CODEBRIX_PLAYTEST_HEADED=1` to open a live SDL3 preview on Windows, macOS,
X11 or Wayland. The preview runs in a separate process. Its initial size and
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
Supported API includes role/text/label/test-ID locators, scoped role/test-ID
locators, filtering, First/Last/Nth, click/fill/press/check/scroll, mouse and keyboard input,
value/text/state/count assertions, scripted storage pickers, state evaluation and screenshots. Unsupported
Playwright browser features (DOM/CSS/JavaScript, network routing, browser contexts,
tracing and browser downloads) are not simulated. GPU-only controls, other native
dialogs, multiple windows and horizontal wheel input are outside this prototype.
On Linux, an application that already references the WebView add-in can use its
offscreen WPE WebKit browser, including rendering and XAML pointer/keyboard input;
the system WPE libraries are still required. PlayTest does not supply a browser
engine, DOM locators, or native WebView providers for Windows/macOS.

The AriaRole enum is adapted from MIT-licensed Microsoft Playwright for .NET.
See the packaged `THIRD-PARTY-NOTICES.txt` for attribution and license text.
