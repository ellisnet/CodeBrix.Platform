// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.
//
// Layout data provenance: see KeyboardLayouts.cs and THIRD-PARTY-NOTICES.txt.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

internal static partial class KeyboardLayouts
{
	internal static readonly KeyboardLayoutDefinition Polish = new()
	{
		Id = "pl",
		DisplayName = "Polski",
		// The Polish "programmers" convention: plain QWERTY with the ogonek and
		// kreska letters on the AltGr level (presented as long-press alternates).
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		AltGrRows = ["  ę     ó ", "ąś      ł", "żźć  ń "],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "ę",
			['o'] = "ó",
			['a'] = "ą",
			['s'] = "ś",
			['l'] = "ł",
			['z'] = "żź",
			['c'] = "ć",
			['n'] = "ń",
		},
	};

	internal static readonly KeyboardLayoutDefinition Czech = new()
	{
		Id = "cs",
		DisplayName = "Čeština",
		Rows = ["qwertzuiop", "asdfghjkl", "yxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "á",
			['c'] = "č",
			['d'] = "ď",
			['e'] = "éě",
			['i'] = "í",
			['n'] = "ň",
			['o'] = "ó",
			['r'] = "ř",
			['s'] = "š",
			['t'] = "ť",
			['u'] = "úů",
			['y'] = "ý",
			['z'] = "ž",
		},
	};

	internal static readonly KeyboardLayoutDefinition Slovak = new()
	{
		Id = "sk",
		DisplayName = "Slovenčina",
		Rows = ["qwertzuiop", "asdfghjkl", "yxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "áä",
			['c'] = "č",
			['d'] = "ď",
			['e'] = "é",
			['i'] = "í",
			['l'] = "ľĺ",
			['n'] = "ň",
			['o'] = "óô",
			['r'] = "ŕ",
			['s'] = "š",
			['t'] = "ť",
			['u'] = "ú",
			['y'] = "ý",
			['z'] = "ž",
		},
	};

	internal static readonly KeyboardLayoutDefinition Hungarian = new()
	{
		Id = "hu",
		DisplayName = "Magyar",
		Rows = ["qwertzuiopő", "asdfghjkléá", "yxcvbnmű"],
		LongPress = new Dictionary<char, string>
		{
			['o'] = "óöő",
			['u'] = "úüű",
			['i'] = "í",
			['e'] = "é",
			['a'] = "á",
		},
	};

	internal static readonly KeyboardLayoutDefinition Romanian = new()
	{
		Id = "ro",
		DisplayName = "Română",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "ăâ",
			['i'] = "î",
			['s'] = "ș",
			['t'] = "ț",
		},
	};

	internal static readonly KeyboardLayoutDefinition Croatian = new()
	{
		Id = "hr",
		DisplayName = "Hrvatski",
		// Shared by the South-Slavic Latin orthographies (also aliased for
		// Slovenian and Bosnian).
		Rows = ["qwertzuiop", "asdfghjklčć", "yxcvbnmšđž"],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "é",
			['c'] = "čć",
			['s'] = "š",
			['d'] = "đ",
			['z'] = "ž",
		},
	};

	internal static readonly KeyboardLayoutDefinition SerbianLatin = new()
	{
		Id = "sr-Latn",
		DisplayName = "Srpski (latinica)",
		Rows = ["qwertzuiop", "asdfghjklčć", "yxcvbnmšđž"],
		LongPress = Croatian.LongPress,
	};
}
