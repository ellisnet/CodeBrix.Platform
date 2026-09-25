using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CodeBrix.Platform.Contracts;
using Windows.WinRT;

namespace CodeBrix.Platform.Skia;

/// <summary>
/// The Skia implementation of <see cref="IGlobalizationPreferencesPlatform"/>: on Windows it reads the user's
/// language list from the Windows language profile; on the other desktop systems it reports no languages, so that
/// the application's manifest languages apply.
/// </summary>
internal sealed class GlobalizationPreferencesSkiaPlatform : IGlobalizationPreferencesPlatform
{
	/// <inheritdoc />
	public IReadOnlyList<string> Languages =>
		OperatingSystem.IsWindows() ? GetWinUserLanguageList() : Array.Empty<string>();

	private static string[] GetWinUserLanguageList()
	{
		if (NativeMethods.EnsureLanguageProfileExists() >= 0)
		{
			const char Delimiter = ';';
			if (NativeMethods.GetUserLanguages(Delimiter, out var handle) >= 0)
			{
				var languages = MarshalString.FromAbi(handle).Split(Delimiter);
				MarshalString.DisposeAbi(handle);

				return languages;
			}
		}

		return Array.Empty<string>();
	}

	private static class NativeMethods
	{
		[DllImport("winlangdb.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int EnsureLanguageProfileExists();

		[DllImport("bcp47langs.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetUserLanguages(char Delimiter, out IntPtr UserLanguages);
	}
}
