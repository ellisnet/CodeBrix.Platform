#!/bin/bash

set -e

rm -rf build
cd PlatformNativeMac
chmod +x getSkiaSharpDylib.sh
./getSkiaSharpDylib.sh
cd ..
xcodebuild $@
mkdir -p ../runtimes/osx/native
cp -R build/Release/libCodeBrixNativeMac.* ../runtimes/osx/native || true
