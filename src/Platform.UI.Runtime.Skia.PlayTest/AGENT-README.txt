================================================================================
AGENT-README: CodeBrix.Platform.PlayTest
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.Platform.PlayTest.ApacheLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.Platform.PlayTest.ApacheLicenseForever is a TEST HEAD for
CodeBrix.Platform applications (.NET 10 or later). A test project references it
instead of a desktop head; it runs the application's REAL XAML, view models and
services offscreen on a fixed virtual Skia screen, and drives them through a C#
locator / action / assertion API:

    await using var app = await PlayTestApplication.LaunchAsync(() => new App());
    var page = app.Page;
    await page.GetByRole(AriaRole.Textbox, new() { Name = "Message" }).FillAsync("Hello");
    await page.GetByRole(AriaRole.Button, new() { Name = "Send", Exact = true }).ClickAsync();
    await Assertions.Expect(page.GetByTestId("status")).ToHaveTextAsync("Sent");

What you get:
  - A virtual screen of exactly 1920 x 1080 (landscape) or 1080 x 1920
    (portrait) logical pixels at scale 1, rendered by software Skia with the
    application's embedded fonts and a stable default culture - identical
    pixels on Windows, Linux and macOS, with no display server needed.
  - Lazy, strict, retrying locators: every action waits until its element is
    visible, enabled, at stable bounds and actually receives the hit test, then
    injects real pointer or keyboard input through the normal input pipeline.
  - Retrying assertions, scripted file/folder pickers, a recording launcher,
    held keys, window close/minimize requests, an isolated clipboard, a
    simulated OS theme, per-test orientation, page and element PNG
    screenshots, automatic screenshot recording, and an optional live
    view-only preview window.

The API names and call shapes intentionally follow Microsoft Playwright's C#
API, and the AriaRole enum is adapted from MIT-licensed Microsoft Playwright
for .NET (see THIRD-PARTY-NOTICES.txt). It is not a browser: no browser or
Playwright driver is started.

INSTALLATION
============
PackageId:   CodeBrix.Platform.PlayTest.ApacheLicenseForever
License:     Apache-2.0
Assembly:    CodeBrix.Platform.UI.Runtime.Skia.PlayTest
Namespace:   CodeBrix.Platform.PlayTest

    dotnet add package CodeBrix.Platform.PlayTest.ApacheLicenseForever

Reference it from a TEST project only - never from an application or head
project. NuGet dependencies (pulled in automatically, no versions to manage):
  - CodeBrix.Platform.ApacheLicenseForever        the framework (same family
                                                  version as this package)
  - CodeBrix.Platform.Runtime.Skia.ApacheLicenseForever
  - SkiaSharp + HarfBuzzSharp (and their Linux native assets)
  - CodeBrix.Platform.Unicode / UnicodeMacOs      ICU data for Windows/macOS
  - an SDL3 binding package                       the headed preview window only

The package also ships build logic (buildTransitive) that makes the test
project a PlayTest head, records project preferences as assembly metadata, and
compiles two small runner adapters into Microsoft.Testing.Platform / xUnit v3
test projects (command-line switches, screenshot recording). Nothing to wire up.

Requirements:
  - An EXECUTABLE test project (OutputType Exe) - the norm for xUnit v3 and
    Microsoft.Testing.Platform test projects.
  - For the command-line switches: the Microsoft.Testing.Platform runner
    (global.json "test": { "runner": "Microsoft.Testing.Platform" }).
  - For --screenshotfolder: the reflection-based xUnit v3 runner (assembly
    fixtures and test-lifecycle notifications).
  - Headless runs need no display. A headed run needs a desktop session
    (Windows, macOS, or Linux X11/Wayland).

KEY NAMESPACES / USINGS
=======================
    using CodeBrix.Platform.PlayTest;      // everything a test uses
    using Microsoft.UI.Xaml;               // ApplicationTheme, UIElement
    using System.Text.RegularExpressions;  // Regex overloads

CodeBrix.Platform.PlayTest.Recording.PlayTestRecording is public only for the
package's compiled-in runner adapter (hidden from IntelliSense); tests never
call it.

CORE API REFERENCE
==================

PlayTestApplication  (sealed, IAsyncDisposable)
-----------------------------------------------
One application per PROCESS: launch it once in a fixture, share it, and dispose
it when the suite ends. A second LaunchAsync in the same process throws
InvalidOperationException.

    static Task<PlayTestApplication> LaunchAsync(Func<Application> application,
                                                 PlayTestOptions? options = null)
        Resolves and validates every preference once, starts the app on its own
        UI thread and waits for the first rendered frame. A headed run also
        starts the preview process.
    Page Page                             the page (locators, input, content)
    PlayTestFilePickers FilePickers       scripted picker responses
    PlayTestLauncher Launcher             recorded Launcher requests (no process)
    Window Window                         the window the application created
                                          (read it inside EvaluateAsync)
    int Width, Height                     current virtual screen size
    bool Headless                         resolved at launch
    ApplicationTheme SystemTheme          simulated OS theme, fixed for the run
    ScreenOrientation PreferredOrientation  the launch preference
    ScreenOrientation Orientation         the current screen orientation
    PlayTestOpenGLInfo OpenGL             what the launch found out about OpenGL
                                          (see OPENGL)
    Task<T> EvaluateAsync<T>(Func<T> expression)   run on the UI thread, read state
    Task EvaluateAsync(Action action)
    Task SetOrientationAsync(ScreenOrientation? orientation = null)
        Re-lays-out the current page; null restores the launch preference.
    Task<bool> RequestCloseAsync()        close request; false = app cancelled
    Task MinimizeAsync(), RestoreAsync()  see WINDOW ACTIONS
    Task<T> WaitForAsync<T>(Func<T> probe, Func<T, bool> predicate,
                            float? timeout = null,
                            string description = "application outcome")
        Polls probe on the UI thread until predicate accepts it - for
        non-visual outcomes (a service call completed, a file was written).
    ValueTask DisposeAsync()

PlayTestOptions  (sealed)
-------------------------
    ScreenOrientation Orientation     explicit launch orientation (else resolved)
    ApplicationTheme Theme            explicit simulated OS theme (else resolved)
    Assembly? ConfigurationAssembly   where project preferences are read from;
                                      default: the entry assembly. Set it to
                                      typeof(YourFixture).Assembly.
    bool Headless                     explicit preview choice (else resolved)
    float Timeout                     default action/assertion timeout in ms (10000)
    float SlowMo                      delay after each action in ms (else resolved)
    string ArtifactsDirectory         failure screenshots; default TestResults/PlayTest
    PlayTestOpenGL OpenGL             Available (default) | Unavailable: a launch
                                      without OpenGL (see OPENGL)
    ScreenOrientation ResolveOrientation()   what the launch will use
    ApplicationTheme ResolveTheme()          what the launch will use

Leave a property UNSET to let the command line, environment and project decide;
assigning it (even to the default value) makes it win. See PREFERENCES.

Page  (sealed)
--------------
    Keyboard Keyboard, Mouse Mouse
    Locator GetByRole(AriaRole role, PageGetByRoleOptions? options = null)
    Locator GetByTestId(string testId, PageGetByTestIdOptions? options = null)
                                                   AutomationProperties.AutomationId
    Locator GetByText(string text, PageGetByTextOptions? options = null)
    Locator GetByText(Regex text)
    Locator GetByLabel(string text, PageGetByTextOptions? options = null)
    Locator GetByType<T>(bool includeHidden = false) where T : UIElement
    void SetDefaultTimeout(float timeout)
    Task<T> EvaluateAsync<T>(Func<T> expression);  Task EvaluateAsync(Action expression)
    Task SetContentAsync(Func<UIElement> content)
    Task SetContentAsync(Func<UIElement> content, ScreenOrientation? orientation)
        Installs a fresh page: closes open popups, disposes the previous page's
        disposable DataContext (also a page hosted in a Frame), applies the
        orientation BEFORE the factory runs, clears the isolated clipboard,
        waits for layout and rendering.
    Task<byte[]> ScreenshotAsync(PageScreenshotOptions? options = null)  PNG
        Path, Stable (wait until two consecutive captures are identical), Timeout
    Task<string> ClipboardTextAsync()     the app's isolated clipboard text

Locator matching rules:
  - GetByRole: role from the control type or its automation peer; Name matches
    the accessible name (AutomationProperties.Name, LabeledBy, or the peer's
    name) as a case-insensitive, whitespace-normalized substring, or exactly
    (case-sensitive) with Exact = true; NameRegex matches a pattern;
    IncludeHidden = true also matches hidden elements (see below).
  - GetByText: the innermost TextBlock or string-Content control whose text
    matches. GetByLabel: controls whose accessible name matches.
  - GetByType<T>: finds an application- or add-in-owned type without PlayTest
    referencing that library.
  - All searches include open popups and flyouts.
  - EVERY factory (GetByRole, GetByTestId, GetByText, GetByLabel, GetByType)
    matches only visible elements unless its IncludeHidden / includeHidden
    option is set. Hidden = not Visible itself or under a non-Visible
    ancestor, zero-size, or a cell an ItemsRepeater keeps off-screen for
    reuse. ToBeHiddenAsync keeps Playwright's meaning: it passes when nothing
    visible matches.

Locator  (sealed; lazy - resolved on every use)
-----------------------------------------------
    Locator First, Last;  Locator Nth(int index)
    Locator Filter(LocatorFilterOptions options)    HasText / HasTextRegex
    Locator GetByRole(...), GetByTestId(...), GetByText(string, ...),
            GetByLabel(...), GetByType<T>(...)      scoped to this locator
    Task<IReadOnlyList<string>> SelectOptionAsync(string | IEnumerable<string> |
            SelectOptionValue | IEnumerable<SelectOptionValue>, LocatorSelectOptionOptions?)
        ComboBox, ListBox, ListView, GridView: selects by value or label
        (exact) or SelectOptionValue { Value, Label, Index } without opening a
        drop-down or realizing rows; raises the control's normal
        SelectionChanged. Single selection takes the first match; a multiple
        selection list gets exactly the matches; an empty list clears. Waits
        until every requested option exists; returns the selected values.
        Label = DisplayMemberPath member, string item, string Content, the
        realized row's text, or ToString(); Value = SelectedValuePath member,
        else the label.
    Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions? options = null)
        PNG cropped to the element (clipped to the screen) once it is visible
        at stable bounds; Path, Stable, Timeout. A partly scrolled-out element
        is brought into view first.
    Task ClickAsync(LocatorClickOptions? options = null)
        Position (relative logical px), Button (Left/Right/Middle),
        ClickCount 1-3, Timeout
    Task HoverAsync(LocatorHoverOptions? options = null)
    Task DragByAsync(float deltaX, float deltaY, LocatorDragOptions? options = null)
        Press, move in Steps (default 10, 1-1000), release (always, even on
        failure). The end point must be inside the virtual screen.
    Task FillAsync(string value, LocatorFillOptions? options = null)
        TextBox, PasswordBox, or any IValueProvider peer; replaces the value.
    Task PressAsync(string key, LocatorPressOptions? options = null)
        Delay = milliseconds the key/chord is held (see Keyboard.PressAsync)
    Task PressSequentiallyAsync(string text, LocatorPressOptions? options = null)
        Focuses, then types real key events (Enter/Tab for newline/tab);
        Delay = wait between characters. Both focus a control or any element
        made focusable with IsTabStop (a game surface, a canvas).
    Task CheckAsync / UncheckAsync(LocatorClickOptions? options = null)
    Task SetCheckedAsync(bool value, LocatorClickOptions? options = null)
    Task ScrollIntoViewIfNeededAsync(LocatorOptions? options = null)
    Task<string> InputValueAsync(), InnerTextAsync(), TextContentAsync()
    Task<bool> IsCheckedAsync()           retries until attached
    Task<int> CountAsync(); Task<bool> IsVisibleAsync(), IsEnabledAsync(),
        IsDisabledAsync()                 single reads, no retry
    Task<LocatorBoundingBoxResult?> BoundingBoxAsync()   null when not visible

Strict mode: an operation on ONE element throws PlayTestException when the
locator matches more than one ("Strict mode violation ... resolved to N
elements. Use a unique name, test ID, or Nth()"). Inside retrying actions and
assertions an ambiguous match is retried - one dialog briefly overlapping the
next is not an error - and the violation (with the match count) is reported
only when the timeout elapses. One-shot reads (IsVisibleAsync, IsEnabledAsync,
IsDisabledAsync, BoundingBoxAsync) still report it immediately.

Assertions / LocatorAssertions
------------------------------
    static LocatorAssertions Assertions.Expect(Locator locator)
    LocatorAssertions Not
    ToBeVisibleAsync, ToBeHiddenAsync, ToBeEnabledAsync, ToBeDisabledAsync,
    ToBeCheckedAsync, ToHaveCountAsync(int), ToHaveValueAsync(string | Regex),
    ToHaveTextAsync(string | Regex), ToContainTextAsync(string)
Every assertion retries until it holds or the timeout elapses. Text
comparisons normalize whitespace; ToContainTextAsync is case-sensitive. A
ContentDialog's text is all of its text: title, content and button labels.
Enabled means the element and every ancestor CONTROL are enabled (in this
framework only controls carry IsEnabled; a panel cannot be disabled).

Keyboard and Mouse
------------------
    Keyboard.PressAsync(string key, KeyboardPressOptions? options = null)
                                          "Enter", "Control+z", "Shift+Tab",
                                          "ArrowDown", "a", "7", "F5", "Escape";
                                          Delay = ms the key/chord is held
    Keyboard.DownAsync(string key)        press and HOLD one key (no auto-repeat)
    Keyboard.UpAsync(string key)          release it
    Keyboard.TypeAsync(string text, KeyboardTypeOptions? options = null)
                                          real key events to the focused control;
                                          Delay = ms between characters
    Keyboard.InsertTextAsync(string text) replaces the focused TextBox selection
                                          without key events (like a paste)
    Mouse.MoveAsync(x, y), DownAsync(), UpAsync(), ClickAsync(x, y),
    Mouse.WheelAsync(deltaX, deltaY)      vertical only; deltaX must be 0
Key names: a single letter or digit, any Windows.System.VirtualKey name
(case-insensitive), or Control, Alt, Meta, Backspace, ArrowLeft, ArrowRight,
ArrowUp, ArrowDown, ControlOrMeta. The virtual head uses one Control-based
shortcut model on EVERY OS: ControlOrMeta is Control, and TextBox editing
shortcuts follow Control on macOS too.

Held keys. PressAsync without a Delay dispatches key down and key up in one
UI-thread step, so code that samples key state once per frame or game cycle
(a game loop) usually never sees the key. Hold it instead:
    await Page.Keyboard.DownAsync("ArrowRight");   // returns after a rendered
                                                   // frame, key still down
    await app.WaitForAsync(() => game.ShipX, x => x > 100);
    await Page.Keyboard.UpAsync("ArrowRight");
    await Page.Keyboard.PressAsync("Space", new() { Delay = 150 });
DownAsync takes one key, not a chord; hold modifiers with separate calls - a
held Shift/Control/Alt/Meta applies to later presses and typing. Keys still
held are released (their key-up events are dispatched to the old page) by
Page.SetContentAsync before the page is replaced, and by DisposeAsync, so
they never leak into the next test. A launch-once suite that never resets
must release its own keys (UpAsync in a finally block).

PageTest  (abstract, runner-neutral)
------------------------------------
    protected PageTest(PlayTestApplication application)
    Page Page
    protected static LocatorAssertions Expect(Locator locator)

Other types
-----------
    PlayTestException          timeouts, strict-mode violations, unsupported
                               elements, application/renderer failures
    PlayTestLauncher           recorded launcher requests (LAUNCHER)
    ScreenOrientation          Landscape | Portrait
    MouseButton                Left | Right | Middle
    AriaRole                   the ARIA role names
    PlayTestOrientationAttribute, LocatorPosition, LocatorBoundingBoxResult,
    PageGetByRoleOptions, PageGetByTextOptions, PageGetByTestIdOptions,
    LocatorFilterOptions, LocatorOptions (Timeout) and its per-action
    subclasses, KeyboardPressOptions, KeyboardTypeOptions, SelectOptionValue,
    PageScreenshotOptions, LocatorScreenshotOptions
    PixelStats, PixelHalf, PixelAxis, PixelBounds, PixelDifference   (OPENGL)
    PlayTestOpenGL, PlayTestOpenGLInfo, IPlayTestOpenGLProvider,
    IPlayTestOpenGLContext, PlayTestOpenGLProviders                  (OPENGL)

Roles PlayTest assigns: Button (also split buttons), Checkbox, Combobox,
Dialog (ContentDialog), Group, Img, Link, Listbox, Menu, Menubar, Menuitem
(also submenus and MenuBarItem), Menuitemcheckbox (ToggleMenuFlyoutItem),
Option (list and combo items), Progressbar, Radio, Separator, Slider, Switch
(ToggleSwitch), Tab, Tablist, Textbox, Toolbar, Tree, Treeitem. Everything else
is Generic. Menu names exclude template arrow/check glyphs.

PREFERENCES
===========
Each preference is resolved ONCE at launch, highest priority first:

  Orientation   PlayTestOptions.Orientation (code)
                > --orientation=landscape|portrait
                > CODEBRIX_PLAYTEST_ORIENTATION=landscape|portrait
                > <CodeBrixPlayTestPreferredOrientation> in the test .csproj
                > Landscape
  Theme         PlayTestOptions.Theme (code)
                > --theme=light|dark
                > CODEBRIX_PLAYTEST_THEME=light|dark
                > <CodeBrixPlayTestPreferredTheme> in the test .csproj
                > Light
  Preview       PlayTestOptions.Headless (code)
                > --headed / --nonheadless / --headless
                > CODEBRIX_PLAYTEST_HEADED=1 (any other value: headless)
                > headless
  Action delay  PlayTestOptions.SlowMo (code, including an explicit 0)
                > CODEBRIX_PLAYTEST_SLOWMO=<milliseconds>
                > 250 ms headed, 0 headless (follows the FINAL Headless value)

Values are case-insensitive. An unset or empty environment variable falls
through to the next level; an invalid SELECTED value fails with an
ArgumentException that names its source. SlowMo uses '.' as the decimal
separator and must be finite and non-negative. The project properties are
emitted as assembly metadata, so set ConfigurationAssembly to your fixture's
assembly - a runner may use a different entry assembly.

The theme is a simulated OPERATING-SYSTEM preference: it reaches application
startup, theme resources and UISettings system colors, but an application or
element can still choose its own RequestedTheme. It never follows the real
desktop, and it is fixed for the process - test startup under both themes in
separate processes.

ORIENTATION PER TEST
====================
  - Page.SetContentAsync(() => new MainPage(), ScreenOrientation.Portrait)
    applies the orientation BEFORE the page is constructed; omitting the
    argument restores the launch preference on every reset.
  - PlayTestApplication.SetOrientationAsync(...) changes an existing page.
  - [PlayTestOrientation(ScreenOrientation.Portrait)] declares a method
    requirement; a theory row can override it with the trait
    PlayTestOrientationAttribute.CaseTraitName ("PlayTestOrientation").
    The attribute installs no hooks: your fixture resolves it with
        ScreenOrientation? PlayTestOrientationAttribute.Resolve(
            MethodInfo method, IEnumerable<string>? caseOrientations = null)
    Row > method > null (null = inherit the launch preference). Conflicting
    row values throw ArgumentException.
  - Application singletons stay alive across orientation changes; test process
    STARTUP in each orientation with separate processes.

PREVIEW (HEADED MODE)
=====================
Headless is the default. --headed (alias --nonheadless) or
CODEBRIX_PLAYTEST_HEADED=1 opens a live, resizable, VIEW-ONLY preview window in
a separate process (Windows, macOS, Linux X11/Wayland). Its proportions follow
the launch preference and never change when a test switches orientation:
opposite-orientation frames are letterboxed (bars left/right or top/bottom).
All physical keyboard, pointer, wheel and touch input to the preview is
discarded, and closing it does not stop the tests. Screenshots always contain
the virtual screen itself, never the preview bars.

COMMAND-LINE SWITCHES
=====================
With the Microsoft.Testing.Platform runner the package registers, automatically:

    dotnet test MyApp.PlayTests.csproj --headed
    dotnet test MyApp.PlayTests.csproj --nonheadless       (alias for --headed)
    dotnet test MyApp.PlayTests.csproj --headless
    dotnet test MyApp.PlayTests.csproj --nonheadless --theme=dark --orientation=portrait
    dotnet test MyApp.PlayTests.csproj --screenshotfolder="$HOME/Temp/run-1"

  - --theme accepts light|dark and --orientation landscape|portrait (any case;
    "--theme=dark" and "--theme dark" both work). They override environment
    and project preferences; explicit fixture options, per-test orientation
    requirements and application RequestedTheme choices still win.
  - Combining --headless with --headed or --nonheadless is rejected before any
    test runs.
  - The switches appear in --help. The generated xUnit/MSTest/NUnit MTP entry
    points register them; a CUSTOM MTP entry point must call its generated
    AddSelfRegisteredExtensions(builder, args).
  - When running a whole solution, unrelated test projects may reject an
    unknown switch - pass it to the PlayTest project(s) only.
  - Other runners (VSTest mode) do not get the switches; use the environment
    variables and fixture options instead.

SCREENSHOT RECORDING
====================
In xUnit v3 projects, --screenshotfolder=<path> records the whole run with no
test, fixture or project changes. The folder must ALREADY EXIST and be EMPTY
(hidden files and empty subfolders count); relative paths, absolute paths and a
leading ~/ are accepted - quote values containing spaces. Invalid or non-empty
folders fail before tests run; the recorder never empties or overwrites
anything. Use a fresh empty folder for every run and every test module.

For namespace MyApp.PlayTests.SendButton (root namespace MyApp.PlayTests),
class ClickTests, method clicking_button_adds_text:

    screenshot-index.json
    SendButton/ClickTests/clicking_button_adds_text/
      screenshot-start.png      after test setup, before the body
      screenshot-1.png          after each PlayTest action / wait / assertion
      screenshot-2.png
      screenshot-final.png      before cleanup - also when the test failed

  - Theory rows get test-case-1/, test-case-2/, ... (execution order within
    the method for that run; use the recorded IDs and arguments to identify a
    row). Re-runs of the same test get attempt-2/, ...
  - The project's RootNamespace prefix is omitted; names unsafe on Windows are
    escaped; long names and collisions get a short hash suffix.
  - One image per PlayTest API call: nested calls and retry polls do not add
    images. Changes made by background work appear at the next capture.
  - Skipped tests produce no entry; a test without a running application, or
    whose setup failed, gets metadata and a screenshotNote but no images.
  - screenshot-index.json (schemaVersion 1, replaced atomically) records the
    run (ID, status, local timestamps with UTC offsets, OS, architecture,
    runtime, time zone, machine name, process ID), the test/app/PlayTest
    assemblies, the CLI and environment preferences, the resolved
    preview/theme/orientation/delay, and per test its ID, display name,
    namespace, class, method, arguments, outcome, errors, duration and every
    PNG (kind, step, operation, locator, size, orientation, themes, and the
    calling source file/line when PDBs allow).
  - A normally finished run is "completed" even if tests failed; an
    interrupted run leaves a valid index marked "running".
  - "Zero tests ran" from dotnet test can hide an option error; run the test
    DLL directly (dotnet exec MyApp.PlayTests.dll --screenshotfolder=...) to
    see the diagnostic.

SCRIPTED FILE AND FOLDER PICKERS
================================
Queue the answer BEFORE the action that opens the application's normal
Windows.Storage.Pickers picker. No dialog is ever displayed, headed or not.

    app.FilePickers.EnqueueFolder(string? path)
    app.FilePickers.EnqueueOpenFile(string? path)
    app.FilePickers.EnqueueOpenFiles(params string[]? paths)
    app.FilePickers.EnqueueSaveFile(string? path)
    app.FilePickers.EnqueueFolderFailure(Exception error)
    app.FilePickers.EnqueueOpenFileFailure(Exception error)   (single and multiple)
    app.FilePickers.EnqueueSaveFileFailure(Exception error)
    app.FilePickers.Clear()                    queues AND request history
    FolderRequestCount, OpenFileRequestCount, SaveFileRequestCount,
    LastSuggestedFileName

  - Separate FIFO queues for folder, open-file (single and multiple share it)
    and save-file. Null (or an empty list) = the user cancelled: single pickers
    return null, multiple pickers an empty list.
  - Paths become absolute when queued. Folders and open-files must exist when
    the picker opens; a save path's parent folder must exist. A NEW save path
    creates an empty file (like a native picker); an EXISTING file is returned
    intact, never truncated.
  - A queued failure makes that picker throw the given exception (in queue
    order with the other answers), so an application's "picker failed" or
    "pickers not supported" branch runs; it counts as a request.
  - An unqueued picker request throws PlayTestException - it never silently
    cancels. A single-file picker rejects a multi-file response.
  - SetContentAsync does not clear the queues: call FilePickers.Clear() in
    your per-test reset.

LAUNCHER
========
PlayTest registers its own launcher, as every desktop head does, so
Windows.System.Launcher (a HyperlinkButton's NavigateUri, LaunchUriAsync with
an https:, mailto: or file: URI for a document or folder) NEVER starts a
browser, viewer or other process. Every request is recorded:

    app.Launcher.LaunchedUris              IReadOnlyList<Uri>, in order
    app.Launcher.QueriedUris               QueryUriSupportAsync requests
    app.Launcher.LaunchResult              returned by LaunchUriAsync (true)
    app.Launcher.QueryUriSupportResult     returned by QueryUriSupportAsync
                                           (Available)
    app.Launcher.Clear()                   history AND results

    await Page.GetByRole(AriaRole.Link, new() { Name = "Help" }).ClickAsync();
    await app.WaitForAsync(() => app.Launcher.LaunchedUris.Count, n => n == 1);
    Assert.Equal(new Uri("https://example.com/help"), app.Launcher.LaunchedUris[0]);

Set LaunchResult = false to reach an application's "could not open" branch.
Call Launcher.Clear() in your per-test reset.

WINDOW ACTIONS
==============
    bool closed = await app.RequestCloseAsync();
        As the window's close button: AppWindow.Closing handlers run and may
        cancel (a "save changes?" prompt), then Window.Closed handlers run and
        may cancel by marking the event handled. Returns false when the
        application cancelled. When the close goes ahead the window is hidden
        (Window.VisibilityChanged) but the process and the application keep
        running; the next Page.SetContentAsync shows the window again.
    await app.MinimizeAsync();  await app.RestoreAsync();
        As the Linux desktop heads report it: minimizing deactivates the
        window (Window.Activated with Deactivated) and hides it
        (Window.VisibilityChanged, Visible false), so pause-on-hidden logic
        runs; restoring makes it visible and active again. Rendering and
        screenshots continue while minimized. Page.SetContentAsync restores a
        minimized window.
The window is the application's own: app.Window (read it in EvaluateAsync) is
the Window the application created, with the content it set - no test-side
App subclass is needed to reach it.

SCREENSHOTS
===========
    await Page.ScreenshotAsync(new() { Path = "page.png", Stable = true });
    await Page.GetByTestId("Chart").ScreenshotAsync(new() { Path = "chart.png" });

  - Element screenshots are cropped to the element's bounds in whole pixels,
    clipped to the virtual screen.
  - Stable = true repeats the capture until two consecutive captures are
    identical (transitions and animations have settled), failing at the
    timeout. An endless animation (a spinner, an animated player) inside the
    captured area never settles: capture an area without it.

CHECKED CONTROLS, SCROLLING, EDITORS, MENUS AND DRAGS
=====================================================
  - CheckAsync / UncheckAsync / SetCheckedAsync support CheckBox, RadioButton,
    ToggleButton, ToggleSwitch (by its thumb), ToggleMenuFlyoutItem and any
    IToggleProvider peer. They click with real pointer input only when the
    state must change, then wait for the requested state. A checked
    RadioButton cannot be unchecked by clicking, so UncheckAsync on it times
    out. A toggle menu item's result is verified even after its flyout closes.
  - ScrollIntoViewIfNeededAsync brings an ATTACHED element into its
    ScrollViewer's viewport. Virtualized, not-yet-realized items must first be
    materialized by scrolling their container - or, for a selector, use
    SelectOptionAsync, which needs no realized rows.
  - FillAsync replaces the whole value (TextBox, PasswordBox, IValueProvider;
    read-only providers are waited on, not written). PressSequentiallyAsync and
    Keyboard.TypeAsync send real key events, so completion, indentation and
    shortcuts run. Focus already inside a composite editor's subtree is kept.
  - Input values report LF line endings (TextBox internally uses CR).
  - Hover, positioned / right / double / triple clicks and stepped drags use
    the same strictness, bounds and hit-test checks as ClickAsync. Positions
    must lie inside the element.

WEBVIEW CONTENT
===============
An application that references the CodeBrix.Platform.WebView add-in can be
tested with its browser rendered offscreen and composited into the virtual
screen (including screenshots and the preview), with XAML pointer and keyboard
input: WPE WebKit on Linux (system libraries required), Edge WebView2 on
Windows (installed runtime required), WKWebView on macOS through the add-in's
offscreen helper. PlayTest adds no browser dependency of its own and offers no
DOM locators or browser download, upload or script-dialog API.

Downloads started by real input do work: on Linux (WPE WebKit), a PlayTest
pointer click on a download link in the page raised the WebView's
DownloadStarting event, the application's download policy ran, and an
accepted download completed to the policy's target path (verified against a
loopback HTTP server). This has been verified on Linux only; Windows (Edge
WebView2) and macOS (WKWebView) downloads have not been tried.

OPENGL
======
An application that uses the Graphics3DGL add-in (GLCanvasElement,
SkiaGLCanvasElement, OffscreenGLContext, SkiaGpuContext - also the VideoPlayer
add-in's GPU path) gets real OpenGL contexts when the test project references
CodeBrix.Platform.PlayTest.OpenGL.ApacheLicenseForever and registers it before
the launch:

    using CodeBrix.Platform.PlayTest.OpenGL;

    CodeBrixPlayTestOpenGL.Register();
    Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
    {
        ConfigurationAssembly = typeof(AppFixture).Assembly,
    });

From then on OpenGL is available to every test, as on a desktop head: GL
elements initialize, render on the application's UI thread, and their pixels
are in every screenshot and in the preview. This package itself adds no OpenGL
code and no native dependency; without the provider package there is no OpenGL.

Per operating system (the provider package's AGENT-README has the detail):
  Linux     Mesa EGL, surfaceless platform; needs libegl1 and libgl1-mesa-dri
            (apt install libegl1 libgl1-mesa-dri). Mesa renders on the GPU's
            render node when there is one, else on llvmpipe. No display needed.
  Windows   WGL on opengl32.dll (a hidden window), as the Win32 head does: an
            interactive desktop session with a GPU OpenGL driver; on Windows on
            ARM, Microsoft's OpenCL and OpenGL Compatibility Pack.
  macOS     ANGLE on Metal, as the macOS head does: a Mac with a Metal device.
  Vulkan is not supported.

Opting out. A launch with PlayTestOptions.OpenGL = PlayTestOpenGL.Unavailable
has no OpenGL, exactly as if the provider were never registered: GL elements
report that initialization failed (GLCanvasElement.GetGLInitializationState(),
SkiaGLCanvasElement.IsGpuInitialized == false) and the application's own
fallback runs. A registered provider is then left alone. OpenGL is a LAUNCH
option, for the whole run: GLCanvasElement keeps its context for the life of
the window. Test an application's no-OpenGL path in a process of its own (a
second fixture/test project that launches with Unavailable).

Both failures are loud; nothing is skipped:
  - A registered provider that cannot create a context on this machine makes
    LaunchAsync throw a PlayTestException naming the provider and the concrete
    reason (a missing library, no driver, OpenGL older than 3.0, no Metal ...).
  - With NO provider registered (and OpenGL not Unavailable), the first OpenGL
    element the application initializes fails the test with
        "The application initialized an OpenGL element, but no OpenGL provider
        is registered - reference CodeBrix.Platform.PlayTest.OpenGL.
        ApacheLicenseForever and call CodeBrixPlayTestOpenGL.Register() ..."
    The elements themselves catch every error and only report "initialization
    failed", so PlayTest records this as the run's failure: the PlayTest call
    during which the element loaded (typically the click or SetContentAsync that
    showed it) and every later call throw it. An application that never
    initializes a GL element is not affected.

PlayTestApplication.OpenGL (PlayTestOpenGLInfo), fixed at launch:
    bool IsAvailable          real contexts in this run
    string? Provider          the registered provider's name
    string? Renderer          GL_RENDERER of the launch's probe context
    string? Version           GL_VERSION of the probe context
    bool IsGles               OpenGL ES (Linux, macOS) or desktop OpenGL (Windows)
    bool IsSoftware           a CPU rasterizer (llvmpipe, SwiftShader, ...)
    string? UnavailableReason why there is no OpenGL, else null
Both branches of an application can be asserted from it (if IsAvailable,
expect the 3D view; else expect the fallback) - neither skips.

How to assert GL content. GL pixels have no visual tree. Measure the pixels the
running test captured - Locator.ScreenshotAsync or Page.ScreenshotAsync bytes -
with PixelStats:

    var canvas = PixelStats.FromPng(await Page.GetByTestId("Viewport")
        .ScreenshotAsync(new() { Stable = true }));

    PixelStats.FromPng(byte[] png)            the only way in: bytes from this run
    Width, Height, GetPixel(x, y), Region(x, y, w, h), Half(PixelHalf)
    double Coverage(Color, tolerance = 8)     fraction of pixels near a colour
    int DistinctColorCount; bool IsUniform    one colour only (all black, all
                                              magenta ...)
    bool IsBlank                              all transparent or black: nothing
                                              was drawn or read back
    PixelBounds? Bounds(Color background, tolerance = 8)
                                              box around everything else
    (double X, double Y)? Centroid(Color background, tolerance = 8)
    double MeanLuminance(Color? background = null, tolerance = 8)
                                              compare Half(Left) with Half(Right)
                                              for the lighting direction
    double LuminanceVariance()                shading / texture present
    double MirrorSymmetry(PixelAxis, tolerance = 8)   1 = perfect mirror image
    PixelDifference Difference(PixelStats other, tolerance = 0)
                                              two captures of the SAME test, e.g.
                                              before and after a drag or a zoom:
                                              ChangedFraction, MaxChannelDelta
Colours are unpremultiplied RGBA compared per channel; luminance is Rec. 709
(0-255). Typical checks: the element initialized; the region is not blank and
not uniform; the object covers the expected share at the expected place
(bounds, centroid); the lit side is brighter; an input changed the picture (and
zoom changed the coverage). Use tolerances - Linux may render on llvmpipe or a
GPU, Windows on its driver, macOS on Metal. PixelStats reads only the bytes it
is given: no files, no saved images, no baselines. When analysis cannot decide
whether something "looks right", assert the structure and leave the looks to
the screenshots (Path / --screenshotfolder) for a person to review.

A single capture after an action already shows the GL frame that action caused
(the capture renders twice with a UI-thread barrier between, and the elements
render inside it); Stable = true additionally waits out animation.

The provider seam (public, for the provider package; tests never call it):
IPlayTestOpenGLProvider (Name, CreateContext(), Dispose), IPlayTestOpenGLContext
(IsGles, GetProcAddress returning 0 for absent names, MakeCurrent returning a
scope that restores the previous context), PlayTestOpenGLProviders.Register /
IsRegistered (one provider per process; registering the same type again does
nothing). Contexts are created and made current on the application's UI thread;
the head disposes the provider when the application is disposed.

COMPLETE EXAMPLES
=================

1. xUnit v3 fixture, per-test reset and a test class
----------------------------------------------------
    using System;
    using System.Threading.Tasks;
    using CodeBrix.Platform.PlayTest;
    using MyApp.Views;
    using Xunit;

    [assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

    namespace MyApp.PlayTests;

    public sealed class AppFixture : IAsyncLifetime
    {
        public PlayTestApplication Application { get; private set; }

        public async ValueTask InitializeAsync() =>
            Application = await PlayTestApplication.LaunchAsync(() => new App(), new()
            {
                ConfigurationAssembly = typeof(AppFixture).Assembly,
            });

        public Task ResetAsync(ScreenOrientation? orientation = null)
        {
            Application.FilePickers.Clear();
            Application.Launcher.Clear();
            return Application.Page.SetContentAsync(() => new MainPage(), orientation);
        }

        public async ValueTask DisposeAsync()
        {
            if (Application != null) await Application.DisposeAsync();
        }
    }

    [CollectionDefinition("App")]
    public sealed class AppCollection : ICollectionFixture<AppFixture> { }

    [Collection("App")]
    public sealed class SendTests(AppFixture fixture) : PageTest(fixture.Application), IAsyncLifetime
    {
        public async ValueTask InitializeAsync()
        {
            var test = (Xunit.v3.IXunitTest)TestContext.Current.Test;
            test.Traits.TryGetValue(PlayTestOrientationAttribute.CaseTraitName, out var rows);
            await fixture.ResetAsync(PlayTestOrientationAttribute.Resolve(test.TestMethod.Method, rows));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        [Fact]
        public async Task Sending_shows_the_status()
        {
            await Page.GetByRole(AriaRole.Textbox, new() { Name = "Message" }).FillAsync("Hello");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Send", Exact = true }).ClickAsync();
            await Expect(Page.GetByTestId("status")).ToHaveTextAsync("Sent");
        }

        [Theory]
        [PlayTestOrientation(ScreenOrientation.Landscape)]
        [InlineData("portrait row", Traits = new[] { "PlayTestOrientation", "Portrait" })]
        [InlineData("landscape row")]                  // inherits the method requirement
        public async Task Layout_fits(string scenario)
        {
            var box = await Page.GetByTestId("status").BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box.X + box.Width <= fixture.Application.Width, scenario);
        }
    }

Resolve the EXECUTING test's traits in InitializeAsync, as above - not the
discovery test case's traits, which may group deferred data rows.

2. Pickers, keyboard, clipboard and a non-visual outcome
--------------------------------------------------------
    var app = fixture.Application;
    app.FilePickers.EnqueueOpenFile(inputPath);
    await Page.GetByRole(AriaRole.Button, new() { Name = "Open…", Exact = true }).ClickAsync();
    await Expect(Page.GetByTestId("FileName")).ToHaveTextAsync("input.txt");

    var editor = Page.GetByRole(AriaRole.Textbox, new() { Name = "Source editor", Exact = true });
    await editor.FillAsync("first value");
    await editor.FillAsync("second value");
    await editor.PressAsync("Control+z");        // an editor with undoable Fill, e.g. AdvancedTextEdit
    await Expect(editor).ToHaveValueAsync("first value");

    await Page.GetByRole(AriaRole.Button, new() { Name = "Copy", Exact = true }).ClickAsync();
    Assert.Equal("first value", await Page.ClipboardTextAsync());   // isolated clipboard

    app.FilePickers.EnqueueSaveFile(outputPath);
    await Page.GetByRole(AriaRole.Button, new() { Name = "Save…", Exact = true }).ClickAsync();
    await app.WaitForAsync(() => File.Exists(outputPath) && new FileInfo(outputPath).Length > 0,
        written => written, description: "the saved file");

3. Menus, typed locators, drags and screenshots
-----------------------------------------------
    await Page.GetByRole(AriaRole.Menuitem, new() { Name = "View", Exact = true }).ClickAsync();
    await Page.GetByRole(AriaRole.Menuitemcheckbox, new() { Name = "Word wrap" }).CheckAsync();
    await Page.GetByType<MySplitter>().DragByAsync(80, 0);
    await Page.GetByRole(AriaRole.Listbox).GetByText("Item 3").ClickAsync(
        new() { Button = MouseButton.Right, Position = new() { X = 10, Y = 5 } });
    await Page.ScreenshotAsync(new() { Path = "TestResults/PlayTest/after-drag.png" });

MINIMUM VIABLE PROJECT TEMPLATE
===============================
tests/MyApp.PlayTests/MyApp.PlayTests.csproj - it compiles the application's
shared .UI sources and references its .Core library, exactly like a head:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <OutputType>Exe</OutputType>
        <IsPackable>false</IsPackable>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
        <!-- Match the application's own settings: its shared .UI sources
             compile into this project. -->
        <Nullable>disable</Nullable>
        <ImplicitUsings>disable</ImplicitUsings>
        <!-- Optional project preferences (see PREFERENCES). -->
        <CodeBrixPlayTestPreferredOrientation>Landscape</CodeBrixPlayTestPreferredOrientation>
        <CodeBrixPlayTestPreferredTheme>Light</CodeBrixPlayTestPreferredTheme>
      </PropertyGroup>
      <Import Project="../../src/MyApp.UI/MyApp.UI.projitems" Label="Shared" />
      <ItemGroup>
        <ProjectReference Include="../../src/MyApp.Core/MyApp.Core.csproj" />
        <PackageReference Include="CodeBrix.Platform.PlayTest.ApacheLicenseForever" Version="..." />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="..." />
        <PackageReference Include="xunit.v3" Version="..." />
        <PackageReference Include="xunit.runner.visualstudio" Version="..." PrivateAssets="all" />
      </ItemGroup>
    </Project>

global.json beside the solution:

    { "test": { "runner": "Microsoft.Testing.Platform" } }

Use the same PlayTest version as the application's CodeBrix.Platform packages
(the whole family ships together). Do not add a desktop head package to the
test project.

PERFORMANCE TIPS
================
  - Launch ONE application per test process and reset the page per test
    (SetContentAsync) instead of relaunching.
  - Prefer assertions and WaitForAsync to fixed delays: they return as soon as
    the condition holds. Never Task.Delay to "let the UI settle".
  - Leave SlowMo unset: headless runs then have no action delay; headed runs
    get 250 ms so a human can follow along (CODEBRIX_PLAYTEST_SLOWMO=0 for full
    speed while watching).
  - GetByTestId is the cheapest, least ambiguous locator; give important
    controls an AutomationProperties.AutomationId.
  - Recording costs one PNG per API call; enable it only for runs you will
    inspect.

COMMON PITFALLS TO AVOID
========================
  - Running PlayTests in parallel. The application, its singletons and the
    preferences are per process: serialize the assembly
    ([assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)])
    and share one fixture.
  - Calling LaunchAsync twice in one process (InvalidOperationException). Use
    separate test projects or processes for independent applications.
  - Assigning PlayTestOptions.Headless / Theme / Orientation / SlowMo "just in
    case" in a fixture: an assigned value overrides the command line and the
    environment. Leave them unset.
  - Forgetting ConfigurationAssembly = typeof(YourFixture).Assembly: under some
    runners the entry assembly is not the test assembly, and the project
    preferences are then ignored.
  - Ambiguous locators ("Strict mode violation"): use Exact = true, a test ID,
    a scoped locator, Filter(...) or Nth(...).
  - Setting control state directly (IsChecked, Text, command.Execute) instead
    of using actions - that skips the input pipeline the test is meant to
    prove. Use CheckAsync, FillAsync, ClickAsync.
  - Asserting with a one-shot read (IsVisibleAsync, CountAsync) right after an
    action. Use Expect(...) assertions, which retry.
  - Pressing keys a game loop never sees: use Keyboard.DownAsync/UpAsync or
    PressAsync with a Delay (see Keyboard and Mouse).
  - Reading a hidden element with GetByText/GetByTestId/GetByLabel: they match
    visible elements only; pass IncludeHidden = true, or read the state with
    EvaluateAsync.
  - Clicking links that open a browser: they do not - read app.Launcher.
  - Leaving picker responses queued between tests: call FilePickers.Clear() in
    the per-test reset. An unqueued picker request fails the test. Clear the
    launcher history (Launcher.Clear()) there too.
  - Reading the desktop clipboard: the application's clipboard is isolated;
    read it with Page.ClipboardTextAsync().
  - Expecting Meta/Command shortcuts on macOS: the virtual head is
    Control-based everywhere ("ControlOrMeta" is Control).
  - Comparing TextBox values with CR line endings: values report LF.
  - Reusing a --screenshotfolder: it must be empty every time.
  - Expecting a test-orientation change to resize the preview window: it
    letterboxes instead.

WHAT THIS PACKAGE DOES NOT DO
=============================
  - No browser, DOM, CSS or JavaScript, network routing, browser contexts,
    tracing or a download API - it tests CodeBrix.Platform XAML apps.
  - One window and one application per process; no multiple windows.
  - No OpenGL without CodeBrix.Platform.PlayTest.OpenGL (see OPENGL); no Metal
    or Vulkan surfaces; no native dialogs other than the scripted storage
    pickers, no horizontal mouse wheel.
  - No physical input: the preview window is view-only.
  - No pixel-diff / visual-regression comparison: ScreenshotAsync and the
    recorder produce PNGs and are never compared with saved images. PixelStats
    measures captures taken in the running test only.
  - No real window close, minimize or process exit: RequestCloseAsync and
    MinimizeAsync raise the window events; the application keeps running.
  - No automatic key repeat while a key is held.
  - No automatic orientation-attribute hooks: your fixture applies
    PlayTestOrientationAttribute.Resolve (example 1).
  - Screenshot recording only under the reflection-based xUnit v3 runner;
    other MTP runners reject --screenshotfolder with an explanation.

WORKING EXAMPLES ON GITHUB
==========================
  - PlayTestDemo - a dedicated six-head demo application and its PlayTests
    project: checked controls, scrolling, every picker kind, cancellation and
    failure, method/row orientation, typed locators, editors, menus, tool
    bars, split buttons, hover/positioned clicks and drags, and an API lab
    (held keys, hidden elements, strictness while waiting, SelectOptionAsync,
    dialogs, the launcher, window close/minimize, element and stable
    screenshots), and an OpenGL page measured with PixelStats (plus
    tests/PlayTestDemo.NoOpenGL.PlayTests, which launches without OpenGL):
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/samples/CodeBrixPlatform/PlayTestDemo
    Fixture: tests/PlayTestDemo.PlayTests/AppFixture.cs
  - Host-free tests of the preferences, command-line switches, orientation
    resolution and recording folder rules:
    https://github.com/ellisnet/CodeBrix.Platform/tree/main/src/Platform.UI.Runtime.Skia.PlayTest.Tests
  - Application suites (one PlayTests project per application, sharing a
    fixture in PlayTestSupport/):
    https://github.com/ellisnet/CodeBrix.Samples

QUICK REFERENCE CARD
====================
Package     CodeBrix.Platform.PlayTest.ApacheLicenseForever (test projects only)
Using       using CodeBrix.Platform.PlayTest;
Launch      app = await PlayTestApplication.LaunchAsync(() => new App(),
                new() { ConfigurationAssembly = typeof(AppFixture).Assembly });
Reset       app.FilePickers.Clear(); app.Launcher.Clear();
            await app.Page.SetContentAsync(() => new MainPage(), orientation);
Locate      Page.GetByRole(AriaRole.Button, new() { Name = "Send", Exact = true })
            Page.GetByTestId("id") / GetByText("t") / GetByLabel("l") / GetByType<T>()
            .First .Last .Nth(i) .Filter(new() { HasText = "x" }) .GetByRole(...) (scoped)
Act         ClickAsync(new() { Button, Position, ClickCount }) HoverAsync DragByAsync(dx, dy)
            FillAsync PressAsync("Control+z") PressSequentiallyAsync("text\n")
            CheckAsync UncheckAsync SetCheckedAsync ScrollIntoViewIfNeededAsync
            SelectOptionAsync("Row 24") ScreenshotAsync(new() { Stable = true })
Keys        Keyboard.DownAsync("ArrowRight") / UpAsync(...) / PressAsync(k, new() { Delay = 150 })
Assert      await Assertions.Expect(l).ToHaveTextAsync("x")   (.Not, ToBeVisibleAsync,
            ToBeHiddenAsync, ToBeEnabledAsync, ToBeDisabledAsync, ToBeCheckedAsync,
            ToHaveCountAsync, ToHaveValueAsync, ToContainTextAsync)
Read        InputValueAsync InnerTextAsync IsCheckedAsync BoundingBoxAsync CountAsync
State       await app.EvaluateAsync(() => vm.Count); await app.WaitForAsync(probe, ok)
Pickers     app.FilePickers.EnqueueFolder/EnqueueOpenFile(s)/EnqueueSaveFile(path | null)
            app.FilePickers.Enqueue{Folder|OpenFile|SaveFile}Failure(exception)
Launcher    app.Launcher.LaunchedUris (no process is started); app.Launcher.Clear()
Window      app.Window; await app.RequestCloseAsync() / MinimizeAsync() / RestoreAsync()
Screen      1920x1080 Landscape | 1080x1920 Portrait; app.SetOrientationAsync(...)
Prefs       code > --switch > CODEBRIX_PLAYTEST_* > <CodeBrixPlayTestPreferred*> > default
CLI         --headed | --nonheadless | --headless  --theme=light|dark
            --orientation=landscape|portrait  --screenshotfolder=<empty folder>
Env         CODEBRIX_PLAYTEST_HEADED=1  CODEBRIX_PLAYTEST_SLOWMO=<ms>
            CODEBRIX_PLAYTEST_THEME  CODEBRIX_PLAYTEST_ORIENTATION
Timeout     10 s default; Page.SetDefaultTimeout(ms); per call new() { Timeout = ms }
OpenGL      CodeBrix.Platform.PlayTest.OpenGL: CodeBrixPlayTestOpenGL.Register() before launch;
            app.OpenGL (info); new() { OpenGL = PlayTestOpenGL.Unavailable } (opt out)
Pixels      PixelStats.FromPng(await locator.ScreenshotAsync()) .Coverage .Bounds .Centroid
            .Half(PixelHalf.Left).MeanLuminance(bg) .MirrorSymmetry .Difference(before)
Failure     PlayTestException with a screenshot path + visible-UI description
            (TestResults/PlayTest by default)
Rules       one app per process; serialized tests; real input only
