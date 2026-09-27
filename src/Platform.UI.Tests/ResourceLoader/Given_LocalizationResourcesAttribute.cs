using System;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SilverAssertions;

namespace CodeBrix.Platform.UI.Tests.ResourceLoaderTests;

/// <summary>
/// WPE1-11 (FIXLIST [WPE1-7]): the unit-test flavour of the framework (assembly CodeBrix.Platform.UI, built by
/// src/Platform.UI/Platform.UI.Tests.csproj) compiles the SAME .resw files as CodeBrix.Platform.UI.Core. The XAML
/// generator's resource cache was keyed without the assembly name, so whichever of the two the compiler server built
/// first decided the other's CodeBrixHasLocalizationResources attribute (this flavour was False in Release, True in
/// Debug). It embeds the framework's strings, so it must say True in every build.
/// </summary>
[TestClass]
public class Given_LocalizationResourcesAttribute
{
	[TestMethod]
	public void When_The_Framework_Unit_Test_Flavour_Is_Built_Then_It_Says_It_Has_Localization_Resources()
	{
		var framework = typeof(ToggleSwitch).Assembly;

		var value = framework.GetCustomAttributes<AssemblyMetadataAttribute>()
			.SingleOrDefault(a => a.Key == "CodeBrixHasLocalizationResources")?.Value;
		var resourceFiles = framework.GetManifestResourceNames().Count(n => n.EndsWith(".upri", StringComparison.Ordinal));

		framework.GetName().Name.Should().Be("CodeBrix.Platform.UI");
		resourceFiles.Should().BeGreaterThan(0);
		value.Should().Be("True");
	}
}
