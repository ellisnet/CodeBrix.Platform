#nullable enable

using System;
using Microsoft.UI.Xaml;
using Windows.UI.Text;

namespace CodeBrix.Platform.UI.TextLayout.Internal;

/// <summary>
/// Maps this add-in's framework-neutral enums onto the engine's own types.
/// </summary>
/// <remarks>
/// The public surface deliberately exposes none of the underlying framework enums, so that a
/// consumer never has to reference XAML types to lay text out. This is the whole of the cost of
/// that choice.
/// </remarks>
internal static class EnumConversions
{
	internal static FontWeight ToFontWeight(this TextFontWeight weight) => new((ushort)weight);

	internal static FontStyle ToFontStyle(this TextFontStyle style) => style switch
	{
		TextFontStyle.Normal => FontStyle.Normal,
		TextFontStyle.Oblique => FontStyle.Oblique,
		TextFontStyle.Italic => FontStyle.Italic,
		_ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown font style."),
	};

	internal static FontStretch ToFontStretch(this TextFontStretch stretch) => stretch switch
	{
		TextFontStretch.Undefined => FontStretch.Undefined,
		TextFontStretch.UltraCondensed => FontStretch.UltraCondensed,
		TextFontStretch.ExtraCondensed => FontStretch.ExtraCondensed,
		TextFontStretch.Condensed => FontStretch.Condensed,
		TextFontStretch.SemiCondensed => FontStretch.SemiCondensed,
		TextFontStretch.Normal => FontStretch.Normal,
		TextFontStretch.SemiExpanded => FontStretch.SemiExpanded,
		TextFontStretch.Expanded => FontStretch.Expanded,
		TextFontStretch.ExtraExpanded => FontStretch.ExtraExpanded,
		TextFontStretch.UltraExpanded => FontStretch.UltraExpanded,
		_ => throw new ArgumentOutOfRangeException(nameof(stretch), stretch, "Unknown font stretch."),
	};

	internal static TextAlignment ToTextAlignment(this TextAlign align) => align switch
	{
		TextAlign.Left => TextAlignment.Left,
		TextAlign.Center => TextAlignment.Center,
		TextAlign.Right => TextAlignment.Right,
		_ => throw new ArgumentOutOfRangeException(nameof(align), align, "Unknown text alignment."),
	};
}
