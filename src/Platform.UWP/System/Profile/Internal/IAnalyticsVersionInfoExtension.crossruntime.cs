#if !__NETSTD_REFERENCE__
#nullable enable

namespace Windows.System.Profile.Internal;

/// <summary>
/// The platform seam behind <see cref="AnalyticsVersionInfo.DeviceFamilyVersion"/> on the Skia and Core runtimes: a
/// platform that knows its operating-system release (CodeBrix.Android, CodeBrix.Mobile) registers an implementation with
/// ApiExtensibility, and the value is read from it every time the property is read.
/// </summary>
/// <remarks>
/// Additive beside <see cref="IAnalyticsInfoExtension"/> (the device form); a platform implements either or both.
/// With nothing registered, the property reports the host's <see cref="global::System.Environment.OSVersion"/>.
/// </remarks>
internal interface IAnalyticsVersionInfoExtension
{
	/// <summary>
	/// Gets the operating-system version, encoded the way WinUI encodes DeviceFamilyVersion (major in bits 63..48, minor
	/// in 47..32, build in 31..16, revision in 15..0, written as an unsigned decimal string;
	/// <see cref="VersionHelpers.ToDeviceFamilyVersion(global::System.Version)"/> produces it from a <see cref="global::System.Version"/>).
	/// </summary>
	/// <returns>The encoded version, or null when the platform cannot say (the property then reports "0").</returns>
	string? GetDeviceFamilyVersion();
}
#endif
