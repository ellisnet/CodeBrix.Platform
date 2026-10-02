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

The test project accepts the PlayTest command-line switches on Windows, macOS
and Linux: `--headed` (alias `--nonheadless`) or `--headless`,
`--theme=light|dark`, `--orientation=landscape|portrait`, and
`--screenshotfolder=<existing empty folder>` for automatic screenshot recording
with no test edits. Precedence against environment variables, project
preferences and fixture options, the action delay (`CODEBRIX_PLAYTEST_SLOWMO`)
and the recording folder layout are described in the PlayTest
[consumer guide](../../../src/Platform.UI.Runtime.Skia.PlayTest/AGENT-README.txt).

Use the relevant desktop head on other operating systems; the PlayTests project
is the same on all of them. FrameBuffer requires a device or an emulator; it is
not a normal desktop preview.

## Coverage and adding tests

This demo's test project holds the UI cases that need a running application:

- Checkbox and toggle-switch input, checked-state assertions, and idempotence.
- Scrolling an initially clipped button into view and clicking it.
- Folder, open, multiple-open and save selections and cancellation.
- Ordered multi-file results, actual text reads, existing-file preservation,
  invalid-path and unscripted-picker diagnostics, FIFO responses and queue reset.
- Forced landscape and portrait at both method and theory-row level.
- A check that the requested preview mode reaches the running application.
- The desktop-controls screen: AdvancedTextEdit, menus, CommandBar and related
  interactions.

Configuration-only cases that need no running application - delay defaults,
command-line/environment/project precedence, the `--nonheadless` alias, conflicting
options, invalid values and screenshot-folder validation - live in the host-free
`src/Platform.UI.Runtime.Skia.PlayTest.Tests` project, not in this demo.

The UI suite is serialized against one application: the fixture launches it once
and installs a fresh **demo MainPage** for each test. It clears picker responses,
resolves orientation traits, and removes its own temporary files at teardown.
Tests queue paths before clicking the application's real picker buttons; they
verify visible status and view-model/file outcomes. The application handles
picker errors by displaying their type/message, so a failing picker remains
inspectable in the preview.

Put new demonstration UI in the shared application, and its regression cases in
this test project. Configuration-only tests belong in
`src/Platform.UI.Runtime.Skia.PlayTest.Tests`. Application-specific suites in
CodeBrix.Samples should stay focused on their own screens. Controlled data or
service implementations for an application's real workflow belong in that
application's fixture.

The test project supplies `CodeBrixPlayTestPreferredOrientation` and
`CodeBrixPlayTestPreferredTheme` defaults. Opposite orientations are letterboxed
in a stable preview window; virtual screen sizes remain 1920×1080 or 1080×1920,
irrespective of preview resizing.

Snapshots are written below `TestResults/PlayTest` in the test process directory.
See the [PlayTest consumer guide](../../../src/Platform.UI.Runtime.Skia.PlayTest/AGENT-README.txt)
for configuration, locators, assertions, supported controls and limitations.

## Native preview checks

These checkers run from the repository root against built test output. Each
opens only its own preview windows, captures their client areas, and validates
alternating landscape/portrait frames with stable window sizes and centered
letterboxing, plus the preview protocol's error handling. Keep the preview
windows fully visible while a capture check runs.

All three checkers launch the preview from this demo's built test output by
default; build `tests/PlayTestDemo.PlayTests` in Release first. Each accepts a
test-name option (`-TestName` on Windows, `--test-name` on Linux and macOS) to
run against another PlayTest project's output instead.

Windows (PowerShell 7 and Windows' drawing APIs; no Python imaging dependency):

```powershell
./build/test-scripts/playtest-preview-windows.ps1 `
  -TestOutput samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests/bin/Release/net10.0 `
  -Artifacts TestResults/PlayTestPreview-Windows
```

Linux X11 (an X11 desktop, xdotool, ImageMagick `import`, and Pillow):

```sh
python3 build/test-scripts/playtest-preview-orientation.py \
  --test-output samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests/bin/Release/net10.0 \
  --artifacts TestResults/PlayTestPreview
```

macOS (macOS 14+, Apple's command-line tools, and Screen Recording permission
for the invoking terminal or agent; no Python imaging package):

```sh
python3 build/test-scripts/playtest-preview-macos.py \
  --test-output samples/CodeBrixPlatform/PlayTestDemo/tests/PlayTestDemo.PlayTests/bin/Release/net10.0 \
  --artifacts TestResults/PlayTestPreview-macOS
```

The macOS checker captures only the preview processes it starts, using
ScreenCaptureKit. Application tests and their virtual screenshots do not need
Screen Recording permission.

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
