using CodeBrix.Platform.UI.SourceGenerators.Tests.Verifiers;
using Microsoft.CodeAnalysis.Testing;

namespace CodeBrix.Platform.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.GridTests; //Was previously: Uno.UI.SourceGenerators.Tests.Windows_UI_Xaml_Controls.GridTests

using Verify = XamlSourceGeneratorVerifier;

[TestClass]
public class Given_Grid
{
	[TestMethod]
	public async Task When_Grid_Uses_Both_Syntaxes()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_Both_Syntaxes.xaml", subFolder: Path.Combine("SourceGenerators", "Platform.UI.SourceGenerators.Tests", "XamlCodeGeneratorTests", "TestCases"))
		{
			ExpectedDiagnostics =
			{
				// CodeBrix.Platform.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\Grid_Uses_Both_Syntaxes_5d0366ecb133af4c31f7d61373b58fb4.cs(115,5): error CS1912: Duplicate initialization of member 'ColumnDefinitions'
				DiagnosticResult.CompilerError("CS1912").WithSpan(Path.Combine("CodeBrix.Platform.UI.SourceGenerators","CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator","Grid_Uses_Both_Syntaxes_5d0366ecb133af4c31f7d61373b58fb4.cs"), 115, 5, 115, 22).WithArguments("ColumnDefinitions"),
				// CodeBrix.Platform.UI.SourceGenerators\Uno.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator\Grid_Uses_Both_Syntaxes_5d0366ecb133af4c31f7d61373b58fb4.cs(149,5): error CS1912: Duplicate initialization of member 'RowDefinitions'
				DiagnosticResult.CompilerError("CS1912").WithSpan(Path.Combine("CodeBrix.Platform.UI.SourceGenerators","CodeBrix.Platform.UI.SourceGenerators.XamlGenerator.XamlCodeGenerator","Grid_Uses_Both_Syntaxes_5d0366ecb133af4c31f7d61373b58fb4.cs"), 149, 5, 149, 19).WithArguments("RowDefinitions"),
			},
		};

		await Verify.AssertXamlGenerator(test);
	}

	[TestMethod]
	public async Task When_Grid_Uses_Common_Syntax()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_Common_Syntax.xaml", subFolder: Path.Combine("Platform.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));
		await Verify.AssertXamlGenerator(test);
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Assigned_ContentProperty_Syntax()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_New_Assigned_ContentProperty_Syntax.xaml", subFolder: Path.Combine("Platform.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));
		await Verify.AssertXamlGenerator(test);
	}

	[TestMethod]
	public async Task When_Grid_Uses_New_Succinct_Syntax()
	{
		var test = new TestSetup(xamlFileName: "Grid_Uses_New_Succinct_Syntax.xaml", subFolder: Path.Combine("Platform.UI.Tests", "Windows_UI_XAML_Controls", "GridTests", "Controls"));
		await Verify.AssertXamlGenerator(test);
	}
}
