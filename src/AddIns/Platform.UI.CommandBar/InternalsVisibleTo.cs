using System.Runtime.CompilerServices;

// The add-in's unit suite drives internals directly: the inherited attached properties'
// storage, the icon rasterisation cache and the tool bar's overflow partition are all
// implementation detail that must not widen the public surface just to be measurable.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.CommandBar.Tests")]
// The host-free engine proof (WPE1 C14): the icon helpers of the Engine namespace, with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]

// CodeBrix.Platform.UI.CommandBar.Core (the platform-neutral assembly this file is compiled into since the Core/Skia
// split): its Skia twin, which implements the add-in's platform contract (Contracts/IIconRasterizationPlatform),
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.CommandBar")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.CommandBar")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.CommandBar.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.CommandBar")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.CommandBar.Tests")]
