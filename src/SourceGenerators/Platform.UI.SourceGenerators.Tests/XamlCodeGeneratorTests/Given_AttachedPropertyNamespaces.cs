using CodeBrix.Platform.UI.SourceGenerators.Tests.Verifiers;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.XamlGenerator.AttachedPropertyNamespaces;

using Verify = XamlSourceGeneratorVerifier;

/// <summary>
/// An attached property reached through an xmlns <b>prefix</b> must resolve identically whether the
/// prefix is declared with the <c>using:</c> or the <c>clr-namespace:</c> form. Visual Studio 2026
/// does not recognise the <c>using:</c> form, so app XAML is moving to <c>clr-namespace:</c>.
/// </summary>
[TestClass]
public class Given_AttachedPropertyNamespaces
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

	private static string Xaml(string flexNamespace) => $$"""
		<Page x:Class="TestRepro.MainPage"
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			xmlns:c="{{flexNamespace}}">
			<Grid>
				<TextBlock c:Grid.Row="1" Text="Attached through a prefix" />
			</Grid>
		</Page>
		""";

	[TestMethod]
	public async Task When_Prefix_Declared_With_Using_Form()
	{
		var test = new Verify.Test(new XamlFile("MainPage.xaml", Xaml("using:Microsoft.UI.Xaml.Controls")))
		{
			TestState = { Sources = { CodeBehind } },
		}.AddGeneratedSources();

		await test.RunAsync();
	}

	[TestMethod]
	public async Task When_Prefix_Declared_With_ClrNamespace_Form()
	{
		var test = new Verify.Test(new XamlFile("MainPage.xaml",
			Xaml("clr-namespace:Microsoft.UI.Xaml.Controls;assembly=CodeBrix.Platform")))
		{
			TestState = { Sources = { CodeBehind } },
		}.AddGeneratedSources();

		await test.RunAsync();
	}
}
