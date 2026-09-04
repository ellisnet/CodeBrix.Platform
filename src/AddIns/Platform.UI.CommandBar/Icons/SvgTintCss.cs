using System.Globalization;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CodeBrix.Platform.UI.CommandBar;

/// <summary>
/// Turns a tint brush and a <see cref="IconTintMode"/> into the CSS the SVG parser is handed.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of the tinting mechanism. The platform's SVG route parses through
/// CodeBrix.SkiaSvg, which accepts an author stylesheet alongside the document, so a rule such as
/// <c>* { color: #2266DD; }</c> resolves every <c>currentColor</c> in the artwork without the file
/// being touched. Nothing here draws; nothing here knows about SkiaSharp.
/// </para>
/// <para>
/// The selector is <c>*</c> rather than <c>svg</c>: measured against CodeBrix.SkiaSvg, a type
/// selector on the root element does not reach the shapes inside it, while the universal selector
/// does, and it is equally harmless because the <c>color</c> property affects only artwork that
/// asked for <c>currentColor</c>.
/// </para>
/// </remarks>
internal static class SvgTintCss
{
	/// <summary>The colours <see cref="IconTintMode.ReplaceBlackAndWhite"/> treats as "no colour of
	/// its own", written the six ways an SVG file spells them.</summary>
	private static readonly string[] MonochromeLiterals =
	[
		"#000000", "#000", "black", "#ffffff", "#FFFFFF", "#fff", "#FFF", "white"
	];

	/// <summary>
	/// Composes the stylesheet for one tint, or null when nothing should be applied.
	/// </summary>
	/// <param name="tint">The tint brush; only a <see cref="SolidColorBrush"/> can tint artwork.</param>
	/// <param name="mode">How far the tint reaches.</param>
	/// <returns>A CSS snippet, or null to parse the file exactly as drawn.</returns>
	internal static string? Compose(Brush? tint, IconTintMode mode)
	{
		if (mode == IconTintMode.None || tint is not SolidColorBrush solid)
		{
			return null;
		}

		return Compose(solid.Color, mode);
	}

	/// <summary>
	/// Composes the stylesheet for one tint colour.
	/// </summary>
	/// <param name="tint">The colour to paint with. Its alpha is not carried into the stylesheet;
	/// use the element's <c>Opacity</c> for a translucent icon.</param>
	/// <param name="mode">How far the tint reaches.</param>
	/// <returns>A CSS snippet, or null when <paramref name="mode"/> is
	/// <see cref="IconTintMode.None"/>.</returns>
	internal static string? Compose(Color tint, IconTintMode mode)
	{
		if (mode == IconTintMode.None)
		{
			return null;
		}

		var colour = ToCssColor(tint);
		var css = $"* {{ color: {colour}; }}";

		if (mode == IconTintMode.ReplaceBlackAndWhite)
		{
			css += $" {SelectorFor("fill")} {{ fill: {colour}; }}"
				+ $" {SelectorFor("stroke")} {{ stroke: {colour}; }}";
		}

		return css;
	}

	/// <summary>Writes one colour the way CSS spells it.</summary>
	/// <param name="colour">The colour to write.</param>
	/// <returns>A six-digit hexadecimal colour, for example <c>#2266DD</c>.</returns>
	internal static string ToCssColor(Color colour)
		=> string.Create(
			CultureInfo.InvariantCulture,
			$"#{colour.R:X2}{colour.G:X2}{colour.B:X2}");

	/// <summary>The attribute selector matching every monochrome value of one attribute.</summary>
	/// <param name="attribute">Either <c>fill</c> or <c>stroke</c>.</param>
	/// <returns>A comma-separated attribute selector.</returns>
	private static string SelectorFor(string attribute)
		=> string.Join(",", System.Array.ConvertAll(MonochromeLiterals, v => $"[{attribute}=\"{v}\"]"));
}
