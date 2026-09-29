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

The virtual screen is always 1920×1080 at scale 1. Set
`new PlayTestOptions { Orientation = ScreenOrientation.Portrait }` at launch
for 1080×1920. Each orientation requires a separate test process. Resolution
is not configurable. The host uses software Skia, embedded fonts, and a stable
default culture; application-specific OS services can still differ across systems.

Run the project with `dotnet test`. Headless is the default. Set
`CODEBRIX_PLAYTEST_HEADED=1` to open a live SDL3 preview on Windows, macOS,
X11 or Wayland. The preview runs in a separate process, preserves the virtual
aspect ratio, and scales the image as the window is resized. All physical
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
