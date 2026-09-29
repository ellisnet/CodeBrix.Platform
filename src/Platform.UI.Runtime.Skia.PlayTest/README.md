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
does not close the test application. `PlayTestOptions.SlowMo` (milliseconds)
can make test actions easier to watch. Headless execution needs no display server.

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

Version 0.1 supports a single window and a single application per process.
`Page.SetContentAsync(() => new MainPage())` installs a fresh page between tests;
application singletons are still shared and should be reset explicitly as needed.
Supported API includes role/text/label/test-ID locators, scoped role/test-ID
locators, filtering, First/Last/Nth, click/fill/press, mouse and keyboard input,
value/text/state/count assertions, state evaluation and screenshots. Unsupported
Playwright browser features (DOM/CSS/JavaScript, network routing, browser contexts,
tracing and browser downloads) are not simulated. GPU-only controls, native
dialogs, multiple windows and horizontal wheel input are outside this prototype.

The AriaRole enum is adapted from MIT-licensed Microsoft Playwright for .NET.
See the packaged `THIRD-PARTY-NOTICES.txt` for attribution and license text.
