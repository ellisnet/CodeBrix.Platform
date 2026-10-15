using System.Runtime.CompilerServices;

// CodeBrix.Platform.UI.WebGpu.Core (the platform-neutral assembly this file is compiled into): its Skia twin, which
// holds the WebGpuRenderView element and drives the rules and the frame scheduler,
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebGpu.Skia")]
// the add-in's own test suite,
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.WebGpu.Skia.Tests")]
// and, by rule (decision P4), the CodeBrix.Android and CodeBrix.Mobile assemblies (and their tests) for the same library.
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.WebGpu")]
[assembly: InternalsVisibleTo("CodeBrix.Android.UI.WebGpu.Tests")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.WebGpu")]
[assembly: InternalsVisibleTo("CodeBrix.Mobile.UI.WebGpu.Tests")]

// The host-free Core suite (src/Platform.UI.Core.Tests) exercises the rules and the frame scheduler with no XAML.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Core.Tests")]

// The host-free engine proof: the frame scheduler and the timing rules with no XAML loaded.
[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Engine.Tests")]

// The PlayTest.WebGpu provider package's suite: the view's gate evaluation against the PlayTest head's real gate.
[assembly: InternalsVisibleTo("CodeBrix.Platform.PlayTest.WebGpu.Tests")]
