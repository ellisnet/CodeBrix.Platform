#nullable enable

using System;
using System.Globalization;

namespace Windows.System.Profile.Internal;

internal static class VersionHelpers
{
	internal static long ToLong(Version version)
	{
		var major = (ushort)version.Major;
		var minor = (ushort)version.Minor;
		var build = (ushort)version.Build;
		var revision = (ushort)version.Revision;

		var versionNumber = (long)major << 48 | (long)minor << 32 | (long)build << 16 | (long)revision;
		return versionNumber;
	}

	/// <summary>
	/// Encodes <paramref name="version"/> the way WinUI encodes AnalyticsVersionInfo.DeviceFamilyVersion: major in bits
	/// 63..48, minor in 47..32, build in 31..16, revision in 15..0, written as an unsigned decimal string. Each component
	/// is clamped to 0..65535 (an unset component, -1, becomes 0).
	/// </summary>
	/// <param name="version">The version to encode.</param>
	/// <returns>The encoded version.</returns>
	internal static string ToDeviceFamilyVersion(Version version)
	{
		ArgumentNullException.ThrowIfNull(version);

		var encoded = (ulong)Clamp(version.Major) << 48
			| (ulong)Clamp(version.Minor) << 32
			| (ulong)Clamp(version.Build) << 16
			| Clamp(version.Revision);
		return encoded.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>
	/// Decodes a DeviceFamilyVersion string (see <see cref="ToDeviceFamilyVersion(Version)"/>) into its four components.
	/// </summary>
	/// <param name="deviceFamilyVersion">The encoded version.</param>
	/// <returns>The decoded version, or null when the string is not an unsigned decimal number.</returns>
	internal static Version? FromDeviceFamilyVersion(string? deviceFamilyVersion)
	{
		if (!ulong.TryParse(deviceFamilyVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var encoded))
		{
			return null;
		}

		return new Version(
			(int)((encoded >> 48) & 0xFFFF),
			(int)((encoded >> 32) & 0xFFFF),
			(int)((encoded >> 16) & 0xFFFF),
			(int)(encoded & 0xFFFF));
	}

	private static ushort Clamp(int component) => (ushort)Math.Clamp(component, 0, ushort.MaxValue);
}
