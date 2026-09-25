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
/// <para>
/// WPE1 C14: the composing itself is the Engine's (Engine/SvgTintCss, over red/green/blue); these XAML-typed overloads
/// are its adapter.
/// </para>
/// </remarks>
internal static class SvgTintCss
{
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
		=> Engine.SvgTintCss.Compose(tint.R, tint.G, tint.B, mode);

	/// <summary>Writes one colour the way CSS spells it.</summary>
	/// <param name="colour">The colour to write.</param>
	/// <returns>A six-digit hexadecimal colour, for example <c>#2266DD</c>.</returns>
	internal static string ToCssColor(Color colour)
		=> Engine.SvgTintCss.ToCssColor(colour.R, colour.G, colour.B);
}
