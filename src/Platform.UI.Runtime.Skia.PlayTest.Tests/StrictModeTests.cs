using System;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// Retrying actions and assertions recognise a strict-mode violation and keep waiting; every
// other PlayTestException still ends the retry at once.
public sealed class StrictModeTests
{
    [Fact]
    public void A_strict_mode_violation_names_the_locator_and_the_match_count()
    {
        //Act
        var error = Locator.StrictModeViolation("GetByText(\"Save\")", 3);

        //Assert
        error.Message.Should().Be("Strict mode violation: GetByText(\"Save\") resolved to 3 elements. Use a unique name, test ID, or Nth().");
        Locator.IsStrictModeViolation(error).Should().BeTrue();
    }

    [Fact]
    public void Other_failures_are_not_strict_mode_violations()
    {
        Locator.IsStrictModeViolation(new PlayTestException("Strict mode violation: looks similar")).Should().BeFalse();
        Locator.IsStrictModeViolation(new InvalidOperationException("other")).Should().BeFalse();
    }
}
