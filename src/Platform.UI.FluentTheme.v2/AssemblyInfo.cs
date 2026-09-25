using global::System.Reflection;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.Wasm")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Tests")]

// CodeBrix.Platform.UI.FluentTheme.v2.Core (the platform-neutral Core assembly this file is compiled into since the Core/Skia split):
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.FluentTheme.v2")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.FluentTheme.v2.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.FluentTheme.v2")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.FluentTheme.v2.Tests")]

// Element handler seam (WPH1, decisions D-P3 and the AP1-B cross-assembly rule): the CodeBrix.Android and CodeBrix.Mobile
// counterparts (and their tests) of every Skia-side assembly granted above - the framework Skia twins and the add-ins'
// platform assemblies - so that a platform implementation of a Core contract, or a platform handler, compiles against the
// same internals the Skia side uses.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Android")]
[assembly: InternalsVisibleTo("CodeBrix.Android.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.Tests")]

[assembly: AssemblyMetadata("IsTrimmable", "True")]

#if false
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: Foundation.LinkerSafe]
#pragma warning restore CS0618 // Type or member is obsolete
#elif false
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: Android.LinkerSafe]
#pragma warning restore CS0618 // Type or member is obsolete
#endif
