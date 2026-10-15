#nullable enable

namespace CodeBrix.Platform.Graphics;

/// <summary>
/// The graphics API of the native GPU device a head exposes through <c>INativeGpuDeviceProvider</c>.
/// </summary>
/// <remarks>
/// This lives in a platform-agnostic (non-<c>.skia</c>) source file and carries no SkiaSharp dependency, so
/// it is present in the Core assembly every consumer compiles against - unlike <c>INativeGpuDeviceProvider</c>,
/// whose <c>GRContext</c> member confines it to the Skia build (the same split as <see cref="SkiaGpuBackend"/>).
/// </remarks>
public enum NativeGpuDeviceKind
{
	/// <summary>A Vulkan device (the X11 head on its Vulkan renderer, the Wayland head on its Vulkan renderer).</summary>
	Vulkan,

	/// <summary>An Apple Metal device (the macOS head in Metal mode).</summary>
	Metal,
}
