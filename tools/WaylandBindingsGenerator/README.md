# WaylandBindingsGenerator

Dev-only tool that generates the committed C# Wayland protocol bindings for the
native Wayland head (`src/Platform.UI.Runtime.Skia.Wayland/Wayland_Bindings/`).
It is a frozen, CodeBrix-owned fork of the MIT-licensed
[NWayland](https://github.com/AvaloniaUI/NWayland) bindings generator, driven by
pinned copies of the MIT protocol XML from the upstream freedesktop
`wayland` / `wayland-protocols` repositories.

**This tool is never packed into any NuGet package.**

Regenerate the bindings with:

```bash
dotnet run --project tools/WaylandBindingsGenerator/src/GeneratorRunner
```

then review and commit the diff under `Wayland_Bindings/`.

See `PORTING-NOTES.txt` for upstream commit SHAs, the fork patch list, and how
to add or upgrade a protocol; `LICENSE-NWayland.md` holds the upstream MIT
license text. Provenance and the licensing guard-rail for the whole Wayland
head effort live in `THIRD-PARTY-NOTICES_wayland_addendum.txt` at the repo root.
