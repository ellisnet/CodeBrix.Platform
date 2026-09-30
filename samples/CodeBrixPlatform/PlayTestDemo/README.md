# PlayTestDemo

A dedicated application for exercising CodeBrix.Platform.PlayTest controls and
interaction contracts. The same XAML runs in six desktop heads and the PlayTest
head. It contains a checkbox, a toggle switch, a nested scrolling target, and
folder, single-file, multiple-file and save-path pickers. Status text shows the
results and event counts. File opening reads text; choosing a save path does not
write or replace file contents.

## Projects

- `src/PlayTestDemo.Core`: view model and shared framework/font references.
- `src/PlayTestDemo.UI`: shared App and MainPage XAML, compiled into each head.
- `src/PlayTestDemo.{LinuxX11,LinuxWayland,LinuxFrameBuffer,Win32Skia,WinWpfSkia,MacOS}`:
  ordinary interactive desktop applications.
- `tests/PlayTestDemo.PlayTests/PlayTestDemo.PlayTests.csproj`: executable xUnit v3
  tests with a direct SilverAssertions reference. Nullable and implicit usings
  are disabled throughout the sample.

Open the solution for your OS: `PlayTestDemo.Linux.slnx`,
`PlayTestDemo.Windows.slnx`, or `PlayTestDemo.MacOS.slnx`.

As with the other framework-repository samples, framework references build from
source. No prerelease feed, packed PlayTest package, or CodeBrix.Samples checkout
is needed. `PlayTestDemo.Head.targets` supplies the source-based XAML generator
and Skia runtime replacement wiring shared by these heads. The macOS and
FrameBuffer heads also import the repository's existing head-specific targets.

## Run

From this directory:

```sh
# Normal, interactive X11 application, using the desktop theme and native pickers.
dotnet run --project src/PlayTestDemo.LinuxX11 -c Release

# Offscreen tests: Landscape and Light unless explicitly overridden.
dotnet test --project tests/PlayTestDemo.PlayTests/PlayTestDemo.PlayTests.csproj -c Release

# Visible, view-only preview on a desktop; no picker dialogs appear.
CODEBRIX_PLAYTEST_HEADED=1 CODEBRIX_PLAYTEST_ORIENTATION=landscape \
CODEBRIX_PLAYTEST_THEME=dark \
dotnet test --project tests/PlayTestDemo.PlayTests/PlayTestDemo.PlayTests.csproj -c Release
```

The environment assignment syntax above is for bash/zsh. On PowerShell, set the
corresponding `$env:CODEBRIX_PLAYTEST_*` variables before `dotnet test`.
Headed runs default to 250 ms between actions. Set `CODEBRIX_PLAYTEST_SLOWMO`
to another delay, including `0`, to override it. Headless runs default to zero.
Use the relevant desktop head on other operating systems; the PlayTests project
is the same on all of them. FrameBuffer requires a device or an emulator; it is
not a normal desktop preview.

## Coverage and adding tests

The 19 UI cases include 15 control/picker regression cases transferred from
JustBetweenUs.PlayTests, plus four method/case orientation examples:

- Checkbox and toggle-switch input, checked-state assertions, and idempotence.
- Scrolling an initially clipped button into view and clicking it.
- Folder, open, multiple-open and save selections and cancellation.
- Ordered multi-file results, actual text reads, existing-file preservation,
  invalid-path and unscripted-picker diagnostics, FIFO responses and queue reset.
- Forced landscape and portrait at both method and theory-row level.

`SlowMoPreferenceTests` adds 19 configuration cases for delay defaults,
environment/code overrides, explicit zero, culture and invalid values.

The fixture launches one application and installs a fresh **demo MainPage** for
each serialized test. It clears picker responses, resolves orientation traits,
and removes its own temporary files at teardown. Tests queue paths before
clicking the application's real picker buttons; they verify visible status and
view-model/file outcomes. The application handles picker errors by displaying
their type/message, so a failing picker remains inspectable in the preview.

Put new demonstration UI in the shared application, and its regression cases in
this test project. Application-specific suites in CodeBrix.Samples should stay
focused on their own screens. Controlled data or service implementations for an
application's real workflow belong in that application's fixture.

The test project supplies `CodeBrixPlayTestPreferredOrientation` and
`CodeBrixPlayTestPreferredTheme` defaults. Environment variables override those
defaults. Explicit launch orientation wins over the orientation environment
variable; an individual test/row orientation wins for that test. Opposite
orientations are letterboxed in a stable preview window. Virtual screen sizes
remain 1920×1080 or 1080×1920, irrespective of preview resizing.

Snapshots are written below `TestResults/PlayTest` in the test process directory.
See the [PlayTest API guide](../../../src/Platform.UI.Runtime.Skia.PlayTest/README.md)
for configuration, locators, assertions, supported controls and limitations.

## Validation

On Linux x64 / X11:

- All 19 UI cases passed in offscreen landscape/light and visible portrait/dark,
  including the opposite-orientation rows in the letterboxed preview.
- The normal X11 application launched and its desktop layout was inspected.
- Linux, Windows and macOS solutions compiled in Release with zero warnings
  and errors. Windows/macOS were compile checks on Linux; their native runtime
  behavior, native Wayland and FrameBuffer still need their respective hosts.
- JustBetweenUs's remaining 80 cases passed after the move. The other five
  CodeBrix.Samples suites were inspected and contained no artificial screens to
  transfer; their application-specific coverage remains in place.

After adding the delay preferences, all 38 cases passed headless, headed with
`CODEBRIX_PLAYTEST_SLOWMO` unset (the new 250 ms default), and headed with an
explicit zero. Fixture `SlowMo` overrides remain supported.
