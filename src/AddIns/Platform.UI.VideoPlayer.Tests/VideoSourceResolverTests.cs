#nullable enable

using System;
using System.IO;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.VideoPlayer.Tests;

public class VideoSourceResolverTests
{
    [Fact]
    public void a_bare_path_resolves_to_itself()
    {
        //Act
        var (pathOrUrl, stream) = VideoSourceResolver.Resolve("/home/someone/clips/holiday.cbv");

        //Assert
        pathOrUrl.Should().Be("/home/someone/clips/holiday.cbv");
        stream.Should().BeNull();
    }

    [Fact]
    public void a_windows_path_resolves_to_itself()
    {
        //Act
        var (pathOrUrl, stream) = VideoSourceResolver.Resolve(@"C:\Clips\holiday.webm");

        //Assert
        pathOrUrl.Should().Be(@"C:\Clips\holiday.webm");
        stream.Should().BeNull();
    }

    [Fact]
    public void a_file_uri_resolves_to_its_local_path()
    {
        //Act
        var (pathOrUrl, stream) = VideoSourceResolver.Resolve("file:///home/someone/clips/holiday.cbv");

        //Assert
        pathOrUrl.Should().Be("/home/someone/clips/holiday.cbv");
        stream.Should().BeNull();
    }

    [Fact]
    public void an_http_address_is_passed_through_untouched()
    {
        //Arrange
        const string address = "http://example.com/media/clip.webm?token=abc";

        //Act
        var (pathOrUrl, stream) = VideoSourceResolver.Resolve(address);

        //Assert
        //The session fetches an address itself, so it must arrive exactly as it was written -
        //  query string, case and all.
        pathOrUrl.Should().Be(address);
        stream.Should().BeNull();
    }

    [Fact]
    public void an_https_address_is_passed_through_untouched()
    {
        //Arrange
        const string address = "https://Example.COM/Media/Clip.cbv";

        //Act
        var (pathOrUrl, stream) = VideoSourceResolver.Resolve(address);

        //Assert
        pathOrUrl.Should().Be(address);
        stream.Should().BeNull();
    }

    [Fact]
    public void an_embedded_resource_resolves_to_an_open_stream()
    {
        //Arrange
        var assemblyName = typeof(VideoSourceResolverTests).Assembly.GetName().Name;

        //Act
        var (pathOrUrl, stream) = VideoSourceResolver.Resolve(
            $"embedded://{assemblyName}/CodeBrix.Platform.UI.VideoPlayer.Tests.Assets.resolver_probe.txt");

        //Assert
        pathOrUrl.Should().BeNull();
        stream.Should().NotBeNull();

        using var open = stream!;
        using var reader = new StreamReader(open);
        reader.ReadToEnd().Should().Contain("probe file");
    }

    [Fact]
    public void the_assembly_placeholder_is_replaced_with_the_resolved_assemblys_name()
    {
        //Arrange
        //"(assembly)" stands for the ASSEMBLY name, which is not always the root namespace the
        //  resource names were built from - as in this very suite, whose assembly is
        //  ...VideoPlayer.Unit.Tests while its resources are named ...VideoPlayer.Tests.*. The
        //  substitution is what is under test, so the failure message is the evidence.
        var assemblyName = typeof(VideoSourceResolverTests).Assembly.GetName().Name;

        //Act
        var act = () => VideoSourceResolver.Resolve(
            $"embedded://{assemblyName}/(assembly).Assets.resolver_probe.txt");

        //Assert
        act.Should().Throw<FileNotFoundException>().WithMessage($"*{assemblyName}.Assets.resolver_probe.txt*");
    }

    [Fact]
    public void a_dot_assembly_name_means_the_running_applications_own_assembly()
    {
        //Arrange
        //There is no Application in a unit-test process, so the resolver cannot reach one - which
        //  is itself the behaviour worth pinning: "." is resolved through Application.Current and
        //  belongs to a running application, never to a library.

        //Act
        var act = () => VideoSourceResolver.Resolve("embedded://./Some.Resource.txt");

        //Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void a_missing_embedded_resource_says_which_one_and_where()
    {
        //Arrange
        var assemblyName = typeof(VideoSourceResolverTests).Assembly.GetName().Name;

        //Act
        var act = () => VideoSourceResolver.Resolve($"embedded://{assemblyName}/nothing.here.txt");

        //Assert
        act.Should().Throw<FileNotFoundException>().WithMessage("*nothing.here.txt*");
    }

    [Fact]
    public void an_empty_source_is_refused()
    {
        //Act
        var act = () => VideoSourceResolver.Resolve("   ");

        //Assert
        act.Should().Throw<ArgumentException>();
    }
}
