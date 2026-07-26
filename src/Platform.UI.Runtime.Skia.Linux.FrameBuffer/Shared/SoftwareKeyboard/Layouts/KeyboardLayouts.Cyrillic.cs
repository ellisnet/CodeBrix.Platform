// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.
//
// Layout data provenance: see KeyboardLayouts.cs and THIRD-PARTY-NOTICES.txt.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

internal static partial class KeyboardLayouts
{
	internal static readonly KeyboardLayoutDefinition Russian = new()
	{
		Id = "ru",
		DisplayName = "Русский",
		Rows = ["йцукенгшщзх", "фывапролджэ", "ячсмитьбю"],
		LongPress = new Dictionary<char, string>
		{
			['е'] = "ё",
			['ь'] = "ъ",
		},
	};

	internal static readonly KeyboardLayoutDefinition Ukrainian = new()
	{
		Id = "uk",
		DisplayName = "Українська",
		Rows = ["йцукенгшщзхї", "фівапролджє", "ячсмитьбю"],
		LongPress = new Dictionary<char, string>
		{
			['г'] = "ґ",
			['и'] = "і",
		},
	};

	internal static readonly KeyboardLayoutDefinition Belarusian = new()
	{
		Id = "be",
		DisplayName = "Беларуская",
		Rows = ["йцукенгшўзх", "фывапролджэ", "ячсмітьбю"],
		LongPress = new Dictionary<char, string>
		{
			['е'] = "ё",
		},
	};

	internal static readonly KeyboardLayoutDefinition Bulgarian = new()
	{
		Id = "bg",
		DisplayName = "Български",
		// The BDS national standard arrangement.
		Rows = ["уеишщксдзц", "ьяаожгтнвмч", "юйъфхпрлб"],
	};

	internal static readonly KeyboardLayoutDefinition SerbianCyrillic = new()
	{
		Id = "sr",
		DisplayName = "Српски (ћирилица)",
		Rows = ["љњертзуиопш", "асдфгхјклчћ", "ѕџцвбнмђж"],
	};

	internal static readonly KeyboardLayoutDefinition Macedonian = new()
	{
		Id = "mk",
		DisplayName = "Македонски",
		Rows = ["љњертѕуиопш", "асдфгхјклчќ", "џцвбнмѓж"],
		LongPress = new Dictionary<char, string>
		{
			['е'] = "ѐ",
			['и'] = "ѝ",
		},
	};
}
