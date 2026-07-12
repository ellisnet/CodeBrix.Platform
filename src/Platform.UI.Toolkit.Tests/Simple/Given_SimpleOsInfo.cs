using System;
using CodeBrix.Platform.Simple;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeBrix.Platform.UI.Toolkit.Tests.Simple;

[TestClass]
public class Given_SimpleOsInfo
{
	[TestMethod]
	public void When_FullVersion_Uses_VersionNumber() =>
		new OsVersionInfo { VersionNumber = "24.04" }.FullVersion.Should().Be("24.04");

	[TestMethod]
	public void When_FullVersion_Composes_Version_Parts() =>
		new OsVersionInfo
		{
			MajorVersion = 12,
			MinorVersion = 3,
			BuildVersion = 4,
			RevisionVersion = 5,
		}.FullVersion.Should().Be("12.3.4.5");

	[TestMethod]
	public void When_FullVersion_Appends_LTS() =>
		new OsVersionInfo { VersionNumber = "24.04", IsLongTermSupported = true }
			.FullVersion.Should().Be("24.04 LTS");

	[TestMethod]
	public void When_FullVersion_Skips_LTS_Already_In_VersionNumber() =>
		new OsVersionInfo { VersionNumber = "24.04 LTS", IsLongTermSupported = true }
			.FullVersion.Should().Be("24.04 LTS");

	[TestMethod]
	public void When_FullVersion_Includes_Codename_In_Parens() =>
		new OsVersionInfo { VersionNumber = "13", VersionCodename = "trixie" }
			.FullVersion.Should().Be("13 (trixie)");

	[TestMethod]
	public void When_FullVersion_Skips_Codename_Already_In_VersionNumber() =>
		new OsVersionInfo { VersionNumber = "13 (trixie)", VersionCodename = "trixie" }
			.FullVersion.Should().Be("13 (trixie)");

	[TestMethod]
	public void When_FullVersion_Includes_BasedOnVersion() =>
		new OsVersionInfo { VersionNumber = "7", VersionCodename = "gigi", BasedOnVersion = "Debian 13" }
			.FullVersion.Should().Be("7 (gigi - based on: Debian 13)");

	[TestMethod]
	public void When_RunUnixShellCommandResult_Defaults()
	{
		//Arrange
		var result = new RunUnixShellCommandResult();

		//Assert
		Assert.IsFalse(result.IsComplete);
		Assert.IsFalse(result.IsError);
		Assert.IsTrue(result.IsEmptyOutput);
	}

	[TestMethod]
	public void When_RunUnixShellCommandResult_Has_Error_Text()
	{
		//Arrange
		var result = new RunUnixShellCommandResult { Error = "boom" };

		//Assert
		Assert.IsTrue(result.IsError);
	}

	[TestMethod]
	public void When_RunUnixShellCommandResult_Has_Exception()
	{
		//Arrange
		var result = new RunUnixShellCommandResult { Exception = new InvalidOperationException() };

		//Assert
		Assert.IsTrue(result.IsError);
	}

	[TestMethod]
	public void When_RunUnixShellCommandResult_Splits_Output_Lines()
	{
		//Arrange
		var result = new RunUnixShellCommandResult { Output = "one\ntwo\r\nthree\n" };

		//Assert
		result.OutputLines.Length.Should().Be(3);
		result.OutputLines[0].Should().Be("one");
		result.OutputLines[2].Should().Be("three");
	}

	[TestMethod]
	public void When_SetComplete_Marks_Complete()
	{
		//Arrange
		var result = new RunUnixShellCommandResult();

		//Act
		result.SetComplete();

		//Assert
		Assert.IsTrue(result.IsComplete);
	}
}
