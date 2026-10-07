using CodeBrix.Platform.PlayTest.Hosting;
using SilverAssertions;
using Windows.Foundation;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// Element cropping for Locator.ScreenshotAsync.
public sealed class ScreenshotsTests
{
    [Theory]
    [InlineData(10, 20, 30, 40, 10, 20, 30, 40)]
    [InlineData(10.5, 20.25, 30, 40, 10, 20, 31, 41)]
    [InlineData(-5, -5, 20, 20, 0, 0, 15, 15)]
    [InlineData(1900, 1070, 50, 50, 1900, 1070, 20, 10)]
    [InlineData(2000, 10, 50, 50, 1920, 10, 0, 50)]
    public void PixelRegion_covers_the_bounds_in_whole_pixels_clipped_to_the_screen(
        double x, double y, double width, double height, int left, int top, int regionWidth, int regionHeight)
        => Screenshots.PixelRegion(new Rect(x, y, width, height), 1920, 1080).Should().Be((left, top, regionWidth, regionHeight));

    [Fact]
    public void Crop_copies_the_region_rows_of_a_frame()
    {
        //Arrange
        var screen = new VirtualScreen(ScreenOrientation.Landscape);
        var pixels = new byte[screen.Width * screen.Height * 4];
        pixels[(5 * screen.Width + 7) * 4] = 42;
        pixels[(6 * screen.Width + 8) * 4 + 3] = 99;
        var frame = new VirtualFrame(screen, pixels);

        //Act
        var region = Screenshots.Crop(frame, (7, 5, 2, 2));

        //Assert
        region.Should().HaveCount(16);
        region[0].Should().Be(42);
        region[(1 * 2 + 1) * 4 + 3].Should().Be(99);
        Screenshots.Crop(frame, (0, 0, screen.Width, screen.Height)).Should().BeSameAs(pixels);
    }
}
