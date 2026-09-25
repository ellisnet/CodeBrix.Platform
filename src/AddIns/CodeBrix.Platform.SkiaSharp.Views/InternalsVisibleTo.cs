using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.SkiaSharp.Views.Tests")]

// The Skia twin (CodeBrix.Platform.SkiaSharp.Views) implements this assembly's per-element contracts.
[assembly: InternalsVisibleTo("CodeBrix.Platform.SkiaSharp.Views")]

// Reserved for the add-in's Android and Mobile platform assemblies and their tests (decision P4).
[assembly: InternalsVisibleTo("CodeBrix.Android.SkiaSharp.Views")]
[assembly: InternalsVisibleTo("CodeBrix.Android.SkiaSharp.Views.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.SkiaSharp.Views")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.SkiaSharp.Views.Tests")]

// The drawing add-ins that build on the Skia-canvas host seam (Hosting/SKCanvasHost.cs, SKCanvasHostElement.cs).
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Svg.Core")]
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Lottie.Core")]
