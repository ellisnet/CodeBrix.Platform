#nullable enable

using System;
using System.Globalization;
using CodeBrix.Platform.Foundation.Extensibility;
using SilverAssertions;
using Windows.System.Profile;
using Windows.System.Profile.Internal;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fences for AnalyticsVersionInfo.DeviceFamilyVersion on the Core runtime: a registered IAnalyticsVersionInfoExtension
/// answers it (CodeBrix.Android supplies the Android release that way), and with none registered the host's
/// Environment.OSVersion is encoded the WinUI way (major 63..48, minor 47..32, build 31..16, revision 15..0).
/// </summary>
public class AnalyticsVersionInfoTests
{
	private static FakeVersionExtension? _extension;
	private static bool _registered;
	private static readonly object _gate = new();

	[Fact]
	public void When_A_Version_Extension_Is_Registered_Then_DeviceFamilyVersion_Is_Its_Value()
	{
		//Arrange
		var android13 = VersionHelpers.ToDeviceFamilyVersion(new Version(13, 0));

		lock (_gate)
		{
			UseExtension(new FakeVersionExtension(android13));
			try
			{
				//Act
				var fromShared = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;
				var fromNew = new AnalyticsVersionInfo().DeviceFamilyVersion;

				//Assert
				fromShared.Should().Be(android13);
				fromNew.Should().Be(android13);
				android13.Should().Be((13UL << 48).ToString(CultureInfo.InvariantCulture));
			}
			finally
			{
				UseExtension(null);
			}
		}
	}

	[Fact]
	public void When_The_Registered_Extension_Changes_Its_Answer_Between_Reads_Then_DeviceFamilyVersion_Follows()
	{
		//Arrange
		var extension = new FakeVersionExtension(VersionHelpers.ToDeviceFamilyVersion(new Version(13, 0)));

		lock (_gate)
		{
			UseExtension(extension);
			try
			{
				var info = new AnalyticsVersionInfo();
				var first = info.DeviceFamilyVersion;

				//Act
				extension.Value = VersionHelpers.ToDeviceFamilyVersion(new Version(16, 0));
				var second = info.DeviceFamilyVersion;

				//Assert
				VersionHelpers.FromDeviceFamilyVersion(first).Should().Be(new Version(13, 0, 0, 0));
				VersionHelpers.FromDeviceFamilyVersion(second).Should().Be(new Version(16, 0, 0, 0));
			}
			finally
			{
				UseExtension(null);
			}
		}
	}

	[Fact]
	public void When_The_Registered_Extension_Cannot_Say_Then_DeviceFamilyVersion_Is_Zero()
	{
		lock (_gate)
		{
			//Arrange
			UseExtension(new FakeVersionExtension(null));
			try
			{
				//Act
				var version = new AnalyticsVersionInfo().DeviceFamilyVersion;

				//Assert
				version.Should().Be("0", "a platform that registered the seam owns the answer, and null means it cannot say");
			}
			finally
			{
				UseExtension(null);
			}
		}
	}

	[Fact]
	public void When_No_Version_Extension_Is_Registered_Then_DeviceFamilyVersion_Is_The_Encoded_Host_Version()
	{
		lock (_gate)
		{
			//Arrange
			UseExtension(null);

			//Act
			var version = AnalyticsInfo.VersionInfo.DeviceFamilyVersion;

			//Assert
			version.Should().Be(VersionHelpers.ToDeviceFamilyVersion(Environment.OSVersion.Version));
			version.Should().NotBe("0", "every desktop host reports a major version above zero");
		}
	}

	[Fact]
	public void When_The_Rule_Is_Given_No_Extension_Then_It_Encodes_The_Host_Version()
	{
		//Act
		var withNone = AnalyticsVersionInfo.ResolveDeviceFamilyVersion(null, new Version(6, 12, 113));
		var withOne = AnalyticsVersionInfo.ResolveDeviceFamilyVersion(new FakeVersionExtension("42"), new Version(6, 12, 113));

		//Assert
		withNone.Should().Be(((6UL << 48) | (12UL << 32) | (113UL << 16)).ToString(CultureInfo.InvariantCulture));
		withOne.Should().Be("42");
	}

	[Theory]
	[InlineData("13.0", 13, 0, 0, 0)]
	[InlineData("10.0.22631.4460", 10, 0, 22631, 4460)]
	[InlineData("6.12.113", 6, 12, 113, 0)]
	[InlineData("70000.65536.65535.65536", 65535, 65535, 65535, 65535)]
	public void When_A_Version_Is_Encoded_Then_Decoding_Returns_Each_Component_Clamped(
		string version, int expectedMajor, int expectedMinor, int expectedBuild, int expectedRevision)
	{
		//Act (an unset build or revision is -1 in System.Version, and encodes as 0)
		var encoded = VersionHelpers.ToDeviceFamilyVersion(Version.Parse(version));
		var decoded = VersionHelpers.FromDeviceFamilyVersion(encoded);

		//Assert
		decoded.Should().Be(new Version(expectedMajor, expectedMinor, expectedBuild, expectedRevision));
	}

	[Fact]
	public void When_An_Encoded_Version_Is_Decoded_The_Way_SimpleOsInfo_Does_Then_The_Components_Match()
	{
		//Arrange: Android 14 (major 14, minor 0), as CodeBrix.Android would supply it
		var encoded = VersionHelpers.ToDeviceFamilyVersion(new Version(14, 0, 0, 0));

		//Act: the same masks and shifts as src-platforms/Platform.Simple/SimpleOsInfo.cs (HAS_CODEBRIX branch)
		var v = ulong.Parse(encoded, CultureInfo.InvariantCulture);
		var major = (int)(ushort)((v & 0xFFFF000000000000L) >> 48);
		var minor = (int)(ushort)((v & 0x0000FFFF00000000L) >> 32);
		var build = (int)(ushort)((v & 0x00000000FFFF0000L) >> 16);
		var revision = (int)(ushort)(v & 0x000000000000FFFFL);

		//Assert
		major.Should().Be(14);
		minor.Should().Be(0);
		build.Should().Be(0);
		revision.Should().Be(0);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("-1")]
	[InlineData("not a number")]
	public void When_A_DeviceFamilyVersion_Is_Not_An_Unsigned_Number_Then_It_Does_Not_Decode(string? encoded)
	{
		//Act
		var decoded = VersionHelpers.FromDeviceFamilyVersion(encoded);

		//Assert
		decoded.Should().BeNull();
	}

	private static void UseExtension(FakeVersionExtension? extension)
	{
		if (!_registered)
		{
			// A builder that returns null means "not registered" to the registry.
			ApiExtensibility.Register(typeof(IAnalyticsVersionInfoExtension), _ => _extension!);
			_registered = true;
		}

		_extension = extension;
	}

	private sealed class FakeVersionExtension(string? value) : IAnalyticsVersionInfoExtension
	{
		public string? Value { get; set; } = value;

		public string? GetDeviceFamilyVersion() => Value;
	}
}
