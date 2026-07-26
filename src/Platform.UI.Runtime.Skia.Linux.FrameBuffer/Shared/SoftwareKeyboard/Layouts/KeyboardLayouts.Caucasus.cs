// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.
//
// Layout data provenance: see KeyboardLayouts.cs and THIRD-PARTY-NOTICES.txt.
//
// BOTH layouts here ship ADDED-BUT-UNTESTED: no font currently bundled with
// CodeBrix.Platform covers Georgian or Armenian, so key legends (and echoed text)
// may render as missing-glyph boxes until such a font is found, bundled, and these
// layouts are then verified. Character injection itself is unaffected — every
// character is in the Basic Multilingual Plane.

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

internal static partial class KeyboardLayouts
{
	internal static readonly KeyboardLayoutDefinition Georgian = new()
	{
		Id = "ka",
		DisplayName = "ქართული",
		// Georgian has no letter case; the shift level carries the letters the
		// national layout reaches through shift (ჭ ღ თ შ ჟ ძ ჩ).
		Rows = ["ქწერტყუიოპ", "ასდფგჰჯკლ", "ზხცვბნმ"],
		ShiftRows = ["ქჭეღთყუიოპ", "აშდფგჰჟკლ", "ძხჩვბნმ"],
		Untested = true,
	};

	internal static readonly KeyboardLayoutDefinition Armenian = new()
	{
		Id = "hy",
		DisplayName = "Հայերեն",
		// The phonetic arrangement; Armenian is bicameral so the shift level is
		// derived by upper-casing.
		Rows = ["քոեռտըւիօպ", "ասդֆգհյկլ", "զխծվբնմ"],
		Untested = true,
	};
}
