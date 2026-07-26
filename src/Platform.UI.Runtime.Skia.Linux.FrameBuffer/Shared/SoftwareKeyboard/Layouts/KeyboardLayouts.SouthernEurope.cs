// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.
//
// Layout data provenance: see KeyboardLayouts.cs and THIRD-PARTY-NOTICES.txt.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

internal static partial class KeyboardLayouts
{
	internal static readonly KeyboardLayoutDefinition Spanish = new()
	{
		Id = "es",
		DisplayName = "Español",
		Rows = ["qwertyuiop", "asdfghjklñ", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "áàâä",
			['e'] = "éèêë",
			['i'] = "íìïî",
			['o'] = "óòöô",
			['u'] = "úùüû",
			['c'] = "ç",
		},
	};

	internal static readonly KeyboardLayoutDefinition Portuguese = new()
	{
		Id = "pt",
		DisplayName = "Português",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "áàâãä",
			['e'] = "éêèë",
			['i'] = "íì",
			['o'] = "óôõò",
			['u'] = "úüù",
			['c'] = "ç",
		},
	};

	internal static readonly KeyboardLayoutDefinition Italian = new()
	{
		Id = "it",
		DisplayName = "Italiano",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "àáâ",
			['e'] = "èéê",
			['i'] = "ìíî",
			['o'] = "òóô",
			['u'] = "ùúû",
		},
	};

	internal static readonly KeyboardLayoutDefinition Maltese = new()
	{
		Id = "mt",
		DisplayName = "Malti",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['g'] = "ġ",
			['h'] = "ħ",
			['z'] = "ż",
			['c'] = "ċ",
			['a'] = "à",
			['e'] = "èé",
			['i'] = "ìí",
			['o'] = "ò",
			['u'] = "ù",
		},
	};

	internal static readonly KeyboardLayoutDefinition Albanian = new()
	{
		Id = "sq",
		DisplayName = "Shqip",
		Rows = ["qwertzuiopç", "asdfghjklë", "yxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "éèê",
			['a'] = "âà",
		},
	};

	internal static readonly KeyboardLayoutDefinition Turkish = new()
	{
		Id = "tr",
		DisplayName = "Türkçe",
		Rows = ["qwertyuıopğü", "asdfghjklşi", "zxcvbnmöç"],
		// Stated explicitly because invariant upper-casing is wrong for Turkish:
		// dotless ı shifts to I, and dotted i shifts to İ.
		ShiftRows = ["QWERTYUIOPĞÜ", "ASDFGHJKLŞİ", "ZXCVBNMÖÇ"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "âá",
			['u'] = "ûú",
			['i'] = "îí",
			['o'] = "ô",
		},
	};

	internal static readonly KeyboardLayoutDefinition Greek = new()
	{
		Id = "el",
		DisplayName = "Ελληνικά",
		Rows = ["ςερτυθιοπ", "ασδφγηξκλ", "ζχψωβνμ"],
		LongPress = new Dictionary<char, string>
		{
			['α'] = "ά",
			['ε'] = "έ",
			['η'] = "ή",
			['ι'] = "ίϊΐ",
			['ο'] = "ό",
			['υ'] = "ύϋΰ",
			['ω'] = "ώ",
		},
	};
}
