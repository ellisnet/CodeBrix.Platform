using CodeBrix.Platform.UI.Tasks.LinkerHintsGenerator;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.LinkerHintsGeneratorTests;

/// <summary>
/// Fences the WPE1-5 fix of the linker-hint passes: they receive the application's trimming feature switches, so a
/// descriptor entry guarded by a switch the application turned off (the Toolkit's RootXamlControls) is not rooted in
/// pass 1 and does not keep its XAML alive in the final trim.
/// </summary>
[TestClass]
public class Given_LinkerFeatureSwitches
{
	[TestMethod]
	public void When_The_Application_Sets_Switches_Then_Each_Becomes_A_Linker_Feature_Argument_In_Order()
	{
		var result = LinkerFeatureSwitches.Format(new[]
		{
			("CodeBrix.Platform.UI.Toolkit.RootXamlControls", "false"),
			("CodeBrix.Platform.RootSkiaPlatformAssemblies", "true"),
		});

		result.Should().Be("--feature CodeBrix.Platform.UI.Toolkit.RootXamlControls false --feature CodeBrix.Platform.RootSkiaPlatformAssemblies true");
	}

	[TestMethod]
	public void When_The_Application_Sets_No_Switch_Then_No_Argument_Is_Added()
	{
		var result = LinkerFeatureSwitches.Format(System.Array.Empty<(string, string)>());

		result.Should().BeEmpty();
	}

	[TestMethod]
	public void When_A_Switch_Has_No_Value_Or_Contains_White_Space_Then_It_Is_Skipped()
	{
		var result = LinkerFeatureSwitches.Format(new[]
		{
			("Empty.Value", ""),
			("Has Space", "true"),
			("Good.Switch", "false"),
			("Bad.Value", "not true"),
		});

		result.Should().Be("--feature Good.Switch false");
	}
}
