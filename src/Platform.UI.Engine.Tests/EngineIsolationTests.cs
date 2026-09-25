#nullable enable

using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Engine.Tests;

/// <summary>
/// The assertion helper itself: it must recognize every WinUI assembly and every Skia twin, and accept the base
/// assemblies and engine Cores (checked on names, so that this test loads nothing).
/// </summary>
public class EngineIsolationTests
{
	[Fact]
	public void When_Loaded_Names_Are_Classified_Then_WinUI_Assemblies_And_Skia_Twins_Are_Forbidden_And_Engine_Cores_Are_Not()
	{
		//Arrange
		var loaded = new[]
		{
			"CodeBrix.Platform.Foundation.Core",
			"CodeBrix.Platform.Foundation.Logging.Core",
			"CodeBrix.Platform.Extensions.Logging.Core",
			"CodeBrix.Platform.AppSettings.Core",
			"CodeBrix.Platform.UI.TextLayout.Core",
			"CodeBrix.Platform.UI.Core",
			"CodeBrix.Platform.UI.Composition.Core",
			"CodeBrix.Platform.Core",
			"CodeBrix.Platform.UI.Dispatching.Core",
			"CodeBrix.Platform.Xaml",
			"CodeBrix.Platform.UI",
			"CodeBrix.Platform.UI.TextLayout",
			"CodeBrix.Platform.UI.Runtime.Skia",
			"SkiaSharp",
			"System.Private.CoreLib",
		};

		//Act
		var forbidden = EngineIsolation.FindForbidden(loaded);

		//Assert
		forbidden.Should().Equal(
			"CodeBrix.Platform.UI.Core",
			"CodeBrix.Platform.UI.Composition.Core",
			"CodeBrix.Platform.Core",
			"CodeBrix.Platform.UI.Dispatching.Core",
			"CodeBrix.Platform.Xaml",
			"CodeBrix.Platform.UI",
			"CodeBrix.Platform.UI.TextLayout",
			"CodeBrix.Platform.UI.Runtime.Skia");
	}

	[Fact]
	public void When_Only_The_Test_Host_Runs_Then_No_WinUI_Assembly_Is_Loaded()
	{
		//Assert
		EngineIsolation.AssertNoWinUILoaded("test host");
	}
}
