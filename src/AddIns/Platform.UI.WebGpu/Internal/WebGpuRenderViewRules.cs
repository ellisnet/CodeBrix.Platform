using System;
using CodeBrix.Platform.Graphics;

namespace CodeBrix.Platform.UI.WebGpu.Internal;

/// <summary>
/// The <c>WebGpuRenderView</c> element's decisions that are pure arithmetic or pure policy, kept apart from the
/// element so they can be exercised without a window or a GPU.
/// </summary>
internal static class WebGpuRenderViewRules
{
	/// <summary>The default <c>TargetFrameRate</c>, in frames per second.</summary>
	internal const double DefaultTargetFrameRate = 60.0;

	/// <summary>
	/// Quiet period after the last size change before live presents resume (the VideoPlayer surface's value, for
	/// the same reason: a resize drag must not pile full-size blits onto the render thread).
	/// </summary>
	internal const double ResizeSettleMilliseconds = 500.0;

	/// <summary>
	/// Whether a size change is a RESIZE - one that suppresses live presents until the size has been quiet for
	/// <see cref="ResizeSettleMilliseconds"/> - rather than the element getting its first size. The first layout
	/// after the view is loaded comes from no size at all; treating it as a drag would hide every frame of the first
	/// half second each time the view is shown (the picture would appear only when the settle period ends).
	/// </summary>
	/// <param name="previousWidth">The width before the change.</param>
	/// <param name="previousHeight">The height before the change.</param>
	/// <returns>True when the element already had an area before this change.</returns>
	internal static bool IsResize(double previousWidth, double previousHeight) => previousWidth > 0 && previousHeight > 0;

	/// <summary>
	/// The largest texture side the view asks for: WebGPU's guaranteed <c>maxTextureDimension2D</c>. A larger
	/// element is rendered at this size and stretched.
	/// </summary>
	internal const int MaxTextureDimension = 8192;

	/// <summary>Whether <paramref name="framesPerSecond"/> is a usable <c>TargetFrameRate</c>.</summary>
	/// <param name="framesPerSecond">The candidate rate.</param>
	/// <returns>True for zero (as fast as possible) and for any finite positive rate.</returns>
	internal static bool IsValidTargetFrameRate(double framesPerSecond) =>
		!double.IsNaN(framesPerSecond) && !double.IsInfinity(framesPerSecond) && framesPerSecond >= 0.0;

	/// <summary>
	/// The time between two Continuous frames, in stopwatch ticks.
	/// </summary>
	/// <param name="framesPerSecond">The target rate (zero = as fast as possible).</param>
	/// <param name="ticksPerSecond">The clock's resolution (<c>Stopwatch.Frequency</c>).</param>
	/// <returns>The period; zero for a rate of zero.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The rate is negative, NaN or infinite.</exception>
	internal static long FramePeriodTicks(double framesPerSecond, long ticksPerSecond)
	{
		if (!IsValidTargetFrameRate(framesPerSecond))
		{
			throw new ArgumentOutOfRangeException(nameof(framesPerSecond), framesPerSecond,
				"TargetFrameRate must be zero (as fast as possible) or a finite positive number of frames per second.");
		}

		return framesPerSecond == 0.0 ? 0L : (long)Math.Round(ticksPerSecond / framesPerSecond);
	}

	/// <summary>
	/// When the next Continuous frame is due: one period after the previous deadline - unless that moment has already
	/// passed, in which case it is due NOW. Latest wins: missed frames are dropped, never queued, so a slow frame (or a
	/// busy user-interface thread) costs frame rate and never builds a backlog of frames to catch up on.
	/// </summary>
	/// <param name="previousDeadline">When the frame that just ran was due (stopwatch ticks).</param>
	/// <param name="now">The time now (stopwatch ticks).</param>
	/// <param name="periodTicks">The frame period (zero = as fast as possible).</param>
	/// <returns>The next deadline, never earlier than <paramref name="now"/>.</returns>
	internal static long NextDeadline(long previousDeadline, long now, long periodTicks)
	{
		if (periodTicks <= 0)
		{
			return now;
		}

		var next = previousDeadline + periodTicks;
		return next < now ? now : next;
	}

	/// <summary>
	/// The size of the texture the view renders into: the element's layout size times its rasterization scale,
	/// rounded to whole pixels and capped at <see cref="MaxTextureDimension"/>.
	/// </summary>
	/// <param name="width">The element's width in effective pixels.</param>
	/// <param name="height">The element's height in effective pixels.</param>
	/// <param name="scale">The rasterization scale (1 = 96 DPI).</param>
	/// <returns>The pixel size; (0, 0) when either side rounds to nothing (the view then renders no frame).</returns>
	internal static (int Width, int Height) TargetPixelSize(double width, double height, double scale)
	{
		if (double.IsNaN(scale) || scale <= 0.0 || double.IsInfinity(scale))
		{
			scale = 1.0;
		}

		var w = ToPixels(width * scale);
		var h = ToPixels(height * scale);
		return w == 0 || h == 0 ? (0, 0) : (w, h);

		static int ToPixels(double value) =>
			double.IsNaN(value) || value < 0.5 ? 0
			: value >= MaxTextureDimension ? MaxTextureDimension
			: (int)Math.Round(value, MidpointRounding.AwayFromZero);
	}

	/// <summary>
	/// Whether a head's native GPU device is one the view could adopt for a direct path: only when the application
	/// opted in (<c>UseDirectGpuRenderingMode()</c>) AND the head resolved a provider of a kind a direct path exists
	/// for, and that provider is available right now.
	/// </summary>
	/// <param name="directGpuRenderingEnabled">The application-wide latch (<c>DirectGpuRenderingMode.IsEnabled</c>).</param>
	/// <param name="providerKind">The resolved provider's kind, or null when the head registered none for the window.</param>
	/// <param name="providerAvailable">Whether the provider has a live device now.</param>
	/// <returns>True when a direct path may be attempted.</returns>
	internal static bool IsDirectPathEligible(bool directGpuRenderingEnabled, NativeGpuDeviceKind? providerKind, bool providerAvailable) =>
		directGpuRenderingEnabled
		&& providerAvailable
		&& providerKind is NativeGpuDeviceKind.Vulkan or NativeGpuDeviceKind.Metal;

	/// <summary>The direct render path a provider kind leads to.</summary>
	/// <param name="kind">The provider's kind.</param>
	/// <returns><see cref="WebGpuRenderPath.DirectVulkan"/> or <see cref="WebGpuRenderPath.DirectMetal"/>.</returns>
	internal static WebGpuRenderPath DirectPathFor(NativeGpuDeviceKind kind) =>
		kind == NativeGpuDeviceKind.Metal ? WebGpuRenderPath.DirectMetal : WebGpuRenderPath.DirectVulkan;

	/// <summary>
	/// The render path a newly created device runs on: the direct path for an eligible provider when an adapter for
	/// that path exists, otherwise Readback - which is ALWAYS the fallback, never an error.
	/// </summary>
	/// <param name="eligible">The result of <see cref="IsDirectPathEligible"/>.</param>
	/// <param name="providerKind">The provider's kind (ignored when not eligible).</param>
	/// <param name="directAdapterExists">Whether this build has an adapter for that direct path.</param>
	/// <returns>The path to run.</returns>
	internal static WebGpuRenderPath ChooseRenderPath(bool eligible, NativeGpuDeviceKind? providerKind, bool directAdapterExists) =>
		eligible && directAdapterExists && providerKind is { } kind ? DirectPathFor(kind) : WebGpuRenderPath.Readback;

	/// <summary>
	/// How long, from the moment the view is loaded, its first device waits for a head's native GPU device that the
	/// head has announced but not created yet (the Wayland head creates its renderer on its first frame), before
	/// settling for Readback.
	/// </summary>
	internal const double DirectProviderWaitMilliseconds = 2000.0;

	/// <summary>
	/// Whether this build has a direct adapter for a provider kind on this operating system: Vulkan everywhere (the
	/// heads that offer it are the Linux ones); Metal only on macOS, the one head that offers it.
	/// </summary>
	/// <param name="kind">The provider's kind.</param>
	/// <param name="isMacOS">Whether the process runs on macOS.</param>
	/// <returns>True when a direct adapter can be attempted.</returns>
	internal static bool DirectAdapterExists(NativeGpuDeviceKind kind, bool isMacOS) =>
		kind == NativeGpuDeviceKind.Vulkan || (kind == NativeGpuDeviceKind.Metal && isMacOS);

	/// <summary>
	/// Whether the first device should wait (render no frame yet) for a provider that exists but has no device yet,
	/// instead of starting on Readback: only with the latch on, only for a kind a direct path exists for, and only
	/// within <see cref="DirectProviderWaitMilliseconds"/> of the view being loaded.
	/// </summary>
	/// <param name="directGpuRenderingEnabled">The application-wide latch.</param>
	/// <param name="providerKind">The resolved provider's kind, or null when the head registered none.</param>
	/// <param name="providerAvailable">Whether the provider has a live device now.</param>
	/// <param name="millisecondsSinceAttach">The time since the view was loaded.</param>
	/// <returns>True to skip this frame and ask again at the next one.</returns>
	internal static bool ShouldWaitForProvider(bool directGpuRenderingEnabled, NativeGpuDeviceKind? providerKind, bool providerAvailable, double millisecondsSinceAttach) =>
		directGpuRenderingEnabled
		&& !providerAvailable
		&& providerKind is NativeGpuDeviceKind.Vulkan or NativeGpuDeviceKind.Metal
		&& millisecondsSinceAttach >= 0.0
		&& millisecondsSinceAttach < DirectProviderWaitMilliseconds;

	/// <summary>The log line written once when a direct path was eligible but none exists for it in this build.</summary>
	/// <param name="kind">The provider's kind.</param>
	/// <returns>The message.</returns>
	internal static string DirectPathNotImplementedMessage(NativeGpuDeviceKind kind) =>
		$"Direct GPU rendering is enabled and the head offers a {kind} device, but there is no direct path for it on " +
		"this platform - Readback is used.";

	/// <summary>
	/// The ONE log line written when a direct path was attempted and failed at some step: names the path and why;
	/// the view continues on Readback.
	/// </summary>
	/// <param name="path">The direct path that failed.</param>
	/// <param name="reason">Why.</param>
	/// <returns>The message.</returns>
	internal static string DirectPathFallbackMessage(WebGpuRenderPath path, string reason) =>
		$"The {path} render path could not be used ({reason}) - Readback is used.";

	/// <summary>
	/// Whether the view may adopt a head's GPU device at all: only with the application's latch on, and only when the
	/// view does not require its own device (<c>RequireOwnedDevice</c>). Latch + RequireOwnedDevice = Readback on an
	/// owned device created from <c>DeviceOptions</c>.
	/// </summary>
	/// <param name="directGpuRenderingEnabled">The application-wide latch.</param>
	/// <param name="requireOwnedDevice">The view's <c>RequireOwnedDevice</c>.</param>
	/// <returns>True when a direct path may be attempted.</returns>
	internal static bool MayAdoptHeadDevice(bool directGpuRenderingEnabled, bool requireOwnedDevice) =>
		directGpuRenderingEnabled && !requireOwnedDevice;

	/// <summary>
	/// Whether a <c>DeviceOptions</c> change replaces the device that exists: only a device the view OWNS was created
	/// from the options (Readback); an adopted device is the head's.
	/// </summary>
	/// <param name="path">The render path running now.</param>
	/// <returns>True to replace the device at the next frame.</returns>
	internal static bool DeviceOptionsChangeRecreatesDevice(WebGpuRenderPath path) => path == WebGpuRenderPath.Readback;

	/// <summary>
	/// Whether a <c>RequireOwnedDevice</c> change replaces the device that exists: only with the latch on (without it the
	/// device is always the view's own) and only while there is a device.
	/// </summary>
	/// <param name="directGpuRenderingEnabled">The application-wide latch.</param>
	/// <param name="path">The render path running now.</param>
	/// <returns>True to replace the device at the next frame.</returns>
	internal static bool RequireOwnedDeviceChangeRecreatesDevice(bool directGpuRenderingEnabled, WebGpuRenderPath path) =>
		directGpuRenderingEnabled && path != WebGpuRenderPath.None;

	/// <summary>The DeviceLost reason when a device is replaced because DeviceOptions or RequireOwnedDevice changed.</summary>
	internal const string DeviceOptionsChangedReason = "Device options changed.";

	/// <summary>The log line written once when the latch is on but the view requires its own device.</summary>
	internal const string RequireOwnedDeviceMessage =
		"Direct GPU rendering is enabled, but this view requires its own device (RequireOwnedDevice) - Readback is used.";
}
