# CodeBrixNativeMac

## Building

The normal `CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever`
package build rebuilds and includes `libCodeBrixNativeMac.dylib`. On Apple
Silicon it continues to use Xcode through `build.sh`. The Xcode project includes
`CDBRXMenus.m`, so the opt-in system menu implementation is part of the same
native library, with no separately built menu helper.
`CDBRXApplication.m` also implements the independent `UseSystemAppName` option;
both Xcode and Command Line Tools builds include it for x64 and ARM64.

Naming is applied before the managed application starts. AppKit requires the
process-local main-bundle name caches (including localized names), as changing
`NSProcessInfo.processName` alone does not change the application-menu title.
The bridge checks cache mutability and fails startup explicitly if a future OS
cannot provide it; it never writes an Info.plist. Running-application/Dock naming
uses dynamically looked-up LaunchServices SPI (`_LSGetCurrentApplicationASN`,
`_LSSetApplicationInformationItem`, `_kLSDisplayNameKey`), with a diagnostic and
Cocoa/AppKit fallback if those entry points are absent or the update fails.
Keep the actual AppKit accessibility-title and `NSRunningApplication.localizedName`
regressions when changing this code; checking only the menu item's `title`
property misses the system-generated application label.

To build directly with Xcode, run

```bash
xcodebuild
```

and the `build/Release/` directory will contain:

* `libSkiaSharp.dylib`
* `libCodeBrixNativeMac.dylib`
* `libCodeBrixNativeMac.dylib.dSYM` - the debugging symbols

Release builds contain both `x86_64` and `arm64`. The existing Xcode Debug
configuration uses the active architecture.

On Intel, the .NET project defaults to `build-clang.sh`, which uses Apple's
Command Line Tools and the restored SkiaSharp native package. It builds and
ad-hoc signs a universal dylib in `build/<Configuration>/`. This path requires
no full Xcode installation. Either CPU can explicitly select the compiler:

```bash
dotnet build ../Platform.UI.Runtime.Skia.MacOS.csproj -c Release -p:NativeMacBuildTool=Clang
dotnet build ../Platform.UI.Runtime.Skia.MacOS.csproj -c Release -p:NativeMacBuildTool=Xcode
```

`BuildNativeMac=false` opts out of native compilation for managed-only checks;
do not use it for a release package that needs the native runtime.
