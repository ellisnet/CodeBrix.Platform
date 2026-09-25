using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.TerminalView.Tests")]
// The host-free engine proof (WPE1 C6): drives Engine/TerminalRenderer and Engine/TerminalInputEncoder with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]

// CodeBrix.Platform.UI.TerminalView.Core (the assembly this file is compiled into since the Core/Skia split): its Skia
// twin, which implements the add-in's platform contract (Contracts/IRenderCanvasPlatform, the canvas supply),
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.TerminalView")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TerminalView")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TerminalView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TerminalView")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TerminalView.Tests")]
