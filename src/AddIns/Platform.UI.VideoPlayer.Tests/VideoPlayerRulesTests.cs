#nullable enable

using System;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;
using CodeBrix.VideoPlayback.Rendering;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.VideoPlayer.Tests;

public class VideoPlayerRulesTests
{
    [Fact]
    public void the_render_path_may_be_changed_while_nothing_is_open()
    {
        //Assert
        VideoPlayerRules.IsRenderPathChangeAllowed(false, VideoRenderPath.GpuAuto, VideoRenderPath.Cpu)
            .Should().BeTrue();
        VideoPlayerRules.IsRenderPathChangeAllowed(false, VideoRenderPath.Cpu, VideoRenderPath.GpuNoFallback)
            .Should().BeTrue();
    }

    [Fact]
    public void the_render_path_may_not_be_changed_while_a_source_is_open()
    {
        //Assert
        VideoPlayerRules.IsRenderPathChangeAllowed(true, VideoRenderPath.GpuAuto, VideoRenderPath.Cpu)
            .Should().BeFalse();
    }

    [Fact]
    public void writing_the_render_path_it_already_has_is_always_allowed()
    {
        //Arrange
        //A binding that re-writes the same value must not throw at somebody.

        //Assert
        VideoPlayerRules.IsRenderPathChangeAllowed(true, VideoRenderPath.GpuAuto, VideoRenderPath.GpuAuto)
            .Should().BeTrue();
    }

    [Fact]
    public void the_refusal_says_what_to_do_about_it()
    {
        //Act
        var message = VideoPlayerRules.RenderPathChangeRefusal("RenderPath", "Source");

        //Assert
        message.Should().Contain("RenderPath");
        message.Should().Contain("before a source is opened");
        message.Should().Contain("Source");
    }

    [Fact]
    public void a_position_inside_the_media_is_left_alone()
    {
        //Act
        var clamped = VideoPlayerRules.ClampToDuration(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10));

        //Assert
        clamped.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void a_negative_position_clamps_to_the_beginning()
    {
        //Act
        var clamped = VideoPlayerRules.ClampToDuration(TimeSpan.FromSeconds(-3), TimeSpan.FromSeconds(10));

        //Assert
        clamped.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void a_position_past_the_end_clamps_to_the_end()
    {
        //Act
        var clamped = VideoPlayerRules.ClampToDuration(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));

        //Assert
        clamped.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void an_unknown_duration_clamps_only_at_the_beginning()
    {
        //Arrange
        //A container that does not state its length is not evidence that a position is out of range.

        //Assert
        VideoPlayerRules.ClampToDuration(TimeSpan.FromHours(2), TimeSpan.Zero)
            .Should().Be(TimeSpan.FromHours(2));
        VideoPlayerRules.ClampToDuration(TimeSpan.FromSeconds(-1), TimeSpan.Zero)
            .Should().Be(TimeSpan.Zero);
    }
}
