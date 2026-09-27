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

	/// <summary>
	/// Gets the version within the device family.
	/// </summary>
	/// <remarks>
	/// Needs to be parsable long number.
	/// </remarks>
	[NotImplemented("__SKIA__", "__CODEBRIX_CORE__")]
	public string DeviceFamilyVersion { get; private set; } = "0";
}
