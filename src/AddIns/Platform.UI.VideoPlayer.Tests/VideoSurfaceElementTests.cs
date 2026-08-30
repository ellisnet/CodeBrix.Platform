#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using CodeBrix.Platform.UI.VideoPlayer.Skia.Internal;
using SilverAssertions;
using Xunit;

namespace CodeBrix.Platform.UI.VideoPlayer.Tests;

/// <summary>
/// The letterbox geometry: pure arithmetic, so it is exercised without a window.
/// </summary>
public class VideoSurfaceElementTests
{
    private static readonly Size Area = new(800, 600);

    [Fact]
    public void none_keeps_the_pictures_own_pixel_size_centred()
    {
        //Act
        var rect = VideoSurfaceElement.ComputeDestinationRect(320, 240, Area, Stretch.None);

        //Assert
        rect.Width.Should().Be(320f);
        rect.Height.Should().Be(240f);
        rect.Left.Should().Be(240f);
        rect.Top.Should().Be(180f);
    }

    [Fact]
    public void fill_takes_the_whole_area_and_distorts()
    {
        //Act
        var rect = VideoSurfaceElement.ComputeDestinationRect(320, 240, Area, Stretch.Fill);

        //Assert
        rect.Left.Should().Be(0f);
        rect.Top.Should().Be(0f);
        rect.Width.Should().Be(800f);
        rect.Height.Should().Be(600f);
    }

    [Fact]
    public void uniform_fits_a_wide_picture_with_bars_above_and_below()
    {
        //Arrange
        //16:9 inside a 4:3 area: the width binds.

        //Act
        var rect = VideoSurfaceElement.ComputeDestinationRect(1280, 720, Area, Stretch.Uniform);

        //Assert
        rect.Width.Should().Be(800f);
        rect.Height.Should().Be(450f);
        rect.Left.Should().Be(0f);
        rect.Top.Should().Be(75f);
    }

    [Fact]
    public void uniform_fits_a_tall_picture_with_bars_either_side()
    {
        //Arrange
        //A portrait phone recording inside a landscape window: the height binds.

        //Act
        var rect = VideoSurfaceElement.ComputeDestinationRect(720, 1280, Area, Stretch.Uniform);

        //Assert
        rect.Height.Should().Be(600f);
        rect.Width.Should().Be(337.5f);
        rect.Top.Should().Be(0f);
        rect.Left.Should().Be(231.25f);
    }

    [Fact]
    public void uniform_to_fill_covers_the_area_and_overflows_for_the_caller_to_clip()
    {
        //Act
        var rect = VideoSurfaceElement.ComputeDestinationRect(1280, 720, Area, Stretch.UniformToFill);

        //Assert
        rect.Height.Should().Be(600f);
        rect.Width.Should().BeApproximately(1066.667f, 0.01f);
        rect.Top.Should().Be(0f);
        rect.Left.Should().BeApproximately(-133.333f, 0.01f);
    }

    [Fact]
    public void uniform_is_the_default_for_anything_unrecognized()
    {
        //Act
        var uniform = VideoSurfaceElement.ComputeDestinationRect(1280, 720, Area, Stretch.Uniform);
        var fallback = VideoSurfaceElement.ComputeDestinationRect(1280, 720, Area, (Stretch)99);

        //Assert
        fallback.Should().Be(uniform);
    }

    [Fact]
    public void a_picture_with_no_size_yet_takes_the_whole_area()
    {
        //Arrange
        //Before the first frame is decoded there is nothing to letterbox.

        //Act
        var rect = VideoSurfaceElement.ComputeDestinationRect(0, 0, Area, Stretch.Uniform);

        //Assert
        rect.Left.Should().Be(0f);
        rect.Top.Should().Be(0f);
        rect.Width.Should().Be(800f);
        rect.Height.Should().Be(600f);
    }
}
