# CodeBrix.Platform.SkiaSharp.Views — Porting Notes

This library is a CodeBrix-built equivalent of the `SkiaSharp.Views.Uno.WinUI`
NuGet package, compiled against CodeBrix.Platform (this repository) instead of
the Uno NuGet packages. It exists so that CodeBrix.Platform has zero reliance
on Uno NuGet packages while app developers keep the standard
`SkiaSharp.Views.Windows` API surface (`SKXamlCanvas`, `SKSwapChainPanel`,
`SKPaintSurfaceEventArgs`, extension helpers) that every SkiaSharp sample uses.

All heavy Skia parts (libSkiaSharp natives, the managed `SkiaSharp` wrapper,
`SkiaSharp.Skottie`) remain the official upstream NuGet packages. Only this
thin view-glue layer is vendored, because glue compiled against Uno binaries
cannot be used against this fork.

## Upstream source of truth

- Repository: https://github.com/mono/SkiaSharp
- Tag: `v3.119.4`
- Commit: `f568ac94dd768ef9a2f593537cfde2dd0d348ef5`
- License: MIT (see THIRD-PARTY-NOTICES.md at the repository root)
- Local reference clone: `~/GitHome/SkiaSharp` (shallow, no submodules)

## Vendored files (verbatim copies, all under `source/` upstream)

| Local path | Upstream path |
|---|---|
| `SkiaSharp.Views.Shared/Extensions.cs` | `SkiaSharp.Views/SkiaSharp.Views.Shared/Extensions.cs` |
| `SkiaSharp.Views.Shared/SKPaintSurfaceEventArgs.cs` | `SkiaSharp.Views/SkiaSharp.Views.Shared/SKPaintSurfaceEventArgs.cs` |
| `SkiaSharp.Views.Shared/SKPaintGLSurfaceEventArgs.cs` | `SkiaSharp.Views/SkiaSharp.Views.Shared/SKPaintGLSurfaceEventArgs.cs` |
| `SkiaSharp.Views.WinUI/UWPExtensions.cs` | `SkiaSharp.Views/SkiaSharp.Views.WinUI/UWPExtensions.cs` |
| `SkiaSharp.Views.Uno.WinUI.Shared/SKXamlCanvas.cs` | `SkiaSharp.Views.Uno/SkiaSharp.Views.Uno.WinUI.Shared/SKXamlCanvas.cs` |
| `SkiaSharp.Views.Uno.WinUI.Shared/SKSwapChainPanel.cs` | `SkiaSharp.Views.Uno/SkiaSharp.Views.Uno.WinUI.Shared/SKSwapChainPanel.cs` |
| `SkiaSharp.Views.Uno.WinUI/SKXamlCanvas.Reference.cs` | `SkiaSharp.Views.Uno/SkiaSharp.Views.Uno.WinUI/SKXamlCanvas.Reference.cs` |
| `SkiaSharp.Views.Uno.WinUI/SKSwapChainPanel.Reference.cs` | `SkiaSharp.Views.Uno/SkiaSharp.Views.Uno.WinUI/SKSwapChainPanel.Reference.cs` |
| `SkiaSharp.Views.Uno.WinUI.Skia/SKXamlCanvas.Skia.cs` | `SkiaSharp.Views.Uno/SkiaSharp.Views.Uno.WinUI.Skia/SKXamlCanvas.Skia.cs` |
| `SkiaSharp.Views.Uno.WinUI.Skia/SKSwapChainPanel.Skia.cs` | `SkiaSharp.Views.Uno/SkiaSharp.Views.Uno.WinUI.Skia/SKSwapChainPanel.Skia.cs` |

Files are copied byte-for-byte. Do not edit them locally; if a change is ever
unavoidable, record it here so version bumps can re-apply it.

## Deliberately NOT vendored

- `SkiaSharp.Views.Shared/Properties/SkiaSharpViewsAssemblyInfo.cs` — its
  assembly-level attributes collide with the SDK-generated ones (CS0579);
  the equivalent metadata is set as properties in the csproj files instead.
- `SkiaSharp.Views.Shared/GlesInterop/Gles.cs` — compiles to nothing in both
  flavors we build (its `#if` requires a platform symbol we never define, and
  upstream's shipped skia/reference assemblies verifiably contain no `Gles`
  class). If `SKSwapChainPanel` ever gets a real GPU implementation here,
  revisit.
- Everything in `SkiaSharp.Views.Uno.WinUI.Wasm/` and the per-platform
  mobile files (`*.Android.cs`, `*.iOS.cs`, …) — this fork builds only the
  Skia desktop heads. Resurrect from the upstream tag if heads return.

## How the two csproj flavors map to upstream

- `CodeBrix.Platform.SkiaSharp.Views.Skia.csproj` mirrors upstream
  `SkiaSharp.Views.Uno.WinUI.Skia.csproj`: the real implementation
  (software-rendered `SKXamlCanvas` via `WriteableBitmap`; `SKSwapChainPanel`
  throws `NotSupportedException` — same as upstream on Skia heads). This is
  the assembly that ships in the NuGet package's `lib/`.
- `CodeBrix.Platform.SkiaSharp.Views.Reference.csproj` mirrors the plain-TFM
  build of upstream `SkiaSharp.Views.Uno.WinUI.csproj`: the `*.Reference.cs`
  API shells, used only inside this repository so Reference-flavor projects
  (e.g. `Uno.UI.Lottie.Reference`) can compile against matching binaries.
  It is not packaged.
- Upstream defines `HAS_CODEBRIX`/`HAS_CODEBRIX_WINUI`/`CODEBRIX_REFERENCE_API` via the
  Uno.WinUI package props; here they are set explicitly in each csproj
  (see comments there). `Uno.UI.Lottie.Tests.csproj` compiles these sources
  directly (with `CODEBRIX_REFERENCE_API`) instead of referencing this project,
  because it must bind against the Tests flavor of Uno.UI.

## Versioning policy

Package version == vendored SkiaSharp version (`3.119.4`), NOT the
CodeBrix.Platform family version. The fourth segment (`3.119.4.1`, …) is
reserved for glue-only fixes. Never use prerelease suffixes (they sort below
the release). Family-wide version-bump tooling must skip this project.

## Bumping to a new SkiaSharp version

1. Update the reference clone: `git -C ~/GitHome/SkiaSharp fetch --depth 1
   origin tag v<NEW> && git -C ~/GitHome/SkiaSharp checkout v<NEW>`
2. Diff each vendored file against its upstream path (table above); re-copy.
3. Check upstream `SkiaSharp.Views.Uno.WinUI*.csproj` for new/removed compile
   items or defines; mirror any changes in the two csprojs here.
4. Update `$(SkiaSharpVersion)` in `src/Directory.Build.targets`, the
   `<Version>`/`<PackageVersion>` in both csprojs here, the nuspec, and the
   tag/commit at the top of this file.
5. Build all three solution files; runtime-smoke an `SKXamlCanvas` sample.
