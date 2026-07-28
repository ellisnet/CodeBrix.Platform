// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI;

namespace CodeBrix.Platform.UI.Runtime.Skia;

/// <summary>
/// Glyphs from the Fluent symbols font, for the small pieces of platform UI —
/// the keyboard's arrow keys, the picker's "up one folder" button — that need a
/// pictogram rather than a word.
/// <para>
/// These are Private Use Area codepoints, so they mean nothing outside that one
/// font: any control painting one MUST also set its FontFamily to
/// <see cref="SymbolFontFamily"/>.
/// </para>
/// <para>
/// WHY NOT A PLAIN UNICODE ARROW. "↑" (U+2191) and its siblings are absent from
/// both Open Sans and Roboto — the fonts most CodeBrix.Platform applications
/// ship — so they only ever rendered because a desktop host happened to have
/// something like DejaVu Sans to fall back on. On a frame-buffer device carrying
/// nothing but the application's own fonts they came out as missing-glyph boxes.
/// The Fluent symbols font is a dependency of CodeBrix.Platform itself, so it is
/// present wherever the framework is, and it is the one font that font isolation
/// deliberately still allows.
/// </para>
/// </summary>
internal static class SymbolGlyphs
{
	/// <summary>Upwards arrow.</summary>
	internal const string ArrowUp = "";

	/// <summary>Downwards arrow.</summary>
	internal const string ArrowDown = "";

	/// <summary>Leftwards arrow.</summary>
	internal const string ArrowLeft = "";

	/// <summary>Rightwards arrow.</summary>
	internal const string ArrowRight = "";

	/// <summary>
	/// The Fluent symbols font these glyphs live in, read fresh each time so an
	/// application that replaces
	/// <see cref="FeatureConfiguration.Font.SymbolsFont"/> is honored.
	/// </summary>
	internal static FontFamily SymbolFontFamily =>
		new FontFamily(FeatureConfiguration.Font.SymbolsFont);

	/// <summary>
	/// True for a legend that is one of these glyphs rather than ordinary text,
	/// and therefore has to be painted in <see cref="SymbolFontFamily"/>.
	/// </summary>
	internal static bool IsSymbolGlyph(string legend) =>
		legend is ArrowUp or ArrowDown or ArrowLeft or ArrowRight;
}
