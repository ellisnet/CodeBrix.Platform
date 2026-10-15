using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("CodeBrix.Platform.UI.Runtime.Skia.PlayTest.Tests")]
// The PlayTest.WebGpu provider package's suite drives the launch-time WebGPU setup (plan, probe, gate) with the real
// provider and its own registry.
[assembly: InternalsVisibleTo("CodeBrix.Platform.PlayTest.WebGpu.Tests")]
