# PlayTestDemo

A dedicated application for exercising CodeBrix.Platform.PlayTest controls and
interaction contracts. The same XAML runs in six desktop heads and the PlayTest
head. It contains a checkbox, a toggle switch, a nested scrolling target, and
folder, single-file, multiple-file and save-path pickers. Status text shows the
results and event counts. File opening reads text; choosing a save path does not
write or replace file contents.

“Try desktop controls” opens a shared interactive screen with AdvancedTextEdit,
nested menus, toolbar commands, split buttons, checked items, overflow and
read-only editing. The demo application references the CommandBar and
AdvancedTextEdit add-ins; the PlayTest package does not. `DesktopControlTests.cs`
covers these interactions through typed/role locators and real input, including
failure paths, undo, composite focus and empty completion lists.

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
dotnet test --project tests/PlayTestDemo.PlayTests/PlayTestDemo.PlayTests.csproj -c Release --headed
```

`--nonheadless` is an alias for `--headed`; `--headless` suppresses the preview.
These switches work on Windows, macOS and Linux, overriding
`CODEBRIX_PLAYTEST_HEADED`. Explicit `PlayTestOptions.Headless` in a fixture wins
over both. The test project automatically registers these options through the
same PlayTest targets used by NuGet consumers. `--theme=dark|light` and
`--orientation=portrait|landscape` (case-insensitive) override environment/project
preferences while retaining explicit fixture/test requirements. Add
`--screenshotfolder="/absolute/path/to/an/existing empty folder"` for automatic
start/step/final PNGs and a root `screenshot-index.json`; no UI test edits are needed.
The [recording guide](../../../src/Platform.UI.Runtime.Skia.PlayTest/README.md#automatic-screenshot-recording)
describes folder layout, metadata and supported operation boundaries.
Headed runs default to 250 ms between actions. Set `CODEBRIX_PLAYTEST_SLOWMO`
to another delay, including `0`, to override it. Headless runs default to zero.
Use the relevant desktop head on other operating systems; the PlayTests project
is the same on all of them. FrameBuffer requires a device or an emulator; it is
not a normal desktop preview.

## Coverage and adding tests

The 20 UI cases include 15 control/picker regression cases transferred from
JustBetweenUs.PlayTests, four method/case orientation examples, and one check that
the requested preview mode reaches the running application:

- Checkbox and toggle-switch input, checked-state assertions, and idempotence.
- Scrolling an initially clipped button into view and clicking it.
- Folder, open, multiple-open and save selections and cancellation.
- Ordered multi-file results, actual text reads, existing-file preservation,
  invalid-path and unscripted-picker diagnostics, FIFO responses and queue reset.
- Forced landscape and portrait at both method and theory-row level.

`SlowMoPreferenceTests` adds 19 configuration cases for delay defaults,
environment/code overrides, explicit zero, culture and invalid values.
`PreviewCommandLineTests` adds 12 cases for command-line/environment precedence,
the `--nonheadless` alias, delay defaults, explicit fixture overrides and conflicting
options. `DisplayCommandLineTests` adds 10 cases for theme/orientation precedence,
invalid values, and empty-directory validation, for **61 cases total**. Configuration tests restore process settings so
they can also run inside a suite launched with an explicit preview switch.

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

The Windows native-preview check is available from the repository root:

```powershell
./build/test-scripts/playtest-preview-windows.ps1 `
  -TestOutput ../CodeBrix.Samples/JustBetweenUs/CodeBrixPlatform/tests/JustBetweenUs.PlayTests/bin/Release/net10.0 `
  -Artifacts TestResults/PlayTestPreview-Windows
```

Build the sample test output first. The check opens only its own preview windows,
captures their client areas, and validates eight alternating landscape/portrait
frames with stable window sizes and centered letterboxing. Four additional checks
cover clean EOF, invalid dimensions, a truncated header and truncated pixels.
It uses PowerShell 7 and Windows' drawing APIs, with no Python imaging dependency.
Keep the preview windows unobscured while the capture check runs.

On Windows x64, 2026-09-30, all 38 cases passed in each of headless/light
landscape, headless/light portrait, headed/dark landscape and headed/dark
portrait: 152 executions, zero failures or skips. Headed runs used the Windows
SDL driver and the default 250 ms action delay. The native-preview check above
also passed all eight frame/orientation checks and four protocol checks.
The six CodeBrix.Samples application suites passed the same matrix using local
preview `.12`, for 676 passing executions across both repositories.

The macOS native-preview check is available from the repository root:

```sh
python3 build/test-scripts/playtest-preview-macos.py \
  --test-output samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests/bin/Release/net10.0 \
  --artifacts TestResults/PlayTestPreview-macOS
```

Build the test project first. This checker requires macOS 14+, Apple's command-line
tools, and Screen Recording permission for the invoking terminal or agent. It
captures only the preview processes it starts, using ScreenCaptureKit, and needs
no Python imaging package. Application tests and their virtual screenshots do
not need Screen Recording permission.

On Intel macOS 15.8, 2026-09-30, all eight native frame/orientation checks and four
protocol checks passed. Both 960x540 and 540x960 client areas retained their sizes
through alternating landscape/portrait frames, with centered black letterboxing.

All 38 PlayTestDemo cases also passed in each of headless/light landscape,
headless/light portrait, headed/dark landscape and headed/dark portrait:
152 executions, zero failures or skips. Headed runs used Cocoa and the default
250 ms action delay. The six CodeBrix.Samples suites passed that same matrix with
local preview `.16`, for 684 passing executions across both repositories.

After adding the command-line preview switches, all 51 cases passed on this Intel
Mac in each of four launches: `--headless` with default environment, `--headed`
with `CODEBRIX_PLAYTEST_HEADED=0`, `--nonheadless` with that same environment, and
`--headless` with `CODEBRIX_PLAYTEST_HEADED=1`. The application-level case checked
the actual running host's mode. Conflicting flags were rejected before execution;
the direct test executable printed the conflict diagnostic. Package consumers
also passed with the `.17` adapter; see CodeBrix.Samples/PlayTestSupport/README.md.

### Automatic recording regression harness

From the repository root, build and run the separate checker:

```sh
dotnet build build/test-scripts/PlayTestRecordingProbe/PlayTestRecordingProbe.csproj -c Release
python3 build/test-scripts/playtest-recording.py
```

The probe deliberately contains one failing and one skipped test. The Python
checker verifies the expected runner result, final capture after failure, nested
namespace/theory folders, PNG signatures/dimensions/pixel changes, source locations,
metadata/outcomes, CLI precedence, and rejected destination folders/values. The
checker succeeds only when these contracts hold. Its negative cases are separate
from this demo's normal passing suite.


On Intel macOS (2026-09-30), all 61 cases passed with CLI Dark/Portrait overriding
Light/Landscape environment values and no recording. All 61 also passed in headed
mode with CLI Light/Landscape overriding the reverse environment values, producing
194 validated PNGs for the 20 UI cases; the 41 configuration-only cases have index
metadata and notes. The standalone recording checker passed its expected failing
case, two passing theory rows, 17 PNGs and nine invalid option/destination checks.
The six sample package consumers passed another 133 cases with recording on local
preview `.18`; the combined seven-suite run produced 1,183 PNGs.

### Desktop controls and AdvancedTextEdit

On Intel macOS (2026-09-30), the expanded suite passed all 72 cases headlessly
and all 72 in a dark portrait preview, with zero failures or skips. This includes
the standard automation Value provider, composite focus, undo, whole-document
and partial read-only protection, completion, tab header names, toolbar commands
and overflow, nested/toggle menus, right/double clicks, and disabled-drag cleanup.
The editor and CommandBar references belong to the demo application; PlayTest
itself gains no package dependency. Windows/Linux and Apple Silicon execution
of these new cases still needs validation on those machines.
