using System.Runtime.CompilerServices;

// The Skia twin (CodeBrix.Platform.UI.Svg, the composition hookup).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg.Tests")]

// Reserved for the add-in's Android and Mobile platform assemblies and their tests (decision P4).
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.Svg.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Svg")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.Svg.Tests")]
