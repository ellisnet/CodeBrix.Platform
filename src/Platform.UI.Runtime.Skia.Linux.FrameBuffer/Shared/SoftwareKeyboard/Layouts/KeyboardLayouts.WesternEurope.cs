// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.
//
// Layout data provenance: see KeyboardLayouts.cs and THIRD-PARTY-NOTICES.txt.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

internal static partial class KeyboardLayouts
{
	internal static readonly KeyboardLayoutDefinition EnglishUS = new()
	{
		Id = "en",
		DisplayName = "English (US)",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "àáâäãåæā",
			['e'] = "èéêëēėę",
			['i'] = "îïíīįì",
			['o'] = "ôöòóœøōõ",
			['u'] = "ûüùúū",
			['n'] = "ñń",
			['c'] = "çćč",
			['s'] = "śš",
			['y'] = "ÿý",
			['z'] = "žźż",
			['l'] = "ł",
			['d'] = "ð",
			['t'] = "þ",
			['g'] = "ğ",
		},
	};

	internal static readonly KeyboardLayoutDefinition EnglishUK = new()
	{
		Id = "en-GB",
		DisplayName = "English (UK)",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = EnglishUS.LongPress,
	};

	internal static readonly KeyboardLayoutDefinition German = new()
	{
		Id = "de",
		DisplayName = "Deutsch",
		Rows = ["qwertzuiopü", "asdfghjklöä", "yxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['s'] = "ßśš",
			['a'] = "àáâãåæ",
			['e'] = "èéêë",
			['i'] = "îïíì",
			['o'] = "òóôõœø",
			['u'] = "ùúû",
			['n'] = "ñ",
			['c'] = "çć",
		},
	};

	internal static readonly KeyboardLayoutDefinition GermanSwiss = new()
	{
		Id = "de-CH",
		DisplayName = "Deutsch (Schweiz)",
		Rows = ["qwertzuiopü", "asdfghjklöä", "yxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['s'] = "ßśš",
			['a'] = "àáâãåæ",
			['e'] = "éèêë",
			['i'] = "îïíì",
			['o'] = "òóôõœø",
			['u'] = "ùúû",
			['n'] = "ñ",
			['c'] = "çć",
		},
	};

	internal static readonly KeyboardLayoutDefinition French = new()
	{
		Id = "fr",
		DisplayName = "Français",
		Rows = ["azertyuiop", "qsdfghjklm", "wxcvbn"],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "éèêë",
			['a'] = "àâæá",
			['u'] = "ùûü",
			['i'] = "îïí",
			['o'] = "ôœöó",
			['c'] = "ç",
			['y'] = "ÿ",
		},
	};

	internal static readonly KeyboardLayoutDefinition FrenchBelgian = new()
	{
		Id = "fr-BE",
		DisplayName = "Français (Belgique)",
		Rows = ["azertyuiop", "qsdfghjklm", "wxcvbn"],
		LongPress = French.LongPress,
	};

	internal static readonly KeyboardLayoutDefinition FrenchSwiss = new()
	{
		Id = "fr-CH",
		DisplayName = "Français (Suisse)",
		Rows = ["qwertzuiopè", "asdfghjkléà", "yxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "êëè",
			['a'] = "âæ",
			['u'] = "ùûü",
			['i'] = "îï",
			['o'] = "ôœö",
			['c'] = "ç",
		},
	};

	internal static readonly KeyboardLayoutDefinition Dutch = new()
	{
		Id = "nl",
		DisplayName = "Nederlands",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "éèêë",
			['a'] = "áàâä",
			['i'] = "íìîïĳ",
			['o'] = "óòôö",
			['u'] = "úùûü",
		},
	};
}
