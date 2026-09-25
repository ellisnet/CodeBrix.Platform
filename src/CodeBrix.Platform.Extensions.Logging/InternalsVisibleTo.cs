using System.Runtime.CompilerServices;

// CodeBrix.Platform.Extensions.Logging.Core (renamed to .Core by whole in the Core/Skia split): by rule (decision P4),
// the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.Extensions.Logging")]
[assembly: InternalsVisibleTo("CodeBrix.Android.Extensions.Logging.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.Extensions.Logging")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.Extensions.Logging.Tests")]
