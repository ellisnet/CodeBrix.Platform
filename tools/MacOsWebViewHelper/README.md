# macOS PlayTest WebView helper

This folder contains the native WKWebView helper's complete source and build
recipe. A checkout of CodeBrix.Platform can rebuild it on either an Intel or
Apple Silicon Mac. No prebuilt binary, files from another checkout, or files from
a previous workstation are required.

## Build from a checkout

Install .NET SDK 10 and Apple's Command Line Tools (`xcode-select --install`) or
Xcode. Run from the CodeBrix.Platform root:

```sh
dotnet build tools/MacOsWebViewHelper/MacOsWebViewHelper.proj --no-restore
file tools/MacOsWebViewHelper/bin/CodeBrix.PlayTest.WebView
```

The native-only project needs no NuGet restore. It builds a universal executable
containing **both x86_64 and arm64**, regardless of which Mac builds it. The minimum
deployment target is macOS 12. Generated files stay in this folder's ignored
`bin/` and `obj/` directories. The helper is a pipe-driven child process, not an
application to open manually.

## Framework and package builds

`src/AddIns/Platform.UI.WebView.Skia/Platform.UI.WebView.Skia.csproj` uses this
same source and `CodeBrix.WebView.MacOS.targets` recipe. Building or packing the
WebView add-in on macOS automatically builds the helper; there is no separate
manual copy step. For a complete local PlayTest package family, use the repository's
`build/pack-playtest-preview.py --version <new-unused-preview-version>` helper.

The WebView NuGet package includes this source and build recipe. Packages built
on macOS also include the universal executable. Packages built on another OS
compile the included source during a macOS PlayTest consumer build, using Apple's
command-line tools. The executable is copied to the application's output as
`CodeBrix.PlayTest.WebView`; builds do not modify the NuGet package cache.

## Runtime design and validation

`PlayTestWebView.m` owns AppKit and WKWebView on the helper's main thread. It uses
a hidden window and a nonpersistent browser data store. The managed bridge lives
in `src/AddIns/Platform.UI.WebView.Skia/MacOS/MacOSOffscreenWebView.cs`. It exchanges
navigation, native input, script results and PNG snapshots over redirected
stdin/stdout. Page unload closes the helper. Native file uploads, script dialogs
and downloads are outside this offscreen adapter's current scope.

The WikipediaPublisher PlayTests in CodeBrix.Samples exercise navigation,
pointer and keyboard input, page replacement and composited browser pixels across
orientation changes. The shared macOS runner also validates the other sample
suites and their Cocoa previews. Intel macOS runtime validation is recorded in
that repository's `PlayTestSupport/README.md`; producing an arm64 binary is not
an Apple Silicon runtime test.

## Windows and Linux helper source

All custom PlayTest preview and WebView integration code is in CodeBrix.Platform.
Windows and Linux do not build a separate custom native WebView executable:

| Component | Repository source | External dependency |
| --- | --- | --- |
| Shared visible preview | `src/Platform.UI.Runtime.Skia.PlayTest/Preview/` | SDL3 through the `CodeBrix.Sdl3.ZlibLicenseForever` NuGet dependency |
| Windows WebView adapter | `src/AddIns/Platform.UI.WebView.Skia/Windows/WindowsOffscreenWebView.cs` | Microsoft WebView2 SDK/loader from NuGet, plus installed Edge WebView2 runtime |
| Windows message pump | `src/Platform.UI.Runtime.Skia.PlayTest/Hosting/WindowsMessagePump.cs` | Windows system APIs |
| Linux WebView adapter | `src/AddIns/Platform.UI.WebView.Skia/Wpe*.cs`, `Interop/`, and `Input/` | System WPE WebKit, WPEBackend-fdo, libwpe and their dependencies |
| Windows preview pixel checker | `build/test-scripts/playtest-preview-windows.ps1`, including its embedded C# capture helper | PowerShell 7 and Windows drawing APIs |
| Linux preview pixel checker | `build/test-scripts/playtest-preview-orientation.py` | X11, xdotool, ImageMagick and Pillow |
| macOS preview pixel checker | `build/test-scripts/playtest-preview-macos.py` and `playtest-preview-capture-macos.m` | macOS 14+, Apple's command-line tools and Screen Recording permission |

The preview executable and Windows/Linux adapters build with the regular .NET
projects. The upstream source of third-party libraries and installed runtimes is
not all vendored here. The macOS WebView helper in this folder is the only current
PlayTest WebView adapter that needs a separately compiled custom native executable.
