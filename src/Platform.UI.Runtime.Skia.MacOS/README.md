# Skia Host for macOS

Set the macOS-visible application name with
`.UseMacOS(mac => mac.UseSystemAppName("My App"))`. It sets Cocoa's process name,
AppKit's application-menu name and the running-application display name used by
the Dock. It works independently of `UseSystemMenuBar()`; both options may be
combined in either order. The override is applied at startup and does not
rename the executable or assembly. Omitting it keeps the existing name.
See `AGENT-README.txt` for validation rules and macOS compatibility details.

The default uses in-window XAML menus. Opt into the system menu bar in this
head's startup code with `.UseMacOS(mac => mac.UseSystemMenuBar())`. The first
visible `MenuBar` in each window is projected into AppKit and takes zero space
in the window. Additional menu bars remain in the window. Other heads,
including PlayTest, keep their existing behavior. See the repository's
`AGENT-README.txt` for the complete menu contract.

Help shows the application's own menu items. AppKit's automatic Spotlight Help
search is suppressed; macOS does not require it. Menu updates retain native
objects and preserve items owned by AppKit.

The native implementation is included in the regular macOS runtime NuGet.
Apple Silicon package builds retain the Xcode workflow and universal Release
binary. See [native build details](PlatformNativeMac/README.md) for the
Command Line Tools option available on either architecture.

## Preparation

Before you can build you need the libSkiaSharp.dylib.
```bash
cd PlatformNativeMac/PlatformNativeMac
./getSkiaSharpDylib.sh
```
See [PlatformNativeMac/PlatformNativeMac/README.md](PlatformNativeMac/PlatformNativeMac/README.md) for more info.

## Requirements

* Minimum OS version: same as the [dotnet version used](https://learn.microsoft.com/en-us/dotnet/core/install/macos)

## Pros

* Faster startup: Fewer dependencies
  * Removing GTK3+ (native, requires separate installation)
  * Removing Silk.NET on macOS (not needed for Metal)

* Faster execution: Reduced number of managed<->native transitions

* Faster builds: No dependency on the Xamarin/Microsoft macOS SDK and toolchain

* Faster rendering: Use Metal (not OpenGL, which was disabled for Gtk/Skia/macOS) by default. Software rendering is used as a fallback.
