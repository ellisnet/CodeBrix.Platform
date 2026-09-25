using System.Runtime.CompilerServices;

// CodeBrix.Platform.UI.VideoPlayer.Core (the platform-neutral assembly this file is compiled into since the Core/Skia
// split): its Skia twin, which holds the VideoPlayer element and uses the rules, the source resolver and the failure
// event args,
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.VideoPlayer.Skia")]
// the add-in's own test suite (as before the split),
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.VideoPlayer.Tests")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.VideoPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.VideoPlayer.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.VideoPlayer")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.VideoPlayer.Tests")]

// The host-free Core suite (src/Platform.UI.Core.Tests) exercises the rules and the resolver.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core.Tests")]

// The host-free engine proof (WPE1 C11): the rules and the resolver (over IAssetLocation) with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]
