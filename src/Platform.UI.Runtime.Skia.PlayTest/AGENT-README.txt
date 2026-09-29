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

Preference order: explicit PlayTestOptions.Orientation, environment variable
CODEBRIX_PLAYTEST_ORIENTATION, CodeBrixPlayTestPreferredOrientation project
metadata in ConfigurationAssembly (entry assembly by default), then Landscape.
An omitted Orientation differs from an explicitly assigned Landscape.
PlayTestOrientationAttribute is runner-neutral metadata; a fixture hook must
resolve executing row traits before method attributes and apply the result.

PreviewProgram is an executable entry point in this assembly. PreviewConnection
launches it with the consumer's deps/runtimeconfig. SDL requires the process main
thread on macOS. Frames cross a bounded queue and stdin pipe, each prefixed by
two little-endian Int32 dimensions. Input never crosses back. The SDL window
keeps the launch preference's proportions and letterboxes opposite-orientation
frames; resize textures/logical presentation on the SDL main thread, never
resize the native window in response to test orientation. No SDL initialization
occurs in the offscreen application process.

Normal packaging: build/CodeBrix.Platform.Build.csproj includes this project in
_CsprojPackage and applies the shared PackageVersion. Do not set assembly Version
to a NuGet version. The core/runtime packages must include the PlayTest friend
declarations. The older published core alone cannot run the new head.

Application tests must assert actual outcomes. Preserve lazy locator resolution,
strictness, bounded retry, hit testing, and dispatcher confinement when extending
the API. Do not replace clicks with direct command invocation. Use a serialized
fixture and reset page/application state between tests. Test both timeout and
successful paths when changing the locator engine.

AriaRole.cs retains its original MIT notice. Update the root third-party notice
whenever adapting additional upstream code. SDL3 is a normal NuGet dependency.
