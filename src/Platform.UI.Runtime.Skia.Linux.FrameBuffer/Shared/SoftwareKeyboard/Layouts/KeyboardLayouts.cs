// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.
//
// Layout data provenance: generated from the Unicode CLDR keyboard data, with
// xkeyboard-config as a secondary reference for national hardware conventions;
// long-press alternates curated after the AOSP LatinIME and FlorisBoard designs
// (Apache-2.0; design reference only — no code copied). See THIRD-PARTY-NOTICES.txt
// at the repository root.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

/// <summary>
/// The built-in software-keyboard layouts — the languages of Europe in three
/// scripts plus Georgian and Armenian. Each regional file defines its layouts as
/// pure data; this file is the explicit assembly of them all (no reflection, so
/// trimming and AOT stay safe).
/// </summary>
internal static partial class KeyboardLayouts
{
	private static KeyboardLayoutDefinition[]? _all;

	// Built lazily, NOT as a field initializer: static field initialization order
	// across the partial-class files is unspecified, so an eager array here could
	// capture layout fields before they are assigned.
	internal static IReadOnlyList<KeyboardLayoutDefinition> All => _all ??=
	[
		// Western Europe
		EnglishUS, EnglishUK, German, GermanSwiss, French, FrenchBelgian, FrenchSwiss, Dutch,
		// Southern Europe
		Spanish, Portuguese, Italian, Maltese, Albanian, Turkish, Greek,
		// Northern Europe
		Danish, Norwegian, Swedish, Finnish, Icelandic, Lithuanian, Latvian, Estonian,
		// Central Europe
		Polish, Czech, Slovak, Hungarian, Romanian, Croatian, SerbianLatin,
		// Cyrillic script
		Russian, Ukrainian, Belarusian, Bulgarian, SerbianCyrillic, Macedonian,
		// Caucasus (added-but-untested pending a bundled font with coverage)
		Georgian, Armenian,
	];
}
