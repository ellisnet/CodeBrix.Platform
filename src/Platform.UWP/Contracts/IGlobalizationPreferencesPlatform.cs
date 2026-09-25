using System.Collections.Generic;

namespace CodeBrix.Platform.Contracts;

/// <summary>
/// The user's preferred languages as the operating system reports them, behind
/// <see cref="Windows.System.UserProfile.GlobalizationPreferences.Languages"/> (and, through it, the language
/// list that <see cref="Windows.Globalization.ApplicationLanguages"/> builds).
/// </summary>
/// <remarks>
/// Implementers: Platform (Skia), Android, Mobile.
/// <para>
/// Resolved once by <c>GlobalizationPreferences</c> (lazily, on first use) through
/// <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>. The Skia implementation is
/// <c>CodeBrix.Platform.Skia.GlobalizationPreferencesSkiaPlatform</c>, registered by the assembly's
/// <c>SkiaPlatformBootstrap</c>.
/// </para>
/// </remarks>
internal interface IGlobalizationPreferencesPlatform
{
	/// <summary>
	/// Gets the user's preferred languages as BCP-47 tags, most preferred first; empty when the platform does
	/// not report any. Read on every access of <c>GlobalizationPreferences.Languages</c>.
	/// </summary>
	IReadOnlyList<string> Languages { get; }
}
