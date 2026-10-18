#nullable enable

using CodeBrix.Platform.UI.Hosting;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.Core.Tests;

/// <summary>
/// Fences for the RestrictToEmbeddedFonts() host-builder call: it sets FeatureConfiguration.Font.RestrictToEmbeddedFonts,
/// returns the same builder for chaining, and - unlike the two rendering latches - leaves the property settable.
/// </summary>
public class RestrictToEmbeddedFontsBuilderTests
{
	[Fact]
	public void When_The_Builder_Restricts_To_Embedded_Fonts_Then_The_Feature_Flag_Is_Set_And_The_Builder_Is_Returned()
	{
		//Arrange
		var previous = FeatureConfiguration.Font.RestrictToEmbeddedFonts;
		FeatureConfiguration.Font.RestrictToEmbeddedFonts = false;
		var builder = CodeBrixPlatformHostBuilder.Create();

		try
		{
			//Act
			var returned = builder.RestrictToEmbeddedFonts();

			//Assert
			FeatureConfiguration.Font.RestrictToEmbeddedFonts.Should().BeTrue();
			returned.Should().BeSameAs(builder);
		}
		finally
		{
			FeatureConfiguration.Font.RestrictToEmbeddedFonts = previous;
		}
	}

	[Fact]
	public void When_The_Builder_Has_Restricted_To_Embedded_Fonts_Then_The_Property_Can_Still_Be_Turned_Off()
	{
		//Arrange
		var previous = FeatureConfiguration.Font.RestrictToEmbeddedFonts;
		CodeBrixPlatformHostBuilder.Create().RestrictToEmbeddedFonts();

		try
		{
			//Act
			FeatureConfiguration.Font.RestrictToEmbeddedFonts = false;

			//Assert
			FeatureConfiguration.Font.RestrictToEmbeddedFonts.Should().BeFalse("the builder call is not a latch");
		}
		finally
		{
			FeatureConfiguration.Font.RestrictToEmbeddedFonts = previous;
		}
	}
}
