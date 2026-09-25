using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using CodeBrix.Platform.UI.SourceGenerators.Tests.Verifiers;
using CodeBrix.Platform.UI.SourceGenerators.XamlGenerator;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// A consumer that defines the Apple UIKit symbols (<c>__IOS__</c>, <c>__APPLE_UIKIT__</c>, <c>__TVOS__</c>,
/// <c>__MACCATALYST__</c>) and builds for an iOS runtime identifier gets the Core dialect: template roots are the Core
/// <c>UIElement</c>, no type deriving from <c>UIKit.UIView</c> is treated as a native view (no native constructors, no
/// view adaptation, no accessibility identifier), and no Hot Restart application delegate is generated. This family
/// has no UIKit-native head, so the generator output must not depend on those inputs.
/// </summary>
[TestClass]
public class Given_AppleDefinedConsumer
{
	private static readonly string[] AppleSymbols = ["__IOS__", "__APPLE_UIKIT__", "__TVOS__", "__MACCATALYST__"];

	private const string PageXaml = """
		<Page x:Class="AppleDialectConsumer.Views.TemplatedPage"
		      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			<Page.Resources>
				<BitmapImage x:Key="LogoSource" UriSource="ms-appx:///Assets/logo.png" />
				<ControlTemplate x:Key="RoundButtonTemplate" TargetType="Button">
					<Border Background="{TemplateBinding Background}" CornerRadius="8" Padding="4">
						<ContentPresenter Content="{TemplateBinding Content}" />
					</Border>
				</ControlTemplate>
				<DataTemplate x:Key="ItemTemplate">
					<StackPanel Orientation="Horizontal">
						<Image Source="{StaticResource LogoSource}" Width="16" Height="16" />
						<TextBlock Text="{Binding}" />
					</StackPanel>
				</DataTemplate>
			</Page.Resources>
			<StackPanel>
				<Image x:Name="HeaderImage" Source="ms-appx:///Assets/logo.png" Width="32" Height="32" />
				<Image Source="{StaticResource LogoSource}" />
				<Button x:Name="GoButton" Template="{StaticResource RoundButtonTemplate}" Content="Go" />
				<ListView x:Name="Items" ItemTemplate="{StaticResource ItemTemplate}" />
			</StackPanel>
		</Page>
		""";

	private const string CodeBehind = """
		using Microsoft.UI.Xaml.Controls;

		namespace AppleDialectConsumer.Views
		{
			public sealed partial class TemplatedPage : Page
			{
				public TemplatedPage()
				{
					this.InitializeComponent();
				}
			}
		}
		""";

	// Stand-ins for the Apple platform types the removed upstream paths keyed on, so that the upstream UIKit-head
	// code (template root alias, native view detection and adaptation, native constructors) would have something
	// to bind to.
	private const string AppleStubs = """
		using System;

		namespace Foundation
		{
			[AttributeUsage(AttributeTargets.All)]
			public sealed class RegisterAttribute : Attribute
			{
				public RegisterAttribute() { }
				public RegisterAttribute(string name) { }
			}
		}

		namespace ObjCRuntime
		{
			public readonly struct NativeHandle { }
		}

		namespace UIKit
		{
			public class UIView
			{
				public UIView() { }
				protected UIView(IntPtr handle) { }
				protected UIView(ObjCRuntime.NativeHandle handle) { }
				public string AccessibilityIdentifier { get; set; }
			}
		}

		namespace AppleDialectConsumer
		{
			public partial class LegacyNativeView : UIKit.UIView
			{
			}
		}
		""";

	private const string CoreUIElement = "Microsoft.UI.Xaml.UIElement";

	[TestMethod]
	public async Task When_Apple_Defined_Templated_Page_Matches_Baseline_And_Compiles()
	{
		// The snapshot harness compiles the generated code with the consumer's symbols; with the UIKit branch of the
		// template root alias the template builders were typed UIKit.UIView here and did not compile.
		var test = new Verify.Test(new XamlFile("TemplatedPage.xaml", PageXaml))
		{
			TestState =
			{
				Sources = { CodeBehind, AppleStubs },
			},
			PreprocessorSymbols = AppleSymbols,
			GlobalConfigOverride = new Dictionary<string, string>
			{
				{ "build_property.RuntimeIdentifier", "ios-arm64" },
				{ "build_property.TargetFramework", "net10.0-ios" },
			},
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Apple_Defined_Template_Roots_Are_Core_UIElement()
	{
		var run = await RunAllGeneratorsAsync(AppleSymbols, appleTarget: true);

		run.CompilationErrors.Should().BeEmpty();

		var page = run.GeneratedTrees.Single(t => Path.GetFileName(t.FilePath).StartsWith("TemplatedPage_", StringComparison.Ordinal));
		var model = run.Output.GetSemanticModel(page);
		var root = page.GetRoot();

		// Only the active branch of the generated #if chain is a syntax node; its alias must bind to the Core UIElement.
		var viewAlias = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Single(u => u.Alias?.Name.Identifier.ValueText == "_View");
		model.GetDeclaredSymbol(viewAlias)!.Target.ToDisplayString().Should().Be(CoreUIElement);

		// The ControlTemplate and DataTemplate builders return the template root type.
		var templateBuilders = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
			.Where(m => m.ReturnType is IdentifierNameSyntax { Identifier.ValueText: "_View" })
			.ToArray();
		templateBuilders.Length.Should().BeGreaterThanOrEqualTo(2);
		foreach (var builder in templateBuilders)
		{
			model.GetDeclaredSymbol(builder)!.ReturnType.ToDisplayString().Should().Be(CoreUIElement);
		}
	}

	[TestMethod]
	public async Task When_Apple_Target_Set_No_UIKit_Head_Code_Is_Generated()
	{
		var run = await RunAllGeneratorsAsync(AppleSymbols, appleTarget: true);

		foreach (var (hintName, text) in run.Sources)
		{
			hintName.Should().NotContain("HotRestart");
			text.Should().NotContain("UIKit.");
			text.Should().NotContain("__APPLE_UIKIT__");
			text.Should().NotContain("Foundation.Register");
			text.Should().NotContain("ObjCRuntime.NativeHandle");
			text.Should().NotContain("AccessibilityIdentifier");
			text.Should().NotContain("AdaptNative");
			text.Should().NotContain("WillMoveToSuperview");
			text.Should().NotContain("MovedToWindow");
		}

		// A consumer type deriving from UIKit.UIView is not a native view: no native constructor is generated.
		run.Sources.Keys.Should().NotContain(k => k.Contains("LegacyNativeView"));
	}

	[TestMethod]
	public async Task When_Apple_Symbols_Toggled_Generated_Sources_Are_Identical()
	{
		var apple = await RunAllGeneratorsAsync(AppleSymbols, appleTarget: true);
		var skia = await RunAllGeneratorsAsync(["HAS_CODEBRIX_SKIA"], appleTarget: false);

		apple.Sources.Keys.OrderBy(k => k, StringComparer.Ordinal)
			.Should().Equal(skia.Sources.Keys.OrderBy(k => k, StringComparer.Ordinal));
		foreach (var (hintName, text) in skia.Sources)
		{
			apple.Sources[hintName].Should().Be(text, $"'{hintName}' must not depend on the Apple UIKit symbols or an iOS target");
		}
	}

	private sealed record GeneratorRun(
		Compilation Output,
		IReadOnlyList<SyntaxTree> GeneratedTrees,
		IReadOnlyDictionary<string, string> Sources,
		IReadOnlyList<string> CompilationErrors);

	/// <summary>
	/// Runs every generator of the XAML generator assembly over the consumer (code-behind, one XAML page and, for the
	/// Apple target, the Apple stand-ins), the way an application head with the given preprocessor symbols, and
	/// optionally an iOS target framework and runtime identifier, would run them.
	/// </summary>
	private static async Task<GeneratorRun> RunAllGeneratorsAsync(string[] preprocessorSymbols, bool appleTarget)
	{
		var parseOptions = new CSharpParseOptions(LanguageVersion.Default).WithPreprocessorSymbols(preprocessorSymbols);
		var projectRoot = OperatingSystem.IsWindows() ? "C:/Project" : "//Project";

		// The Apple stand-ins (and their UIView-derived consumer type) are only part of the Apple-target consumer, so a
		// source generated for them alone would make the two runs differ.
		var trees = appleTarget
			? new[]
			{
				CSharpSyntaxTree.ParseText(CodeBehind, parseOptions, path: $"{projectRoot}/Views/TemplatedPage.xaml.cs"),
				CSharpSyntaxTree.ParseText(AppleStubs, parseOptions, path: $"{projectRoot}/AppleStubs.cs"),
			}
			: new[]
			{
				CSharpSyntaxTree.ParseText(CodeBehind, parseOptions, path: $"{projectRoot}/Views/TemplatedPage.xaml.cs"),
			};

		var references = (await ReferenceAssemblies.Net.Net100.ResolveAsync(LanguageNames.CSharp, CancellationToken.None))
			.AddRange(CodeBrixAssemblyHelper.LoadAssemblies());

		var compilation = CSharpCompilation.Create(
			"AppleDialectConsumer",
			trees,
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var globalOptions = new Dictionary<string, string>
		{
			{ "build_property.MSBuildProjectFullPath", $"{projectRoot}/AppleDialectConsumer.csproj" },
			{ "build_property.RootNamespace", "AppleDialectConsumer" },
			{ "build_property.Configuration", "Debug" },
			{ "build_property.IsCodeBrixHead", "true" },
			{ "build_property.CodeBrixForceHotReloadCodeGen", "false" },
			{ "build_property.CodeBrixEnableXamlFuzzyMatching", "false" },
		};
		if (appleTarget)
		{
			globalOptions.Add("build_property.RuntimeIdentifier", "ios-arm64");
			globalOptions.Add("build_property.TargetFramework", "net10.0-ios");
		}

		var xaml = new InMemoryAdditionalText("//Project/0/TemplatedPage.xaml", PageXaml);
		var optionsProvider = new InMemoryOptionsProvider(
			globalOptions,
			new Dictionary<string, Dictionary<string, string>>
			{
				{ xaml.Path, new Dictionary<string, string> { { "build_metadata.AdditionalFiles.SourceItemGroup", "Page" } } },
			});

		var generators = typeof(XamlCodeGenerator).Assembly.GetTypes()
			.Where(t => t is { IsAbstract: false, IsClass: true } && t.GetCustomAttributes(typeof(GeneratorAttribute), inherit: false).Length > 0)
			.OrderBy(t => t.FullName, StringComparer.Ordinal)
			.Select(t => Activator.CreateInstance(t) switch
			{
				IIncrementalGenerator incremental => incremental.AsSourceGenerator(),
				ISourceGenerator generator => generator,
				_ => throw new InvalidOperationException($"{t} is not a generator"),
			})
			.ToArray();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generators, [xaml], parseOptions, optionsProvider);
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

		var generatedTrees = output.SyntaxTrees.Skip(trees.Length).ToArray();
		var sources = driver.GetRunResult().Results
			.SelectMany(r => r.GeneratedSources.Select(s => (Key: $"{r.Generator.GetGeneratorType().Name}/{s.HintName}", Text: s.SourceText.ToString())))
			.ToDictionary(s => s.Key, s => s.Text, StringComparer.Ordinal);

		var errors = output.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.Select(d => d.ToString())
			.ToArray();

		return new GeneratorRun(output, generatedTrees, sources, errors);
	}

	private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
	{
		private readonly SourceText _text = SourceText.From(text);

		public override string Path { get; } = path;

		public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
	}

	// Nullable annotations are off for this override: the generator assembly under test also defines the
	// NotNullWhen polyfill, so the attribute the base signature carries cannot be named here unambiguously.
#nullable disable
	private sealed class InMemoryOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
	{
		public static readonly InMemoryOptions Empty = new(new Dictionary<string, string>());

		private readonly Dictionary<string, string> _values = new(values, StringComparer.OrdinalIgnoreCase);

		public override bool TryGetValue(string key, out string value)
			=> _values.TryGetValue(key, out value);
	}
#nullable restore

	private sealed class InMemoryOptionsProvider(
		IReadOnlyDictionary<string, string> globalOptions,
		IReadOnlyDictionary<string, Dictionary<string, string>> fileOptions) : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions { get; } = new InMemoryOptions(globalOptions);

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => InMemoryOptions.Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
			=> fileOptions.TryGetValue(textFile.Path, out var options) ? new InMemoryOptions(options) : InMemoryOptions.Empty;
	}
}
