using System;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

public sealed class OrientationPreferenceTests
{
    [Theory]
    [InlineData("landscape", ScreenOrientation.Landscape)]
    [InlineData("Landscape", ScreenOrientation.Landscape)]
    [InlineData("LANDSCAPE", ScreenOrientation.Landscape)]
    [InlineData("portrait", ScreenOrientation.Portrait)]
    [InlineData("PoRtRaIt", ScreenOrientation.Portrait)]
    public void Parse_accepts_either_orientation_in_any_case(string value, ScreenOrientation expected)
        => OrientationPreference.Parse(value, "source").Should().Be(expected);

    [Theory]
    [InlineData("sideways")]
    [InlineData(" portrait")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_rejects_other_values_naming_the_source(string? value)
    {
        //Act
        Action parse = () => OrientationPreference.Parse(value, "MySetting");

        //Assert
        parse.Should().Throw<ArgumentException>().WithMessage("MySetting must be Landscape or Portrait*");
    }
}
