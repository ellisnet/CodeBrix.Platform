// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head globs this folder from its csproj). Keep head-neutral.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// Resolves the theme resources that style ContentDialog, so this head's built-in
/// chrome (the file/folder picker dialogs and the software keyboard) renders — and
/// can be restyled by the application — the same way as the framework's own
/// dialogs: overriding a ContentDialog resource key in the application's resources
/// restyles this chrome identically. Lookups go through the full resolution chain
/// (application, merged/theme dictionaries for the active theme, then system
/// resources), so the keys resolve wherever ContentDialog's own would.
/// </summary>
internal static class DialogThemeResources
{
	/// <summary>
	/// Resolves a brush resource, falling back to the given standard Fluent
	/// light-theme value when the key cannot be resolved.
	/// </summary>
	internal static Brush Brush(string key, Color fallback)
		=> Resolve(key) switch
		{
			Brush brush => brush,
			Color color => new SolidColorBrush(color),
			_ => new SolidColorBrush(fallback),
		};

	/// <summary>
	/// Resolves a resource to a plain color (for chrome shades that are computed
	/// rather than drawn directly), falling back to the given standard Fluent
	/// light-theme value when the key cannot be resolved to a solid color.
	/// </summary>
	internal static Color ColorOf(string key, Color fallback)
		=> Resolve(key) switch
		{
			Color color => color,
			SolidColorBrush brush => brush.Color,
			_ => fallback,
		};

	/// <summary>
	/// Composites <paramref name="over"/> at <paramref name="alpha"/> onto the
	/// opaque <paramref name="under"/>, so derived shades (e.g. the keyboard's
	/// special keys) stay coherent with whatever colors the theme resolved to.
	/// </summary>
	internal static Color Composite(Color over, double alpha, Color under)
	{
		var effective = alpha * (over.A / 255d);
		return Color.FromArgb(
			0xFF,
			(byte)(over.R * effective + under.R * (1 - effective)),
			(byte)(over.G * effective + under.G * (1 - effective)),
			(byte)(over.B * effective + under.B * (1 - effective)));
	}

	private static object? Resolve(string key)
		=> Application.Current?.Resources is { } resources && resources.TryGetValue(key, out var value)
			? value
			: null;
}
