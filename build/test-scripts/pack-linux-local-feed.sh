#!/bin/bash
# =====================================================================================================
# pack-linux-local-feed.sh - a Linux VERIFICATION aid. It NEVER publishes anything.
#
#   build/test-scripts/pack-linux-local-feed.sh <output-folder> [version]
#
# Produces the Linux-buildable subset of the CodeBrix.Platform package family into <output-folder>
# (usable as a local folder feed), with the SAME pack commands and the SAME dependency gate as the
# Windows packaging driver, build/CodeBrix.Platform.Build.csproj:
#   1. the package dependency gate in --emit-properties mode writes the per-nuspec $dep_...$ tokens;
#   2. every nuspec is packed through build/nuget-pack-shim/CodeBrix.Pack.Shim.csproj;
#   3. every csproj package is packed with `dotnet pack -c Release -p:PackageVersion=<version>`;
#   4. the package dependency gate runs over the produced packages (--package-dir) as a HARD gate.
#
# [version] defaults to the driver's date-stamped scheme, computed here in bash with the same formula:
#   1.<UTC year - 2026>.<UTC day of year>.<UTC minute of day>
#
# Requires a prior `dotnet build CodeBrix.Platform.Linux.slnx -c Release` (the nuspecs pack
# already-built outputs; the script checks for them and stops with a clear message if any is missing).
#
# The nuspecs are packed exactly as the driver packs them. Linux file names are case-sensitive, so every
# nuspec <file src> path must match the folder names on disk exactly; the preflight below reports any that
# does not (Windows would not notice).
#
# NOT packed here (and why):
#   - CodeBrix.Platform.Runtime.Skia.Win32 / .Wpf  : Windows heads - packed by the Windows driver only.
#   - CodeBrix.Platform.Runtime.Skia.MacOS          : needs the native libCodeBrixNativeMac.dylib, which
#                                                    only builds on Apple Silicon (Xcode).
#   Everything else the driver packs is packed here: the 6 nuspecs and the 16 remaining csprojs.
# =====================================================================================================
set -euo pipefail

usage() {
	echo "Usage: $0 <output-folder> [version]" >&2
	echo "  Packs the Linux-buildable CodeBrix.Platform packages into <output-folder>. Never publishes." >&2
	exit 2
}

[ $# -ge 1 ] && [ $# -le 2 ] || usage

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
NUSPEC_DIR="$REPO_ROOT/build/nuget"
PACK_SHIM="$REPO_ROOT/build/nuget-pack-shim/CodeBrix.Pack.Shim.csproj"
DEPENDENCY_VALIDATOR="$REPO_ROOT/src/Platform.PackageDependencyValidator/Platform.PackageDependencyValidator.csproj"
DEPENDENCY_MAP="$NUSPEC_DIR/package-dependency-map.json"
NUSPEC_PROPS_DIR="$REPO_ROOT/build/obj/nuspec-props"

mkdir -p "$1"
OUTPUT_DIR="$(cd "$1" && pwd)"

# ---- version: the driver's formula, 1.<years since 2026>.<dayOfYear>.<minuteOfDay>, all UTC ----------
if [ $# -ge 2 ]; then
	BUILD_VERSION="$2"
else
	read -r utc_year utc_doy utc_hour utc_min < <(date -u '+%Y %j %H %M')
	BUILD_VERSION="1.$((utc_year - 2026)).$((10#$utc_doy)).$((10#$utc_hour * 60 + 10#$utc_min))"
fi

# ---- git branch/commit for the nuspec <repository> tokens (read-only, like the driver) --------------
GIT_BRANCH="$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD 2>/dev/null || true)"
GIT_COMMIT="$(git -C "$REPO_ROOT" rev-parse HEAD 2>/dev/null || true)"
GIT_BRANCH="${GIT_BRANCH:-unknown}"
GIT_COMMIT="${GIT_COMMIT:-unknown}"

# ---- what the driver packs, minus the Windows/macOS-only packages ----------------------------------
NUSPECS=(
	Platform.WinUI.nuspec
	Platform.WinUI.Graphics2DSK.nuspec
	Platform.WinUI.Graphics3DGL.nuspec
	Platform.WinUI.Lottie.nuspec
	Platform.WinUI.Svg.nuspec
	CodeBrix.Platform.SkiaSharp.Views.nuspec
)

CSPROJS=(
	src/Platform.UI.Runtime.Skia/Platform.UI.Runtime.Skia.csproj
	src/Platform.UI.Runtime.Skia.Linux.FrameBuffer/Platform.UI.Runtime.Skia.Linux.FrameBuffer.csproj
	src/Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated/Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.csproj
	src/Platform.UI.Runtime.Skia.X11/Platform.UI.Runtime.Skia.X11.csproj
	src/Platform.UI.Runtime.Skia.Wayland/Platform.UI.Runtime.Skia.Wayland.csproj
	src/AddIns/Platform.UI.WebView.Skia/Platform.UI.WebView.Skia.csproj
	src/AddIns/Platform.UI.AudioPlayer.Skia/Platform.UI.AudioPlayer.Skia.csproj
	src/AddIns/Platform.UI.VideoPlayer.Skia/Platform.UI.VideoPlayer.Skia.csproj
	src/AddIns/Platform.UI.MediaPlayer.Skia/Platform.UI.MediaPlayer.Skia.csproj
	src/AddIns/Platform.UI.TextLayout/Platform.UI.TextLayout.Core.csproj
	src/AddIns/Platform.UI.FlexPanel/Platform.UI.FlexPanel.Core.csproj
	src/AddIns/Platform.UI.AdvancedTextEdit/Platform.UI.AdvancedTextEdit.Skia.csproj
	src/AddIns/Platform.UI.TerminalView/Platform.UI.TerminalView.Skia.csproj
	src/AddIns/Platform.UI.PlotterView/Platform.UI.PlotterView.Skia.csproj
	src/AddIns/Platform.AppSettings/Platform.AppSettings.csproj
	src/AddIns/Platform.UI.CommandBar/Platform.UI.CommandBar.Skia.csproj
)

echo "CodeBrix pack (Linux verification): version $BUILD_VERSION -> $OUTPUT_DIR"
echo "  branch $GIT_BRANCH, commit $GIT_COMMIT"

# ---- check that every input each nuspec names exists (case-sensitive, as Linux is) -------------------
missing_file="$(mktemp)"
trap 'rm -f "$missing_file"' EXIT

for nuspec in "${NUSPECS[@]}"; do
	python3 - "$NUSPEC_DIR/$nuspec" "$NUSPEC_DIR" "$missing_file" <<'PY'
import glob, os, re, sys

source, base, missing_path = sys.argv[1:4]
name = os.path.basename(source)
text = open(source, encoding="utf-8-sig").read()
missing = []

for src in re.findall(r'<file\s+src="([^"]+)"', text):
    pattern = os.path.join(base, src.replace("\\", "/"))
    if glob.glob(pattern, recursive=True):
        continue
    wildcard = any(c in pattern for c in "*?[")
    folder = os.path.dirname(pattern.split("*")[0]) if wildcard else None
    if wildcard and os.path.isdir(folder):
        # An empty wildcard match packs nothing, exactly as it does for the Windows driver.
        print(f"  note: {name}: {src} matches no file (nothing packed for it, as on Windows)")
    else:
        missing.append(f"{name}: {src}")

with open(missing_path, "a") as m:
    for line in missing:
        m.write(line + "\n")
PY
done

if [ -s "$missing_file" ]; then
	echo "" >&2
	echo "ERROR: nuspec inputs are missing. Build the Release solution first:" >&2
	echo "         dotnet build CodeBrix.Platform.Linux.slnx -c Release" >&2
	echo "       (if the build is there, check the path's letter case against the folders on disk)" >&2
	echo "       Missing:" >&2
	sed 's/^/         /' "$missing_file" >&2
	exit 1
fi

# ---- 1. pack-time dependency tokens (driver step: --emit-properties ... --no-fail) -----------------
echo "CodeBrix pack: generating nuspec dependency tokens"
dotnet run --project "$DEPENDENCY_VALIDATOR" -c Release --nologo -- --map "$DEPENDENCY_MAP" --repo-root "$REPO_ROOT/." --emit-properties "$NUSPEC_PROPS_DIR" --no-fail

# ---- 2. nuspec packages through the pack shim --------------------------------------------------------
for nuspec in "${NUSPECS[@]}"; do
	echo "CodeBrix pack: $nuspec"
	dotnet pack "$PACK_SHIM" -p:CbxNuspec="$NUSPEC_DIR/$nuspec" -p:CbxNuspecBasePath="$NUSPEC_DIR" -p:CbxVersion="$BUILD_VERSION" -p:CbxBranch="$GIT_BRANCH" -p:CbxCommit="$GIT_COMMIT" -p:CbxNuspecPropsFile="$NUSPEC_PROPS_DIR/${nuspec%.nuspec}.props" --output "$OUTPUT_DIR" --nologo --verbosity minimal
done

# ---- 3. csproj packages (PackageVersion only, never Version - see the driver) ----------------------
for csproj in "${CSPROJS[@]}"; do
	echo "CodeBrix pack: $csproj"
	dotnet pack "$REPO_ROOT/$csproj" -c Release -p:PackageVersion="$BUILD_VERSION" --output "$OUTPUT_DIR" --nologo --verbosity minimal
done

# ---- 4. THE GATE over the produced packages --------------------------------------------------------
echo "CodeBrix pack: verifying package dependencies"
dotnet run --project "$DEPENDENCY_VALIDATOR" -c Release --nologo -- --map "$DEPENDENCY_MAP" --repo-root "$REPO_ROOT/." --package-dir "$OUTPUT_DIR"

echo ""
echo "CodeBrix pack (Linux verification) done: $(ls "$OUTPUT_DIR"/*.nupkg | wc -l) package(s) in $OUTPUT_DIR"
ls -1 "$OUTPUT_DIR"/*.nupkg | xargs -n1 basename
