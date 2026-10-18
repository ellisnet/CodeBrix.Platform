using System;
using CodeBrix.Platform;
using CodeBrix.Platform.Extensions;

namespace Windows.System.Profile;

/// <summary>
/// Provides version information about the device family.
/// </summary>
public partial class AnalyticsVersionInfo
{
	public AnalyticsVersionInfo() => Initialize();

	partial void Initialize();

	/// <summary>
	/// Lets the platform compute <see cref="DeviceFamily"/> at the time it is read (it leaves the value alone when it
	/// has nothing live to say, and the value composed when this instance was created is returned).
	/// </summary>
	/// <param name="deviceFamily">The value composed at creation; replaced by the live value when there is one.</param>
	partial void GetLiveDeviceFamily(ref string deviceFamily);

	/// <summary>
	/// Lets the platform compute <see cref="DeviceFamilyVersion"/> at the time it is read (it leaves the value alone when
	/// it has nothing live to say, and the value composed when this instance was created is returned).
	/// </summary>
	/// <param name="deviceFamilyVersion">The value composed at creation; replaced by the live value when there is one.</param>
	partial void GetLiveDeviceFamilyVersion(ref string deviceFamilyVersion);

	private string _deviceFamily = $"{Environment.OSVersion.Platform}.Desktop";

	/// <summary>
	/// Gets a string that represents the type of device the application is running on.
	/// </summary>
	/// <remarks>
	/// When the platform registered IDeviceFamilyPlatform (CodeBrix.Android, CodeBrix.Mobile) it is read on EVERY access,
	/// "&lt;family&gt;.&lt;current AnalyticsInfo.DeviceForm&gt;", so it follows the window between size classes (a
	/// docked phone). Otherwise (the Skia heads) it is the value composed once, when AnalyticsInfo.VersionInfo was
	/// created, as before.
	/// </remarks>
	public string DeviceFamily
	{
		get
		{
			var deviceFamily = _deviceFamily;
			GetLiveDeviceFamily(ref deviceFamily);
			return deviceFamily;
		}
		private set => _deviceFamily = value;
	}

	private string _deviceFamilyVersion = "0";

	/// <summary>
	/// Gets the version within the device family, encoded the way WinUI encodes it: one unsigned 64-bit number written
	/// as a decimal string, with the major version in bits 63..48, the minor in 47..32, the build in 31..16 and the
	/// revision in 15..0.
	/// </summary>
	/// <remarks>
	/// On the Skia and Core runtimes the platform supplies it when it registers a version extension (CodeBrix.Android
	/// reports the Android release); otherwise it is the host's <see cref="Environment.OSVersion"/>, encoded the same way.
	/// "0" means the platform could not say.
	/// </remarks>
	public string DeviceFamilyVersion
	{
		get
		{
			var deviceFamilyVersion = _deviceFamilyVersion;
			GetLiveDeviceFamilyVersion(ref deviceFamilyVersion);
			return deviceFamilyVersion;
		}
		private set => _deviceFamilyVersion = value;
	}
}
