#nullable enable

namespace CodeBrix.Platform.UI.Localization;

/// <summary>
/// Every string CodeBrix.Platform itself shows a user, in one language. Only
/// text the PLATFORM puts on screen belongs here — an application's own text is
/// the application developer's business, and developer-facing text (exception
/// messages, log lines, theme-resource keys) stays English.
/// <para>
/// The sets live in the PlatformStrings.* data files, grouped the same way the
/// software keyboard's layouts are, and are selected by
/// <see cref="PlatformStrings"/> from the active culture. Adding a language is
/// adding a set, not a redesign.
/// </para>
/// <para>
/// These are held as C# data rather than .resw because the resource loader
/// matches cultures on their BASE language only (everything before the first
/// dash), which cannot tell "sr-Latn" from "sr" — two different scripts of the
/// same language, and two different sets of words. Keying on the full tag here
/// keeps every variant exact.
/// </para>
/// </summary>
internal sealed class PlatformStringSet
{
	/// <summary>
	/// The language tag this set is for, matching the software keyboard's
	/// layout ids ("de", "de-CH", "sr-Latn"…).
	/// </summary>
	public required string Code { get; init; }

	// ---- Pickers -------------------------------------------------------

	/// <summary>Dismisses a picker or an inline row without acting.</summary>
	public required string Cancel { get; init; }

	/// <summary>Commits the new-folder row.</summary>
	public required string Create { get; init; }

	/// <summary>Placeholder in the new-folder name box.</summary>
	public required string FolderNamePlaceholder { get; init; }

	/// <summary>Declines an overwrite and returns to the save name box.</summary>
	public required string KeepEditing { get; init; }

	/// <summary>Labels the save-as name box; carries its own colon.</summary>
	public required string NameLabel { get; init; }

	/// <summary>Opens the new-folder row.</summary>
	public required string NewFolder { get; init; }

	/// <summary>Shown in place of the list when a folder has nothing to show.</summary>
	public required string NoItems { get; init; }

	/// <summary>Default commit button of an open-file picker.</summary>
	public required string Open { get; init; }

	/// <summary>Title of an open-file picker.</summary>
	public required string OpenFileTitle { get; init; }

	/// <summary>Confirms overwriting an existing file.</summary>
	public required string Replace { get; init; }

	/// <summary>
	/// Asks whether to overwrite; {0} is the file name, already quoted by the
	/// format itself so each language can quote the way it quotes.
	/// </summary>
	public required string ReplaceFileFormat { get; init; }

	/// <summary>Default commit button of a save-file picker.</summary>
	public required string Save { get; init; }

	/// <summary>Title of a save-file picker.</summary>
	public required string SaveFileTitle { get; init; }

	/// <summary>Title and default commit button of a folder picker.</summary>
	public required string SelectFolderTitle { get; init; }

	// ---- Dialogs -------------------------------------------------------

	/// <summary>Accepts a dialog. Left as "OK" wherever that is what is used.</summary>
	public required string Ok { get; init; }

	/// <summary>Affirmative answer to a yes/no dialog.</summary>
	public required string Yes { get; init; }

	/// <summary>Negative answer to a yes/no dialog.</summary>
	public required string No { get; init; }

	/// <summary>Title of an informational dialog.</summary>
	public required string InformationTitle { get; init; }

	/// <summary>
	/// Title of an error dialog. Rendered in each language's normal case — the
	/// English source shouts ("ERROR"), which most languages do not do.
	/// </summary>
	public required string ErrorTitle { get; init; }

	/// <summary>
	/// Opens the body of an error dialog, with the error itself on the line
	/// below. Carries its own colon.
	/// </summary>
	public required string ErrorOccurredLabel { get; init; }

	/// <summary>
	/// Introduces the detail text under an error message. Carries its own colon.
	/// </summary>
	public required string DetailsLabel { get; init; }

	/// <summary>Default title of a confirmation dialog; a question.</summary>
	public required string ConfirmTitle { get; init; }

	// ---- Software keyboard ---------------------------------------------

	/// <summary>
	/// Returns from the symbols page to the letters page. Spelled with the
	/// first letters of the layout's OWN alphabet — "ABC", "АБВ", "ΑΒΓ" — the
	/// way a phone keyboard labels it.
	/// </summary>
	public required string KeyAbc { get; init; }

	/// <summary>
	/// The tab key's legend. Kept short: it is painted on one key. Left as the
	/// Latin "Tab" in every language whose keyboards are labelled that way —
	/// see the modifier-key note on <see cref="PlatformStrings"/> before
	/// translating it.
	/// </summary>
	public required string KeyTab { get; init; }

	/// <summary>The enter key's legend.</summary>
	public required string KeyEnter { get; init; }

	/// <summary>
	/// The shift key's legend, shift off. Deliberately still "Shift" in the
	/// many languages that have no everyday word for this key — see the
	/// modifier-key note on <see cref="PlatformStrings"/>. A set whose Shift is
	/// Latin while its Enter and Backspace are not is correct, not unfinished.
	/// </summary>
	public required string KeyShift { get; init; }

	/// <summary>The shift key's legend, shift latched — the loud form of <see cref="KeyShift"/>.</summary>
	public required string KeyShiftUpper { get; init; }

	/// <summary>The backspace key's legend. Abbreviated on every layout.</summary>
	public required string KeyBackspace { get; init; }

	/// <summary>
	/// The file picker's "go to the parent folder" button, beside an upwards
	/// arrow. Kept to one short word: it shares a toolbar with the new-folder
	/// button on a narrow device screen.
	/// </summary>
	public required string NavigateUp { get; init; }
}
