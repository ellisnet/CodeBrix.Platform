using System;
using Windows.Foundation;
using Windows.Graphics;

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// The single place where a Skia head converts the window-sizing numbers an application supplies -
/// which are always EFFECTIVE PIXELS (also called logical pixels: the unit the XAML tree is laid
/// out in) - into the units the native windowing call underneath that head expects.
/// </summary>
/// <remarks>
/// <para>
/// Two public seams take effective pixels: <c>ApplicationView.PreferredLaunchViewSize</c> and
/// <c>OverlappedPresenter.PreferredMinimumWidth</c> / <c>PreferredMinimumHeight</c> /
/// <c>PreferredMaximumWidth</c> / <c>PreferredMaximumHeight</c>. A head whose native call already
/// speaks effective pixels (Wayland's surface-local window geometry, an NSWindow's points, a WPF
/// window's device-independent units) passes the number through untouched; a head whose native call
/// speaks raw device pixels (X11's <c>XCreateWindow</c> and <c>XSetWMNormalHints</c>, Win32's window
/// rect and <c>WM_GETMINMAXINFO</c>) multiplies by the display scale using the methods here.
/// </para>
/// <para>
/// Nothing in this class talks to a windowing system: it is pure arithmetic, so the rule can be read
/// in one place and fenced by a host-free unit test.
/// </para>
/// </remarks>
internal static class WindowSizeConversion
{
	/// <summary>
	/// The scale a head falls back to when it cannot read a usable one - for instance before the
	/// first window exists, when no display has been resolved yet.
	/// </summary>
	internal const double FallbackScale = 1.0;

	/// <summary>
	/// Returns <paramref name="scale"/> when it is a usable multiplier, and <see cref="FallbackScale"/>
	/// when it is not (zero, negative, infinite or NaN), so a head that reads a nonsense scale from the
	/// windowing system still gets a window instead of a division by zero or a zero-sized one.
	/// </summary>
	/// <param name="scale">The candidate raw-pixels-per-effective-pixel value.</param>
	/// <returns>A strictly positive, finite scale.</returns>
	internal static double NormalizeScale(double scale)
		=> double.IsFinite(scale) && scale > 0 ? scale : FallbackScale;

	/// <summary>
	/// Converts a size given in effective pixels into raw device pixels.
	/// </summary>
	/// <param name="logicalSize">The size in effective pixels.</param>
	/// <param name="scale">Raw pixels per effective pixel.</param>
	/// <returns>
	/// The same size in raw pixels. An empty size is returned unchanged, and a dimension that was
	/// positive stays at least one pixel, so a small window can never be rounded out of existence.
	/// </returns>
	internal static Size LogicalToNative(Size logicalSize, double scale)
	{
		if (logicalSize.IsEmpty)
		{
			return logicalSize;
		}

		var normalized = NormalizeScale(scale);

		return new Size(
			ScaleDimension(logicalSize.Width, normalized),
			ScaleDimension(logicalSize.Height, normalized));
	}

	/// <summary>
	/// Converts a size given in effective pixels into raw device pixels.
	/// </summary>
	/// <param name="logicalSize">The size in effective pixels.</param>
	/// <param name="scale">Raw pixels per effective pixel.</param>
	/// <returns>The same size in raw pixels, each dimension rounded to the nearest whole pixel.</returns>
	internal static SizeInt32 LogicalToNative(SizeInt32 logicalSize, double scale)
		=> new()
		{
			Width = LogicalToNative(logicalSize.Width, scale),
			Height = LogicalToNative(logicalSize.Height, scale),
		};

	/// <summary>
	/// Converts a single measurement given in effective pixels into raw device pixels.
	/// </summary>
	/// <param name="logicalValue">The measurement in effective pixels.</param>
	/// <param name="scale">Raw pixels per effective pixel.</param>
	/// <returns>
	/// The same measurement in raw pixels. Zero and negative values - which the presenter seams use
	/// to mean "unconstrained" - are returned unchanged, and a value that would overflow saturates at
	/// <see cref="int.MaxValue"/>, which is the sentinel the heads already pass for "no maximum".
	/// </returns>
	internal static int LogicalToNative(int logicalValue, double scale)
	{
		if (logicalValue <= 0)
		{
			return logicalValue;
		}

		var scaled = Math.Round(logicalValue * NormalizeScale(scale), MidpointRounding.AwayFromZero);

		return scaled >= int.MaxValue ? int.MaxValue : (int)scaled;
	}

	/// <summary>
	/// Converts an optional measurement given in effective pixels into raw device pixels, leaving
	/// <c>null</c> - the presenter's "not set" - as <c>null</c>.
	/// </summary>
	/// <param name="logicalValue">The measurement in effective pixels, or <c>null</c>.</param>
	/// <param name="scale">Raw pixels per effective pixel.</param>
	/// <returns>The same measurement in raw pixels, or <c>null</c>.</returns>
	internal static int? LogicalToNative(int? logicalValue, double scale)
		=> logicalValue is { } value ? LogicalToNative(value, scale) : null;

	private static double ScaleDimension(double logicalValue, double normalizedScale)
	{
		if (!double.IsFinite(logicalValue) || logicalValue <= 0)
		{
			return logicalValue;
		}

		return Math.Max(1, Math.Round(logicalValue * normalizedScale, MidpointRounding.AwayFromZero));
	}
}
