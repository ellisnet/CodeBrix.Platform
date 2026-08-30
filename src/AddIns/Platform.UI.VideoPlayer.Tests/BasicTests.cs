#nullable enable

using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.VideoPlayer.Tests;

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
