using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.AppSettings.Tests")]

// CodeBrix.Platform.AppSettings.Core (the platform-neutral assembly this file is compiled into since the Core/Skia
// split): its Skia twin, which implements the add-in's storage contract (Contracts/IAppSettingsStoragePlatform),
[assembly: InternalsVisibleTo("CodeBrix.Platform.AppSettings")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.AppSettings")]
[assembly: InternalsVisibleTo("CodeBrix.Android.AppSettings.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.AppSettings")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.AppSettings.Tests")]

// The host-free Core suite (src/Platform.UI.Core.Tests) registers test doubles for the platform contracts.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core.Tests")]

// The host-free engine proof (src/Platform.UI.Engine.Tests, WPE1 C3) drives the settings engine over an in-memory storage
// double and asserts that no WinUI assembly is loaded.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]
