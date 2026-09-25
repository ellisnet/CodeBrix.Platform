using System;
using System.Collections.Generic;
using System.Text;

#if false
using Java.Util;
#elif false
using Foundation;
#elif __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
using CodeBrix.Platform.Contracts;
#endif

namespace Windows.System.UserProfile;

public static partial class GlobalizationPreferences
{

#if __CROSSRUNTIME__ && !__NETSTD_REFERENCE__
	private static IGlobalizationPreferencesPlatform _preferencesPlatform;

	public static IReadOnlyList<string> Languages =>
#if false
		new[] { Locale.Default.ToLanguageTag() };
#elif false
		NSLocale.PreferredLanguages;
#else
		PreferencesPlatform.Languages;
#endif

	/// <summary>
	/// Gets the platform that reports the user's preferred languages, resolved once on first use.
	/// </summary>
	private static IGlobalizationPreferencesPlatform PreferencesPlatform =>
		_preferencesPlatform ??= PlatformContract.Resolve<IGlobalizationPreferencesPlatform>();
#endif
}
