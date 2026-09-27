using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using CodeBrix.Platform.UI.SourceGenerators.XamlGenerator;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

/// <summary>
/// WPE1-11 (FIXLIST [WPE1-7]): the XAML generator's <c>CodeBrixHasLocalizationResources</c> attribute must depend only on
/// the compilation it is generated for. The generator keeps a static cache of parsed resw files (it lives as long as the
/// compiler server, across projects), and the cached keys carry the name of the assembly that parsed the file; the cache
/// used to be keyed by path and checksum only, so CodeBrix.Platform.UI.Core and the framework's unit-test flavour
/// (CodeBrix.Platform.UI), which compile the SAME resw files, got True or False depending on which compiled first.
/// Every test here runs the real generator over several compilations in ONE process, as the compiler server does.
/// </summary>
[TestClass]
public class Given_LocalizationResourcesAttribute
{
	private const string Resw = """
		<?xml version="1.0" encoding="utf-8"?>
		<root>
		  <data name="TEXT_TOGGLESWITCH_ON" xml:space="preserve">
		    <value>On</value>
		  </data>
		  <data name="TEXT_TOGGLESWITCH_OFF" xml:space="preserve">
		    <value>Off</value>
		  </data>
		</root>
		""";

	private const string EmptyResw = """
		<?xml version="1.0" encoding="utf-8"?>
		<root>
		</root>
		""";

	[TestMethod]
	public void When_Two_Assemblies_Compile_The_Same_Resource_File_Then_Both_Say_They_Have_Localization_Resources()
	{
		var shared = NewResourceFile(Resw);

		var core = Generate("Test.Localization.Core", shared);
		var skia = Generate("Test.Localization.Framework", shared);

		core.Should().Be("True");
		skia.Should().Be("True", "the second compilation of the same resw file must not see the first one's assembly name");
	}

	[TestMethod]
	public void When_The_Same_Two_Assemblies_Compile_In_The_Other_Order_Then_Both_Still_Say_True()
	{
		var shared = NewResourceFile(Resw);

		var skia = Generate("Test.Localization.Framework", shared);
		var core = Generate("Test.Localization.Core", shared);

		skia.Should().Be("True");
		core.Should().Be("True");
	}

	[TestMethod]
	public void When_One_Assembly_Compiles_A_Resource_File_Twice_Then_The_Cached_Parse_Gives_The_Same_Answer()
	{
		var shared = NewResourceFile(Resw);

		var first = Generate("Test.Localization.Core", shared);
		var other = Generate("Test.Localization.Framework", shared);
		var again = Generate("Test.Localization.Core", shared);

		first.Should().Be("True");
		other.Should().Be("True");
		again.Should().Be("True");
	}

	[TestMethod]
	public void When_Assemblies_Compile_Different_Resource_Sets_Then_Each_Answer_Follows_Its_Own_Set()
	{
		var withStrings = NewResourceFile(Resw);
		var withoutStrings = NewResourceFile(EmptyResw);

		var strings = Generate("Test.Localization.Core", withStrings);
		var empty = Generate("Test.Localization.Framework", withoutStrings);
		var none = Generate("Test.Localization.Other");
		var both = Generate("Test.Localization.Framework", withoutStrings, withStrings);

		strings.Should().Be("True");
		empty.Should().Be("False");
		none.Should().Be("False");
		both.Should().Be("True");
	}

	/// <summary>A resw file at a path no other test uses (the generator's cache is process-wide).</summary>
	private static (string Path, string Text) NewResourceFile(string text)
		=> ($"/Project/{Guid.NewGuid():N}/Strings/en/Resources.resw", text);

	/// <summary>Runs the XAML generator over an empty compilation named <paramref name="assemblyName"/> with the given
	/// PRIResource items and returns the value of its CodeBrixHasLocalizationResources attribute.</summary>
	private static string Generate(string assemblyName, params (string Path, string Text)[] resourceFiles)
	{
		var compilation = CSharpCompilation.Create(
			assemblyName,
			[],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var additionalTexts = resourceFiles.Select(f => (AdditionalText)new InMemoryAdditionalText(f.Path, f.Text)).ToImmutableArray();

		var driver = CSharpGeneratorDriver.Create(
			[new XamlCodeGenerator()],
			additionalTexts,
			new CSharpParseOptions(LanguageVersion.Latest),
			new TestOptionsProvider(additionalTexts));

		var result = driver.RunGenerators(compilation).GetRunResult().Results.Single();
		var attribute = result.GeneratedSources.Single(s => s.HintName.StartsWith("LocalizationResources", StringComparison.Ordinal));
		var text = attribute.SourceText.ToString();

		const string marker = "\"CodeBrixHasLocalizationResources\", \"";
		var start = text.IndexOf(marker, StringComparison.Ordinal);
		start.Should().BeGreaterThan(-1, text);
		start += marker.Length;
		return text.Substring(start, text.IndexOf('"', start) - start);
	}

	private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
	{
		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
	}

	private sealed class TestOptionsProvider(ImmutableArray<AdditionalText> resourceFiles) : AnalyzerConfigOptionsProvider
	{
		private static readonly DictionaryOptions Empty = new(new Dictionary<string, string>());

		private readonly ImmutableArray<AdditionalText> _resourceFiles = resourceFiles;

		public override AnalyzerConfigOptions GlobalOptions { get; } = new DictionaryOptions(new Dictionary<string, string>
		{
			["build_property.MSBuildProjectFullPath"] = "/Project/Project.csproj",
			["build_property.RootNamespace"] = "Test.Localization",
			["build_property.Configuration"] = "Release",
			["build_property.CodeBrixForceHotReloadCodeGen"] = "false",
		});

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
			=> _resourceFiles.Contains(textFile)
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
