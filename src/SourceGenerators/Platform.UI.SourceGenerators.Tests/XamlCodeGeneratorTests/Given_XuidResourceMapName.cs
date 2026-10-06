using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using CodeBrix.Platform.UI.SourceGenerators.Tests.Verifiers;
using CodeBrix.Platform.UI.SourceGenerators.XamlGenerator;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

/// <summary>
/// WPE1-13 (FIXLIST_platform_core_split [WPE1-11] FOUND latent): an x:Uid in a library's XAML is looked up in the
/// resource map "&lt;library&gt;/&lt;resw file&gt;". CodeBrix.Platform.UI.Core generates its string resources into the map
/// of the framework's library name ("CodeBrix.Platform.UI/Resources", $(CodeBrixResourceMapLibraryName)), so its x:Uid
/// paths must name that map, not the assembly name. Without the property the assembly name is used, as before; the
/// localization-resources attribute says True either way.
/// </summary>
[TestClass]
public class Given_XuidResourceMapName
{
	private const string Resw = """
		<?xml version="1.0" encoding="utf-8"?>
		<root>
		  <data name="MapNameGreeting.Text" xml:space="preserve">
		    <value>Hello</value>
		  </data>
		</root>
		""";

	private const string Xaml = """
		<Page
			x:Class="Test.Xuid.MapNamePage"
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			<TextBlock x:Uid="MapNameGreeting" />
		</Page>
		""";

	[TestMethod]
	public void When_The_Library_Names_Its_Resource_Map_Then_The_XUid_Path_Names_That_Map()
	{
		var (code, hasResources) = Generate("Test.Xuid.Core", resourceMapLibraryName: "Test.Xuid");

		code.Should().Contain("GetResourceStringForXUid(\"Test.Xuid/Resources\", \"MapNameGreeting/Text\")");
		code.Should().NotContain("\"Test.Xuid.Core/Resources\"");
		hasResources.Should().Be("True");
	}

	[TestMethod]
	public void When_No_Resource_Map_Name_Is_Set_Then_The_XUid_Path_Names_The_Assembly()
	{
		var (code, hasResources) = Generate("Test.Xuid.Library", resourceMapLibraryName: null);

		code.Should().Contain("GetResourceStringForXUid(\"Test.Xuid.Library/Resources\", \"MapNameGreeting/Text\")");
		hasResources.Should().Be("True");
	}

	/// <summary>Runs the XAML generator over a library compilation with one page and one resw file; returns the page's
	/// generated code and the CodeBrixHasLocalizationResources value.</summary>
	private static (string Code, string HasResources) Generate(string assemblyName, string? resourceMapLibraryName)
	{
		// The generator turns the project folder into a Uri, which needs a rooted path on the build host:
		// "/Project" has no drive on Windows ("Invalid URI").
		var projectRoot = OperatingSystem.IsWindows() ? "C:/Project" : "/Project";
		var folder = $"{projectRoot}/{Guid.NewGuid():N}";
		var page = new InMemoryAdditionalText($"{folder}/MapNamePage.xaml", Xaml);
		var resources = new InMemoryAdditionalText($"{folder}/Strings/en/Resources.resw", Resw);

		var trustedPlatformAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Where(p => !Path.GetFileName(p).StartsWith("CodeBrix.", StringComparison.Ordinal))
			.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));

		var compilation = CSharpCompilation.Create(
			assemblyName,
			[CSharpSyntaxTree.ParseText("namespace Test.Xuid { public sealed partial class MapNamePage : global::Microsoft.UI.Xaml.Controls.Page { } }")],
			trustedPlatformAssemblies.Concat(CodeBrixAssemblyHelper.LoadAssemblies()),
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var globalOptions = new Dictionary<string, string>
		{
			["build_property.MSBuildProjectFullPath"] = $"{folder}/Project.csproj",
			["build_property.RootNamespace"] = "Test.Xuid",
			["build_property.Configuration"] = "Release",
			["build_property.CodeBrixForceHotReloadCodeGen"] = "false",
		};

		if (resourceMapLibraryName is not null)
		{
			globalOptions["build_property.CodeBrixResourceMapLibraryName"] = resourceMapLibraryName;
		}

		var driver = CSharpGeneratorDriver.Create(
			[new XamlCodeGenerator()],
			[page, resources],
			new CSharpParseOptions(LanguageVersion.Latest),
			new TestOptionsProvider(globalOptions, page, resources));

		var result = driver.RunGenerators(compilation).GetRunResult().Results.Single();

		var code = string.Join(
			"\n",
			result.GeneratedSources
				.Where(s => s.HintName.Contains("MapNamePage", StringComparison.Ordinal))
				.Select(s => s.SourceText.ToString()));

		var attribute = result.GeneratedSources.Single(s => s.HintName.StartsWith("LocalizationResources", StringComparison.Ordinal)).SourceText.ToString();
		const string marker = "\"CodeBrixHasLocalizationResources\", \"";
		var start = attribute.IndexOf(marker, StringComparison.Ordinal);
		start.Should().BeGreaterThan(-1, attribute);
		start += marker.Length;

		code.Should().NotBeEmpty($"the page must be generated (generator diagnostics: {string.Join("; ", result.Diagnostics)})");

		return (code, attribute.Substring(start, attribute.IndexOf('"', start) - start));
	}

	private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
	}

	private sealed class TestOptionsProvider(Dictionary<string, string> globalOptions, AdditionalText page, AdditionalText resources) : AnalyzerConfigOptionsProvider
	{
		private static readonly DictionaryOptions Empty = new(new Dictionary<string, string>());

		public override AnalyzerConfigOptions GlobalOptions { get; } = new DictionaryOptions(globalOptions);

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
			=> textFile == page
				? new DictionaryOptions(new Dictionary<string, string> { ["build_metadata.AdditionalFiles.SourceItemGroup"] = "Page" })
				: textFile == resources
					? new DictionaryOptions(new Dictionary<string, string> { ["build_metadata.AdditionalFiles.SourceItemGroup"] = "PRIResource" })
					: Empty;
	}

	private sealed class DictionaryOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
	{
		// [NotNullWhen(true)] is not written: the generator assembly under test carries its own copy of that attribute,
		// which makes the name ambiguous here (CS0433).
#pragma warning disable CS8765
		public override bool TryGetValue(string key, out string? value) => values.TryGetValue(key, out value);
#pragma warning restore CS8765
	}
}
