#!/usr/bin/env bash
# Build the same native host with Apple's Command Line Tools on either Mac CPU.
set -euo pipefail
native_root="$(cd "$(dirname "$0")" && pwd)"
native_configuration="${1:-Release}"
skia_library="${2:?Pass the restored SkiaSharp.NativeAssets.macOS libSkiaSharp.dylib path}"
native_output="$native_root/build/$native_configuration"
mkdir -p "$native_output"
native_flags=(-O2)
if [[ "$native_configuration" == Debug ]]; then native_flags=(-O0 -g -DDEBUG=1); fi
xcrun --sdk macosx clang -dynamiclib -fobjc-arc -fmodules \
  -arch x86_64 -arch arm64 -mmacosx-version-min=12.0 \
  -install_name @rpath/libCodeBrixNativeMac.dylib \
  "${native_flags[@]}" "$native_root"/PlatformNativeMac/*.m "$skia_library" \
  -framework AppKit -framework Metal -framework QuartzCore -framework WebKit \
  -framework AVFoundation -framework CoreMedia \
  -o "$native_output/libCodeBrixNativeMac.dylib"
xcrun lipo "$native_output/libCodeBrixNativeMac.dylib" -verify_arch x86_64 arm64
codesign --force --sign - "$native_output/libCodeBrixNativeMac.dylib"
echo "Universal native host: $native_output/libCodeBrixNativeMac.dylib"
