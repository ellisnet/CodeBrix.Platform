#if !__NETSTD_REFERENCE__
using System;

namespace Windows.System.Profile;

public partial class AnalyticsVersionInfo
{
	partial void Initialize()
	{
		// The platform names its operating-system family when it registers IDeviceFamilyPlatform (Android, Mobile);
		// otherwise (the Skia heads) it is Environment.OSVersion.Platform, as before.
		var family = CodeBrix.Platform.Contracts.PlatformContract.TryResolve<CodeBrix.Platform.Contracts.IDeviceFamilyPlatform>()?.OperatingSystemFamily;
		DeviceFamily = $"{(string.IsNullOrEmpty(family) ? Environment.OSVersion.Platform.ToString() : family)}.{AnalyticsInfo.DeviceForm}";
	}

	// WPE1-11 (FIXLIST [AP1.11]): a registered IDeviceFamilyPlatform makes DeviceFamily live - the contract and
	// AnalyticsInfo.DeviceForm (itself read on every call) are consulted on each read, so a window that moves to another
	// size class reports its current form. With nothing registered the value composed in Initialize is kept, unchanged.
	partial void GetLiveDeviceFamily(ref string deviceFamily)
	{
		var family = CodeBrix.Platform.Contracts.PlatformContract.TryResolve<CodeBrix.Platform.Contracts.IDeviceFamilyPlatform>()?.OperatingSystemFamily;
		if (!string.IsNullOrEmpty(family))
		{
			deviceFamily = $"{family}.{AnalyticsInfo.DeviceForm}";
		}
	}
}
#endif
