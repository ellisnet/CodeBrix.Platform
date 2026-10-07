using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The option-matching rules behind Locator.SelectOptionAsync.
public sealed class SelectorOptionsTests
{
    private static readonly SelectorOption Option = new(Index: 2, Label: "Row 3", Value: "r3", Selected: false);

    [Theory]
    [InlineData("r3", null, null, true)]
    [InlineData(null, "Row 3", null, true)]
    [InlineData(null, "  Row   3 ", null, true)]
    [InlineData(null, null, 2, true)]
    [InlineData("r3", "Row 3", 2, true)]
    [InlineData("r3", "Row 4", null, false)]
    [InlineData(null, "row 3", null, false)]
    [InlineData(null, "Row", null, false)]
    [InlineData(null, null, 3, false)]
    public void Every_property_that_is_set_must_match(string? value, string? label, int? index, bool matches)
        => SelectorOptions.Matches(Option, new SelectOptionValue { Value = value, Label = label, Index = index }).Should().Be(matches);

    [Fact]
    public void Member_reads_a_dotted_property_path()
    {
        //Arrange
        var item = new { Name = "Paris", Country = new { Code = "FR" } };

        //Act
        var code = SelectorOptions.Member(item, "Country.Code");
        var missing = SelectorOptions.Member(item, "Country.Missing");

        //Assert
        code.Should().Be("FR");
        missing.Should().BeNull();
        SelectorOptions.Member(null, "Name").Should().BeNull();
    }

    [Fact]
    public void Describe_names_only_the_properties_that_are_set()
        => SelectorOptions.Describe(new SelectOptionValue { Label = "Row 3", Index = 2 }).Should().Be("{Label 'Row 3', Index 2}");
}
