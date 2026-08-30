#nullable enable

using CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.VideoPlayer.Tests;

public class VideoFailureExplanationTests
{
    //The engine's own wording, verbatim: these are the two messages the explanation exists to
    //  recognize, and a test that made them up would not prove anything.
    private const string MissingAv1Decoder =
        "video codec 'av01' has no registered decoder \u2014 reference CodeBrix.VideoPlayback.Dav1d and call " +
        "CodeBrixVideoPlaybackDav1d.Register()";

    private const string MissingOpusDecoder =
        "audio codec 'opus' has no registered decoder \u2014 reference CodeBrix.Audio.Opus and call " +
        "CodeBrixAudioOpus.Register()";

    [Fact]
    public void a_missing_av1_decoder_names_the_dav1d_package()
    {
        //Act
        var amended = VideoFailureExplanation.Amend("The video source 'clip.cbv' could not be opened.", MissingAv1Decoder);

        //Assert
        amended.Should().StartWith("The video source 'clip.cbv' could not be opened.");
        amended.Should().Contain("CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever");
        amended.Should().Contain("CodeBrixVideoPlaybackDav1d.Register()");
    }

    [Fact]
    public void a_missing_opus_decoder_names_the_opus_package()
    {
        //Act
        var amended = VideoFailureExplanation.Amend("The video source 'clip.cbv' could not be opened.", MissingOpusDecoder);

        //Assert
        amended.Should().Contain("CodeBrix.Audio.Opus.BsdLicenseForever");
        amended.Should().Contain("CodeBrixAudioOpus.Register()");
    }

    [Fact]
    public void an_unrelated_failure_is_left_exactly_as_it_was()
    {
        //Arrange
        const string message = "The video source 'missing.cbv' could not be opened.";

        //Act
        var amended = VideoFailureExplanation.Amend(message, "Could not find file '/tmp/missing.cbv'.");

        //Assert
        amended.Should().Be(message);
    }

    [Fact]
    public void a_failure_with_no_engine_message_is_left_exactly_as_it_was()
    {
        //Arrange
        const string message = "The video source 'stream' could not be opened.";

        //Assert
        VideoFailureExplanation.Amend(message, null).Should().Be(message);
        VideoFailureExplanation.Amend(message, "").Should().Be(message);
    }

    [Fact]
    public void the_video_decoder_explanation_wins_when_a_message_could_match_both()
    {
        //Arrange
        //Nothing plays at all without the video decoder, so that is the advice worth giving first.
        var both = MissingAv1Decoder + " / " + MissingOpusDecoder;

        //Act
        var amended = VideoFailureExplanation.Amend("Failed.", both);

        //Assert
        amended.Should().Contain("CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever");
        amended.Should().NotContain("CodeBrix.Audio.Opus.BsdLicenseForever");
    }
}
