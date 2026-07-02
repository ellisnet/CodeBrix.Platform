# CodeBrix.Platform.WinUI

Toolkit-style libraries for **WinUI** (Windows App SDK) applications — MVVM
primitives plus Skia-rendered SVG and Lottie visuals that match the
CodeBrix.Platform rendering pipeline.

These packages are **not** Uno-based and share no code with the Uno-derived
`CodeBrix.Platform.*` packages. The only shared source is the linked
`CodeBrix.Platform.Simple` files (namespace `CodeBrix.Platform.Simple`) and the
Lottie/SVG rendering code ported from the CodeBrix.Platform add-ins.

This is the README for the **CodeBrix.Platform.WinUI** NuGet family:

* **CodeBrix.Platform.WinUI.ApacheLicenseForever** — the `Core` MVVM toolkit
  (`SimpleViewModel`, `SimpleCommand`, dialogs, messaging, service resolution
  and OS info).
* **CodeBrix.Platform.WinUI.Skia.ApacheLicenseForever** — Skia-rendered image
  controls: `EmbeddedImage` and `EmbeddedImageButton` (namespace
  `CodeBrix.Platform.WinUI.Controls`), loading images via
  `embedded://AssemblyName/Resource.Name` or standard URIs. SVGs render
  vector-direct through CodeBrix.SkiaSvg at full display resolution — the same
  engine and drawing path the CodeBrix.Platform Skia heads use — so SVG assets
  look identical across both families.
* **CodeBrix.Platform.WinUI.Lottie.ApacheLicenseForever** — Skia-rendered
  Lottie player: `AnimatedVisualPlayer`, `LottieVisualSource` and
  `ThemableLottieVisualSource` (namespace `CodeBrix.Platform.WinUI.Lottie`),
  rendered with SkiaSharp.Skottie using the same playback engine and drawing
  math as the `CodeBrix.Platform.Lottie.ApacheLicenseForever` package.

```xml
<!-- XAML usage of the Lottie player -->
xmlns:lottie="using:CodeBrix.Platform.WinUI.Lottie"

<lottie:AnimatedVisualPlayer AutoPlay="True" Width="50" Height="40">
    <lottie:LottieVisualSource UriSource="ms-appx:///Assets/animation.json" />
</lottie:AnimatedVisualPlayer>
```

Additional `CodeBrix.Platform.WinUI.*` packages may be added to this family over
time.

## Target framework

`net10.0-windows10.0.19041.0` (Windows App SDK; minimum platform
`10.0.17763.0`).

## License

Licensed under the Apache License, Version 2.0.
See: https://en.wikipedia.org/wiki/Apache_License
