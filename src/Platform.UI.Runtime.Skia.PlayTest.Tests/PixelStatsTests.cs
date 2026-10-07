using System;
using SilverAssertions;
using Windows.UI;
using Xunit;

namespace CodeBrix.Platform.PlayTest.Tests;

// PixelStats on synthetic regions: every measurement, from bytes made in the test itself.
public sealed class PixelStatsTests
{
    private static readonly Color Grey = Color.FromArgb(255, 26, 26, 26);
    private static readonly Color Red = Color.FromArgb(255, 255, 0, 0);
    private static readonly Color White = Color.FromArgb(255, 255, 255, 255);
    private static readonly Color Black = Color.FromArgb(255, 0, 0, 0);

    private static PixelStats Fill(int width, int height, Func<int, int, Color> color)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var c = color(x, y);
                var i = (y * width + x) * 4;
                pixels[i] = c.B;
                pixels[i + 1] = c.G;
                pixels[i + 2] = c.R;
                pixels[i + 3] = c.A;
            }
        return PixelStats.FromBgra(pixels, width, height);
    }

    // A 10x10 grey region with a 3x2 red block whose top-left pixel is (4, 5).
    private static PixelStats Block() => Fill(10, 10, (x, y) => x is >= 4 and < 7 && y is >= 5 and < 7 ? Red : Grey);

    [Fact]
    public void FromPng_reads_the_bytes_a_screenshot_returns()
    {
        //Arrange - the renderer's premultiplied BGRA, encoded exactly as ScreenshotAsync encodes it
        byte[] frame = [0, 0, 255, 255, /**/ 255, 0, 0, 255, /**/ 0, 255, 0, 255, /**/ 0, 0, 0, 0];
        var png = Screenshots.Encode(frame, 2, 2);

        //Act
        var stats = PixelStats.FromPng(png);

        //Assert
        stats.Width.Should().Be(2);
        stats.Height.Should().Be(2);
        stats.GetPixel(0, 0).Should().Be(Red);
        stats.GetPixel(1, 0).Should().Be(Color.FromArgb(255, 0, 0, 255));
        stats.GetPixel(0, 1).Should().Be(Color.FromArgb(255, 0, 255, 0));
        stats.GetPixel(1, 1).A.Should().Be(0);
    }

    [Fact]
    public void FromPng_rejects_bytes_that_are_not_an_image()
    {
        var read = () => PixelStats.FromPng([1, 2, 3, 4]);
        read.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPixel_and_Region_reject_positions_outside_the_region()
    {
        var stats = Block();
        ((Action)(() => stats.GetPixel(10, 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => stats.GetPixel(0, -1))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => stats.Region(5, 5, 6, 1))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Coverage_counts_the_pixels_within_the_tolerance_of_a_colour()
    {
        //Arrange - six exact reds and one red off by 6 in green
        var stats = Fill(10, 10, (x, y) => x is >= 4 and < 7 && y is >= 5 and < 7 ? Red : x == 0 && y == 0 ? Color.FromArgb(255, 255, 6, 0) : Grey);

        //Assert
        stats.Coverage(Red, tolerance: 0).Should().BeApproximately(0.06, 1e-9);
        stats.Coverage(Red).Should().BeApproximately(0.07, 1e-9);
        stats.Coverage(Grey).Should().BeApproximately(0.93, 1e-9);
        stats.Coverage(White).Should().Be(0);
    }

    [Fact]
    public void Distinct_colours_tell_uniform_and_blank_regions_apart()
    {
        var black = Fill(4, 4, (_, _) => Black);
        var transparent = Fill(4, 4, (_, _) => Color.FromArgb(0, 0, 0, 0));
        var magenta = Fill(4, 4, (_, _) => Color.FromArgb(255, 255, 0, 255));
        var mixed = Fill(4, 4, (x, _) => x < 2 ? Black : Color.FromArgb(0, 0, 0, 0));

        black.IsUniform.Should().BeTrue();
        black.IsBlank.Should().BeTrue();
        transparent.IsBlank.Should().BeTrue();
        magenta.IsUniform.Should().BeTrue();
        magenta.IsBlank.Should().BeFalse();
        mixed.IsUniform.Should().BeFalse();
        mixed.IsBlank.Should().BeTrue("black and transparent are both what an undrawn canvas shows");
        Block().DistinctColorCount.Should().Be(2);
        Block().IsUniform.Should().BeFalse();
        Block().IsBlank.Should().BeFalse();
    }

    [Fact]
    public void Bounds_and_centroid_locate_everything_that_is_not_background()
    {
        var stats = Block();

        stats.Bounds(Grey).Should().Be(new PixelBounds(4, 5, 3, 2));
        var centre = stats.Centroid(Grey);
        centre.Should().NotBeNull();
        centre!.Value.X.Should().BeApproximately(5.5, 1e-9);
        centre.Value.Y.Should().BeApproximately(6.0, 1e-9);
    }

    [Fact]
    public void Bounds_and_centroid_are_null_when_everything_is_background()
    {
        var stats = Fill(5, 5, (_, _) => Grey);

        stats.Bounds(Grey).Should().BeNull();
        stats.Centroid(Grey).Should().BeNull();
        stats.Bounds(Color.FromArgb(255, 30, 30, 30)).Should().BeNull("within the default tolerance");
    }

    [Fact]
    public void Halves_compare_the_mean_luminance_of_each_side()
    {
        //Arrange - white on the left, black on the right, and a red middle column in neither half
        var stats = Fill(5, 2, (x, _) => x < 2 ? White : x == 2 ? Red : Black);

        //Act
        var left = stats.Half(PixelHalf.Left);
        var right = stats.Half(PixelHalf.Right);

        //Assert
        left.Width.Should().Be(2);
        right.Width.Should().Be(2);
        left.MeanLuminance().Should().BeApproximately(255, 1e-9);
        right.MeanLuminance().Should().BeApproximately(0, 1e-9);
        stats.Half(PixelHalf.Top).Height.Should().Be(1);
        stats.Half(PixelHalf.Bottom).GetPixel(0, 0).Should().Be(White);
    }

    [Fact]
    public void Mean_luminance_can_leave_the_background_out()
    {
        //Arrange - one white pixel on black
        var stats = Fill(4, 1, (x, _) => x == 0 ? White : Black);

        //Assert
        stats.MeanLuminance().Should().BeApproximately(255 / 4.0, 1e-9);
        stats.MeanLuminance(Black).Should().BeApproximately(255, 1e-9);
        Fill(2, 2, (_, _) => Black).MeanLuminance(Black).Should().Be(double.NaN);
        // Rec. 709 weights: pure red is 0.2126 of white.
        Fill(1, 1, (_, _) => Red).MeanLuminance().Should().BeApproximately(0.2126 * 255, 1e-9);
    }

    [Fact]
    public void Luminance_variance_is_zero_for_a_flat_fill()
    {
        Fill(3, 3, (_, _) => Grey).LuminanceVariance().Should().Be(0);
        // Half black, half white: every pixel is 127.5 from the mean.
        Fill(2, 1, (x, _) => x == 0 ? Black : White).LuminanceVariance().Should().BeApproximately(127.5 * 127.5, 1e-6);
    }

    [Fact]
    public void Mirror_symmetry_scores_how_much_each_side_mirrors_the_other()
    {
        var symmetric = Fill(6, 4, (x, y) => x is 0 or 5 ? Red : Grey);
        var lopsided = Fill(6, 4, (x, _) => x == 0 ? Red : Grey);

        symmetric.MirrorSymmetry(PixelAxis.Vertical).Should().Be(1);
        symmetric.MirrorSymmetry(PixelAxis.Horizontal).Should().Be(1);
        // The red column and its mirror image disagree: 8 of 24 pixels.
        lopsided.MirrorSymmetry(PixelAxis.Vertical).Should().BeApproximately(16 / 24.0, 1e-9);
        lopsided.MirrorSymmetry(PixelAxis.Horizontal).Should().Be(1);
    }

    [Fact]
    public void Difference_measures_what_changed_between_two_captures()
    {
        //Arrange
        var before = Block();
        var after = Fill(10, 10, (x, y) => x == 9 && y == 9 ? Color.FromArgb(255, 76, 26, 26) : x is >= 4 and < 7 && y is >= 5 and < 7 ? Red : Grey);

        //Act
        var same = before.Difference(Block());
        var changed = before.Difference(after);
        var tolerated = before.Difference(after, tolerance: 50);

        //Assert
        same.Should().Be(new PixelDifference(0, 0));
        changed.ChangedFraction.Should().BeApproximately(0.01, 1e-9);
        changed.MaxChannelDelta.Should().Be(50);
        tolerated.ChangedFraction.Should().Be(0);
        tolerated.MaxChannelDelta.Should().Be(50);
    }

    [Fact]
    public void Difference_rejects_captures_of_another_size()
    {
        var compare = () => Block().Difference(Fill(9, 10, (_, _) => Grey));
        compare.Should().Throw<ArgumentException>().WithMessage("*differ in size*");
    }
}
