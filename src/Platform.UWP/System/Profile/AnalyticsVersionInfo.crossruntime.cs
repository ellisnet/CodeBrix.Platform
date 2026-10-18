#if !__NETSTD_REFERENCE__
using System;
using CodeBrix.Platform.Foundation.Extensibility;
using Windows.System.Profile.Internal;

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

	// DeviceFamilyVersion is read live as well: a registered IAnalyticsVersionInfoExtension (CodeBrix.Android) answers it;
	// with none registered the host's Environment.OSVersion is encoded the WinUI way, so desktop heads report something
	// real instead of "0".
	partial void GetLiveDeviceFamilyVersion(ref string deviceFamilyVersion)
	{
		ApiExtensibility.CreateInstance<IAnalyticsVersionInfoExtension>(this, out var extension);
		deviceFamilyVersion = ResolveDeviceFamilyVersion(extension, Environment.OSVersion.Version);
	}

	/// <summary>
	/// The DeviceFamilyVersion rule: the registered extension's value when there is an extension (its null - "cannot
	/// say" - becomes "0"); otherwise <paramref name="hostVersion"/> encoded the WinUI way.
	/// </summary>
	/// <param name="extension">The registered extension, or null when none is registered.</param>
	/// <param name="hostVersion">The host operating-system version (Environment.OSVersion.Version).</param>
	/// <returns>The encoded version, as an unsigned decimal string.</returns>
	internal static string ResolveDeviceFamilyVersion(IAnalyticsVersionInfoExtension extension, Version hostVersion)
	{
		if (extension is not null)
		{
			var supplied = extension.GetDeviceFamilyVersion();
			return string.IsNullOrEmpty(supplied) ? "0" : supplied;
		}

		return VersionHelpers.ToDeviceFamilyVersion(hostVersion);
	}
}
#endif
