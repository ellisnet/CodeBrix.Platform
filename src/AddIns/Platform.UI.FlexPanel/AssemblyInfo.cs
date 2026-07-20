using System.Runtime.CompilerServices;

// The flex layout engine (Internal/Flex.cs) is internal; the unit-test suite drives it directly
// (host-free Item trees, the same model the original xamarin/flex C test suite uses).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.FlexPanel.Unit.Tests")]
