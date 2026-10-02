using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The text rules behind every name/text locator and text/value assertion.
public sealed class VisualTreeTests
{
    [Theory]
    [InlineData("Send", "Send")]
    [InlineData("  Send   now \t\n", "Send now")]
    [InlineData("a\r\nb", "a b")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_collapses_and_trims_whitespace(string? value, string expected)
        => VisualTree.Normalize(value).Should().Be(expected);

    [Theory]
    [InlineData("one\r\ntwo", "one\ntwo")]
    [InlineData("one\rtwo", "one\ntwo")]
    [InlineData("one\ntwo", "one\ntwo")]
    [InlineData("a\r\n\rb", "a\n\nb")]
    [InlineData(null, "")]
    public void InputText_exposes_lf_line_endings(string? value, string expected)
        => VisualTree.InputText(value).Should().Be(expected);

    [Theory]
    [InlineData("Choose assets folder", "assets", true)]
    [InlineData("Choose assets folder", "ASSETS", true)]
    [InlineData("Choose  assets\nfolder", "assets folder", true)]
    [InlineData("Choose assets folder", "images", false)]
    [InlineData("Anything", "", true)]
    public void Matches_inexact_is_a_case_insensitive_normalized_substring(string actual, string expected, bool matches)
        => VisualTree.Matches(actual, expected, exact: false).Should().Be(matches);

    [Theory]
    [InlineData("Send", "Send", true)]
    [InlineData("  Send ", "Send", true)]
    [InlineData("Send", "send", false)]
    [InlineData("Send now", "Send", false)]
    [InlineData(null, "", true)]
    public void Matches_exact_is_a_case_sensitive_normalized_equality(string? actual, string expected, bool matches)
        => VisualTree.Matches(actual, expected, exact: true).Should().Be(matches);
}
