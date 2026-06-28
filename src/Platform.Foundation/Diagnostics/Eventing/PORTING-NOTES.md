# Uno.Diagnostics.Eventing (vendored) — Porting Notes

This folder vendors the complete source of the `Uno.Diagnostics.Eventing`
library into `Uno.Foundation`, removing the `Uno.Diagnostics.Eventing` NuGet
package (previously 2.0.1) from the dependency graph entirely — both the
fork's own PackageReferences and the consumer-flowing nuspec dependency.

The `Uno.Diagnostics.Eventing` NAMESPACE is kept verbatim: the fork's own
sources use it in 36+ files, and `DependencyObjectGenerator` emits
`using Uno.Diagnostics.Eventing;` into consumer apps' generated code. With
these types compiled into `Uno.Foundation.dll` (which every consumer
references), all of that keeps working with no package dependency.

## Upstream source of truth

- Repository: https://github.com/unoplatform/Uno.Diagnostics.Eventing
- Tag: `2.1.0` (commit `fb046f1`)
- Source equivalence: `git diff 2.0..2.1.0 -- src/Uno.Diagnostics.Eventing/`
  shows ZERO C# changes (only csproj packaging) — so these files are
  byte-identical to what the previously consumed 2.0.1 package was built
  from.
- License: Apache-2.0, copyright nventive — the same license and copyright
  holder as this repository's upstream (uno). Diligence note: the upstream
  `License.md` header says "Uno.MonoAnalyzers" (a copy-paste artifact from a
  sibling repo), but the grant itself — Apache 2.0, nventive, covering that
  repository — is unambiguous.

## Vendored files (verbatim copies of `src/Uno.Diagnostics.Eventing/*.cs`)

`Tracing.cs`, `IEventProvider.cs`, `IEventProviderFactory.cs`,
`NullEventProvider.cs`, `EventProviderExtensions.cs`, `EventActivity.cs`,
`EventDescriptor.cs`, `EventOpcode.cs`

The sibling `Uno.Diagnostics.Eventing.Providers` project (file/manifest
event sinks, a separate upstream package) was never referenced by this
repository and is NOT vendored.

## What was removed along with the package

- `PackageReference Include="Uno.Diagnostics.Eventing"` from all four
  Uno.Foundation flavors (base/Skia/Reference/Tests csprojs).
- The version pin in `src/Directory.Build.targets`.
- The `<dependency id="Uno.Diagnostics.Eventing" .../>` entry in
  `build/nuget/Platform.WinUI.nuspec` (the types now ship inside
  Uno.Foundation.dll, so consumers need no separate package).
