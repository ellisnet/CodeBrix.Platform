// Shared source: compiled into BOTH the FrameBuffer and FrameBuffer.Emulated heads
// (the Emulated head links this file from its csproj). Keep head-neutral.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeBrix.Platform.UI.Runtime.Skia.SoftwareKeyboard;

/// <summary>
/// The registry of software-keyboard layouts and the rules for choosing the active
/// one. Resolution order: the host builder's pinned layout, then the emulated
/// device's system language when running under the CodeBrix.Develop frame-buffer
/// emulator, then the XKB_DEFAULT_LAYOUT environment variable (an operator's
/// explicit keyboard setting, consistent with how the hardware keymap resolves),
/// then the locale environment (LC_ALL / LC_CTYPE / LANG), then US English.
/// <para>
/// The emulator's language outranks the XKB and locale probes because those
/// describe the DEVELOPER'S machine, which is exactly what an emulated device is
/// meant to stop looking like — but it yields to the pinned layout, which is the
/// application author's own deliberate choice.
/// </para>
/// </summary>
internal static class KeyboardLayoutCatalog
{
	/// <summary>
	/// The emulator's launch-contract language variable, and the value of it
	/// that means "follow the host". Spelled out rather than referenced: this
	/// file is compiled into the real FrameBuffer head too, which has no
	/// emulator transport to take the constants from. Keep in step with
	/// FrameBufferEmulatorProtocol.LanguageVariable in the Emulated head.
	/// </summary>
	private const string EmulatorLanguageVariable = "CODEBRIX_FBEMU_LANGUAGE";
	private const string EmulatorSystemDefaultLanguage = "system-default";

	/// <summary>
	/// Locale languages that have no layout of their own but are conventionally
	/// typed on another one (regional and minority languages of Europe).
	/// </summary>
	private static readonly Dictionary<string, string> LanguageAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["sl"] = "hr",    // Slovenian shares the South-Slavic Latin layout
		["bs"] = "hr",    // Bosnian likewise
		["ca"] = "es",    // Catalan
		["gl"] = "es",    // Galician
		["eu"] = "es",    // Basque
		["ga"] = "en-GB", // Irish
		["cy"] = "en-GB", // Welsh
		["gd"] = "en-GB", // Scottish Gaelic
		["br"] = "fr",    // Breton
		["oc"] = "fr",    // Occitan
		["co"] = "fr",    // Corsican
		["lb"] = "de-CH", // Luxembourgish (Swiss-style layout)
		["rm"] = "de-CH", // Romansh
		["fo"] = "da",    // Faroese
		["se"] = "sv",    // Northern Sami (Swedish-style layout)
		["nn"] = "no",    // Norwegian Nynorsk
		["nb"] = "no",    // Norwegian Bokmål
	};

	/// <summary>
	/// XKB layout codes (country-flavored) to BCP-47 layout ids, for the
	/// XKB_DEFAULT_LAYOUT step. Codes equal to a layout id (de, fr, pl…) need no
	/// entry. A compound value like "us,ru" resolves by its first entry.
	/// </summary>
	private static readonly Dictionary<string, string> XkbAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		["us"] = "en",
		["gb"] = "en-GB",
		["ie"] = "en-GB",
		["ua"] = "uk",
		["by"] = "be",
		["rs"] = "sr",
		["cz"] = "cs",
		["dk"] = "da",
		["se"] = "sv",
		["gr"] = "el",
		["ge"] = "ka",
		["am"] = "hy",
		["ch"] = "de-CH",
		["be"] = "fr-BE",
		["ee"] = "et",
		["si"] = "hr",
		["ba"] = "hr",
		["al"] = "sq",
		["latam"] = "es",
		["pt"] = "pt",
		["no"] = "no",
	};

	internal static IReadOnlyList<KeyboardLayoutDefinition> All => KeyboardLayouts.All;

	internal static KeyboardLayoutDefinition Fallback
		=> KeyboardLayouts.All.First(layout => layout.Id == "en");

	/// <summary>
	/// Finds a layout by BCP-47 tag: exact id first, then alias, then the bare
	/// language part of a regional tag ("de-AT" → "de"), then null.
	/// </summary>
	internal static KeyboardLayoutDefinition? Find(string? tag)
	{
		if (string.IsNullOrWhiteSpace(tag))
		{
			return null;
		}
		var wanted = tag.Trim().Replace('_', '-');
		var match = KeyboardLayouts.All.FirstOrDefault(layout
			=> string.Equals(layout.Id, wanted, StringComparison.OrdinalIgnoreCase));
		if (match is not null)
		{
			return match;
		}
		if (LanguageAliases.TryGetValue(wanted, out var aliased))
		{
			return Find(aliased);
		}
		var dash = wanted.IndexOf('-');
		return dash > 0 ? Find(wanted[..dash]) : null;
	}

	internal static KeyboardLayoutDefinition ResolveActive(string? pinnedLayout)
		=> Find(pinnedLayout)
			?? FindFromEmulatorEnvironment()
			?? FindFromXkbEnvironment()
			?? FindFromLocaleEnvironment()
			?? Fallback;

	/// <summary>
	/// The globe-key cycle: the resolved enabled list, with the active layout
	/// always included. A single distinct layout means no globe key.
	/// </summary>
	internal static IReadOnlyList<KeyboardLayoutDefinition> ResolveEnabled(
		KeyboardLayoutDefinition active, IEnumerable<string>? enabledLayouts)
	{
		var result = new List<KeyboardLayoutDefinition> { active };
		foreach (var tag in enabledLayouts ?? [])
		{
			if (Find(tag) is { } layout && !result.Contains(layout))
			{
				result.Add(layout);
			}
		}
		return result;
	}

	// The emulated device's system language, which the IDE puts in the
	// environment before launching the application. Absent on the real head,
	// and "system-default" means the user asked to follow the host — both of
	// which fall through to the probes below.
	private static KeyboardLayoutDefinition? FindFromEmulatorEnvironment()
	{
		var language = Environment.GetEnvironmentVariable(EmulatorLanguageVariable);
		if (string.IsNullOrWhiteSpace(language)
			|| string.Equals(language.Trim(), EmulatorSystemDefaultLanguage, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return Find(language);
	}

	private static KeyboardLayoutDefinition? FindFromXkbEnvironment()
	{
		var xkb = Environment.GetEnvironmentVariable("XKB_DEFAULT_LAYOUT");
		if (string.IsNullOrWhiteSpace(xkb))
		{
			return null;
		}
		var first = xkb.Split(',')[0].Trim();
		return XkbAliases.TryGetValue(first, out var mapped) ? Find(mapped) : Find(first);
	}

	private static KeyboardLayoutDefinition? FindFromLocaleEnvironment()
	{
		foreach (var variable in (string[])["LC_ALL", "LC_CTYPE", "LANG"])
		{
			var value = Environment.GetEnvironmentVariable(variable);
			if (string.IsNullOrWhiteSpace(value) || value is "C" or "POSIX")
			{
				continue;
			}
			// de_DE.UTF-8 → de-DE; the Find fallback then tries "de".
			var tag = value.Split('.')[0].Split('@')[0].Trim();
			if (Find(tag) is { } layout)
			{
				return layout;
			}
		}
		return null;
	}
}
