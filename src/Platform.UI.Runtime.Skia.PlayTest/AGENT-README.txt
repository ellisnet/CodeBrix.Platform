CodeBrix.Platform.PlayTest.ApacheLicenseForever — maintainer notes

Read README.md for the supported 0.1 surface and consumer setup. Namespace:
CodeBrix.Platform.PlayTest. Target: net10.0. Nullable and implicit usings are off.

This is a separate head; do not add PlayTest branches to existing desktop heads.
VirtualHost owns the UI dispatcher, CPU Skia renderer, window/display extensions,
isolated clipboard and synthetic input. Shared framework assembly friend entries
permit the same internal extension and compositor APIs used by other heads.
Virtual resolution is fixed (1920x1080 or1080x1920), with rasterization scale1.

PreviewProgram is an executable entry point in this assembly. PreviewConnection
launches it with the consumer's deps/runtimeconfig. SDL requires the process main
thread on macOS. Frames cross a bounded queue and stdin pipe; input never crosses
back. No SDL initialization occurs in the offscreen application process.

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
