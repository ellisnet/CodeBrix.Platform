#!/usr/bin/env python3
"""Build a local PlayTest/core/runtime NuGet set; never publish or modify Git."""
import argparse
from pathlib import Path
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def run(*args):
    print("+", " ".join(map(str, args)), flush=True)
    subprocess.run(list(map(str, args)), cwd=ROOT, check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True, help="Use a new prerelease version for each changed package set.")
    parser.add_argument("--output", type=Path, default=ROOT / "nugets/PlayTest")
    parser.add_argument("--with-editor", action="store_true", help="Also build matching AdvancedTextEdit/TextLayout add-ins for editor integration tests; these are not PlayTest dependencies.")
    args = parser.parse_args()
    if "-" not in args.version:
        parser.error("This local preview helper requires a prerelease version (for example 1.0.271.1-playtest.2).")
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    if any(output.glob(f"*.{args.version}.nupkg")):
        parser.error("That version already exists in the output feed; choose a fresh version to avoid NuGet cache ambiguity.")

    # These graphs cover every assembly in the core nuspec. This includes companion
    # resource/toolkit libraries that a bare head-project build does not reference.
    for project in (
        "src/SourceGenerators/Platform.XamlMerge.Task/Platform.XamlMerge.Task.csproj",
        "src/Platform.UI.FluentTheme/Platform.UI.FluentTheme.Core.csproj",
        "src/Platform.UI.Toolkit/Platform.UI.Toolkit.Core.csproj",
        "src/Platform.UI.Toolkit/Platform.UI.Toolkit.Skia.csproj",
        "src/Platform.UI.Adapter.Microsoft.Extensions.Logging/Platform.UI.Adapter.Microsoft.Extensions.Logging.Core.csproj",
        "src/Platform.Analyzers/Platform.Analyzers.csproj",
        "src/Platform.UI.Runtime.Skia.PlayTest/Platform.UI.Runtime.Skia.PlayTest.csproj",
        "src/AddIns/Platform.UI.WebView.Skia/Platform.UI.WebView.Skia.csproj",
    ):
        run("dotnet", "build", project, "-c", "Release", "--verbosity", "minimal")

    editor_projects = (
        "src/AddIns/Platform.UI.TextLayout/Platform.UI.TextLayout.Core.csproj",
        "src/AddIns/Platform.UI.AdvancedTextEdit/Platform.UI.AdvancedTextEdit.Skia.csproj",
    ) if args.with_editor else ()
    for project in editor_projects:
        run("dotnet", "build", project, "-c", "Release", "--verbosity", "minimal")

    props = ROOT / "build/obj/playtest-nuspec-props"
    run("dotnet", "run", "--project", "src/Platform.PackageDependencyValidator/Platform.PackageDependencyValidator.csproj",
        "-c", "Release", "--", "--map", ROOT / "build/nuget/package-dependency-map.json",
        "--repo-root", ROOT, "--emit-properties", props, "--no-fail")
    run("dotnet", "pack", "build/nuget-pack-shim/CodeBrix.Pack.Shim.csproj",
        f"-p:CbxNuspec={ROOT / 'build/nuget/Platform.WinUI.nuspec'}",
        f"-p:CbxNuspecBasePath={ROOT / 'build/nuget'}", f"-p:CbxVersion={args.version}",
        "-p:CbxBranch=playtest-local", "-p:CbxCommit=uncommitted",
        f"-p:CbxNuspecPropsFile={props / 'Platform.WinUI.props'}", "--output", output, "--verbosity", "minimal")
    for project in (
        "src/Platform.UI.Runtime.Skia/Platform.UI.Runtime.Skia.csproj",
        "src/Platform.UI.Runtime.Skia.PlayTest/Platform.UI.Runtime.Skia.PlayTest.csproj",
        "src/AddIns/Platform.UI.WebView.Skia/Platform.UI.WebView.Skia.csproj",
    ) + editor_projects:
        run("dotnet", "pack", project, "-c", "Release", f"-p:PackageVersion={args.version}",
            "--no-restore", "--output", output, "--verbosity", "minimal")
    with zipfile.ZipFile(output / f"CodeBrix.Platform.WebView.ApacheLicenseForever.{args.version}.nupkg") as package:
        for asset in ("buildTransitive/CodeBrix.WebView.MacOS.targets", "buildTransitive/macos/PlayTestWebView.m"):
            if asset not in package.namelist():
                raise RuntimeError(f"WebView package is missing required macOS asset: {asset}")
    with zipfile.ZipFile(output / f"CodeBrix.Platform.PlayTest.ApacheLicenseForever.{args.version}.nupkg") as package:
        for asset in ("buildTransitive/CodeBrix.PlayTest.TestingPlatform.cs", "buildTransitive/CodeBrix.PlayTest.Xunit.cs"):
            if asset not in package.namelist():
                raise RuntimeError(f"PlayTest package is missing its runner adapter: {asset}")
    print(f"Local packages are in {output}. No packages were published.")


if __name__ == "__main__":
    main()
