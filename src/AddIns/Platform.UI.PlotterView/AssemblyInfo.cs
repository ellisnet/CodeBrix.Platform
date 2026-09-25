using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.PlotterView.Tests")]
// The host-free engine proof (WPE1 C7): drives Engine/PlotHost with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]

// CodeBrix.Platform.UI.PlotterView.Core (the assembly this file is compiled into since the Core/Skia split): its Skia
// twin, which implements the add-in's platform contract (Contracts/IRenderCanvasPlatform; the typefaces come from the
// platform's font source, IFontSourcePlatform in Foundation.Core, since WPE1 C7),
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.PlotterView")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.PlotterView")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.PlotterView.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.PlotterView")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.PlotterView.Tests")]
