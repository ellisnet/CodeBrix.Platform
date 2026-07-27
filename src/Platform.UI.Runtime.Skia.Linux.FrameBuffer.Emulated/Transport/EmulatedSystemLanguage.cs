#nullable enable

using System;
using System.Globalization;
using CodeBrix.Platform.Foundation.Logging;
using Windows.Globalization;

namespace CodeBrix.Platform.UI.Runtime.Skia.Linux.FrameBuffer.Emulated.Transport;

/// <summary>
/// Makes the application believe the emulated device is set to the language
/// CodeBrix.Develop chose for it, by reading
/// <see cref="FrameBufferEmulatorProtocol.LanguageVariable"/> from the launch
/// contract and making it the application's culture.
/// <para>
/// Applied ONCE, before anything is built, so every culture-sensitive thing the
/// application does — resource lookups, date and number formatting, the strings
/// CodeBrix.Platform itself shows — starts out in that language rather than
/// switching under the user partway through. The environment is fixed at
/// process launch, so this cannot change while the application runs: a language
/// changed in the IDE takes effect the next time the emulator opens.
/// </para>
/// </summary>
internal static class EmulatedSystemLanguage
{
	// The resolved culture, kept so the UI thread can be given it once the
	// event loop is running (see ApplyToCurrentThread).
	private static CultureInfo? _culture;

	/// <summary>
	/// The language the emulated device is set to, or null to follow the host —
	/// which is the case when the variable is absent (an IDE from before the
	/// setting existed), blank, or the literal "system-default".
	/// </summary>
	internal static string? Read()
	{
		var value = Environment.GetEnvironmentVariable(FrameBufferEmulatorProtocol.LanguageVariable);
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		var trimmed = value.Trim();
		return string.Equals(trimmed, FrameBufferEmulatorProtocol.SystemDefaultLanguage,
			StringComparison.OrdinalIgnoreCase)
			? null
			: trimmed;
	}

	/// <summary>
	/// Makes <see cref="Read"/> the application's culture, and returns it so the
	/// caller can log what the device is running as. Does nothing and returns
	/// null when the host's own language is to be followed.
	/// <para>
	/// The culture is assigned DIRECTLY rather than through
	/// <see cref="ApplicationLanguages.PrimaryLanguageOverride"/>, for two
	/// reasons. Its setter persists the value into the application's own
	/// LocalSettings — the emulated device's language is not the application's
	/// saved preference and must not survive into a real run — and reading those
	/// settings needs the package identity, which does not exist this early in
	/// startup. Assigning the culture reaches everything the override would:
	/// ApplicationLanguages builds its language list from the current culture,
	/// ResourceLoader resolves against that list, and the strings
	/// CodeBrix.Platform shows follow CurrentUICulture directly.
	/// </para>
	/// <para>
	/// A tag the runtime cannot make a culture of is a bad value in a launch
	/// contract, not a reason to refuse to start: the region or script subtag is
	/// dropped and, failing that, the host language is kept.
	/// </para>
	/// </summary>
	internal static string? Apply()
	{
		var language = Read();
		if (language is null)
		{
			return null;
		}
		if (CreateCulture(language) is not { } culture)
		{
			typeof(EmulatedSystemLanguage).Log().Warn(
				$"The emulator asked for language '{language}', which this runtime has no culture " +
				$"for; keeping the host language.");
			return null;
		}

		_culture = culture;
		ApplyToCurrentThread();
		return language;
	}

	/// <summary>
	/// (Re-)asserts the language chosen by <see cref="Apply"/> — on the CALLING
	/// thread and as the default for threads that have not resolved a culture of
	/// their own. Does nothing when the host language is being followed.
	/// <para>
	/// Called more than once, deliberately. The UI thread needs it explicitly
	/// because it is the event loop's own thread, already running by the time the
	/// launch contract is read, and a thread that has resolved its culture keeps
	/// it — the DefaultThread* values only reach threads that have not. It is
	/// then needed AGAIN after the application is constructed, because
	/// <see cref="ApplicationLanguages.ApplyCulture"/> — which Application's
	/// constructor calls — resets the culture to the first of its language list,
	/// and that list is led by the host machine's installed UI culture.
	/// </para>
	/// <para>
	/// Get this wrong and the symptom is oddly specific: the keyboard draws the
	/// right layout with the wrong words. The layout comes from the environment,
	/// the words from the culture of whichever thread renders them.
	/// </para>
	/// </summary>
	internal static void ApplyToCurrentThread()
	{
		if (_culture is not { } culture)
		{
			return;
		}
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
	}

	// "sr-Latn" or "fr-CH" whole if the runtime knows it, else the bare language
	// ("sr", "fr"), else nothing.
	private static CultureInfo? CreateCulture(string tag)
	{
		try
		{
			return new CultureInfo(tag);
		}
		catch (CultureNotFoundException)
		{
			var dash = tag.IndexOf('-');
			if (dash <= 0)
			{
				return null;
			}
			try
			{
				return new CultureInfo(tag[..dash]);
			}
			catch (CultureNotFoundException)
			{
				return null;
			}
		}
	}
}
