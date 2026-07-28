#!/usr/bin/env bash
#
# pack-local-feed.sh — packs the CodeBrix.Platform packages this sample consumes
# from the WORKING TREE into the local folder feed its NuGet.Config points at.
#
# Version 1.0.205.9999 is deliberately outside the real date-stamped scheme
# (minute-of-day never reaches 9999): it marks a local development pack, sorts
# above the 1.0.209.480 family publish, and can never collide with a real
# publish. After the family's next real publish, re-run this script with a new
# LOCAL_VERSION and bump the pins in the sample csprojs + NuGet.Config together.
#
# Same pack commands as build/CodeBrix.Platform.Build.csproj (the pack driver),
# restricted to the closure this Linux-only sample needs. Run from this folder,
# or from anywhere — paths are resolved from the script's own location.
#
# SKIP_BUILD=1 skips the Release solution build when it is already up to date.

set -euo pipefail

LOCAL_VERSION="${LOCAL_VERSION:-1.0.205.9999}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
OUT_DIR="$REPO_ROOT/nugets/Release/$LOCAL_VERSION"
NUSPEC_DIR="$REPO_ROOT/build/nuget"
SHIM="$REPO_ROOT/build/nuget-pack-shim/CodeBrix.Pack.Shim.csproj"

BRANCH="$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null || echo unknown)"
COMMIT="$(git -C "$REPO_ROOT" rev-parse HEAD 2>/dev/null || echo unknown)"

if [ "${SKIP_BUILD:-0}" != "1" ]; then
    echo "== Building CodeBrix.Platform.Linux.slnx (Release) =="
    dotnet build "$REPO_ROOT/CodeBrix.Platform.Linux.slnx" -c Release -nologo -v:q
fi

mkdir -p "$OUT_DIR"
echo "== Packing to $OUT_DIR =="

# Nuspec-driven packages (gather the already-built Release outputs).
for NUSPEC in \
    Platform.WinUI.nuspec \
    Platform.WinUI.Graphics2DSK.nuspec \
    Platform.WinUI.Graphics3DGL.nuspec \
    CodeBrix.Platform.SkiaSharp.Views.nuspec
do
    dotnet pack "$SHIM" \
        -p:CbxNuspec="$NUSPEC_DIR/$NUSPEC" -p:CbxNuspecBasePath="$NUSPEC_DIR" \
        -p:CbxVersion="$LOCAL_VERSION" -p:CbxBranch="$BRANCH" -p:CbxCommit="$COMMIT" \
        --output "$OUT_DIR" --nologo --verbosity minimal
done

# Csproj-packed packages (the Linux head/runtime set + the WebView add-in).
# -p:PackageVersion (NOT -p:Version) — see the pack driver for the rationale.
for CSPROJ in \
    src/Platform.UI.Runtime.Skia/Platform.UI.Runtime.Skia.csproj \
    src/Platform.UI.Runtime.Skia.X11/Platform.UI.Runtime.Skia.X11.csproj \
    src/Platform.UI.Runtime.Skia.Wayland/Platform.UI.Runtime.Skia.Wayland.csproj \
    src/Platform.UI.Runtime.Skia.Linux.FrameBuffer/Platform.UI.Runtime.Skia.Linux.FrameBuffer.csproj \
    src/Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated/Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.csproj \
    src/AddIns/Platform.UI.WebView.Skia/Platform.UI.WebView.Skia.csproj
do
    dotnet pack "$REPO_ROOT/$CSPROJ" -c Release -p:PackageVersion="$LOCAL_VERSION" \
        --output "$OUT_DIR" --nologo --verbosity minimal
done

echo "== Done =="
ls -1 "$OUT_DIR"
