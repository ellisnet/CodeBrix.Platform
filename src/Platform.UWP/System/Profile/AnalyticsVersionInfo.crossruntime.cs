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
}
#endif
