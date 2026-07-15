#nullable enable

namespace CodeBrix.Platform.UI.Hosting; //Was previously: Uno.UI.Hosting

/// <summary>
/// App-wide switch for the opt-in direct <see cref="SkiaSharp.Views.Windows.SKXamlCanvas"/> present
/// path. When enabled, <c>SKXamlCanvas</c> draws each frame straight into its on-screen
/// <c>WriteableBitmap</c> buffer instead of drawing into an intermediate staging buffer and copying,
/// removing one full-frame copy per paint.
/// </summary>
/// <remarks>
/// <para>
/// This is a deliberate opt-in, turned on once by
/// <see cref="CodeBrixPlatformHostBuilderExtensions.UseDirectSkiaCanvasMode"/> during host build. It
/// is a one-way latch: there is no public way to set it, and no way to turn it off — an app either
/// runs in this mode for its whole lifetime (the call is present) or never (the call is omitted).
/// When it is off, the <c>SKXamlCanvas</c> present path is byte-for-byte unchanged.
/// </para>
/// <para>
/// The switch is process-wide by design; there is intentionally no per-canvas override, so an app is
/// entirely in this mode or entirely out of it.
/// </para>
/// </remarks>
public static class DirectSkiaCanvasMode
{
	private static bool _isEnabled;

	/// <summary>
	/// Whether the direct <c>SKXamlCanvas</c> present path is enabled for this app. <see langword="false"/>
	/// unless <see cref="CodeBrixPlatformHostBuilderExtensions.UseDirectSkiaCanvasMode"/> was called.
	/// </summary>
	public static bool IsEnabled => _isEnabled;

	// Latches on. Only CodeBrixPlatformHostBuilderExtensions.UseDirectSkiaCanvasMode() calls this;
	// there is deliberately no way to turn it back off.
	internal static void Enable() => _isEnabled = true;
}
