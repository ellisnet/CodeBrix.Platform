#nullable enable

using System;
using Microsoft.UI.Xaml.Documents.TextFormatting;

namespace CodeBrix.Platform.UI.TextLayout.Internal;

/// <summary>
/// Maps this add-in's framework-neutral enums onto the text engine's own types.
/// </summary>
/// <remarks>
/// The public surface deliberately exposes none of the underlying framework enums, so that a
/// consumer never has to reference XAML types to lay text out. Since the text-engine home change
/// (WPE1 C5) the engine's own types are not XAML types either (they carry the same values), so the
/// mapping is the same as before with the engine's names.
/// </remarks>
internal static class EnumConversions
{
	internal static ushort ToEngineWeight(this TextFontWeight weight) => (ushort)weight;

	internal static EngineFontStyle ToEngineStyle(this TextFontStyle style) => style switch
	{
		TextFontStyle.Normal => EngineFontStyle.Normal,
		TextFontStyle.Oblique => EngineFontStyle.Oblique,
		TextFontStyle.Italic => EngineFontStyle.Italic,
		_ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown font style."),
	};

	internal static EngineFontStretch ToEngineStretch(this TextFontStretch stretch) => stretch switch
	{
		TextFontStretch.Undefined => EngineFontStretch.Undefined,
		TextFontStretch.UltraCondensed => EngineFontStretch.UltraCondensed,
		TextFontStretch.ExtraCondensed => EngineFontStretch.ExtraCondensed,
		TextFontStretch.Condensed => EngineFontStretch.Condensed,
		TextFontStretch.SemiCondensed => EngineFontStretch.SemiCondensed,
		TextFontStretch.Normal => EngineFontStretch.Normal,
		TextFontStretch.SemiExpanded => EngineFontStretch.SemiExpanded,
		TextFontStretch.Expanded => EngineFontStretch.Expanded,
		TextFontStretch.ExtraExpanded => EngineFontStretch.ExtraExpanded,
		TextFontStretch.UltraExpanded => EngineFontStretch.UltraExpanded,
		_ => throw new ArgumentOutOfRangeException(nameof(stretch), stretch, "Unknown font stretch."),
	};

	internal static EngineTextAlignment ToEngineTextAlignment(this TextAlign align) => align switch
	{
		TextAlign.Left => EngineTextAlignment.Left,
		TextAlign.Center => EngineTextAlignment.Center,
		TextAlign.Right => EngineTextAlignment.Right,
		_ => throw new ArgumentOutOfRangeException(nameof(align), align, "Unknown text alignment."),
	};
}
