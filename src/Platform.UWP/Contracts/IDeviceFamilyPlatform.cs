namespace CodeBrix.Platform.Contracts;

/// <summary>
/// The operating-system part of <see cref="Windows.System.Profile.AnalyticsVersionInfo.DeviceFamily"/>
/// ("&lt;family&gt;.&lt;form&gt;", e.g. "Android.Tablet"); the form part is AnalyticsInfo.DeviceForm.
/// </summary>
/// <remarks>
/// Implementers: Android, Mobile. Platform (Skia): not registered - the family stays
/// <see cref="System.Environment.OSVersion"/>.Platform ("Unix" on Linux and macOS, "Win32NT" on Windows), as before.
/// <para>
/// OPTIONAL contract, resolved through <see cref="CodeBrix.Platform.Foundation.Extensibility.ApiExtensibility"/> on EVERY
/// read of AnalyticsVersionInfo.DeviceFamily (WPE1-11): while it is registered, DeviceFamily is live -
/// "&lt;OperatingSystemFamily&gt;.&lt;AnalyticsInfo.DeviceForm&gt;" at the time of the read, so it follows the window
/// between size classes. Keep the getter cheap. When it is not registered, DeviceFamily is composed once, when
/// AnalyticsInfo creates its AnalyticsVersionInfo, as before. Suggested values: "Android" (CodeBrix.Android), "Apple"
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
