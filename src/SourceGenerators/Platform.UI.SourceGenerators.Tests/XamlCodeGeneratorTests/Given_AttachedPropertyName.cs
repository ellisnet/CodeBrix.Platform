using Microsoft.CodeAnalysis.Testing;
using CodeBrix.Platform.UI.SourceGenerators.Tests.Verifiers;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.XamlGenerator.AttachedPropertyName;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// An attached property whose member is <c>Name</c> (<c>AutomationProperties.Name</c>, or any <c>Owner.Name</c>) is never
/// the element's own name. Only <c>x:Name</c>, or a plain <c>Name</c> attribute that belongs to the element's type, names
/// the element (backs the field, registers it in the name scope, lists it for a deferred <c>x:Load</c> subtree). An owner
/// type that does not resolve is reported as a XAML error at the attribute instead of being turned into C# built from the
/// attribute's value.
/// </summary>
[TestClass]
public class Given_AttachedPropertyName
{
	private const string CodeBehind = """
		using Microsoft.UI.Xaml.Controls;

		namespace TestRepro
		{
			public sealed partial class MainPage : Page
			{
				public MainPage()
				{
					this.InitializeComponent();
				}
			}
		}
		""";

	[TestMethod]
	public async Task When_Owner_Unresolved_Then_Error_Not_Field()
	{
		// The application template's page shape: the default xmlns is the Controls CLR namespace, which does not contain
		// Microsoft.UI.Xaml.Automation, so AutomationProperties does not resolve. Before the fix the generator took the
		// attribute's VALUE as the element's name and emitted a field "Text to keep" (CS0246 'to', CS0102 '_Text', CS1527).
		var xaml = """
			<Page x:Class="TestRepro.MainPage"
				xmlns="clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform.UI"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<StackPanel>
					<TextBox x:Name="Entry" AutomationProperties.Name="Text to keep" />
				</StackPanel>
			</Page>
			""";

		var test = new Verify.Test(new XamlFile("MainPage.xaml", xaml))
		{
			TestState = { Sources = { CodeBehind } },
		}.AddGeneratedSources();

		test.ExpectedDiagnostics.AddRange(
		[
			DiagnosticResult.CompilerError("UXAML0001").WithSpan("//Project/0/MainPage.xaml", 5, 4, 5, 4)
				.WithArguments("Unable to find the owner type 'AutomationProperties' of attached property 'AutomationProperties.Name', value is 'Text to keep'; declare an xmlns prefix for the namespace that declares 'AutomationProperties' and qualify the attribute with it (for AutomationProperties: xmlns:auto=\"clr-namespace:Microsoft.UI.Xaml.Automation;assembly=CodeBrix.Platform.UI\" and auto:AutomationProperties.Name)"),
		]);

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Owner_Qualified_As_Hinted_Then_It_Resolves()
	{
		// The same template page shape with the xmlns the error message suggests: the attached property resolves, the
		// element keeps its x:Name, and no diagnostic is reported.
		var xaml = """
			<Page x:Class="TestRepro.MainPage"
				xmlns="clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform.UI"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				xmlns:auto="clr-namespace:Microsoft.UI.Xaml.Automation;assembly=CodeBrix.Platform.UI">
				<StackPanel>
					<TextBox x:Name="Entry" auto:AutomationProperties.Name="Text to keep" />
				</StackPanel>
			</Page>
			""";

		var test = new Verify.Test(new XamlFile("MainPage.xaml", xaml))
		{
			TestState = { Sources = { CodeBehind } },
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Owner_Resolved_Then_Element_Keeps_Its_Own_Name()
	{
		// Standard presentation xmlns: AutomationProperties resolves. The attached Name sits next to x:Name inside an
		// x:Load="False" subtree (the deferred subtree's name list is built from its elements' names), with an ElementName
		// binding to the element. Fences that the element keeps its own name everywhere (backing field, name list, binding)
		// and that the attached property is still set; the generated output is unchanged by the fix.
		var xaml = """
			<Page x:Class="TestRepro.MainPage"
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<StackPanel>
					<StackPanel x:Name="Deferred" x:Load="False">
						<TextBox AutomationProperties.Name="Text to keep" x:Name="Entry" />
						<TextBlock Text="{Binding Text, ElementName=Entry}" />
					</StackPanel>
				</StackPanel>
			</Page>
			""";

		var test = new Verify.Test(new XamlFile("MainPage.xaml", xaml))
		{
			TestState = { Sources = { CodeBehind } },
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
