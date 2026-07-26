// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

/// <summary>
/// One software-keyboard layout as pure data: three character rows in up to three
/// levels (base, shift, AltGr) plus long-press alternates. The layout files under
/// SoftwareKeyboard/Layouts are generated from the Unicode CLDR keyboard data (with
/// xkeyboard-config as a secondary reference) — see THIRD-PARTY-NOTICES.txt at the
/// repository root for provenance. Adding a language is adding a data file, not a
/// redesign.
/// </summary>
internal sealed class KeyboardLayoutDefinition
{
	/// <summary>BCP-47 language tag identifying the layout ("de", "en-GB", "uk"…).</summary>
	public required string Id { get; init; }

	/// <summary>The layout's own name for itself, shown when switching ("Deutsch").</summary>
	public required string DisplayName { get; init; }

	/// <summary>
	/// The three character rows at the BASE level, one string per row, one key per
	/// char. Rows may have different lengths; keys are sized evenly per row.
	/// </summary>
	public required string[] Rows { get; init; }

	/// <summary>
	/// The SHIFT level of <see cref="Rows"/>, aligned per char. Null derives it by
	/// invariant upper-casing — only layouts where that is wrong (Turkish dotted and
	/// dotless i) or where shift types different punctuation need to state it.
	/// </summary>
	public string[]? ShiftRows { get; init; }

	/// <summary>
	/// The AltGr level of <see cref="Rows"/>, aligned per char, with a space meaning
	/// "no AltGr character on this key". Presented to the user as long-press
	/// alternates on the base key (there is no hardware AltGr on a touch panel).
	/// Null means the layout has no AltGr level.
	/// </summary>
	public string[]? AltGrRows { get; init; }

	/// <summary>
	/// Additional long-press alternates per base character (accents and variants),
	/// merged with any AltGr character for that key.
	/// </summary>
	public IReadOnlyDictionary<char, string>? LongPress { get; init; }

	/// <summary>
	/// True for layouts that ship wired-up but not yet verified on hardware because
	/// no bundled font covers their script yet (Georgian, Armenian). Behavior is
	/// identical; key legends may render as missing-glyph boxes until a font with
	/// coverage is bundled and the layout is then tested.
	/// </summary>
	public bool Untested { get; init; }
}
