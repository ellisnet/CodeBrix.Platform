#nullable enable

namespace CodeBrix.Platform.UI.Hosting;

/// <summary>
/// App-wide switch for the opt-in direct GPU rendering mode - EXPERIMENTAL. When enabled, it lets add-ins
/// that produce GPU content composite it directly on the head's GPU device where the head offers one
/// (Vulkan or Metal renderers), instead of reading frames back to the processor; heads without such a
/// renderer ignore it.
/// </summary>
/// <remarks>
/// <para>
/// This is a deliberate opt-in, turned on once by
/// <see cref="CodeBrixPlatformHostBuilderExtensions.UseDirectGpuRenderingMode"/> during host build. It is
/// a one-way latch: there is no public way to set it, and no way to turn it off — an app either runs in
/// this mode for its whole lifetime (the call is present) or never (the call is omitted). When it is off,
/// nothing changes: every add-in keeps its read-back path and every head keeps its default renderer.
/// </para>
/// <para>
/// When it is on and the application made no explicit renderer choice for the X11 head, the X11 head
/// tries its Vulkan renderer first and falls back to its normal OpenGL/software chain if Vulkan cannot be
/// created. The Wayland head already renders with Vulkan by default; the macOS head renders with Metal by
/// default. An add-in that cannot use the head's device (no GPU renderer, or anything on the direct path
/// fails) falls back to its read-back path on its own.
/// </para>
/// <para>
/// The switch is process-wide by design; there is intentionally no per-element override, so an app is
/// entirely in this mode or entirely out of it.
/// </para>
/// </remarks>
public static class DirectGpuRenderingMode
{
	private static bool _isEnabled;

	/// <summary>
	/// Whether direct GPU rendering is enabled for this app. <see langword="false"/> unless
	/// <see cref="CodeBrixPlatformHostBuilderExtensions.UseDirectGpuRenderingMode"/> was called.
	/// </summary>
	public static bool IsEnabled => _isEnabled;

	// Latches on. Only CodeBrixPlatformHostBuilderExtensions.UseDirectGpuRenderingMode() (and, on Android,
	// the application class's opt-in through the Core assembly's friend grant) calls this; there is
	// deliberately no way to turn it back off.
	internal static void Enable() => _isEnabled = true;
}
