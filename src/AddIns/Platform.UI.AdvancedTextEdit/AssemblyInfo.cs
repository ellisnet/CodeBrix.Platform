using System.Runtime.CompilerServices;

// The document, highlighting and rendering internals are exercised directly by the unit-test
// suite (host-free document trees and layout geometry, without a running application head).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AdvancedTextEdit.Tests")]
// The host-free engine proof (WPE1 C8): drives the document, highlighting, folding and completion filter with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]

// CodeBrix.Platform.UI.AdvancedTextEdit.Core (the assembly this file is compiled into since the Core/Skia split): its
// Skia twin, which implements the add-in's platform contract (Contracts/IRenderCanvasPlatform, the canvas supply),
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.AdvancedTextEdit")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AdvancedTextEdit")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.AdvancedTextEdit.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AdvancedTextEdit")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.AdvancedTextEdit.Tests")]
