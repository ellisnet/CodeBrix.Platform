using System.Runtime.CompilerServices;

// The flex layout engine (Internal/Flex.cs) is internal; the unit-test suite drives it directly
// (host-free Item trees, the same model the original xamarin/flex C test suite uses).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.FlexPanel.Tests")]

// CodeBrix.Platform.UI.FlexPanel.Core is Core by whole (no Skia twin). By rule (decision P4), the CodeBrix.Android and
// CodeBrix.Mobile assemblies for the same library (and their tests) are granted access now, so a platform library
// that needs these internals later does not cost a release of this package.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.FlexPanel")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.FlexPanel.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.FlexPanel")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.FlexPanel.Tests")]
