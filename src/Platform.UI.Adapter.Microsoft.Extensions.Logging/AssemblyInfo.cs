using global::System.Reflection;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;

// CodeBrix.Platform.UI.Adapter.Microsoft.Extensions.Logging.Core (renamed to .Core by whole in the Core/Skia split):
// by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Adapter.Microsoft.Extensions.Logging")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Adapter.Microsoft.Extensions.Logging.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Adapter.Microsoft.Extensions.Logging")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Adapter.Microsoft.Extensions.Logging.Tests")]
