# CodeBrix.Platform.Mobile

Toolkit-style libraries for **.NET MAUI** applications — starting with the
platform-agnostic CodeBrix "Simple" MVVM toolkit (`SimpleViewModel`,
`SimpleCommand`, dialogs, messaging, service resolution and OS info) compiled
for MAUI targets.

These packages are **not** Uno-based and share no code with the Uno-derived
`CodeBrix.Platform.*` packages. The only shared source is the linked
`CodeBrix.Platform.Simple` files (namespace `CodeBrix.Platform.Simple`).

This is the README for the **CodeBrix.Platform.Mobile** NuGet family. Today the
family has a single package:

* **CodeBrix.Platform.Mobile.ApacheLicenseForever** — the `Core` MVVM toolkit.

Additional `CodeBrix.Platform.Mobile.*` packages may be added to this family
over time.

## Target frameworks

`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst` (and
`net10.0-windows10.0.19041.0` when built on Windows).

## License

Licensed under the Apache License, Version 2.0.
See: https://en.wikipedia.org/wiki/Apache_License
