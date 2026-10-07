using System;
using System.Threading.Tasks;
using SilverAssertions;
using Windows.System;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// The head's recording launcher: it answers Launcher requests and never starts a process.
public sealed class PlayTestLauncherTests
{
    [Fact]
    public async Task Launches_are_recorded_in_order_and_succeed_by_default()
    {
        //Arrange
        var launcher = new PlayTestLauncher();
        var extension = new PlayTestLauncher.Extension(launcher);
        var site = new Uri("https://example.com/help");
        var file = new Uri(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "report.pdf"));

        //Act
        var first = await extension.LaunchUriAsync(site);
        var second = await extension.LaunchUriAsync(file);

        //Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
        launcher.LaunchedUris.Should().Equal(site, file);
        launcher.QueriedUris.Should().BeEmpty();
    }

    [Fact]
    public async Task The_configured_results_are_returned_and_queries_are_recorded()
    {
        //Arrange
        var launcher = new PlayTestLauncher { LaunchResult = false, QueryUriSupportResult = LaunchQuerySupportStatus.NotSupported };
        var extension = new PlayTestLauncher.Extension(launcher);
        var uri = new Uri("mailto:someone@example.com");

        //Act
        var launched = await extension.LaunchUriAsync(uri);
        var support = await extension.QueryUriSupportAsync(uri, LaunchQuerySupportType.Uri);

        //Assert
        launched.Should().BeFalse();
        support.Should().Be(LaunchQuerySupportStatus.NotSupported);
        launcher.QueriedUris.Should().Equal(uri);
    }

    [Fact]
    public async Task Clear_resets_the_history_and_the_results()
    {
        //Arrange
        var launcher = new PlayTestLauncher { LaunchResult = false, QueryUriSupportResult = LaunchQuerySupportStatus.NotSupported };
        var extension = new PlayTestLauncher.Extension(launcher);
        await extension.LaunchUriAsync(new Uri("https://example.com"));
        await extension.QueryUriSupportAsync(new Uri("https://example.com"), LaunchQuerySupportType.Uri);

        //Act
        launcher.Clear();

        //Assert
        launcher.LaunchedUris.Should().BeEmpty();
        launcher.QueriedUris.Should().BeEmpty();
        launcher.LaunchResult.Should().BeTrue();
        launcher.QueryUriSupportResult.Should().Be(LaunchQuerySupportStatus.Available);
    }
}
