#nullable enable

using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.TerminalView.Tests;

//was previously: Lily.Shell.TerminalView.Tests.BasicTests, namespace changed.

public class BasicTests
{
    [Fact]
    public void can_run_tests()
    {
        //Arrange
        var isRunning = true;

        //Assert
        isRunning.Should().Be(true);
    }
}
