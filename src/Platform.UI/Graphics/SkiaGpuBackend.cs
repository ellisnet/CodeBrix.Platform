#nullable enable

namespace CodeBrix.Platform.Graphics; //Was previously: Uno.Graphics

/// <summary>
/// The GPU graphics backend that produced a Skia <c>GRContext</c> — informational/diagnostic. A
/// backend-neutral GPU Skia context (the Graphics3DGL add-in's <c>SkiaGpuContext</c>) reports which
/// API it actually resolved: OpenGL/GLES on the Windows, X11, Wayland and Frame Buffer heads; Metal
/// on macOS.
/// </summary>
/// <remarks>
/// This lives in a platform-agnostic (non-<c>.skia</c>) source file, and carries no SkiaSharp
/// dependency, so it is present in the reference assembly consumers compile against — unlike
/// <c>INativeSkiaGpuContext</c>, whose <c>GRContext</c> signature confines it to the Skia build.
/// </remarks>
public enum SkiaGpuBackend
{
	/// <summary>OpenGL / OpenGL ES, via an off-screen native GL context.</summary>
	OpenGL,

	/// <summary>Apple Metal (macOS).</summary>
	Metal,
}
