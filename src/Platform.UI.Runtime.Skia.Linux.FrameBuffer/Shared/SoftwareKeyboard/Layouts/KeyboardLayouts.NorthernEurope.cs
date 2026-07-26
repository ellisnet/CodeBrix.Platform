// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.
//
// Layout data provenance: see KeyboardLayouts.cs and THIRD-PARTY-NOTICES.txt.

using System.Collections.Generic;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

internal static partial class KeyboardLayouts
{
	internal static readonly KeyboardLayoutDefinition Danish = new()
	{
		Id = "da",
		DisplayName = "Dansk",
		Rows = ["qwertyuiopå", "asdfghjklæø", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "éè",
			['a'] = "áà",
			['o'] = "óò",
			['u'] = "úü",
		},
	};

	internal static readonly KeyboardLayoutDefinition Norwegian = new()
	{
		Id = "no",
		DisplayName = "Norsk",
		Rows = ["qwertyuiopå", "asdfghjkløæ", "zxcvbnm"],
		LongPress = Danish.LongPress,
	};

	internal static readonly KeyboardLayoutDefinition Swedish = new()
	{
		Id = "sv",
		DisplayName = "Svenska",
		Rows = ["qwertyuiopå", "asdfghjklöä", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['e'] = "éè",
			['a'] = "á",
			['o'] = "óò",
			['u'] = "üú",
		},
	};

	internal static readonly KeyboardLayoutDefinition Finnish = new()
	{
		Id = "fi",
		DisplayName = "Suomi",
		Rows = ["qwertyuiopå", "asdfghjklöä", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['s'] = "š",
			['z'] = "ž",
			['e'] = "é",
		},
	};

	internal static readonly KeyboardLayoutDefinition Icelandic = new()
	{
		Id = "is",
		DisplayName = "Íslenska",
		Rows = ["qwertyuiopð", "asdfghjklæ", "zxcvbnmþ"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "á",
			['e'] = "é",
			['i'] = "í",
			['o'] = "óö",
			['u'] = "úü",
			['y'] = "ý",
		},
	};

	internal static readonly KeyboardLayoutDefinition Lithuanian = new()
	{
		Id = "lt",
		DisplayName = "Lietuvių",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "ą",
			['c'] = "č",
			['e'] = "ęė",
			['i'] = "į",
			['s'] = "š",
			['u'] = "ųū",
			['z'] = "ž",
		},
	};

	internal static readonly KeyboardLayoutDefinition Latvian = new()
	{
		Id = "lv",
		DisplayName = "Latviešu",
		Rows = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['a'] = "ā",
			['c'] = "č",
			['e'] = "ē",
			['g'] = "ģ",
			['i'] = "ī",
			['k'] = "ķ",
			['l'] = "ļ",
			['n'] = "ņ",
			['s'] = "š",
			['u'] = "ū",
			['z'] = "ž",
		},
	};

	internal static readonly KeyboardLayoutDefinition Estonian = new()
	{
		Id = "et",
		DisplayName = "Eesti",
		Rows = ["qwertyuiopü", "asdfghjklöä", "zxcvbnm"],
		LongPress = new Dictionary<char, string>
		{
			['o'] = "õ",
			['s'] = "š",
			['z'] = "ž",
		},
	};
}
