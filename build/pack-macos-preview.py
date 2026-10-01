#!/usr/bin/env python3
"""Build a local macOS/core/runtime NuGet preview; never publish or modify Git."""
import argparse
from pathlib import Path
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def run(*args):
    print("+", " ".join(map(str, args)), flush=True)
    subprocess.run(list(map(str, args)), cwd=ROOT, check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", type=Path, default=ROOT / "nugets/MacOSPreview")
    args = parser.parse_args()
    if "-" not in args.version:
        parser.error("Choose a fresh prerelease version for each changed preview.")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    if any(output.glob(f"*.{args.version}.nupkg")):
        parser.error("That version is already in the feed. Choose a new version to avoid cached packages.")
    for project in (
        "src/SourceGenerators/Platform.XamlMerge.Task/Platform.XamlMerge.Task.csproj",
        "src/Platform.UI.FluentTheme/Platform.UI.FluentTheme.Reference.csproj",
        "src/Platform.UI.FluentTheme/Platform.UI.FluentTheme.Skia.csproj",
        "src/Platform.UI.Toolkit/Platform.UI.Toolkit.Reference.csproj",
        "src/Platform.UI.Toolkit/Platform.UI.Toolkit.Skia.csproj",
        "src/Platform.UI.Adapter.Microsoft.Extensions.Logging/Platform.UI.Adapter.Microsoft.Extensions.Logging.csproj",
        "src/Platform.Analyzers/Platform.Analyzers.csproj",
        "src/Platform.UI.Runtime.Skia.MacOS/Platform.UI.Runtime.Skia.MacOS.csproj",
    ):
        run("dotnet", "build", project, "-c", "Release", "--disable-build-servers", "-m:1", "-v:minimal")
    props = ROOT / "build/obj/macos-nuspec-props"
    run("dotnet", "run", "--project", "src/Platform.PackageDependencyValidator/Platform.PackageDependencyValidator.csproj",
        "-c", "Release", "--", "--map", ROOT / "build/nuget/package-dependency-map.json",
        "--repo-root", ROOT, "--emit-properties", props, "--no-fail")
    run("dotnet", "pack", "build/nuget-pack-shim/CodeBrix.Pack.Shim.csproj",
        f"-p:CbxNuspec={ROOT / 'build/nuget/Platform.WinUI.nuspec'}",
        f"-p:CbxNuspecBasePath={ROOT / 'build/nuget'}", f"-p:CbxVersion={args.version}",
        "-p:CbxBranch=macos-local", "-p:CbxCommit=uncommitted",
        f"-p:CbxNuspecPropsFile={props / 'Platform.WinUI.props'}", "--output", output, "-v:minimal")
    for name in ("Platform.UI.Runtime.Skia", "Platform.UI.Runtime.Skia.Linux.FrameBuffer", "Platform.UI.Runtime.Skia.MacOS"):
        run("dotnet", "pack", f"src/{name}/{name}.csproj", "-c", "Release",
            f"-p:PackageVersion={args.version}", "--no-restore", "--output", output, "-v:minimal")
    package_file = output / f"CodeBrix.Platform.Runtime.Skia.MacOS.ApacheLicenseForever.{args.version}.nupkg"
    with zipfile.ZipFile(package_file) as package, tempfile.TemporaryDirectory() as scratch:
        native = Path(package.extract("runtimes/osx/native/libCodeBrixNativeMac.dylib", scratch))
        run("xcrun", "lipo", native, "-verify_arch", "x86_64", "arm64")
        symbols = subprocess.check_output(["nm", "-gU", str(native)], text=True)
        for symbol in ("_codebrix_menu_create", "_codebrix_application_set_name"):
            if symbol not in symbols:
                raise RuntimeError(f"The package contains a native library without {symbol}.")
    print(f"Local preview packages: {output}. Nothing was published.")


if __name__ == "__main__":
    main()
