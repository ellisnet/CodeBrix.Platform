using global::System.Runtime.CompilerServices;

// Since the Core/Skia split this assembly holds only the Skia implementations; it keeps every grant the
// single pre-split assembly had (the list below is the one of the shared AssemblyInfo.cs, which now goes to
// the Core assembly), so every head, add-in and test that reached these internals still reaches them.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.RuntimeTests.Windows")]
[assembly: InternalsVisibleTo("SamplesApp")]
[assembly: InternalsVisibleTo("SamplesApp.Droid")]
[assembly: InternalsVisibleTo("SamplesApp.macOS")]
[assembly: InternalsVisibleTo("SamplesApp.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.App.Mcp.Client")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Toolkit.Tests")]

[assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]

// The UI Skia bootstrap registers this assembly's contracts first (SkiaPlatformBootstrap.EnsureRegistered).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI")]
