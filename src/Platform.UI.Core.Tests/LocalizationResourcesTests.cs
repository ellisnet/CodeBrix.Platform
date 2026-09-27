#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.UI.Xaml.Controls;
using SilverAssertions;
using Windows.ApplicationModel.Resources;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// WPE1-11 (FIXLIST [WPE1-7] / [AP1.11]): the generated <c>CodeBrixHasLocalizationResources</c> assembly attribute tells a
/// consuming application's XAML generator whether to register an assembly's localized strings
/// (<c>ResourceLoader.AddLookupAssembly</c>). CodeBrix.Platform.UI.Core embeds the framework's strings, so the attribute
/// must say True - it said True or False depending on which project the compiler server had compiled first, and with False
/// every Core-supplied string (a ToggleSwitch's default On/Off, ...) was empty in an application.
/// </summary>
public class LocalizationResourcesTests
{
	private const string AttributeKey = "CodeBrixHasLocalizationResources";

	[Fact]
	public void When_The_Core_UI_Assembly_Is_Built_Then_It_Says_It_Has_Localization_Resources()
	{
		//Arrange
		var core = typeof(ToggleSwitch).Assembly;

		//Act
		var value = core.GetCustomAttributes<AssemblyMetadataAttribute>().SingleOrDefault(a => a.Key == AttributeKey)?.Value;
		var resourceFiles = core.GetManifestResourceNames().Count(n => n.EndsWith(".upri", StringComparison.Ordinal));

		//Assert
		core.GetName().Name.Should().Be("CodeBrix.Platform.UI.Core");
		resourceFiles.Should().BeGreaterThan(0);
		value.Should().Be("True");
	}

	[Fact]
	public void When_Any_Core_Assembly_Carries_The_Attribute_Then_It_Says_True_Exactly_When_The_Assembly_Embeds_Localized_Strings()
	{
		//Arrange
		var entries = CoreAssemblyReferenceTests.CoreAssemblyEntries();

		//Act
		var wrong = entries
			.Select(e => (Name: e.Key, Info: ReadLocalization(e.Value.Path)))
			.Where(e => e.Info.Attribute is not null && e.Info.Attribute != (e.Info.ResourceFiles > 0 ? "True" : "False"))
			.Select(e => $"{e.Name}: attribute {e.Info.Attribute}, {e.Info.ResourceFiles} .upri resources")
			.ToArray();

		//Assert
		wrong.Should().BeEmpty();
	}

	[Fact]
	public void When_An_Application_Registers_Core_As_Its_Generated_Code_Does_Then_A_ToggleSwitch_Shows_On_And_Off()
	{
		//Arrange
		var loader = ResourceLoader.GetForViewIndependentUse("CodeBrix.Platform.UI/Resources");
		var before = loader.GetString("TEXT_TOGGLESWITCH_ON");

		//Act
		// What an application's generated App code does (XamlFileGenerator.BuildResourceLoaderFromAssembly): set the
		// default language, then register every referenced assembly whose CodeBrixHasLocalizationResources says True.
		ResourceLoader.DefaultLanguage = "en";
		var core = typeof(ToggleSwitch).Assembly;
		var registered = core.GetCustomAttributes<AssemblyMetadataAttribute>()
			.Any(a => a.Key == AttributeKey && string.Equals(a.Value, "True", StringComparison.OrdinalIgnoreCase));
		if (registered)
		{
			ResourceLoader.AddLookupAssembly(Assembly.Load("CodeBrix.Platform.UI.Core"));
		}

		var toggle = new ToggleSwitch();

		//Assert
		before.Should().BeEmpty("nothing in this process registered the Core strings before this test");
		registered.Should().BeTrue();
		toggle.OnContent.Should().Be("On");
		toggle.OffContent.Should().Be("Off");
	}

	private static (string? Attribute, int ResourceFiles) ReadLocalization(string path)
	{
		using var stream = File.OpenRead(path);
		using var pe = new PEReader(stream);
		var md = pe.GetMetadataReader();

		string? attribute = null;
		foreach (var handle in md.GetAssemblyDefinition().GetCustomAttributes())
		{
			var custom = md.GetCustomAttribute(handle);
			if (custom.Constructor.Kind != HandleKind.MemberReference)
			{
				continue;
			}

			var constructor = md.GetMemberReference((MemberReferenceHandle)custom.Constructor);
			if (constructor.Parent.Kind != HandleKind.TypeReference
				|| md.GetString(md.GetTypeReference((TypeReferenceHandle)constructor.Parent).Name) != nameof(AssemblyMetadataAttribute))
			{
				continue;
			}

			var blob = md.GetBlobReader(custom.Value);
			blob.ReadUInt16();
			if (blob.ReadSerializedString() == AttributeKey)
			{
				attribute = blob.ReadSerializedString();
			}
		}

		var resourceFiles = md.ManifestResources
			.Count(h => md.GetString(md.GetManifestResource(h).Name).EndsWith(".upri", StringComparison.Ordinal));

		return (attribute, resourceFiles);
	}
}
