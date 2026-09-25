using System.Runtime.CompilerServices;

// CodeBrix.Platform.UI.TextLayout.Core (the whole add-in since the text-engine home change, WPE1 C5; it has no Skia twin
// any more): the host-free engine proof (src/Platform.UI.Engine.Tests), which lays text out with no XAML assembly loaded;
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]

// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TextLayout")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.TextLayout.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TextLayout")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.TextLayout.Tests")]
