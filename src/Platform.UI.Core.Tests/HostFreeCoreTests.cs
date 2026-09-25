#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// The host-free Core proof (plan gate G6): only the Core assemblies are loaded, a Page with a binding and a style
/// is navigated to in a Frame, and no SkiaSharp (or HarfBuzz) assembly is loaded into the process.
/// </summary>
public class HostFreeCoreTests
{
	[Fact]
	public void When_A_Frame_Navigates_To_A_Page_With_Bindings_And_Styles_Then_No_Skia_Assembly_Is_Loaded()
	{
		//Arrange
		var frame = new Frame();
		Exception? navigationError = null;
		frame.NavigationFailed += (_, e) =>
		{
			navigationError = e.Exception;
			e.Handled = true;
		};

		//Act
		var navigated = frame.Navigate(typeof(TestPage), "Hello from Core");

		//Assert
		navigationError.Should().BeNull();
		navigated.Should().BeTrue();
		var page = frame.Content.Should().BeOfType<TestPage>().Subject;
		page.Title.Text.Should().Be("Hello from Core");
		page.Title.FontSize.Should().Be(TestPage.StyledFontSize);
		page.Title.Tag.Should().Be("styled");

		var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name ?? "").ToArray();
		loaded.Should().NotContain(n => n.StartsWith("SkiaSharp", StringComparison.Ordinal) || n.StartsWith("HarfBuzzSharp", StringComparison.Ordinal));
		loaded.Should().Contain("CodeBrix.Platform.UI.Core");
		loaded.Should().NotContain(new[] { "CodeBrix.Platform.UI", "CodeBrix.Platform.UI.Composition", "CodeBrix.Platform", "CodeBrix.Platform.UI.Dispatching" });
	}

	[Fact]
	public void When_A_Type_Is_Named_With_Its_PreSplit_Assembly_Then_ApiInformation_Still_Finds_It()
	{
		//Arrange
		// Application registers the framework assemblies with ApiInformation when it starts; there is no
		// Application in this process, so register the UI assembly the same way.
		Windows.Foundation.Metadata.ApiInformation.RegisterAssembly(typeof(Frame).Assembly);

		//Act + Assert
		Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Controls.Frame, CodeBrix.Platform.UI").Should().BeTrue();
		Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Controls.Frame, CodeBrix.Platform.UI.Core").Should().BeTrue();
	}

	/// <summary>A page whose text is bound to the navigation parameter and styled by an implicit page style.</summary>
	private sealed partial class TestPage : Page
	{
		internal const double StyledFontSize = 31;

		public TestPage()
		{
			var style = new Style(typeof(TextBlock));
			style.Setters.Add(new Setter(TextBlock.FontSizeProperty, StyledFontSize));
			style.Setters.Add(new Setter(FrameworkElement.TagProperty, "styled"));
			Resources[typeof(TextBlock)] = style;

			Title = new TextBlock { Style = style };
			Title.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(Model.Text)) });
			Content = Title;
		}

		internal TextBlock Title { get; }

		protected internal override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			DataContext = new Model((string)e.Parameter);
		}
	}

	/// <summary>The binding source.</summary>
	/// <param name="Text">The text shown by the page.</param>
	public sealed record Model(string Text);
}
