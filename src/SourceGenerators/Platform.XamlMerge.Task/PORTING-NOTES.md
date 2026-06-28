# Uno.XamlMerge.Task (vendored) — Porting Notes

This project vendors the Uno-org `Uno.XamlMerge.Task` MSBuild task into the
CodeBrix.Platform repository, removing the build-time dependency on the
`Uno.XamlMerge.Task` NuGet package (previously the prerelease `1.27.0-dev.44`).

The task has exactly one consumer: `src/Uno.UI/XamlMerge.targets`, which merges
Uno.UI's control-style XAML files into `UI/Xaml/Style/mergedstyles.xaml` during
the outer build of `Uno.UI.Skia.csproj`. Nothing from this project is packaged
or shipped to consumers.

## Upstream source of truth

- Repository: https://github.com/unoplatform/uno.xamlmerge.task
- Commit: `f76645703193d279913d46c9181e0642bc79c095`
  (identified from the shipped package's assembly file name
  `Uno.XamlMerge.Task.f766457....dll`)
- License: Apache-2.0 (Uno Platform / nventive — same license and origin as
  this repository's upstream)

## Vendored files

Verbatim copies from `src/Uno.XamlMerge.Task/` upstream:
`BatchMergeXaml.cs`, `CustomTask.cs`, `MergedDictionary.cs`, `Utils.cs`.

`build/Platform.XamlMerge.Task.targets` is vendored from the upstream package's
`build/` folder with one change: the two conditional `UsingTask` lines
(package tools folder vs. local dev shadow) are replaced by a single
unconditional `UsingTask` pointing at this project's shadow-copy output.

## Adaptations (csproj only; sources untouched)

- Retargeted `netstandard2.0` → `$(NetCurrent)` per this fork's dotnet-only
  MSBuild-tasks convention (same rationale as `Uno.UI.Tasks`).
- `Microsoft.Build.*` references bumped 15.4.8 → 18.6.3 to match
  `Uno.UI.Tasks`.
- Upstream's `Mono.Cecil` reference dropped: no source file uses it.
- Packaging metadata removed (`PackageId`, `Version`, pack items) — this is an
  in-repo build tool, not a package.
- The upstream `_copyTasksBuildTime` shadow-copy target is kept (consumers
  load the shadow DLL so MSBuild nodes never lock the build output); the
  destination path lost one `..\` segment because this project sets
  `AppendTargetFrameworkToOutputPath=false`.

## Verification

`src/Uno.UI/UI/Xaml/Style/mergedstyles.xaml` is git-tracked; regenerating it
with the vendored task must produce no diff against the output of the
upstream 1.27.0-dev.44 package (checked 2026-06-12).
