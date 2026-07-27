#nullable enable

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CodeBrix.Platform.UI.Localization;

/// <summary>
/// The strings CodeBrix.Platform shows a user, in the language the application
/// is running in. Read them as properties — <c>PlatformStrings.Cancel</c> — at
/// the moment they are shown, never cached by a caller: the active language is
/// resolved from <see cref="CultureInfo.CurrentUICulture"/> on every read, so a
/// language set after start is picked up.
/// <para>
/// Resolution goes exact tag ("de-CH"), then base language ("de"), then English.
/// Falling back rather than throwing is deliberate: an application may be run
/// in a language CodeBrix.Platform has no set for, and English words on a
/// button beat an exception in a file picker.
/// </para>
/// </summary>
public static partial class PlatformStrings
{
	// Built on first use, NOT in a field initializer: the sets themselves live
	// in the PlatformStrings.* partial files, and the order in which one partial
	// class's field initializers run across files is not defined — reading them
	// during static initialization saw them null.
	static IReadOnlyDictionary<string, PlatformStringSet>? _sets;

	static IReadOnlyDictionary<string, PlatformStringSet> Sets => _sets ??= BuildSets();

	static PlatformStringSet English => Sets["en"];

	// The last resolution, kept so the common case — every string in one dialog
	// read under one culture — costs a reference compare rather than a lookup.
	static string? _lastTag;
	static PlatformStringSet? _lastSet;

	/// <inheritdoc cref="PlatformStringSet.Cancel"/>
	public static string Cancel => Active.Cancel;

	/// <inheritdoc cref="PlatformStringSet.Create"/>
	public static string Create => Active.Create;

	/// <inheritdoc cref="PlatformStringSet.FolderNamePlaceholder"/>
	public static string FolderNamePlaceholder => Active.FolderNamePlaceholder;

	/// <inheritdoc cref="PlatformStringSet.KeepEditing"/>
	public static string KeepEditing => Active.KeepEditing;

	/// <inheritdoc cref="PlatformStringSet.NameLabel"/>
	public static string NameLabel => Active.NameLabel;

	/// <inheritdoc cref="PlatformStringSet.NewFolder"/>
	public static string NewFolder => Active.NewFolder;

	/// <inheritdoc cref="PlatformStringSet.NoItems"/>
	public static string NoItems => Active.NoItems;

	/// <inheritdoc cref="PlatformStringSet.Open"/>
	public static string Open => Active.Open;

	/// <inheritdoc cref="PlatformStringSet.OpenFileTitle"/>
	public static string OpenFileTitle => Active.OpenFileTitle;

	/// <inheritdoc cref="PlatformStringSet.Replace"/>
	public static string Replace => Active.Replace;

	/// <inheritdoc cref="PlatformStringSet.Save"/>
	public static string Save => Active.Save;

	/// <inheritdoc cref="PlatformStringSet.SaveFileTitle"/>
	public static string SaveFileTitle => Active.SaveFileTitle;

	/// <inheritdoc cref="PlatformStringSet.SelectFolderTitle"/>
	public static string SelectFolderTitle => Active.SelectFolderTitle;

	/// <inheritdoc cref="PlatformStringSet.Ok"/>
	public static string Ok => Active.Ok;

	/// <inheritdoc cref="PlatformStringSet.Yes"/>
	public static string Yes => Active.Yes;

	/// <inheritdoc cref="PlatformStringSet.No"/>
	public static string No => Active.No;

	/// <inheritdoc cref="PlatformStringSet.InformationTitle"/>
	public static string InformationTitle => Active.InformationTitle;

	/// <inheritdoc cref="PlatformStringSet.ErrorTitle"/>
	public static string ErrorTitle => Active.ErrorTitle;

	/// <inheritdoc cref="PlatformStringSet.ErrorOccurredLabel"/>
	public static string ErrorOccurredLabel => Active.ErrorOccurredLabel;

	/// <inheritdoc cref="PlatformStringSet.DetailsLabel"/>
	public static string DetailsLabel => Active.DetailsLabel;

	/// <inheritdoc cref="PlatformStringSet.ConfirmTitle"/>
	public static string ConfirmTitle => Active.ConfirmTitle;

	/// <inheritdoc cref="PlatformStringSet.KeyAbc"/>
	public static string KeyAbc => Active.KeyAbc;

	/// <inheritdoc cref="PlatformStringSet.KeyTab"/>
	public static string KeyTab => Active.KeyTab;

	/// <inheritdoc cref="PlatformStringSet.KeyEnter"/>
	public static string KeyEnter => Active.KeyEnter;

	/// <inheritdoc cref="PlatformStringSet.KeyShift"/>
	public static string KeyShift => Active.KeyShift;

	/// <inheritdoc cref="PlatformStringSet.KeyShiftUpper"/>
	public static string KeyShiftUpper => Active.KeyShiftUpper;

	/// <inheritdoc cref="PlatformStringSet.KeyBackspace"/>
	public static string KeyBackspace => Active.KeyBackspace;

	/// <summary>
	/// Asks whether to overwrite <paramref name="fileName"/>. The quoting is
	/// part of each language's format, so languages that quote differently do.
	/// </summary>
	public static string ReplaceFile(string fileName) =>
		string.Format(CultureInfo.CurrentCulture, Active.ReplaceFileFormat, fileName);

	/// <summary>The language tags a set exists for, in no particular order.</summary>
	public static IEnumerable<string> SupportedLanguages => Sets.Keys;

	/// <summary>
	/// The set for <paramref name="tag"/> — exact, then base language, then
	/// English. Never null.
	/// </summary>
	internal static PlatformStringSet Resolve(string? tag)
	{
		if (string.IsNullOrWhiteSpace(tag))
		{
			return English;
		}
		var wanted = tag.Trim().Replace('_', '-');
		if (Sets.TryGetValue(wanted, out var exact))
		{
			return exact;
		}
		var dash = wanted.IndexOf('-');
		return dash > 0 && Sets.TryGetValue(wanted[..dash], out var baseLanguage)
			? baseLanguage
			: English;
	}

	static PlatformStringSet Active
	{
		get
		{
			var tag = CultureInfo.CurrentUICulture.Name;
			if (_lastSet is null || !string.Equals(tag, _lastTag, System.StringComparison.Ordinal))
			{
				_lastSet = Resolve(tag);
				_lastTag = tag;
			}
			return _lastSet;
		}
	}

	static IReadOnlyDictionary<string, PlatformStringSet> BuildSets()
	{
		var all = WesternEurope
			.Concat(SouthernEurope)
			.Concat(NorthernEurope)
			.Concat(CentralEurope)
			.Concat(Cyrillic)
			.Concat(Caucasus);
		var map = new Dictionary<string, PlatformStringSet>(System.StringComparer.OrdinalIgnoreCase);
		foreach (var set in all)
		{
			map[set.Code] = set;
		}
		return map;
	}
}
