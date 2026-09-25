namespace CodeBrix.Platform.Contracts;

/// <summary>
/// The operating-system part of <see cref="Windows.System.Profile.AnalyticsVersionInfo.DeviceFamily"/>
/// ("&lt;family&gt;.&lt;form&gt;", e.g. "Android.Tablet"); the form part is AnalyticsInfo.DeviceForm.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered - the family stays
/// <see cref="System.Environment.OSVersion"/>.Platform ("Unix" on Linux and macOS, "Win32NT" on Windows), as before.
/// <para>
/// OPTIONAL contract: read once, when AnalyticsInfo creates its AnalyticsVersionInfo (lazily, on the first read of
/// AnalyticsInfo.VersionInfo), through <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/>. The
/// platform bootstrap registers it before that first read. Suggested values: "Android" (CodeBrix.Android), "Apple"
/// or the Apple OS name (CodeBrix.Mobile).
/// </para>
/// </remarks>
internal interface IDeviceFamilyPlatform
{
	/// <summary>
	/// Gets the operating-system family name that comes before the dot of DeviceFamily (no dot in it).
	/// </summary>
	string OperatingSystemFamily { get; }
}
