using System.Collections.Generic;
using CodeBrix.Platform.UI.SourceGenerators.BindableTypeProviders;
using CodeBrix.Platform.UI.Tasks.LinkerHintsGenerator;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.LinkerHintsGeneratorTests;

/// <summary>
/// Fences decision D10 (WPE1-11): with CodeBrixXamlResourcesTrimming=true the generated BindableMetadataProvider of an
/// application is gated by a linker hint that no pass ever found "available", so it was compiled away in the final
/// trim and a navigated page lost its parameterless constructor (MissingMethodException at startup). The linker-hint
/// passes and the final trim keep every provider hint on; the per-type hints still come from the passes. The whole path
/// is proven by publishing the template app trimmed in that mode and starting it (MAINTAINER-README, TRIMMING).
/// </summary>
[TestClass]
public class Given_LinkerHintBindableMetadata
{
	private static readonly string AppProviderHint = LinkerHintsHelpers.GetPropertyAvailableName("TemplateApp.LinuxFrameBuffer.BindableMetadataProvider");
	private static readonly string PageHint = LinkerHintsHelpers.GetPropertyAvailableName("TemplateApp.Views.MainPage");

	[TestMethod]
	public void When_A_Hint_Gates_A_Generated_BindableMetadataProvider_Then_It_Is_Recognised()
	{
		AppProviderHint.Should().Be("Is_TemplateApp_LinuxFrameBuffer_BindableMetadataProvider_Available");
		LinkerHintBindableMetadata.IsProviderHint(AppProviderHint).Should().BeTrue();
		LinkerHintBindableMetadata.IsProviderHint(LinkerHintsHelpers.GetPropertyAvailableName("CodeBrix.Platform.UI.BindableMetadataProvider")).Should().BeTrue();
	}

	[TestMethod]
	public void When_A_Hint_Gates_A_Type_Then_It_Is_Not_A_Provider_Hint()
	{
		LinkerHintBindableMetadata.IsProviderHint(PageHint).Should().BeFalse();
		LinkerHintBindableMetadata.IsProviderHint("Is_TemplateApp_BindableMetadataProviderHelper_Available").Should().BeFalse();
		LinkerHintBindableMetadata.IsProviderHint("TemplateApp_BindableMetadataProvider_Available").Should().BeFalse();
	}

	[TestMethod]
	public void When_The_First_Pass_Starts_Then_Only_The_Provider_Hints_Are_On()
	{
		LinkerHintBindableMetadata.InitialValue(AppProviderHint).Should().Be("true");
		LinkerHintBindableMetadata.InitialValue(PageHint).Should().Be("false");
	}

	[TestMethod]
	public void When_A_Pass_Found_No_Provider_Available_Then_The_Next_Pass_And_The_Final_Trim_Still_Enable_It()
	{
		// What BuildResultingFeaturesList produced before D10: every hint false, then true for the DependencyObject types
		// that survived the pass. The provider class is not one, so it stayed false - also in the final trim.
		var features = new Dictionary<string, string>
		{
			[AppProviderHint] = "false",
			[PageHint] = "true",
			["Is_TemplateApp_Views_UnusedPage_Available"] = "false",
		};

		LinkerHintBindableMetadata.EnableProviders(features);

		features[AppProviderHint].Should().Be("true");
		features[PageHint].Should().Be("true");
		features["Is_TemplateApp_Views_UnusedPage_Available"].Should().Be("false", "a type the passes found unused stays trimmed");
	}
}
